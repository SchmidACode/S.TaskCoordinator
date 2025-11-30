using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TaskCoordinator;
using TaskCoordinator.Properties;

namespace ScheduleTaskCoordinator
{
    public partial class MainForm : Form
    {
        private Connector connector;
        private TimeSpan FreeTime, BusyTime;
        private Dictionary<TaskKey, string> Deadline = new Dictionary<TaskKey, string>();

        private ContextMenuStrip contextMenuStrip;
        private DataGridViewRow selectedRow;

        private TimeSpan preparationTime = TimeSpan.Zero;
        private bool isFirstRun = true;

        public MainForm()
        {
            InitializeComponent();

            typeof(DataGridView).InvokeMember("DoubleBuffered",
    System.Reflection.BindingFlags.NonPublic |
    System.Reflection.BindingFlags.Instance |
    System.Reflection.BindingFlags.SetProperty,
    null, dataGridView1, new object[] { true });


            connector = new Connector();

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.ReadOnly = true;

            dataGridView1.DefaultCellStyle.Font = new Font(
            dataGridView1.Font.FontFamily,
            dataGridView1.Font.Size * 1.3f,
            dataGridView1.Font.Style);
            dataGridView1.RowTemplate.Height = (int)(dataGridView1.RowTemplate.Height * 1.3f);

            Controls.Add(dataGridView1);

            dataGridView1.ColumnHeaderMouseClick += DataGridView1_ColumnHeaderMouseClick;
            dataGridView1.DataBindingComplete += DataGridView1_DataBindingComplete;
            dataGridView1.MouseDown += DataGridView1_MouseDown;

            contextMenuStrip = new ContextMenuStrip();
            var postponeMenuItem = new ToolStripMenuItem("Отложить задание", null, PostponeMenuItem_Click);
            var borderTaskMenuItem = new ToolStripMenuItem("Ограничить задание", null, BorderTaskMenuItem_Click);
            contextMenuStrip.Items.Add(postponeMenuItem);
            contextMenuStrip.Items.Add(borderTaskMenuItem);
            dataGridView1.ContextMenuStrip = contextMenuStrip;

            LoadAndUpdateData();

            if (Settings.Default.LastOnlineTime != DateTime.MinValue)
            {
                // Проверка удаления логов
                // if (Settings.Default.LastOnlineTime.AddMilliseconds(millisToMidnight) < DateTime.Now) { /* DeleteLoggedTasks(); */ }
            }
            var now = DateTime.Now;
            int msToNextMinute = (int)(
                (60 - now.Second) * 1000
                - now.Millisecond
            );
            if (msToNextMinute < 0) msToNextMinute = 0;

            timer1.Tick += FirstTickThenMinuteTicks;
            timer1.Interval = msToNextMinute;
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
            foreach (DataRow dataRow in mergedData.Rows)
            {
                if (dataRow.IsNull("DayOfWeek") || (string)dataRow["DayOfWeek"] == "Нет времени")
                    continue;

                if (dataRow.IsNull("DayOfWeekNumber"))
                    continue;

                int dayOfWeek = Convert.ToInt32(dataRow["DayOfWeekNumber"]);
                if (dayOfWeek != (int)DateTime.Now.DayOfWeek)
                    continue;

                var times = ((string)dataRow["TimeInterval"]).Split('-');
                var endTime = TimeSpan.Parse(times[1].Trim());
                if (endTime <= DateTime.Now.TimeOfDay && (string)dataRow["Type"] == "Free")
                    rowsToRemove.Add(dataRow);
                else if (endTime <= DateTime.Now.TimeOfDay)
                {
                    movedValues.Add((object[])dataRow.ItemArray.Clone());
                    rowsToRemove.Add(dataRow);
                }
            }

            foreach (var row in rowsToRemove)
                mergedData.Rows.Remove(row);

            return movedValues;
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

                // Первый запуск — спрашиваем подготовку
                if (isFirstRun)
                {
                    isFirstRun = false;
                    AskForPreparationTime();
                }

                var scheduleData = connector.LoadSortedDataFromDB("SELECT * FROM Schedule");
                var tasksData = connector.LoadSortedDataFromDB("SELECT * FROM Tasks");

                // Объединяем и получаем список непроставленных задач
                DataTable mergedData = MergeScheduleAndTasks(scheduleData, tasksData, out var remainingTasks);
                // Переносим прошедшие сегодня интервалы
                var movedValuesList = MovePastDataToEnd(mergedData);
                dataGridView1.DataSource = mergedData;

                // Для каждого перенесённого блока: (обрабатываем индивидуально)
                for (int i = 0; i < movedValuesList.Count; i++)
                {
                    var currentRowValues = movedValuesList[i];

                    // Воссоздаём строку из массива значений
                    var currentRow = mergedData.NewRow();
                    currentRow.ItemArray = currentRowValues;

                    // Сдвигаем день недели на следующую неделю (weekOffset = 1)
                    int originalDayNumber = Convert.ToInt32(currentRow["DayOfWeekNumber"]);
                    int shiftedDayNumber = originalDayNumber; // оставляем номер дня тот же, но учтём weekOffset при планировании

                    string shiftedRussianDay = GetRussianDayOfWeek(shiftedDayNumber);
                    currentRow["DayOfWeekNumber"] = shiftedDayNumber;
                    currentRow["DayOfWeek"] = shiftedRussianDay;

                    // Обновляем план с пометкой о переносе
                    if (currentRow["Plan"] != DBNull.Value && currentRow["Plan"] is string)
                        currentRow["Plan"] = "[Перенос] " + currentRow["Plan"];

                    // Получаем границы интервала времени
                    var currentTimeIntervalParts = ((string)currentRow["TimeInterval"]).Split('-');
                    TimeSpan currentFreeEnd = TimeSpan.Parse(currentTimeIntervalParts[1].Trim());

                    // Добавляем строку в таблицу
                    mergedData.Rows.Add(currentRow);

                    // Ищем следующий блок в том же дне, чтобы определить границу поиска свободного времени
                    TimeSpan nextFreeStart = TimeSpan.Parse("23:59:59");
                    var sameDayRows = mergedData.AsEnumerable()
                        .Where(r => !r.IsNull("DayOfWeekNumber") && Convert.ToInt32(r["DayOfWeekNumber"]) == shiftedDayNumber)
                        .Select(r => r.Field<string>("TimeInterval")).ToList();
                    
                    TimeSpan? foundNextStart = null;
                    foreach (var ti in sameDayRows)
                    {
                        var parts = ti.Split('-');
                        if (parts.Length < 2) continue;
                        var start = TimeSpan.Parse(parts[0].Trim());
                        if (start > currentFreeEnd)
                        {
                            if (foundNextStart == null || start < foundNextStart)
                                foundNextStart = start;
                        }
                    }
                    if (foundNextStart.HasValue)
                        nextFreeStart = foundNextStart.Value;

                    // Запускаем автоматическое распределение задач на этот отложенный слот (на следующую неделю)
                    ProcessFreeInterval(mergedData, remainingTasks, shiftedRussianDay, shiftedDayNumber, currentFreeEnd, nextFreeStart, Settings.Default.DelayTime, weekOffset: 1);
                }

                // Обновляем представление
                dataGridView1.Refresh();

                // Остальное — как было
                UpdateTaskInformation();
                string f_FreeTime = string.Format("{0:D2}d {1:D2}:{2:D2}:{3:D2}", FreeTime.Days, FreeTime.Hours, FreeTime.Minutes, FreeTime.Seconds);
                labelFreeTime.Text = $"Свободное время:\n{f_FreeTime}";
                labelBusyTime.Text = $"Время отведенное\nна занятия:\n{BusyTime}";

                if (dataGridView1.Columns.Contains("Type")) dataGridView1.Columns["Type"].Visible = false;
                if (dataGridView1.Columns.Contains("DayOfWeekNumber")) dataGridView1.Columns["DayOfWeekNumber"].Visible = false;
                if (dataGridView1.Columns.Contains("TaskId")) dataGridView1.Columns["TaskId"].Visible = false;
                dataGridView1.ClearSelection();
                if (dataGridView1.Columns.Contains("DayOfWeek")) dataGridView1.Columns["DayOfWeek"].HeaderText = "День недели";
                if (dataGridView1.Columns.Contains("TimeInterval")) dataGridView1.Columns["TimeInterval"].HeaderText = "Время";
                if (dataGridView1.Columns.Contains("Priority")) dataGridView1.Columns["Priority"].HeaderText = "Приоритет";
                if (dataGridView1.Columns.Contains("Plan")) dataGridView1.Columns["Plan"].HeaderText = "Название";

                dataGridView1.Columns["Priority"].Width = dataGridView1.Columns["DayOfWeek"].Width / 4;

                LogTaskIds(mergedData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Собирает расписание + задачи, планируя их на свободные слоты.
        /// Учитывает ограниченные задачи: если они есть, сокращает freeEnd до их StartTime,
        /// затем продолжает обрабатывать оставшуюся часть интервала.
        /// </summary>
        public DataTable MergeScheduleAndTasks(DataTable scheduleTable, DataTable tasksTable, out List<DataRow> remainingTasks)
        {
            // Приводим тип DayOfWeek к int
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

                foreach (var schedRow in daySchedule)
                {
                    var start = TimeSpan.Parse(schedRow.Field<string>("StartTime"));
                    var end = TimeSpan.Parse(schedRow.Field<string>("EndTime"));

                    // Обрабатываем интервал [currentPointer, start] с учётом ограниченных задач
                    // ЦИКЛ: пока не дошли до start
                    while (currentPointer < start)
                    {
                        TimeSpan adjustedEnd = start;

                        // Ищем первую ограниченную задачу между currentPointer и start
                        foreach (var task in tasksList)
                        {
                            if (task.Table.Columns.Contains("StartTime") &&
                                task.Table.Columns.Contains("EndTime") &&
                                !task.IsNull("StartTime") &&
                                !task.IsNull("EndTime"))
                            {
                                var taskStart = TimeSpan.Parse(task["StartTime"].ToString());
                                var taskEnd = TimeSpan.Parse(task["EndTime"].ToString());

                                // Если это не полнодневная задача и её окно начинается в нашем диапазоне
                                if (!(taskStart == TimeSpan.Zero && taskEnd == TimeSpan.Parse("23:59:59")))
                                {
                                    if (taskStart > currentPointer && taskStart < adjustedEnd)
                                        adjustedEnd = taskStart;
                                }
                            }
                        }

                        // Обрабатываем слот [currentPointer, adjustedEnd]
                        ProcessFreeInterval(mergedTable, tasksList, russianDay, dow, currentPointer, adjustedEnd, Settings.Default.DelayTime);
                        currentPointer = adjustedEnd;
                    }

                    mergedTable.Rows.Add(russianDay, $"{schedRow.Field<string>("StartTime")} - {schedRow.Field<string>("EndTime")}", "Scheduled", DBNull.Value, schedRow.Field<string>("Plan"), dow, DBNull.Value);
                    currentPointer = end;
                }

                // Оставшийся кусок дня — аналогично в цикле
                TimeSpan dayEnd = TimeSpan.Parse("23:59:59");
                while (currentPointer < dayEnd)
                {
                    TimeSpan adjustedDayEnd = dayEnd;

                    // Ищем первую ограниченную задачу в остатке дня
                    foreach (var task in tasksList)
                    {
                        if (task.Table.Columns.Contains("StartTime") &&
                            task.Table.Columns.Contains("EndTime") &&
                            !task.IsNull("StartTime") &&
                            !task.IsNull("EndTime"))
                        {
                            var taskStart = TimeSpan.Parse(task["StartTime"].ToString());
                            var taskEnd = TimeSpan.Parse(task["EndTime"].ToString());

                            // Если это не полнодневная задача и её окно начинается после currentPointer
                            if (!(taskStart == TimeSpan.Zero && taskEnd == TimeSpan.Parse("23:59:59")))
                            {
                                if (taskStart > currentPointer && taskStart < adjustedDayEnd)
                                    adjustedDayEnd = taskStart;
                            }
                        }
                    }

                    ProcessFreeInterval(mergedTable, tasksList, russianDay, dow, currentPointer, adjustedDayEnd, Settings.Default.DelayTime);
                    currentPointer = adjustedDayEnd;
                }
            }

            foreach (var unscheduled in tasksList)
            {
                int priority = Convert.ToInt32(unscheduled["Priority"]);
                string title = unscheduled.Field<string>("Title");
                int id = Convert.ToInt32(unscheduled["Id"]);
                mergedTable.Rows.Add(
                    "Нет времени",
                    "-",
                    "Mandatory",
                    priority,
                    title,
                    -1,
                    id
                );
            }

            remainingTasks = tasksList;
            return mergedTable;
        }

        /// <summary>
        /// Обрабатывает свободный интервал [freeStart, freeEnd] для добавления задач.
        /// Возвращает конечное значение свободного интервала (то есть freeEnd).
        /// </summary>
        private TimeSpan ProcessFreeInterval(DataTable mergedTable, List<DataRow> tasksList, string russianDay, int dayOfWeek, TimeSpan freeStart, TimeSpan freeEnd, TimeSpan delayToApply, int weekOffset = 0)
        {
            TimeSpan currentFreeStart = freeStart;

            // Определяем сегодня и смещение к нужному дню недели
            int todayDow = (int)DateTime.Today.DayOfWeek;
            int daysUntil = (dayOfWeek - todayDow + 7) % 7 + weekOffset * 7;

            var sortedTasks = tasksList
            .OrderBy(task =>
            {
                if (task.IsNull("DueDate") || !DateTime.TryParse(task["DueDate"].ToString(), out DateTime dueDate))
                    return DateTime.MaxValue;
                return dueDate;
            })
            .ThenBy(task =>
            {
                if (task.IsNull("Priority") || !int.TryParse(task["Priority"].ToString(), out int priority))
                    return int.MaxValue;
                return priority;
            })
            .ThenBy(task =>
            {
                if (task.IsNull("DelayTime") || !TimeSpan.TryParse(task["DelayTime"].ToString(), out TimeSpan delayTime))
                    return TimeSpan.Zero;
                return delayTime;
            })
            .ThenBy(task =>
            {
                if (task.IsNull("CompleteTime") || !TimeSpan.TryParse(task["CompleteTime"].ToString(), out TimeSpan completeTime))
                    return TimeSpan.Zero;
                return completeTime;
            })
            .ToList();

            bool isAfterScheduled = true;
            bool setted = false;

            foreach (var taskRow in sortedTasks.ToList())
            {
                // Пропускаем уже прошедшие сегодня интервалы
                if ((int)DateTime.Now.DayOfWeek == dayOfWeek && freeEnd < DateTime.Now.TimeOfDay && weekOffset == 0)
                    continue;

                // Вставка подготовительного слота на текущем дне
                if ((int)DateTime.Now.DayOfWeek == dayOfWeek
                && freeStart < DateTime.Now.TimeOfDay
                && !setted && weekOffset == 0)
                {
                    var nowTs = DateTime.Now.TimeOfDay;
                    nowTs = nowTs.Subtract(TimeSpan.FromMilliseconds(nowTs.Milliseconds)); // убираем миллисекунды
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
                    // Убедитесь, что задача не пересекается с предыдущей
                    if (tentativeStart >= currentFreeStart)
                    {
                        DateTime scheduledDateTime = DateTime.Today
                        .AddDays(daysUntil) 
                        .Add(tentativeStart);

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

        private string FormatTime(TimeSpan time)
        {
            return time.ToString(@"hh\:mm");
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
            string userInput = Interaction.InputBox("Введите время на подготовку (в формате hh:mm)", "Время на подготовку", "00:10");
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
            try
            {
                string filePath = "task_ids.txt";
                DayOfWeek currentDay = DateTime.Now.DayOfWeek;
                List<string> taskIds = new List<string>();
                foreach (DataRow row in mergedData.Rows)
                {
                    if (!row.Table.Columns.Contains("DayOfWeekNumber")) continue;
                    if (row.IsNull("DayOfWeekNumber")) continue;
                    int dow = Convert.ToInt32(row["DayOfWeekNumber"]);
                    if (dow != (int)currentDay) continue;
                    if (row.IsNull("TaskId")) continue;
                    taskIds.Add(row["TaskId"].ToString());
                }
                File.WriteAllLines(filePath, taskIds);
            }
            catch (Exception ex)
            {
                Console.WriteLine("LogTaskIds error: " + ex.Message);
            }
        }

        /// <summary>
        /// Преобразует тип указанного столбца DataTable в заданный тип.
        /// Более безопасная реализация: создаём новую таблицу и конвертируем значения по одной строке.
        /// </summary>
        private DataTable ConvertColumnType(DataTable dt, string columnName, Type newType)
        {
            DataTable newTable = dt.Clone();
            newTable.Columns[columnName].DataType = newType;

            foreach (DataRow row in dt.Rows)
            {
                var newRow = newTable.NewRow();
                foreach (DataColumn col in dt.Columns)
                {
                    if (col.ColumnName == columnName)
                    {
                        if (row.IsNull(col))
                            newRow[col.ColumnName] = DBNull.Value;
                        else
                            newRow[col.ColumnName] = Convert.ChangeType(row[col], newType, CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        newRow[col.ColumnName] = row[col.ColumnName];
                    }
                }
                newTable.Rows.Add(newRow);
            }
            return newTable;
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
                        if (int.TryParse(taskId, out int id))
                        {
                            string deleteQuery = $"DELETE FROM Tasks WHERE Id = {id}";
                            connector.ExecuteQuery(deleteQuery);
                        }
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
                var typeCell = selectedRow.Cells["Type"].Value;
                if (typeCell != null && typeCell.ToString() == "Mandatory")
                {
                    if (selectedRow.Cells["TaskId"].Value != null && int.TryParse(selectedRow.Cells["TaskId"].Value.ToString(), out int taskId))
                    {
                        action(taskId);
                        LoadAndUpdateData();
                    }
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
            if (clickedColumn.Name == "DayOfWeek" && dataGridView1.Columns.Contains("DayOfWeekNumber"))
                dataGridView1.Sort(dataGridView1.Columns["DayOfWeekNumber"], ListSortDirection.Ascending);
        }

        private void DataGridView1_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (dataGridView1.Columns.Contains("Type"))
                {
                    var cellVal = row.Cells["Type"].Value;
                    if (cellVal == null || cellVal == DBNull.Value) continue;

                    string type = cellVal.ToString();

                    if (type == "Free")
                        row.DefaultCellStyle.BackColor = Color.FromArgb(88, 165, 209);
                    else if (type == "Scheduled")
                        row.DefaultCellStyle.BackColor = Color.FromArgb(144, 111, 217);
                    else if (type == "Mandatory")
                        row.DefaultCellStyle.BackColor = Color.FromArgb(214, 126, 195);
                    row.DefaultCellStyle.ForeColor = Color.White;
                }
            }

            int minHeight = 26;
            int maxHeight = 100;          
            double pixelsPer30Min = 6;   

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                if (row.Cells["TimeInterval"].Value == null) continue;

                var tiText = row.Cells["TimeInterval"].Value.ToString();
                var parts = tiText.Split('-');
                if (parts.Length != 2) continue;

                if (!TimeSpan.TryParse(parts[0].Trim(), out var startTs)) continue;
                if (!TimeSpan.TryParse(parts[1].Trim(), out var endTs)) continue;

                var duration = endTs - startTs;
                if (duration <= TimeSpan.Zero) continue;

                double blocks = duration.TotalMinutes / 30.0;
                int height = minHeight + (int)Math.Round(blocks * pixelsPer30Min);

                if (height < minHeight) height = minHeight;
                if (height > maxHeight) height = maxHeight;

                row.Height = height;
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

        private void FirstTickThenMinuteTicks(object sender, EventArgs e)
        {
            timer1.Tick -= FirstTickThenMinuteTicks;
            timer1.Tick += timer1_Tick;
            timer1.Interval = 60000; 
            timer1_Tick(sender, e);       
        }

        /// <summary>
        /// Обновлённый timer1_Tick:
        /// 1) Двигает левую границу свободного времени текущим временем
        /// 2) Уменьшает DelayTime на 1 минуту для задач, у которых оно есть
        /// </summary>
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                var now = DateTime.Now;
                var nowTs = now.TimeOfDay - TimeSpan.FromMilliseconds(now.TimeOfDay.Milliseconds);
                int todayDow = (int)now.DayOfWeek;

                if (!(dataGridView1.DataSource is DataTable dt)) return;

                // 1) Удаляем все просроченные "Free"-строки для сегодняшнего дня
                for (int i = dt.Rows.Count - 1; i >= 0; i--)
                {
                    var row = dt.Rows[i];
                    if (row.Table.Columns.Contains("Type") && row.Table.Columns.Contains("DayOfWeekNumber"))
                    {
                        var typeVal = row["Type"];
                        var dowVal = row["DayOfWeekNumber"];
                        if (typeVal != DBNull.Value && dowVal != DBNull.Value && typeVal.ToString() == "Free" && Convert.ToInt32(dowVal) == todayDow)
                        {
                            var endText = row["TimeInterval"].ToString().Split('-')[1].Trim();
                            var endTs = TimeSpan.Parse(endText);
                            if (nowTs >= endTs)
                            {
                                dt.Rows.RemoveAt(i);
                            }
                        }
                        else if (typeVal != DBNull.Value && dowVal != DBNull.Value && typeVal.ToString() == "Mandatory" && Convert.ToInt32(dowVal) == todayDow) {
                            var endText = row["TimeInterval"].ToString().Split('-')[1].Trim();
                            var endTs = TimeSpan.Parse(endText);
                            if (nowTs >= endTs)
                            {
                                dt.Rows.RemoveAt(i);
                            }
                        }
                    }
                }

                // 2) Для ПЕРВОГО "Free" слота сегодня двигаем ЛЕВую границу, правая неподвижна
                if (dt.Rows.Count == 0) return;

                var firstRow = dt.Rows[0];
                if (firstRow["Type"] != DBNull.Value && firstRow["Type"].ToString() == "Free" &&
                    firstRow["DayOfWeekNumber"] != DBNull.Value && Convert.ToInt32(firstRow["DayOfWeekNumber"]) == todayDow)
                {
                    var parts = firstRow["TimeInterval"].ToString().Split('-');
                    var endPart = parts[1].Trim();
                    var endTs = TimeSpan.Parse(endPart);

                    // Округляем текущее время
                    var nowRounded = nowTs;
                    if (nowRounded.Seconds >= 30)
                        nowRounded = nowRounded.Add(TimeSpan.FromMinutes(1));
                    nowRounded = new TimeSpan(nowRounded.Hours, nowRounded.Minutes, 0);

                    // Двигаем левую границу к текущему времени, правая остаётся
                    firstRow["TimeInterval"] = $"{FormatTime(nowRounded)} - {FormatTime(endTs)}";
                }

                // 3) Уменьшаем DelayTime на 1 минуту для задач, у которых оно есть
                foreach (DataRow row in dt.Rows)
                {
                    // Только для текущего дня
                    if (row.IsNull("DayOfWeekNumber") || Convert.ToInt32(row["DayOfWeekNumber"]) != todayDow)
                        continue;

                    // Только для задач (Type = "Mandatory")
                    if (row.IsNull("Type") || row["Type"].ToString() != "Mandatory")
                        continue;

                    // Проверяем наличие DelayTime в базе
                    if (row.Table.Columns.Contains("DelayTime") && !row.IsNull("DelayTime"))
                    {
                        if (TimeSpan.TryParse(row["DelayTime"].ToString(), out TimeSpan delayTime))
                        {
                            // Если DelayTime > 0, уменьшаем на 1 минуту
                            if (delayTime > TimeSpan.Zero)
                            {
                                TimeSpan newDelayTime = delayTime.Subtract(TimeSpan.FromMinutes(1));
                                if (newDelayTime < TimeSpan.Zero)
                                    newDelayTime = TimeSpan.Zero;

                                // Обновляем в БД
                                if (row.Table.Columns.Contains("TaskId") && !row.IsNull("TaskId"))
                                {
                                    int taskId = Convert.ToInt32(row["TaskId"]);
                                    string updateQuery = $"UPDATE Tasks SET DelayTime = '{newDelayTime.ToString(@"hh\:mm\:ss")}' WHERE Id = {taskId}";
                                    try
                                    {
                                        connector.ExecuteQuery(updateQuery);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error updating DelayTime: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("timer1_Tick error: " + ex.Message);
            }
        }

        public struct TaskKey : IEquatable<TaskKey>
        {
            public DateTime DueDate { get; }
            public int TaskId { get; }

            public TaskKey(DateTime dueDate, int taskId)
            {
                DueDate = dueDate;
                TaskId = taskId;
            }

            public bool Equals(TaskKey other)
            {
                return DueDate == other.DueDate && TaskId == other.TaskId;
            }
        }
    }
}