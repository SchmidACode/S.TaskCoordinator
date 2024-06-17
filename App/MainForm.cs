using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace App
{
	public partial class MainForm : Form
	{
		private string configFilePath = "config.cfg";
		private Connector connector;
		private TimeSpan Delay;
		private TimeSpan FreeTime = TimeSpan.Zero, BusyTime = TimeSpan.Zero;

		public MainForm()
		{
			InitializeComponent();

			Delay = TimeSpan.Parse(File.ReadAllLines(configFilePath)[0]);
			connector = new Connector();

			dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			dataGridView1.ReadOnly = true;

			Controls.Add(dataGridView1);
			LoadAndUpdateData();

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
		private void LoadAndUpdateData()
		{
			try
			{
				FreeTime = BusyTime = TimeSpan.Zero;
				DataTable scheduleData = connector.LoadSortedDataFromDB("SELECT * FROM Schedule");
				DataTable tasksData = connector.LoadSortedDataFromDB("SELECT * FROM Tasks");

				DataTable mergedData = MergeScheduleAndTasks(scheduleData, tasksData, Delay);
				dataGridView1.DataSource = mergedData;

				labelFreeTime.Text = $"Свободное время:\n{FreeTime}";
				labelBusyTime.Text = $"Время отведенное\nна занятия:\n{BusyTime}";

				dataGridView1.Columns["Type"].Visible = false;
				dataGridView1.Columns["DayOfWeekNumber"].Visible = false;
				dataGridView1.Sort(dataGridView1.Columns["DayOfWeekNumber"], ListSortDirection.Ascending);
				dataGridView1.ClearSelection();

				dataGridView1.Columns["DayOfWeek"].HeaderText = "День недели";
				dataGridView1.Columns["TimeInterval"].HeaderText = "Время";
				dataGridView1.Columns["Priority"].HeaderText = "Приоритет";
				dataGridView1.Columns["Plan"].HeaderText = "Название";
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
			}
		}
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
			// Чтение config файла
			string[] configLines = File.ReadAllLines(configFilePath);
			TimeSpan BorderStartTime = TimeSpan.Parse(configLines[1]);
			TimeSpan BorderEndTime = TimeSpan.Parse(configLines[2]);

			TimeSpan currentFreeTimeStart = freeTimeStart;

			// Сортируем задачи по приоритету в порядке убывания (более высокий приоритет первым)
			var sortedTasks = tasksList.OrderByDescending(task => Convert.ToInt32(task["Priority"])).ToList();

			// Перебираем каждое задание и пытаемся вписать его в свободное время
			foreach (var taskRow in sortedTasks.ToList())
			{
				TimeSpan taskDuration = TimeSpan.Parse(taskRow["CompleteTime"].ToString());
				DateTime dueDate = DateTime.Parse(taskRow["DueDate"].ToString());

				if (dueDate > DateTime.Now + taskDuration) // Проверяем, не просрочено ли задание
				{
					// Рассчитываем время начала и окончания задания с учетом задержки
					TimeSpan taskStartTime = currentFreeTimeStart + delay;
					TimeSpan taskEndTime = taskStartTime + taskDuration;

					// Проверяем, помещается ли задание в пределах границ и свободного времени
					if (BorderStartTime <= taskStartTime && taskEndTime <= BorderEndTime && taskEndTime <= freeTimeEnd)
					{
						int priority = Convert.ToInt32(taskRow["Priority"]);

						// Добавляем начальный интервал свободного времени перед заданием
						if (taskStartTime > currentFreeTimeStart) {
							FreeTime += taskStartTime - currentFreeTimeStart;
							mergedTable.Rows.Add(russianDayOfWeek, $"{currentFreeTimeStart} - {taskStartTime}", "Free", DBNull.Value, DBNull.Value, dayOfWeek);
						}

						// Добавляем задание в расписание
						BusyTime += taskEndTime - taskStartTime;
						mergedTable.Rows.Add(russianDayOfWeek, $"{taskStartTime} - {taskEndTime}", "Mandatory", priority, taskRow.Field<string>("Title"), dayOfWeek);
						tasksList.Remove(taskRow);  // Удаляем задачу, как только она будет запланирована

						currentFreeTimeStart = taskEndTime; // Обновляем текущее время начала свободного интервала до окончания этого задания
					}
				}
			}

			// Добавляем оставшееся свободное время после запланированной задачи
			if (currentFreeTimeStart < freeTimeEnd) {
				FreeTime += freeTimeEnd - currentFreeTimeStart;
				mergedTable.Rows.Add(russianDayOfWeek, $"{currentFreeTimeStart} - {freeTimeEnd}", "Free", DBNull.Value, DBNull.Value, dayOfWeek);
			}

			return freeTimeEnd.ToString(); // Возвращаем время окончания свободного интервала
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
			LoadAndUpdateData();
		}
		private void CaseToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CasesForm form = new CasesForm();
			form.ShowDialog();
			LoadAndUpdateData();
		}
		private void DelayToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DelayForm delayForm = new DelayForm(configFilePath);
			delayForm.ShowDialog();
			Delay = TimeSpan.Parse(File.ReadAllLines(configFilePath)[0]);
			LoadAndUpdateData();
		}
		private void timer1_Tick(object sender, EventArgs e)
		{
			LoadAndUpdateData();
		}
	}
}
