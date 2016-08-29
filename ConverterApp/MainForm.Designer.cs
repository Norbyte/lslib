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
            this.inputFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.outputFileDlg = new System.Windows.Forms.SaveFileDialog();
            this.conformSkeletonFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.gr2Tab = new System.Windows.Forms.TabPage();
            this.saveOutputBtn = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
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
            this.gr2Game = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.applyBasisTransforms = new System.Windows.Forms.CheckBox();
            this.conformantGR2BrowseBtn = new System.Windows.Forms.Button();
            this.conformantGR2Path = new System.Windows.Forms.TextBox();
            this.conformToOriginal = new System.Windows.Forms.CheckBox();
            this.buildDummySkeleton = new System.Windows.Forms.CheckBox();
            this.use16bitIndex = new System.Windows.Forms.CheckBox();
            this.forceLegacyVersion = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.loadInputBtn = new System.Windows.Forms.Button();
            this.outputFileBrowserBtn = new System.Windows.Forms.Button();
            this.lblOutputPath = new System.Windows.Forms.Label();
            this.outputPath = new System.Windows.Forms.TextBox();
            this.inputFileBrowseBtn = new System.Windows.Forms.Button();
            this.lblSrcPath = new System.Windows.Forms.Label();
            this.inputPath = new System.Windows.Forms.TextBox();
            this.packageTab = new System.Windows.Forms.TabPage();
            this.packageVersion = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.compressionMethod = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.exportPathBrowseBtn = new System.Windows.Forms.Button();
            this.packageBrowseBtn = new System.Windows.Forms.Button();
            this.packageProgressLabel = new System.Windows.Forms.Label();
            this.createPackageBtn = new System.Windows.Forms.Button();
            this.packageProgress = new System.Windows.Forms.ProgressBar();
            this.label5 = new System.Windows.Forms.Label();
            this.packagePath = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.extractPackageBtn = new System.Windows.Forms.Button();
            this.extractionPath = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.objectTab = new System.Windows.Forms.TabPage();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.resourceProgressLabel = new System.Windows.Forms.Label();
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
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.resourceConvertBtn = new System.Windows.Forms.Button();
            this.resourceOutputBrowseBtn = new System.Windows.Forms.Button();
            this.resourceInputBrowseBtn = new System.Windows.Forms.Button();
            this.resourceInputPath = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.resourceOutputPath = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.databaseGrid = new System.Windows.Forms.DataGridView();
            this.databaseSelectorCb = new System.Windows.Forms.ComboBox();
            this.label18 = new System.Windows.Forms.Label();
            this.loadStoryBtn = new System.Windows.Forms.Button();
            this.decompileStoryBtn = new System.Windows.Forms.Button();
            this.storyFilePath = new System.Windows.Forms.TextBox();
            this.goalPathBrowseBtn = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.goalPath = new System.Windows.Forms.TextBox();
            this.storyFileBrowseBtn = new System.Windows.Forms.Button();
            this.label10 = new System.Windows.Forms.Label();
            this.packageFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.exportPathDlg = new System.Windows.Forms.FolderBrowserDialog();
            this.resourceInputFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.resourceOutputFileDlg = new System.Windows.Forms.SaveFileDialog();
            this.storyPathDlg = new System.Windows.Forms.OpenFileDialog();
            this.goalPathDlg = new System.Windows.Forms.FolderBrowserDialog();
            this.resourceInputPathDlg = new System.Windows.Forms.FolderBrowserDialog();
            this.resourceOutputPathDlg = new System.Windows.Forms.FolderBrowserDialog();
            this.gr2ModeTabControl = new System.Windows.Forms.TabControl();
            this.gr2SingleFileTab = new System.Windows.Forms.TabPage();
            this.gr2BatchTab = new System.Windows.Forms.TabPage();
            this.label19 = new System.Windows.Forms.Label();
            this.gr2BatchConvertBtn = new System.Windows.Forms.Button();
            this.gr2BatchInputDir = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.gr2BatchInputBrowseBtn = new System.Windows.Forms.Button();
            this.gr2BatchOutputDir = new System.Windows.Forms.TextBox();
            this.gr2BatchOutputBrowseBtn = new System.Windows.Forms.Button();
            this.label21 = new System.Windows.Forms.Label();
            this.gr2BatchOutputFormat = new System.Windows.Forms.ComboBox();
            this.gr2InputDirDlg = new System.Windows.Forms.FolderBrowserDialog();
            this.gr2OutputDirDlg = new System.Windows.Forms.FolderBrowserDialog();
            this.resourceFormats = new ConverterApp.ExportItemSelection();
            this.gr2BatchInputFormat = new System.Windows.Forms.ComboBox();
            this.label22 = new System.Windows.Forms.Label();
            this.gr2BatchProgressBar = new System.Windows.Forms.ProgressBar();
            this.label23 = new System.Windows.Forms.Label();
            this.gr2BatchProgressLabel = new System.Windows.Forms.Label();
            this.tabControl.SuspendLayout();
            this.gr2Tab.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.packageTab.SuspendLayout();
            this.objectTab.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.databaseGrid)).BeginInit();
            this.gr2ModeTabControl.SuspendLayout();
            this.gr2SingleFileTab.SuspendLayout();
            this.gr2BatchTab.SuspendLayout();
            this.SuspendLayout();
            // 
            // inputFileDlg
            // 
            this.inputFileDlg.Filter = "COLLADA/GR2 files|*.dae;*.gr2";
            this.inputFileDlg.Title = "Select Input File";
            this.inputFileDlg.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk);
            // 
            // outputFileDlg
            // 
            this.outputFileDlg.Filter = "COLLADA/GR2 files|*.dae;*.gr2";
            this.outputFileDlg.Title = "Select Output File";
            // 
            // conformSkeletonFileDlg
            // 
            this.conformSkeletonFileDlg.Filter = "Granny GR2|*.gr2";
            this.conformSkeletonFileDlg.Title = "Select Conforming Skeleton File";
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.gr2Tab);
            this.tabControl.Controls.Add(this.packageTab);
            this.tabControl.Controls.Add(this.objectTab);
            this.tabControl.Controls.Add(this.tabPage1);
            this.tabControl.Location = new System.Drawing.Point(12, 12);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(917, 629);
            this.tabControl.TabIndex = 0;
            // 
            // gr2Tab
            // 
            this.gr2Tab.Controls.Add(this.gr2ModeTabControl);
            this.gr2Tab.Controls.Add(this.groupBox2);
            this.gr2Tab.Controls.Add(this.groupBox1);
            this.gr2Tab.Location = new System.Drawing.Point(4, 22);
            this.gr2Tab.Name = "gr2Tab";
            this.gr2Tab.Padding = new System.Windows.Forms.Padding(3);
            this.gr2Tab.Size = new System.Drawing.Size(909, 603);
            this.gr2Tab.TabIndex = 0;
            this.gr2Tab.Text = "GR2 Tools";
            this.gr2Tab.UseVisualStyleBackColor = true;
            // 
            // saveOutputBtn
            // 
            this.saveOutputBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.saveOutputBtn.Enabled = false;
            this.saveOutputBtn.Location = new System.Drawing.Point(721, 60);
            this.saveOutputBtn.Name = "saveOutputBtn";
            this.saveOutputBtn.Size = new System.Drawing.Size(141, 23);
            this.saveOutputBtn.TabIndex = 34;
            this.saveOutputBtn.Text = "Export";
            this.saveOutputBtn.UseVisualStyleBackColor = true;
            this.saveOutputBtn.Click += new System.EventHandler(this.saveOutputBtn_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
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
            this.groupBox2.Location = new System.Drawing.Point(9, 178);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(395, 419);
            this.groupBox2.TabIndex = 33;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Export Options";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 151);
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
            this.exportableObjects.Location = new System.Drawing.Point(9, 167);
            this.exportableObjects.Name = "exportableObjects";
            this.exportableObjects.Size = new System.Drawing.Size(373, 238);
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
            this.filterUVs.Enabled = false;
            this.filterUVs.Location = new System.Drawing.Point(9, 110);
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
            this.recalculateJointIWT.CheckedChanged += new System.EventHandler(this.recalculateJointIWT_CheckedChanged);
            // 
            // deduplicateVertices
            // 
            this.deduplicateVertices.AutoSize = true;
            this.deduplicateVertices.Checked = true;
            this.deduplicateVertices.CheckState = System.Windows.Forms.CheckState.Checked;
            this.deduplicateVertices.Location = new System.Drawing.Point(9, 87);
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
            this.groupBox1.Controls.Add(this.gr2Game);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.applyBasisTransforms);
            this.groupBox1.Controls.Add(this.conformantGR2BrowseBtn);
            this.groupBox1.Controls.Add(this.conformantGR2Path);
            this.groupBox1.Controls.Add(this.conformToOriginal);
            this.groupBox1.Controls.Add(this.buildDummySkeleton);
            this.groupBox1.Controls.Add(this.use16bitIndex);
            this.groupBox1.Controls.Add(this.forceLegacyVersion);
            this.groupBox1.Controls.Add(this.resourceFormats);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(420, 178);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(476, 419);
            this.groupBox1.TabIndex = 32;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "GR2 Export Options";
            // 
            // gr2Game
            // 
            this.gr2Game.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gr2Game.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.gr2Game.FormattingEnabled = true;
            this.gr2Game.Items.AddRange(new object[] {
            "Divinity: Original Sin (32-bit)",
            "Divinity: Original Sin EE (64-bit)"});
            this.gr2Game.Location = new System.Drawing.Point(78, 20);
            this.gr2Game.Name = "gr2Game";
            this.gr2Game.Size = new System.Drawing.Size(356, 21);
            this.gr2Game.TabIndex = 28;
            this.gr2Game.SelectedIndexChanged += new System.EventHandler(this.gr2Game_SelectedIndexChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(16, 22);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(55, 13);
            this.label7.TabIndex = 27;
            this.label7.Text = "Export for:";
            // 
            // applyBasisTransforms
            // 
            this.applyBasisTransforms.AutoSize = true;
            this.applyBasisTransforms.Checked = true;
            this.applyBasisTransforms.CheckState = System.Windows.Forms.CheckState.Checked;
            this.applyBasisTransforms.Location = new System.Drawing.Point(249, 50);
            this.applyBasisTransforms.Name = "applyBasisTransforms";
            this.applyBasisTransforms.Size = new System.Drawing.Size(100, 17);
            this.applyBasisTransforms.TabIndex = 26;
            this.applyBasisTransforms.Text = "Convert to Y-up";
            this.applyBasisTransforms.UseVisualStyleBackColor = true;
            // 
            // conformantGR2BrowseBtn
            // 
            this.conformantGR2BrowseBtn.Enabled = false;
            this.conformantGR2BrowseBtn.Location = new System.Drawing.Point(423, 137);
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
            this.conformantGR2Path.Location = new System.Drawing.Point(19, 139);
            this.conformantGR2Path.Name = "conformantGR2Path";
            this.conformantGR2Path.Size = new System.Drawing.Size(405, 20);
            this.conformantGR2Path.TabIndex = 24;
            // 
            // conformToOriginal
            // 
            this.conformToOriginal.AutoSize = true;
            this.conformToOriginal.Enabled = false;
            this.conformToOriginal.Location = new System.Drawing.Point(19, 119);
            this.conformToOriginal.Name = "conformToOriginal";
            this.conformToOriginal.Size = new System.Drawing.Size(141, 17);
            this.conformToOriginal.TabIndex = 23;
            this.conformToOriginal.Text = "Conform to original GR2:";
            this.conformToOriginal.UseVisualStyleBackColor = true;
            this.conformToOriginal.CheckedChanged += new System.EventHandler(this.conformToSkeleton_CheckedChanged);
            // 
            // buildDummySkeleton
            // 
            this.buildDummySkeleton.AutoSize = true;
            this.buildDummySkeleton.Checked = true;
            this.buildDummySkeleton.CheckState = System.Windows.Forms.CheckState.Checked;
            this.buildDummySkeleton.Location = new System.Drawing.Point(19, 96);
            this.buildDummySkeleton.Name = "buildDummySkeleton";
            this.buildDummySkeleton.Size = new System.Drawing.Size(136, 17);
            this.buildDummySkeleton.TabIndex = 22;
            this.buildDummySkeleton.Text = "Create dummy skeleton";
            this.buildDummySkeleton.UseVisualStyleBackColor = true;
            // 
            // use16bitIndex
            // 
            this.use16bitIndex.AutoSize = true;
            this.use16bitIndex.Location = new System.Drawing.Point(19, 73);
            this.use16bitIndex.Name = "use16bitIndex";
            this.use16bitIndex.Size = new System.Drawing.Size(142, 17);
            this.use16bitIndex.TabIndex = 18;
            this.use16bitIndex.Text = "Store compact tri indices";
            this.use16bitIndex.UseVisualStyleBackColor = true;
            // 
            // forceLegacyVersion
            // 
            this.forceLegacyVersion.AutoSize = true;
            this.forceLegacyVersion.Location = new System.Drawing.Point(19, 50);
            this.forceLegacyVersion.Name = "forceLegacyVersion";
            this.forceLegacyVersion.Size = new System.Drawing.Size(167, 17);
            this.forceLegacyVersion.TabIndex = 17;
            this.forceLegacyVersion.Text = "Force legacy GR2 version tag";
            this.forceLegacyVersion.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 167);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(139, 13);
            this.label1.TabIndex = 15;
            this.label1.Text = "Customize resource formats:";
            // 
            // loadInputBtn
            // 
            this.loadInputBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.loadInputBtn.Location = new System.Drawing.Point(721, 19);
            this.loadInputBtn.Name = "loadInputBtn";
            this.loadInputBtn.Size = new System.Drawing.Size(141, 23);
            this.loadInputBtn.TabIndex = 31;
            this.loadInputBtn.Text = "Import";
            this.loadInputBtn.UseVisualStyleBackColor = true;
            this.loadInputBtn.Click += new System.EventHandler(this.loadInputBtn_Click);
            // 
            // outputFileBrowserBtn
            // 
            this.outputFileBrowserBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.outputFileBrowserBtn.Location = new System.Drawing.Point(666, 60);
            this.outputFileBrowserBtn.Name = "outputFileBrowserBtn";
            this.outputFileBrowserBtn.Size = new System.Drawing.Size(41, 23);
            this.outputFileBrowserBtn.TabIndex = 30;
            this.outputFileBrowserBtn.Text = "...";
            this.outputFileBrowserBtn.UseVisualStyleBackColor = true;
            this.outputFileBrowserBtn.Click += new System.EventHandler(this.outputFileBrowserBtn_Click);
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
            // outputPath
            // 
            this.outputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.outputPath.Location = new System.Drawing.Point(9, 62);
            this.outputPath.Name = "outputPath";
            this.outputPath.Size = new System.Drawing.Size(659, 20);
            this.outputPath.TabIndex = 28;
            // 
            // inputFileBrowseBtn
            // 
            this.inputFileBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.inputFileBrowseBtn.Location = new System.Drawing.Point(666, 17);
            this.inputFileBrowseBtn.Name = "inputFileBrowseBtn";
            this.inputFileBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.inputFileBrowseBtn.TabIndex = 27;
            this.inputFileBrowseBtn.Text = "...";
            this.inputFileBrowseBtn.UseVisualStyleBackColor = true;
            this.inputFileBrowseBtn.Click += new System.EventHandler(this.inputFileBrowseBtn_Click);
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
            // inputPath
            // 
            this.inputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.inputPath.Location = new System.Drawing.Point(9, 19);
            this.inputPath.Name = "inputPath";
            this.inputPath.Size = new System.Drawing.Size(659, 20);
            this.inputPath.TabIndex = 25;
            // 
            // packageTab
            // 
            this.packageTab.Controls.Add(this.packageVersion);
            this.packageTab.Controls.Add(this.label8);
            this.packageTab.Controls.Add(this.compressionMethod);
            this.packageTab.Controls.Add(this.label6);
            this.packageTab.Controls.Add(this.exportPathBrowseBtn);
            this.packageTab.Controls.Add(this.packageBrowseBtn);
            this.packageTab.Controls.Add(this.packageProgressLabel);
            this.packageTab.Controls.Add(this.createPackageBtn);
            this.packageTab.Controls.Add(this.packageProgress);
            this.packageTab.Controls.Add(this.label5);
            this.packageTab.Controls.Add(this.packagePath);
            this.packageTab.Controls.Add(this.label4);
            this.packageTab.Controls.Add(this.extractPackageBtn);
            this.packageTab.Controls.Add(this.extractionPath);
            this.packageTab.Controls.Add(this.label3);
            this.packageTab.Location = new System.Drawing.Point(4, 22);
            this.packageTab.Name = "packageTab";
            this.packageTab.Padding = new System.Windows.Forms.Padding(3);
            this.packageTab.Size = new System.Drawing.Size(909, 603);
            this.packageTab.TabIndex = 1;
            this.packageTab.Text = "PAK / LSV Tools";
            this.packageTab.UseVisualStyleBackColor = true;
            // 
            // packageVersion
            // 
            this.packageVersion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.packageVersion.FormattingEnabled = true;
            this.packageVersion.Items.AddRange(new object[] {
            "V13 (Divinity Original Sin: EE)",
            "V10 (Divinity Original Sin)",
            "V9 (Divinity Original Sin Old)",
            "V7 (Divinity Original Sin Old)"});
            this.packageVersion.Location = new System.Drawing.Point(10, 114);
            this.packageVersion.Name = "packageVersion";
            this.packageVersion.Size = new System.Drawing.Size(187, 21);
            this.packageVersion.TabIndex = 49;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(7, 97);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(45, 13);
            this.label8.TabIndex = 48;
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
            this.compressionMethod.Location = new System.Drawing.Point(221, 114);
            this.compressionMethod.Name = "compressionMethod";
            this.compressionMethod.Size = new System.Drawing.Size(187, 21);
            this.compressionMethod.TabIndex = 47;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(218, 97);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(70, 13);
            this.label6.TabIndex = 46;
            this.label6.Text = "Compression:";
            // 
            // exportPathBrowseBtn
            // 
            this.exportPathBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.exportPathBrowseBtn.Location = new System.Drawing.Point(657, 63);
            this.exportPathBrowseBtn.Name = "exportPathBrowseBtn";
            this.exportPathBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.exportPathBrowseBtn.TabIndex = 40;
            this.exportPathBrowseBtn.Text = "...";
            this.exportPathBrowseBtn.UseVisualStyleBackColor = true;
            this.exportPathBrowseBtn.Click += new System.EventHandler(this.exportPathBrowseBtn_Click);
            // 
            // packageBrowseBtn
            // 
            this.packageBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.packageBrowseBtn.Location = new System.Drawing.Point(657, 17);
            this.packageBrowseBtn.Name = "packageBrowseBtn";
            this.packageBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.packageBrowseBtn.TabIndex = 37;
            this.packageBrowseBtn.Text = "...";
            this.packageBrowseBtn.UseVisualStyleBackColor = true;
            this.packageBrowseBtn.Click += new System.EventHandler(this.packageBrowseBtn_Click);
            // 
            // packageProgressLabel
            // 
            this.packageProgressLabel.AutoSize = true;
            this.packageProgressLabel.Location = new System.Drawing.Point(63, 144);
            this.packageProgressLabel.Name = "packageProgressLabel";
            this.packageProgressLabel.Size = new System.Drawing.Size(0, 13);
            this.packageProgressLabel.TabIndex = 44;
            // 
            // createPackageBtn
            // 
            this.createPackageBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.createPackageBtn.Location = new System.Drawing.Point(725, 19);
            this.createPackageBtn.Name = "createPackageBtn";
            this.createPackageBtn.Size = new System.Drawing.Size(151, 23);
            this.createPackageBtn.TabIndex = 45;
            this.createPackageBtn.Text = "Create Package";
            this.createPackageBtn.UseVisualStyleBackColor = true;
            this.createPackageBtn.Click += new System.EventHandler(this.createPackageBtn_Click);
            // 
            // packageProgress
            // 
            this.packageProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.packageProgress.Location = new System.Drawing.Point(9, 160);
            this.packageProgress.Name = "packageProgress";
            this.packageProgress.Size = new System.Drawing.Size(883, 23);
            this.packageProgress.TabIndex = 42;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 144);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(51, 13);
            this.label5.TabIndex = 43;
            this.label5.Text = "Progress:";
            // 
            // packagePath
            // 
            this.packagePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.packagePath.Location = new System.Drawing.Point(9, 19);
            this.packagePath.Name = "packagePath";
            this.packagePath.Size = new System.Drawing.Size(650, 20);
            this.packagePath.TabIndex = 35;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 3);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(77, 13);
            this.label4.TabIndex = 36;
            this.label4.Text = "Package path:";
            // 
            // extractPackageBtn
            // 
            this.extractPackageBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.extractPackageBtn.Location = new System.Drawing.Point(725, 62);
            this.extractPackageBtn.Name = "extractPackageBtn";
            this.extractPackageBtn.Size = new System.Drawing.Size(151, 23);
            this.extractPackageBtn.TabIndex = 41;
            this.extractPackageBtn.Text = "Extract Package";
            this.extractPackageBtn.UseVisualStyleBackColor = true;
            this.extractPackageBtn.Click += new System.EventHandler(this.extractPackageBtn_Click);
            // 
            // extractionPath
            // 
            this.extractionPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.extractionPath.Location = new System.Drawing.Point(9, 65);
            this.extractionPath.Name = "extractionPath";
            this.extractionPath.Size = new System.Drawing.Size(650, 20);
            this.extractionPath.TabIndex = 38;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 49);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(121, 13);
            this.label3.TabIndex = 39;
            this.label3.Text = "Extracion / source path:";
            // 
            // objectTab
            // 
            this.objectTab.Controls.Add(this.groupBox5);
            this.objectTab.Controls.Add(this.groupBox4);
            this.objectTab.Location = new System.Drawing.Point(4, 22);
            this.objectTab.Name = "objectTab";
            this.objectTab.Padding = new System.Windows.Forms.Padding(3);
            this.objectTab.Size = new System.Drawing.Size(909, 603);
            this.objectTab.TabIndex = 2;
            this.objectTab.Text = "LSX / LSB / LSF Tools";
            this.objectTab.UseVisualStyleBackColor = true;
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
            this.groupBox5.Location = new System.Drawing.Point(7, 169);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(895, 207);
            this.groupBox5.TabIndex = 60;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Batch Convert";
            // 
            // resourceProgressLabel
            // 
            this.resourceProgressLabel.AutoSize = true;
            this.resourceProgressLabel.Location = new System.Drawing.Point(64, 156);
            this.resourceProgressLabel.Name = "resourceProgressLabel";
            this.resourceProgressLabel.Size = new System.Drawing.Size(0, 13);
            this.resourceProgressLabel.TabIndex = 67;
            // 
            // resourceConversionProgress
            // 
            this.resourceConversionProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceConversionProgress.Location = new System.Drawing.Point(11, 172);
            this.resourceConversionProgress.Name = "resourceConversionProgress";
            this.resourceConversionProgress.Size = new System.Drawing.Size(878, 23);
            this.resourceConversionProgress.TabIndex = 65;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(8, 156);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(51, 13);
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
            "LSF (binary) file"});
            this.resourceOutputFormatCb.Location = new System.Drawing.Point(180, 128);
            this.resourceOutputFormatCb.Name = "resourceOutputFormatCb";
            this.resourceOutputFormatCb.Size = new System.Drawing.Size(151, 21);
            this.resourceOutputFormatCb.TabIndex = 64;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(177, 111);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(74, 13);
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
            "LSF (binary) file"});
            this.resourceInputFormatCb.Location = new System.Drawing.Point(11, 128);
            this.resourceInputFormatCb.Name = "resourceInputFormatCb";
            this.resourceInputFormatCb.Size = new System.Drawing.Size(151, 21);
            this.resourceInputFormatCb.TabIndex = 62;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(8, 111);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(66, 13);
            this.label15.TabIndex = 61;
            this.label15.Text = "Input format:";
            // 
            // resourceBulkConvertBtn
            // 
            this.resourceBulkConvertBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceBulkConvertBtn.Location = new System.Drawing.Point(348, 128);
            this.resourceBulkConvertBtn.Name = "resourceBulkConvertBtn";
            this.resourceBulkConvertBtn.Size = new System.Drawing.Size(151, 23);
            this.resourceBulkConvertBtn.TabIndex = 60;
            this.resourceBulkConvertBtn.Text = "Convert";
            this.resourceBulkConvertBtn.UseVisualStyleBackColor = true;
            this.resourceBulkConvertBtn.Click += new System.EventHandler(this.resourceBulkConvertBtn_Click);
            // 
            // resourceOutputPathBrowseBtn
            // 
            this.resourceOutputPathBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceOutputPathBrowseBtn.Location = new System.Drawing.Point(848, 83);
            this.resourceOutputPathBrowseBtn.Name = "resourceOutputPathBrowseBtn";
            this.resourceOutputPathBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.resourceOutputPathBrowseBtn.TabIndex = 59;
            this.resourceOutputPathBrowseBtn.Text = "...";
            this.resourceOutputPathBrowseBtn.UseVisualStyleBackColor = true;
            this.resourceOutputPathBrowseBtn.Click += new System.EventHandler(this.resourceOutputPathBrowseBtn_Click);
            // 
            // resourceInputPathBrowseBtn
            // 
            this.resourceInputPathBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceInputPathBrowseBtn.Location = new System.Drawing.Point(848, 35);
            this.resourceInputPathBrowseBtn.Name = "resourceInputPathBrowseBtn";
            this.resourceInputPathBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.resourceInputPathBrowseBtn.TabIndex = 56;
            this.resourceInputPathBrowseBtn.Text = "...";
            this.resourceInputPathBrowseBtn.UseVisualStyleBackColor = true;
            this.resourceInputPathBrowseBtn.Click += new System.EventHandler(this.resourceInputPathBrowseBtn_Click);
            // 
            // resourceInputDir
            // 
            this.resourceInputDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceInputDir.Location = new System.Drawing.Point(10, 36);
            this.resourceInputDir.Name = "resourceInputDir";
            this.resourceInputDir.Size = new System.Drawing.Size(840, 20);
            this.resourceInputDir.TabIndex = 54;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(7, 20);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(77, 13);
            this.label13.TabIndex = 55;
            this.label13.Text = "Input directory:";
            // 
            // resourceOutputDir
            // 
            this.resourceOutputDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceOutputDir.Location = new System.Drawing.Point(10, 84);
            this.resourceOutputDir.Name = "resourceOutputDir";
            this.resourceOutputDir.Size = new System.Drawing.Size(840, 20);
            this.resourceOutputDir.TabIndex = 57;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(7, 68);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(85, 13);
            this.label14.TabIndex = 58;
            this.label14.Text = "Output directory:";
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
            this.groupBox4.Location = new System.Drawing.Point(6, 6);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(895, 152);
            this.groupBox4.TabIndex = 59;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Convert LSX / LSB / LSF files";
            // 
            // resourceConvertBtn
            // 
            this.resourceConvertBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceConvertBtn.Location = new System.Drawing.Point(11, 113);
            this.resourceConvertBtn.Name = "resourceConvertBtn";
            this.resourceConvertBtn.Size = new System.Drawing.Size(151, 23);
            this.resourceConvertBtn.TabIndex = 60;
            this.resourceConvertBtn.Text = "Convert";
            this.resourceConvertBtn.UseVisualStyleBackColor = true;
            this.resourceConvertBtn.Click += new System.EventHandler(this.resourceConvertBtn_Click);
            // 
            // resourceOutputBrowseBtn
            // 
            this.resourceOutputBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceOutputBrowseBtn.Location = new System.Drawing.Point(848, 83);
            this.resourceOutputBrowseBtn.Name = "resourceOutputBrowseBtn";
            this.resourceOutputBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.resourceOutputBrowseBtn.TabIndex = 59;
            this.resourceOutputBrowseBtn.Text = "...";
            this.resourceOutputBrowseBtn.UseVisualStyleBackColor = true;
            this.resourceOutputBrowseBtn.Click += new System.EventHandler(this.resourceOutputBrowseBtn_Click);
            // 
            // resourceInputBrowseBtn
            // 
            this.resourceInputBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceInputBrowseBtn.Location = new System.Drawing.Point(848, 35);
            this.resourceInputBrowseBtn.Name = "resourceInputBrowseBtn";
            this.resourceInputBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.resourceInputBrowseBtn.TabIndex = 56;
            this.resourceInputBrowseBtn.Text = "...";
            this.resourceInputBrowseBtn.UseVisualStyleBackColor = true;
            this.resourceInputBrowseBtn.Click += new System.EventHandler(this.resourceInputBrowseBtn_Click);
            // 
            // resourceInputPath
            // 
            this.resourceInputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceInputPath.Location = new System.Drawing.Point(10, 36);
            this.resourceInputPath.Name = "resourceInputPath";
            this.resourceInputPath.Size = new System.Drawing.Size(840, 20);
            this.resourceInputPath.TabIndex = 54;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(7, 20);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(74, 13);
            this.label11.TabIndex = 55;
            this.label11.Text = "Input file path:";
            // 
            // resourceOutputPath
            // 
            this.resourceOutputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceOutputPath.Location = new System.Drawing.Point(10, 84);
            this.resourceOutputPath.Name = "resourceOutputPath";
            this.resourceOutputPath.Size = new System.Drawing.Size(840, 20);
            this.resourceOutputPath.TabIndex = 57;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(7, 68);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(82, 13);
            this.label12.TabIndex = 58;
            this.label12.Text = "Output file path:";
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox3);
            this.tabPage1.Controls.Add(this.loadStoryBtn);
            this.tabPage1.Controls.Add(this.decompileStoryBtn);
            this.tabPage1.Controls.Add(this.storyFilePath);
            this.tabPage1.Controls.Add(this.goalPathBrowseBtn);
            this.tabPage1.Controls.Add(this.label9);
            this.tabPage1.Controls.Add(this.goalPath);
            this.tabPage1.Controls.Add(this.storyFileBrowseBtn);
            this.tabPage1.Controls.Add(this.label10);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(909, 603);
            this.tabPage1.TabIndex = 3;
            this.tabPage1.Text = "Story (OSI) tools";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.databaseGrid);
            this.groupBox3.Controls.Add(this.databaseSelectorCb);
            this.groupBox3.Controls.Add(this.label18);
            this.groupBox3.Location = new System.Drawing.Point(7, 98);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(896, 499);
            this.groupBox3.TabIndex = 58;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Database Editor";
            // 
            // databaseGrid
            // 
            this.databaseGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.databaseGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.databaseGrid.Location = new System.Drawing.Point(10, 47);
            this.databaseGrid.Name = "databaseGrid";
            this.databaseGrid.Size = new System.Drawing.Size(880, 446);
            this.databaseGrid.TabIndex = 2;
            // 
            // databaseSelectorCb
            // 
            this.databaseSelectorCb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.databaseSelectorCb.FormattingEnabled = true;
            this.databaseSelectorCb.Location = new System.Drawing.Point(69, 20);
            this.databaseSelectorCb.Name = "databaseSelectorCb";
            this.databaseSelectorCb.Size = new System.Drawing.Size(471, 21);
            this.databaseSelectorCb.TabIndex = 1;
            this.databaseSelectorCb.SelectedIndexChanged += new System.EventHandler(this.databaseSelectorCb_SelectedIndexChanged);
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(7, 24);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(56, 13);
            this.label18.TabIndex = 0;
            this.label18.Text = "Database:";
            // 
            // loadStoryBtn
            // 
            this.loadStoryBtn.Location = new System.Drawing.Point(756, 21);
            this.loadStoryBtn.Name = "loadStoryBtn";
            this.loadStoryBtn.Size = new System.Drawing.Size(121, 23);
            this.loadStoryBtn.TabIndex = 57;
            this.loadStoryBtn.Text = "Load";
            this.loadStoryBtn.UseVisualStyleBackColor = true;
            this.loadStoryBtn.Click += new System.EventHandler(this.loadStoryBtn_Click);
            // 
            // decompileStoryBtn
            // 
            this.decompileStoryBtn.Location = new System.Drawing.Point(756, 68);
            this.decompileStoryBtn.Name = "decompileStoryBtn";
            this.decompileStoryBtn.Size = new System.Drawing.Size(121, 23);
            this.decompileStoryBtn.TabIndex = 56;
            this.decompileStoryBtn.Text = "Extract";
            this.decompileStoryBtn.UseVisualStyleBackColor = true;
            this.decompileStoryBtn.Click += new System.EventHandler(this.decompileStoryBtn_Click);
            // 
            // storyFilePath
            // 
            this.storyFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.storyFilePath.Location = new System.Drawing.Point(6, 23);
            this.storyFilePath.Name = "storyFilePath";
            this.storyFilePath.Size = new System.Drawing.Size(706, 20);
            this.storyFilePath.TabIndex = 51;
            // 
            // goalPathBrowseBtn
            // 
            this.goalPathBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.goalPathBrowseBtn.Location = new System.Drawing.Point(711, 69);
            this.goalPathBrowseBtn.Name = "goalPathBrowseBtn";
            this.goalPathBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.goalPathBrowseBtn.TabIndex = 55;
            this.goalPathBrowseBtn.Text = "...";
            this.goalPathBrowseBtn.UseVisualStyleBackColor = true;
            this.goalPathBrowseBtn.Click += new System.EventHandler(this.goalPathBrowseBtn_Click);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(3, 7);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(74, 13);
            this.label9.TabIndex = 0;
            this.label9.Text = "Story file path:";
            // 
            // goalPath
            // 
            this.goalPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.goalPath.Location = new System.Drawing.Point(6, 71);
            this.goalPath.Name = "goalPath";
            this.goalPath.Size = new System.Drawing.Size(706, 20);
            this.goalPath.TabIndex = 54;
            // 
            // storyFileBrowseBtn
            // 
            this.storyFileBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.storyFileBrowseBtn.Location = new System.Drawing.Point(711, 21);
            this.storyFileBrowseBtn.Name = "storyFileBrowseBtn";
            this.storyFileBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.storyFileBrowseBtn.TabIndex = 52;
            this.storyFileBrowseBtn.Text = "...";
            this.storyFileBrowseBtn.UseVisualStyleBackColor = true;
            this.storyFileBrowseBtn.Click += new System.EventHandler(this.storyFileBrowseBtn_Click);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(3, 55);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(89, 13);
            this.label10.TabIndex = 53;
            this.label10.Text = "Goal output path:";
            // 
            // packageFileDlg
            // 
            this.packageFileDlg.CheckFileExists = false;
            this.packageFileDlg.Filter = "LS package / savegame files|*.pak;*.lsv";
            // 
            // resourceInputFileDlg
            // 
            this.resourceInputFileDlg.Filter = "LS files|*.lsx;*.lsb;*.lsf";
            this.resourceInputFileDlg.Title = "Select Input File";
            // 
            // resourceOutputFileDlg
            // 
            this.resourceOutputFileDlg.Filter = "LS files|*.lsx;*.lsb;*.lsf";
            this.resourceOutputFileDlg.Title = "Select Output File";
            // 
            // storyPathDlg
            // 
            this.storyPathDlg.CheckFileExists = false;
            this.storyPathDlg.Filter = "LS story files|*.osi";
            // 
            // gr2ModeTabControl
            // 
            this.gr2ModeTabControl.Controls.Add(this.gr2SingleFileTab);
            this.gr2ModeTabControl.Controls.Add(this.gr2BatchTab);
            this.gr2ModeTabControl.Location = new System.Drawing.Point(9, 13);
            this.gr2ModeTabControl.Name = "gr2ModeTabControl";
            this.gr2ModeTabControl.SelectedIndex = 0;
            this.gr2ModeTabControl.Size = new System.Drawing.Size(887, 159);
            this.gr2ModeTabControl.TabIndex = 35;
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
            this.gr2BatchConvertBtn.Click += new System.EventHandler(this.gr2BatchConvertBtn_Click);
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
            // gr2BatchInputBrowseBtn
            // 
            this.gr2BatchInputBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gr2BatchInputBrowseBtn.Location = new System.Drawing.Point(822, 17);
            this.gr2BatchInputBrowseBtn.Name = "gr2BatchInputBrowseBtn";
            this.gr2BatchInputBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.gr2BatchInputBrowseBtn.TabIndex = 37;
            this.gr2BatchInputBrowseBtn.Text = "...";
            this.gr2BatchInputBrowseBtn.UseVisualStyleBackColor = true;
            this.gr2BatchInputBrowseBtn.Click += new System.EventHandler(this.gr2BatchInputBrowseBtn_Click);
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
            // gr2BatchOutputBrowseBtn
            // 
            this.gr2BatchOutputBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gr2BatchOutputBrowseBtn.Location = new System.Drawing.Point(822, 60);
            this.gr2BatchOutputBrowseBtn.Name = "gr2BatchOutputBrowseBtn";
            this.gr2BatchOutputBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.gr2BatchOutputBrowseBtn.TabIndex = 40;
            this.gr2BatchOutputBrowseBtn.Text = "...";
            this.gr2BatchOutputBrowseBtn.UseVisualStyleBackColor = true;
            this.gr2BatchOutputBrowseBtn.Click += new System.EventHandler(this.gr2BatchOutputBrowseBtn_Click);
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
            // resourceFormats
            // 
            this.resourceFormats.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceFormats.FullRowSelect = true;
            this.resourceFormats.Location = new System.Drawing.Point(19, 188);
            this.resourceFormats.Name = "resourceFormats";
            this.resourceFormats.Size = new System.Drawing.Size(445, 217);
            this.resourceFormats.TabIndex = 16;
            this.resourceFormats.UseCompatibleStateImageBehavior = false;
            this.resourceFormats.View = System.Windows.Forms.View.Details;
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
            // gr2BatchProgressLabel
            // 
            this.gr2BatchProgressLabel.AutoSize = true;
            this.gr2BatchProgressLabel.Location = new System.Drawing.Point(82, 88);
            this.gr2BatchProgressLabel.Name = "gr2BatchProgressLabel";
            this.gr2BatchProgressLabel.Size = new System.Drawing.Size(0, 13);
            this.gr2BatchProgressLabel.TabIndex = 49;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(941, 653);
            this.Controls.Add(this.tabControl);
            this.Name = "MainForm";
            this.Text = "GR2 Converter";
            this.tabControl.ResumeLayout(false);
            this.gr2Tab.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.packageTab.ResumeLayout(false);
            this.packageTab.PerformLayout();
            this.objectTab.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.databaseGrid)).EndInit();
            this.gr2ModeTabControl.ResumeLayout(false);
            this.gr2SingleFileTab.ResumeLayout(false);
            this.gr2SingleFileTab.PerformLayout();
            this.gr2BatchTab.ResumeLayout(false);
            this.gr2BatchTab.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog inputFileDlg;
        private System.Windows.Forms.SaveFileDialog outputFileDlg;
        private System.Windows.Forms.OpenFileDialog conformSkeletonFileDlg;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage gr2Tab;
        private System.Windows.Forms.Button saveOutputBtn;
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
        private System.Windows.Forms.CheckBox use16bitIndex;
        private System.Windows.Forms.CheckBox forceLegacyVersion;
        private ExportItemSelection resourceFormats;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button loadInputBtn;
        private System.Windows.Forms.Button outputFileBrowserBtn;
        private System.Windows.Forms.Label lblOutputPath;
        private System.Windows.Forms.TextBox outputPath;
        private System.Windows.Forms.Button inputFileBrowseBtn;
        private System.Windows.Forms.Label lblSrcPath;
        private System.Windows.Forms.TextBox inputPath;
        private System.Windows.Forms.TabPage packageTab;
        private System.Windows.Forms.Button extractPackageBtn;
        private System.Windows.Forms.Button exportPathBrowseBtn;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox extractionPath;
        private System.Windows.Forms.Button packageBrowseBtn;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox packagePath;
        private System.Windows.Forms.OpenFileDialog packageFileDlg;
        private System.Windows.Forms.FolderBrowserDialog exportPathDlg;
        private System.Windows.Forms.Label packageProgressLabel;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ProgressBar packageProgress;
        private System.Windows.Forms.Button createPackageBtn;
        private System.Windows.Forms.ComboBox compressionMethod;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox gr2Game;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TabPage objectTab;
        private System.Windows.Forms.OpenFileDialog resourceInputFileDlg;
        private System.Windows.Forms.SaveFileDialog resourceOutputFileDlg;
        private System.Windows.Forms.ComboBox packageVersion;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.OpenFileDialog storyPathDlg;
        private System.Windows.Forms.FolderBrowserDialog goalPathDlg;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button resourceConvertBtn;
        private System.Windows.Forms.Button resourceOutputBrowseBtn;
        private System.Windows.Forms.Button resourceInputBrowseBtn;
        private System.Windows.Forms.TextBox resourceInputPath;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox resourceOutputPath;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.ComboBox resourceOutputFormatCb;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.ComboBox resourceInputFormatCb;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Button resourceBulkConvertBtn;
        private System.Windows.Forms.Button resourceOutputPathBrowseBtn;
        private System.Windows.Forms.Button resourceInputPathBrowseBtn;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox resourceOutputDir;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.FolderBrowserDialog resourceInputPathDlg;
        private System.Windows.Forms.FolderBrowserDialog resourceOutputPathDlg;
        private System.Windows.Forms.TextBox resourceInputDir;
        private System.Windows.Forms.ProgressBar resourceConversionProgress;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label resourceProgressLabel;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.DataGridView databaseGrid;
        private System.Windows.Forms.ComboBox databaseSelectorCb;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Button loadStoryBtn;
        private System.Windows.Forms.Button decompileStoryBtn;
        private System.Windows.Forms.TextBox storyFilePath;
        private System.Windows.Forms.Button goalPathBrowseBtn;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox goalPath;
        private System.Windows.Forms.Button storyFileBrowseBtn;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TabControl gr2ModeTabControl;
        private System.Windows.Forms.TabPage gr2SingleFileTab;
        private System.Windows.Forms.TabPage gr2BatchTab;
        private System.Windows.Forms.ComboBox gr2BatchOutputFormat;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Button gr2BatchConvertBtn;
        private System.Windows.Forms.TextBox gr2BatchInputDir;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Button gr2BatchInputBrowseBtn;
        private System.Windows.Forms.TextBox gr2BatchOutputDir;
        private System.Windows.Forms.Button gr2BatchOutputBrowseBtn;
        private System.Windows.Forms.FolderBrowserDialog gr2InputDirDlg;
        private System.Windows.Forms.FolderBrowserDialog gr2OutputDirDlg;
        private System.Windows.Forms.ComboBox gr2BatchInputFormat;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.ProgressBar gr2BatchProgressBar;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Label gr2BatchProgressLabel;
    }
}

