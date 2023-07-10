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

        private ExporterOptions lastExporterSettings;

        public GR2Pane(MainForm form)
        {
            _form = form;
            InitializeComponent();

            gr2BatchInputFormat.SelectedIndex = 0;
            gr2BatchOutputFormat.SelectedIndex = 1;

            inputPath.DataBindings.Add("Text", _form, "Settings.GR2.InputPath", true, DataSourceUpdateMode.OnPropertyChanged);
            outputPath.DataBindings.Add("Text", _form, "Settings.GR2.OutputPath", true, DataSourceUpdateMode.OnPropertyChanged);

            conformantGR2Path.DataBindings.Add("Text", _form, "Settings.GR2.ConformPath", true, DataSourceUpdateMode.OnPropertyChanged);

            gr2BatchInputDir.DataBindings.Add("Text", _form, "Settings.GR2.BatchInputPath", true, DataSourceUpdateMode.OnPropertyChanged);
            gr2BatchOutputDir.DataBindings.Add("Text", _form, "Settings.GR2.BatchOutputPath", true, DataSourceUpdateMode.OnPropertyChanged);

            gr2BatchInputFormat.DataBindings.Add("SelectedIndex", _form, "Settings.GR2.BatchInputFormat", true, DataSourceUpdateMode.OnPropertyChanged);
            gr2BatchOutputFormat.DataBindings.Add("SelectedIndex", _form, "Settings.GR2.BatchOutputFormat", true, DataSourceUpdateMode.OnPropertyChanged);

            if (File.Exists(inputPath.Text))
            {
                loadInputBtn_Click(loadInputBtn, EventArgs.Empty);
            }
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
                conformCopySkeletons.Enabled = false;
                buildDummySkeleton.Enabled = true;
                buildDummySkeleton.Checked = true;
            }

            bool hasUndeterminedModelTypes = false;
            DivinityModelFlag accumulatedModelFlags = 0;
            foreach (var mesh in _root.Meshes ?? Enumerable.Empty<Mesh>())
            {
                if (mesh.ExtendedData?.UserMeshProperties == null
                    || mesh.ExtendedData.UserMeshProperties.MeshFlags == 0)
                {
                    hasUndeterminedModelTypes = true;
                }
                else if (mesh.ExtendedData?.UserMeshProperties == null)
                {
                    accumulatedModelFlags |= mesh.ExtendedData.UserMeshProperties.MeshFlags;
                }
            }

            // If the type of all models are known, either via LSMv1 ExtendedData
            // or via Collada <extra> properties, there is nothing to override.
            meshRigid.Enabled = hasUndeterminedModelTypes;
            meshCloth.Enabled = hasUndeterminedModelTypes;
            meshProxy.Enabled = hasUndeterminedModelTypes;

            meshRigid.Checked = accumulatedModelFlags.IsRigid();
            meshCloth.Checked = accumulatedModelFlags.IsCloth();
            meshProxy.Checked = accumulatedModelFlags.IsMeshProxy();
            
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

            foreach (ListViewItem item in exportableObjects.Items)
            {
                if(!item.Checked)
                {
                    var name = item.SubItems[0].Text;
                    var itemType = item.SubItems[1].Text;

                    if (itemType == "Model")
                    {
                        settings.DisabledModels.Add(name);
                    }
                    else if (itemType == "Skeleton")
                    {
                        settings.DisabledSkeletons.Add(name);
                    }
                    else if (itemType == "Animation")
                    {
                        settings.DisabledAnimations.Add(name);
                    }
                }
            }

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
            
            settings.FlipUVs = flipUVs.Checked;
            settings.BuildDummySkeleton = buildDummySkeleton.Checked;
            settings.DeduplicateUVs = filterUVs.Checked;
            settings.ApplyBasisTransforms = applyBasisTransforms.Checked;
            settings.FlipMesh = flipMeshes.Checked;
            settings.FlipSkeleton = flipSkeletons.Checked;

            settings.LoadGameSettings(game);

            settings.ModelType = 0;
            if (meshRigid.Checked)
            {
                settings.ModelType |= DivinityModelFlag.Rigid;
            }

            if (meshCloth.Checked)
            {
                settings.ModelType |= DivinityModelFlag.Cloth;
            }

            if (meshProxy.Checked)
            {
                settings.ModelType |= DivinityModelFlag.MeshProxy | DivinityModelFlag.HasProxyGeometry;
            }
            
            settings.ConformGR2Path = conformToOriginal.Checked && conformantGR2Path.Text.Length > 0 ? conformantGR2Path.Text : null;
            settings.ConformSkeletonsCopy = conformCopySkeletons.Checked;
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
            conformCopySkeletons.Enabled = conformToOriginal.Checked;
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
            lastExporterSettings = exporter.Options;
#if !DEBUG
            try
            {
#endif
                exporter.Export();

                MessageBox.Show("Export completed successfully.");
#if !DEBUG
            }
            catch (Exception exc)
            {
                GR2ConversionError(exporter.Options.InputPath, exporter.Options.OutputPath, exc);
            }
#endif
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
