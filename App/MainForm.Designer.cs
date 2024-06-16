namespace App
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.настройкиToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ScheduleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.DelayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.настройкиToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(595, 24);
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
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.MenuHighlight;
			this.ClientSize = new System.Drawing.Size(595, 450);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "MainForm";
			this.Text = "Task Coordinator";
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem настройкиToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ScheduleToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CaseToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem DelayToolStripMenuItem;
	}
}

