using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LSLib.Granny;
using LSLib.Granny.GR2;
using LSLib.Granny.Model;
using LSLib.LS.Enums;
using Animation = LSLib.Granny.Model.Animation;
using Mesh = LSLib.Granny.Model.Mesh;

namespace ConverterApp
{
    public partial class GR2Pane : UserControl
    {
        private readonly MainForm _form;
        private Root _root;

        public GR2Pane(MainForm form)
        {
            _form = form;
            InitializeComponent();
            gr2BatchInputFormat.SelectedIndex = 0;
            gr2BatchOutputFormat.SelectedIndex = 1;
            gr2ExtraProps.SelectedIndex = 0;
        }

        private void UpdateExportableObjects()
        {
            exportableObjects.Items.Clear();

            if (_root.Models != null)
            {
                foreach (Model model in _root.Models)
                {
                    // ReSharper disable once UseObjectOrCollectionInitializer
                    var item = new ListViewItem(new[]
                    {
                        model.Name,
                        "Model"
                    });
                    item.Checked = true;
                    exportableObjects.Items.Add(item);
                }
            }

            if (_root.Skeletons != null)
            {
                foreach (Skeleton skeleton in _root.Skeletons)
                {
                    // ReSharper disable once UseObjectOrCollectionInitializer
                    var item = new ListViewItem(new[]
                    {
                        skeleton.Name,
                        "Skeleton"
                    });
                    item.Checked = true;
                    exportableObjects.Items.Add(item);
                }
            }

            // ReSharper disable once InvertIf
            if (_root.Animations != null)
            {
                foreach (Animation animation in _root.Animations)
                {
                    // ReSharper disable once UseObjectOrCollectionInitializer
                    var item = new ListViewItem(new[]
                    {
                        animation.Name,
                        "Animation"
                    });
                    item.Checked = true;
                    exportableObjects.Items.Add(item);
                }
            }
        }

        private void UpdateResourceFormats()
        {
            resourceFormats.Items.Clear();

            if (_root.Meshes != null)
            {
                foreach (Mesh mesh in _root.Meshes)
                {
                    var item = new ListViewItem(new[]
                    {
                        mesh.Name,
                        "Mesh",
                        "Automatic"
                    });
                    resourceFormats.Items.Add(item);
                }
            }

            // ReSharper disable once InvertIf
            if (_root.TrackGroups != null)
            {
                foreach (TrackGroup trackGroup in _root.TrackGroups)
                {
                    foreach (TransformTrack track in trackGroup.TransformTracks)
                    {
                        if (track.PositionCurve.CurveData.NumKnots() > 2)
                        {
                            var item = new ListViewItem(new[]
                            {
                                track.Name,
                                "Position Track",
                                "Automatic"
                            });
                            resourceFormats.Items.Add(item);
                        }

                        if (track.OrientationCurve.CurveData.NumKnots() > 2)
                        {
                            var item = new ListViewItem(new[]
                            {
                                track.Name,
                                "Rotation Track",
                                "Automatic"
                            });
                            resourceFormats.Items.Add(item);
                        }

                        // ReSharper disable once InvertIf
                        if (track.ScaleShearCurve.CurveData.NumKnots() > 2)
                        {
                            var item = new ListViewItem(new[]
                            {
                                track.Name,
                                "Scale/Shear Track",
                                "Automatic"
                            });
                            resourceFormats.Items.Add(item);
                        }
                    }
                }
            }
        }

        private void UpdateInputState()
        {
            bool skinned = _root.Skeletons != null && _root.Skeletons.Count > 0;
            bool animationsOnly = !skinned && (_root.Models == null || _root.Models.Count == 0) && _root.Animations != null && _root.Animations.Count > 0;

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
                buildDummySkeleton.Enabled = true;
                buildDummySkeleton.Checked = true;
            }

            var modelType = DivinityHelpers.DetermineModelType(_root);
            switch (modelType)
            {
                case DivinityModelType.Undefined:
                case DivinityModelType.Normal:
                    gr2ExtraProps.SelectedIndex = 0;
                    break;

                case DivinityModelType.Rigid:
                    gr2ExtraProps.SelectedIndex = 1;
                    break;

                case DivinityModelType.Cloth:
                    gr2ExtraProps.SelectedIndex = 2;
                    break;

                case DivinityModelType.MeshProxy:
                    gr2ExtraProps.SelectedIndex = 3;
                    break;
            }

            UpdateExportableObjects();
            UpdateResourceFormats();

            saveOutputBtn.Enabled = true;
        }

        private void LoadFile(string inPath)
        {
            _root = GR2Utils.LoadModel(inPath);
            UpdateInputState();
        }

        private void UpdateExporterSettings(ExporterOptions settings)
        {
            UpdateCommonExporterSettings(settings);

            settings.InputPath = inputPath.Text;
            var inputExtension = Path.GetExtension(settings.InputPath)?.ToLower();
            bool inputIsGr2 = inputExtension == ".gr2" || inputExtension == ".lsm";
            settings.InputFormat = inputIsGr2 ? ExportFormat.GR2 : ExportFormat.DAE;

            settings.OutputPath = outputPath.Text;
            var outputExtension = Path.GetExtension(settings.OutputPath)?.ToLower();
            bool outputIsGr2 = outputExtension == ".gr2" || outputExtension == ".lsm";
            settings.OutputFormat = outputIsGr2 ? ExportFormat.GR2 : ExportFormat.DAE;

            foreach (ListViewItem setting in from object item in resourceFormats.Items select item as ListViewItem)
            {
                string name = setting.SubItems[0].Text;
                string type = setting.SubItems[1].Text;
                string value = setting.SubItems[2].Text;
                if (type == "Mesh" && value != "Automatic")
                {
                    // TODO - support for name -> format translation
                    throw new NotImplementedException("Custom vertex formats not supported");
                    // settings.VertexFormats.Add(name, value);
                }
            }
        }

