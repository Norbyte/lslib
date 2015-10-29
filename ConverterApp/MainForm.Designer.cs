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
            this.inputPath = new System.Windows.Forms.TextBox();
            this.lblSrcPath = new System.Windows.Forms.Label();
            this.inputFileBrowseBtn = new System.Windows.Forms.Button();
            this.outputFileBrowserBtn = new System.Windows.Forms.Button();
            this.lblOutputPath = new System.Windows.Forms.Label();
            this.outputPath = new System.Windows.Forms.TextBox();
            this.recalculateNormals = new System.Windows.Forms.CheckBox();
            this.recalculateTangents = new System.Windows.Forms.CheckBox();
            this.deduplicateVertices = new System.Windows.Forms.CheckBox();
            this.filterUVs = new System.Windows.Forms.CheckBox();
            this.recalculateJointIWT = new System.Windows.Forms.CheckBox();
            this.loadInputBtn = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.conformantSkeletonBrowseBtn = new System.Windows.Forms.Button();
            this.conformantSkeletonPath = new System.Windows.Forms.TextBox();
            this.conformToSkeleton = new System.Windows.Forms.CheckBox();
            this.buildDummySkeleton = new System.Windows.Forms.CheckBox();
            this.use16bitIndex = new System.Windows.Forms.CheckBox();
            this.forceLegacyVersion = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.exportableObjects = new System.Windows.Forms.ListView();
            this.exportableName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.exportableType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.exportUVs = new System.Windows.Forms.CheckBox();
            this.exportTangents = new System.Windows.Forms.CheckBox();
            this.exportNormals = new System.Windows.Forms.CheckBox();
            this.saveOutputBtn = new System.Windows.Forms.Button();
            this.outputFileDlg = new System.Windows.Forms.SaveFileDialog();
            this.conformSkeletonFileDlg = new System.Windows.Forms.OpenFileDialog();
            this.applyBasisTransforms = new System.Windows.Forms.CheckBox();
            this.resourceFormats = new ConverterApp.ExportItemSelection();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // inputFileDlg
            // 
            this.inputFileDlg.Filter = "COLLADA/GR2 files|*.dae;*.gr2";
            this.inputFileDlg.Title = "Select Input File";
            this.inputFileDlg.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk);
            // 
            // inputPath
            // 
            this.inputPath.Location = new System.Drawing.Point(16, 24);
            this.inputPath.Name = "inputPath";
            this.inputPath.Size = new System.Drawing.Size(660, 20);
            this.inputPath.TabIndex = 0;
            // 
            // lblSrcPath
            // 
            this.lblSrcPath.AutoSize = true;
            this.lblSrcPath.Location = new System.Drawing.Point(13, 8);
            this.lblSrcPath.Name = "lblSrcPath";
            this.lblSrcPath.Size = new System.Drawing.Size(74, 13);
            this.lblSrcPath.TabIndex = 1;
            this.lblSrcPath.Text = "Input file path:";
            // 
            // inputFileBrowseBtn
            // 
            this.inputFileBrowseBtn.Location = new System.Drawing.Point(674, 22);
            this.inputFileBrowseBtn.Name = "inputFileBrowseBtn";
            this.inputFileBrowseBtn.Size = new System.Drawing.Size(41, 23);
            this.inputFileBrowseBtn.TabIndex = 2;
            this.inputFileBrowseBtn.Text = "...";
            this.inputFileBrowseBtn.UseVisualStyleBackColor = true;
            this.inputFileBrowseBtn.Click += new System.EventHandler(this.inputFileBrowseBtn_Click);
            // 
            // outputFileBrowserBtn
            // 
            this.outputFileBrowserBtn.Location = new System.Drawing.Point(674, 68);
            this.outputFileBrowserBtn.Name = "outputFileBrowserBtn";
            this.outputFileBrowserBtn.Size = new System.Drawing.Size(41, 23);
            this.outputFileBrowserBtn.TabIndex = 5;
            this.outputFileBrowserBtn.Text = "...";
            this.outputFileBrowserBtn.UseVisualStyleBackColor = true;
            this.outputFileBrowserBtn.Click += new System.EventHandler(this.outputFileBrowserBtn_Click);
            // 
            // lblOutputPath
            // 
            this.lblOutputPath.AutoSize = true;
            this.lblOutputPath.Location = new System.Drawing.Point(13, 54);
            this.lblOutputPath.Name = "lblOutputPath";
            this.lblOutputPath.Size = new System.Drawing.Size(82, 13);
            this.lblOutputPath.TabIndex = 4;
            this.lblOutputPath.Text = "Output file path:";
            // 
            // outputPath
            // 
            this.outputPath.Location = new System.Drawing.Point(16, 70);
            this.outputPath.Name = "outputPath";
            this.outputPath.Size = new System.Drawing.Size(660, 20);
            this.outputPath.TabIndex = 3;
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
            // loadInputBtn
            // 
            this.loadInputBtn.Location = new System.Drawing.Point(742, 24);
            this.loadInputBtn.Name = "loadInputBtn";
            this.loadInputBtn.Size = new System.Drawing.Size(151, 23);
            this.loadInputBtn.TabIndex = 19;
            this.loadInputBtn.Text = "Import";
            this.loadInputBtn.UseVisualStyleBackColor = true;
            this.loadInputBtn.Click += new System.EventHandler(this.loadInputBtn_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.applyBasisTransforms);
            this.groupBox1.Controls.Add(this.conformantSkeletonBrowseBtn);
            this.groupBox1.Controls.Add(this.conformantSkeletonPath);
            this.groupBox1.Controls.Add(this.conformToSkeleton);
            this.groupBox1.Controls.Add(this.buildDummySkeleton);
            this.groupBox1.Controls.Add(this.use16bitIndex);
            this.groupBox1.Controls.Add(this.forceLegacyVersion);
            this.groupBox1.Controls.Add(this.resourceFormats);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(425, 105);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(476, 400);
            this.groupBox1.TabIndex = 22;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "GR2 Export Options";
            // 
            // conformantSkeletonBrowseBtn
            // 
            this.conformantSkeletonBrowseBtn.Enabled = false;
            this.conformantSkeletonBrowseBtn.Location = new System.Drawing.Point(423, 106);
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
            this.conformantSkeletonPath.Location = new System.Drawing.Point(19, 108);
            this.conformantSkeletonPath.Name = "conformantSkeletonPath";
            this.conformantSkeletonPath.Size = new System.Drawing.Size(405, 20);
            this.conformantSkeletonPath.TabIndex = 24;
            // 
            // conformToSkeleton
            // 
            this.conformToSkeleton.AutoSize = true;
            this.conformToSkeleton.Enabled = false;
            this.conformToSkeleton.Location = new System.Drawing.Point(19, 88);
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
            this.buildDummySkeleton.Location = new System.Drawing.Point(19, 65);
            this.buildDummySkeleton.Name = "buildDummySkeleton";
            this.buildDummySkeleton.Size = new System.Drawing.Size(136, 17);
            this.buildDummySkeleton.TabIndex = 22;
            this.buildDummySkeleton.Text = "Create dummy skeleton";
            this.buildDummySkeleton.UseVisualStyleBackColor = true;
            // 
            // use16bitIndex
            // 
            this.use16bitIndex.AutoSize = true;
            this.use16bitIndex.Location = new System.Drawing.Point(19, 42);
            this.use16bitIndex.Name = "use16bitIndex";
            this.use16bitIndex.Size = new System.Drawing.Size(142, 17);
            this.use16bitIndex.TabIndex = 18;
            this.use16bitIndex.Text = "Store compact tri indices";
            this.use16bitIndex.UseVisualStyleBackColor = true;
            // 
            // forceLegacyVersion
            // 
            this.forceLegacyVersion.AutoSize = true;
            this.forceLegacyVersion.Location = new System.Drawing.Point(19, 19);
            this.forceLegacyVersion.Name = "forceLegacyVersion";
            this.forceLegacyVersion.Size = new System.Drawing.Size(167, 17);
            this.forceLegacyVersion.TabIndex = 17;
            this.forceLegacyVersion.Text = "Force legacy GR2 version tag";
            this.forceLegacyVersion.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 151);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(139, 13);
            this.label1.TabIndex = 15;
            this.label1.Text = "Customize resource formats:";
            // 
            // groupBox2
            // 
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
            this.groupBox2.Location = new System.Drawing.Point(16, 105);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(390, 400);
            this.groupBox2.TabIndex = 23;
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
            this.exportableObjects.CheckBoxes = true;
            this.exportableObjects.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.exportableName,
            this.exportableType});
            this.exportableObjects.Enabled = false;
            this.exportableObjects.FullRowSelect = true;
            this.exportableObjects.Location = new System.Drawing.Point(9, 167);
            this.exportableObjects.Name = "exportableObjects";
            this.exportableObjects.Size = new System.Drawing.Size(368, 219);
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
            // saveOutputBtn
            // 
            this.saveOutputBtn.Enabled = false;
            this.saveOutputBtn.Location = new System.Drawing.Point(742, 67);
            this.saveOutputBtn.Name = "saveOutputBtn";
            this.saveOutputBtn.Size = new System.Drawing.Size(151, 23);
            this.saveOutputBtn.TabIndex = 24;
            this.saveOutputBtn.Text = "Export";
            this.saveOutputBtn.UseVisualStyleBackColor = true;
            this.saveOutputBtn.Click += new System.EventHandler(this.saveOutputBtn_Click);
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
            // applyBasisTransforms
            // 
            this.applyBasisTransforms.AutoSize = true;
            this.applyBasisTransforms.Checked = true;
            this.applyBasisTransforms.CheckState = System.Windows.Forms.CheckState.Checked;
            this.applyBasisTransforms.Location = new System.Drawing.Point(249, 19);
            this.applyBasisTransforms.Name = "applyBasisTransforms";
            this.applyBasisTransforms.Size = new System.Drawing.Size(185, 17);
            this.applyBasisTransforms.TabIndex = 26;
            this.applyBasisTransforms.Text = "Apply basis transformation to Y-up";
            this.applyBasisTransforms.UseVisualStyleBackColor = true;
            // 
            // resourceFormats
            // 
            this.resourceFormats.FullRowSelect = true;
            this.resourceFormats.Location = new System.Drawing.Point(19, 167);
            this.resourceFormats.Name = "resourceFormats";
            this.resourceFormats.Size = new System.Drawing.Size(445, 219);
            this.resourceFormats.TabIndex = 16;
            this.resourceFormats.UseCompatibleStateImageBehavior = false;
            this.resourceFormats.View = System.Windows.Forms.View.Details;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(913, 517);
            this.Controls.Add(this.saveOutputBtn);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.loadInputBtn);
            this.Controls.Add(this.outputFileBrowserBtn);
            this.Controls.Add(this.lblOutputPath);
            this.Controls.Add(this.outputPath);
            this.Controls.Add(this.inputFileBrowseBtn);
            this.Controls.Add(this.lblSrcPath);
            this.Controls.Add(this.inputPath);
            this.Name = "MainForm";
            this.Text = "GR2 Converter";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog inputFileDlg;
        private System.Windows.Forms.TextBox inputPath;
        private System.Windows.Forms.Label lblSrcPath;
        private System.Windows.Forms.Button inputFileBrowseBtn;
        private System.Windows.Forms.Button outputFileBrowserBtn;
        private System.Windows.Forms.Label lblOutputPath;
        private System.Windows.Forms.TextBox outputPath;
        private System.Windows.Forms.CheckBox recalculateNormals;
        private System.Windows.Forms.CheckBox recalculateTangents;
        private System.Windows.Forms.CheckBox deduplicateVertices;
        private System.Windows.Forms.CheckBox filterUVs;
        private System.Windows.Forms.CheckBox recalculateJointIWT;
        private System.Windows.Forms.Button loadInputBtn;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox buildDummySkeleton;
        private System.Windows.Forms.CheckBox use16bitIndex;
        private System.Windows.Forms.CheckBox forceLegacyVersion;
        private ExportItemSelection resourceFormats;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListView exportableObjects;
        private System.Windows.Forms.ColumnHeader exportableName;
        private System.Windows.Forms.ColumnHeader exportableType;
        private System.Windows.Forms.CheckBox exportUVs;
        private System.Windows.Forms.CheckBox exportTangents;
        private System.Windows.Forms.CheckBox exportNormals;
        private System.Windows.Forms.Button saveOutputBtn;
        private System.Windows.Forms.Button conformantSkeletonBrowseBtn;
        private System.Windows.Forms.TextBox conformantSkeletonPath;
        private System.Windows.Forms.CheckBox conformToSkeleton;
        private System.Windows.Forms.SaveFileDialog outputFileDlg;
        private System.Windows.Forms.OpenFileDialog conformSkeletonFileDlg;
        private System.Windows.Forms.CheckBox applyBasisTransforms;
    }
}

