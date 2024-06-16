namespace App
{
	partial class CasesForm
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
			this.btnQuestion = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnAdd = new System.Windows.Forms.Button();
			this.lblEnd = new System.Windows.Forms.Label();
			this.lblTaskName = new System.Windows.Forms.Label();
			this.dateTimePicker = new System.Windows.Forms.DateTimePicker();
			this.dataGridView = new System.Windows.Forms.DataGridView();
			this.richTextBox = new System.Windows.Forms.RichTextBox();
			this.numericUpDown = new System.Windows.Forms.NumericUpDown();
			this.lblPriority = new System.Windows.Forms.Label();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.DeleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
			this.label1 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown)).BeginInit();
			this.contextMenuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnQuestion
			// 
			this.btnQuestion.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.btnQuestion.Location = new System.Drawing.Point(518, 37);
			this.btnQuestion.Margin = new System.Windows.Forms.Padding(1);
			this.btnQuestion.Name = "btnQuestion";
			this.btnQuestion.Size = new System.Drawing.Size(30, 30);
			this.btnQuestion.TabIndex = 22;
			this.btnQuestion.UseVisualStyleBackColor = true;
			// 
			// btnOK
			// 
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(473, 10);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(75, 23);
			this.btnOK.TabIndex = 21;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// btnAdd
			// 
			this.btnAdd.Location = new System.Drawing.Point(356, 10);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Size = new System.Drawing.Size(75, 23);
			this.btnAdd.TabIndex = 20;
			this.btnAdd.Text = "Добавить";
			this.btnAdd.UseVisualStyleBackColor = true;
			this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
			// 
			// lblEnd
			// 
			this.lblEnd.AutoSize = true;
			this.lblEnd.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.lblEnd.Location = new System.Drawing.Point(12, 64);
			this.lblEnd.Name = "lblEnd";
			this.lblEnd.Size = new System.Drawing.Size(99, 15);
			this.lblEnd.TabIndex = 19;
			this.lblEnd.Text = "Заканчивается:";
			// 
			// lblTaskName
			// 
			this.lblTaskName.AutoSize = true;
			this.lblTaskName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.lblTaskName.Location = new System.Drawing.Point(255, 13);
			this.lblTaskName.Name = "lblTaskName";
			this.lblTaskName.Size = new System.Drawing.Size(95, 15);
			this.lblTaskName.TabIndex = 18;
			this.lblTaskName.Text = "Название дела";
			// 
			// dateTimePicker
			// 
			this.dateTimePicker.Location = new System.Drawing.Point(111, 64);
			this.dateTimePicker.Name = "dateTimePicker";
			this.dateTimePicker.Size = new System.Drawing.Size(138, 20);
			this.dateTimePicker.TabIndex = 17;
			// 
			// dataGridView
			// 
			this.dataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.dataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
			this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridView.Location = new System.Drawing.Point(12, 90);
			this.dataGridView.Name = "dataGridView";
			this.dataGridView.ReadOnly = true;
			this.dataGridView.Size = new System.Drawing.Size(536, 291);
			this.dataGridView.TabIndex = 16;
			this.dataGridView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dataGridView1_KeyDown);
			// 
			// richTextBox
			// 
			this.richTextBox.Location = new System.Drawing.Point(12, 10);
			this.richTextBox.Name = "richTextBox";
			this.richTextBox.Size = new System.Drawing.Size(237, 21);
			this.richTextBox.TabIndex = 15;
			this.richTextBox.Text = "";
			// 
			// numericUpDown
			// 
			this.numericUpDown.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.numericUpDown.Location = new System.Drawing.Point(340, 64);
			this.numericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numericUpDown.Name = "numericUpDown";
			this.numericUpDown.Size = new System.Drawing.Size(93, 20);
			this.numericUpDown.TabIndex = 23;
			this.numericUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// lblPriority
			// 
			this.lblPriority.AutoSize = true;
			this.lblPriority.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.lblPriority.Location = new System.Drawing.Point(259, 64);
			this.lblPriority.Name = "lblPriority";
			this.lblPriority.Size = new System.Drawing.Size(75, 15);
			this.lblPriority.TabIndex = 24;
			this.lblPriority.Text = "Приоритет:";
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DeleteToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(148, 26);
			// 
			// DeleteToolStripMenuItem
			// 
			this.DeleteToolStripMenuItem.Name = "DeleteToolStripMenuItem";
			this.DeleteToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
			this.DeleteToolStripMenuItem.Text = "Удалить дело";
			this.DeleteToolStripMenuItem.Click += new System.EventHandler(this.DeleteToolStripMenuItem_Click);
			// 
			// dateTimePicker1
			// 
			this.dateTimePicker1.CustomFormat = "hh\\:mm";
			this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Time;
			this.dateTimePicker1.Location = new System.Drawing.Point(158, 37);
			this.dateTimePicker1.Name = "dateTimePicker1";
			this.dateTimePicker1.ShowUpDown = true;
			this.dateTimePicker1.Size = new System.Drawing.Size(138, 20);
			this.dateTimePicker1.TabIndex = 25;
			this.dateTimePicker1.Value = new System.DateTime(2024, 6, 13, 0, 0, 0, 0);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label1.Location = new System.Drawing.Point(12, 37);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(140, 15);
			this.label1.TabIndex = 26;
			this.label1.Text = "Время на выполнение:";
			// 
			// CasesForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(562, 388);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.dateTimePicker1);
			this.Controls.Add(this.lblPriority);
			this.Controls.Add(this.numericUpDown);
			this.Controls.Add(this.btnQuestion);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnAdd);
			this.Controls.Add(this.lblEnd);
			this.Controls.Add(this.lblTaskName);
			this.Controls.Add(this.dateTimePicker);
			this.Controls.Add(this.dataGridView);
			this.Controls.Add(this.richTextBox);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Name = "CasesForm";
			this.Text = "Дела";
			((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown)).EndInit();
			this.contextMenuStrip1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnQuestion;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.Label lblEnd;
		private System.Windows.Forms.Label lblTaskName;
		private System.Windows.Forms.DateTimePicker dateTimePicker;
		private System.Windows.Forms.DataGridView dataGridView;
		private System.Windows.Forms.RichTextBox richTextBox;
		private System.Windows.Forms.NumericUpDown numericUpDown;
		private System.Windows.Forms.Label lblPriority;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolStripMenuItem DeleteToolStripMenuItem;
		private System.Windows.Forms.DateTimePicker dateTimePicker1;
		private System.Windows.Forms.Label label1;
	}
}