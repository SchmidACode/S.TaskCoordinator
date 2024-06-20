using System.Data;
using System.Windows.Forms;
using System;
using System.IO;

namespace ScheduleTaskCoordinator
{
	public partial class PostponeForm : Form
	{
		private Connector connector;
		int id;
		public PostponeForm(int id)
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
					string time = dateTimePicker1.Value.TimeOfDay.ToString(@"hh\:mm\:ss");
					if (time == "00:00:00") connector.UpdateDataInBase("Tasks", "DelayTime", null, id);
					else
						connector.UpdateDataInBase("Tasks", "DelayTime", time, id);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error updating the data in the DB file: " + ex.Message);
				}
			}
		}
		private void PostponeForm_Load(object sender, EventArgs e)
		{
			DataTable dt = connector.LoadSortedDataFromDB($"SELECT DelayTime FROM Tasks WHERE Id = {id}");
			if (dt.Rows.Count > 0)
			{
				if (dt.Rows[0]["DelayTime"] == DBNull.Value || string.IsNullOrEmpty(dt.Rows[0]["DelayTime"].ToString()))
				{
					dateTimePicker1.Value = DateTime.Today.Add(TimeSpan.Zero);
				}
				else
				{
					if (TimeSpan.TryParse(dt.Rows[0]["DelayTime"].ToString(), out TimeSpan delayTime))
					{
						dateTimePicker1.Value = DateTime.Today.Add(delayTime);
					}
				}
			}
		}
	}
}

