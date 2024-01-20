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
            this.packageProgress = new System.Windows.Forms.ProgressBar();
            this.label5 = new System.Windows.Forms.Label();
            this.extractPathDlg = new System.Windows.Forms.FolderBrowserDialog();
            this.extractPackageFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.packageProgressLabel = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.extractPackageBtn = new System.Windows.Forms.Button();
            this.extractPathBrowseBtn = new System.Windows.Forms.Button();
            this.extractPackageBrowseBtn = new System.Windows.Forms.Button();
            this.extractPackagePath = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.extractionPath = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.solid = new System.Windows.Forms.CheckBox();
            this.preloadIntoCache = new System.Windows.Forms.CheckBox();
            this.allowMemoryMapping = new System.Windows.Forms.CheckBox();
            this.packagePriority = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.createPackagePathBrowseBtn = new System.Windows.Forms.Button();
            this.createPackagePath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.packageVersion = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.compressionMethod = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.createPackageBtn = new System.Windows.Forms.Button();
            this.createSrcPathBrowseBtn = new System.Windows.Forms.Button();
            this.createSrcPath = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.createPackageFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.createPackagePathDlg = new System.Windows.Forms.FolderBrowserDialog();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.packagePriority)).BeginInit();
            this.SuspendLayout();
            // 
            // packageProgress
            // 
            this.packageProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.packageProgress.Location = new System.Drawing.Point(8, 428);
            this.packageProgress.Margin = new System.Windows.Forms.Padding(4);
            this.packageProgress.Name = "packageProgress";
            this.packageProgress.Size = new System.Drawing.Size(1168, 28);
            this.packageProgress.TabIndex = 57;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(4, 409);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 16);
            this.label5.TabIndex = 58;
            this.label5.Text = "Progress:";
            // 
            // extractPackageFileDlg
            // 
            this.extractPackageFileDlg.Filter = "LS package / savegame files|*.pak;*.lsv";
            // 
            // packageProgressLabel
            // 
            this.packageProgressLabel.AutoSize = true;
            this.packageProgressLabel.Location = new System.Drawing.Point(93, 411);
            this.packageProgressLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.packageProgressLabel.Name = "packageProgressLabel";
            this.packageProgressLabel.Size = new System.Drawing.Size(0, 16);
            this.packageProgressLabel.TabIndex = 64;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.extractPackageBtn);
            this.groupBox1.Controls.Add(this.extractPathBrowseBtn);
            this.groupBox1.Controls.Add(this.extractPackageBrowseBtn);
            this.groupBox1.Controls.Add(this.extractPackagePath);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.extractionPath);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Location = new System.Drawing.Point(9, 16);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox1.Size = new System.Drawing.Size(1167, 170);
            this.groupBox1.TabIndex = 65;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Extract Package";
            // 
            // extractPackageBtn
            // 
            this.extractPackageBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.extractPackageBtn.Location = new System.Drawing.Point(945, 134);
            this.extractPackageBtn.Margin = new System.Windows.Forms.Padding(4);
            this.extractPackageBtn.Name = "extractPackageBtn";
            this.extractPackageBtn.Size = new System.Drawing.Size(213, 28);
            this.extractPackageBtn.TabIndex = 62;
            this.extractPackageBtn.Text = "Extract Package";
            this.extractPackageBtn.UseVisualStyleBackColor = true;
            this.extractPackageBtn.Click += new System.EventHandler(this.extractPackageBtn_Click);
            // 
            // extractPathBrowseBtn
            // 
            this.extractPathBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.extractPathBrowseBtn.Location = new System.Drawing.Point(1105, 94);
            this.extractPathBrowseBtn.Margin = new System.Windows.Forms.Padding(4);
            this.extractPathBrowseBtn.Name = "extractPathBrowseBtn";
            this.extractPathBrowseBtn.Size = new System.Drawing.Size(55, 28);
            this.extractPathBrowseBtn.TabIndex = 61;
            this.extractPathBrowseBtn.Text = "...";
            this.extractPathBrowseBtn.UseVisualStyleBackColor = true;
            this.extractPathBrowseBtn.Click += new System.EventHandler(this.extractPathBrowseBtn_Click);
            // 
            // extractPackageBrowseBtn
            // 
            this.extractPackageBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.extractPackageBrowseBtn.Location = new System.Drawing.Point(1105, 37);
            this.extractPackageBrowseBtn.Margin = new System.Windows.Forms.Padding(4);
            this.extractPackageBrowseBtn.Name = "extractPackageBrowseBtn";
            this.extractPackageBrowseBtn.Size = new System.Drawing.Size(55, 28);
            this.extractPackageBrowseBtn.TabIndex = 58;
            this.extractPackageBrowseBtn.Text = "...";
            this.extractPackageBrowseBtn.UseVisualStyleBackColor = true;
            this.extractPackageBrowseBtn.Click += new System.EventHandler(this.extractPackageBrowseBtn_Click);
            // 
            // extractPackagePath
            // 
            this.extractPackagePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.extractPackagePath.Location = new System.Drawing.Point(12, 39);
            this.extractPackagePath.Margin = new System.Windows.Forms.Padding(4);
            this.extractPackagePath.Name = "extractPackagePath";
            this.extractPackagePath.Size = new System.Drawing.Size(1093, 22);
            this.extractPackagePath.TabIndex = 56;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 20);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(94, 16);
            this.label4.TabIndex = 57;
            this.label4.Text = "Package path:";
            // 
            // extractionPath
            // 
            this.extractionPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.extractionPath.Location = new System.Drawing.Point(12, 96);
            this.extractionPath.Margin = new System.Windows.Forms.Padding(4);
            this.extractionPath.Name = "extractionPath";
            this.extractionPath.Size = new System.Drawing.Size(1093, 22);
            this.extractionPath.TabIndex = 59;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 76);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(106, 16);
            this.label3.TabIndex = 60;
            this.label3.Text = "Destination path:";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.solid);
            this.groupBox2.Controls.Add(this.preloadIntoCache);
            this.groupBox2.Controls.Add(this.allowMemoryMapping);
            this.groupBox2.Controls.Add(this.packagePriority);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.createPackagePathBrowseBtn);
            this.groupBox2.Controls.Add(this.createPackagePath);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.packageVersion);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.compressionMethod);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.createPackageBtn);
            this.groupBox2.Controls.Add(this.createSrcPathBrowseBtn);
            this.groupBox2.Controls.Add(this.createSrcPath);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Location = new System.Drawing.Point(9, 194);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox2.Size = new System.Drawing.Size(1167, 198);
            this.groupBox2.TabIndex = 66;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Create Package";
            // 
            // solid
            // 
            this.solid.AutoSize = true;
            this.solid.Location = new System.Drawing.Point(604, 137);
            this.solid.Margin = new System.Windows.Forms.Padding(4);
            this.solid.Name = "solid";
            this.solid.Size = new System.Drawing.Size(60, 20);
            this.solid.TabIndex = 76;
            this.solid.Text = "Solid";
            this.solid.UseVisualStyleBackColor = true;
            // 
            // preloadIntoCache
            // 
            this.preloadIntoCache.AutoSize = true;
            this.preloadIntoCache.Location = new System.Drawing.Point(789, 138);
            this.preloadIntoCache.Margin = new System.Windows.Forms.Padding(4);
            this.preloadIntoCache.Name = "preloadIntoCache";
            this.preloadIntoCache.Size = new System.Drawing.Size(141, 20);
            this.preloadIntoCache.TabIndex = 75;
            this.preloadIntoCache.Text = "Preload into cache";
            this.preloadIntoCache.UseVisualStyleBackColor = true;
            this.preloadIntoCache.Visible = false;
            // 
            // allowMemoryMapping
            // 
            this.allowMemoryMapping.AutoSize = true;
            this.allowMemoryMapping.Location = new System.Drawing.Point(604, 162);
            this.allowMemoryMapping.Margin = new System.Windows.Forms.Padding(4);
            this.allowMemoryMapping.Name = "allowMemoryMapping";
            this.allowMemoryMapping.Size = new System.Drawing.Size(169, 20);
            this.allowMemoryMapping.TabIndex = 74;
            this.allowMemoryMapping.Text = "Allow memory mapping";
            this.allowMemoryMapping.UseVisualStyleBackColor = true;
            this.allowMemoryMapping.Visible = false;
            // 
            // packagePriority
            // 
            this.packagePriority.Location = new System.Drawing.Point(513, 156);
            this.packagePriority.Margin = new System.Windows.Forms.Padding(4);
            this.packagePriority.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.packagePriority.Name = "packagePriority";
            this.packagePriority.Size = new System.Drawing.Size(77, 22);
            this.packagePriority.TabIndex = 73;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(509, 135);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(51, 16);
            this.label7.TabIndex = 72;
            this.label7.Text = "Priority:";
            // 
            // createPackagePathBrowseBtn
            // 
            this.createPackagePathBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.createPackagePathBrowseBtn.Location = new System.Drawing.Point(1104, 96);
            this.createPackagePathBrowseBtn.Margin = new System.Windows.Forms.Padding(4);
            this.createPackagePathBrowseBtn.Name = "createPackagePathBrowseBtn";
            this.createPackagePathBrowseBtn.Size = new System.Drawing.Size(55, 28);
            this.createPackagePathBrowseBtn.TabIndex = 71;
            this.createPackagePathBrowseBtn.Text = "...";
            this.createPackagePathBrowseBtn.UseVisualStyleBackColor = true;
            this.createPackagePathBrowseBtn.Click += new System.EventHandler(this.createPackagePathBrowseBtn_Click);
            // 
            // createPackagePath
            // 
            this.createPackagePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.createPackagePath.Location = new System.Drawing.Point(11, 98);
            this.createPackagePath.Margin = new System.Windows.Forms.Padding(4);
            this.createPackagePath.Name = "createPackagePath";
            this.createPackagePath.Size = new System.Drawing.Size(1093, 22);
            this.createPackagePath.TabIndex = 69;
            this.createPackagePath.TextChanged += new System.EventHandler(this.packagePath_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 79);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(94, 16);
            this.label1.TabIndex = 70;
            this.label1.Text = "Package path:";
            // 
            // packageVersion
            // 
            this.packageVersion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.packageVersion.FormattingEnabled = true;
            this.packageVersion.Items.AddRange(new object[] {
            "V18 (Baldur\'s Gate 3 Release)",
            "V13 (Divinity Original Sin: EE, Original Sin 2)",
            "V10 (Divinity Original Sin)",
            "V9 (Divinity Original Sin Classic)",
            "V7 (Divinity Original Sin Classic - Old)"});
            this.packageVersion.Location = new System.Drawing.Point(12, 156);
            this.packageVersion.Margin = new System.Windows.Forms.Padding(4);
            this.packageVersion.Name = "packageVersion";
            this.packageVersion.Size = new System.Drawing.Size(315, 24);
            this.packageVersion.TabIndex = 68;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(8, 135);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(56, 16);
            this.label8.TabIndex = 67;
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
            "LZ4 HC",
            "ZStd Fast",
            "ZStd Optimal",
            "ZStd Max"
            });
            this.compressionMethod.Location = new System.Drawing.Point(353, 156);
            this.compressionMethod.Margin = new System.Windows.Forms.Padding(4);
            this.compressionMethod.Name = "compressionMethod";
            this.compressionMethod.Size = new System.Drawing.Size(141, 24);
            this.compressionMethod.TabIndex = 66;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(349, 135);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(90, 16);
            this.label6.TabIndex = 65;
            this.label6.Text = "Compression:";
            // 
            // createPackageBtn
            // 
            this.createPackageBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.createPackageBtn.Location = new System.Drawing.Point(945, 162);
            this.createPackageBtn.Margin = new System.Windows.Forms.Padding(4);
            this.createPackageBtn.Name = "createPackageBtn";
            this.createPackageBtn.Size = new System.Drawing.Size(213, 28);
            this.createPackageBtn.TabIndex = 64;
            this.createPackageBtn.Text = "Create Package";
            this.createPackageBtn.UseVisualStyleBackColor = true;
            this.createPackageBtn.Click += new System.EventHandler(this.createPackageBtn_Click);
            // 
            // createSrcPathBrowseBtn
            // 
            this.createSrcPathBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.createSrcPathBrowseBtn.Location = new System.Drawing.Point(1105, 43);
            this.createSrcPathBrowseBtn.Margin = new System.Windows.Forms.Padding(4);
            this.createSrcPathBrowseBtn.Name = "createSrcPathBrowseBtn";
            this.createSrcPathBrowseBtn.Size = new System.Drawing.Size(55, 28);
            this.createSrcPathBrowseBtn.TabIndex = 61;
            this.createSrcPathBrowseBtn.Text = "...";
            this.createSrcPathBrowseBtn.UseVisualStyleBackColor = true;
            this.createSrcPathBrowseBtn.Click += new System.EventHandler(this.createSrcPathBrowseBtn_Click);
            // 
            // createSrcPath
            // 
            this.createSrcPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.createSrcPath.Location = new System.Drawing.Point(12, 46);
            this.createSrcPath.Margin = new System.Windows.Forms.Padding(4);
            this.createSrcPath.Name = "createSrcPath";
            this.createSrcPath.Size = new System.Drawing.Size(1093, 22);
            this.createSrcPath.TabIndex = 59;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 26);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(82, 16);
            this.label2.TabIndex = 60;
            this.label2.Text = "Source path:";
            // 
            // createPackageFileDlg
            // 
            this.createPackageFileDlg.CheckFileExists = false;
            this.createPackageFileDlg.Filter = "LS package / savegame files|*.pak;*.lsv";
            // 
            // PackagePane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.packageProgressLabel);
            this.Controls.Add(this.packageProgress);
            this.Controls.Add(this.label5);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "PackagePane";
            this.Size = new System.Drawing.Size(1188, 480);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.packagePriority)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ProgressBar packageProgress;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.FolderBrowserDialog extractPathDlg;
        private System.Windows.Forms.OpenFileDialog extractPackageFileDlg;
        private System.Windows.Forms.Label packageProgressLabel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button extractPackageBtn;
        private System.Windows.Forms.Button extractPathBrowseBtn;
        private System.Windows.Forms.Button extractPackageBrowseBtn;
        private System.Windows.Forms.TextBox extractPackagePath;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox extractionPath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button createPackagePathBrowseBtn;
        private System.Windows.Forms.TextBox createPackagePath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox packageVersion;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox compressionMethod;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button createPackageBtn;
        private System.Windows.Forms.Button createSrcPathBrowseBtn;
        private System.Windows.Forms.TextBox createSrcPath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.OpenFileDialog createPackageFileDlg;
        private System.Windows.Forms.FolderBrowserDialog createPackagePathDlg;
        private System.Windows.Forms.CheckBox preloadIntoCache;
        private System.Windows.Forms.CheckBox allowMemoryMapping;
        private System.Windows.Forms.NumericUpDown packagePriority;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox solid;
    }
}
