using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Linq;
using System.Drawing;

namespace ScheduleTaskCoordinator
{
	public partial class ScheduleForm : Form
	{
		private Connector connector;

		public ScheduleForm()
		{
			InitializeComponent();
			connector = new Connector();
			LoadTable();

			DayOfWeek[] daysOfWeek = (DayOfWeek[])Enum.GetValues(typeof(DayOfWeek));

			foreach (DayOfWeek dayOfWeek in daysOfWeek)
			{
				string russianDay = GetRussianDayOfWeek((int)dayOfWeek);
				comboBox1.Items.Add(russianDay);
			}
			comboBox1.SelectedIndex = 1;
			dataGridView1.ContextMenuStrip = contextMenuStrip1;
			dataGridView1.AllowUserToDeleteRows = false;

			Icon icon = new Icon(SystemIcons.Question, btnQuestion.Height, btnQuestion.Width);
			Bitmap bitmap = icon.ToBitmap();
			btnQuestion.BackgroundImage = bitmap;
			btnQuestion.BackgroundImageLayout = ImageLayout.Zoom;
			btnQuestion.FlatAppearance.MouseOverBackColor = btnQuestion.BackColor;

			this.KeyDown += new KeyEventHandler(Form1_KeyDown); this.KeyPreview = true;
		}
		private void LoadTable()
		{
			try
			{
				string query = @"
                        SELECT * FROM Schedule 
                        ORDER BY 
                            CASE 
                                WHEN DayOfWeek = 0 THEN 7  -- Воскресенье - последний день недели
                                ELSE DayOfWeek 
                            END, 
                            StartTime";
				DataTable dataTable = connector.LoadSortedDataFromDB(query);

				dataTable.Columns.Add("Время от - до", typeof(string));
				foreach (DataRow row in dataTable.Rows)
				{
					row["Время от - до"] = row["StartTime"] + " - " + row["EndTime"];
				}

				dataTable.Columns.Add("День недели", typeof(string));
				foreach (DataRow row in dataTable.Rows)
				{
					int dayOfWeek = Convert.ToInt32(row["DayOfWeek"]);
					row["День недели"] = GetRussianDayOfWeek(dayOfWeek);
				}

				dataGridView1.DataSource = dataTable;

				dataGridView1.Columns["Plan"].HeaderText = "План";
				dataGridView1.Columns["Id"].Visible = false;
				dataGridView1.Columns["DayOfWeek"].Visible = false;
				dataGridView1.Columns["StartTime"].Visible = false;
				dataGridView1.Columns["EndTime"].Visible = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void AddScheduleItem()
		{
			string plan = richTextBox1.Text;
			DayOfWeek day = (DayOfWeek)comboBox1.SelectedIndex;
			TimeSpan start = dateStartTime.Value.TimeOfDay;
			TimeSpan end = dateEndTime.Value.TimeOfDay;
			if (start + TimeSpan.FromMinutes(5) > end)
			{
				MessageBox.Show("Время окончания должно быть позже времени начала как минимум на 5 минут.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			DataTable scheduleData = connector.LoadSortedDataFromDB("SELECT * FROM Schedule");
			foreach (DataRow row in scheduleData.Rows)
			{
				int dayOfWeek = Convert.ToInt32(row["DayOfWeek"]);
				TimeSpan existingStart = TimeSpan.Parse(row["StartTime"].ToString());
				TimeSpan existingEnd = TimeSpan.Parse(row["EndTime"].ToString());

				if (dayOfWeek == (int)day && !(end <= existingStart || start >= existingEnd))
				{
					MessageBox.Show("Расписание пересекается с существующим элементом.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}
			connector.InsertDataToBase("Schedule", "Plan, DayOfWeek, StartTime, EndTime", $"'{plan}', {(int)day}, '{start}', '{end}'");
			LoadTable();
		}
		private void DeleteSelectedRow()
		{
			if (dataGridView1.SelectedRows.Count > 0 )
			{
				DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];
				if (selectedRow.Cells["Id"].Value == DBNull.Value) return;
				int ID = Convert.ToInt32(selectedRow.Cells["Id"].Value);
				connector.DeleteDataFromBase("Schedule", ID);
				foreach (DataGridViewRow row in dataGridView1.SelectedRows)
				{
					dataGridView1.Rows.RemoveAt(row.Index);
				}
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите строку для удаления.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}
		private string GetRussianDayOfWeek(int dayOfWeek)
		{
			string[] russianDays = { "Воскресенье", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота" };
			return russianDays[dayOfWeek % 7];
		}
		private void btnAdd_Click(object sender, EventArgs e)
		{
			AddScheduleItem();
		}
		private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DeleteSelectedRow();
		}
		private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete) {
				DeleteSelectedRow();
			}
		}
		private void btnQuestion_Click(object sender, EventArgs e)
		{
			string str = @"Пожалуйста, укажите хотя бы примерное время сна для каждого дня недели. Это поможет правильно составить ваше расписание. Также, важно добавить дела, которые повторяются каждый день. 

Остальные задачи, которые не выполняются ежедневно, должны находиться в другой таблице!";
			MessageBox.Show(str, "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		private void btnFinish_Click(object sender, EventArgs e)
		{
			if (connector.IsExists("Schedule"))
			{
				this.DialogResult = DialogResult.OK;
				this.Close();
			}
			else
				MessageBox.Show("Настройка плана должна быть как минимум на 1 день", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
        private void Form1_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter) {
				AddScheduleItem();
			}
		}
	}
}
