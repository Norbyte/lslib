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
using System.Diagnostics;

namespace ConverterApp
{
    public partial class PackagePane : UserControl
    {
        private Stopwatch DisplayTimer;

        public PackagePane()
        {
            InitializeComponent();
            packageVersion.SelectedIndex = 0;
            compressionMethod.SelectedIndex = 4;
        }

        private void PackageProgressUpdate(string status, long numerator, long denominator, FileInfo file)
        {
            if (file != null)
            {
                // Throttle the progress displays to 10 updates per second to prevent UI
                // updates from slowing down the compression/decompression process
                if (DisplayTimer == null)
                {
                    DisplayTimer = new Stopwatch();
                    DisplayTimer.Start();
                }
                else if (DisplayTimer.ElapsedMilliseconds < 100)
                {
                    return;
                }
                else
                {
                    DisplayTimer.Restart();
                }
            }
            
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

        private void packageBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = packageFileDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                packagePath.Text = packageFileDlg.FileName;
                // Savegames (.lsv files) are saved using ZLib
                if (System.IO.Path.GetExtension(packageFileDlg.FileName) == ".lsv")
                {
                    compressionMethod.SelectedIndex = 2;
                }
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
            DisplayTimer = null;

            try
            {
                var packager = new Packager();
                packager.progressUpdate += this.PackageProgressUpdate;
                packager.UncompressPackage(packagePath.Text, extractionPath.Text);
                MessageBox.Show("Package extracted successfully.");
            }
            catch (NotAPackageException)
            {
                MessageBox.Show("The specified package (" + packagePath.Text + ") is not an Original Sin package or savegame archive.", "Extraction Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            DisplayTimer = null;

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

        private void packagePath_TextChanged(object sender, EventArgs e)
        {
            // Savegames (.lsv files) are saved using ZLib
            if (System.IO.Path.GetExtension(packagePath.Text) == ".lsv")
            {
                compressionMethod.SelectedIndex = 2;
            }
        }
    }
}
