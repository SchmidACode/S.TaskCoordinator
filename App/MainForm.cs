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
using TaskCoordinator;

namespace ScheduleTaskCoordinator
{
    public partial class MainForm : Form
    {
        private Connector connector;
        private TimeSpan FreeTime, BusyTime;
        // Словарь для хранения ближайшего дедлайна: ключ – пара (DueDate, TaskId)
        private Dictionary<TaskKey, string> Deadline = new Dictionary<TaskKey, string>();

        private ContextMenuStrip contextMenuStrip;
        private DataGridViewRow selectedRow;

        public MainForm()
        {
            InitializeComponent();
            connector = new Connector();

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.ReadOnly = true;
            Controls.Add(dataGridView1);

            // Инициализация событий и контекстного меню
            dataGridView1.ColumnHeaderMouseClick += DataGridView1_ColumnHeaderMouseClick;
            dataGridView1.DataBindingComplete += DataGridView1_DataBindingComplete;
            dataGridView1.MouseClick += dataGridView1_MouseClick;
            dataGridView1.MouseDown += DataGridView1_MouseDown;
            contextMenuStrip = new ContextMenuStrip();
            var postponeMenuItem = new ToolStripMenuItem("Отложить задание", null, PostponeMenuItem_Click);
            var borderTaskMenuItem = new ToolStripMenuItem("Ограничить задание", null, BorderTaskMenuItem_Click);
            contextMenuStrip.Items.Add(postponeMenuItem);
            contextMenuStrip.Items.Add(borderTaskMenuItem);
            dataGridView1.ContextMenuStrip = contextMenuStrip;

            LoadAndUpdateData();
            DeleteExpiredTasks();

            // Настройка таймера: первый интервал до конца дня
            timer1.Interval = Convert.ToInt32(TimeSpan.FromHours(24).TotalMilliseconds - DateTime.Now.TimeOfDay.TotalMilliseconds);
            if (Settings.Default.LastOnlineTime != DateTime.MinValue)
            {
                if (Settings.Default.LastOnlineTime.AddMilliseconds(TimeSpan.FromHours(24).TotalMilliseconds - DateTime.Now.TimeOfDay.TotalMilliseconds) < DateTime.Now)
                {
                    DeleteLoggedTasks();
                }
            }
        }

        private void DeleteExpiredTasks()
        {
            try
            {
                string deleteQuery = $"DELETE FROM Tasks WHERE DueDate <= '{DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}'";
                connector.ExecuteQuery(deleteQuery);
                LoadAndUpdateData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting expired tasks: {ex.Message}");
            }
        }

