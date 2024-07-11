namespace ConverterApp
{
    partial class ClothPane
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
            this.inputPath = new System.Windows.Forms.TextBox();
            this.fileLbl = new System.Windows.Forms.Label();
            this.browseBtn = new System.Windows.Forms.Button();
            this.generatedTextBox = new System.Windows.Forms.TextBox();
            this.generateBtn = new System.Windows.Forms.Button();
            this.physicsMeshComboBox = new System.Windows.Forms.ComboBox();
            this.physicsMeshLbl = new System.Windows.Forms.Label();
            this.meshesLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.physicsPanel = new System.Windows.Forms.Panel();
            this.targetsPanel = new System.Windows.Forms.Panel();
            this.targetMeshesLbl = new System.Windows.Forms.Label();
            this.targetMeshesListView = new System.Windows.Forms.ListView();
            this.nameColumn = new System.Windows.Forms.ColumnHeader();
            this.inputFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.loadBtn = new System.Windows.Forms.Button();
            this.resourceNameTextBox = new System.Windows.Forms.TextBox();
            this.resourceNameLbl = new System.Windows.Forms.Label();
            this.meshesLayoutPanel.SuspendLayout();
            this.physicsPanel.SuspendLayout();
            this.targetsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // inputPath
            // 
            this.inputPath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.inputPath.Location = new System.Drawing.Point(3, 18);
            this.inputPath.Name = "inputPath";
            this.inputPath.Size = new System.Drawing.Size(884, 23);
            this.inputPath.TabIndex = 0;
            // 
            // fileLbl
            // 
            this.fileLbl.AutoSize = true;
            this.fileLbl.Location = new System.Drawing.Point(3, 0);
            this.fileLbl.Name = "fileLbl";
            this.fileLbl.Size = new System.Drawing.Size(79, 15);
            this.fileLbl.TabIndex = 1;
            this.fileLbl.Text = "GR2 File Path:";
            // 
            // browseBtn
            // 
            this.browseBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.browseBtn.Location = new System.Drawing.Point(893, 18);
            this.browseBtn.Name = "browseBtn";
            this.browseBtn.Size = new System.Drawing.Size(45, 23);
            this.browseBtn.TabIndex = 2;
            this.browseBtn.Text = "...";
            this.browseBtn.UseVisualStyleBackColor = true;
            this.browseBtn.Click += this.browseBtn_Click;
            // 
            // generatedTextBox
            // 
            this.generatedTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.generatedTextBox.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            this.generatedTextBox.Location = new System.Drawing.Point(3, 305);
            this.generatedTextBox.Multiline = true;
            this.generatedTextBox.Name = "generatedTextBox";
            this.generatedTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.generatedTextBox.Size = new System.Drawing.Size(994, 242);
            this.generatedTextBox.TabIndex = 3;
            this.generatedTextBox.WordWrap = false;
            // 
            // generateBtn
            // 
            this.generateBtn.Location = new System.Drawing.Point(3, 276);
            this.generateBtn.Name = "generateBtn";
            this.generateBtn.Size = new System.Drawing.Size(75, 23);
            this.generateBtn.TabIndex = 4;
            this.generateBtn.Text = "Generate";
            this.generateBtn.UseVisualStyleBackColor = true;
            this.generateBtn.Click += this.generateBtn_Click;
            // 
            // physicsMeshComboBox
            // 
            this.physicsMeshComboBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.physicsMeshComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.physicsMeshComboBox.FormattingEnabled = true;
            this.physicsMeshComboBox.Location = new System.Drawing.Point(3, 23);
            this.physicsMeshComboBox.Name = "physicsMeshComboBox";
            this.physicsMeshComboBox.Size = new System.Drawing.Size(494, 23);
            this.physicsMeshComboBox.TabIndex = 5;
            this.physicsMeshComboBox.SelectedIndexChanged += this.physicsMeshComboBox_SelectedIndexChanged;
            // 
            // physicsMeshLbl
            // 
            this.physicsMeshLbl.AutoSize = true;
            this.physicsMeshLbl.Location = new System.Drawing.Point(3, 4);
            this.physicsMeshLbl.Name = "physicsMeshLbl";
            this.physicsMeshLbl.Size = new System.Drawing.Size(81, 15);
            this.physicsMeshLbl.TabIndex = 6;
            this.physicsMeshLbl.Text = "Physics Mesh:";
            // 
            // meshesLayoutPanel
            // 
            this.meshesLayoutPanel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.meshesLayoutPanel.ColumnCount = 2;
            this.meshesLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.meshesLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.meshesLayoutPanel.Controls.Add(this.physicsPanel, 0, 0);
            this.meshesLayoutPanel.Controls.Add(this.targetsPanel, 1, 0);
            this.meshesLayoutPanel.Location = new System.Drawing.Point(0, 97);
            this.meshesLayoutPanel.Name = "meshesLayoutPanel";
            this.meshesLayoutPanel.RowCount = 1;
            this.meshesLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.meshesLayoutPanel.Size = new System.Drawing.Size(1000, 173);
            this.meshesLayoutPanel.TabIndex = 7;
            // 
            // physicsPanel
            // 
            this.physicsPanel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.physicsPanel.Controls.Add(this.physicsMeshComboBox);
            this.physicsPanel.Controls.Add(this.physicsMeshLbl);
            this.physicsPanel.Location = new System.Drawing.Point(0, 0);
            this.physicsPanel.Margin = new System.Windows.Forms.Padding(0);
            this.physicsPanel.Name = "physicsPanel";
            this.physicsPanel.Size = new System.Drawing.Size(500, 173);
            this.physicsPanel.TabIndex = 0;
            // 
            // targetsPanel
            // 
            this.targetsPanel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.targetsPanel.Controls.Add(this.targetMeshesLbl);
            this.targetsPanel.Controls.Add(this.targetMeshesListView);
            this.targetsPanel.Location = new System.Drawing.Point(500, 0);
            this.targetsPanel.Margin = new System.Windows.Forms.Padding(0);
            this.targetsPanel.Name = "targetsPanel";
            this.targetsPanel.Size = new System.Drawing.Size(500, 173);
            this.targetsPanel.TabIndex = 1;
            // 
            // targetMeshesLbl
            // 
            this.targetMeshesLbl.AutoSize = true;
            this.targetMeshesLbl.Location = new System.Drawing.Point(3, 4);
            this.targetMeshesLbl.Name = "targetMeshesLbl";
            this.targetMeshesLbl.Size = new System.Drawing.Size(85, 15);
            this.targetMeshesLbl.TabIndex = 1;
            this.targetMeshesLbl.Text = "Target Meshes:";
            // 
            // targetMeshesListView
            // 
            this.targetMeshesListView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.targetMeshesListView.CheckBoxes = true;
            this.targetMeshesListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { this.nameColumn });
            this.targetMeshesListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.targetMeshesListView.Location = new System.Drawing.Point(3, 23);
            this.targetMeshesListView.Name = "targetMeshesListView";
            this.targetMeshesListView.Size = new System.Drawing.Size(494, 147);
            this.targetMeshesListView.TabIndex = 0;
            this.targetMeshesListView.UseCompatibleStateImageBehavior = false;
            this.targetMeshesListView.View = System.Windows.Forms.View.Details;
            // 
            // nameColumn
            // 
            this.nameColumn.Text = "Name";
            this.nameColumn.Width = 400;
            // 
            // inputFileDlg
            // 
            this.inputFileDlg.Filter = "GR2 files|*.gr2;*.lsm";
            // 
            // loadBtn
            // 
            this.loadBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.loadBtn.Location = new System.Drawing.Point(944, 18);
            this.loadBtn.Name = "loadBtn";
            this.loadBtn.Size = new System.Drawing.Size(53, 23);
            this.loadBtn.TabIndex = 8;
            this.loadBtn.Text = "Load";
            this.loadBtn.UseVisualStyleBackColor = true;
            this.loadBtn.Click += this.loadBtn_Click;
            // 
            // resourceNameTextBox
            // 
            this.resourceNameTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.resourceNameTextBox.Location = new System.Drawing.Point(3, 68);
            this.resourceNameTextBox.Name = "resourceNameTextBox";
            this.resourceNameTextBox.Size = new System.Drawing.Size(994, 23);
            this.resourceNameTextBox.TabIndex = 9;
            // 
            // resourceNameLbl
            // 
            this.resourceNameLbl.AutoSize = true;
            this.resourceNameLbl.Location = new System.Drawing.Point(3, 50);
            this.resourceNameLbl.Name = "resourceNameLbl";
            this.resourceNameLbl.Size = new System.Drawing.Size(93, 15);
            this.resourceNameLbl.TabIndex = 10;
            this.resourceNameLbl.Text = "Resource Name:";
            // 
            // ClothPane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.resourceNameLbl);
            this.Controls.Add(this.resourceNameTextBox);
            this.Controls.Add(this.loadBtn);
            this.Controls.Add(this.meshesLayoutPanel);
            this.Controls.Add(this.generateBtn);
            this.Controls.Add(this.generatedTextBox);
            this.Controls.Add(this.browseBtn);
            this.Controls.Add(this.fileLbl);
            this.Controls.Add(this.inputPath);
            this.Name = "ClothPane";
            this.Size = new System.Drawing.Size(1000, 550);
            this.meshesLayoutPanel.ResumeLayout(false);
            this.physicsPanel.ResumeLayout(false);
            this.physicsPanel.PerformLayout();
            this.targetsPanel.ResumeLayout(false);
            this.targetsPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox inputPath;
        private System.Windows.Forms.Label fileLbl;
        private System.Windows.Forms.Button browseBtn;
        private System.Windows.Forms.TextBox generatedTextBox;
        private System.Windows.Forms.Button generateBtn;
        private System.Windows.Forms.ComboBox physicsMeshComboBox;
        private System.Windows.Forms.Label physicsMeshLbl;
        private System.Windows.Forms.TableLayoutPanel meshesLayoutPanel;
        private System.Windows.Forms.Panel physicsPanel;
        private System.Windows.Forms.Panel targetsPanel;
        private System.Windows.Forms.ListView targetMeshesListView;
        private System.Windows.Forms.ColumnHeader nameColumn;
        private System.Windows.Forms.OpenFileDialog inputFileDlg;
        private System.Windows.Forms.Label targetMeshesLbl;
        private System.Windows.Forms.Button loadBtn;
        private System.Windows.Forms.TextBox resourceNameTextBox;
        private System.Windows.Forms.Label resourceNameLbl;
    }
}
