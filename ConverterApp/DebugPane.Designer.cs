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
            this.loadSaveBtn = new System.Windows.Forms.Button();
            this.saveFilePath = new System.Windows.Forms.TextBox();
            this.savePathLbl = new System.Windows.Forms.Label();
            this.saveFileBrowseBtn = new System.Windows.Forms.Button();
            this.savePathDlg = new System.Windows.Forms.OpenFileDialog();
            this.dumpGlobalVars = new System.Windows.Forms.CheckBox();
            this.variableDumperBox = new System.Windows.Forms.GroupBox();
            this.dumpCharacterVars = new System.Windows.Forms.CheckBox();
            this.dumpItemVars = new System.Windows.Forms.CheckBox();
            this.includeDeleted = new System.Windows.Forms.CheckBox();
            this.dumpDatabases = new System.Windows.Forms.CheckBox();
            this.includeUnnamedDbs = new System.Windows.Forms.CheckBox();
            this.includeLocalScopes = new System.Windows.Forms.CheckBox();
            this.variableDumperBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // dumpVariablesBtn
            // 
            this.dumpVariablesBtn.Enabled = false;
            this.dumpVariablesBtn.Location = new System.Drawing.Point(235, 88);
            this.dumpVariablesBtn.Name = "dumpVariablesBtn";
            this.dumpVariablesBtn.Size = new System.Drawing.Size(174, 23);
            this.dumpVariablesBtn.TabIndex = 72;
            this.dumpVariablesBtn.Text = "Dump variables and databases";
            this.dumpVariablesBtn.UseVisualStyleBackColor = true;
            this.dumpVariablesBtn.Click += new System.EventHandler(this.dumpVariablesBtn_Click);
            // 
            // loadSaveBtn
            // 
            this.loadSaveBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.loadSaveBtn.Location = new System.Drawing.Point(716, 28);
            this.loadSaveBtn.Name = "loadSaveBtn";
            this.loadSaveBtn.Size = new System.Drawing.Size(121, 23);
            this.loadSaveBtn.TabIndex = 76;
            this.loadSaveBtn.Text = "Load";
            this.loadSaveBtn.UseVisualStyleBackColor = true;
            this.loadSaveBtn.Click += new System.EventHandler(this.loadSaveBtn_Click);
            // 
            // saveFilePath
            // 
            this.saveFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.saveFilePath.Location = new System.Drawing.Point(13, 29);
            this.saveFilePath.Name = "saveFilePath";
            this.saveFilePath.Size = new System.Drawing.Size(660, 20);
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
            this.saveFileBrowseBtn.Location = new System.Drawing.Point(671, 28);
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
            this.variableDumperBox.Size = new System.Drawing.Size(434, 157);
            this.variableDumperBox.TabIndex = 78;
            this.variableDumperBox.TabStop = false;
            this.variableDumperBox.Text = "Story Dump";
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
            // dumpDatabases
            // 
            this.dumpDatabases.AutoSize = true;
            this.dumpDatabases.Checked = true;
            this.dumpDatabases.CheckState = System.Windows.Forms.CheckState.Checked;
            this.dumpDatabases.Location = new System.Drawing.Point(235, 19);
            this.dumpDatabases.Name = "dumpDatabases";
            this.dumpDatabases.Size = new System.Drawing.Size(106, 17);
            this.dumpDatabases.TabIndex = 81;
            this.dumpDatabases.Text = "Dump databases";
            this.dumpDatabases.UseVisualStyleBackColor = true;
            // 
            // includeUnnamedDbs
            // 
            this.includeUnnamedDbs.AutoSize = true;
            this.includeUnnamedDbs.Location = new System.Drawing.Point(235, 42);
            this.includeUnnamedDbs.Name = "includeUnnamedDbs";
            this.includeUnnamedDbs.Size = new System.Drawing.Size(173, 17);
            this.includeUnnamedDbs.TabIndex = 82;
            this.includeUnnamedDbs.Text = "Include intermediate databases";
            this.includeUnnamedDbs.UseVisualStyleBackColor = true;
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
            // DebugPane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.variableDumperBox);
            this.Controls.Add(this.loadSaveBtn);
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
        private System.Windows.Forms.Button loadSaveBtn;
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
    }
}
