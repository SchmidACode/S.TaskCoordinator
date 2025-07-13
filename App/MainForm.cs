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
using Microsoft.VisualBasic;
using Telerik.WinControls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

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

        private TimeSpan preparationTime = TimeSpan.Zero;
        private bool isFirstRun = true; // Флаг для проверки первого запуска

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
            //DeleteExpiredTasks();

            // Настройка таймера: первый интервал до конца дня
            timer1.Interval = Convert.ToInt32(TimeSpan.FromHours(24).TotalMilliseconds - DateTime.Now.TimeOfDay.TotalMilliseconds);
            if (Settings.Default.LastOnlineTime != DateTime.MinValue)
            {
                if (Settings.Default.LastOnlineTime.AddMilliseconds(TimeSpan.FromHours(24).TotalMilliseconds - DateTime.Now.TimeOfDay.TotalMilliseconds) < DateTime.Now)
                {
                    //DeleteLoggedTasks();
                }
            }
            timer1.Interval = 1000;
            timer1.Tick += timer1_Tick;
            timer1.Start();
        }
        /// <summary>
        /// Переносит из mergedData строки текущего дня с концом интервала ≤ Now,
        /// возвращая массивы значений этих строк.
        /// </summary>
        private List<object[]> MovePastDataToEnd(DataTable mergedData)
        {
            var movedValues = new List<object[]>();
            var rowsToRemove = new List<DataRow>();
            foreach (DataRow dataRow in mergedData.Rows) {
                if ((string)dataRow["DayOfWeek"] == "Нет времени")
                    continue;

                int dayOfWeek = Convert.ToInt32(dataRow["DayOfWeekNumber"]);
                if (dayOfWeek != (int)DateTime.Now.DayOfWeek)
                    continue;

                var times = ((string)dataRow["TimeInterval"]).Split('-');
                var endTime = TimeSpan.Parse(times[1].Trim());
                if (endTime <= DateTime.Now.TimeOfDay && (string)dataRow["Type"]=="Free")
                    rowsToRemove.Add(dataRow);
                else if (endTime <= DateTime.Now.TimeOfDay)
                {
                    movedValues.Add((object[])dataRow.ItemArray.Clone());
                    rowsToRemove.Add(dataRow);
                }
            }

            // Удаляем их из таблицы
            foreach (var row in rowsToRemove)
                mergedData.Rows.Remove(row);

            return movedValues;
        }
        /// <summary>
        /// Собирает расписание + задачи, планируя их на свободные слоты.
        /// В out-параметре возвращает те задачи, которые не удалось запланировать.
        /// </summary>
        public DataTable MergeScheduleAndTasks(DataTable scheduleTable, DataTable tasksTable, out List<DataRow> remainingTasks)
        {
            // Приводим тип DayOfWeek к int, если нужно
            if (scheduleTable.Columns["DayOfWeek"].DataType != typeof(int))
                scheduleTable = ConvertColumnType(scheduleTable, "DayOfWeek", typeof(int));

            // Инициализация результирующей таблицы
            DataTable mergedTable = new DataTable();
            mergedTable.Columns.Add("DayOfWeek", typeof(string));
            mergedTable.Columns.Add("TimeInterval", typeof(string));
            mergedTable.Columns.Add("Type", typeof(string));
            mergedTable.Columns.Add("Priority", typeof(int));
            mergedTable.Columns.Add("Plan", typeof(string));
            mergedTable.Columns.Add("DayOfWeekNumber", typeof(int));
            mergedTable.Columns.Add("TaskId", typeof(int));

            int today = (int)DateTime.Now.DayOfWeek;
            var groupedSchedule = scheduleTable.AsEnumerable()
                .GroupBy(r => r.Field<int>("DayOfWeek"))
                .OrderBy(g => (g.Key - today + 7) % 7);

            // Копируем все задачи в список для удаления по мере планирования
            List<DataRow> tasksList = tasksTable.AsEnumerable().ToList();

            // Проходим по каждому дню в расписании
            foreach (var group in groupedSchedule)
            {
                int dow = group.Key;
                string russianDay = GetRussianDayOfWeek(dow);
                var daySchedule = group
                    .OrderBy(r => TimeSpan.Parse(r.Field<string>("StartTime")))
                    .ToList();

                TimeSpan currentPointer = Settings.Default.BorderStartTime;

                foreach (var schedRow in daySchedule) {
                    var start = TimeSpan.Parse(schedRow.Field<string>("StartTime"));
                    var end = TimeSpan.Parse(schedRow.Field<string>("EndTime"));

                    if (start > currentPointer)
                        currentPointer = ProcessFreeInterval(mergedTable, tasksList, russianDay, dow, currentPointer, start, Settings.Default.DelayTime);

                    mergedTable.Rows.Add(russianDay, $"{schedRow.Field<string>("StartTime")} - {schedRow.Field<string>("EndTime")}", "Scheduled", DBNull.Value, schedRow.Field<string>("Plan"), dow, DBNull.Value);
                    currentPointer = end;
                }

                // Оставшийся кусок дня
                TimeSpan dayEnd = TimeSpan.Parse("23:59:59");
                if (currentPointer < dayEnd) 
                    currentPointer = ProcessFreeInterval(mergedTable, tasksList, russianDay, dow, currentPointer, dayEnd, Settings.Default.DelayTime);
            }
            foreach (var unscheduled in tasksList)
            {
                int priority = Convert.ToInt32(unscheduled["Priority"]);
                string title = unscheduled.Field<string>("Title");
                int id = Convert.ToInt32(unscheduled["Id"]);
                // DayOfWeekNumber = -1, будет отображаться как «Нет времени»
                mergedTable.Rows.Add(
                    "Нет времени",      // DayOfWeek
                    "-",                // TimeInterval
                    "Mandatory",           // Type или любой ваш маркер
                    priority,
                    title,
                    -1,                 // DayOfWeekNumber
                    id                  // TaskId
                );
            }
            // Всё, что не удалось запланировать, останется в remainingTasks
            remainingTasks = tasksList;
            // Возвращаем результирующую таблицу
            return mergedTable;
        }
        /// <summary>
        /// Полная версия LoadAndUpdateData с автозаполнением перенесённых блоков.
        /// </summary>
        private void LoadAndUpdateData()
        {
            try
            {
                Deadline.Clear();
                FreeTime = BusyTime = TimeSpan.Zero;

                var scheduleData = connector.LoadSortedDataFromDB("SELECT * FROM Schedule");
                var tasksData = connector.LoadSortedDataFromDB("SELECT * FROM Tasks");

                // Объединяем и получаем список непроставленных задач
                DataTable mergedData = MergeScheduleAndTasks(scheduleData, tasksData, out var remainingTasks);
                // Переносим прошедшие сегодня интервалы
                var movedValuesList = MovePastDataToEnd(mergedData);
                dataGridView1.DataSource = mergedData;

                // Для каждого перенесённого блока:
                for (int i = 0; i < movedValuesList.Count - 1; i++)
                {
                    var currentRowValues = movedValuesList[i];
                    var nextRowValues = movedValuesList[i + 1];

                    // Воссоздаём строки из массива значений
                    var currentRow = mergedData.NewRow();
                    currentRow.ItemArray = currentRowValues;

                    var nextRow = mergedData.NewRow();
                    nextRow.ItemArray = nextRowValues;

                    // Сдвигаем день недели на +7 (перенос на следующую неделю)
                    int originalDayNumber = Convert.ToInt32(currentRow["DayOfWeekNumber"]);
                    int shiftedDayNumber = (originalDayNumber + 7) % 7;
                    currentRow["DayOfWeekNumber"] = shiftedDayNumber;

                    string shiftedRussianDay = GetRussianDayOfWeek(shiftedDayNumber);
                    currentRow["DayOfWeek"] = shiftedRussianDay;

                    // Получаем границы интервала времени
                    var currentTimeIntervalParts = ((string)currentRow["TimeInterval"]).Split('-');
                    var nextTimeIntervalParts = ((string)nextRow["TimeInterval"]).Split('-');

                    TimeSpan currentFreeEnd = TimeSpan.Parse(currentTimeIntervalParts[1].Trim());
                    TimeSpan nextFreeStart = TimeSpan.Parse(nextTimeIntervalParts[0].Trim());

                    // Обновляем план с пометкой о переносе
                    currentRow["Plan"] = "[Перенос] " + currentRow["Plan"];

                    // Добавляем строку в таблицу
                    mergedData.Rows.Add(currentRow);

                    // Запускаем автоматическое распределение задач на этот отложенный слот
                    ProcessFreeInterval(mergedData,remainingTasks,shiftedRussianDay,shiftedDayNumber, currentFreeEnd,nextFreeStart,Settings.Default.DelayTime);
                }

                // Обновляем представление
                dataGridView1.Refresh();

                // Первый запуск — спрашиваем подготовку
                if (isFirstRun)
                {
                    isFirstRun = false;
                    AskForPreparationTime();
                }

                // Остальное — как было
                UpdateTaskInformation();
                string f_FreeTime = string.Format("{0:D2}d {1:D2}:{2:D2}:{3:D2}", FreeTime.Days, FreeTime.Hours, FreeTime.Minutes, FreeTime.Seconds);
                labelFreeTime.Text = $"Свободное время:\n{f_FreeTime}";
                labelBusyTime.Text = $"Время отведенное\nна занятия:\n{BusyTime}";

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
        /// Обрабатывает свободный интервал [freeStart, freeEnd] для добавления задач.
        /// Возвращает конечное значение свободного интервала (то есть freeEnd).
        /// </summary>
        private TimeSpan ProcessFreeInterval(DataTable mergedTable,List<DataRow> tasksList,string russianDay, int dayOfWeek,TimeSpan freeStart,TimeSpan freeEnd,TimeSpan delayToApply)
        {
            TimeSpan currentFreeStart = freeStart;

            // Определяем сегодня и смещение к нужному дню недели
            int todayDow = (int)DateTime.Today.DayOfWeek;
            int daysUntil = (dayOfWeek - todayDow + 7) % 7;
            // Если dayOfWeek == todayDow, daysUntil == 0, иначе — сколько дней вперед

            var sortedTasks = tasksList
                .OrderBy(task => Convert.ToInt32(task["Priority"]))
                .ThenByDescending(task =>
                    task.IsNull("DelayTime")
                        ? TimeSpan.Zero
                        : TimeSpan.Parse(task["DelayTime"].ToString()))
                .ToList();

            bool isAfterScheduled = true;
            bool setted = false;

            foreach (var taskRow in sortedTasks.ToList())
            {
                // Пропускаем уже прошедшие сегодня интервалы
                if ((int)DateTime.Now.DayOfWeek == dayOfWeek && freeEnd < DateTime.Now.TimeOfDay)
                    continue;

                // Вставка подготовительного слота на текущем дне
                if ((int)DateTime.Now.DayOfWeek == dayOfWeek
                    && freeStart < DateTime.Now.TimeOfDay
                    && !setted)
                {
                    var nowTs = DateTime.Now.TimeOfDay;
                    nowTs = nowTs.Subtract(TimeSpan.FromMilliseconds(nowTs.Milliseconds));
                    currentFreeStart = nowTs + preparationTime;
                    mergedTable.Rows.Add(
                        russianDay,
                        $"{FormatTime(currentFreeStart - preparationTime)} - {FormatTime(currentFreeStart)}",
                        "Free",
                        DBNull.Value, DBNull.Value,
                        dayOfWeek, DBNull.Value);
                    setted = true;
                    preparationTime = TimeSpan.Zero;
                }

                TimeSpan taskDuration = TimeSpan.Parse(taskRow["CompleteTime"].ToString());
                DateTime dueDate = DateTime.Parse(taskRow["DueDate"].ToString());
                TimeSpan taskDelay = taskRow.IsNull("DelayTime")
                    ? TimeSpan.Zero
                    : TimeSpan.Parse(taskRow["DelayTime"].ToString());

                TimeSpan tentativeStart = currentFreeStart + delayToApply + taskDelay;
                if (isAfterScheduled)
                    tentativeStart -= delayToApply;

                TimeSpan tentativeEnd = tentativeStart + taskDuration;

                // Проверка ограничений StartTime/EndTime из задачи
                if (taskRow.Table.Columns.Contains("StartTime") &&
                    taskRow.Table.Columns.Contains("EndTime"))
                {
                    TimeSpan taskAllowedStart = TimeSpan.Parse(taskRow["StartTime"].ToString());
                    TimeSpan taskAllowedEnd = TimeSpan.Parse(taskRow["EndTime"].ToString());

                    TimeSpan allowedStart =
                        Settings.Default.BorderStartTime > taskAllowedStart
                            ? Settings.Default.BorderStartTime
                            : taskAllowedStart;
                    TimeSpan allowedEnd =
                        Settings.Default.BorderEndTime < taskAllowedEnd
                            ? Settings.Default.BorderEndTime
                            : taskAllowedEnd;

                    if (tentativeStart < allowedStart)
                    {
                        tentativeStart = allowedStart;
                        tentativeEnd = tentativeStart + taskDuration;
                    }
                    if (tentativeEnd > allowedEnd)
                    {
                        if (allowedEnd - taskDuration >= allowedStart)
                        {
                            tentativeStart = allowedEnd - taskDuration;
                            tentativeEnd = allowedEnd;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                // Проверка укладывается ли задача в свободный слот
                if (tentativeEnd <= freeEnd
                    && tentativeStart >= Settings.Default.BorderStartTime
                    && tentativeEnd <= Settings.Default.BorderEndTime)
                {
                    // --- Здесь изменили логику: учитываем день недели ---
                    DateTime scheduledDateTime = DateTime.Today
                        .AddDays(daysUntil)       // смещение к дню недели
                        .Add(tentativeStart);     // добавляем время

                    if (dueDate > scheduledDateTime)
                    {
                        // Вставляем промежуток до задачи
                        if (tentativeStart > currentFreeStart)
                        {
                            mergedTable.Rows.Add(
                                russianDay,
                                $"{FormatTime(currentFreeStart)} - {FormatTime(tentativeStart)}",
                                "Free",
                                DBNull.Value, DBNull.Value,
                                dayOfWeek, DBNull.Value);
                            FreeTime += tentativeStart - currentFreeStart;
                        }

                        // Вставляем саму задачу
                        int priority = Convert.ToInt32(taskRow["Priority"]);
                        mergedTable.Rows.Add(
                            russianDay,
                            $"{FormatTime(tentativeStart)} - {FormatTime(tentativeEnd)}",
                            "Mandatory",
                            priority,
                            taskRow.Field<string>("Title"),
                            dayOfWeek,
                            taskRow["Id"]);
                        BusyTime += tentativeEnd - tentativeStart;

                        // Обновляем дедлайн с учётом реальной даты/времени
                        TaskKey taskKey = new TaskKey(dueDate, Convert.ToInt32(taskRow["Id"]));
                        Deadline[taskKey] = taskRow["Title"].ToString();

                        tasksList.Remove(taskRow);
                        currentFreeStart = tentativeEnd;
                        isAfterScheduled = false;
                    }
                }
            }

            // Оставшийся кусок дня
            if (currentFreeStart < freeEnd)
            {
                FreeTime += freeEnd - currentFreeStart;
                mergedTable.Rows.Add(
                    russianDay,
                    $"{FormatTime(currentFreeStart)} - {FormatTime(freeEnd)}",
                    "Free",
                    DBNull.Value, DBNull.Value,
                    dayOfWeek, DBNull.Value);
            }

            return freeEnd;
        }
        private string FormatTime(TimeSpan time) {
            return time.ToString(@"hh\:mm\:ss");
        }
        private void UpdateTaskInformation()
        {
            // Список для хранения всех будущих дедлайнов
            var futureDeadlines = new List<string>();
            foreach (var entry in Deadline)
                if (entry.Key.DueDate > DateTime.Now)
                    futureDeadlines.Add($"{entry.Value} - {entry.Key.DueDate.ToString("yyyy-MM-dd HH:mm")}");
            if (futureDeadlines.Any())
                label1.Text = "Предстоящие задания:\n" + string.Join("\n", futureDeadlines);
            else
                label1.Text = "Нет предстоящих заданий";
            
        }
        private void AskForPreparationTime()
        {
            string userInput = Interaction.InputBox("Введите время на подготовку (в формате hh:mm)", "Время на подготовку", "00:10:00");
            if (TimeSpan.TryParse(userInput, out TimeSpan result))
                preparationTime = result;
            else
            {
                MessageBox.Show("Неверный формат времени. Попробуйте еще раз.");
                AskForPreparationTime(); // Запрашиваем снова, если ввод неправильный
            }
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
            // Текущее время без миллисекунд
            var now = DateTime.Now;
            var nowTs = now.TimeOfDay - TimeSpan.FromMilliseconds(now.TimeOfDay.Milliseconds);
            int todayDow = (int)now.DayOfWeek;

            // 1) Удаляем все просроченные "Free"-строки для сегодняшнего дня
            for (int i = dataGridView1.Rows.Count - 1; i >= 0; i--)
            {
                var row = dataGridView1.Rows[i];
                if (row.Cells["Type"].Value?.ToString() == "Free" &&
                    Convert.ToInt32(row.Cells["DayOfWeekNumber"].Value) == todayDow)
                {
                    var endText = row.Cells["TimeInterval"].Value.ToString().Split('-')[1].Trim();
                    var endTs = TimeSpan.Parse(endText);
                    if (nowTs >= endTs)
                    {
                        dataGridView1.Rows.RemoveAt(i);
                    }
                }
            }

            // 2) Проверяем только самую первую строку
            if (dataGridView1.Rows.Count == 0)
                return;

            var firstRow = dataGridView1.Rows[0];
            // Обновляем только если это свободное время сегодня
            if (firstRow.Cells["Type"].Value?.ToString() == "Free" &&
                Convert.ToInt32(firstRow.Cells["DayOfWeekNumber"].Value) == todayDow)
            {
                var parts = firstRow.Cells["TimeInterval"].Value.ToString().Split('-');
                var endPart = parts[1].Trim();

                // Меняем только начало
                firstRow.Cells["TimeInterval"].Value = $"{nowTs:hh\\:mm\\:ss} - {endPart}";
            }
            // Иначе — это задача, не трогаем ничего
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
