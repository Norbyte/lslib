using LSLib.VirtualTextures;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ConverterApp;

public partial class VirtualTexturesPane : UserControl
{
    public VirtualTexturesPane(ISettingsDataSource settingsDataSource)
    {
        InitializeComponent();

        gtsPath.DataBindings.Add("Text", settingsDataSource, "Settings.VirtualTextures.GTSPath", true, DataSourceUpdateMode.OnPropertyChanged);
        destinationPath.DataBindings.Add("Text", settingsDataSource, "Settings.VirtualTextures.DestinationPath", true, DataSourceUpdateMode.OnPropertyChanged);
    }

    private void gtpBrowseBtn_Click(object sender, EventArgs e)
    {
        if (gtsFileDlg.ShowDialog(this) == DialogResult.OK)
        {
            gtsPath.Text = gtsFileDlg.FileName;
        }
    }

    private void destinationPathBrowseBtn_Click(object sender, EventArgs e)
    {
        DialogResult result = destinationPathDlg.ShowDialog(this);
        if (result == DialogResult.OK)
        {
            destinationPath.Text = destinationPathDlg.SelectedPath;
        }
    }

    private void extractTileSetBtn_Click(object sender, EventArgs e)
    {
        extractTileSetBtn.Enabled = false;
        try
        {
            var tileSet = new VirtualTileSet(gtsPath.Text);
            var textures = tileSet.FourCCMetadata.ExtractTextureMetadata();

            var texName = gTexNameInput.Text.Trim();
            if (texName.Length > 0)
            {
                textures = textures.Where(tex => tex.Name == texName).ToList();
                if (textures.Count == 0)
                {
                    MessageBox.Show($"GTex was not found in this tile set: {texName}", "Extraction Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            var i = 0;
            foreach (var texture in textures)
            {
                actionProgressLabel.Text = "GTex: " + texture.Name;
                actionProgress.Value = i++ * 100 / textures.Count;
                Application.DoEvents();

                for (var layer = 0; layer < tileSet.TileSetLayers.Length; layer++)
                {
                    BC5Image tex = null;
                    var level = 0;
                    do
                    {
                        tex = tileSet.ExtractTexture(level, layer, texture);
                        level++;
                    } while (tex == null && level < tileSet.TileSetLevels.Length);

                    if (tex != null)
                    {
                        var outputPath = Path.Join(destinationPath.Text, texture.Name + $"_{layer}.dds");
                        tex.SaveDDS(outputPath);
                    }
                }

                tileSet.ReleasePageFiles();
                GC.Collect();
            }

            MessageBox.Show("Textures extracted successfully.");
        }
        catch (Exception exc)
        {
            MessageBox.Show($"Internal error!{Environment.NewLine}{Environment.NewLine}{exc}", "Extraction Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            actionProgressLabel.Text = "";
            actionProgress.Value = 0;
            extractTileSetBtn.Enabled = true;
        }
    }

    private void tileSetConfigBrowseBtn_Click(object sender, EventArgs e)
    {
        if (tileSetConfigDlg.ShowDialog(this) == DialogResult.OK)
        {
            tileSetConfigPath.Text = tileSetConfigDlg.FileName;
        }
    }

    private void modRootPathBrowseBtn_Click(object sender, EventArgs e)
    {
        DialogResult result = modRootPathDlg.ShowDialog(this);
        if (result == DialogResult.OK)
        {
            modRootPath.Text = modRootPathDlg.SelectedPath;
        }
    }

    private void tileSetBuildBtn_Click(object sender, EventArgs ev)
    {
        try
        {
            var descriptor = new TileSetDescriptor();
            descriptor.RootPath = modRootPath.Text;
            descriptor.Load(tileSetConfigPath.Text);

            var builder = new TileSetBuilder(descriptor.Config);
            builder.OnStepStarted += (step) =>
            {
                actionProgressLabel.Text = step;
                Application.DoEvents();
            };
            builder.OnStepProgress += (numerator, denumerator) =>
            {
                actionProgress.Maximum = denumerator;
                actionProgress.Value = numerator;
                Application.DoEvents();
            };

            builder.OnStepStarted("Adding textures");
            foreach (var texture in descriptor.Textures)
            {
                var layerPaths = texture.Layers.Select(name => name != null ? Path.Combine(descriptor.SourceTexturePath, name) : null).ToList();
                builder.AddTexture(texture.Name, layerPaths);
            }

            builder.Build(descriptor.VirtualTexturePath);

            MessageBox.Show("Tile set build completed.");
        }
        catch (InvalidDataException e)
        {
            MessageBox.Show($"{e.Message}", "Tile Set Build Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (FileNotFoundException e)
        {
            MessageBox.Show($"{e.Message}", "Tile Set Build Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception e)
        {
            MessageBox.Show($"Internal error!{Environment.NewLine}{Environment.NewLine}{e}", "Tile Set Build Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        actionProgressLabel.Text = "";
        actionProgress.Value = 0;
    }
}
