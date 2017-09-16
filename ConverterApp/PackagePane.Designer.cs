namespace ConverterApp
{
    partial class PackagePane
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
            this.packageVersion = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.compressionMethod = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.exportPathBrowseBtn = new System.Windows.Forms.Button();
            this.packageBrowseBtn = new System.Windows.Forms.Button();
            this.createPackageBtn = new System.Windows.Forms.Button();
            this.packageProgress = new System.Windows.Forms.ProgressBar();
            this.label5 = new System.Windows.Forms.Label();
            this.packagePath = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.extractPackageBtn = new System.Windows.Forms.Button();
            this.extractionPath = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.exportPathDlg = new System.Windows.Forms.FolderBrowserDialog();
            this.packageFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.packageProgressLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // packageVersion
            // 
            this.packageVersion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.packageVersion.FormattingEnabled = true;
            this.packageVersion.Items.AddRange(new object[] {
            "V13 (Divinity Original Sin: EE, Original Sin 2)",
            "V10 (Divinity Original Sin)",
            "V9 (Divinity Original Sin Old)",
            "V7 (Divinity Original Sin Old)"});
            this.packageVersion.Location = new System.Drawing.Point(7, 117);
            this.packageVersion.Name = "packageVersion";
            this.packageVersion.Size = new System.Drawing.Size(237, 21);
            this.packageVersion.TabIndex = 63;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(4, 100);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(45, 13);
            this.label8.TabIndex = 62;
            this.label8.Text = "Version:";
            // 
            // compressionMethod
            // 
            this.compressionMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.compressionMethod.FormattingEnabled = true;
            this.compressionMethod.Items.AddRange(new object[] {
            "No compression",
            "Zlib Fast",
            "Zlib Optimal",
            "LZ4",
            "LZ4 HC"});
            this.compressionMethod.Location = new System.Drawing.Point(263, 117);
            this.compressionMethod.Name = "compressionMethod";
            this.compressionMethod.Size = new System.Drawing.Size(187, 21);
            this.compressionMethod.TabIndex = 61;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(260, 100);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(70, 13);
            this.label6.TabIndex = 60;
            this.label6.Text = "Compression:";
            // 
            // exportPathBrowseBtn
            // 
            this.exportPathBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.exportPathBrowseBtn.Location = new System.Drawing.Point(654, 66);
            this.exportPathBrowseBtn.Name = "exportPathBrowseBtn";
            this.exportPathBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.exportPathBrowseBtn.TabIndex = 55;
            this.exportPathBrowseBtn.Text = "...";
            this.exportPathBrowseBtn.UseVisualStyleBackColor = true;
            this.exportPathBrowseBtn.Click += new System.EventHandler(this.exportPathBrowseBtn_Click);
            // 
            // packageBrowseBtn
            // 
            this.packageBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.packageBrowseBtn.Location = new System.Drawing.Point(654, 20);
            this.packageBrowseBtn.Name = "packageBrowseBtn";
            this.packageBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.packageBrowseBtn.TabIndex = 52;
            this.packageBrowseBtn.Text = "...";
            this.packageBrowseBtn.UseVisualStyleBackColor = true;
            this.packageBrowseBtn.Click += new System.EventHandler(this.packageBrowseBtn_Click);
            // 
            // createPackageBtn
            // 
            this.createPackageBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.createPackageBtn.Location = new System.Drawing.Point(722, 22);
            this.createPackageBtn.Name = "createPackageBtn";
            this.createPackageBtn.Size = new System.Drawing.Size(160, 23);
            this.createPackageBtn.TabIndex = 59;
            this.createPackageBtn.Text = "Create Package";
            this.createPackageBtn.UseVisualStyleBackColor = true;
            this.createPackageBtn.Click += new System.EventHandler(this.createPackageBtn_Click);
            // 
            // packageProgress
            // 
            this.packageProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.packageProgress.Location = new System.Drawing.Point(6, 163);
            this.packageProgress.Name = "packageProgress";
            this.packageProgress.Size = new System.Drawing.Size(876, 23);
            this.packageProgress.TabIndex = 57;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 147);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(51, 13);
            this.label5.TabIndex = 58;
            this.label5.Text = "Progress:";
            // 
            // packagePath
            // 
            this.packagePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.packagePath.Location = new System.Drawing.Point(6, 22);
            this.packagePath.Name = "packagePath";
            this.packagePath.Size = new System.Drawing.Size(650, 20);
            this.packagePath.TabIndex = 50;
            this.packagePath.TextChanged += new System.EventHandler(this.packagePath_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 6);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(77, 13);
            this.label4.TabIndex = 51;
            this.label4.Text = "Package path:";
            // 
            // extractPackageBtn
            // 
            this.extractPackageBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.extractPackageBtn.Location = new System.Drawing.Point(722, 65);
            this.extractPackageBtn.Name = "extractPackageBtn";
            this.extractPackageBtn.Size = new System.Drawing.Size(160, 23);
            this.extractPackageBtn.TabIndex = 56;
            this.extractPackageBtn.Text = "Extract Package";
            this.extractPackageBtn.UseVisualStyleBackColor = true;
            this.extractPackageBtn.Click += new System.EventHandler(this.extractPackageBtn_Click);
            // 
            // extractionPath
            // 
            this.extractionPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.extractionPath.Location = new System.Drawing.Point(6, 68);
            this.extractionPath.Name = "extractionPath";
            this.extractionPath.Size = new System.Drawing.Size(650, 20);
            this.extractionPath.TabIndex = 53;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 52);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(344, 13);
            this.label3.TabIndex = 54;
            this.label3.Text = "Destination (when extracting) / source path (when creating a package):";
            // 
            // packageFileDlg
            // 
            this.packageFileDlg.CheckFileExists = false;
            this.packageFileDlg.Filter = "LS package / savegame files|*.pak;*.lsv";
            // 
            // packageProgressLabel
            // 
            this.packageProgressLabel.AutoSize = true;
            this.packageProgressLabel.Location = new System.Drawing.Point(70, 147);
            this.packageProgressLabel.Name = "packageProgressLabel";
            this.packageProgressLabel.Size = new System.Drawing.Size(0, 13);
            this.packageProgressLabel.TabIndex = 64;
            // 
            // PackagePane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.packageProgressLabel);
            this.Controls.Add(this.packageVersion);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.compressionMethod);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.exportPathBrowseBtn);
            this.Controls.Add(this.packageBrowseBtn);
            this.Controls.Add(this.createPackageBtn);
            this.Controls.Add(this.packageProgress);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.packagePath);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.extractPackageBtn);
            this.Controls.Add(this.extractionPath);
            this.Controls.Add(this.label3);
            this.Name = "PackagePane";
            this.Size = new System.Drawing.Size(891, 200);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox packageVersion;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox compressionMethod;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button exportPathBrowseBtn;
        private System.Windows.Forms.Button packageBrowseBtn;
        private System.Windows.Forms.Button createPackageBtn;
        private System.Windows.Forms.ProgressBar packageProgress;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox packagePath;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button extractPackageBtn;
        private System.Windows.Forms.TextBox extractionPath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.FolderBrowserDialog exportPathDlg;
        private System.Windows.Forms.OpenFileDialog packageFileDlg;
        private System.Windows.Forms.Label packageProgressLabel;
    }
}
