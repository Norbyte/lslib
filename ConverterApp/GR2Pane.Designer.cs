namespace ConverterApp
{
    partial class GR2Pane
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
            this.gr2ModeTabControl = new System.Windows.Forms.TabControl();
            this.gr2SingleFileTab = new System.Windows.Forms.TabPage();
            this.lblOutputPath = new System.Windows.Forms.Label();
            this.saveOutputBtn = new System.Windows.Forms.Button();
            this.inputPath = new System.Windows.Forms.TextBox();
            this.lblSrcPath = new System.Windows.Forms.Label();
            this.inputFileBrowseBtn = new System.Windows.Forms.Button();
            this.loadInputBtn = new System.Windows.Forms.Button();
            this.outputPath = new System.Windows.Forms.TextBox();
            this.outputFileBrowserBtn = new System.Windows.Forms.Button();
            this.gr2BatchTab = new System.Windows.Forms.TabPage();
            this.gr2BatchProgressLabel = new System.Windows.Forms.Label();
            this.gr2BatchInputBrowseBtn = new System.Windows.Forms.Button();
            this.gr2BatchOutputBrowseBtn = new System.Windows.Forms.Button();
            this.gr2BatchProgressBar = new System.Windows.Forms.ProgressBar();
            this.label23 = new System.Windows.Forms.Label();
            this.gr2BatchInputFormat = new System.Windows.Forms.ComboBox();
            this.label22 = new System.Windows.Forms.Label();
            this.gr2BatchOutputFormat = new System.Windows.Forms.ComboBox();
            this.label21 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.gr2BatchConvertBtn = new System.Windows.Forms.Button();
            this.gr2BatchInputDir = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.gr2BatchOutputDir = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.exportColors = new System.Windows.Forms.CheckBox();
            this.flipUVs = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.exportableObjects = new System.Windows.Forms.ListView();
            this.exportableName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.exportableType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.exportUVs = new System.Windows.Forms.CheckBox();
            this.exportTangents = new System.Windows.Forms.CheckBox();
            this.exportNormals = new System.Windows.Forms.CheckBox();
            this.recalculateTangents = new System.Windows.Forms.CheckBox();
            this.filterUVs = new System.Windows.Forms.CheckBox();
            this.recalculateJointIWT = new System.Windows.Forms.CheckBox();
            this.deduplicateVertices = new System.Windows.Forms.CheckBox();
            this.recalculateNormals = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.applyBasisTransforms = new System.Windows.Forms.CheckBox();
            this.conformantGR2BrowseBtn = new System.Windows.Forms.Button();
            this.conformantGR2Path = new System.Windows.Forms.TextBox();
            this.conformToOriginal = new System.Windows.Forms.CheckBox();
            this.buildDummySkeleton = new System.Windows.Forms.CheckBox();
            this.use16bitIndex = new System.Windows.Forms.CheckBox();
            this.forceLegacyVersion = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.gr2OutputDirDlg = new System.Windows.Forms.FolderBrowserDialog();
            this.gr2InputDirDlg = new System.Windows.Forms.FolderBrowserDialog();
            this.conformSkeletonFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.outputFileDlg = new System.Windows.Forms.SaveFileDialog();
            this.inputFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.rigid = new System.Windows.Forms.CheckBox();
            this.resourceFormats = new ConverterApp.ExportItemSelection();
            this.gr2ModeTabControl.SuspendLayout();
            this.gr2SingleFileTab.SuspendLayout();
            this.gr2BatchTab.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // gr2ModeTabControl
            // 
            this.gr2ModeTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gr2ModeTabControl.Controls.Add(this.gr2SingleFileTab);
            this.gr2ModeTabControl.Controls.Add(this.gr2BatchTab);
            this.gr2ModeTabControl.Location = new System.Drawing.Point(7, 9);
            this.gr2ModeTabControl.Name = "gr2ModeTabControl";
            this.gr2ModeTabControl.SelectedIndex = 0;
            this.gr2ModeTabControl.Size = new System.Drawing.Size(887, 159);
            this.gr2ModeTabControl.TabIndex = 38;
            // 
            // gr2SingleFileTab
            // 
            this.gr2SingleFileTab.Controls.Add(this.lblOutputPath);
            this.gr2SingleFileTab.Controls.Add(this.saveOutputBtn);
            this.gr2SingleFileTab.Controls.Add(this.inputPath);
            this.gr2SingleFileTab.Controls.Add(this.lblSrcPath);
            this.gr2SingleFileTab.Controls.Add(this.inputFileBrowseBtn);
            this.gr2SingleFileTab.Controls.Add(this.loadInputBtn);
            this.gr2SingleFileTab.Controls.Add(this.outputPath);
            this.gr2SingleFileTab.Controls.Add(this.outputFileBrowserBtn);
            this.gr2SingleFileTab.Location = new System.Drawing.Point(4, 22);
            this.gr2SingleFileTab.Name = "gr2SingleFileTab";
            this.gr2SingleFileTab.Padding = new System.Windows.Forms.Padding(3);
            this.gr2SingleFileTab.Size = new System.Drawing.Size(879, 133);
            this.gr2SingleFileTab.TabIndex = 0;
            this.gr2SingleFileTab.Text = "Single File";
            this.gr2SingleFileTab.UseVisualStyleBackColor = true;
            // 
            // lblOutputPath
            // 
            this.lblOutputPath.AutoSize = true;
            this.lblOutputPath.Location = new System.Drawing.Point(6, 46);
            this.lblOutputPath.Name = "lblOutputPath";
            this.lblOutputPath.Size = new System.Drawing.Size(82, 13);
            this.lblOutputPath.TabIndex = 29;
            this.lblOutputPath.Text = "Output file path:";
            // 
            // saveOutputBtn
            // 
            this.saveOutputBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.saveOutputBtn.Enabled = false;
            this.saveOutputBtn.Location = new System.Drawing.Point(721, 61);
            this.saveOutputBtn.Name = "saveOutputBtn";
            this.saveOutputBtn.Size = new System.Drawing.Size(141, 23);
            this.saveOutputBtn.TabIndex = 34;
            this.saveOutputBtn.Text = "Export";
            this.saveOutputBtn.UseVisualStyleBackColor = true;
            this.saveOutputBtn.Click += new System.EventHandler(this.saveOutputBtn_Click);
            // 
            // inputPath
            // 
            this.inputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.inputPath.Location = new System.Drawing.Point(9, 19);
            this.inputPath.Name = "inputPath";
            this.inputPath.Size = new System.Drawing.Size(659, 20);
            this.inputPath.TabIndex = 25;
            // 
            // lblSrcPath
            // 
            this.lblSrcPath.AutoSize = true;
            this.lblSrcPath.Location = new System.Drawing.Point(6, 3);
            this.lblSrcPath.Name = "lblSrcPath";
            this.lblSrcPath.Size = new System.Drawing.Size(74, 13);
            this.lblSrcPath.TabIndex = 26;
            this.lblSrcPath.Text = "Input file path:";
            // 
            // inputFileBrowseBtn
            // 
            this.inputFileBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.inputFileBrowseBtn.Location = new System.Drawing.Point(666, 18);
            this.inputFileBrowseBtn.Name = "inputFileBrowseBtn";
            this.inputFileBrowseBtn.Size = new System.Drawing.Size(41, 22);
            this.inputFileBrowseBtn.TabIndex = 27;
            this.inputFileBrowseBtn.Text = "...";
            this.inputFileBrowseBtn.UseVisualStyleBackColor = true;
            this.inputFileBrowseBtn.Click += new System.EventHandler(this.inputFileBrowseBtn_Click);
            // 
            // loadInputBtn
            // 
            this.loadInputBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.loadInputBtn.Location = new System.Drawing.Point(721, 18);
            this.loadInputBtn.Name = "loadInputBtn";
            this.loadInputBtn.Size = new System.Drawing.Size(141, 23);
            this.loadInputBtn.TabIndex = 31;
            this.loadInputBtn.Text = "Import";
            this.loadInputBtn.UseVisualStyleBackColor = true;
            this.loadInputBtn.Click += new System.EventHandler(this.loadInputBtn_Click);
            // 
            // outputPath
            // 
            this.outputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.outputPath.Location = new System.Drawing.Point(9, 62);
            this.outputPath.Name = "outputPath";
            this.outputPath.Size = new System.Drawing.Size(659, 20);
            this.outputPath.TabIndex = 28;
            // 
            // outputFileBrowserBtn
            // 
            this.outputFileBrowserBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.outputFileBrowserBtn.Location = new System.Drawing.Point(666, 61);
            this.outputFileBrowserBtn.Name = "outputFileBrowserBtn";
            this.outputFileBrowserBtn.Size = new System.Drawing.Size(41, 22);
            this.outputFileBrowserBtn.TabIndex = 30;
            this.outputFileBrowserBtn.Text = "...";
            this.outputFileBrowserBtn.UseVisualStyleBackColor = true;
            this.outputFileBrowserBtn.Click += new System.EventHandler(this.outputFileBrowserBtn_Click);
            // 
            // gr2BatchTab
            // 
            this.gr2BatchTab.Controls.Add(this.gr2BatchProgressLabel);
            this.gr2BatchTab.Controls.Add(this.gr2BatchInputBrowseBtn);
            this.gr2BatchTab.Controls.Add(this.gr2BatchOutputBrowseBtn);
            this.gr2BatchTab.Controls.Add(this.gr2BatchProgressBar);
            this.gr2BatchTab.Controls.Add(this.label23);
            this.gr2BatchTab.Controls.Add(this.gr2BatchInputFormat);
            this.gr2BatchTab.Controls.Add(this.label22);
            this.gr2BatchTab.Controls.Add(this.gr2BatchOutputFormat);
            this.gr2BatchTab.Controls.Add(this.label21);
            this.gr2BatchTab.Controls.Add(this.label19);
            this.gr2BatchTab.Controls.Add(this.gr2BatchConvertBtn);
            this.gr2BatchTab.Controls.Add(this.gr2BatchInputDir);
            this.gr2BatchTab.Controls.Add(this.label20);
            this.gr2BatchTab.Controls.Add(this.gr2BatchOutputDir);
            this.gr2BatchTab.Location = new System.Drawing.Point(4, 22);
            this.gr2BatchTab.Name = "gr2BatchTab";
            this.gr2BatchTab.Padding = new System.Windows.Forms.Padding(3);
            this.gr2BatchTab.Size = new System.Drawing.Size(879, 133);
            this.gr2BatchTab.TabIndex = 1;
            this.gr2BatchTab.Text = "Batch";
            this.gr2BatchTab.UseVisualStyleBackColor = true;
            // 
            // gr2BatchProgressLabel
            // 
            this.gr2BatchProgressLabel.AutoSize = true;
            this.gr2BatchProgressLabel.Location = new System.Drawing.Point(82, 88);
            this.gr2BatchProgressLabel.Name = "gr2BatchProgressLabel";
            this.gr2BatchProgressLabel.Size = new System.Drawing.Size(0, 13);
            this.gr2BatchProgressLabel.TabIndex = 49;
            // 
            // gr2BatchInputBrowseBtn
            // 
            this.gr2BatchInputBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gr2BatchInputBrowseBtn.Location = new System.Drawing.Point(822, 17);
            this.gr2BatchInputBrowseBtn.Name = "gr2BatchInputBrowseBtn";
            this.gr2BatchInputBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.gr2BatchInputBrowseBtn.TabIndex = 37;
            this.gr2BatchInputBrowseBtn.Text = "...";
            this.gr2BatchInputBrowseBtn.UseVisualStyleBackColor = true;
            this.gr2BatchInputBrowseBtn.Click += new System.EventHandler(this.GR2BatchInputBrowseBtn_Click);
            // 
            // gr2BatchOutputBrowseBtn
            // 
            this.gr2BatchOutputBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gr2BatchOutputBrowseBtn.Location = new System.Drawing.Point(822, 60);
            this.gr2BatchOutputBrowseBtn.Name = "gr2BatchOutputBrowseBtn";
            this.gr2BatchOutputBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.gr2BatchOutputBrowseBtn.TabIndex = 40;
            this.gr2BatchOutputBrowseBtn.Text = "...";
            this.gr2BatchOutputBrowseBtn.UseVisualStyleBackColor = true;
            this.gr2BatchOutputBrowseBtn.Click += new System.EventHandler(this.GR2BatchOutputBrowseBtn_Click);
            // 
            // gr2BatchProgressBar
            // 
            this.gr2BatchProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gr2BatchProgressBar.Location = new System.Drawing.Point(9, 104);
            this.gr2BatchProgressBar.Name = "gr2BatchProgressBar";
            this.gr2BatchProgressBar.Size = new System.Drawing.Size(700, 23);
            this.gr2BatchProgressBar.TabIndex = 47;
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(6, 88);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(51, 13);
            this.label23.TabIndex = 48;
            this.label23.Text = "Progress:";
            // 
            // gr2BatchInputFormat
            // 
            this.gr2BatchInputFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.gr2BatchInputFormat.FormattingEnabled = true;
            this.gr2BatchInputFormat.Items.AddRange(new object[] {
            "GR2",
            "DAE"});
            this.gr2BatchInputFormat.Location = new System.Drawing.Point(9, 19);
            this.gr2BatchInputFormat.Name = "gr2BatchInputFormat";
            this.gr2BatchInputFormat.Size = new System.Drawing.Size(67, 21);
            this.gr2BatchInputFormat.TabIndex = 46;
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(6, 3);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(66, 13);
            this.label22.TabIndex = 45;
            this.label22.Text = "Input format:";
            // 
            // gr2BatchOutputFormat
            // 
            this.gr2BatchOutputFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.gr2BatchOutputFormat.FormattingEnabled = true;
            this.gr2BatchOutputFormat.Items.AddRange(new object[] {
            "GR2",
            "DAE"});
            this.gr2BatchOutputFormat.Location = new System.Drawing.Point(9, 62);
            this.gr2BatchOutputFormat.Name = "gr2BatchOutputFormat";
            this.gr2BatchOutputFormat.Size = new System.Drawing.Size(67, 21);
            this.gr2BatchOutputFormat.TabIndex = 44;
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(6, 46);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(74, 13);
            this.label21.TabIndex = 43;
            this.label21.Text = "Output format:";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(79, 46);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(85, 13);
            this.label19.TabIndex = 39;
            this.label19.Text = "Output directory:";
            // 
            // gr2BatchConvertBtn
            // 
            this.gr2BatchConvertBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gr2BatchConvertBtn.Location = new System.Drawing.Point(723, 104);
            this.gr2BatchConvertBtn.Name = "gr2BatchConvertBtn";
            this.gr2BatchConvertBtn.Size = new System.Drawing.Size(141, 23);
            this.gr2BatchConvertBtn.TabIndex = 42;
            this.gr2BatchConvertBtn.Text = "Convert";
            this.gr2BatchConvertBtn.UseVisualStyleBackColor = true;
            this.gr2BatchConvertBtn.Click += new System.EventHandler(this.GR2BatchConvertBtn_Click);
            // 
            // gr2BatchInputDir
            // 
            this.gr2BatchInputDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gr2BatchInputDir.Location = new System.Drawing.Point(82, 19);
            this.gr2BatchInputDir.Name = "gr2BatchInputDir";
            this.gr2BatchInputDir.Size = new System.Drawing.Size(742, 20);
            this.gr2BatchInputDir.TabIndex = 35;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(79, 3);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(77, 13);
            this.label20.TabIndex = 36;
            this.label20.Text = "Input directory:";
            // 
            // gr2BatchOutputDir
            // 
            this.gr2BatchOutputDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gr2BatchOutputDir.Location = new System.Drawing.Point(82, 62);
            this.gr2BatchOutputDir.Name = "gr2BatchOutputDir";
            this.gr2BatchOutputDir.Size = new System.Drawing.Size(742, 20);
            this.gr2BatchOutputDir.TabIndex = 38;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox2.Controls.Add(this.exportColors);
            this.groupBox2.Controls.Add(this.flipUVs);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.exportableObjects);
            this.groupBox2.Controls.Add(this.exportUVs);
            this.groupBox2.Controls.Add(this.exportTangents);
            this.groupBox2.Controls.Add(this.exportNormals);
            this.groupBox2.Controls.Add(this.recalculateTangents);
            this.groupBox2.Controls.Add(this.filterUVs);
            this.groupBox2.Controls.Add(this.recalculateJointIWT);
            this.groupBox2.Controls.Add(this.deduplicateVertices);
            this.groupBox2.Controls.Add(this.recalculateNormals);
            this.groupBox2.Location = new System.Drawing.Point(7, 174);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(395, 410);
            this.groupBox2.TabIndex = 37;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Export Options";
            // 
            // exportColors
            // 
            this.exportColors.AutoSize = true;
            this.exportColors.Checked = true;
            this.exportColors.CheckState = System.Windows.Forms.CheckState.Checked;
            this.exportColors.Location = new System.Drawing.Point(9, 87);
            this.exportColors.Name = "exportColors";
            this.exportColors.Size = new System.Drawing.Size(87, 17);
            this.exportColors.TabIndex = 24;
            this.exportColors.Text = "Export colors";
            this.exportColors.UseVisualStyleBackColor = true;
            // 
            // flipUVs
            // 
            this.flipUVs.AutoSize = true;
            this.flipUVs.Checked = true;
            this.flipUVs.CheckState = System.Windows.Forms.CheckState.Checked;
            this.flipUVs.Location = new System.Drawing.Point(189, 87);
            this.flipUVs.Name = "flipUVs";
            this.flipUVs.Size = new System.Drawing.Size(65, 17);
            this.flipUVs.TabIndex = 23;
            this.flipUVs.Text = "Flip UVs";
            this.flipUVs.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 139);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(141, 13);
            this.label2.TabIndex = 22;
            this.label2.Text = "Select subobjects for export:";
            // 
            // exportableObjects
            // 
            this.exportableObjects.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.exportableObjects.CheckBoxes = true;
            this.exportableObjects.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.exportableName,
            this.exportableType});
            this.exportableObjects.Enabled = false;
            this.exportableObjects.FullRowSelect = true;
            this.exportableObjects.Location = new System.Drawing.Point(9, 160);
            this.exportableObjects.Name = "exportableObjects";
            this.exportableObjects.Size = new System.Drawing.Size(373, 233);
            this.exportableObjects.TabIndex = 21;
            this.exportableObjects.UseCompatibleStateImageBehavior = false;
            this.exportableObjects.View = System.Windows.Forms.View.Details;
            // 
            // exportableName
            // 
            this.exportableName.Text = "Name";
            this.exportableName.Width = 230;
            // 
            // exportableType
            // 
            this.exportableType.Text = "Type";
            this.exportableType.Width = 130;
            // 
            // exportUVs
            // 
            this.exportUVs.AutoSize = true;
            this.exportUVs.Checked = true;
            this.exportUVs.CheckState = System.Windows.Forms.CheckState.Checked;
            this.exportUVs.Location = new System.Drawing.Point(9, 64);
            this.exportUVs.Name = "exportUVs";
            this.exportUVs.Size = new System.Drawing.Size(79, 17);
            this.exportUVs.TabIndex = 13;
            this.exportUVs.Text = "Export UVs";
            this.exportUVs.UseVisualStyleBackColor = true;
            // 
            // exportTangents
            // 
            this.exportTangents.AutoSize = true;
            this.exportTangents.Checked = true;
            this.exportTangents.CheckState = System.Windows.Forms.CheckState.Checked;
            this.exportTangents.Location = new System.Drawing.Point(9, 41);
            this.exportTangents.Name = "exportTangents";
            this.exportTangents.Size = new System.Drawing.Size(144, 17);
            this.exportTangents.TabIndex = 12;
            this.exportTangents.Text = "Export tangent/bitangent";
            this.exportTangents.UseVisualStyleBackColor = true;
            // 
            // exportNormals
            // 
            this.exportNormals.AutoSize = true;
            this.exportNormals.Checked = true;
            this.exportNormals.CheckState = System.Windows.Forms.CheckState.Checked;
            this.exportNormals.Location = new System.Drawing.Point(9, 18);
            this.exportNormals.Name = "exportNormals";
            this.exportNormals.Size = new System.Drawing.Size(95, 17);
            this.exportNormals.TabIndex = 11;
            this.exportNormals.Text = "Export normals";
            this.exportNormals.UseVisualStyleBackColor = true;
            // 
            // recalculateTangents
            // 
            this.recalculateTangents.AutoSize = true;
            this.recalculateTangents.Location = new System.Drawing.Point(189, 41);
            this.recalculateTangents.Name = "recalculateTangents";
            this.recalculateTangents.Size = new System.Drawing.Size(177, 17);
            this.recalculateTangents.TabIndex = 7;
            this.recalculateTangents.Text = "Recalculate tangent / bitangent";
            this.recalculateTangents.UseVisualStyleBackColor = true;
            // 
            // filterUVs
            // 
            this.filterUVs.AutoSize = true;
            this.filterUVs.Location = new System.Drawing.Point(189, 110);
            this.filterUVs.Name = "filterUVs";
            this.filterUVs.Size = new System.Drawing.Size(71, 17);
            this.filterUVs.TabIndex = 16;
            this.filterUVs.Text = "Filter UVs";
            this.filterUVs.UseVisualStyleBackColor = true;
            // 
            // recalculateJointIWT
            // 
            this.recalculateJointIWT.AutoSize = true;
            this.recalculateJointIWT.Location = new System.Drawing.Point(189, 64);
            this.recalculateJointIWT.Name = "recalculateJointIWT";
            this.recalculateJointIWT.Size = new System.Drawing.Size(199, 17);
            this.recalculateJointIWT.TabIndex = 18;
            this.recalculateJointIWT.Text = "Recalculate inverse world transforms";
            this.recalculateJointIWT.UseVisualStyleBackColor = true;
            // 
            // deduplicateVertices
            // 
            this.deduplicateVertices.AutoSize = true;
            this.deduplicateVertices.Checked = true;
            this.deduplicateVertices.CheckState = System.Windows.Forms.CheckState.Checked;
            this.deduplicateVertices.Location = new System.Drawing.Point(9, 110);
            this.deduplicateVertices.Name = "deduplicateVertices";
            this.deduplicateVertices.Size = new System.Drawing.Size(123, 17);
            this.deduplicateVertices.TabIndex = 15;
            this.deduplicateVertices.Text = "Deduplicate vertices";
            this.deduplicateVertices.UseVisualStyleBackColor = true;
            // 
            // recalculateNormals
            // 
            this.recalculateNormals.AutoSize = true;
            this.recalculateNormals.Location = new System.Drawing.Point(189, 18);
            this.recalculateNormals.Name = "recalculateNormals";
            this.recalculateNormals.Size = new System.Drawing.Size(122, 17);
            this.recalculateNormals.TabIndex = 6;
            this.recalculateNormals.Text = "Recalculate normals";
            this.recalculateNormals.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.rigid);
            this.groupBox1.Controls.Add(this.applyBasisTransforms);
            this.groupBox1.Controls.Add(this.conformantGR2BrowseBtn);
            this.groupBox1.Controls.Add(this.conformantGR2Path);
            this.groupBox1.Controls.Add(this.conformToOriginal);
            this.groupBox1.Controls.Add(this.buildDummySkeleton);
            this.groupBox1.Controls.Add(this.use16bitIndex);
            this.groupBox1.Controls.Add(this.forceLegacyVersion);
            this.groupBox1.Controls.Add(this.resourceFormats);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(418, 174);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(476, 410);
            this.groupBox1.TabIndex = 36;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "GR2 Export Options";
            // 
            // applyBasisTransforms
            // 
            this.applyBasisTransforms.AutoSize = true;
            this.applyBasisTransforms.Checked = true;
            this.applyBasisTransforms.CheckState = System.Windows.Forms.CheckState.Checked;
            this.applyBasisTransforms.Location = new System.Drawing.Point(245, 22);
            this.applyBasisTransforms.Name = "applyBasisTransforms";
            this.applyBasisTransforms.Size = new System.Drawing.Size(100, 17);
            this.applyBasisTransforms.TabIndex = 26;
            this.applyBasisTransforms.Text = "Convert to Y-up";
            this.applyBasisTransforms.UseVisualStyleBackColor = true;
            // 
            // conformantGR2BrowseBtn
            // 
            this.conformantGR2BrowseBtn.Enabled = false;
            this.conformantGR2BrowseBtn.Location = new System.Drawing.Point(419, 109);
            this.conformantGR2BrowseBtn.Name = "conformantGR2BrowseBtn";
            this.conformantGR2BrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.conformantGR2BrowseBtn.TabIndex = 25;
            this.conformantGR2BrowseBtn.Text = "...";
            this.conformantGR2BrowseBtn.UseVisualStyleBackColor = true;
            this.conformantGR2BrowseBtn.Click += new System.EventHandler(this.conformantSkeletonBrowseBtn_Click);
            // 
            // conformantGR2Path
            // 
            this.conformantGR2Path.Enabled = false;
            this.conformantGR2Path.Location = new System.Drawing.Point(15, 111);
            this.conformantGR2Path.Name = "conformantGR2Path";
            this.conformantGR2Path.Size = new System.Drawing.Size(405, 20);
            this.conformantGR2Path.TabIndex = 24;
            // 
            // conformToOriginal
            // 
            this.conformToOriginal.AutoSize = true;
            this.conformToOriginal.Enabled = false;
            this.conformToOriginal.Location = new System.Drawing.Point(15, 91);
            this.conformToOriginal.Name = "conformToOriginal";
            this.conformToOriginal.Size = new System.Drawing.Size(141, 17);
            this.conformToOriginal.TabIndex = 23;
            this.conformToOriginal.Text = "Conform to original GR2:";
            this.conformToOriginal.UseVisualStyleBackColor = true;
            this.conformToOriginal.Click += new System.EventHandler(this.conformToSkeleton_CheckedChanged);
            // 
            // buildDummySkeleton
            // 
            this.buildDummySkeleton.AutoSize = true;
            this.buildDummySkeleton.Checked = true;
            this.buildDummySkeleton.CheckState = System.Windows.Forms.CheckState.Checked;
            this.buildDummySkeleton.Location = new System.Drawing.Point(15, 68);
            this.buildDummySkeleton.Name = "buildDummySkeleton";
            this.buildDummySkeleton.Size = new System.Drawing.Size(136, 17);
            this.buildDummySkeleton.TabIndex = 22;
            this.buildDummySkeleton.Text = "Create dummy skeleton";
            this.buildDummySkeleton.UseVisualStyleBackColor = true;
            // 
            // use16bitIndex
            // 
            this.use16bitIndex.AutoSize = true;
            this.use16bitIndex.Location = new System.Drawing.Point(15, 45);
            this.use16bitIndex.Name = "use16bitIndex";
            this.use16bitIndex.Size = new System.Drawing.Size(142, 17);
            this.use16bitIndex.TabIndex = 18;
            this.use16bitIndex.Text = "Store compact tri indices";
            this.use16bitIndex.UseVisualStyleBackColor = true;
            // 
            // forceLegacyVersion
            // 
            this.forceLegacyVersion.AutoSize = true;
            this.forceLegacyVersion.Location = new System.Drawing.Point(15, 22);
            this.forceLegacyVersion.Name = "forceLegacyVersion";
            this.forceLegacyVersion.Size = new System.Drawing.Size(167, 17);
            this.forceLegacyVersion.TabIndex = 17;
            this.forceLegacyVersion.Text = "Force legacy GR2 version tag";
            this.forceLegacyVersion.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 139);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(139, 13);
            this.label1.TabIndex = 15;
            this.label1.Text = "Customize resource formats:";
            // 
            // conformSkeletonFileDlg
            // 
            this.conformSkeletonFileDlg.Filter = "Granny GR2|*.gr2";
            this.conformSkeletonFileDlg.Title = "Select Conforming Skeleton File";
            // 
            // outputFileDlg
            // 
            this.outputFileDlg.Filter = "COLLADA/GR2 files|*.dae;*.gr2";
            this.outputFileDlg.Title = "Select Output File";
            // 
            // inputFileDlg
            // 
            this.inputFileDlg.Filter = "COLLADA/GR2 files|*.dae;*.gr2";
            this.inputFileDlg.Title = "Select Input File";
            // 
            // rigid
            // 
            this.rigid.AutoSize = true;
            this.rigid.Location = new System.Drawing.Point(245, 45);
            this.rigid.Name = "rigid";
            this.rigid.Size = new System.Drawing.Size(50, 17);
            this.rigid.TabIndex = 27;
            this.rigid.Text = "Rigid";
            this.rigid.UseVisualStyleBackColor = true;
            // 
            // resourceFormats
            // 
            this.resourceFormats.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceFormats.FullRowSelect = true;
            this.resourceFormats.Location = new System.Drawing.Point(15, 160);
            this.resourceFormats.Name = "resourceFormats";
            this.resourceFormats.Size = new System.Drawing.Size(445, 233);
            this.resourceFormats.TabIndex = 16;
            this.resourceFormats.UseCompatibleStateImageBehavior = false;
            this.resourceFormats.View = System.Windows.Forms.View.Details;
            // 
            // GR2Pane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gr2ModeTabControl);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "GR2Pane";
            this.Size = new System.Drawing.Size(901, 592);
            this.gr2ModeTabControl.ResumeLayout(false);
            this.gr2SingleFileTab.ResumeLayout(false);
            this.gr2SingleFileTab.PerformLayout();
            this.gr2BatchTab.ResumeLayout(false);
            this.gr2BatchTab.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl gr2ModeTabControl;
        private System.Windows.Forms.TabPage gr2SingleFileTab;
        private System.Windows.Forms.Label lblOutputPath;
        private System.Windows.Forms.Button saveOutputBtn;
        private System.Windows.Forms.TextBox inputPath;
        private System.Windows.Forms.Label lblSrcPath;
        private System.Windows.Forms.Button inputFileBrowseBtn;
        private System.Windows.Forms.Button loadInputBtn;
        private System.Windows.Forms.TextBox outputPath;
        private System.Windows.Forms.Button outputFileBrowserBtn;
        private System.Windows.Forms.TabPage gr2BatchTab;
        private System.Windows.Forms.Label gr2BatchProgressLabel;
        private System.Windows.Forms.Button gr2BatchInputBrowseBtn;
        private System.Windows.Forms.Button gr2BatchOutputBrowseBtn;
        private System.Windows.Forms.ProgressBar gr2BatchProgressBar;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.ComboBox gr2BatchInputFormat;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.ComboBox gr2BatchOutputFormat;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Button gr2BatchConvertBtn;
        private System.Windows.Forms.TextBox gr2BatchInputDir;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.TextBox gr2BatchOutputDir;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListView exportableObjects;
        private System.Windows.Forms.ColumnHeader exportableName;
        private System.Windows.Forms.ColumnHeader exportableType;
        private System.Windows.Forms.CheckBox exportUVs;
        private System.Windows.Forms.CheckBox exportTangents;
        private System.Windows.Forms.CheckBox exportNormals;
        private System.Windows.Forms.CheckBox recalculateTangents;
        private System.Windows.Forms.CheckBox filterUVs;
        private System.Windows.Forms.CheckBox recalculateJointIWT;
        private System.Windows.Forms.CheckBox deduplicateVertices;
        private System.Windows.Forms.CheckBox recalculateNormals;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox applyBasisTransforms;
        private System.Windows.Forms.Button conformantGR2BrowseBtn;
        private System.Windows.Forms.TextBox conformantGR2Path;
        private System.Windows.Forms.CheckBox conformToOriginal;
        private System.Windows.Forms.CheckBox buildDummySkeleton;
        private System.Windows.Forms.CheckBox forceLegacyVersion;
        private ExportItemSelection resourceFormats;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.FolderBrowserDialog gr2OutputDirDlg;
        private System.Windows.Forms.FolderBrowserDialog gr2InputDirDlg;
        private System.Windows.Forms.OpenFileDialog conformSkeletonFileDlg;
        private System.Windows.Forms.SaveFileDialog outputFileDlg;
        private System.Windows.Forms.OpenFileDialog inputFileDlg;
        internal System.Windows.Forms.CheckBox use16bitIndex;
        private System.Windows.Forms.CheckBox flipUVs;
        private System.Windows.Forms.CheckBox exportColors;
        private System.Windows.Forms.CheckBox rigid;
    }
}