        private void DeleteLoggedTasks()
        {
            try
            {
                string filePath = "task_ids.txt";
                if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
                {
                    string[] taskIds = File.ReadAllLines(filePath);
                    foreach (string taskId in taskIds)
                    {
                        string deleteQuery = $"DELETE FROM Tasks WHERE Id = {taskId}";
                        connector.ExecuteQuery(deleteQuery);
                    }
                    File.WriteAllText(filePath, string.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting logged tasks: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновляет данные в DataGridView, пересоздавая объединённую таблицу.
        /// </summary>
        private void LoadAndUpdateData()
        {
            try
            {
                Deadline.Clear();
                FreeTime = BusyTime = TimeSpan.Zero;
                DataTable scheduleData = connector.LoadSortedDataFromDB("SELECT * FROM Schedule");
                DataTable tasksData = connector.LoadSortedDataFromDB("SELECT * FROM Tasks");
                DataTable mergedData = MergeScheduleAndTasks(scheduleData, tasksData);
                DeletePastDataInTable(mergedData);
                dataGridView1.DataSource = mergedData;
                dataGridView1.Refresh();

                DateTime nearestDeadline = DateTime.MaxValue;
                string nearestDeadlineTask = string.Empty;
                foreach (var entry in Deadline)
                {
                    if (entry.Key.DueDate > DateTime.Now && entry.Key.DueDate < nearestDeadline)
                    {
                        nearestDeadline = entry.Key.DueDate;
                        nearestDeadlineTask = entry.Value;
                    }
                }

                labelFreeTime.Text = $"Свободное время:\n{FreeTime}";
                labelBusyTime.Text = $"Время отведенное\nна занятия:\n{BusyTime}";
                label1.Text = nearestDeadline == DateTime.MaxValue
                    ? "Нет предстоящих \nзаданий"
                    : $"Время до истечения\nближайшего задания:\n{nearestDeadline}\n{nearestDeadlineTask}";

                // Скрываем служебные столбцы
                dataGridView1.Columns["Type"].Visible = false;
                dataGridView1.Columns["DayOfWeekNumber"].Visible = false;
                dataGridView1.Columns["TaskId"].Visible = false;

                dataGridView1.ClearSelection();
                dataGridView1.Columns["DayOfWeek"].HeaderText = "День недели";
                dataGridView1.Columns["TimeInterval"].HeaderText = "Время";
                dataGridView1.Columns["Priority"].HeaderText = "Приоритет";
                dataGridView1.Columns["Plan"].HeaderText = "Название";

                LogTaskIds(mergedData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Удаляет из объединённой таблицы записи, для которых время уже прошло.
        /// </summary>
        private void DeletePastDataInTable(DataTable mergedData)
        {
            List<DataRow> rowsToRemove = new List<DataRow>();
            foreach (DataRow dataRow in mergedData.Rows)
            {
                if ((string)dataRow["DayOfWeek"] != "Нет времени")
                {
                    int dayOfWeek = Convert.ToInt16(dataRow["DayOfWeekNumber"]);
                    string timeIntervalStr = (string)dataRow["TimeInterval"];
                    var timeParts = timeIntervalStr.Split('-');
                    TimeSpan endTime = TimeSpan.Parse(timeParts[1].Trim());
                    if (dayOfWeek == (int)DateTime.Now.DayOfWeek && endTime <= DateTime.Now.TimeOfDay)
                        rowsToRemove.Add(dataRow);
                }
            }
            foreach (var row in rowsToRemove)
                mergedData.Rows.Remove(row);
        }

        /// <summary>
        /// Записывает идентификаторы задач для текущего дня в файл.
        /// </summary>
        private void LogTaskIds(DataTable mergedData)
        {
            string filePath = "task_ids.txt";
            DayOfWeek currentDay = DateTime.Now.DayOfWeek;
            List<string> taskIds = mergedData.AsEnumerable()
                .Where(row => row.Field<int?>("DayOfWeekNumber") == (int)currentDay)
                .Select(row => row["TaskId"].ToString())
                .ToList();
            File.WriteAllLines(filePath, taskIds);
        }

        /// <summary>
        /// Основной метод объединения расписания и задач.
        /// </summary>
        public DataTable MergeScheduleAndTasks(DataTable scheduleTable, DataTable tasksTable)
        {
            if (scheduleTable.Columns["DayOfWeek"].DataType != typeof(int))
                scheduleTable = ConvertColumnType(scheduleTable, "DayOfWeek", typeof(int));

            DataTable mergedTable = new DataTable();
            mergedTable.Columns.Add("DayOfWeek", typeof(string));
            mergedTable.Columns.Add("TimeInterval", typeof(string));
            mergedTable.Columns.Add("Type", typeof(string)); // Free, Scheduled, Mandatory
            mergedTable.Columns.Add("Priority", typeof(int));
            mergedTable.Columns.Add("Plan", typeof(string));
            mergedTable.Columns.Add("DayOfWeekNumber", typeof(int));
            mergedTable.Columns.Add("TaskId", typeof(int));

            int today = (int)DateTime.Now.DayOfWeek;
            // Группируем расписание по дню недели и сортируем относительно текущего дня
            var groupedSchedule = scheduleTable.AsEnumerable()
                .GroupBy(row => row.Field<int>("DayOfWeek"))
                .OrderBy(group => ((group.Key - today + 7) % 7));

            List<DataRow> tasksList = tasksTable.AsEnumerable().ToList();

            foreach (var group in groupedSchedule)
            {
                int dayOfWeek = group.Key;
                string russianDay = GetRussianDayOfWeek(dayOfWeek);
                // Сортируем интервалы расписания по времени начала
                var daySchedule = group.OrderBy(row => TimeSpan.Parse(row.Field<string>("StartTime"))).ToList();
                // Начинаем с минимально допустимого времени (например, из настроек)
                TimeSpan currentPointer = Settings.Default.BorderStartTime;

                foreach (var scheduleRow in daySchedule)
                {
                    TimeSpan scheduleStart = TimeSpan.Parse(scheduleRow.Field<string>("StartTime"));
                    TimeSpan scheduleEnd = TimeSpan.Parse(scheduleRow.Field<string>("EndTime"));

                    // Если есть свободный интервал до текущего запланированного блока
                    if (scheduleStart > currentPointer)
                    {
                        currentPointer = ProcessFreeInterval(mergedTable, tasksList, russianDay, dayOfWeek, currentPointer, scheduleStart, Settings.Default.DelayTime);
                    }

                    // Добавляем запланированное задание из расписания
                    mergedTable.Rows.Add(russianDay, $"{scheduleRow.Field<string>("StartTime")} - {scheduleRow.Field<string>("EndTime")}", "Scheduled", DBNull.Value, scheduleRow.Field<string>("Plan"), dayOfWeek, DBNull.Value);
                    currentPointer = scheduleEnd;
                }

                // Обрабатываем свободное время от конца последнего запланированного блока до конца дня
                TimeSpan dayEnd = TimeSpan.Parse("23:59:59");
                if (currentPointer < dayEnd)
                {
                    currentPointer = ProcessFreeInterval(mergedTable, tasksList, russianDay, dayOfWeek, currentPointer, dayEnd, Settings.Default.DelayTime);
                }
            }

            // Добавляем оставшиеся незапланированные задачи в конец таблицы
            foreach (var taskRow in tasksList)
            {
                int priority = Convert.ToInt32(taskRow["Priority"]);
                mergedTable.Rows.Add("Нет времени", taskRow["CompleteTime"], "Mandatory", priority, taskRow.Field<string>("Title"), -1, taskRow["Id"]);
            }

            return mergedTable;
        }

        /// <summary>
        /// Обрабатывает свободный интервал [freeStart, freeEnd] для добавления задач.
        /// Возвращает конечное значение свободного интервала (то есть freeEnd).
        /// </summary>
        private TimeSpan ProcessFreeInterval(DataTable mergedTable, List<DataRow> tasksList, string russianDay, int dayOfWeek, TimeSpan freeStart, TimeSpan freeEnd, TimeSpan globalDelay)
        {
            TimeSpan currentFreeStart = freeStart;
            // Сортируем задачи по приоритету (чем меньше значение Priority, тем выше приоритет) и затем по длительности задержки
            var sortedTasks = tasksList.OrderBy(task => Convert.ToInt32(task["Priority"]))
                                       .ThenByDescending(task => task.IsNull("DelayTime") ? TimeSpan.Zero : TimeSpan.Parse(task["DelayTime"].ToString()))
                                       .ToList();
            foreach (var taskRow in sortedTasks.ToList()) // ToList() для безопасного удаления элементов
            {
                TimeSpan taskDuration = TimeSpan.Parse(taskRow["CompleteTime"].ToString());
                DateTime dueDate = DateTime.Parse(taskRow["DueDate"].ToString());
                TimeSpan taskDelay = taskRow.IsNull("DelayTime") ? TimeSpan.Zero : TimeSpan.Parse(taskRow["DelayTime"].ToString());

                // Рассчитываем предполагаемое время начала и окончания задачи
                TimeSpan tentativeStart = currentFreeStart + globalDelay + taskDelay;
                TimeSpan tentativeEnd = tentativeStart + taskDuration;

                // Проверяем, что задание помещается в свободный интервал и в пределах установленных границ
                if (tentativeEnd <= freeEnd &&
                    tentativeStart >= Settings.Default.BorderStartTime &&
                    tentativeEnd <= Settings.Default.BorderEndTime)
                {
                    // Проверка дедлайна: задание должно быть выполнено до DueDate
                    DateTime scheduledDateTime = DateTime.Today.Add(tentativeStart);
                    if (dueDate > scheduledDateTime)
                    {
                        // Если между текущим свободным временем и началом задачи есть промежуток – добавляем его как "Free"
                        if (tentativeStart > currentFreeStart)
                        {
                            mergedTable.Rows.Add(russianDay, $"{currentFreeStart} - {tentativeStart}", "Free", DBNull.Value, DBNull.Value, dayOfWeek, DBNull.Value);
                            FreeTime += tentativeStart - currentFreeStart;
                        }
                        // Добавляем задачу в расписание
                        int priority = Convert.ToInt32(taskRow["Priority"]);
                        mergedTable.Rows.Add(russianDay, $"{tentativeStart} - {tentativeEnd}", "Mandatory", priority, taskRow.Field<string>("Title"), dayOfWeek, taskRow["Id"]);
                        BusyTime += tentativeEnd - tentativeStart;
                        // Записываем дедлайн для отображения
                        TaskKey taskKey = new TaskKey(dueDate, Convert.ToInt32(taskRow["Id"]));
                        Deadline[taskKey] = taskRow["Title"].ToString();
                        // Удаляем задачу из списка
                        tasksList.Remove(taskRow);
                        // Обновляем указатель свободного времени
                        currentFreeStart = tentativeEnd;
                    }
                }
            }
            // Добавляем оставшийся интервал как свободное время, если он не пустой
            if (currentFreeStart < freeEnd)
            {
                FreeTime += freeEnd - currentFreeStart;
                mergedTable.Rows.Add(russianDay, $"{currentFreeStart} - {freeEnd}", "Free", DBNull.Value, DBNull.Value, dayOfWeek, DBNull.Value);
            }
            return freeEnd; // Интервал полностью обработан
        }

        /// <summary>
        /// Преобразует тип указанного столбца DataTable в заданный тип.
       	/// </summary>
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

        private void HandleMandatoryTask(Action<int> action)
        {
            if (selectedRow != null)
            {
                string taskType = selectedRow.Cells["Type"].Value.ToString();
                if (taskType == "Mandatory")
                {
                    int taskId = (int)selectedRow.Cells["TaskId"].Value;
                    action(taskId);
                    LoadAndUpdateData();
                }
                else
                {
                    MessageBox.Show("Можно откладывать/ограничивать только обязательные задания.");
                }
            }
        }

        private void PostponeMenuItem_Click(object sender, EventArgs e)
        {
            HandleMandatoryTask(taskId =>
            {
                PostponeForm postponeForm = new PostponeForm(taskId);
                postponeForm.ShowDialog();
            });
        }

        private void BorderTaskMenuItem_Click(object sender, EventArgs e)
        {
            HandleMandatoryTask(taskId =>
            {
                BorderTaskForm borderTaskForm = new BorderTaskForm(taskId);
                borderTaskForm.ShowDialog();
            });
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

        private void dataGridView1_MouseClick(object sender, EventArgs e)
        {
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
            DeleteLoggedTasks();
            timer1.Interval = 24 * 60 * 60 * 1000;
        }

        public struct TaskKey
        {
            public DateTime DueDate { get; }
            public int TaskId { get; }
            public TaskKey(DateTime dueDate, int taskId)
            {
                DueDate = dueDate;
                TaskId = taskId;
            }
        }
    }
}
