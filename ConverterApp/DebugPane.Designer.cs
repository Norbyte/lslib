namespace ConverterApp
{
    partial class DebugPane
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
            this.dumpVariablesBtn = new System.Windows.Forms.Button();
            this.saveFilePath = new System.Windows.Forms.TextBox();
            this.savePathLbl = new System.Windows.Forms.Label();
            this.saveFileBrowseBtn = new System.Windows.Forms.Button();
            this.savePathDlg = new System.Windows.Forms.OpenFileDialog();
            this.dumpGlobalVars = new System.Windows.Forms.CheckBox();
            this.variableDumperBox = new System.Windows.Forms.GroupBox();
            this.includeLocalScopes = new System.Windows.Forms.CheckBox();
            this.includeUnnamedDbs = new System.Windows.Forms.CheckBox();
            this.dumpDatabases = new System.Windows.Forms.CheckBox();
            this.includeDeleted = new System.Windows.Forms.CheckBox();
            this.dumpItemVars = new System.Windows.Forms.CheckBox();
            this.dumpCharacterVars = new System.Windows.Forms.CheckBox();
            this.lblProgressStatus = new System.Windows.Forms.Label();
            this.dumpProgressBar = new System.Windows.Forms.ProgressBar();
            this.lblProgressText = new System.Windows.Forms.Label();
            this.dumpGoals = new System.Windows.Forms.CheckBox();
            this.extractPackage = new System.Windows.Forms.CheckBox();
            this.exportModList = new System.Windows.Forms.CheckBox();
            this.convertLsf = new System.Windows.Forms.CheckBox();
            this.variableDumperBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // dumpVariablesBtn
            // 
            this.dumpVariablesBtn.Location = new System.Drawing.Point(234, 111);
            this.dumpVariablesBtn.Name = "dumpVariablesBtn";
            this.dumpVariablesBtn.Size = new System.Drawing.Size(174, 23);
            this.dumpVariablesBtn.TabIndex = 72;
            this.dumpVariablesBtn.Text = "Dump savegame";
            this.dumpVariablesBtn.UseVisualStyleBackColor = true;
            this.dumpVariablesBtn.Click += new System.EventHandler(this.dumpVariablesBtn_Click);
            // 
            // saveFilePath
            // 
            this.saveFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.saveFilePath.Location = new System.Drawing.Point(13, 29);
            this.saveFilePath.Name = "saveFilePath";
            this.saveFilePath.Size = new System.Drawing.Size(777, 20);
            this.saveFilePath.TabIndex = 74;
            // 
            // savePathLbl
            // 
            this.savePathLbl.AutoSize = true;
            this.savePathLbl.Location = new System.Drawing.Point(10, 13);
            this.savePathLbl.Name = "savePathLbl";
            this.savePathLbl.Size = new System.Drawing.Size(101, 13);
            this.savePathLbl.TabIndex = 73;
            this.savePathLbl.Text = "Savegame file path:";
            // 
            // saveFileBrowseBtn
            // 
            this.saveFileBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.saveFileBrowseBtn.Location = new System.Drawing.Point(796, 28);
            this.saveFileBrowseBtn.Name = "saveFileBrowseBtn";
            this.saveFileBrowseBtn.Size = new System.Drawing.Size(41, 22);
            this.saveFileBrowseBtn.TabIndex = 75;
            this.saveFileBrowseBtn.Text = "...";
            this.saveFileBrowseBtn.UseVisualStyleBackColor = true;
            this.saveFileBrowseBtn.Click += new System.EventHandler(this.saveFileBrowseBtn_Click);
            // 
            // savePathDlg
            // 
            this.savePathDlg.Filter = "LS savegame files|*.lsv";
            // 
            // dumpGlobalVars
            // 
            this.dumpGlobalVars.AutoSize = true;
            this.dumpGlobalVars.Checked = true;
            this.dumpGlobalVars.CheckState = System.Windows.Forms.CheckState.Checked;
            this.dumpGlobalVars.Location = new System.Drawing.Point(16, 25);
            this.dumpGlobalVars.Name = "dumpGlobalVars";
            this.dumpGlobalVars.Size = new System.Drawing.Size(133, 17);
            this.dumpGlobalVars.TabIndex = 77;
            this.dumpGlobalVars.Text = "Dump global variables ";
            this.dumpGlobalVars.UseVisualStyleBackColor = true;
            // 
            // variableDumperBox
            // 
            this.variableDumperBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.variableDumperBox.Controls.Add(this.extractPackage);
            this.variableDumperBox.Controls.Add(this.exportModList);
            this.variableDumperBox.Controls.Add(this.convertLsf);
            this.variableDumperBox.Controls.Add(this.dumpGoals);
            this.variableDumperBox.Controls.Add(this.includeLocalScopes);
            this.variableDumperBox.Controls.Add(this.includeUnnamedDbs);
            this.variableDumperBox.Controls.Add(this.dumpDatabases);
            this.variableDumperBox.Controls.Add(this.includeDeleted);
            this.variableDumperBox.Controls.Add(this.dumpItemVars);
            this.variableDumperBox.Controls.Add(this.dumpCharacterVars);
            this.variableDumperBox.Controls.Add(this.dumpVariablesBtn);
            this.variableDumperBox.Controls.Add(this.dumpGlobalVars);
            this.variableDumperBox.Location = new System.Drawing.Point(13, 68);
            this.variableDumperBox.Name = "variableDumperBox";
            this.variableDumperBox.Size = new System.Drawing.Size(824, 149);
            this.variableDumperBox.TabIndex = 78;
            this.variableDumperBox.TabStop = false;
            this.variableDumperBox.Text = "Dump Settings";
            // 
            // includeLocalScopes
            // 
            this.includeLocalScopes.AutoSize = true;
            this.includeLocalScopes.Location = new System.Drawing.Point(16, 117);
            this.includeLocalScopes.Name = "includeLocalScopes";
            this.includeLocalScopes.Size = new System.Drawing.Size(123, 17);
            this.includeLocalScopes.TabIndex = 83;
            this.includeLocalScopes.Text = "Include local scopes";
            this.includeLocalScopes.UseVisualStyleBackColor = true;
            // 
            // includeUnnamedDbs
            // 
            this.includeUnnamedDbs.AutoSize = true;
            this.includeUnnamedDbs.Location = new System.Drawing.Point(235, 71);
            this.includeUnnamedDbs.Name = "includeUnnamedDbs";
            this.includeUnnamedDbs.Size = new System.Drawing.Size(173, 17);
            this.includeUnnamedDbs.TabIndex = 82;
            this.includeUnnamedDbs.Text = "Include intermediate databases";
            this.includeUnnamedDbs.UseVisualStyleBackColor = true;
            // 
            // dumpDatabases
            // 
            this.dumpDatabases.AutoSize = true;
            this.dumpDatabases.Checked = true;
            this.dumpDatabases.CheckState = System.Windows.Forms.CheckState.Checked;
            this.dumpDatabases.Location = new System.Drawing.Point(235, 48);
            this.dumpDatabases.Name = "dumpDatabases";
            this.dumpDatabases.Size = new System.Drawing.Size(106, 17);
            this.dumpDatabases.TabIndex = 81;
            this.dumpDatabases.Text = "Dump databases";
            this.dumpDatabases.UseVisualStyleBackColor = true;
            // 
            // includeDeleted
            // 
            this.includeDeleted.AutoSize = true;
            this.includeDeleted.Location = new System.Drawing.Point(16, 94);
            this.includeDeleted.Name = "includeDeleted";
            this.includeDeleted.Size = new System.Drawing.Size(133, 17);
            this.includeDeleted.TabIndex = 80;
            this.includeDeleted.Text = "Include deleted entries";
            this.includeDeleted.UseVisualStyleBackColor = true;
            // 
            // dumpItemVars
            // 
            this.dumpItemVars.AutoSize = true;
            this.dumpItemVars.Checked = true;
            this.dumpItemVars.CheckState = System.Windows.Forms.CheckState.Checked;
            this.dumpItemVars.Location = new System.Drawing.Point(16, 71);
            this.dumpItemVars.Name = "dumpItemVars";
            this.dumpItemVars.Size = new System.Drawing.Size(124, 17);
            this.dumpItemVars.TabIndex = 79;
            this.dumpItemVars.Text = "Dump item variables ";
            this.dumpItemVars.UseVisualStyleBackColor = true;
            // 
            // dumpCharacterVars
            // 
            this.dumpCharacterVars.AutoSize = true;
            this.dumpCharacterVars.Checked = true;
            this.dumpCharacterVars.CheckState = System.Windows.Forms.CheckState.Checked;
            this.dumpCharacterVars.Location = new System.Drawing.Point(16, 48);
            this.dumpCharacterVars.Name = "dumpCharacterVars";
            this.dumpCharacterVars.Size = new System.Drawing.Size(150, 17);
            this.dumpCharacterVars.TabIndex = 78;
            this.dumpCharacterVars.Text = "Dump character variables ";
            this.dumpCharacterVars.UseVisualStyleBackColor = true;
            // 
            // lblProgressStatus
            // 
            this.lblProgressStatus.AutoSize = true;
            this.lblProgressStatus.Location = new System.Drawing.Point(77, 231);
            this.lblProgressStatus.Name = "lblProgressStatus";
            this.lblProgressStatus.Size = new System.Drawing.Size(0, 13);
            this.lblProgressStatus.TabIndex = 81;
            // 
            // dumpProgressBar
            // 
            this.dumpProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dumpProgressBar.Location = new System.Drawing.Point(13, 247);
            this.dumpProgressBar.Name = "dumpProgressBar";
            this.dumpProgressBar.Size = new System.Drawing.Size(824, 23);
            this.dumpProgressBar.TabIndex = 79;
            // 
            // lblProgressText
            // 
            this.lblProgressText.AutoSize = true;
            this.lblProgressText.Location = new System.Drawing.Point(10, 231);
            this.lblProgressText.Name = "lblProgressText";
            this.lblProgressText.Size = new System.Drawing.Size(51, 13);
            this.lblProgressText.TabIndex = 80;
            this.lblProgressText.Text = "Progress:";
            // 
            // dumpGoals
            // 
            this.dumpGoals.AutoSize = true;
            this.dumpGoals.Checked = true;
            this.dumpGoals.CheckState = System.Windows.Forms.CheckState.Checked;
            this.dumpGoals.Location = new System.Drawing.Point(235, 25);
            this.dumpGoals.Name = "dumpGoals";
            this.dumpGoals.Size = new System.Drawing.Size(82, 17);
            this.dumpGoals.TabIndex = 84;
            this.dumpGoals.Text = "Dump goals";
            this.dumpGoals.UseVisualStyleBackColor = true;
            // 
            // extractPackage
            // 
            this.extractPackage.AutoSize = true;
            this.extractPackage.Checked = true;
            this.extractPackage.CheckState = System.Windows.Forms.CheckState.Checked;
            this.extractPackage.Location = new System.Drawing.Point(443, 25);
            this.extractPackage.Name = "extractPackage";
            this.extractPackage.Size = new System.Drawing.Size(111, 17);
            this.extractPackage.TabIndex = 87;
            this.extractPackage.Text = "Extract savegame";
            this.extractPackage.UseVisualStyleBackColor = true;
            // 
            // exportModList
            // 
            this.exportModList.AutoSize = true;
            this.exportModList.Checked = true;
            this.exportModList.CheckState = System.Windows.Forms.CheckState.Checked;
            this.exportModList.Location = new System.Drawing.Point(443, 71);
            this.exportModList.Name = "exportModList";
            this.exportModList.Size = new System.Drawing.Size(94, 17);
            this.exportModList.TabIndex = 86;
            this.exportModList.Text = "Export mod list";
            this.exportModList.UseVisualStyleBackColor = true;
            // 
            // convertLsf
            // 
            this.convertLsf.AutoSize = true;
            this.convertLsf.Checked = true;
            this.convertLsf.CheckState = System.Windows.Forms.CheckState.Checked;
            this.convertLsf.Location = new System.Drawing.Point(443, 48);
            this.convertLsf.Name = "convertLsf";
            this.convertLsf.Size = new System.Drawing.Size(125, 17);
            this.convertLsf.TabIndex = 85;
            this.convertLsf.Text = "Convert LSFs to LSX";
            this.convertLsf.UseVisualStyleBackColor = true;
            // 
            // DebugPane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblProgressStatus);
            this.Controls.Add(this.dumpProgressBar);
            this.Controls.Add(this.lblProgressText);
            this.Controls.Add(this.variableDumperBox);
            this.Controls.Add(this.saveFilePath);
            this.Controls.Add(this.savePathLbl);
            this.Controls.Add(this.saveFileBrowseBtn);
            this.Name = "DebugPane";
            this.Size = new System.Drawing.Size(856, 475);
            this.variableDumperBox.ResumeLayout(false);
            this.variableDumperBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button dumpVariablesBtn;
        private System.Windows.Forms.TextBox saveFilePath;
        private System.Windows.Forms.Label savePathLbl;
        private System.Windows.Forms.Button saveFileBrowseBtn;
        private System.Windows.Forms.OpenFileDialog savePathDlg;
        private System.Windows.Forms.CheckBox dumpGlobalVars;
        private System.Windows.Forms.GroupBox variableDumperBox;
        private System.Windows.Forms.CheckBox includeUnnamedDbs;
        private System.Windows.Forms.CheckBox dumpDatabases;
        private System.Windows.Forms.CheckBox includeDeleted;
        private System.Windows.Forms.CheckBox dumpItemVars;
        private System.Windows.Forms.CheckBox dumpCharacterVars;
        private System.Windows.Forms.CheckBox includeLocalScopes;
        private System.Windows.Forms.Label lblProgressStatus;
        private System.Windows.Forms.ProgressBar dumpProgressBar;
        private System.Windows.Forms.Label lblProgressText;
        private System.Windows.Forms.CheckBox extractPackage;
        private System.Windows.Forms.CheckBox exportModList;
        private System.Windows.Forms.CheckBox convertLsf;
        private System.Windows.Forms.CheckBox dumpGoals;
    }
}
