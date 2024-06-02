using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Data.SQLite;
using System.IO;
using System.Data.SqlClient;
using System.Reflection.Emit;

namespace App
{
	public partial class WelcomeForm : Form
	{
		private string databasePath = "MyApp.db";
		private Timer closeTimer;
		public WelcomeForm()
		{
			InitializeComponent();

			int panelCenterX = panel1.Width / 2;
			int panelCenterY = panel1.Height / 2;

			int labelWidth = labelInformation.Width;
			int labelHeight = labelInformation.Height;

			this.StartPosition = FormStartPosition.CenterScreen;
			labelInformation.Location = new System.Drawing.Point(panelCenterX - labelWidth / 2, panelCenterY - labelHeight / 2);
			if (CheckAndCreateDatabase() == true) StartCloseTimer();
		}

		private bool CheckAndCreateDatabase()
		{
			try {
				if (!File.Exists(databasePath))
				{
					SQLiteConnection.CreateFile(databasePath);
					using (var connection = new SQLiteConnection($"Data Source={databasePath};Version=3")) {
						connection.Open();
						CreateTables(connection);
					}

					if (File.Exists(databasePath)) {
						labelInformation.Text = $"База данных успешно создана по пути {Path.Combine(Directory.GetCurrentDirectory(), databasePath)}";
						return true;
					}
					else {
						labelInformation.Text = $"База данных не создана из-за ошибки";
						return false;
					}
				}
				else {
					labelInformation.Text = $"База данных найдена по пути {Path.Combine(Directory.GetCurrentDirectory(), databasePath)}";
					return true;
				}
			}
			catch (Exception ex) {
				labelInformation.Text = $"Ошибка при создании или открытии базы данных: {ex.Message}";
				return false;
			}
		}
		private void CreateTables(SQLiteConnection connection) 
		{
			string createScheduleTableQuery = @"
                CREATE TABLE Schedule (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Plan TEXT NOT NULL,
                    StartTime DATETIME NOT NULL,
                    EndTime DATETIME NOT NULL
				)";

			string createTasksTableQuery = @"
                CREATE TABLE Tasks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    DueDate DATETIME NOT NULL,
                    Priority INTEGER NOT NULL
                )";

			using (var command = new SQLiteCommand(createScheduleTableQuery, connection))
				command.ExecuteNonQuery();

			using (var command = new SQLiteCommand(createTasksTableQuery, connection))
				command.ExecuteNonQuery();
			
		}
		private void StartCloseTimer()
		{
			closeTimer = new Timer();
			closeTimer.Interval = 3000;
			closeTimer.Tick += CloseTimer_Tick;
			closeTimer.Start();
		}

		private void CloseTimer_Tick(object sender, EventArgs e)
		{
			closeTimer.Stop();
			this.DialogResult = DialogResult.OK;
			this.Close();
		}
		private void WelcomeForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			this.DialogResult = DialogResult.OK;
		}
	}
}
