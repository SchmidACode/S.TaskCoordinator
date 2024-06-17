using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace App
{
	public partial class DelayForm : Form
	{
		private string configFilePath;
		public DelayForm(string configFilePath)
		{
			InitializeComponent();
			this.configFilePath = configFilePath;
		}
		private void DelayForm_Load(object sender, EventArgs e)
		{
			if (File.Exists(configFilePath))
			{
				try
				{
					string[] lines = File.ReadAllLines(configFilePath);
					dateTimePicker1.Value = DateTime.Parse(lines[0]);
					dateTimePicker2.Value = DateTime.Parse(lines[1]);
					dateTimePicker3.Value = DateTime.Parse(lines[2]);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error reading the date from config file: " + ex.Message);
				}
			}
		}
		private void btnOK_Click(object sender, EventArgs e)
		{
			if (File.Exists(configFilePath))
			{
				try
				{
					TimeSpan date = dateTimePicker1.Value.TimeOfDay;
					TimeSpan TimeStart = dateTimePicker2.Value.TimeOfDay;
					TimeSpan TimeEnd = dateTimePicker3.Value.TimeOfDay;
					File.WriteAllText(configFilePath, $"{date}\n{TimeStart}\n{TimeEnd}");
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error reading the date from config file: " + ex.Message);
				}
			}
		}
	}
}
