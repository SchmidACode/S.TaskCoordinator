using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace App
{
	public partial class MainForm : Form
	{
		DataGridView dataGridView1;
		Connector connector;
		TimeSpan Delay = TimeSpan.Parse("00:20:00");

		public MainForm()
		{
			InitializeComponent();

			connector = new Connector();
			dataGridView1 = new DataGridView
			{
				Dock = DockStyle.Fill,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				ReadOnly = true
			};

			Controls.Add(dataGridView1);

			DataTable scheduleData = connector.LoadSortedDataFromDB("SELECT * FROM Schedule");
			DataTable tasksData = connector.LoadSortedDataFromDB("SELECT * FROM Tasks");

			DataTable mergedData = MergeScheduleAndTasks(scheduleData, tasksData, Delay);
			dataGridView1.DataSource = mergedData;

			dataGridView1.Columns["Type"].Visible = false;
			dataGridView1.Columns["DayOfWeekNumber"].Visible = false;
			dataGridView1.Sort(dataGridView1.Columns["DayOfWeekNumber"], ListSortDirection.Ascending);

			dataGridView1.ColumnHeaderMouseClick += DataGridView1_ColumnHeaderMouseClick;
			dataGridView1.DataBindingComplete += DataGridView1_DataBindingComplete;
		}
		//public void VerifyDataTypes(DataTable dataTable)
		//{
		//	foreach (DataColumn column in dataTable.Columns)
		//	{
		//		Console.WriteLine($"{column.ColumnName}: {column.DataType}");
		//	}
		//}
		private DataTable ConvertColumnType(DataTable dt, string columnName, Type newType)
		{
			DataTable newTable = dt.Clone();
			newTable.Columns[columnName].DataType = newType;
			foreach (DataRow row in dt.Rows)
				newTable.ImportRow(row);
			foreach (DataRow row in newTable.Rows)
				row[columnName] = Convert.ChangeType(row[columnName], newType);

			return newTable;
		}
		public DataTable MergeScheduleAndTasks(DataTable scheduleTable, DataTable tasksTable, TimeSpan delay)
		{
			//VerifyDataTypes(scheduleTable);
			//VerifyDataTypes(tasksTable);

			if (scheduleTable.Columns["DayOfWeek"].DataType != typeof(int))
				scheduleTable = ConvertColumnType(scheduleTable, "DayOfWeek", typeof(int));

			DataTable mergedTable = new DataTable();
			mergedTable.Columns.Add("DayOfWeek", typeof(string));
			mergedTable.Columns.Add("TimeInterval", typeof(string));
			mergedTable.Columns.Add("Type", typeof(string)); // Free, Scheduled, Mandatory
			mergedTable.Columns.Add("Priority", typeof(int)).AllowDBNull = true;
			mergedTable.Columns.Add("Plan", typeof(string)).AllowDBNull = true; // Plan from both tables
			mergedTable.Columns.Add("DayOfWeekNumber", typeof(int)); // Hidden column for sorting

			var groupedSchedule = scheduleTable.AsEnumerable().GroupBy(row => row.Field<int>("DayOfWeek"));
			var tasksList = tasksTable.AsEnumerable().ToList();

			foreach (var group in groupedSchedule)
			{
				int dayOfWeek = group.Key;
				var daySchedule = group.OrderBy(row => TimeSpan.Parse(row.Field<string>("StartTime"))).ToList();
				string russianDayOfWeek = GetRussianDayOfWeek(dayOfWeek);

				string lastEndTime = "00:00:00";
				foreach (var row in daySchedule)
				{
					string startTime = row.Field<string>("StartTime");
					string endTime = row.Field<string>("EndTime");

					// Добавляем свободное время, если между последним временем окончания и текущим временем начала есть разрыв
					if (TimeSpan.Parse(startTime) > TimeSpan.Parse(lastEndTime))
					{
						TimeSpan freeTimeStart = TimeSpan.Parse(lastEndTime);
						TimeSpan freeTimeEnd = TimeSpan.Parse(startTime);

						// Проверяем, достаточно ли свободного времени для задачи, включая задержку
						TimeSpan freeTimeWithDelay = freeTimeEnd - freeTimeStart - delay;
						if (freeTimeWithDelay >= TimeSpan.Zero)
						{
							//mergedTable.Rows.Add(russianDayOfWeek, $"{freeTimeStart} - {freeTimeEnd}", "Free", DBNull.Value, DBNull.Value, dayOfWeek);//

							// Вставляем задачи, которые вписываются в это свободное время с задержкой
							lastEndTime = InsertTasksIntoFreeTime(mergedTable, tasksList, russianDayOfWeek, dayOfWeek, freeTimeStart, freeTimeEnd, delay);
						}
						else
						{
							// Недостаточно времени на задачу после применения задержки, добавляем свободное время
							mergedTable.Rows.Add(russianDayOfWeek, $"{freeTimeStart} - {freeTimeEnd}", "Free", DBNull.Value, DBNull.Value, dayOfWeek);
						}
					}

					// Добавляем запланированную задачу расписанием
					mergedTable.Rows.Add(russianDayOfWeek, $"{startTime} - {endTime}", "Scheduled", DBNull.Value, row.Field<string>("Plan"), dayOfWeek);
					lastEndTime = endTime;
				}

				// Добавляем свободное время от последнего запланированного действия до конца дня
				if (TimeSpan.Parse(lastEndTime) < TimeSpan.Parse("23:59:59"))
				{
					TimeSpan freeTimeStart = TimeSpan.Parse(lastEndTime);
					TimeSpan freeTimeEnd = TimeSpan.Parse("23:59:59");

					// Проверяем, достаточно ли свободного времени для задачи, включая задержку
					TimeSpan freeTimeWithDelay = freeTimeEnd - freeTimeStart - delay;
					if (freeTimeWithDelay >= TimeSpan.Zero)
					{
						//mergedTable.Rows.Add(russianDayOfWeek, $"{freeTimeStart} - {freeTimeEnd}", "Free", DBNull.Value, DBNull.Value, dayOfWeek);

						// Вставляем задачи, которые вписываются в это свободное время с задержкой
						lastEndTime = InsertTasksIntoFreeTime(mergedTable, tasksList, russianDayOfWeek, dayOfWeek, freeTimeStart, freeTimeEnd, delay);
					}
					else
					{
						// Недостаточно времени на задачу после применения задержки, добавляем свободное время
						mergedTable.Rows.Add(russianDayOfWeek, $"{freeTimeStart} - {freeTimeEnd}", "Free", DBNull.Value, DBNull.Value, dayOfWeek);
					}
				}
			}

			// Добавляем оставшиеся незапланированные задачи в конец объединенной таблицы
			foreach (var taskRow in tasksList)
			{
				int priority = Convert.ToInt32(taskRow["Priority"]);
				mergedTable.Rows.Add("Нет времени", taskRow["CompleteTime"], "Mandatory", priority, taskRow.Field<string>("Title"), -1);
			}

			return mergedTable;
		}
		private string InsertTasksIntoFreeTime(DataTable mergedTable, List<DataRow> tasksList, string russianDayOfWeek, int dayOfWeek, TimeSpan freeTimeStart, TimeSpan freeTimeEnd, TimeSpan delay)
		{
			foreach (var taskRow in tasksList.ToList())
			{
				TimeSpan taskDuration = TimeSpan.Parse(taskRow["CompleteTime"].ToString());

				// Вычисляем время начала и окончания с учетом задержки
				TimeSpan taskStartTime = freeTimeStart + delay;
				TimeSpan taskEndTime = taskStartTime + taskDuration;

				if (taskEndTime <= freeTimeEnd)
				{
					int priority = Convert.ToInt32(taskRow["Priority"]);
					mergedTable.Rows.Add(russianDayOfWeek, $"{freeTimeStart} - {taskStartTime}", "Free", DBNull.Value, DBNull.Value, dayOfWeek);
					mergedTable.Rows.Add(russianDayOfWeek, $"{taskStartTime} - {taskEndTime}", "Mandatory", priority, taskRow.Field<string>("Title"), dayOfWeek);
					tasksList.Remove(taskRow); // Удаляем задачу, как только она будет запланирована

					// Добавляем оставшееся свободное время после запланированной задачи
					if (taskEndTime < freeTimeEnd)
					{
						mergedTable.Rows.Add(russianDayOfWeek, $"{taskEndTime} - {freeTimeEnd}", "Free", DBNull.Value, DBNull.Value, dayOfWeek);
					}

					return taskEndTime.ToString(); // Возвращаем время окончания вставленной задачи
				}
			}
			mergedTable.Rows.Add(russianDayOfWeek, $"{freeTimeStart} - {freeTimeEnd}", "Free", DBNull.Value, DBNull.Value, dayOfWeek);
			return freeTimeStart.ToString(); // Возвращаем исходное время начала, если задача не была вставлена
		}
		private string GetRussianDayOfWeek(int dayOfWeek)
		{
			switch ((DayOfWeek)dayOfWeek)
			{
				case DayOfWeek.Sunday: return "Воскресенье";
				case DayOfWeek.Monday: return "Понедельник";
				case DayOfWeek.Tuesday: return "Вторник";
				case DayOfWeek.Wednesday: return "Среда";
				case DayOfWeek.Thursday: return "Четверг";
				case DayOfWeek.Friday: return "Пятница";
				case DayOfWeek.Saturday: return "Суббота";
				default: return "";
			}
		}
		private void DataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			DataGridViewColumn clickedColumn = dataGridView1.Columns[e.ColumnIndex];

			if (clickedColumn.Name == "DayOfWeek")
				dataGridView1.Sort(dataGridView1.Columns["DayOfWeekNumber"], ListSortDirection.Ascending);
		}
		private void DataGridView1_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
		{
			foreach (DataGridViewRow row in dataGridView1.Rows)
			{
				string type = row.Cells["Type"].Value.ToString();
				if (type == "Free")
					row.DefaultCellStyle.BackColor = Color.LightGreen;
				else if (type == "Scheduled")
					row.DefaultCellStyle.BackColor = Color.LightSkyBlue;
				else if (type == "Mandatory")
					row.DefaultCellStyle.BackColor = Color.PaleVioletRed;
			}
			dataGridView1.ClearSelection();
		}
		private void ScheduleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ScheduleForm form = new ScheduleForm();
			form.ShowDialog();
		}
		private void CaseToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CasesForm form = new CasesForm();
			form.ShowDialog();
		}
		private void DelayToolStripMenuItem_Click(object sender, EventArgs e)
		{
			
		}
	}
}
