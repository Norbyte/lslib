using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using LSLib.LS;
using LSLib.LS.Enums;

namespace ConverterApp
{
    public partial class PackagePane : UserControl
    {
        private Stopwatch _displayTimer;

        private Action SaveSettings { get; set; }

        public PackagePane(ISettingsDataSource settingsDataSource)
        {
            InitializeComponent();

            SaveSettings = settingsDataSource.SaveSettings;

            packageVersion.SelectedIndex = 0;
            compressionMethod.SelectedIndex = 3;

            extractPackagePath.DataBindings.Add("Text", settingsDataSource, "Settings.PAK.ExtractInputPath");
            extractionPath.DataBindings.Add("Text", settingsDataSource, "Settings.PAK.ExtractOutputPath");
            createSrcPath.DataBindings.Add("Text", settingsDataSource, "Settings.PAK.CreateInputPath");
            createPackagePath.DataBindings.Add("Text", settingsDataSource, "Settings.PAK.CreateOutputPath");

            packageVersion.DataBindings.Add("SelectedIndex", settingsDataSource, "Settings.PAK.CreatePackageVersion", true, DataSourceUpdateMode.OnPropertyChanged);
            compressionMethod.DataBindings.Add("SelectedIndex", settingsDataSource, "Settings.PAK.CreatePackageCompression", true, DataSourceUpdateMode.OnPropertyChanged);
        }

        private void PackageProgressUpdate(string status, long numerator, long denominator, AbstractFileInfo file)
        {
            if (file != null)
            {
                // Throttle the progress displays to 10 updates per second to prevent UI
                // updates from slowing down the compression/decompression process
                if (_displayTimer == null)
                {
                    _displayTimer = new Stopwatch();
                    _displayTimer.Start();
                }
                else if (_displayTimer.ElapsedMilliseconds < 100)
                {
                    return;
                }
                else
                {
                    _displayTimer.Restart();
                }
            }

            packageProgressLabel.Text = status;
            if (denominator == 0)
            {
                packageProgress.Value = 0;
            }
            else
            {
                packageProgress.Value = (int) (numerator * 100 / denominator);
            }

            Application.DoEvents();
        }

        private void extractPackageBtn_Click(object sender, EventArgs e)
        {
            extractPackageBtn.Enabled = false;
            _displayTimer = null;

            try
            {
                var packager = new Packager();
                packager.ProgressUpdate += PackageProgressUpdate;
                packager.UncompressPackage(extractPackagePath.Text, extractionPath.Text);
                MessageBox.Show("Package extracted successfully.");

                SaveSettings?.Invoke();
            }
            catch (NotAPackageException)
            {
                MessageBox.Show($"The specified package ({extractPackagePath.Text}) is not an Original Sin package or savegame archive.", "Extraction Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Internal error!{Environment.NewLine}{Environment.NewLine}{exc}", "Extraction Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            _displayTimer = null;

            try
            {
                PackageVersion version = Package.CurrentVersion;
                switch (packageVersion.SelectedIndex)
                {
                    case 0:
                    case 1:
                    {
                        version = PackageVersion.V13;
                        break;
                    }
                    case 2:
                    {
                        version = PackageVersion.V10;
                        break;
                    }
                    case 3:
                    {
                        version = PackageVersion.V9;
                        break;
                    }
                    case 4:
                    {
                        version = PackageVersion.V7;
                        break;
                    }
                }

                var compression = CompressionMethod.None;
                var fastCompression = true;
                switch (compressionMethod.SelectedIndex)
                {
                    case 1:
                    {
                        compression = CompressionMethod.Zlib;
                        break;
                    }
                    case 2:
                    {
                        compression = CompressionMethod.Zlib;
                        fastCompression = false;
                        break;
                    }
                    case 3:
                    {
                        compression = CompressionMethod.LZ4;
                        break;
                    }
                    case 4:
                    {
                        compression = CompressionMethod.LZ4;
                        fastCompression = false;
                        break;
                    }
                }

                // Fallback to Zlib, if the package version doesn't support LZ4
                if (compression == CompressionMethod.LZ4 && version <= PackageVersion.V9)
                {
                    compression = CompressionMethod.Zlib;
                }

                var packager = new Packager();
                packager.ProgressUpdate += PackageProgressUpdate;
                packager.CreatePackage(createPackagePath.Text, createSrcPath.Text, version, compression, fastCompression);

                MessageBox.Show("Package created successfully.");

                SaveSettings?.Invoke();
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Internal error!{Environment.NewLine}{Environment.NewLine}{exc}", "Package Build Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (Path.GetExtension(createPackagePath.Text) == ".lsv")
            {
                compressionMethod.SelectedIndex = 2;

                SaveSettings?.Invoke();
            }
        }

        private void extractPackageBrowseBtn_Click(object sender, EventArgs e)
        {
            if (extractPackageFileDlg.ShowDialog(this) == DialogResult.OK)
            {
                extractPackagePath.Text = extractPackageFileDlg.FileName;

                SaveSettings?.Invoke();
            }
        }

        private void extractPathBrowseBtn_Click(object sender, EventArgs e)
        {
            DialogResult result = extractPathDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                extractionPath.Text = extractPathDlg.SelectedPath;

                SaveSettings?.Invoke();
            }
        }

        private void createSrcPathBrowseBtn_Click(object sender, EventArgs e)
        {
            DialogResult result = createPackagePathDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                createSrcPath.Text = createPackagePathDlg.SelectedPath;

                SaveSettings?.Invoke();
            }
        }

        private void createPackagePathBrowseBtn_Click(object sender, EventArgs e)
        {
            if (createPackageFileDlg.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            createPackagePath.Text = createPackageFileDlg.FileName;
            // Savegames (.lsv files) are saved using ZLib
            if (Path.GetExtension(createPackageFileDlg.FileName) == ".lsv")
            {
                compressionMethod.SelectedIndex = 2;
            }

            SaveSettings?.Invoke();
        }

        public void SetGame(Game game)
        {
            switch (game)
            {
                case Game.DivinityOriginalSin:
                    packageVersion.SelectedIndex = 3;
                    break;

                case Game.DivinityOriginalSinEE:
                    packageVersion.SelectedIndex = 2;
                    break;

                case Game.DivinityOriginalSin2:
                    packageVersion.SelectedIndex = 1;
                    break;
                case Game.DivinityOriginalSin2DE:
                    packageVersion.SelectedIndex = 0;
                    break;
            }
        }
    }
}
