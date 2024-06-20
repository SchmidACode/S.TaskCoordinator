using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using ScheduleTaskCoordinator;
using TaskCoordinator.Properties;

namespace ScheduleTaskCoordinator
{
	public partial class MainForm : Form
	{
		private Connector connector;
		private TimeSpan Delay;
		private TimeSpan FreeTime, BusyTime;
		private Dictionary<DateTime, string> Deadline = new Dictionary<DateTime, string>();

		private ContextMenuStrip contextMenuStrip;
		private DataGridViewRow selectedRow;
		public MainForm()
		{
			InitializeComponent();

			Delay = Settings.Default.DelayTime;
			connector = new Connector();

			dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			dataGridView1.ReadOnly = true;

			Controls.Add(dataGridView1);
			LoadAndUpdateData();

			dataGridView1.ColumnHeaderMouseClick += DataGridView1_ColumnHeaderMouseClick;
			dataGridView1.DataBindingComplete += DataGridView1_DataBindingComplete;

			// contextmenustrip
			contextMenuStrip = new ContextMenuStrip();
			var postponeMenuItem = new ToolStripMenuItem("Отложить задание", null, PostponeMenuItem_Click);
			//var deleteMenuItem = new ToolStripMenuItem("Удалить задание", null, DeleteMenuItem_Click);
			contextMenuStrip.Items.Add(postponeMenuItem);
			//contextMenuStrip.Items.Add(deleteMenuItem);

			dataGridView1.MouseDown += DataGridView1_MouseDown;
			dataGridView1.ContextMenuStrip = contextMenuStrip;

			//MessageBox.Show($"{TimeSpan.FromMilliseconds(TimeSpan.FromHours(24).TotalMilliseconds - DateTime.Now.TimeOfDay.TotalMilliseconds)}");
			timer1.Interval = Convert.ToInt32(TimeSpan.FromHours(24).TotalMilliseconds - DateTime.Now.TimeOfDay.TotalMilliseconds);
			if (Settings.Default.LastOnlineTime != DateTime.MinValue)
			{
				if (Settings.Default.LastOnlineTime.AddDays(1) <= DateTime.Now)
				{
					DeleteExpiredTasks();
				}
			}
		}
		private void DeleteExpiredTasks()
		{
			try
			{
				DateTime lastOnlineTime = Settings.Default.LastOnlineTime;

				string deleteQuery = $"DELETE FROM Tasks WHERE DueDate <= '{lastOnlineTime.ToString("yyyy-MM-dd HH:mm:ss")}'";
				connector.ExecuteQuery(deleteQuery);
				LoadAndUpdateData();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error deleting expired tasks: {ex.Message}");
			}
		}
		private void LoadAndUpdateData()
		{
			try
			{
				Deadline.Clear();
				FreeTime = BusyTime = TimeSpan.Zero;
				DataTable scheduleData = connector.LoadSortedDataFromDB("SELECT * FROM Schedule");
				DataTable tasksData = connector.LoadSortedDataFromDB("SELECT * FROM Tasks");

				DataTable mergedData = MergeScheduleAndTasks(scheduleData, tasksData, Delay);
				dataGridView1.DataSource = mergedData;

				DateTime nearestDeadline = DateTime.MaxValue;
				string nearestDeadlineTask = string.Empty;

				foreach (var entry in Deadline)
				{
					if (entry.Key > DateTime.Now && entry.Key < nearestDeadline)
					{
						nearestDeadline = entry.Key;
						nearestDeadlineTask = entry.Value;
					}
				}

				labelFreeTime.Text = $"Свободное время:\n{FreeTime}";
				labelBusyTime.Text = $"Время отведенное\nна занятия:\n{BusyTime}";
				label1.Text = nearestDeadline == DateTime.MaxValue
					? "Нет предстоящих \nзаданий"
					: $"Время до истечения\nближайшего задания:\n{nearestDeadline}\n{nearestDeadlineTask}";

				dataGridView1.Columns["Type"].Visible = false;
				dataGridView1.Columns["DayOfWeekNumber"].Visible = false;
				dataGridView1.Columns["TaskId"].Visible = false;
				//dataGridView1.Sort(dataGridView1.Columns["DayOfWeekNumber"], ListSortDirection.Ascending);
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
		//private void DeleteMenuItem_Click(object sender, EventArgs e){}
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
			mergedTable.Columns.Add("TaskId", typeof(int)).AllowDBNull = true; // Hidden column for postpone

			int today = (int)DateTime.Now.DayOfWeek;
			var groupedSchedule = scheduleTable.AsEnumerable()
				.GroupBy(row => row.Field<int>("DayOfWeek"))
				.OrderBy(group => (group.Key - today + 7) % 7);
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
							mergedTable.Rows.Add(russianDayOfWeek, $"{freeTimeStart} - {freeTimeEnd}", "Free", DBNull.Value, DBNull.Value, dayOfWeek, DBNull.Value);
						}
					}

