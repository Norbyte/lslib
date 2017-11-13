using System;
using System.IO;
using System.Windows.Forms;
using LSLib.Granny;
using LSLib.Granny.GR2;
using LSLib.Granny.Model;

namespace ConverterApp
{
    public partial class GR2Pane : UserControl
    {
        private Root Root;
        private MainForm Form;

        public GR2Pane(MainForm form)
        {
            Form = form;
            InitializeComponent();
            gr2BatchInputFormat.SelectedIndex = 0;
            gr2BatchOutputFormat.SelectedIndex = 1;
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
            var animationsOnly = !skinned 
                && (Root.Models == null || Root.Models.Count == 0) 
                && Root.Animations != null && Root.Animations.Count > 0;

            if (skinned)
            {
                conformToOriginal.Enabled = true;
                conformToOriginal.Text = "Conform to original GR2:";
                buildDummySkeleton.Enabled = false;
                buildDummySkeleton.Checked = false;
            }
            else if (animationsOnly)
            {
                conformToOriginal.Enabled = true;
                conformToOriginal.Text = "Copy skeleton from:";
                buildDummySkeleton.Enabled = false;
                buildDummySkeleton.Checked = false;
            }
            else
            {
                conformToOriginal.Enabled = false;
                conformToOriginal.Checked = false;
                buildDummySkeleton.Enabled = false;
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
            if (Path.GetExtension(settings.InputPath)?.ToLower() == ".gr2")
            {
                settings.InputFormat = ExportFormat.GR2;
            }
            else
            {
                settings.InputFormat = ExportFormat.DAE;
            }

            settings.OutputPath = outputPath.Text;
            if (Path.GetExtension(settings.OutputPath)?.ToLower() == ".gr2")
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
            var game = Form.GetGame();
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
            settings.FlipUVs = flipUVs.Checked;
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
            if (exc is ExportException)
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
