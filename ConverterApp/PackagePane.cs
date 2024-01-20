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

        public PackagePane(ISettingsDataSource settingsDataSource)
        {
            InitializeComponent();

            packageVersion.SelectedIndex = 0;
            compressionMethod.SelectedIndex = 3;

            extractPackagePath.DataBindings.Add("Text", settingsDataSource, "Settings.PAK.ExtractInputPath", true, DataSourceUpdateMode.OnPropertyChanged);
            extractionPath.DataBindings.Add("Text", settingsDataSource, "Settings.PAK.ExtractOutputPath", true, DataSourceUpdateMode.OnPropertyChanged);
            createSrcPath.DataBindings.Add("Text", settingsDataSource, "Settings.PAK.CreateInputPath", true, DataSourceUpdateMode.OnPropertyChanged);
            createPackagePath.DataBindings.Add("Text", settingsDataSource, "Settings.PAK.CreateOutputPath", true, DataSourceUpdateMode.OnPropertyChanged);

            packageVersion.DataBindings.Add("SelectedIndex", settingsDataSource, "Settings.PAK.CreatePackageVersion", true, DataSourceUpdateMode.OnPropertyChanged);
            compressionMethod.DataBindings.Add("SelectedIndex", settingsDataSource, "Settings.PAK.CreatePackageCompression", true, DataSourceUpdateMode.OnPropertyChanged);

#if DEBUG
            allowMemoryMapping.Visible = true;
            preloadIntoCache.Visible = true;
#endif
        }

        private void PackageProgressUpdate(string status, long numerator, long denominator)
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
            }
            catch (NotAPackageException)
            {
                if (ModPathVisitor.archivePartRe.IsMatch(Path.GetFileName(extractPackagePath.Text)))
                {
                    MessageBox.Show($"The specified file is part of a multi-part package; only the first part needs to be extracted.", "Extraction Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show($"The specified file ({extractPackagePath.Text}) is not an PAK package or savegame archive.", "Extraction Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
#if !DEBUG
            catch (Exception exc)
            {
                MessageBox.Show($"Internal error!{Environment.NewLine}{Environment.NewLine}{exc}", "Extraction Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
#endif
            finally
            {
                packageProgressLabel.Text = "";
                packageProgress.Value = 0;
                extractPackageBtn.Enabled = true;
            }
        }

        private PackageVersion SelectedPackageVersion()
        {
            switch (packageVersion.SelectedIndex)
            {
                case 0: return PackageVersion.V18;
                case 1: return PackageVersion.V13;
                case 2: return PackageVersion.V10;
                case 3: return PackageVersion.V9;
                case 4: return PackageVersion.V7;
                default: throw new ArgumentException();
            }
        }

        private void createPackageBtn_Click(object sender, EventArgs e)
        {
            createPackageBtn.Enabled = false;
            _displayTimer = null;

            try
            {
                var build = new PackageBuildData();
                build.Version = SelectedPackageVersion();
                
                switch (compressionMethod.SelectedIndex)
                {
                    case 1:
                    {
                        build.Compression = CompressionMethod.Zlib;
                        build.CompressionLevel = LSCompressionLevel.Fast;
                        break;
                    }
                    case 2:
                    {
                        build.Compression = CompressionMethod.Zlib;
                        break;
                    }
                    case 3:
                    {
                        build.Compression = CompressionMethod.LZ4;
                        build.CompressionLevel = LSCompressionLevel.Fast;
                        break;
                    }
                    case 4:
                    {
                        build.Compression = CompressionMethod.LZ4;
                        break;
                    }
                    case 5:
                    {
                        build.Compression = CompressionMethod.Zstd;
                        build.CompressionLevel = LSCompressionLevel.Fast;
                        break;
                    }
                    case 6:
                    {
                        build.Compression = CompressionMethod.Zstd;
                        build.CompressionLevel = LSCompressionLevel.Default;
                        break;
                    }
                    case 7:
                    {
                        build.Compression = CompressionMethod.Zstd;
                        build.CompressionLevel = LSCompressionLevel.Max;
                        break;
                    }
                }

                // Fallback to Zlib, if the package version doesn't support LZ4
                if (build.Compression == CompressionMethod.LZ4 && build.Version <= PackageVersion.V9)
                {
                    build.Compression = CompressionMethod.Zlib;
                }

                if (solid.Checked)
                {
                    build.Flags |= PackageFlags.Solid;
                }

                if (allowMemoryMapping.Checked)
                {
                    build.Flags |= PackageFlags.AllowMemoryMapping;
                }

                if (preloadIntoCache.Checked)
                {
                    build.Flags |= PackageFlags.Preload;
                }

                build.Priority = (byte)packagePriority.Value;

                var packager = new Packager();
                packager.ProgressUpdate += PackageProgressUpdate;
                packager.CreatePackage(createPackagePath.Text, createSrcPath.Text, build).Wait();

                MessageBox.Show("Package created successfully.");
            }
#if !DEBUG
            catch (Exception exc)
            {
                MessageBox.Show($"Internal error!{Environment.NewLine}{Environment.NewLine}{exc}", "Package Build Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
#endif
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
            }
        }

        private void extractPackageBrowseBtn_Click(object sender, EventArgs e)
        {
            if (extractPackageFileDlg.ShowDialog(this) == DialogResult.OK)
            {
                extractPackagePath.Text = extractPackageFileDlg.FileName;
            }
        }

        private void extractPathBrowseBtn_Click(object sender, EventArgs e)
        {
            DialogResult result = extractPathDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                extractionPath.Text = extractPathDlg.SelectedPath;
            }
        }

        private void createSrcPathBrowseBtn_Click(object sender, EventArgs e)
        {
            DialogResult result = createPackagePathDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                createSrcPath.Text = createPackagePathDlg.SelectedPath;
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
        }

        public void SetGame(Game game)
        {
            switch (game.PAKVersion())
            {
                case PackageVersion.V7:
                    packageVersion.SelectedIndex = 4;
                    break;

                case PackageVersion.V9:
                    packageVersion.SelectedIndex = 3;
                    break;

                case PackageVersion.V10:
                    packageVersion.SelectedIndex = 2;
                    break;

                case PackageVersion.V13:
                    packageVersion.SelectedIndex = 1;
                    break;

                case PackageVersion.V18:
                    packageVersion.SelectedIndex = 0;
                    break;
            }
        }
    }
}
