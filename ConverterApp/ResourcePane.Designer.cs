namespace ConverterApp
{
    partial class ResourcePane
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
            this.resourceConversionProgress = new System.Windows.Forms.ProgressBar();
            this.label17 = new System.Windows.Forms.Label();
            this.resourceOutputFormatCb = new System.Windows.Forms.ComboBox();
            this.label16 = new System.Windows.Forms.Label();
            this.resourceInputFormatCb = new System.Windows.Forms.ComboBox();
            this.label15 = new System.Windows.Forms.Label();
            this.resourceBulkConvertBtn = new System.Windows.Forms.Button();
            this.resourceOutputPathBrowseBtn = new System.Windows.Forms.Button();
            this.resourceInputPathBrowseBtn = new System.Windows.Forms.Button();
            this.resourceInputDir = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.resourceOutputDir = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.resourceConvertBtn = new System.Windows.Forms.Button();
            this.resourceOutputBrowseBtn = new System.Windows.Forms.Button();
            this.resourceInputBrowseBtn = new System.Windows.Forms.Button();
            this.resourceInputPath = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.resourceOutputPath = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.resourceProgressLabel = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.resourceInputFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.resourceOutputFileDlg = new System.Windows.Forms.SaveFileDialog();
            this.resourceInputPathDlg = new System.Windows.Forms.FolderBrowserDialog();
            this.resourceOutputPathDlg = new System.Windows.Forms.FolderBrowserDialog();
            this.grpGlobalSettings = new System.Windows.Forms.GroupBox();
            this.legacyGuids = new System.Windows.Forms.CheckBox();
            this.lblGuidSerializationInfo = new System.Windows.Forms.Label();
            this.groupBox5.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.grpGlobalSettings.SuspendLayout();
            this.SuspendLayout();
            // 
            // resourceConversionProgress
            // 
            this.resourceConversionProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceConversionProgress.Location = new System.Drawing.Point(16, 215);
            this.resourceConversionProgress.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.resourceConversionProgress.Name = "resourceConversionProgress";
            this.resourceConversionProgress.Size = new System.Drawing.Size(1163, 28);
            this.resourceConversionProgress.TabIndex = 65;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(12, 196);
            this.label17.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(65, 16);
            this.label17.TabIndex = 66;
            this.label17.Text = "Progress:";
            // 
            // resourceOutputFormatCb
            // 
            this.resourceOutputFormatCb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.resourceOutputFormatCb.FormattingEnabled = true;
            this.resourceOutputFormatCb.Items.AddRange(new object[] {
            "LSX (XML) file",
            "LSB (binary) file",
            "LSF (binary) file",
            "LSJ (JSON) file"});
            this.resourceOutputFormatCb.Location = new System.Drawing.Point(241, 161);
            this.resourceOutputFormatCb.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.resourceOutputFormatCb.Name = "resourceOutputFormatCb";
            this.resourceOutputFormatCb.Size = new System.Drawing.Size(200, 24);
            this.resourceOutputFormatCb.TabIndex = 64;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(237, 140);
            this.label16.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(88, 16);
            this.label16.TabIndex = 63;
            this.label16.Text = "Output format:";
            // 
            // resourceInputFormatCb
            // 
            this.resourceInputFormatCb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.resourceInputFormatCb.FormattingEnabled = true;
            this.resourceInputFormatCb.Items.AddRange(new object[] {
            "LSX (XML) file",
            "LSB (binary) file",
            "LSF (binary) file",
            "LSJ (JSON) file"});
            this.resourceInputFormatCb.Location = new System.Drawing.Point(16, 161);
            this.resourceInputFormatCb.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.resourceInputFormatCb.Name = "resourceInputFormatCb";
            this.resourceInputFormatCb.Size = new System.Drawing.Size(200, 24);
            this.resourceInputFormatCb.TabIndex = 62;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(12, 140);
            this.label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(78, 16);
            this.label15.TabIndex = 61;
            this.label15.Text = "Input format:";
            // 
            // resourceBulkConvertBtn
            // 
            this.resourceBulkConvertBtn.Location = new System.Drawing.Point(464, 161);
            this.resourceBulkConvertBtn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.resourceBulkConvertBtn.Name = "resourceBulkConvertBtn";
            this.resourceBulkConvertBtn.Size = new System.Drawing.Size(201, 28);
            this.resourceBulkConvertBtn.TabIndex = 60;
            this.resourceBulkConvertBtn.Text = "Convert";
            this.resourceBulkConvertBtn.UseVisualStyleBackColor = true;
            this.resourceBulkConvertBtn.Click += new System.EventHandler(this.resourceBulkConvertBtn_Click);
            // 
            // resourceOutputPathBrowseBtn
            // 
            this.resourceOutputPathBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceOutputPathBrowseBtn.Location = new System.Drawing.Point(1125, 106);
            this.resourceOutputPathBrowseBtn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.resourceOutputPathBrowseBtn.Name = "resourceOutputPathBrowseBtn";
            this.resourceOutputPathBrowseBtn.Size = new System.Drawing.Size(55, 27);
            this.resourceOutputPathBrowseBtn.TabIndex = 59;
            this.resourceOutputPathBrowseBtn.Text = "...";
            this.resourceOutputPathBrowseBtn.UseVisualStyleBackColor = true;
            this.resourceOutputPathBrowseBtn.Click += new System.EventHandler(this.resourceOutputPathBrowseBtn_Click);
            // 
            // resourceInputPathBrowseBtn
            // 
            this.resourceInputPathBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceInputPathBrowseBtn.Location = new System.Drawing.Point(1124, 47);
            this.resourceInputPathBrowseBtn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.resourceInputPathBrowseBtn.Name = "resourceInputPathBrowseBtn";
            this.resourceInputPathBrowseBtn.Size = new System.Drawing.Size(55, 27);
            this.resourceInputPathBrowseBtn.TabIndex = 56;
            this.resourceInputPathBrowseBtn.Text = "...";
            this.resourceInputPathBrowseBtn.UseVisualStyleBackColor = true;
            this.resourceInputPathBrowseBtn.Click += new System.EventHandler(this.resourceInputPathBrowseBtn_Click);
            // 
            // resourceInputDir
            // 
            this.resourceInputDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceInputDir.Location = new System.Drawing.Point(15, 48);
            this.resourceInputDir.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.resourceInputDir.Name = "resourceInputDir";
            this.resourceInputDir.Size = new System.Drawing.Size(1111, 22);
            this.resourceInputDir.TabIndex = 54;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(11, 28);
            this.label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(93, 16);
            this.label13.TabIndex = 55;
            this.label13.Text = "Input directory:";
            // 
            // resourceOutputDir
            // 
            this.resourceOutputDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceOutputDir.Location = new System.Drawing.Point(15, 107);
            this.resourceOutputDir.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.resourceOutputDir.Name = "resourceOutputDir";
            this.resourceOutputDir.Size = new System.Drawing.Size(1111, 22);
            this.resourceOutputDir.TabIndex = 57;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(11, 87);
            this.label14.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(103, 16);
            this.label14.TabIndex = 58;
            this.label14.Text = "Output directory:";
            // 
            // resourceConvertBtn
            // 
            this.resourceConvertBtn.Location = new System.Drawing.Point(16, 143);
            this.resourceConvertBtn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.resourceConvertBtn.Name = "resourceConvertBtn";
            this.resourceConvertBtn.Size = new System.Drawing.Size(201, 28);
            this.resourceConvertBtn.TabIndex = 60;
            this.resourceConvertBtn.Text = "Convert";
            this.resourceConvertBtn.UseVisualStyleBackColor = true;
            this.resourceConvertBtn.Click += new System.EventHandler(this.resourceConvertBtn_Click);
            // 
            // resourceOutputBrowseBtn
            // 
            this.resourceOutputBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceOutputBrowseBtn.Location = new System.Drawing.Point(1125, 106);
            this.resourceOutputBrowseBtn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.resourceOutputBrowseBtn.Name = "resourceOutputBrowseBtn";
            this.resourceOutputBrowseBtn.Size = new System.Drawing.Size(55, 27);
            this.resourceOutputBrowseBtn.TabIndex = 59;
            this.resourceOutputBrowseBtn.Text = "...";
            this.resourceOutputBrowseBtn.UseVisualStyleBackColor = true;
            this.resourceOutputBrowseBtn.Click += new System.EventHandler(this.resourceOutputBrowseBtn_Click);
            // 
            // resourceInputBrowseBtn
            // 
            this.resourceInputBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceInputBrowseBtn.Location = new System.Drawing.Point(1125, 47);
            this.resourceInputBrowseBtn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.resourceInputBrowseBtn.Name = "resourceInputBrowseBtn";
            this.resourceInputBrowseBtn.Size = new System.Drawing.Size(55, 27);
            this.resourceInputBrowseBtn.TabIndex = 56;
            this.resourceInputBrowseBtn.Text = "...";
            this.resourceInputBrowseBtn.UseVisualStyleBackColor = true;
            this.resourceInputBrowseBtn.Click += new System.EventHandler(this.resourceInputBrowseBtn_Click);
            // 
            // resourceInputPath
            // 
            this.resourceInputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceInputPath.Location = new System.Drawing.Point(15, 48);
            this.resourceInputPath.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.resourceInputPath.Name = "resourceInputPath";
            this.resourceInputPath.Size = new System.Drawing.Size(1111, 22);
            this.resourceInputPath.TabIndex = 54;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(11, 28);
            this.label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(87, 16);
            this.label11.TabIndex = 55;
            this.label11.Text = "Input file path:";
            // 
            // resourceOutputPath
            // 
            this.resourceOutputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceOutputPath.Location = new System.Drawing.Point(15, 107);
            this.resourceOutputPath.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.resourceOutputPath.Name = "resourceOutputPath";
            this.resourceOutputPath.Size = new System.Drawing.Size(1111, 22);
            this.resourceOutputPath.TabIndex = 57;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(11, 87);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(97, 16);
            this.label12.TabIndex = 58;
            this.label12.Text = "Output file path:";
            // 
            // groupBox5
            // 
            this.groupBox5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox5.Controls.Add(this.resourceProgressLabel);
            this.groupBox5.Controls.Add(this.resourceConversionProgress);
            this.groupBox5.Controls.Add(this.label17);
            this.groupBox5.Controls.Add(this.resourceOutputFormatCb);
            this.groupBox5.Controls.Add(this.label16);
            this.groupBox5.Controls.Add(this.resourceInputFormatCb);
            this.groupBox5.Controls.Add(this.label15);
            this.groupBox5.Controls.Add(this.resourceBulkConvertBtn);
            this.groupBox5.Controls.Add(this.resourceOutputPathBrowseBtn);
            this.groupBox5.Controls.Add(this.resourceInputPathBrowseBtn);
            this.groupBox5.Controls.Add(this.resourceInputDir);
            this.groupBox5.Controls.Add(this.label13);
            this.groupBox5.Controls.Add(this.resourceOutputDir);
            this.groupBox5.Controls.Add(this.label14);
            this.groupBox5.Location = new System.Drawing.Point(3, 208);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox5.Size = new System.Drawing.Size(1193, 255);
            this.groupBox5.TabIndex = 62;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Batch Convert";
            // 
            // resourceProgressLabel
            // 
            this.resourceProgressLabel.AutoSize = true;
            this.resourceProgressLabel.Location = new System.Drawing.Point(87, 196);
            this.resourceProgressLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.resourceProgressLabel.Name = "resourceProgressLabel";
            this.resourceProgressLabel.Size = new System.Drawing.Size(0, 16);
            this.resourceProgressLabel.TabIndex = 67;
            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.resourceConvertBtn);
            this.groupBox4.Controls.Add(this.resourceOutputBrowseBtn);
            this.groupBox4.Controls.Add(this.resourceInputBrowseBtn);
            this.groupBox4.Controls.Add(this.resourceInputPath);
            this.groupBox4.Controls.Add(this.label11);
            this.groupBox4.Controls.Add(this.resourceOutputPath);
            this.groupBox4.Controls.Add(this.label12);
            this.groupBox4.Location = new System.Drawing.Point(1, 7);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox4.Size = new System.Drawing.Size(1193, 187);
            this.groupBox4.TabIndex = 61;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Convert LSX / LSB / LSF / LSJ files ";
            // 
            // resourceInputFileDlg
            // 
            this.resourceInputFileDlg.Filter = "LS files|*.lsx;*.lsb;*.lsf;*.lsj;*.lsfx;*.lsbc;*.lsbs";
            this.resourceInputFileDlg.Title = "Select Input File";
            // 
            // resourceOutputFileDlg
            // 
            this.resourceOutputFileDlg.Filter = "LS files|*.lsx;*.lsb;*.lsf;*.lsj;*.lsfx;*.lsbc;*.lsbs";
            this.resourceOutputFileDlg.Title = "Select Output File";
            // 
            // grpGlobalSettings
            // 
            this.grpGlobalSettings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpGlobalSettings.Controls.Add(this.lblGuidSerializationInfo);
            this.grpGlobalSettings.Controls.Add(this.legacyGuids);
            this.grpGlobalSettings.Location = new System.Drawing.Point(3, 471);
            this.grpGlobalSettings.Name = "grpGlobalSettings";
            this.grpGlobalSettings.Size = new System.Drawing.Size(1193, 86);
            this.grpGlobalSettings.TabIndex = 64;
            this.grpGlobalSettings.TabStop = false;
            this.grpGlobalSettings.Text = "Settings";
            // 
            // legacyGuids
            // 
            this.legacyGuids.AutoSize = true;
            this.legacyGuids.Location = new System.Drawing.Point(16, 47);
            this.legacyGuids.Name = "legacyGuids";
            this.legacyGuids.Size = new System.Drawing.Size(196, 20);
            this.legacyGuids.TabIndex = 64;
            this.legacyGuids.Text = "Read using old UUID format";
            this.legacyGuids.UseVisualStyleBackColor = true;
            // 
            // lblGuidSerializationInfo
            // 
            this.lblGuidSerializationInfo.AutoSize = true;
            this.lblGuidSerializationInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblGuidSerializationInfo.Location = new System.Drawing.Point(11, 22);
            this.lblGuidSerializationInfo.Name = "lblGuidSerializationInfo";
            this.lblGuidSerializationInfo.Size = new System.Drawing.Size(690, 16);
            this.lblGuidSerializationInfo.TabIndex = 65;
            this.lblGuidSerializationInfo.Text = "UUID format has changed in v1.18.6! Check this to force LSX files to be read usin" +
    "g the old (non-byteswapped) format.";
            // 
            // ResourcePane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grpGlobalSettings);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "ResourcePane";
            this.Size = new System.Drawing.Size(1207, 717);
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.grpGlobalSettings.ResumeLayout(false);
            this.grpGlobalSettings.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ProgressBar resourceConversionProgress;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.ComboBox resourceOutputFormatCb;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.ComboBox resourceInputFormatCb;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Button resourceBulkConvertBtn;
        private System.Windows.Forms.Button resourceOutputPathBrowseBtn;
        private System.Windows.Forms.Button resourceInputPathBrowseBtn;
        private System.Windows.Forms.TextBox resourceInputDir;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox resourceOutputDir;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Button resourceConvertBtn;
        private System.Windows.Forms.Button resourceOutputBrowseBtn;
        private System.Windows.Forms.Button resourceInputBrowseBtn;
        private System.Windows.Forms.TextBox resourceInputPath;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox resourceOutputPath;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Label resourceProgressLabel;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.OpenFileDialog resourceInputFileDlg;
        private System.Windows.Forms.SaveFileDialog resourceOutputFileDlg;
        private System.Windows.Forms.FolderBrowserDialog resourceInputPathDlg;
        private System.Windows.Forms.FolderBrowserDialog resourceOutputPathDlg;
        private System.Windows.Forms.GroupBox grpGlobalSettings;
        private System.Windows.Forms.Label lblGuidSerializationInfo;
        private System.Windows.Forms.CheckBox legacyGuids;
    }
}
