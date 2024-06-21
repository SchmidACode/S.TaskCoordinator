using ScheduleTaskCoordinator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace TaskCoordinator
{
	public partial class BorderTaskForm : Form
	{
		private Connector connector;
		int id;
		public BorderTaskForm(int id)
		{
			InitializeComponent();
			connector = new Connector();
			this.id = id;
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			if (File.Exists("ScheduleTaskCoordinator.db"))
			{
				try
				{
					string StartTime = dateTimePicker1.Value.TimeOfDay.ToString(@"hh\:mm\:ss");
					string EndTime = dateTimePicker2.Value.TimeOfDay.ToString(@"hh\:mm\:ss");
					if (StartTime != EndTime && TimeSpan.Parse(StartTime) < TimeSpan.Parse(EndTime))
					{
						connector.UpdateDataInBase("Tasks", "StartTime", StartTime, id);
						connector.UpdateDataInBase("Tasks", "EndTime", EndTime, id);
					}
					else
						MessageBox.Show("Промежуток времени не должен совпадать или быть некорректным.");
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error updating the data in the DB file: " + ex.Message);
				}
			}
		}
		private void BorderTaskForm_Load(object sender, EventArgs e)
		{
			DataTable dt = connector.LoadSortedDataFromDB($"SELECT StartTime, EndTime FROM Tasks WHERE Id = {id}");
			if (dt.Rows.Count > 0)
			{
				if (TimeSpan.TryParse(dt.Rows[0]["StartTime"].ToString(), out TimeSpan StartTime))
				{
					dateTimePicker1.Value = DateTime.Today.Add(StartTime);
				}
				if (TimeSpan.TryParse(dt.Rows[0]["EndTime"].ToString(), out TimeSpan EndTime))
				{
					dateTimePicker2.Value = DateTime.Today.Add(EndTime);
				}
			}
		}
	}
}
