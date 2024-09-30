namespace MyOrderMaster
{
    partial class SetPortfolioForm
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.InputCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.ExitButton = new System.Windows.Forms.Button();
            this.SaveSettingButton = new System.Windows.Forms.Button();
            this.MoveDownButton = new System.Windows.Forms.Button();
            this.MoveUpButton = new System.Windows.Forms.Button();
            this.DeleteButton = new System.Windows.Forms.Button();
            this.AddButton = new System.Windows.Forms.Button();
            this.OutputCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.PortfolioLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.ResetPortfolioButton = new System.Windows.Forms.Button();
            this.RenamePortfolioButton = new System.Windows.Forms.Button();
            this.OpenPortfolioButton = new System.Windows.Forms.Button();
            this.PortfolioGrid = new System.Windows.Forms.DataGridView();
            this.PortfolioID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PortfolioName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label3 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PortfolioGrid)).BeginInit();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // InputCheckedListBox
            // 
            this.InputCheckedListBox.CheckOnClick = true;
            this.InputCheckedListBox.Font = new System.Drawing.Font("新細明體-ExtB", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.InputCheckedListBox.FormattingEnabled = true;
            this.InputCheckedListBox.Location = new System.Drawing.Point(387, 71);
            this.InputCheckedListBox.Name = "InputCheckedListBox";
            this.InputCheckedListBox.Size = new System.Drawing.Size(165, 327);
            this.InputCheckedListBox.Sorted = true;
            this.InputCheckedListBox.TabIndex = 24;
            // 
            // ExitButton
            // 
            this.ExitButton.Location = new System.Drawing.Point(788, 424);
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.Size = new System.Drawing.Size(75, 23);
            this.ExitButton.TabIndex = 23;
            this.ExitButton.Text = "離開";
            this.ExitButton.UseVisualStyleBackColor = true;
            this.ExitButton.Click += new System.EventHandler(this.ExitButton_Click);
            // 
            // SaveSettingButton
            // 
            this.SaveSettingButton.Location = new System.Drawing.Point(661, 424);
            this.SaveSettingButton.Name = "SaveSettingButton";
            this.SaveSettingButton.Size = new System.Drawing.Size(75, 23);
            this.SaveSettingButton.TabIndex = 22;
            this.SaveSettingButton.Text = "儲存";
            this.SaveSettingButton.UseVisualStyleBackColor = true;
            this.SaveSettingButton.Click += new System.EventHandler(this.SaveSettingButton_Click);
            // 
            // MoveDownButton
            // 
            this.MoveDownButton.Location = new System.Drawing.Point(584, 335);
            this.MoveDownButton.Name = "MoveDownButton";
            this.MoveDownButton.Size = new System.Drawing.Size(75, 23);
            this.MoveDownButton.TabIndex = 21;
            this.MoveDownButton.Text = "下移";
            this.MoveDownButton.UseVisualStyleBackColor = true;
            this.MoveDownButton.Click += new System.EventHandler(this.MoveDownButton_Click);
            // 
            // MoveUpButton
            // 
            this.MoveUpButton.Location = new System.Drawing.Point(584, 306);
            this.MoveUpButton.Name = "MoveUpButton";
            this.MoveUpButton.Size = new System.Drawing.Size(75, 23);
            this.MoveUpButton.TabIndex = 20;
            this.MoveUpButton.Text = "上移";
            this.MoveUpButton.UseVisualStyleBackColor = true;
            this.MoveUpButton.Click += new System.EventHandler(this.MoveUpButton_Click);
            // 
            // DeleteButton
            // 
            this.DeleteButton.Location = new System.Drawing.Point(584, 169);
            this.DeleteButton.Name = "DeleteButton";
            this.DeleteButton.Size = new System.Drawing.Size(75, 23);
            this.DeleteButton.TabIndex = 19;
            this.DeleteButton.Text = "<  刪除";
            this.DeleteButton.UseVisualStyleBackColor = true;
            this.DeleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            // 
            // AddButton
            // 
            this.AddButton.Location = new System.Drawing.Point(584, 140);
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(75, 23);
            this.AddButton.TabIndex = 18;
            this.AddButton.Text = ">  新增";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // OutputCheckedListBox
            // 
            this.OutputCheckedListBox.CheckOnClick = true;
            this.OutputCheckedListBox.FormattingEnabled = true;
            this.OutputCheckedListBox.Location = new System.Drawing.Point(701, 71);
            this.OutputCheckedListBox.Name = "OutputCheckedListBox";
            this.OutputCheckedListBox.Size = new System.Drawing.Size(165, 327);
            this.OutputCheckedListBox.TabIndex = 17;
            this.OutputCheckedListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.OutputCheckedListBox_ItemCheck);
            // 
            // PortfolioLabel
            // 
            this.PortfolioLabel.AutoSize = true;
            this.PortfolioLabel.Location = new System.Drawing.Point(21, 13);
            this.PortfolioLabel.Name = "PortfolioLabel";
            this.PortfolioLabel.Size = new System.Drawing.Size(41, 12);
            this.PortfolioLabel.TabIndex = 25;
            this.PortfolioLabel.Text = "自選股";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(29, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(89, 12);
            this.label2.TabIndex = 26;
            this.label2.Text = "可選擇商品清單";
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.ResetPortfolioButton);
            this.panel1.Controls.Add(this.RenamePortfolioButton);
            this.panel1.Controls.Add(this.OpenPortfolioButton);
            this.panel1.Controls.Add(this.PortfolioGrid);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Location = new System.Drawing.Point(38, 20);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(271, 378);
            this.panel1.TabIndex = 27;
            // 
            // ResetPortfolioButton
            // 
            this.ResetPortfolioButton.Location = new System.Drawing.Point(176, 239);
            this.ResetPortfolioButton.Name = "ResetPortfolioButton";
            this.ResetPortfolioButton.Size = new System.Drawing.Size(52, 23);
            this.ResetPortfolioButton.TabIndex = 32;
            this.ResetPortfolioButton.Text = "刪除";
            this.ResetPortfolioButton.UseVisualStyleBackColor = true;
            this.ResetPortfolioButton.Click += new System.EventHandler(this.ResetPortfolioButton_Click);
            // 
            // RenamePortfolioButton
            // 
            this.RenamePortfolioButton.Location = new System.Drawing.Point(102, 239);
            this.RenamePortfolioButton.Name = "RenamePortfolioButton";
            this.RenamePortfolioButton.Size = new System.Drawing.Size(52, 23);
            this.RenamePortfolioButton.TabIndex = 31;
            this.RenamePortfolioButton.Text = "更名";
            this.RenamePortfolioButton.UseVisualStyleBackColor = true;
            this.RenamePortfolioButton.Click += new System.EventHandler(this.RenamePortfolioButton_Click);
            // 
            // OpenPortfolioButton
            // 
            this.OpenPortfolioButton.Location = new System.Drawing.Point(28, 239);
            this.OpenPortfolioButton.Name = "OpenPortfolioButton";
            this.OpenPortfolioButton.Size = new System.Drawing.Size(52, 23);
            this.OpenPortfolioButton.TabIndex = 30;
            this.OpenPortfolioButton.Text = "開啟";
            this.OpenPortfolioButton.UseVisualStyleBackColor = true;
            this.OpenPortfolioButton.Click += new System.EventHandler(this.OpenPortfolioButton_Click);
            // 
            // PortfolioGrid
            // 
            this.PortfolioGrid.AllowUserToAddRows = false;
            this.PortfolioGrid.AllowUserToDeleteRows = false;
            this.PortfolioGrid.AllowUserToResizeColumns = false;
            this.PortfolioGrid.AllowUserToResizeRows = false;
            this.PortfolioGrid.BackgroundColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("新細明體", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.PortfolioGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.PortfolioGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.PortfolioGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.PortfolioID,
            this.PortfolioName});
            this.PortfolioGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.PortfolioGrid.Location = new System.Drawing.Point(5, 34);
            this.PortfolioGrid.MultiSelect = false;
            this.PortfolioGrid.Name = "PortfolioGrid";
            this.PortfolioGrid.RowHeadersVisible = false;
            this.PortfolioGrid.RowHeadersWidth = 5;
            this.PortfolioGrid.RowTemplate.Height = 24;
            this.PortfolioGrid.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.PortfolioGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.PortfolioGrid.Size = new System.Drawing.Size(260, 171);
            this.PortfolioGrid.TabIndex = 29;
            this.PortfolioGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.PortfolioGrid_CellDoubleClick);
            this.PortfolioGrid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.PortfolioGrid_CellEndEdit);
            this.PortfolioGrid.SelectionChanged += new System.EventHandler(this.PortfolioGrid_SelectionChanged);
            // 
            // PortfolioID
            // 
            this.PortfolioID.HeaderText = "序號";
            this.PortfolioID.Name = "PortfolioID";
            this.PortfolioID.ReadOnly = true;
            this.PortfolioID.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.PortfolioID.Width = 40;
            // 
            // PortfolioName
            // 
            this.PortfolioName.HeaderText = "自選股組名";
            this.PortfolioName.Name = "PortfolioName";
            this.PortfolioName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.PortfolioName.Width = 200;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(65, 12);
            this.label3.TabIndex = 28;
            this.label3.Text = "自選股組合";
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.label2);
            this.panel2.Location = new System.Drawing.Point(387, 20);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(165, 38);
            this.panel2.TabIndex = 28;
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.PortfolioLabel);
            this.panel3.Location = new System.Drawing.Point(701, 20);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(165, 38);
            this.panel3.TabIndex = 29;
            // 
            // SetPortfolioForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(899, 473);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.InputCheckedListBox);
            this.Controls.Add(this.ExitButton);
            this.Controls.Add(this.SaveSettingButton);
            this.Controls.Add(this.MoveDownButton);
            this.Controls.Add(this.MoveUpButton);
            this.Controls.Add(this.DeleteButton);
            this.Controls.Add(this.AddButton);
            this.Controls.Add(this.OutputCheckedListBox);
            this.Name = "SetPortfolioForm";
            this.Text = "設定自選股組合";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SetPortfolioForm_FormClosing);
            this.Load += new System.EventHandler(this.SetPortfolioForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PortfolioGrid)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckedListBox InputCheckedListBox;
        private System.Windows.Forms.Button ExitButton;
        private System.Windows.Forms.Button SaveSettingButton;
        private System.Windows.Forms.Button MoveDownButton;
        private System.Windows.Forms.Button MoveUpButton;
        private System.Windows.Forms.Button DeleteButton;
        private System.Windows.Forms.Button AddButton;
        private System.Windows.Forms.CheckedListBox OutputCheckedListBox;
        private System.Windows.Forms.Label PortfolioLabel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button ResetPortfolioButton;
        private System.Windows.Forms.Button RenamePortfolioButton;
        private System.Windows.Forms.Button OpenPortfolioButton;
        private System.Windows.Forms.DataGridView PortfolioGrid;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.DataGridViewTextBoxColumn PortfolioID;
        private System.Windows.Forms.DataGridViewTextBoxColumn PortfolioName;
    }
}