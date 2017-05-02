using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LSLib.LS;
using LSLib.LS.LSF;

namespace ConverterApp
{
    public partial class ResourcePane : UserControl
    {
        private Resource Resource;
        private MainForm Form;

        public ResourcePane(MainForm form)
        {
            Form = form;
            InitializeComponent();
            resourceInputFormatCb.SelectedIndex = 2;
            resourceOutputFormatCb.SelectedIndex = 0;
        }

        private void resourceConvertBtn_Click(object sender, EventArgs e)
        {
            try
            {
                Resource = ResourceUtils.LoadResource(resourceInputPath.Text);
                var format = ResourceUtils.ExtensionToResourceFormat(resourceOutputPath.Text);
                int outputVersion = -1;
                if (Form.GetGame() == DivGame.DOS2)
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

                case 3:
                    inputFormat = ResourceFormat.LSJ;
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
                    if (Form.GetGame() == DivGame.DOS2)
                    {
                        outputVersion = (int)FileVersion.VerExtendedNodes;
                    }
                    else
                    {
                        outputVersion = (int)FileVersion.VerChunkedCompress;
                    }
                    break;

                case 3:
                    outputFormat = ResourceFormat.LSJ;
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
    }
}
