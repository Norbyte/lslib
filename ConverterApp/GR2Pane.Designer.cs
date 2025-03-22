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
            gr2ModeTabControl = new System.Windows.Forms.TabControl();
            gr2SingleFileTab = new System.Windows.Forms.TabPage();
            lblOutputPath = new System.Windows.Forms.Label();
            saveOutputBtn = new System.Windows.Forms.Button();
            inputPath = new System.Windows.Forms.TextBox();
            lblSrcPath = new System.Windows.Forms.Label();
            inputFileBrowseBtn = new System.Windows.Forms.Button();
            loadInputBtn = new System.Windows.Forms.Button();
            outputPath = new System.Windows.Forms.TextBox();
            outputFileBrowserBtn = new System.Windows.Forms.Button();
            gr2BatchTab = new System.Windows.Forms.TabPage();
            gr2BatchProgressLabel = new System.Windows.Forms.Label();
            gr2BatchInputBrowseBtn = new System.Windows.Forms.Button();
            gr2BatchOutputBrowseBtn = new System.Windows.Forms.Button();
            gr2BatchProgressBar = new System.Windows.Forms.ProgressBar();
            label23 = new System.Windows.Forms.Label();
            gr2BatchInputFormat = new System.Windows.Forms.ComboBox();
            label22 = new System.Windows.Forms.Label();
            gr2BatchOutputFormat = new System.Windows.Forms.ComboBox();
            label21 = new System.Windows.Forms.Label();
            label19 = new System.Windows.Forms.Label();
            gr2BatchConvertBtn = new System.Windows.Forms.Button();
            gr2BatchInputDir = new System.Windows.Forms.TextBox();
            label20 = new System.Windows.Forms.Label();
            gr2BatchOutputDir = new System.Windows.Forms.TextBox();
            groupBox2 = new System.Windows.Forms.GroupBox();
            flipMeshes = new System.Windows.Forms.CheckBox();
            flipSkeletons = new System.Windows.Forms.CheckBox();
            flipUVs = new System.Windows.Forms.CheckBox();
            label2 = new System.Windows.Forms.Label();
            exportableObjects = new System.Windows.Forms.ListView();
            exportableName = new System.Windows.Forms.ColumnHeader();
            exportableType = new System.Windows.Forms.ColumnHeader();
            filterUVs = new System.Windows.Forms.CheckBox();
            groupBox1 = new System.Windows.Forms.GroupBox();
            conformCopySkeletons = new System.Windows.Forms.CheckBox();
            meshProxy = new System.Windows.Forms.CheckBox();
            meshCloth = new System.Windows.Forms.CheckBox();
            meshRigid = new System.Windows.Forms.CheckBox();
            applyBasisTransforms = new System.Windows.Forms.CheckBox();
            conformantGR2BrowseBtn = new System.Windows.Forms.Button();
            conformantGR2Path = new System.Windows.Forms.TextBox();
            conformToOriginal = new System.Windows.Forms.CheckBox();
            buildDummySkeleton = new System.Windows.Forms.CheckBox();
            resourceFormats = new ExportItemSelection();
            label1 = new System.Windows.Forms.Label();
            gr2OutputDirDlg = new System.Windows.Forms.FolderBrowserDialog();
            gr2InputDirDlg = new System.Windows.Forms.FolderBrowserDialog();
            conformSkeletonFileDlg = new System.Windows.Forms.OpenFileDialog();
            outputFileDlg = new System.Windows.Forms.SaveFileDialog();
            inputFileDlg = new System.Windows.Forms.OpenFileDialog();
            gr2ModeTabControl.SuspendLayout();
            gr2SingleFileTab.SuspendLayout();
            gr2BatchTab.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // gr2ModeTabControl
            // 
            gr2ModeTabControl.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            gr2ModeTabControl.Controls.Add(gr2SingleFileTab);
            gr2ModeTabControl.Controls.Add(gr2BatchTab);
            gr2ModeTabControl.Location = new System.Drawing.Point(9, 14);
            gr2ModeTabControl.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            gr2ModeTabControl.Name = "gr2ModeTabControl";
            gr2ModeTabControl.SelectedIndex = 0;
            gr2ModeTabControl.Size = new System.Drawing.Size(1183, 245);
            gr2ModeTabControl.TabIndex = 38;
            // 
            // gr2SingleFileTab
            // 
            gr2SingleFileTab.Controls.Add(lblOutputPath);
            gr2SingleFileTab.Controls.Add(saveOutputBtn);
            gr2SingleFileTab.Controls.Add(inputPath);
            gr2SingleFileTab.Controls.Add(lblSrcPath);
            gr2SingleFileTab.Controls.Add(inputFileBrowseBtn);
            gr2SingleFileTab.Controls.Add(loadInputBtn);
            gr2SingleFileTab.Controls.Add(outputPath);
            gr2SingleFileTab.Controls.Add(outputFileBrowserBtn);
            gr2SingleFileTab.Location = new System.Drawing.Point(4, 29);
            gr2SingleFileTab.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            gr2SingleFileTab.Name = "gr2SingleFileTab";
            gr2SingleFileTab.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            gr2SingleFileTab.Size = new System.Drawing.Size(1175, 212);
            gr2SingleFileTab.TabIndex = 0;
            gr2SingleFileTab.Text = "Single File";
            gr2SingleFileTab.UseVisualStyleBackColor = true;
            // 
            // lblOutputPath
            // 
            lblOutputPath.AutoSize = true;
            lblOutputPath.Location = new System.Drawing.Point(8, 71);
            lblOutputPath.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblOutputPath.Name = "lblOutputPath";
            lblOutputPath.Size = new System.Drawing.Size(117, 20);
            lblOutputPath.TabIndex = 29;
            lblOutputPath.Text = "Output file path:";
            // 
            // saveOutputBtn
            // 
            saveOutputBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            saveOutputBtn.Enabled = false;
            saveOutputBtn.Location = new System.Drawing.Point(961, 94);
            saveOutputBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            saveOutputBtn.Name = "saveOutputBtn";
            saveOutputBtn.Size = new System.Drawing.Size(188, 35);
            saveOutputBtn.TabIndex = 34;
            saveOutputBtn.Text = "Export";
            saveOutputBtn.UseVisualStyleBackColor = true;
            saveOutputBtn.Click += saveOutputBtn_Click;
            // 
            // inputPath
            // 
            inputPath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            inputPath.Location = new System.Drawing.Point(12, 29);
            inputPath.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            inputPath.Name = "inputPath";
            inputPath.Size = new System.Drawing.Size(877, 27);
            inputPath.TabIndex = 25;
            // 
            // lblSrcPath
            // 
            lblSrcPath.AutoSize = true;
            lblSrcPath.Location = new System.Drawing.Point(8, 5);
            lblSrcPath.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblSrcPath.Name = "lblSrcPath";
            lblSrcPath.Size = new System.Drawing.Size(105, 20);
            lblSrcPath.TabIndex = 26;
            lblSrcPath.Text = "Input file path:";
            // 
            // inputFileBrowseBtn
            // 
            inputFileBrowseBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            inputFileBrowseBtn.Location = new System.Drawing.Point(888, 28);
            inputFileBrowseBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            inputFileBrowseBtn.Name = "inputFileBrowseBtn";
            inputFileBrowseBtn.Size = new System.Drawing.Size(55, 34);
            inputFileBrowseBtn.TabIndex = 27;
            inputFileBrowseBtn.Text = "...";
            inputFileBrowseBtn.UseVisualStyleBackColor = true;
            inputFileBrowseBtn.Click += inputFileBrowseBtn_Click;
            // 
            // loadInputBtn
            // 
            loadInputBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            loadInputBtn.Location = new System.Drawing.Point(961, 28);
            loadInputBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            loadInputBtn.Name = "loadInputBtn";
            loadInputBtn.Size = new System.Drawing.Size(188, 35);
            loadInputBtn.TabIndex = 31;
            loadInputBtn.Text = "Import";
            loadInputBtn.UseVisualStyleBackColor = true;
            loadInputBtn.Click += loadInputBtn_Click;
            // 
            // outputPath
            // 
            outputPath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            outputPath.Location = new System.Drawing.Point(12, 95);
            outputPath.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            outputPath.Name = "outputPath";
            outputPath.Size = new System.Drawing.Size(877, 27);
            outputPath.TabIndex = 28;
            // 
            // outputFileBrowserBtn
            // 
            outputFileBrowserBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            outputFileBrowserBtn.Location = new System.Drawing.Point(888, 94);
            outputFileBrowserBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            outputFileBrowserBtn.Name = "outputFileBrowserBtn";
            outputFileBrowserBtn.Size = new System.Drawing.Size(55, 34);
            outputFileBrowserBtn.TabIndex = 30;
            outputFileBrowserBtn.Text = "...";
            outputFileBrowserBtn.UseVisualStyleBackColor = true;
            outputFileBrowserBtn.Click += outputFileBrowserBtn_Click;
            // 
            // gr2BatchTab
            // 
            gr2BatchTab.Controls.Add(gr2BatchProgressLabel);
            gr2BatchTab.Controls.Add(gr2BatchInputBrowseBtn);
            gr2BatchTab.Controls.Add(gr2BatchOutputBrowseBtn);
            gr2BatchTab.Controls.Add(gr2BatchProgressBar);
            gr2BatchTab.Controls.Add(label23);
            gr2BatchTab.Controls.Add(gr2BatchInputFormat);
            gr2BatchTab.Controls.Add(label22);
            gr2BatchTab.Controls.Add(gr2BatchOutputFormat);
            gr2BatchTab.Controls.Add(label21);
            gr2BatchTab.Controls.Add(label19);
            gr2BatchTab.Controls.Add(gr2BatchConvertBtn);
            gr2BatchTab.Controls.Add(gr2BatchInputDir);
            gr2BatchTab.Controls.Add(label20);
            gr2BatchTab.Controls.Add(gr2BatchOutputDir);
            gr2BatchTab.Location = new System.Drawing.Point(4, 29);
            gr2BatchTab.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            gr2BatchTab.Name = "gr2BatchTab";
            gr2BatchTab.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            gr2BatchTab.Size = new System.Drawing.Size(1175, 212);
            gr2BatchTab.TabIndex = 1;
            gr2BatchTab.Text = "Batch";
            gr2BatchTab.UseVisualStyleBackColor = true;
            // 
            // gr2BatchProgressLabel
            // 
            gr2BatchProgressLabel.AutoSize = true;
            gr2BatchProgressLabel.Location = new System.Drawing.Point(109, 135);
            gr2BatchProgressLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            gr2BatchProgressLabel.Name = "gr2BatchProgressLabel";
            gr2BatchProgressLabel.Size = new System.Drawing.Size(0, 20);
            gr2BatchProgressLabel.TabIndex = 49;
            // 
            // gr2BatchInputBrowseBtn
            // 
            gr2BatchInputBrowseBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            gr2BatchInputBrowseBtn.Location = new System.Drawing.Point(1096, 26);
            gr2BatchInputBrowseBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            gr2BatchInputBrowseBtn.Name = "gr2BatchInputBrowseBtn";
            gr2BatchInputBrowseBtn.Size = new System.Drawing.Size(55, 35);
            gr2BatchInputBrowseBtn.TabIndex = 37;
            gr2BatchInputBrowseBtn.Text = "...";
            gr2BatchInputBrowseBtn.UseVisualStyleBackColor = true;
            gr2BatchInputBrowseBtn.Click += GR2BatchInputBrowseBtn_Click;
            // 
            // gr2BatchOutputBrowseBtn
            // 
            gr2BatchOutputBrowseBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            gr2BatchOutputBrowseBtn.Location = new System.Drawing.Point(1096, 92);
            gr2BatchOutputBrowseBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            gr2BatchOutputBrowseBtn.Name = "gr2BatchOutputBrowseBtn";
            gr2BatchOutputBrowseBtn.Size = new System.Drawing.Size(55, 35);
            gr2BatchOutputBrowseBtn.TabIndex = 40;
            gr2BatchOutputBrowseBtn.Text = "...";
            gr2BatchOutputBrowseBtn.UseVisualStyleBackColor = true;
            gr2BatchOutputBrowseBtn.Click += GR2BatchOutputBrowseBtn_Click;
            // 
            // gr2BatchProgressBar
            // 
            gr2BatchProgressBar.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            gr2BatchProgressBar.Location = new System.Drawing.Point(12, 160);
            gr2BatchProgressBar.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            gr2BatchProgressBar.Name = "gr2BatchProgressBar";
            gr2BatchProgressBar.Size = new System.Drawing.Size(933, 35);
            gr2BatchProgressBar.TabIndex = 47;
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Location = new System.Drawing.Point(8, 135);
            label23.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label23.Name = "label23";
            label23.Size = new System.Drawing.Size(68, 20);
            label23.TabIndex = 48;
            label23.Text = "Progress:";
            // 
            // gr2BatchInputFormat
            // 
            gr2BatchInputFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            gr2BatchInputFormat.FormattingEnabled = true;
            gr2BatchInputFormat.Items.AddRange(new object[] { "GR2", "DAE", "GLTF", "GLB" });
            gr2BatchInputFormat.Location = new System.Drawing.Point(12, 29);
            gr2BatchInputFormat.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            gr2BatchInputFormat.Name = "gr2BatchInputFormat";
            gr2BatchInputFormat.Size = new System.Drawing.Size(88, 28);
            gr2BatchInputFormat.TabIndex = 46;
            // 
            // label22
            // 
            label22.AutoSize = true;
            label22.Location = new System.Drawing.Point(8, 5);
            label22.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label22.Name = "label22";
            label22.Size = new System.Drawing.Size(95, 20);
            label22.TabIndex = 45;
            label22.Text = "Input format:";
            // 
            // gr2BatchOutputFormat
            // 
            gr2BatchOutputFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            gr2BatchOutputFormat.FormattingEnabled = true;
            gr2BatchOutputFormat.Items.AddRange(new object[] { "GR2", "DAE", "GLTF", "GLB" });
            gr2BatchOutputFormat.Location = new System.Drawing.Point(12, 95);
            gr2BatchOutputFormat.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            gr2BatchOutputFormat.Name = "gr2BatchOutputFormat";
            gr2BatchOutputFormat.Size = new System.Drawing.Size(88, 28);
            gr2BatchOutputFormat.TabIndex = 44;
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Location = new System.Drawing.Point(8, 71);
            label21.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label21.Name = "label21";
            label21.Size = new System.Drawing.Size(107, 20);
            label21.TabIndex = 43;
            label21.Text = "Output format:";
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new System.Drawing.Point(105, 71);
            label19.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label19.Name = "label19";
            label19.Size = new System.Drawing.Size(121, 20);
            label19.TabIndex = 39;
            label19.Text = "Output directory:";
            // 
            // gr2BatchConvertBtn
            // 
            gr2BatchConvertBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            gr2BatchConvertBtn.Location = new System.Drawing.Point(964, 160);
            gr2BatchConvertBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            gr2BatchConvertBtn.Name = "gr2BatchConvertBtn";
            gr2BatchConvertBtn.Size = new System.Drawing.Size(188, 35);
            gr2BatchConvertBtn.TabIndex = 42;
            gr2BatchConvertBtn.Text = "Convert";
            gr2BatchConvertBtn.UseVisualStyleBackColor = true;
            gr2BatchConvertBtn.Click += GR2BatchConvertBtn_Click;
            // 
            // gr2BatchInputDir
            // 
            gr2BatchInputDir.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            gr2BatchInputDir.Location = new System.Drawing.Point(109, 29);
            gr2BatchInputDir.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            gr2BatchInputDir.Name = "gr2BatchInputDir";
            gr2BatchInputDir.Size = new System.Drawing.Size(988, 27);
            gr2BatchInputDir.TabIndex = 35;
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new System.Drawing.Point(105, 5);
            label20.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label20.Name = "label20";
            label20.Size = new System.Drawing.Size(109, 20);
            label20.TabIndex = 36;
            label20.Text = "Input directory:";
            // 
            // gr2BatchOutputDir
            // 
            gr2BatchOutputDir.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            gr2BatchOutputDir.Location = new System.Drawing.Point(109, 95);
            gr2BatchOutputDir.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            gr2BatchOutputDir.Name = "gr2BatchOutputDir";
            gr2BatchOutputDir.Size = new System.Drawing.Size(988, 27);
            gr2BatchOutputDir.TabIndex = 38;
            // 
            // groupBox2
            // 
            groupBox2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            groupBox2.Controls.Add(flipMeshes);
            groupBox2.Controls.Add(flipSkeletons);
            groupBox2.Controls.Add(flipUVs);
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(exportableObjects);
            groupBox2.Controls.Add(filterUVs);
            groupBox2.Location = new System.Drawing.Point(9, 268);
            groupBox2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBox2.Size = new System.Drawing.Size(527, 689);
            groupBox2.TabIndex = 37;
            groupBox2.TabStop = false;
            groupBox2.Text = "Export Options";
            // 
            // flipMeshes
            // 
            flipMeshes.AutoSize = true;
            flipMeshes.Checked = true;
            flipMeshes.CheckState = System.Windows.Forms.CheckState.Checked;
            flipMeshes.Location = new System.Drawing.Point(252, 69);
            flipMeshes.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            flipMeshes.Name = "flipMeshes";
            flipMeshes.Size = new System.Drawing.Size(180, 24);
            flipMeshes.TabIndex = 26;
            flipMeshes.Text = "X-flip meshes (D:OS 2)";
            flipMeshes.UseVisualStyleBackColor = true;
            // 
            // flipSkeletons
            // 
            flipSkeletons.AutoSize = true;
            flipSkeletons.Location = new System.Drawing.Point(252, 34);
            flipSkeletons.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            flipSkeletons.Name = "flipSkeletons";
            flipSkeletons.Size = new System.Drawing.Size(192, 24);
            flipSkeletons.TabIndex = 25;
            flipSkeletons.Text = "X-flip skeletons (D:OS 2)";
            flipSkeletons.UseVisualStyleBackColor = true;
            // 
            // flipUVs
            // 
            flipUVs.AutoSize = true;
            flipUVs.Checked = true;
            flipUVs.CheckState = System.Windows.Forms.CheckState.Checked;
            flipUVs.Location = new System.Drawing.Point(15, 34);
            flipUVs.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            flipUVs.Name = "flipUVs";
            flipUVs.Size = new System.Drawing.Size(84, 24);
            flipUVs.TabIndex = 23;
            flipUVs.Text = "Flip UVs";
            flipUVs.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(13, 112);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(197, 20);
            label2.TabIndex = 22;
            label2.Text = "Select subobjects for export:";
            // 
            // exportableObjects
            // 
            exportableObjects.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            exportableObjects.CheckBoxes = true;
            exportableObjects.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { exportableName, exportableType });
            exportableObjects.Enabled = false;
            exportableObjects.FullRowSelect = true;
            exportableObjects.Location = new System.Drawing.Point(15, 140);
            exportableObjects.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            exportableObjects.Name = "exportableObjects";
            exportableObjects.Size = new System.Drawing.Size(496, 521);
            exportableObjects.TabIndex = 21;
            exportableObjects.UseCompatibleStateImageBehavior = false;
            exportableObjects.View = System.Windows.Forms.View.Details;
            // 
            // exportableName
            // 
            exportableName.Text = "Name";
            exportableName.Width = 230;
            // 
            // exportableType
            // 
            exportableType.Text = "Type";
            exportableType.Width = 130;
            // 
            // filterUVs
            // 
            filterUVs.AutoSize = true;
            filterUVs.Location = new System.Drawing.Point(15, 69);
            filterUVs.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            filterUVs.Name = "filterUVs";
            filterUVs.Size = new System.Drawing.Size(93, 24);
            filterUVs.TabIndex = 16;
            filterUVs.Text = "Filter UVs";
            filterUVs.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox1.Controls.Add(conformCopySkeletons);
            groupBox1.Controls.Add(meshProxy);
            groupBox1.Controls.Add(meshCloth);
            groupBox1.Controls.Add(meshRigid);
            groupBox1.Controls.Add(applyBasisTransforms);
            groupBox1.Controls.Add(conformantGR2BrowseBtn);
            groupBox1.Controls.Add(conformantGR2Path);
            groupBox1.Controls.Add(conformToOriginal);
            groupBox1.Controls.Add(buildDummySkeleton);
            groupBox1.Controls.Add(resourceFormats);
            groupBox1.Controls.Add(label1);
            groupBox1.Location = new System.Drawing.Point(557, 268);
            groupBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBox1.Size = new System.Drawing.Size(635, 689);
            groupBox1.TabIndex = 36;
            groupBox1.TabStop = false;
            groupBox1.Text = "GR2 Export Options";
            // 
            // conformCopySkeletons
            // 
            conformCopySkeletons.AutoSize = true;
            conformCopySkeletons.Enabled = false;
            conformCopySkeletons.Location = new System.Drawing.Point(324, 140);
            conformCopySkeletons.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            conformCopySkeletons.Name = "conformCopySkeletons";
            conformCopySkeletons.Size = new System.Drawing.Size(126, 24);
            conformCopySkeletons.TabIndex = 29;
            conformCopySkeletons.Text = "Copy Skeleton";
            conformCopySkeletons.UseVisualStyleBackColor = true;
            // 
            // meshProxy
            // 
            meshProxy.AutoSize = true;
            meshProxy.Location = new System.Drawing.Point(324, 105);
            meshProxy.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            meshProxy.Name = "meshProxy";
            meshProxy.Size = new System.Drawing.Size(165, 24);
            meshProxy.TabIndex = 29;
            meshProxy.Text = "(D:OS 2) Mesh Proxy";
            meshProxy.UseVisualStyleBackColor = true;
            // 
            // meshCloth
            // 
            meshCloth.AutoSize = true;
            meshCloth.Location = new System.Drawing.Point(324, 69);
            meshCloth.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            meshCloth.Name = "meshCloth";
            meshCloth.Size = new System.Drawing.Size(125, 24);
            meshCloth.TabIndex = 28;
            meshCloth.Text = "(D:OS 2) Cloth";
            meshCloth.UseVisualStyleBackColor = true;
            // 
            // meshRigid
            // 
            meshRigid.AutoSize = true;
            meshRigid.Location = new System.Drawing.Point(324, 34);
            meshRigid.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            meshRigid.Name = "meshRigid";
            meshRigid.Size = new System.Drawing.Size(125, 24);
            meshRigid.TabIndex = 27;
            meshRigid.Text = "(D:OS 2) Rigid";
            meshRigid.UseVisualStyleBackColor = true;
            // 
            // applyBasisTransforms
            // 
            applyBasisTransforms.AutoSize = true;
            applyBasisTransforms.Checked = true;
            applyBasisTransforms.CheckState = System.Windows.Forms.CheckState.Checked;
            applyBasisTransforms.Location = new System.Drawing.Point(21, 34);
            applyBasisTransforms.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            applyBasisTransforms.Name = "applyBasisTransforms";
            applyBasisTransforms.Size = new System.Drawing.Size(135, 24);
            applyBasisTransforms.TabIndex = 26;
            applyBasisTransforms.Text = "Convert to Y-up";
            applyBasisTransforms.UseVisualStyleBackColor = true;
            // 
            // conformantGR2BrowseBtn
            // 
            conformantGR2BrowseBtn.Enabled = false;
            conformantGR2BrowseBtn.Location = new System.Drawing.Point(559, 168);
            conformantGR2BrowseBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            conformantGR2BrowseBtn.Name = "conformantGR2BrowseBtn";
            conformantGR2BrowseBtn.Size = new System.Drawing.Size(55, 35);
            conformantGR2BrowseBtn.TabIndex = 25;
            conformantGR2BrowseBtn.Text = "...";
            conformantGR2BrowseBtn.UseVisualStyleBackColor = true;
            conformantGR2BrowseBtn.Click += conformantSkeletonBrowseBtn_Click;
            // 
            // conformantGR2Path
            // 
            conformantGR2Path.Enabled = false;
            conformantGR2Path.Location = new System.Drawing.Point(20, 171);
            conformantGR2Path.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            conformantGR2Path.Name = "conformantGR2Path";
            conformantGR2Path.Size = new System.Drawing.Size(539, 27);
            conformantGR2Path.TabIndex = 24;
            // 
            // conformToOriginal
            // 
            conformToOriginal.AutoSize = true;
            conformToOriginal.Enabled = false;
            conformToOriginal.Location = new System.Drawing.Point(20, 140);
            conformToOriginal.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            conformToOriginal.Name = "conformToOriginal";
            conformToOriginal.Size = new System.Drawing.Size(196, 24);
            conformToOriginal.TabIndex = 23;
            conformToOriginal.Text = "Conform to original GR2:";
            conformToOriginal.UseVisualStyleBackColor = true;
            conformToOriginal.Click += conformToSkeleton_CheckedChanged;
            // 
            // buildDummySkeleton
            // 
            buildDummySkeleton.AutoSize = true;
            buildDummySkeleton.Checked = true;
            buildDummySkeleton.CheckState = System.Windows.Forms.CheckState.Checked;
            buildDummySkeleton.Location = new System.Drawing.Point(20, 105);
            buildDummySkeleton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            buildDummySkeleton.Name = "buildDummySkeleton";
            buildDummySkeleton.Size = new System.Drawing.Size(187, 24);
            buildDummySkeleton.TabIndex = 22;
            buildDummySkeleton.Text = "Create dummy skeleton";
            buildDummySkeleton.UseVisualStyleBackColor = true;
            // 
            // resourceFormats
            // 
            resourceFormats.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            resourceFormats.FullRowSelect = true;
            resourceFormats.Location = new System.Drawing.Point(20, 246);
            resourceFormats.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            resourceFormats.Name = "resourceFormats";
            resourceFormats.Size = new System.Drawing.Size(592, 415);
            resourceFormats.TabIndex = 16;
            resourceFormats.UseCompatibleStateImageBehavior = false;
            resourceFormats.View = System.Windows.Forms.View.Details;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(17, 214);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(196, 20);
            label1.TabIndex = 15;
            label1.Text = "Customize resource formats:";
            // 
            // conformSkeletonFileDlg
            // 
            conformSkeletonFileDlg.Filter = "Granny GR2|*.gr2;*.lsm";
            conformSkeletonFileDlg.Title = "Select Conforming Skeleton File";
            // 
            // outputFileDlg
            // 
            outputFileDlg.Filter = "glTF/COLLADA/GR2 files|*.dae;*.gr2;*.lsm;*.gltf;*.glb";
            outputFileDlg.Title = "Select Output File";
            // 
            // inputFileDlg
            // 
            inputFileDlg.Filter = "glTF/COLLADA/GR2 files|*.dae;*.gr2;*.lsm;*.gltf;*.glb";
            inputFileDlg.Title = "Select Input File";
            // 
            // GR2Pane
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(gr2ModeTabControl);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            Name = "GR2Pane";
            Size = new System.Drawing.Size(1201, 969);
            gr2ModeTabControl.ResumeLayout(false);
            gr2SingleFileTab.ResumeLayout(false);
            gr2SingleFileTab.PerformLayout();
            gr2BatchTab.ResumeLayout(false);
            gr2BatchTab.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
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
        private System.Windows.Forms.CheckBox filterUVs;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox applyBasisTransforms;
        private System.Windows.Forms.Button conformantGR2BrowseBtn;
        private System.Windows.Forms.TextBox conformantGR2Path;
        private System.Windows.Forms.CheckBox conformToOriginal;
        private System.Windows.Forms.CheckBox buildDummySkeleton;
        private System.Windows.Forms.FolderBrowserDialog gr2OutputDirDlg;
        private System.Windows.Forms.FolderBrowserDialog gr2InputDirDlg;
        private System.Windows.Forms.OpenFileDialog conformSkeletonFileDlg;
        private System.Windows.Forms.SaveFileDialog outputFileDlg;
        private System.Windows.Forms.OpenFileDialog inputFileDlg;
        private System.Windows.Forms.CheckBox flipUVs;
        internal System.Windows.Forms.CheckBox flipMeshes;
        internal System.Windows.Forms.CheckBox flipSkeletons;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListView exportableObjects;
        private System.Windows.Forms.ColumnHeader exportableName;
        private System.Windows.Forms.ColumnHeader exportableType;
        private ExportItemSelection resourceFormats;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox conformCopySkeletons;
        private System.Windows.Forms.CheckBox meshProxy;
        private System.Windows.Forms.CheckBox meshCloth;
        private System.Windows.Forms.CheckBox meshRigid;
    }
}
