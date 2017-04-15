using LSLib.Granny;
using LSLib.Granny.GR2;
using LSLib.Granny.Model;
using LSLib.LS;
using LSLib.LS.LSF;
using LSLib.LS.Story;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ConverterApp
{
    enum DivGame
    {
        DOS = 0,
        DOSEE = 1,
        DOS2 = 2
    };

    public partial class MainForm : Form
    {
        private Root Root;
        private Resource Resource;
        private Story Story;

        public MainForm()
        {
            InitializeComponent();
            this.Text += String.Format(" (LSLib v{0})", Common.LibraryVersion());
            packageVersion.SelectedIndex = 0;
            compressionMethod.SelectedIndex = 4;
            gr2Game.SelectedIndex = 1;
            gr2BatchInputFormat.SelectedIndex = 0;
            gr2BatchOutputFormat.SelectedIndex = 1;
            resourceInputFormatCb.SelectedIndex = 2;
            resourceOutputFormatCb.SelectedIndex = 0;
        }

        private DivGame GetGame()
        {
            switch (gr2Game.SelectedIndex)
            {
                case 0: return DivGame.DOS;
                case 1: return DivGame.DOSEE;
                case 2: return DivGame.DOS2;
                default: throw new InvalidOperationException();
            }
        }

        private void PackageProgressUpdate(string status, long numerator, long denominator)
        {
            packageProgressLabel.Text = status;
            if (denominator == 0)
            {
                packageProgress.Value = 0;
            }
            else
            {
                packageProgress.Value = (int)(numerator * 100 / denominator);
            }

            Application.DoEvents();
        }
        
        private void UpdateExportableObjects()
        {
            exportableObjects.Items.Clear();

            if (Root.Models != null)
            {
                foreach (var model in Root.Models)
                {
                    var item = new ListViewItem(new string[] { model.Name, "Model" });
                    item.Checked = true;
                    exportableObjects.Items.Add(item);
                }
            }

            if (Root.Skeletons != null)
            {
                foreach (var skeleton in Root.Skeletons)
                {
                    var item = new ListViewItem(new string[] { skeleton.Name, "Skeleton" });
                    item.Checked = true;
                    exportableObjects.Items.Add(item);
                }
            }

            if (Root.Animations != null)
            {
                foreach (var animation in Root.Animations)
                {
                    var item = new ListViewItem(new string[] { animation.Name, "Animation" });
                    item.Checked = true;
                    exportableObjects.Items.Add(item);
                }
            }
        }

        private void UpdateResourceFormats()
        {
            resourceFormats.Items.Clear();

            if (Root.Meshes != null)
            {
                foreach (var mesh in Root.Meshes)
                {
                    var item = new ListViewItem(new string[] { mesh.Name, "Mesh", "Automatic" });
                    resourceFormats.Items.Add(item);
                }
            }

            if (Root.TrackGroups != null)
            {
                foreach (var trackGroup in Root.TrackGroups)
                {
                    foreach (var track in trackGroup.TransformTracks)
                    {
                        if (track.PositionCurve.CurveData.NumKnots() > 2)
                        {
                            var item = new ListViewItem(new string[] { track.Name, "Position Track", "Automatic" });
                            resourceFormats.Items.Add(item);
                        }

                        if (track.OrientationCurve.CurveData.NumKnots() > 2)
                        {
                            var item = new ListViewItem(new string[] { track.Name, "Rotation Track", "Automatic" });
                            resourceFormats.Items.Add(item);
                        }

                        if (track.ScaleShearCurve.CurveData.NumKnots() > 2)
                        {
                            var item = new ListViewItem(new string[] { track.Name, "Scale/Shear Track", "Automatic" });
                            resourceFormats.Items.Add(item);
                        }
                    }
                }
            }
        }

        private void UpdateInputState()
        {
            var skinned = (Root.Skeletons != null && Root.Skeletons.Count > 0);
            conformToOriginal.Enabled = skinned;
            if (!skinned)
            {
                conformToOriginal.Checked = false;
            }

            buildDummySkeleton.Enabled = !skinned;
            if (skinned)
            {
                buildDummySkeleton.Checked = false;
            }

            UpdateExportableObjects();
            UpdateResourceFormats();

            saveOutputBtn.Enabled = true;
        }

        private void LoadFile(string inPath)
        {
            Root = GR2Utils.LoadModel(inPath);
            UpdateInputState();
        }

        private void UpdateExporterSettings(ExporterOptions settings)
        {
            UpdateCommonExporterSettings(settings);

            settings.InputPath = inputPath.Text;
            if (settings.InputPath.Substring(settings.InputPath.Length - 4).ToLower() == ".gr2")
            {
                settings.InputFormat = ExportFormat.GR2;
            }
            else
            {
                settings.InputFormat = ExportFormat.DAE;
            }

            settings.OutputPath = outputPath.Text;
            if (settings.OutputPath.Substring(settings.OutputPath.Length - 4).ToLower() == ".gr2")
            {
                settings.OutputFormat = ExportFormat.GR2;
            }
            else
            {
                settings.OutputFormat = ExportFormat.DAE;
            }

            foreach (var item in resourceFormats.Items)
            {
                var setting = item as ListViewItem;
                var name = setting.SubItems[0].Text;
                var type = setting.SubItems[1].Text;
                var value = setting.SubItems[2].Text;
                if (type == "Mesh" && value != "Automatic")
                {
                    settings.VertexFormats.Add(name, value);
                }
            }
        }

        private void UpdateCommonExporterSettings(ExporterOptions settings)
        {
            var game = GetGame();
            if (game == DivGame.DOS)
            {
                settings.Is64Bit = false;
                settings.AlternateSignature = false;
                settings.VersionTag = Header.Tag_DOS;
            }
            else
            {
                settings.Is64Bit = true;
                settings.AlternateSignature = true;
                settings.VersionTag = Header.Tag_DOSEE;
            }

            settings.ExportNormals = exportNormals.Checked;
            settings.ExportTangents = exportTangents.Checked;
            settings.ExportUVs = exportUVs.Checked;
            settings.RecalculateNormals = recalculateNormals.Checked;
            settings.RecalculateTangents = recalculateTangents.Checked;
            settings.RecalculateIWT = recalculateJointIWT.Checked;
            settings.BuildDummySkeleton = buildDummySkeleton.Checked;
            settings.CompactIndices = use16bitIndex.Checked;
            settings.DeduplicateVertices = deduplicateVertices.Checked;
            settings.DeduplicateUVs = filterUVs.Checked;
            settings.ApplyBasisTransforms = applyBasisTransforms.Checked;
            settings.UseObsoleteVersionTag = forceLegacyVersion.Checked;

            if (conformToOriginal.Checked && conformantGR2Path.Text.Length > 0)
            {
                settings.ConformGR2Path = conformantGR2Path.Text;
            }
            else
            {
                settings.ConformGR2Path = null;
            }
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void inputFileBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = inputFileDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                inputPath.Text = inputFileDlg.FileName;
            }
        }

        private void loadInputBtn_Click(object sender, EventArgs e)
        {
            try
            {
                LoadFile(inputPath.Text);
            }
            catch (ParsingException exc)
            {
                MessageBox.Show("Import failed!\r\n\r\n" + exc.Message, "Import Failed");
            }
            catch (Exception exc)
            {
                MessageBox.Show("Internal error!\r\n\r\n" + exc.ToString(), "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void recalculateJointIWT_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void conformToSkeleton_CheckedChanged(object sender, EventArgs e)
        {
            conformantGR2Path.Enabled = conformToOriginal.Checked;
            conformantGR2BrowseBtn.Enabled = conformToOriginal.Checked;
        }

        private void outputFileBrowserBtn_Click(object sender, EventArgs e)
        {
            var result = outputFileDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                outputPath.Text = outputFileDlg.FileName;
            }
        }

        private void conformantSkeletonBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = conformSkeletonFileDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                conformantGR2Path.Text = conformSkeletonFileDlg.FileName;
            }
        }

        private void saveOutputBtn_Click(object sender, EventArgs e)
        {
            var exporter = new Exporter();
            UpdateExporterSettings(exporter.Options);
            try
            {
                exporter.Export();
                MessageBox.Show("Export completed successfully.");
            }
            catch (Exception exc)
            {
                GR2ConversionError(exporter.Options.InputPath, exporter.Options.OutputPath, exc);
            }
        }

        private void packageBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = packageFileDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                packagePath.Text = packageFileDlg.FileName;
            }
        }

        private void exportPathBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = exportPathDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                extractionPath.Text = exportPathDlg.SelectedPath;
            }
        }

        private void extractPackageBtn_Click(object sender, EventArgs e)
        {
            extractPackageBtn.Enabled = false;
            try
            {
                var packager = new Packager();
                packager.progressUpdate += this.PackageProgressUpdate;
                packager.UncompressPackage(packagePath.Text, extractionPath.Text);
                MessageBox.Show("Package extracted successfully.");
            }
            catch (Exception exc)
            {
                MessageBox.Show("Internal error!\r\n\r\n" + exc.ToString(), "Extraction Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                packageProgressLabel.Text = "";
                packageProgress.Value = 0;
                extractPackageBtn.Enabled = true;
            }
        }

        private void createPackageBtn_Click(object sender, EventArgs e)
        {
            createPackageBtn.Enabled = false;
            try
            {
                uint version = Package.CurrentVersion;
                switch (packageVersion.SelectedIndex)
                {
                    case 0:
                        version = 13;
                        break;

                    case 1:
                        version = 10;
                        break;

                    case 2:
                        version = 9;
                        break;

                    case 3:
                        version = 7;
                        break;
                }

                CompressionMethod compression = CompressionMethod.None;
                bool fastCompression = true;
                switch (compressionMethod.SelectedIndex)
                {
                    case 1:
                        compression = CompressionMethod.Zlib;
                        break;

                    case 2:
                        compression = CompressionMethod.Zlib;
                        fastCompression = false;
                        break;

                    case 3:
                        compression = CompressionMethod.LZ4;
                        break;

                    case 4:
                        compression = CompressionMethod.LZ4;
                        fastCompression = false;
                        break;
                }

                // Fallback to Zlib, if the package version doesn't support LZ4
                if (compression == CompressionMethod.LZ4 && version <= 9)
                {
                    compression = CompressionMethod.Zlib;
                }

                var packager = new Packager();
                packager.progressUpdate += this.PackageProgressUpdate;
                packager.CreatePackage(packagePath.Text, extractionPath.Text, version, compression, fastCompression);
                MessageBox.Show("Package created successfully.");
            }
            catch (Exception exc)
            {
                MessageBox.Show("Internal error!\r\n\r\n" + exc.ToString(), "Package Build Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                packageProgressLabel.Text = "";
                packageProgress.Value = 0;
                createPackageBtn.Enabled = true;
            }
        }

        private void resourceConvertBtn_Click(object sender, EventArgs e)
        {
            try
            {
                Resource = ResourceUtils.LoadResource(resourceInputPath.Text);
                var format = ResourceUtils.ExtensionToResourceFormat(resourceOutputPath.Text);
                int outputVersion = -1;
                if (GetGame() == DivGame.DOS2)
                {
                    outputVersion = (int)FileVersion.VerExtendedNodes;
                }
                else
                {
                    outputVersion = (int)FileVersion.VerChunkedCompress;
                }
                ResourceUtils.SaveResource(Resource, resourceOutputPath.Text, format, outputVersion);
                MessageBox.Show("Resource saved successfully.");
            }
            catch (Exception exc)
            {
                MessageBox.Show("Internal error!\r\n\r\n" + exc.ToString(), "Conversion Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void resourceInputBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = resourceInputFileDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                resourceInputPath.Text = resourceInputFileDlg.FileName;
            }
        }

        private void resourceOutputBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = resourceOutputFileDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                resourceOutputPath.Text = resourceOutputFileDlg.FileName;
            }
        }

        private void resourceInputPathBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = resourceInputPathDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                resourceInputDir.Text = resourceInputPathDlg.SelectedPath;
            }
        }

        private void resourceOutputPathBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = resourceOutputPathDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                resourceOutputDir.Text = resourceOutputPathDlg.SelectedPath;
            }
        }

        public void ResourceProgressUpdate(string status, long numerator, long denominator)
        {
            resourceProgressLabel.Text = status;
            if (denominator == 0)
            {
                resourceConversionProgress.Value = 0;
            }
            else
            {
                resourceConversionProgress.Value = (int)(numerator * 100 / denominator);
            }

            Application.DoEvents();
        }

        private void resourceBulkConvertBtn_Click(object sender, EventArgs e)
        {
            ResourceFormat inputFormat = ResourceFormat.LSX;
            switch (resourceInputFormatCb.SelectedIndex)
            {
                case 0:
                    inputFormat = ResourceFormat.LSX;
                    break;

                case 1:
                    inputFormat = ResourceFormat.LSB;
                    break;

                case 2:
                    inputFormat = ResourceFormat.LSF;
                    break;
            }

            ResourceFormat outputFormat = ResourceFormat.LSF;
            int outputVersion = -1;
            switch (resourceOutputFormatCb.SelectedIndex)
            {
                case 0:
                    outputFormat = ResourceFormat.LSX;
                    break;

                case 1:
                    outputFormat = ResourceFormat.LSB;
                    break;

                case 2:
                    outputFormat = ResourceFormat.LSF;
                    if (GetGame() == DivGame.DOS2)
                    {
                        outputVersion = (int)FileVersion.VerExtendedNodes;
                    }
                    else
                    {
                        outputVersion = (int)FileVersion.VerChunkedCompress;
                    }
                    break;
            }

            try
            {
                resourceConvertBtn.Enabled = false;
                var utils = new ResourceUtils();
                utils.progressUpdate += this.ResourceProgressUpdate;
                utils.ConvertResources(resourceInputDir.Text, resourceOutputDir.Text, inputFormat, outputFormat, outputVersion);
                MessageBox.Show("Resources converted successfully.");
            }
            catch (Exception exc)
            {
                MessageBox.Show("Internal error!\r\n\r\n" + exc.ToString(), "Conversion Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                resourceProgressLabel.Text = "";
                resourceConversionProgress.Value = 0;
                resourceConvertBtn.Enabled = true;
            }
        }

        private void storyFileBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = storyPathDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                storyFilePath.Text = storyPathDlg.FileName;
            }
        }

        private void goalPathBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = goalPathDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                goalPath.Text = goalPathDlg.SelectedPath;
            }
        }

        private void loadStoryBtn_Click(object sender, EventArgs e)
        {
            using (var file = new FileStream(storyFilePath.Text, FileMode.Open, FileAccess.Read))
            {
                var reader = new StoryReader();
                Story = reader.Read(file);

                databaseSelectorCb.Items.Clear();
                foreach (var database in Story.Databases)
                {
                    var name = "(Unnamed)";
                    var owner = database.Value.OwnerNode;
                    if (owner != null && owner.Name.Length > 0)
                    {
                        name = String.Format("{0}({1})", owner.Name, owner.NumParams);
                    }
                    else if (owner != null)
                    {
                        name = String.Format("<{0}>", owner.TypeName());
                    }

                    name += String.Format(" #{0} ({1} rows)", database.Key, database.Value.Facts.Count);

                    databaseSelectorCb.Items.Add(name);
                }
            }

            if (databaseSelectorCb.Items.Count > 0)
            {
                databaseSelectorCb.SelectedIndex = 0;
            }

            MessageBox.Show("Story file loaded successfully.");

        private void decompileStoryBtn_Click(object sender, EventArgs e)
        {
            if (Story == null)
            {
                MessageBox.Show("A story file must be loaded before exporting.", "Story export failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var debugPath = goalPath.Text + "/debug.log";
            using (var debugFile = new FileStream(debugPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(debugFile))
            {
                Story.DebugDump(writer);
            }

            var unassignedPath = goalPath.Text + "/UNASSIGNED_RULES.txt";
            using (var goalFile = new FileStream(unassignedPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(goalFile))
            {
                var dummyGoal = new Goal();
                dummyGoal.ExitCalls = new List<Call>();
                dummyGoal.InitCalls = new List<Call>();
                dummyGoal.ParentGoals = new List<uint>();
                dummyGoal.SubGoals = new List<uint>();
                dummyGoal.Name = "UNASSIGNED_RULES";
                dummyGoal.Index = 0;
                dummyGoal.MakeScript(writer, Story);
            }

            foreach (var goal in Story.Goals)
            {
                var filePath = goalPath.Text + "/" + goal.Value.Name + ".txt";
                using (var goalFile = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(goalFile))
                {
                    goal.Value.MakeScript(writer, Story);
                }
            }

            MessageBox.Show("Story unpacked successfully.");
        }

        private void databaseSelectorCb_SelectedIndexChanged(object sender, EventArgs e)
        {
            databaseGrid.DataSource = null;
            databaseGrid.Columns.Clear();

            if (databaseSelectorCb.SelectedIndex != -1)
            {
                var database = Story.Databases[(uint)databaseSelectorCb.SelectedIndex + 1];
                databaseGrid.DataSource = database.Facts;

                for (var i = 0; i < database.Parameters.Types.Count; i++)
                {
                    databaseGrid.Columns[i].HeaderText = i.ToString() + " (" + Story.Types[database.Parameters.Types[i]].Name + ")";
                }
            }
        }

        private void gr2Game_SelectedIndexChanged(object sender, EventArgs e)
        {
            DivGame game = GetGame();
            use16bitIndex.Checked = game == DivGame.DOSEE || game == DivGame.DOS2;
        }

        private void gr2BatchInputBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = gr2InputDirDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                gr2BatchInputDir.Text = gr2InputDirDlg.SelectedPath;
            }
        }

        private void gr2BatchOutputBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = gr2OutputDirDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                gr2BatchOutputDir.Text = gr2OutputDirDlg.SelectedPath;
            }
        }

        private void GR2ProgressUpdate(string status, long numerator, long denominator)
        {
            gr2BatchProgressLabel.Text = status;
            if (denominator == 0)
            {
                gr2BatchProgressBar.Value = 0;
            }
            else
            {
                gr2BatchProgressBar.Value = (int)(numerator * 100 / denominator);
            }

            Application.DoEvents();
        }

        private void GR2ConversionError(string inputPath, string outputPath, Exception exc)
        {
            var pathText = "File: " + inputPath + Environment.NewLine;
            if  (exc is ExportException)
            {
                var msg = "Export failed!" + Environment.NewLine + Environment.NewLine + 
                    pathText + Environment.NewLine + exc.Message;
                MessageBox.Show(msg, "Export Failed");
            }
            else if (exc is ParsingException)
            {
                var msg = "Export failed!" + Environment.NewLine + Environment.NewLine +
                    pathText + Environment.NewLine + exc.Message;
                MessageBox.Show(msg, "Export Failed");
            }
            else
            {
                var msg = "Internal error!" + Environment.NewLine + Environment.NewLine +
                    pathText + Environment.NewLine + exc.ToString();
                MessageBox.Show(msg, "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void gr2BatchConvertBtn_Click(object sender, EventArgs e)
        {
            gr2BatchConvertBtn.Enabled = false;
            var exporter = new Exporter();
            UpdateCommonExporterSettings(exporter.Options);

            if (gr2BatchInputFormat.SelectedIndex == 0)
                exporter.Options.InputFormat = ExportFormat.GR2;
            else
                exporter.Options.InputFormat = ExportFormat.DAE;

            if (gr2BatchOutputFormat.SelectedIndex == 0)
                exporter.Options.OutputFormat = ExportFormat.GR2;
            else
                exporter.Options.OutputFormat = ExportFormat.DAE;

            var batchConverter = new GR2Utils();
            batchConverter.progressUpdate = GR2ProgressUpdate;
            batchConverter.conversionError = GR2ConversionError;
            batchConverter.ConvertModels(gr2BatchInputDir.Text, gr2BatchOutputDir.Text, exporter);
            gr2BatchConvertBtn.Enabled = true;
            MessageBox.Show("Batch export completed.");
        }
    }
}
