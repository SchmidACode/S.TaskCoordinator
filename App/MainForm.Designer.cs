namespace ScheduleTaskCoordinator
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.настройкиToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ScheduleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DelayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.dataGridView1 = new System.Windows.Forms.DataGridView();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.labelFreeTime = new System.Windows.Forms.Label();
			this.labelBusyTime = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.menuStrip1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.настройкиToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(656, 24);
			this.menuStrip1.TabIndex = 0;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// настройкиToolStripMenuItem
			// 
			this.настройкиToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ScheduleToolStripMenuItem,
            this.CaseToolStripMenuItem,
            this.DelayToolStripMenuItem});
			this.настройкиToolStripMenuItem.Name = "настройкиToolStripMenuItem";
			this.настройкиToolStripMenuItem.Size = new System.Drawing.Size(79, 20);
			this.настройкиToolStripMenuItem.Text = "Настройки";
			// 
			// ScheduleToolStripMenuItem
			// 
			this.ScheduleToolStripMenuItem.Name = "ScheduleToolStripMenuItem";
			this.ScheduleToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
			this.ScheduleToolStripMenuItem.Text = "Расписание";
			this.ScheduleToolStripMenuItem.Click += new System.EventHandler(this.ScheduleToolStripMenuItem_Click);
			// 
			// CaseToolStripMenuItem
			// 
			this.CaseToolStripMenuItem.Name = "CaseToolStripMenuItem";
			this.CaseToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
			this.CaseToolStripMenuItem.Text = "Задания";
			this.CaseToolStripMenuItem.Click += new System.EventHandler(this.CaseToolStripMenuItem_Click);
			// 
			// DelayToolStripMenuItem
			// 
			this.DelayToolStripMenuItem.Name = "DelayToolStripMenuItem";
			this.DelayToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
			this.DelayToolStripMenuItem.Text = "Промежуток";
			this.DelayToolStripMenuItem.Click += new System.EventHandler(this.DelayToolStripMenuItem_Click);
			// 
			// dataGridView1
			// 
			this.dataGridView1.AllowUserToAddRows = false;
			this.dataGridView1.AllowUserToDeleteRows = false;
			this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.dataGridView1.BackgroundColor = System.Drawing.SystemColors.ActiveCaptionText;
			this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridView1.Location = new System.Drawing.Point(0, 27);
			this.dataGridView1.MultiSelect = false;
			this.dataGridView1.Name = "dataGridView1";
			this.dataGridView1.Size = new System.Drawing.Size(494, 423);
			this.dataGridView1.TabIndex = 1;
			// 
			// timer1
			// 
			this.timer1.Enabled = true;
			this.timer1.Interval = 60000;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// labelFreeTime
			// 
			this.labelFreeTime.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelFreeTime.AutoSize = true;
			this.labelFreeTime.Font = new System.Drawing.Font("Rockwell", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelFreeTime.ForeColor = System.Drawing.Color.White;
			this.labelFreeTime.Location = new System.Drawing.Point(500, 57);
			this.labelFreeTime.Name = "labelFreeTime";
			this.labelFreeTime.Size = new System.Drawing.Size(119, 19);
			this.labelFreeTime.TabIndex = 2;
			this.labelFreeTime.Text = "Свободное время:";
			// 
			// labelBusyTime
			// 
			this.labelBusyTime.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.labelBusyTime.AutoSize = true;
			this.labelBusyTime.Font = new System.Drawing.Font("Rockwell", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelBusyTime.ForeColor = System.Drawing.Color.White;
			this.labelBusyTime.Location = new System.Drawing.Point(500, 142);
			this.labelBusyTime.Name = "labelBusyTime";
			this.labelBusyTime.Size = new System.Drawing.Size(125, 57);
			this.labelBusyTime.TabIndex = 3;
			this.labelBusyTime.Text = "Время отведенное \r\nна занятия:\r\n\r\n";
			// 
			// label1
			// 
			this.label1.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Rockwell", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
			this.label1.Location = new System.Drawing.Point(500, 236);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(138, 38);
			this.label1.TabIndex = 4;
			this.label1.Text = "Время до истечения\r\nближайшего задания:";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.InfoText;
			this.ClientSize = new System.Drawing.Size(656, 450);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.labelBusyTime);
			this.Controls.Add(this.labelFreeTime);
			this.Controls.Add(this.dataGridView1);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "MainForm";
			this.ShowIcon = false;
			this.Text = "  Schedule Task Coordinator";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem настройкиToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ScheduleToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CaseToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DelayToolStripMenuItem;
		private System.Windows.Forms.DataGridView dataGridView1;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.Label labelFreeTime;
		private System.Windows.Forms.Label labelBusyTime;
		private System.Windows.Forms.Label label1;
	}
}

