namespace App
{
	partial class ScheduleForm
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
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.lblDayOfTheWeek = new System.Windows.Forms.Label();
			this.richTextBox1 = new System.Windows.Forms.RichTextBox();
			this.dateStartTime = new System.Windows.Forms.DateTimePicker();
			this.dataGridView1 = new System.Windows.Forms.DataGridView();
			this.dateEndTime = new System.Windows.Forms.DateTimePicker();
			this.lblTaskName = new System.Windows.Forms.Label();
			this.lblEnd = new System.Windows.Forms.Label();
			this.lblStart = new System.Windows.Forms.Label();
			this.btnAdd = new System.Windows.Forms.Button();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.DeleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.btnFinish = new System.Windows.Forms.Button();
			this.btnQuestion = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
			this.contextMenuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// comboBox1
			// 
			this.comboBox1.FormattingEnabled = true;
			this.comboBox1.Location = new System.Drawing.Point(12, 12);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(237, 21);
			this.comboBox1.TabIndex = 1;
			// 
			// lblDayOfTheWeek
			// 
			this.lblDayOfTheWeek.AutoSize = true;
			this.lblDayOfTheWeek.Location = new System.Drawing.Point(255, 15);
			this.lblDayOfTheWeek.Name = "lblDayOfTheWeek";
			this.lblDayOfTheWeek.Size = new System.Drawing.Size(73, 13);
			this.lblDayOfTheWeek.TabIndex = 3;
			this.lblDayOfTheWeek.Text = "День недели";
			// 
			// richTextBox1
			// 
			this.richTextBox1.Location = new System.Drawing.Point(12, 39);
			this.richTextBox1.Name = "richTextBox1";
			this.richTextBox1.Size = new System.Drawing.Size(237, 21);
			this.richTextBox1.TabIndex = 4;
			this.richTextBox1.Text = "";
			// 
			// dateStartTime
			// 
			this.dateStartTime.CustomFormat = "HH:mm";
			this.dateStartTime.Format = System.Windows.Forms.DateTimePickerFormat.Time;
			this.dateStartTime.Location = new System.Drawing.Point(12, 66);
			this.dateStartTime.Name = "dateStartTime";
			this.dateStartTime.ShowUpDown = true;
			this.dateStartTime.Size = new System.Drawing.Size(237, 20);
			this.dateStartTime.TabIndex = 5;
			this.dateStartTime.Value = new System.DateTime(2024, 6, 17, 0, 0, 0, 0);
			// 
			// dataGridView1
			// 
			this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.dataGridView1.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
			this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridView1.Location = new System.Drawing.Point(12, 125);
			this.dataGridView1.Name = "dataGridView1";
			this.dataGridView1.ReadOnly = true;
			this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.dataGridView1.Size = new System.Drawing.Size(536, 313);
			this.dataGridView1.TabIndex = 6;
			this.dataGridView1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dataGridView1_KeyDown);
			// 
			// dateEndTime
			// 
			this.dateEndTime.CustomFormat = "HH:mm";
			this.dateEndTime.Format = System.Windows.Forms.DateTimePickerFormat.Time;
			this.dateEndTime.Location = new System.Drawing.Point(12, 92);
			this.dateEndTime.Name = "dateEndTime";
			this.dateEndTime.ShowUpDown = true;
			this.dateEndTime.Size = new System.Drawing.Size(237, 20);
			this.dateEndTime.TabIndex = 7;
			this.dateEndTime.Value = new System.DateTime(2024, 6, 17, 0, 0, 0, 0);
			// 
			// lblTaskName
			// 
			this.lblTaskName.AutoSize = true;
			this.lblTaskName.Location = new System.Drawing.Point(255, 42);
			this.lblTaskName.Name = "lblTaskName";
			this.lblTaskName.Size = new System.Drawing.Size(84, 13);
			this.lblTaskName.TabIndex = 8;
			this.lblTaskName.Text = "Название дела";
			// 
			// lblEnd
			// 
			this.lblEnd.AutoSize = true;
			this.lblEnd.Location = new System.Drawing.Point(255, 98);
			this.lblEnd.Name = "lblEnd";
			this.lblEnd.Size = new System.Drawing.Size(84, 13);
			this.lblEnd.TabIndex = 9;
			this.lblEnd.Text = "Заканчивается";
			// 
			// lblStart
			// 
			this.lblStart.AutoSize = true;
			this.lblStart.Location = new System.Drawing.Point(255, 72);
			this.lblStart.Name = "lblStart";
			this.lblStart.Size = new System.Drawing.Size(67, 13);
			this.lblStart.TabIndex = 10;
			this.lblStart.Text = "Начинается";
			// 
			// btnAdd
			// 
			this.btnAdd.Location = new System.Drawing.Point(352, 12);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Size = new System.Drawing.Size(75, 23);
			this.btnAdd.TabIndex = 11;
			this.btnAdd.Text = "Добавить";
			this.btnAdd.UseVisualStyleBackColor = true;
			this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DeleteToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(149, 26);
			// 
			// DeleteToolStripMenuItem
			// 
			this.DeleteToolStripMenuItem.Name = "DeleteToolStripMenuItem";
			this.DeleteToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
			this.DeleteToolStripMenuItem.Text = "Удалить план";
			this.DeleteToolStripMenuItem.Click += new System.EventHandler(this.DeleteToolStripMenuItem_Click);
			// 
			// btnFinish
			// 
			this.btnFinish.Location = new System.Drawing.Point(473, 12);
			this.btnFinish.Name = "btnFinish";
			this.btnFinish.Size = new System.Drawing.Size(75, 23);
			this.btnFinish.TabIndex = 13;
			this.btnFinish.Text = "OK";
			this.btnFinish.UseVisualStyleBackColor = true;
			this.btnFinish.Click += new System.EventHandler(this.btnFinish_Click);
			// 
			// btnQuestion
			// 
			this.btnQuestion.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.btnQuestion.Location = new System.Drawing.Point(513, 84);
			this.btnQuestion.Margin = new System.Windows.Forms.Padding(1);
			this.btnQuestion.Name = "btnQuestion";
			this.btnQuestion.Size = new System.Drawing.Size(30, 30);
			this.btnQuestion.TabIndex = 14;
			this.btnQuestion.UseVisualStyleBackColor = true;
			this.btnQuestion.Click += new System.EventHandler(this.btnQuestion_Click);
			// 
			// ScheduleForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size(560, 450);
			this.Controls.Add(this.btnQuestion);
			this.Controls.Add(this.btnFinish);
			this.Controls.Add(this.btnAdd);
			this.Controls.Add(this.lblStart);
			this.Controls.Add(this.lblEnd);
			this.Controls.Add(this.lblTaskName);
			this.Controls.Add(this.dateEndTime);
			this.Controls.Add(this.dataGridView1);
			this.Controls.Add(this.dateStartTime);
			this.Controls.Add(this.richTextBox1);
			this.Controls.Add(this.lblDayOfTheWeek);
			this.Controls.Add(this.comboBox1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Name = "ScheduleForm";
			this.ShowIcon = false;
			this.Text = "Редактирование расписания";
			((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
			this.contextMenuStrip1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.ComboBox comboBox1;
		private System.Windows.Forms.Label lblDayOfTheWeek;
		private System.Windows.Forms.RichTextBox richTextBox1;
		private System.Windows.Forms.DateTimePicker dateStartTime;
		private System.Windows.Forms.DataGridView dataGridView1;
		private System.Windows.Forms.DateTimePicker dateEndTime;
		private System.Windows.Forms.Label lblTaskName;
		private System.Windows.Forms.Label lblEnd;
		private System.Windows.Forms.Label lblStart;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolStripMenuItem DeleteToolStripMenuItem;
		private System.Windows.Forms.Button btnFinish;
		private System.Windows.Forms.Button btnQuestion;
	}
}