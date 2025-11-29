using ScheduleTaskCoordinator;
using System;
using System.Windows.Forms;

namespace WindowsFormsApp
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Открываем окно приветствия
            using (WelcomeForm welcomeForm = new WelcomeForm())
            {
                if (welcomeForm.ShowDialog() == DialogResult.OK)
                {
                    // Проверяем условие
                    if (!IsConditionMet())
                    {
                        // Если условие не выполнено, открываем окно настроек
                        using (ScheduleForm settingsForm = new ScheduleForm())
                        {
                            MessageBox.Show("Настройка плана должна быть как минимум на 1 день", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            if (settingsForm.ShowDialog() == DialogResult.OK)
                            {
                                // После закрытия окна настроек, открываем главное окно
                                Application.Run(new MainForm());
                            }
                        }
                    }
                    else
                    {
                        // Если условие выполнено, открываем главное окно
                        Application.Run(new MainForm());
                    }
                }
            }
        }

        private static bool IsConditionMet()
        {
            ScheduleTaskCoordinator.Connector con = new ScheduleTaskCoordinator.Connector();
            return con.IsExists("Schedule");
        }
    }
}