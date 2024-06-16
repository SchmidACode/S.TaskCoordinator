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

namespace App
{
	public partial class CasesForm : Form
	{
		Connector connector;
		public CasesForm()
		{
			InitializeComponent();
			connector = new Connector();

			LoadTable();
			dataGridView.ContextMenuStrip = contextMenuStrip1;

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
				dataGridView.Columns["CompleteTime"].HeaderText = "Время на выполение";
				dataGridView.Columns["DueDate"].HeaderText = "Последний срок";
				dataGridView.Columns["Priority"].HeaderText = "Приоритет";
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void AddTaskItem() {
			TimeSpan time = dateTimePicker1.Value.TimeOfDay;
			DateTime dueDate = dateTimePicker.Value;
			string title = richTextBox.Text;
			short priority = Convert.ToInt16(numericUpDown.Value);

			connector.InsertDataToBase("Tasks", "Title, CompleteTime, DueDate, Priority", $"'{title}', '{time}', '{dueDate}', {priority}");
			LoadTable();
		}
		private void DeleteSelectedRow()
		{
			if (dataGridView.SelectedRows.Count > 0)
			{
				DataGridViewRow selectedRow = dataGridView.SelectedRows[0];
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
		private void btnAdd_Click(object sender, EventArgs e)
		{
			AddTaskItem();
		}
		private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DeleteSelectedRow();
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
				this.DialogResult = DialogResult.OK;
				this.Close();
		}
	}
}