					// Добавляем запланированную задачу расписанием
					mergedTable.Rows.Add(russianDayOfWeek, $"{startTime} - {endTime}", "Scheduled", DBNull.Value, row.Field<string>("Plan"), dayOfWeek, DBNull.Value);
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
						mergedTable.Rows.Add(russianDayOfWeek, $"{freeTimeStart} - {freeTimeEnd}", "Free", DBNull.Value, DBNull.Value, dayOfWeek, DBNull.Value);
					}
				}
			}

			// Добавляем оставшиеся незапланированные задачи в конец объединенной таблицы
			foreach (var taskRow in tasksList)
			{
				int priority = Convert.ToInt32(taskRow["Priority"]);
				mergedTable.Rows.Add("Нет времени", taskRow["CompleteTime"], "Mandatory", priority, taskRow.Field<string>("Title"), -1, taskRow["Id"]);
			}

			return mergedTable;
		}
		private string InsertTasksIntoFreeTime(DataTable mergedTable, List<DataRow> tasksList, string russianDayOfWeek, int dayOfWeek, TimeSpan freeTimeStart, TimeSpan freeTimeEnd, TimeSpan delay)
		{
			// Чтение config файла
			TimeSpan BorderStartTime = Settings.Default.BorderStartTime;
			TimeSpan BorderEndTime = Settings.Default.BorderEndTime;

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
					TimeSpan taskDelay = TimeSpan.Zero;
					if (taskRow["DelayTime"] != DBNull.Value && !string.IsNullOrEmpty(taskRow["DelayTime"].ToString()))
					{
						taskDelay = TimeSpan.Parse(taskRow["DelayTime"].ToString());
					}

					// Рассчитываем время начала и окончания задания с учетом задержки и задержки при необходимости
					TimeSpan taskStartTime = currentFreeTimeStart + delay + taskDelay;
					TimeSpan taskEndTime = taskStartTime + taskDuration;

					// Если задача не помещается в границы времени, переносим на границу + задержка
					if (taskStartTime < BorderStartTime)
					{
						taskStartTime = BorderStartTime + taskDelay; // Учитываем задержку
						taskEndTime = taskStartTime + taskDuration;
					}
					else if (taskEndTime > BorderEndTime)
					{
						continue; // Если задача не помещается в границы времени, пропускаем ее
					}

					// Проверяем, помещается ли задание в свободное время с учетом задержки между задачами
					if (taskEndTime <= freeTimeEnd)
					{
						int priority = Convert.ToInt32(taskRow["Priority"]);

						// Проверяем, достаточно ли свободного времени между заданиями
						if (currentFreeTimeStart + delay <= taskStartTime)
						{
							// Добавляем начальный интервал свободного времени перед заданием
							if (taskStartTime > currentFreeTimeStart)
							{
								FreeTime += taskStartTime - currentFreeTimeStart;
								mergedTable.Rows.Add(russianDayOfWeek, $"{currentFreeTimeStart} - {taskStartTime}", "Free", DBNull.Value, DBNull.Value, dayOfWeek, DBNull.Value);
							}

							// Добавляем задание в расписание
							BusyTime += taskEndTime - taskStartTime;
							mergedTable.Rows.Add(russianDayOfWeek, $"{taskStartTime} - {taskEndTime}", "Mandatory", priority, taskRow.Field<string>("Title"), dayOfWeek, taskRow["Id"]);
							Deadline.Add(DateTime.Parse(taskRow["DueDate"].ToString()), taskRow["Title"].ToString());

							// Удаляем задачу, как только она будет запланирована
							tasksList.Remove(taskRow);
							currentFreeTimeStart = taskEndTime; // Обновляем текущее время начала свободного интервала до окончания этого задания
						}
						else {
							continue;
						}
					}
				}
			}

			// Добавляем оставшееся свободное время после запланированных задач
			if (currentFreeTimeStart < freeTimeEnd)
			{
				FreeTime += freeTimeEnd - currentFreeTimeStart;
				mergedTable.Rows.Add(russianDayOfWeek, $"{currentFreeTimeStart} - {freeTimeEnd}", "Free", DBNull.Value, DBNull.Value, dayOfWeek, DBNull.Value);
			}

			return freeTimeEnd.ToString(); // Возвращаем время окончания свободного интервала
		}
		private string GetRussianDayOfWeek(int dayOfWeek)
		{
			string[] russianDays = { "Воскресенье", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота" };
			return russianDays[dayOfWeek % 7];
		}
		private void DataGridView1_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				var hitTestInfo = dataGridView1.HitTest(e.X, e.Y);
				if (hitTestInfo.RowIndex >= 0)
				{
					dataGridView1.ClearSelection();
					dataGridView1.Rows[hitTestInfo.RowIndex].Selected = true;
					selectedRow = dataGridView1.Rows[hitTestInfo.RowIndex];
				}
				else
				{
					selectedRow = null;
				}
			}
		}
		private void PostponeMenuItem_Click(object sender, EventArgs e)
		{
			if (selectedRow != null)
			{
				string taskType = selectedRow.Cells["Type"].Value.ToString();
				if (taskType == "Mandatory")
				{
					PostponeForm postponeForm = new PostponeForm((int)selectedRow.Cells["TaskId"].Value);
					postponeForm.ShowDialog();
					LoadAndUpdateData();
				}
				else
				{
					MessageBox.Show("Можно откладывать только обязательные задания.");
				}
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
			DelayForm delayForm = new DelayForm();
			delayForm.ShowDialog();
			LoadAndUpdateData();
		}
		private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			Settings.Default.LastOnlineTime = DateTime.Now;
			Settings.Default.Save();
		}
		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			Settings.Default.LastOnlineTime = DateTime.Now;
			Settings.Default.Save();
		}
		private void timer1_Tick(object sender, EventArgs e)
		{
			DeleteExpiredTasks();
			timer1.Interval = 24 * 60 * 60 * 1000;
		}
	}
}
