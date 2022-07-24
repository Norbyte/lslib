namespace ConverterApp
{
    partial class OsirisPane
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.saveStoryBtn = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.databaseGrid = new System.Windows.Forms.DataGridView();
            this.databaseSelectorCb = new System.Windows.Forms.ComboBox();
            this.label18 = new System.Windows.Forms.Label();
            this.loadStoryBtn = new System.Windows.Forms.Button();
            this.decompileStoryBtn = new System.Windows.Forms.Button();
            this.storyFilePath = new System.Windows.Forms.TextBox();
            this.goalPathBrowseBtn = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.goalPath = new System.Windows.Forms.TextBox();
            this.storyFileBrowseBtn = new System.Windows.Forms.Button();
            this.label10 = new System.Windows.Forms.Label();
            this.storyPathDlg = new System.Windows.Forms.OpenFileDialog();
            this.goalPathDlg = new System.Windows.Forms.FolderBrowserDialog();
            this.btnDebugExport = new System.Windows.Forms.Button();
            this.lblFilter = new System.Windows.Forms.Label();
            this.tbFilter = new System.Windows.Forms.TextBox();
            this.btnFilterMatchCase = new System.Windows.Forms.Button();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.databaseGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // saveStoryBtn
            // 
            this.saveStoryBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.saveStoryBtn.Location = new System.Drawing.Point(728, 50);
            this.saveStoryBtn.Name = "saveStoryBtn";
            this.saveStoryBtn.Size = new System.Drawing.Size(121, 23);
            this.saveStoryBtn.TabIndex = 69;
            this.saveStoryBtn.Text = "Save";
            this.saveStoryBtn.UseVisualStyleBackColor = true;
            this.saveStoryBtn.Click += new System.EventHandler(this.saveStoryBtn_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.databaseGrid);
            this.groupBox3.Controls.Add(this.databaseSelectorCb);
            this.groupBox3.Controls.Add(this.label18);
            this.groupBox3.Controls.Add(this.lblFilter);
            this.groupBox3.Controls.Add(this.tbFilter);
            this.groupBox3.Controls.Add(this.btnFilterMatchCase);
            this.groupBox3.Location = new System.Drawing.Point(5, 147);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(849, 441);
            this.groupBox3.TabIndex = 68;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Database Editor";
            // 
            // lblFilter
            // 
            this.lblFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFilter.AutoSize = true;
            this.lblFilter.Location = new System.Drawing.Point(554, 22);
            this.lblFilter.Name = "lblFilter";
            this.lblFilter.Size = new System.Drawing.Size(32, 13);
            this.lblFilter.TabIndex = 4;
            this.lblFilter.Text = "Filter:";
            // 
            // tbFilter
            // 
            this.tbFilter.AcceptsReturn = true;
            this.tbFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tbFilter.Location = new System.Drawing.Point(594, 18);
            this.tbFilter.Name = "tbFilter";
            this.tbFilter.Size = new System.Drawing.Size(197, 20);
            this.tbFilter.TabIndex = 3;
            this.tbFilter.KeyUp += new System.Windows.Forms.KeyEventHandler(this.databaseFilter_KeyUp);
            // 
            // btnFilterMatchCase
            // 
            this.btnFilterMatchCase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFilterMatchCase.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnFilterMatchCase.Location = new System.Drawing.Point(797, 17);
            this.btnFilterMatchCase.Name = "btnFilterMatchCase";
            this.btnFilterMatchCase.Size = new System.Drawing.Size(41, 22);
            this.btnFilterMatchCase.TabIndex = 5;
            this.btnFilterMatchCase.Text = "Aa";
            this.btnFilterMatchCase.UseVisualStyleBackColor = true;
            this.btnFilterMatchCase.Click += new System.EventHandler(this.btnDatabaseFilterMatchCase_Click);
            // 
            // databaseGrid
            // 
            this.databaseGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.databaseGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.databaseGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.databaseGrid.Location = new System.Drawing.Point(9, 47);
            this.databaseGrid.Name = "databaseGrid";
            this.databaseGrid.Size = new System.Drawing.Size(829, 380);
            this.databaseGrid.TabIndex = 2;
            // 
            // databaseSelectorCb
            // 
            this.databaseSelectorCb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.databaseSelectorCb.FormattingEnabled = true;
            this.databaseSelectorCb.Location = new System.Drawing.Point(67, 18);
            this.databaseSelectorCb.Name = "databaseSelectorCb";
            this.databaseSelectorCb.Size = new System.Drawing.Size(471, 21);
            this.databaseSelectorCb.TabIndex = 1;
            this.databaseSelectorCb.SelectedIndexChanged += new System.EventHandler(this.databaseSelectorCb_SelectedIndexChanged);
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(6, 22);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(56, 13);
            this.label18.TabIndex = 0;
            this.label18.Text = "Database:";
            // 
            // loadStoryBtn
            // 
            this.loadStoryBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.loadStoryBtn.Location = new System.Drawing.Point(728, 21);
            this.loadStoryBtn.Name = "loadStoryBtn";
            this.loadStoryBtn.Size = new System.Drawing.Size(121, 23);
            this.loadStoryBtn.TabIndex = 67;
            this.loadStoryBtn.Text = "Load";
            this.loadStoryBtn.UseVisualStyleBackColor = true;
            this.loadStoryBtn.Click += new System.EventHandler(this.loadStoryBtn_Click);
            // 
            // decompileStoryBtn
            // 
            this.decompileStoryBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.decompileStoryBtn.Location = new System.Drawing.Point(728, 92);
            this.decompileStoryBtn.Name = "decompileStoryBtn";
            this.decompileStoryBtn.Size = new System.Drawing.Size(121, 23);
            this.decompileStoryBtn.TabIndex = 66;
            this.decompileStoryBtn.Text = "Extract";
            this.decompileStoryBtn.UseVisualStyleBackColor = true;
            this.decompileStoryBtn.Click += new System.EventHandler(this.decompileStoryBtn_Click);
            // 
            // storyFilePath
            // 
            this.storyFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.storyFilePath.Location = new System.Drawing.Point(7, 22);
            this.storyFilePath.Name = "storyFilePath";
            this.storyFilePath.Size = new System.Drawing.Size(678, 20);
            this.storyFilePath.TabIndex = 61;
            // 
            // goalPathBrowseBtn
            // 
            this.goalPathBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.goalPathBrowseBtn.Location = new System.Drawing.Point(683, 92);
            this.goalPathBrowseBtn.Name = "goalPathBrowseBtn";
            this.goalPathBrowseBtn.Size = new System.Drawing.Size(41, 22);
            this.goalPathBrowseBtn.TabIndex = 65;
            this.goalPathBrowseBtn.Text = "...";
            this.goalPathBrowseBtn.UseVisualStyleBackColor = true;
            this.goalPathBrowseBtn.Click += new System.EventHandler(this.goalPathBrowseBtn_Click);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(4, 6);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(128, 13);
            this.label9.TabIndex = 60;
            this.label9.Text = "Story/savegame file path:";
            // 
            // goalPath
            // 
            this.goalPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.goalPath.Location = new System.Drawing.Point(7, 93);
            this.goalPath.Name = "goalPath";
            this.goalPath.Size = new System.Drawing.Size(678, 20);
            this.goalPath.TabIndex = 64;
            // 
            // storyFileBrowseBtn
            // 
            this.storyFileBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.storyFileBrowseBtn.Location = new System.Drawing.Point(683, 21);
            this.storyFileBrowseBtn.Name = "storyFileBrowseBtn";
            this.storyFileBrowseBtn.Size = new System.Drawing.Size(41, 22);
            this.storyFileBrowseBtn.TabIndex = 62;
            this.storyFileBrowseBtn.Text = "...";
            this.storyFileBrowseBtn.UseVisualStyleBackColor = true;
            this.storyFileBrowseBtn.Click += new System.EventHandler(this.storyFileBrowseBtn_Click);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(4, 77);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(89, 13);
            this.label10.TabIndex = 63;
            this.label10.Text = "Goal output path:";
            // 
            // storyPathDlg
            // 
            this.storyPathDlg.CheckFileExists = false;
            this.storyPathDlg.Filter = "LS story/savegame files|*.osi;*.lsv";
            // 
            // btnDebugExport
            // 
            this.btnDebugExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDebugExport.Location = new System.Drawing.Point(728, 121);
            this.btnDebugExport.Name = "btnDebugExport";
            this.btnDebugExport.Size = new System.Drawing.Size(121, 23);
            this.btnDebugExport.TabIndex = 70;
            this.btnDebugExport.Text = "Debug Export";
            this.btnDebugExport.UseVisualStyleBackColor = true;
            this.btnDebugExport.Click += new System.EventHandler(this.btnDebugExport_Click);
            // 
            // OsirisPane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnDebugExport);
            this.Controls.Add(this.saveStoryBtn);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.loadStoryBtn);
            this.Controls.Add(this.decompileStoryBtn);
            this.Controls.Add(this.storyFilePath);
            this.Controls.Add(this.goalPathBrowseBtn);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.goalPath);
            this.Controls.Add(this.storyFileBrowseBtn);
            this.Controls.Add(this.label10);
            this.Name = "OsirisPane";
            this.Size = new System.Drawing.Size(863, 588);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.databaseGrid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button saveStoryBtn;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.DataGridView databaseGrid;
        private System.Windows.Forms.ComboBox databaseSelectorCb;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Button loadStoryBtn;
        private System.Windows.Forms.Button decompileStoryBtn;
        private System.Windows.Forms.TextBox storyFilePath;
        private System.Windows.Forms.Button goalPathBrowseBtn;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox goalPath;
        private System.Windows.Forms.Button storyFileBrowseBtn;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.OpenFileDialog storyPathDlg;
        private System.Windows.Forms.FolderBrowserDialog goalPathDlg;
        private System.Windows.Forms.Button btnDebugExport;
        private System.Windows.Forms.Label lblFilter;
        private System.Windows.Forms.TextBox tbFilter;
        private System.Windows.Forms.Button btnFilterMatchCase;
    }
}
