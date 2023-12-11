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
            groupBox1 = new System.Windows.Forms.GroupBox();
            extractTileSetBtn = new System.Windows.Forms.Button();
            destinationPathBrowseBtn = new System.Windows.Forms.Button();
            gtsBrowseBtn = new System.Windows.Forms.Button();
            gtsPath = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            destinationPath = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            destinationPathDlg = new System.Windows.Forms.FolderBrowserDialog();
            gtsFileDlg = new System.Windows.Forms.OpenFileDialog();
            actionProgressLabel = new System.Windows.Forms.Label();
            actionProgress = new System.Windows.Forms.ProgressBar();
            label5 = new System.Windows.Forms.Label();
            groupBox2 = new System.Windows.Forms.GroupBox();
            modRootPathBrowseBtn = new System.Windows.Forms.Button();
            tileSetBrowseBtn = new System.Windows.Forms.Button();
            tileSetBuildBtn = new System.Windows.Forms.Button();
            button1 = new System.Windows.Forms.Button();
            button2 = new System.Windows.Forms.Button();
            button3 = new System.Windows.Forms.Button();
            tileSetConfigPath = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            modRootPath = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            modRootPathDlg = new System.Windows.Forms.FolderBrowserDialog();
            tileSetConfigDlg = new System.Windows.Forms.OpenFileDialog();
            gTexNameInput = new System.Windows.Forms.TextBox();
            label6 = new System.Windows.Forms.Label();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox1.Controls.Add(gTexNameInput);
            groupBox1.Controls.Add(label6);
            groupBox1.Controls.Add(extractTileSetBtn);
            groupBox1.Controls.Add(destinationPathBrowseBtn);
            groupBox1.Controls.Add(gtsBrowseBtn);
            groupBox1.Controls.Add(gtsPath);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(destinationPath);
            groupBox1.Controls.Add(label3);
            groupBox1.Location = new System.Drawing.Point(9, 20);
            groupBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBox1.Size = new System.Drawing.Size(1167, 296);
            groupBox1.TabIndex = 66;
            groupBox1.TabStop = false;
            groupBox1.Text = "Extract Virtual Textures";
            // 
            // extractTileSetBtn
            // 
            extractTileSetBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            extractTileSetBtn.Location = new System.Drawing.Point(945, 252);
            extractTileSetBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            extractTileSetBtn.Name = "extractTileSetBtn";
            extractTileSetBtn.Size = new System.Drawing.Size(213, 35);
            extractTileSetBtn.TabIndex = 62;
            extractTileSetBtn.Text = "Extract Textures";
            extractTileSetBtn.UseVisualStyleBackColor = true;
            extractTileSetBtn.Click += extractTileSetBtn_Click;
            // 
            // destinationPathBrowseBtn
            // 
            destinationPathBrowseBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            destinationPathBrowseBtn.Location = new System.Drawing.Point(1105, 118);
            destinationPathBrowseBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            destinationPathBrowseBtn.Name = "destinationPathBrowseBtn";
            destinationPathBrowseBtn.Size = new System.Drawing.Size(55, 35);
            destinationPathBrowseBtn.TabIndex = 61;
            destinationPathBrowseBtn.Text = "...";
            destinationPathBrowseBtn.UseVisualStyleBackColor = true;
            destinationPathBrowseBtn.Click += destinationPathBrowseBtn_Click;
            // 
            // gtsBrowseBtn
            // 
            gtsBrowseBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            gtsBrowseBtn.Location = new System.Drawing.Point(1105, 46);
            gtsBrowseBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            gtsBrowseBtn.Name = "gtsBrowseBtn";
            gtsBrowseBtn.Size = new System.Drawing.Size(55, 35);
            gtsBrowseBtn.TabIndex = 58;
            gtsBrowseBtn.Text = "...";
            gtsBrowseBtn.UseVisualStyleBackColor = true;
            gtsBrowseBtn.Click += gtpBrowseBtn_Click;
            // 
            // gtsPath
            // 
            gtsPath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            gtsPath.Location = new System.Drawing.Point(12, 49);
            gtsPath.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            gtsPath.Name = "gtsPath";
            gtsPath.Size = new System.Drawing.Size(1093, 27);
            gtsPath.TabIndex = 56;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(8, 25);
            label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(129, 20);
            label4.TabIndex = 57;
            label4.Text = "Tileset (GTS) path:";
            // 
            // destinationPath
            // 
            destinationPath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            destinationPath.Location = new System.Drawing.Point(12, 120);
            destinationPath.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            destinationPath.Name = "destinationPath";
            destinationPath.Size = new System.Drawing.Size(1093, 27);
            destinationPath.TabIndex = 59;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(8, 95);
            label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(122, 20);
            label3.TabIndex = 60;
            label3.Text = "Destination path:";
            // 
            // gtsFileDlg
            // 
            gtsFileDlg.Filter = "Virtual Texture Set (.gts)|*.gts";
            // 
            // actionProgressLabel
            // 
            actionProgressLabel.AutoSize = true;
            actionProgressLabel.Location = new System.Drawing.Point(88, 545);
            actionProgressLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            actionProgressLabel.Name = "actionProgressLabel";
            actionProgressLabel.Size = new System.Drawing.Size(0, 20);
            actionProgressLabel.TabIndex = 67;
            // 
            // actionProgress
            // 
            actionProgress.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            actionProgress.Location = new System.Drawing.Point(9, 568);
            actionProgress.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            actionProgress.Name = "actionProgress";
            actionProgress.Size = new System.Drawing.Size(1168, 35);
            actionProgress.TabIndex = 65;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(5, 544);
            label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(68, 20);
            label5.TabIndex = 66;
            label5.Text = "Progress:";
            // 
            // groupBox2
            // 
            groupBox2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox2.Controls.Add(modRootPathBrowseBtn);
            groupBox2.Controls.Add(tileSetBrowseBtn);
            groupBox2.Controls.Add(tileSetBuildBtn);
            groupBox2.Controls.Add(button1);
            groupBox2.Controls.Add(button2);
            groupBox2.Controls.Add(button3);
            groupBox2.Controls.Add(tileSetConfigPath);
            groupBox2.Controls.Add(label1);
            groupBox2.Controls.Add(modRootPath);
            groupBox2.Controls.Add(label2);
            groupBox2.Location = new System.Drawing.Point(10, 319);
            groupBox2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBox2.Size = new System.Drawing.Size(1167, 212);
            groupBox2.TabIndex = 68;
            groupBox2.TabStop = false;
            groupBox2.Text = "Build Tile Set";
            // 
            // modRootPathBrowseBtn
            // 
            modRootPathBrowseBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            modRootPathBrowseBtn.Location = new System.Drawing.Point(1104, 119);
            modRootPathBrowseBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            modRootPathBrowseBtn.Name = "modRootPathBrowseBtn";
            modRootPathBrowseBtn.Size = new System.Drawing.Size(55, 35);
            modRootPathBrowseBtn.TabIndex = 64;
            modRootPathBrowseBtn.Text = "...";
            modRootPathBrowseBtn.UseVisualStyleBackColor = true;
            modRootPathBrowseBtn.Click += modRootPathBrowseBtn_Click;
            // 
            // tileSetBrowseBtn
            // 
            tileSetBrowseBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            tileSetBrowseBtn.Location = new System.Drawing.Point(1104, 47);
            tileSetBrowseBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tileSetBrowseBtn.Name = "tileSetBrowseBtn";
            tileSetBrowseBtn.Size = new System.Drawing.Size(55, 35);
            tileSetBrowseBtn.TabIndex = 63;
            tileSetBrowseBtn.Text = "...";
            tileSetBrowseBtn.UseVisualStyleBackColor = true;
            tileSetBrowseBtn.Click += tileSetConfigBrowseBtn_Click;
            // 
            // tileSetBuildBtn
            // 
            tileSetBuildBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            tileSetBuildBtn.Location = new System.Drawing.Point(944, 167);
            tileSetBuildBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tileSetBuildBtn.Name = "tileSetBuildBtn";
            tileSetBuildBtn.Size = new System.Drawing.Size(213, 35);
            tileSetBuildBtn.TabIndex = 63;
            tileSetBuildBtn.Text = "Build";
            tileSetBuildBtn.UseVisualStyleBackColor = true;
            tileSetBuildBtn.Click += tileSetBuildBtn_Click;
            // 
            // button1
            // 
            button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            button1.Location = new System.Drawing.Point(1911, 278);
            button1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(213, 35);
            button1.TabIndex = 62;
            button1.Text = "Extract Textures";
            button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            button2.Location = new System.Drawing.Point(2071, 120);
            button2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            button2.Name = "button2";
            button2.Size = new System.Drawing.Size(55, 35);
            button2.TabIndex = 61;
            button2.Text = "...";
            button2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            button3.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            button3.Location = new System.Drawing.Point(2071, 48);
            button3.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            button3.Name = "button3";
            button3.Size = new System.Drawing.Size(55, 35);
            button3.TabIndex = 58;
            button3.Text = "...";
            button3.UseVisualStyleBackColor = true;
            // 
            // tileSetConfigPath
            // 
            tileSetConfigPath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tileSetConfigPath.Location = new System.Drawing.Point(13, 51);
            tileSetConfigPath.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tileSetConfigPath.Name = "tileSetConfigPath";
            tileSetConfigPath.Size = new System.Drawing.Size(1091, 27);
            tileSetConfigPath.TabIndex = 56;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(9, 27);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(156, 20);
            label1.TabIndex = 57;
            label1.Text = "Tile Set Configuration:";
            // 
            // modRootPath
            // 
            modRootPath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            modRootPath.Location = new System.Drawing.Point(13, 122);
            modRootPath.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            modRootPath.Name = "modRootPath";
            modRootPath.Size = new System.Drawing.Size(1091, 27);
            modRootPath.TabIndex = 59;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(9, 97);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(109, 20);
            label2.TabIndex = 60;
            label2.Text = "Mod root path:";
            // 
            // tileSetConfigDlg
            // 
            tileSetConfigDlg.Filter = "Virtual Texture Set Configuration (.xml)|*.xml";
            // 
            // gTexNameInput
            // 
            gTexNameInput.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            gTexNameInput.Location = new System.Drawing.Point(10, 194);
            gTexNameInput.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            gTexNameInput.Name = "gTexNameInput";
            gTexNameInput.Size = new System.Drawing.Size(1148, 27);
            gTexNameInput.TabIndex = 63;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(6, 169);
            label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(326, 20);
            label6.TabIndex = 64;
            label6.Text = "GTex Name: (leave empty to extract all textures)";
            // 
            // VirtualTexturesPane
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(groupBox2);
            Controls.Add(actionProgressLabel);
            Controls.Add(groupBox1);
            Controls.Add(actionProgress);
            Controls.Add(label5);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "VirtualTexturesPane";
            Size = new System.Drawing.Size(1188, 666);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
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
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.TextBox tileSetConfigPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox modRootPath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.FolderBrowserDialog modRootPathDlg;
        private System.Windows.Forms.OpenFileDialog tileSetConfigDlg;
        private System.Windows.Forms.Button modRootPathBrowseBtn;
        private System.Windows.Forms.Button tileSetBrowseBtn;
        private System.Windows.Forms.Button tileSetBuildBtn;
        private System.Windows.Forms.TextBox gTexNameInput;
        private System.Windows.Forms.Label label6;
    }
}
