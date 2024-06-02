using App;
using System;
using System.Windows.Forms;

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
						using (SettingsForm settingsForm = new SettingsForm())
						{
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
			// Здесь нужно проверить ваше условие
			// Например, проверка существования файла настроек
			// return File.Exists("settings.config");
			return false; // Временно возвращаем false для демонстрации
		}
	}
}