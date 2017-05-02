namespace ConverterApp
{
    partial class MainForm
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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.gr2Tab = new System.Windows.Forms.TabPage();
            this.packageTab = new System.Windows.Forms.TabPage();
            this.resourceTab = new System.Windows.Forms.TabPage();
            this.osirisTab = new System.Windows.Forms.TabPage();
            this.gr2Game = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.tabControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.gr2Tab);
            this.tabControl.Controls.Add(this.packageTab);
            this.tabControl.Controls.Add(this.resourceTab);
            this.tabControl.Controls.Add(this.osirisTab);
            this.tabControl.Location = new System.Drawing.Point(12, 42);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(917, 620);
            this.tabControl.TabIndex = 0;
            // 
            // gr2Tab
            // 
            this.gr2Tab.Location = new System.Drawing.Point(4, 22);
            this.gr2Tab.Name = "gr2Tab";
            this.gr2Tab.Padding = new System.Windows.Forms.Padding(3);
            this.gr2Tab.Size = new System.Drawing.Size(909, 594);
            this.gr2Tab.TabIndex = 0;
            this.gr2Tab.Text = "GR2 Tools";
            this.gr2Tab.UseVisualStyleBackColor = true;
            // 
            // packageTab
            // 
            this.packageTab.Location = new System.Drawing.Point(4, 22);
            this.packageTab.Name = "packageTab";
            this.packageTab.Padding = new System.Windows.Forms.Padding(3);
            this.packageTab.Size = new System.Drawing.Size(909, 594);
            this.packageTab.TabIndex = 1;
            this.packageTab.Text = "PAK / LSV Tools";
            this.packageTab.UseVisualStyleBackColor = true;
            // 
            // resourceTab
            // 
            this.resourceTab.Location = new System.Drawing.Point(4, 22);
            this.resourceTab.Name = "resourceTab";
            this.resourceTab.Padding = new System.Windows.Forms.Padding(3);
            this.resourceTab.Size = new System.Drawing.Size(909, 594);
            this.resourceTab.TabIndex = 2;
            this.resourceTab.Text = "LSX / LSB / LSF / LSJ Tools";
            this.resourceTab.UseVisualStyleBackColor = true;
            // 
            // osirisTab
            // 
            this.osirisTab.Location = new System.Drawing.Point(4, 22);
            this.osirisTab.Name = "osirisTab";
            this.osirisTab.Padding = new System.Windows.Forms.Padding(3);
            this.osirisTab.Size = new System.Drawing.Size(909, 594);
            this.osirisTab.TabIndex = 3;
            this.osirisTab.Text = "Story (OSI) tools";
            this.osirisTab.UseVisualStyleBackColor = true;
            // 
            // gr2Game
            // 
            this.gr2Game.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.gr2Game.FormattingEnabled = true;
            this.gr2Game.Items.AddRange(new object[] {
            "Divinity: Original Sin (32-bit)",
            "Divinity: Original Sin EE (64-bit)",
            "Divinity: Original Sin 2 (64-bit)"});
            this.gr2Game.Location = new System.Drawing.Point(74, 12);
            this.gr2Game.Name = "gr2Game";
            this.gr2Game.Size = new System.Drawing.Size(356, 21);
            this.gr2Game.TabIndex = 30;
            this.gr2Game.SelectedIndexChanged += new System.EventHandler(this.gr2Game_SelectedIndexChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 14);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(38, 13);
            this.label7.TabIndex = 29;
            this.label7.Text = "Game:";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(941, 671);
            this.Controls.Add(this.gr2Game);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.tabControl);
            this.Name = "MainForm";
            this.Text = "GR2 Converter";
            this.tabControl.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage gr2Tab;
        private System.Windows.Forms.TabPage packageTab;
        private System.Windows.Forms.TabPage resourceTab;
        private System.Windows.Forms.TabPage osirisTab;
        private System.Windows.Forms.ComboBox gr2Game;
        private System.Windows.Forms.Label label7;
    }
}