        private void UpdateCommonExporterSettings(ExporterOptions settings)
        {
            Game game = _form.GetGame();
            if (game == Game.DivinityOriginalSin)
            {
                settings.Is64Bit = false;
                settings.AlternateSignature = false;
                settings.VersionTag = Header.Tag_DOS;
                settings.ModelInfoFormat = DivinityModelInfoFormat.None;
            }
            else
            {
                settings.Is64Bit = true;
                settings.AlternateSignature = true;
                settings.VersionTag = Header.Tag_DOSEE;

                if (game == Game.DivinityOriginalSinEE)
                {
                    settings.ModelInfoFormat = DivinityModelInfoFormat.UserDefinedProperties;
                }
                else
                {
                    settings.ModelInfoFormat = DivinityModelInfoFormat.LSMv1;
                }
            }

            settings.ExportNormals = exportNormals.Checked;
            settings.ExportTangents = exportTangents.Checked;
            settings.ExportUVs = exportUVs.Checked;
            settings.ExportColors = exportColors.Checked;
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
            settings.FlipMesh = flipMeshes.Checked;
            settings.FlipSkeleton = flipSkeletons.Checked;

            switch (gr2ExtraProps.SelectedIndex)
            {
                case 0: settings.ModelType = DivinityModelType.Normal; break;
                case 1: settings.ModelType = DivinityModelType.Rigid; break;
                case 2: settings.ModelType = DivinityModelType.Cloth; break;
                case 3: settings.ModelType = DivinityModelType.MeshProxy; break;
                default: throw new Exception("Unknown model type selected");
            }

            settings.ConformGR2Path = conformToOriginal.Checked && conformantGR2Path.Text.Length > 0 ? conformantGR2Path.Text : null;
        }

        private void inputFileBrowseBtn_Click(object sender, EventArgs e)
        {
            if (inputFileDlg.ShowDialog(this) == DialogResult.OK)
            {
                inputPath.Text = inputFileDlg.FileName;
            }
        }

        private void loadInputBtn_Click(object sender, EventArgs e)
        {
            string nl = Environment.NewLine;

            try
            {
                LoadFile(inputPath.Text);
            }
            catch (ParsingException exc)
            {
                MessageBox.Show($"Import failed!{nl}{nl}{exc.Message}", "Import Failed");
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Internal error!{nl}{nl}{exc}", "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void conformToSkeleton_CheckedChanged(object sender, EventArgs e)
        {
            conformantGR2Path.Enabled = conformToOriginal.Checked;
            conformantGR2BrowseBtn.Enabled = conformToOriginal.Checked;
        }

        private void outputFileBrowserBtn_Click(object sender, EventArgs e)
        {
            if (outputFileDlg.ShowDialog(this) == DialogResult.OK)
            {
                outputPath.Text = outputFileDlg.FileName;
            }
        }

        private void conformantSkeletonBrowseBtn_Click(object sender, EventArgs e)
        {
            if (conformSkeletonFileDlg.ShowDialog(this) == DialogResult.OK)
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

        private void GR2BatchInputBrowseBtn_Click(object sender, EventArgs e)
        {
            if (gr2InputDirDlg.ShowDialog(this) == DialogResult.OK)
            {
                gr2BatchInputDir.Text = gr2InputDirDlg.SelectedPath;
            }
        }

        private void GR2BatchOutputBrowseBtn_Click(object sender, EventArgs e)
        {
            if (gr2OutputDirDlg.ShowDialog(this) == DialogResult.OK)
            {
                gr2BatchOutputDir.Text = gr2OutputDirDlg.SelectedPath;
            }
        }

        private void GR2ProgressUpdate(string status, long numerator, long denominator)
        {
            gr2BatchProgressLabel.Text = status;
            gr2BatchProgressBar.Value = denominator == 0 ? 0 : (int) (numerator * 100 / denominator);

            Application.DoEvents();
        }

        private static void GR2ConversionError(string inputPath, string outputPath, Exception exc)
        {
            string nl = Environment.NewLine;

            string pathText = $"Input File: {inputPath}{nl}Output File: {outputPath}{nl}";
            switch (exc)
            {
                case ExportException _:
                case ParsingException _:
                {
                    MessageBox.Show($"Export failed!{nl}{nl}{pathText}{nl}{exc.Message}", "Export Failed");
                    break;
                }
                default:
                {
                    MessageBox.Show($"Internal error!{nl}{nl}{pathText}{nl}{exc}", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                }
            }
        }

        private void GR2BatchConvertBtn_Click(object sender, EventArgs e)
        {
            gr2BatchConvertBtn.Enabled = false;
            var exporter = new Exporter();
            UpdateCommonExporterSettings(exporter.Options);

            exporter.Options.InputFormat = gr2BatchInputFormat.SelectedIndex == 0 ? ExportFormat.GR2 : ExportFormat.DAE;

            exporter.Options.OutputFormat = gr2BatchOutputFormat.SelectedIndex == 0 ? ExportFormat.GR2 : ExportFormat.DAE;

            var batchConverter = new GR2Utils
            {
                ProgressUpdate = GR2ProgressUpdate,
                ConversionError = GR2ConversionError
            };

            batchConverter.ConvertModels(gr2BatchInputDir.Text, gr2BatchOutputDir.Text, exporter);
            gr2BatchConvertBtn.Enabled = true;

            MessageBox.Show("Batch export completed.");
        }
    }
}
