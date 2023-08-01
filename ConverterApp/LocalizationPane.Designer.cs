namespace ConverterApp
{
    partial class LocalizationPane
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
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.locaConvertBtn = new System.Windows.Forms.Button();
            this.locaOutputBrowseBtn = new System.Windows.Forms.Button();
            this.locaInputBrowseBtn = new System.Windows.Forms.Button();
            this.locaInputPath = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.locaOutputPath = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.locaInputFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.locaOutputFileDlg = new System.Windows.Forms.SaveFileDialog();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.locaConvertBtn);
            this.groupBox4.Controls.Add(this.locaOutputBrowseBtn);
            this.groupBox4.Controls.Add(this.locaInputBrowseBtn);
            this.groupBox4.Controls.Add(this.locaInputPath);
            this.groupBox4.Controls.Add(this.label11);
            this.groupBox4.Controls.Add(this.locaOutputPath);
            this.groupBox4.Controls.Add(this.label12);
            this.groupBox4.Location = new System.Drawing.Point(9, 9);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox4.Size = new System.Drawing.Size(1100, 187);
            this.groupBox4.TabIndex = 63;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Convert localization files";
            // 
            // locaConvertBtn
            // 
            this.locaConvertBtn.Location = new System.Drawing.Point(16, 143);
            this.locaConvertBtn.Margin = new System.Windows.Forms.Padding(4);
            this.locaConvertBtn.Name = "locaConvertBtn";
            this.locaConvertBtn.Size = new System.Drawing.Size(201, 28);
            this.locaConvertBtn.TabIndex = 60;
            this.locaConvertBtn.Text = "Convert";
            this.locaConvertBtn.UseVisualStyleBackColor = true;
            this.locaConvertBtn.Click += new System.EventHandler(this.locaConvertBtn_Click);
            // 
            // locaOutputBrowseBtn
            // 
            this.locaOutputBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.locaOutputBrowseBtn.Location = new System.Drawing.Point(1032, 106);
            this.locaOutputBrowseBtn.Margin = new System.Windows.Forms.Padding(4);
            this.locaOutputBrowseBtn.Name = "locaOutputBrowseBtn";
            this.locaOutputBrowseBtn.Size = new System.Drawing.Size(55, 27);
            this.locaOutputBrowseBtn.TabIndex = 59;
            this.locaOutputBrowseBtn.Text = "...";
            this.locaOutputBrowseBtn.UseVisualStyleBackColor = true;
            this.locaOutputBrowseBtn.Click += new System.EventHandler(this.locaOutputBrowseBtn_Click);
            // 
            // locaInputBrowseBtn
            // 
            this.locaInputBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.locaInputBrowseBtn.Location = new System.Drawing.Point(1032, 47);
            this.locaInputBrowseBtn.Margin = new System.Windows.Forms.Padding(4);
            this.locaInputBrowseBtn.Name = "locaInputBrowseBtn";
            this.locaInputBrowseBtn.Size = new System.Drawing.Size(55, 27);
            this.locaInputBrowseBtn.TabIndex = 56;
            this.locaInputBrowseBtn.Text = "...";
            this.locaInputBrowseBtn.UseVisualStyleBackColor = true;
            this.locaInputBrowseBtn.Click += new System.EventHandler(this.resourceInputBrowseBtn_Click);
            // 
            // locaInputPath
            // 
            this.locaInputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.locaInputPath.Location = new System.Drawing.Point(15, 48);
            this.locaInputPath.Margin = new System.Windows.Forms.Padding(4);
            this.locaInputPath.Name = "locaInputPath";
            this.locaInputPath.Size = new System.Drawing.Size(1018, 22);
            this.locaInputPath.TabIndex = 54;
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
            // locaOutputPath
            // 
            this.locaOutputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.locaOutputPath.Location = new System.Drawing.Point(15, 107);
            this.locaOutputPath.Margin = new System.Windows.Forms.Padding(4);
            this.locaOutputPath.Name = "locaOutputPath";
            this.locaOutputPath.Size = new System.Drawing.Size(1018, 22);
            this.locaOutputPath.TabIndex = 57;
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
            // locaInputFileDlg
            // 
            this.locaInputFileDlg.Filter = "Localization files|*.loca;*.xml";
            this.locaInputFileDlg.Title = "Select Input File";
            // 
            // locaOutputFileDlg
            // 
            this.locaOutputFileDlg.Filter = "Localization files|*.loca;*.xml";
            this.locaOutputFileDlg.Title = "Select Output File";
            // 
            // LocalizationPane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox4);
            this.Name = "LocalizationPane";
            this.Size = new System.Drawing.Size(1119, 205);
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button locaConvertBtn;
        private System.Windows.Forms.Button locaOutputBrowseBtn;
        private System.Windows.Forms.Button locaInputBrowseBtn;
        private System.Windows.Forms.TextBox locaInputPath;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox locaOutputPath;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.OpenFileDialog locaInputFileDlg;
        private System.Windows.Forms.SaveFileDialog locaOutputFileDlg;
    }
}
