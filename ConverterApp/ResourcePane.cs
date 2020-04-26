using System;
using System.Windows.Forms;
using LSLib.LS;
using LSLib.LS.Enums;

namespace ConverterApp
{
    public partial class ResourcePane : UserControl
    {
        private readonly MainForm _form;
        private Resource _resource;

        public ResourcePane(MainForm form)
        {
            InitializeComponent();

            _form = form;

            resourceInputFormatCb.SelectedIndex = 2;
            resourceOutputFormatCb.SelectedIndex = 0;

            resourceInputPath.DataBindings.Add("Text", form, "Settings.Resources.InputPath", true, DataSourceUpdateMode.OnPropertyChanged);
            resourceOutputPath.DataBindings.Add("Text", form, "Settings.Resources.OutputPath", true, DataSourceUpdateMode.OnPropertyChanged);
            resourceInputDir.DataBindings.Add("Text", form, "Settings.Resources.BatchInputPath", true, DataSourceUpdateMode.OnPropertyChanged);
            resourceOutputDir.DataBindings.Add("Text", form, "Settings.Resources.BatchOutputPath", true, DataSourceUpdateMode.OnPropertyChanged);

            resourceInputFormatCb.DataBindings.Add("SelectedIndex", form, "Settings.Resources.BatchInputFormat", true, DataSourceUpdateMode.OnPropertyChanged);
            resourceOutputFormatCb.DataBindings.Add("SelectedIndex", form, "Settings.Resources.BatchOutputFormat", true, DataSourceUpdateMode.OnPropertyChanged);
        }

        private void resourceConvertBtn_Click(object sender, EventArgs e)
        {
            try
            {
                _resource = ResourceUtils.LoadResource(resourceInputPath.Text);
                ResourceFormat format = ResourceUtils.ExtensionToResourceFormat(resourceOutputPath.Text);
                FileVersion outputVersion = _form.GetGame().IsDOS2() ? FileVersion.VerExtendedNodes : FileVersion.VerChunkedCompress;
                ResourceUtils.SaveResource(_resource, resourceOutputPath.Text, format, outputVersion);

                MessageBox.Show("Resource saved successfully.");
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Internal error!{Environment.NewLine}{Environment.NewLine}{exc}", "Conversion Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void resourceInputBrowseBtn_Click(object sender, EventArgs e)
        {
            if (resourceInputFileDlg.ShowDialog(this) == DialogResult.OK)
            {
                resourceInputPath.Text = resourceInputFileDlg.FileName;
            }
        }

        private void resourceOutputBrowseBtn_Click(object sender, EventArgs e)
        {
            if (resourceOutputFileDlg.ShowDialog(this) == DialogResult.OK)
            {
                resourceOutputPath.Text = resourceOutputFileDlg.FileName;
            }
        }

        private void resourceInputPathBrowseBtn_Click(object sender, EventArgs e)
        {
            if (resourceInputPathDlg.ShowDialog(this) == DialogResult.OK)
            {
                resourceInputDir.Text = resourceInputPathDlg.SelectedPath;
            }
        }

        private void resourceOutputPathBrowseBtn_Click(object sender, EventArgs e)
        {
            if (resourceOutputPathDlg.ShowDialog(this) == DialogResult.OK)
            {
                resourceOutputDir.Text = resourceOutputPathDlg.SelectedPath;
            }
        }

        public void ResourceProgressUpdate(string status, long numerator, long denominator)
        {
            resourceProgressLabel.Text = status;
            resourceConversionProgress.Value = denominator == 0 ? 0 : (int) (numerator * 100 / denominator);

            Application.DoEvents();
        }

        private void resourceBulkConvertBtn_Click(object sender, EventArgs e)
        {
            var inputFormat = ResourceFormat.LSX;
            switch (resourceInputFormatCb.SelectedIndex)
            {
                case 0:
                {
                    inputFormat = ResourceFormat.LSX;
                    break;
                }
                case 1:
                {
                    inputFormat = ResourceFormat.LSB;
                    break;
                }
                case 2:
                {
                    inputFormat = ResourceFormat.LSF;
                    break;
                }
                case 3:
                {
                    inputFormat = ResourceFormat.LSJ;
                    break;
                }
            }

            var outputFormat = ResourceFormat.LSF;
            FileVersion outputVersion = 0x0;

            switch (resourceOutputFormatCb.SelectedIndex)
            {
                case 0:
                {
                    outputFormat = ResourceFormat.LSX;
                    break;
                }
                case 1:
                {
                    outputFormat = ResourceFormat.LSB;
                    break;
                }
                case 2:
                {
                    outputFormat = ResourceFormat.LSF;
                    outputVersion = _form.GetGame().IsDOS2() ? FileVersion.VerExtendedNodes : FileVersion.VerChunkedCompress;
                    break;
                }
                case 3:
                {
                    outputFormat = ResourceFormat.LSJ;
                    break;
                }
            }

            try
            {
                resourceConvertBtn.Enabled = false;
                var utils = new ResourceUtils();
                utils.progressUpdate += ResourceProgressUpdate;
                utils.ConvertResources(resourceInputDir.Text, resourceOutputDir.Text, inputFormat, outputFormat, outputVersion);

                MessageBox.Show("Resources converted successfully.");
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Internal error!{Environment.NewLine}{Environment.NewLine}{exc}", "Conversion Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                resourceProgressLabel.Text = "";
                resourceConversionProgress.Value = 0;
                resourceConvertBtn.Enabled = true;
            }
        }
    }
}
