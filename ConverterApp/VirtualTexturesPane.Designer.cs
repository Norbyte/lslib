namespace ConverterApp
{
    partial class VirtualTexturesPane
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.extractTileSetBtn = new System.Windows.Forms.Button();
            this.destinationPathBrowseBtn = new System.Windows.Forms.Button();
            this.gtsBrowseBtn = new System.Windows.Forms.Button();
            this.gtsPath = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.destinationPath = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.destinationPathDlg = new System.Windows.Forms.FolderBrowserDialog();
            this.gtsFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.actionProgressLabel = new System.Windows.Forms.Label();
            this.actionProgress = new System.Windows.Forms.ProgressBar();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.extractTileSetBtn);
            this.groupBox1.Controls.Add(this.destinationPathBrowseBtn);
            this.groupBox1.Controls.Add(this.gtsBrowseBtn);
            this.groupBox1.Controls.Add(this.gtsPath);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.destinationPath);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Location = new System.Drawing.Point(9, 16);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox1.Size = new System.Drawing.Size(1167, 170);
            this.groupBox1.TabIndex = 66;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Extract Virtual Textures";
            // 
            // extractTileSetBtn
            // 
            this.extractTileSetBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.extractTileSetBtn.Location = new System.Drawing.Point(945, 134);
            this.extractTileSetBtn.Margin = new System.Windows.Forms.Padding(4);
            this.extractTileSetBtn.Name = "extractTileSetBtn";
            this.extractTileSetBtn.Size = new System.Drawing.Size(213, 28);
            this.extractTileSetBtn.TabIndex = 62;
            this.extractTileSetBtn.Text = "Extract Textures";
            this.extractTileSetBtn.UseVisualStyleBackColor = true;
            this.extractTileSetBtn.Click += new System.EventHandler(this.extractTileSetBtn_Click);
            // 
            // destinationPathBrowseBtn
            // 
            this.destinationPathBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.destinationPathBrowseBtn.Location = new System.Drawing.Point(1105, 94);
            this.destinationPathBrowseBtn.Margin = new System.Windows.Forms.Padding(4);
            this.destinationPathBrowseBtn.Name = "destinationPathBrowseBtn";
            this.destinationPathBrowseBtn.Size = new System.Drawing.Size(55, 28);
            this.destinationPathBrowseBtn.TabIndex = 61;
            this.destinationPathBrowseBtn.Text = "...";
            this.destinationPathBrowseBtn.UseVisualStyleBackColor = true;
            this.destinationPathBrowseBtn.Click += new System.EventHandler(this.destinationPathBrowseBtn_Click);
            // 
            // gtsBrowseBtn
            // 
            this.gtsBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gtsBrowseBtn.Location = new System.Drawing.Point(1105, 37);
            this.gtsBrowseBtn.Margin = new System.Windows.Forms.Padding(4);
            this.gtsBrowseBtn.Name = "gtsBrowseBtn";
            this.gtsBrowseBtn.Size = new System.Drawing.Size(55, 28);
            this.gtsBrowseBtn.TabIndex = 58;
            this.gtsBrowseBtn.Text = "...";
            this.gtsBrowseBtn.UseVisualStyleBackColor = true;
            this.gtsBrowseBtn.Click += new System.EventHandler(this.gtpBrowseBtn_Click);
            // 
            // gtsPath
            // 
            this.gtsPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gtsPath.Location = new System.Drawing.Point(12, 39);
            this.gtsPath.Margin = new System.Windows.Forms.Padding(4);
            this.gtsPath.Name = "gtsPath";
            this.gtsPath.Size = new System.Drawing.Size(1093, 22);
            this.gtsPath.TabIndex = 56;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 20);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(119, 16);
            this.label4.TabIndex = 57;
            this.label4.Text = "Tileset (GTS) path:";
            // 
            // destinationPath
            // 
            this.destinationPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.destinationPath.Location = new System.Drawing.Point(12, 96);
            this.destinationPath.Margin = new System.Windows.Forms.Padding(4);
            this.destinationPath.Name = "destinationPath";
            this.destinationPath.Size = new System.Drawing.Size(1093, 22);
            this.destinationPath.TabIndex = 59;
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
            // gtsFileDlg
            // 
            this.gtsFileDlg.Filter = "Virtual Texture Set (.gts)|*.gts";
            // 
            // actionProgressLabel
            // 
            this.actionProgressLabel.AutoSize = true;
            this.actionProgressLabel.Location = new System.Drawing.Point(94, 195);
            this.actionProgressLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.actionProgressLabel.Name = "actionProgressLabel";
            this.actionProgressLabel.Size = new System.Drawing.Size(0, 16);
            this.actionProgressLabel.TabIndex = 67;
            // 
            // actionProgress
            // 
            this.actionProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.actionProgress.Location = new System.Drawing.Point(9, 212);
            this.actionProgress.Margin = new System.Windows.Forms.Padding(4);
            this.actionProgress.Name = "actionProgress";
            this.actionProgress.Size = new System.Drawing.Size(1168, 28);
            this.actionProgress.TabIndex = 65;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(5, 193);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 16);
            this.label5.TabIndex = 66;
            this.label5.Text = "Progress:";
            // 
            // VirtualTexturesPane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.actionProgressLabel);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.actionProgress);
            this.Controls.Add(this.label5);
            this.Name = "VirtualTexturesPane";
            this.Size = new System.Drawing.Size(1188, 378);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button extractTileSetBtn;
        private System.Windows.Forms.Button destinationPathBrowseBtn;
        private System.Windows.Forms.Button gtsBrowseBtn;
        private System.Windows.Forms.TextBox gtsPath;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox destinationPath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.FolderBrowserDialog destinationPathDlg;
        private System.Windows.Forms.OpenFileDialog gtsFileDlg;
        private System.Windows.Forms.Label actionProgressLabel;
        private System.Windows.Forms.ProgressBar actionProgress;
        private System.Windows.Forms.Label label5;
    }
}
