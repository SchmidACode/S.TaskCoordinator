using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScheduleTaskCoordinator
{
    public partial class CasesForm : Form
    {
        private Connector connector;
        private int? currentEditingTaskId = null;

        public CasesForm()
        {
            InitializeComponent();
            connector = new Connector();

            dateTimePicker.Value = DateTime.Now.Date.AddDays(1);
            LoadTable();
            dataGridView.ContextMenuStrip = contextMenuStrip1;
            dataGridView.AllowUserToDeleteRows = false;

            Icon icon = new Icon(SystemIcons.Question, btnQuestion.Height, btnQuestion.Width);
            Bitmap bitmap = icon.ToBitmap();
            btnQuestion.BackgroundImage = bitmap;
            btnQuestion.BackgroundImageLayout = ImageLayout.Zoom;
            btnQuestion.FlatAppearance.MouseOverBackColor = btnQuestion.BackColor;
            dateTimePicker.Value.AddDays(1);

            this.KeyDown += new KeyEventHandler(Form1_KeyDown); this.KeyPreview = true;
        }

        private void LoadTable()
        {
            try
            {
                string query = @"
                SELECT *
                FROM Tasks
                ORDER BY DueDate ASC";
                DataTable dataTable = connector.LoadSortedDataFromDB(query);

                dataGridView.DataSource = dataTable;

                dataGridView.Columns["Title"].HeaderText = "Дела";
                dataGridView.Columns["Id"].Visible = false;
                dataGridView.Columns["DelayTime"].Visible = false;
                dataGridView.Columns["StartTime"].Visible = false;
                dataGridView.Columns["EndTime"].Visible = false;
                dataGridView.Columns["CompleteTime"].HeaderText = "Время на выполение";
                dataGridView.Columns["DueDate"].HeaderText = "Последний срок";
                dataGridView.Columns["Priority"].HeaderText = "Приоритет";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearForm()
        {
            richTextBox.Text = "";
            dateTimePicker.Value = DateTime.Now.Date.AddDays(1);
            numericUpDown.Value = 1;
            currentEditingTaskId = null;
        }

        private void AddTaskItem()
        {
            TimeSpan time = dateTimePicker1.Value.TimeOfDay;
            DateTime dueDate = dateTimePicker.Value;
            string title = richTextBox.Text;
            short priority = Convert.ToInt16(numericUpDown.Value);
            TimeSpan delayTime = TimeSpan.Zero;

            if (time == TimeSpan.Zero)
            {
                MessageBox.Show("Пожалуйста, введите время для задания.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (currentEditingTaskId.HasValue)
            {
                int ID = currentEditingTaskId.Value;
                connector.DeleteDataFromBase("Tasks", ID);
                connector.InsertDataToBase("Tasks", "Title, CompleteTime, DueDate, Priority, DelayTime", $"'{title}', '{time}', '{dueDate}', {priority}, '{delayTime}'");
                MessageBox.Show("Задача обновлена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                currentEditingTaskId = null;
                ClearForm();
            }
            else
                connector.InsertDataToBase("Tasks", "Title, CompleteTime, DueDate, Priority, DelayTime", $"'{title}', '{time}', '{dueDate}', {priority}, '{delayTime}'");

            LoadTable();
        }

        private void DeleteSelectedRow()
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dataGridView.SelectedRows[0];
                if (selectedRow.Cells["Id"].Value == DBNull.Value) return;
                int ID = Convert.ToInt32(selectedRow.Cells["Id"].Value);
                connector.DeleteDataFromBase("Tasks", ID);

                foreach (DataGridViewRow row in dataGridView.SelectedRows)
                {
                    dataGridView.Rows.RemoveAt(row.Index);
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите строку для удаления.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void EditSelectedRow()
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dataGridView.SelectedRows[0];
                if (selectedRow.Cells["Id"].Value == DBNull.Value) return;
                int ID = Convert.ToInt32(selectedRow.Cells["Id"].Value);
                DataTable data = connector.LoadTaskById(ID);
                if (data.Rows.Count > 0)
                {
                    DataRow row = data.Rows[0];
                    richTextBox.Text = row["Title"].ToString();

                    if (TimeSpan.TryParse(row["CompleteTime"].ToString(), out TimeSpan completeTime))
                    {
                        dateTimePicker1.Value = DateTime.Now.Date.Add(completeTime);
                    }

                    if (DateTime.TryParse(row["DueDate"].ToString(), out DateTime dueDate))
                    {
                        dateTimePicker.Value = dueDate;
                    }

                    if (short.TryParse(row["Priority"].ToString(), out short priority))
                    {
                        numericUpDown.Value = priority;
                    }

                    currentEditingTaskId = ID;
                }
                else
                {
                    MessageBox.Show("Задача не найдена.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите строку для редактирования.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AddTaskItem();
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteSelectedRow();
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditSelectedRow();
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedRow();
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                AddTaskItem();
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown.Value > 100) numericUpDown.Value = 100;
            if (numericUpDown.Value < 0) numericUpDown.Value = 0;
        }

        private void btnQuestion_Click(object sender, EventArgs e)
        {
            string str = @"Здесь нужно добавлять дела, которые нужно сделать до определенной даты.

Остальные задачи, которые выполняются ежедневно, должны находиться в другой таблице!";
            MessageBox.Show(str, "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}