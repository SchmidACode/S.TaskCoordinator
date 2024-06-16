using App;
using System;
using System.Windows.Forms;
using System.Data.SQLite;

namespace WindowsFormsApp
{
	static class Program
	{
		[STAThread]
		static void Main()
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

		static bool IsConditionMet()
		{
			App.Connector con = new App.Connector();
			return con.IsExists("Schedule"); 
		}
	}
}