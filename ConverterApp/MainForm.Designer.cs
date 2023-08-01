namespace ConverterApp
{
    sealed partial class MainForm
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
            this.virtualTextureTab = new System.Windows.Forms.TabPage();
            this.osirisTab = new System.Windows.Forms.TabPage();
            this.debugTab = new System.Windows.Forms.TabPage();
            this.gr2Game = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.locaTab = new System.Windows.Forms.TabPage();
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
            this.tabControl.Controls.Add(this.virtualTextureTab);
            this.tabControl.Controls.Add(this.locaTab);
            this.tabControl.Controls.Add(this.osirisTab);
            this.tabControl.Controls.Add(this.debugTab);
            this.tabControl.Location = new System.Drawing.Point(16, 52);
            this.tabControl.Margin = new System.Windows.Forms.Padding(4);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1223, 763);
            this.tabControl.TabIndex = 0;
            // 
            // gr2Tab
            // 
            this.gr2Tab.Location = new System.Drawing.Point(4, 25);
            this.gr2Tab.Margin = new System.Windows.Forms.Padding(4);
            this.gr2Tab.Name = "gr2Tab";
            this.gr2Tab.Padding = new System.Windows.Forms.Padding(4);
            this.gr2Tab.Size = new System.Drawing.Size(1215, 734);
            this.gr2Tab.TabIndex = 0;
            this.gr2Tab.Text = "GR2 Tools";
            this.gr2Tab.UseVisualStyleBackColor = true;
            // 
            // packageTab
            // 
            this.packageTab.Location = new System.Drawing.Point(4, 25);
            this.packageTab.Margin = new System.Windows.Forms.Padding(4);
            this.packageTab.Name = "packageTab";
            this.packageTab.Padding = new System.Windows.Forms.Padding(4);
            this.packageTab.Size = new System.Drawing.Size(1215, 734);
            this.packageTab.TabIndex = 1;
            this.packageTab.Text = "PAK / LSV Tools";
            this.packageTab.UseVisualStyleBackColor = true;
            // 
            // resourceTab
            // 
            this.resourceTab.Location = new System.Drawing.Point(4, 25);
            this.resourceTab.Margin = new System.Windows.Forms.Padding(4);
            this.resourceTab.Name = "resourceTab";
            this.resourceTab.Padding = new System.Windows.Forms.Padding(4);
            this.resourceTab.Size = new System.Drawing.Size(1215, 734);
            this.resourceTab.TabIndex = 2;
            this.resourceTab.Text = "LSX / LSB / LSF / LSJ Tools";
            this.resourceTab.UseVisualStyleBackColor = true;
            // 
            // virtualTextureTab
            // 
            this.virtualTextureTab.Location = new System.Drawing.Point(4, 25);
            this.virtualTextureTab.Name = "virtualTextureTab";
            this.virtualTextureTab.Padding = new System.Windows.Forms.Padding(3);
            this.virtualTextureTab.Size = new System.Drawing.Size(1215, 734);
            this.virtualTextureTab.TabIndex = 5;
            this.virtualTextureTab.Text = "Virtual Textures";
            this.virtualTextureTab.UseVisualStyleBackColor = true;
            // 
            // osirisTab
            // 
            this.osirisTab.Location = new System.Drawing.Point(4, 25);
            this.osirisTab.Margin = new System.Windows.Forms.Padding(4);
            this.osirisTab.Name = "osirisTab";
            this.osirisTab.Padding = new System.Windows.Forms.Padding(4);
            this.osirisTab.Size = new System.Drawing.Size(1215, 734);
            this.osirisTab.TabIndex = 3;
            this.osirisTab.Text = "Story (OSI) tools";
            this.osirisTab.UseVisualStyleBackColor = true;
            // 
            // debugTab
            // 
            this.debugTab.Location = new System.Drawing.Point(4, 25);
            this.debugTab.Margin = new System.Windows.Forms.Padding(4);
            this.debugTab.Name = "debugTab";
            this.debugTab.Size = new System.Drawing.Size(1215, 734);
            this.debugTab.TabIndex = 4;
            this.debugTab.Text = "Savegame Debugging";
            this.debugTab.UseVisualStyleBackColor = true;
            // 
            // gr2Game
            // 
            this.gr2Game.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.gr2Game.FormattingEnabled = true;
            this.gr2Game.Items.AddRange(new object[] {
            "Divinity: Original Sin (32-bit)",
            "Divinity: Original Sin EE (64-bit)",
            "Divinity: Original Sin 2 (64-bit)",
            "Divinity: Original Sin 2 DE (64-bit)",
            "Baldur\'s Gate 3 (64-bit)"});
            this.gr2Game.Location = new System.Drawing.Point(99, 15);
            this.gr2Game.Margin = new System.Windows.Forms.Padding(4);
            this.gr2Game.Name = "gr2Game";
            this.gr2Game.Size = new System.Drawing.Size(473, 24);
            this.gr2Game.TabIndex = 30;
            this.gr2Game.SelectedIndexChanged += new System.EventHandler(this.gr2Game_SelectedIndexChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(16, 17);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(47, 16);
            this.label7.TabIndex = 29;
            this.label7.Text = "Game:";
            // 
            // locaTab
            // 
            this.locaTab.Location = new System.Drawing.Point(4, 25);
            this.locaTab.Name = "locaTab";
            this.locaTab.Padding = new System.Windows.Forms.Padding(3);
            this.locaTab.Size = new System.Drawing.Size(1215, 734);
            this.locaTab.TabIndex = 6;
            this.locaTab.Text = "Localization";
            this.locaTab.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1255, 826);
            this.Controls.Add(this.gr2Game);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.tabControl);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.Text = "LSLib Toolkit";
            this.tabControl.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage gr2Tab;
        private System.Windows.Forms.TabPage packageTab;
        private System.Windows.Forms.TabPage resourceTab;
        private System.Windows.Forms.TabPage virtualTextureTab;
        private System.Windows.Forms.TabPage osirisTab;
        private System.Windows.Forms.ComboBox gr2Game;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TabPage debugTab;
        private System.Windows.Forms.TabPage locaTab;
    }
}

