using System;
using System.Windows.Forms;
using TaskCoordinator.Properties;

namespace ScheduleTaskCoordinator
{
    public partial class DelayForm : Form
    {
        public DelayForm()
        {
            InitializeComponent();
        }

        private void DelayForm_Load(object sender, EventArgs e)
        {
            dateTimePicker1.Value = DateTime.Today.Add(Settings.Default.DelayTime);
            dateTimePicker2.Value = DateTime.Today.Add(Settings.Default.BorderStartTime);
            dateTimePicker3.Value = DateTime.Today.Add(Settings.Default.BorderEndTime);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Settings.Default.DelayTime = dateTimePicker1.Value.TimeOfDay;
            if (dateTimePicker2.Value < dateTimePicker3.Value)
            {
                Settings.Default.BorderStartTime = dateTimePicker2.Value.TimeOfDay;
                Settings.Default.BorderEndTime = dateTimePicker3.Value.TimeOfDay;
            }
            else
                MessageBox.Show("Промежуток времени не должен совпадать или быть некорректным");
            Settings.Default.Save();
        }

    }
}