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
            this.packageTab = new System.Windows.Forms.TabPage();
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
            this.applyBasisTransforms = new System.Windows.Forms.CheckBox();
            this.conformantSkeletonBrowseBtn = new System.Windows.Forms.Button();
            this.conformantSkeletonPath = new System.Windows.Forms.TextBox();
            this.conformToSkeleton = new System.Windows.Forms.CheckBox();
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
            this.extractPackageBtn = new System.Windows.Forms.Button();
            this.exportPathBrowseBtn = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.extractionPath = new System.Windows.Forms.TextBox();
            this.packageBrowseBtn = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.packagePath = new System.Windows.Forms.TextBox();
            this.packageFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.exportPathDlg = new System.Windows.Forms.FolderBrowserDialog();
            this.packageProgress = new System.Windows.Forms.ProgressBar();
            this.label5 = new System.Windows.Forms.Label();
            this.packageProgressLabel = new System.Windows.Forms.Label();
            this.createPackageBtn = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.compressionMethod = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.gr2Game = new System.Windows.Forms.ComboBox();
            this.resourceFormats = new ConverterApp.ExportItemSelection();
            this.tabControl.SuspendLayout();
            this.gr2Tab.SuspendLayout();
            this.packageTab.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
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
            this.tabControl.Location = new System.Drawing.Point(12, 12);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(917, 629);
            this.tabControl.TabIndex = 0;
            // 
            // gr2Tab
            // 
            this.gr2Tab.Controls.Add(this.saveOutputBtn);
            this.gr2Tab.Controls.Add(this.groupBox2);
            this.gr2Tab.Controls.Add(this.groupBox1);
            this.gr2Tab.Controls.Add(this.loadInputBtn);
            this.gr2Tab.Controls.Add(this.outputFileBrowserBtn);
            this.gr2Tab.Controls.Add(this.lblOutputPath);
            this.gr2Tab.Controls.Add(this.outputPath);
            this.gr2Tab.Controls.Add(this.inputFileBrowseBtn);
            this.gr2Tab.Controls.Add(this.lblSrcPath);
            this.gr2Tab.Controls.Add(this.inputPath);
            this.gr2Tab.Location = new System.Drawing.Point(4, 22);
            this.gr2Tab.Name = "gr2Tab";
            this.gr2Tab.Padding = new System.Windows.Forms.Padding(3);
            this.gr2Tab.Size = new System.Drawing.Size(909, 603);
            this.gr2Tab.TabIndex = 0;
            this.gr2Tab.Text = "GR2 Tools";
            this.gr2Tab.UseVisualStyleBackColor = true;
            // 
            // packageTab
            // 
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
            // saveOutputBtn
            // 
            this.saveOutputBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.saveOutputBtn.Enabled = false;
            this.saveOutputBtn.Location = new System.Drawing.Point(725, 62);
            this.saveOutputBtn.Name = "saveOutputBtn";
            this.saveOutputBtn.Size = new System.Drawing.Size(151, 23);
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
            this.groupBox2.Location = new System.Drawing.Point(9, 100);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(395, 492);
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
            this.exportableObjects.Size = new System.Drawing.Size(373, 311);
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
            this.exportUVs.Enabled = false;
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
            this.exportTangents.Enabled = false;
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
            this.exportNormals.Enabled = false;
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
            this.recalculateTangents.Enabled = false;
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
            this.recalculateNormals.Enabled = false;
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
            this.groupBox1.Controls.Add(this.conformantSkeletonBrowseBtn);
            this.groupBox1.Controls.Add(this.conformantSkeletonPath);
            this.groupBox1.Controls.Add(this.conformToSkeleton);
            this.groupBox1.Controls.Add(this.buildDummySkeleton);
            this.groupBox1.Controls.Add(this.use16bitIndex);
            this.groupBox1.Controls.Add(this.forceLegacyVersion);
            this.groupBox1.Controls.Add(this.resourceFormats);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(418, 100);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(476, 492);
            this.groupBox1.TabIndex = 32;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "GR2 Export Options";
            // 
            // applyBasisTransforms
            // 
            this.applyBasisTransforms.AutoSize = true;
            this.applyBasisTransforms.Checked = true;
            this.applyBasisTransforms.CheckState = System.Windows.Forms.CheckState.Checked;
            this.applyBasisTransforms.Location = new System.Drawing.Point(249, 50);
            this.applyBasisTransforms.Name = "applyBasisTransforms";
            this.applyBasisTransforms.Size = new System.Drawing.Size(185, 17);
            this.applyBasisTransforms.TabIndex = 26;
            this.applyBasisTransforms.Text = "Apply basis transformation to Y-up";
            this.applyBasisTransforms.UseVisualStyleBackColor = true;
            // 
            // conformantSkeletonBrowseBtn
            // 
            this.conformantSkeletonBrowseBtn.Enabled = false;
            this.conformantSkeletonBrowseBtn.Location = new System.Drawing.Point(423, 137);
            this.conformantSkeletonBrowseBtn.Name = "conformantSkeletonBrowseBtn";
            this.conformantSkeletonBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.conformantSkeletonBrowseBtn.TabIndex = 25;
            this.conformantSkeletonBrowseBtn.Text = "...";
            this.conformantSkeletonBrowseBtn.UseVisualStyleBackColor = true;
            this.conformantSkeletonBrowseBtn.Click += new System.EventHandler(this.conformantSkeletonBrowseBtn_Click);
            // 
            // conformantSkeletonPath
            // 
            this.conformantSkeletonPath.Enabled = false;
            this.conformantSkeletonPath.Location = new System.Drawing.Point(19, 139);
            this.conformantSkeletonPath.Name = "conformantSkeletonPath";
            this.conformantSkeletonPath.Size = new System.Drawing.Size(405, 20);
            this.conformantSkeletonPath.TabIndex = 24;
            // 
            // conformToSkeleton
            // 
            this.conformToSkeleton.AutoSize = true;
            this.conformToSkeleton.Enabled = false;
            this.conformToSkeleton.Location = new System.Drawing.Point(19, 119);
            this.conformToSkeleton.Name = "conformToSkeleton";
            this.conformToSkeleton.Size = new System.Drawing.Size(123, 17);
            this.conformToSkeleton.TabIndex = 23;
            this.conformToSkeleton.Text = "Conform to skeleton:";
            this.conformToSkeleton.UseVisualStyleBackColor = true;
            this.conformToSkeleton.CheckedChanged += new System.EventHandler(this.conformToSkeleton_CheckedChanged);
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
            this.loadInputBtn.Location = new System.Drawing.Point(725, 19);
            this.loadInputBtn.Name = "loadInputBtn";
            this.loadInputBtn.Size = new System.Drawing.Size(151, 23);
            this.loadInputBtn.TabIndex = 31;
            this.loadInputBtn.Text = "Import";
            this.loadInputBtn.UseVisualStyleBackColor = true;
            this.loadInputBtn.Click += new System.EventHandler(this.loadInputBtn_Click);
            // 
            // outputFileBrowserBtn
            // 
            this.outputFileBrowserBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.outputFileBrowserBtn.Location = new System.Drawing.Point(657, 63);
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
            this.lblOutputPath.Location = new System.Drawing.Point(6, 49);
            this.lblOutputPath.Name = "lblOutputPath";
            this.lblOutputPath.Size = new System.Drawing.Size(82, 13);
            this.lblOutputPath.TabIndex = 29;
            this.lblOutputPath.Text = "Output file path:";
            // 
            // outputPath
            // 
            this.outputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.outputPath.Location = new System.Drawing.Point(9, 65);
            this.outputPath.Name = "outputPath";
            this.outputPath.Size = new System.Drawing.Size(650, 20);
            this.outputPath.TabIndex = 28;
            // 
            // inputFileBrowseBtn
            // 
            this.inputFileBrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.inputFileBrowseBtn.Location = new System.Drawing.Point(657, 17);
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
            this.inputPath.Size = new System.Drawing.Size(650, 20);
            this.inputPath.TabIndex = 25;
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
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 49);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(121, 13);
            this.label3.TabIndex = 39;
            this.label3.Text = "Extracion / source path:";
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
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 3);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(77, 13);
            this.label4.TabIndex = 36;
            this.label4.Text = "Package path:";
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
            // packageFileDlg
            // 
            this.packageFileDlg.CheckFileExists = false;
            this.packageFileDlg.Filter = "LS package / savegame files|*.pak;*.lsv";
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
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 97);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(70, 13);
            this.label6.TabIndex = 46;
            this.label6.Text = "Compression:";
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
            this.compressionMethod.Location = new System.Drawing.Point(9, 114);
            this.compressionMethod.Name = "compressionMethod";
            this.compressionMethod.Size = new System.Drawing.Size(187, 21);
            this.compressionMethod.TabIndex = 47;
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
            this.gr2Game.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // resourceFormats
            // 
            this.resourceFormats.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resourceFormats.FullRowSelect = true;
            this.resourceFormats.Location = new System.Drawing.Point(19, 188);
            this.resourceFormats.Name = "resourceFormats";
            this.resourceFormats.Size = new System.Drawing.Size(445, 290);
            this.resourceFormats.TabIndex = 16;
            this.resourceFormats.UseCompatibleStateImageBehavior = false;
            this.resourceFormats.View = System.Windows.Forms.View.Details;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(941, 653);
            this.Controls.Add(this.tabControl);
            this.Name = "MainForm";
            this.Text = "GR2 Converter (v1.0.1)";
            this.tabControl.ResumeLayout(false);
            this.gr2Tab.ResumeLayout(false);
            this.gr2Tab.PerformLayout();
            this.packageTab.ResumeLayout(false);
            this.packageTab.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
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
        private System.Windows.Forms.Button conformantSkeletonBrowseBtn;
        private System.Windows.Forms.TextBox conformantSkeletonPath;
        private System.Windows.Forms.CheckBox conformToSkeleton;
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
    }
}

