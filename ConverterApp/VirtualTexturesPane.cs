using Alphaleonis.Win32.Filesystem;
using LSLib.LS;
using LSLib.VirtualTextures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConverterApp
{
    public partial class VirtualTexturesPane : UserControl
    {
        public VirtualTexturesPane()
        {
            InitializeComponent();
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
                for (var pfIdx = 0; pfIdx < tileSet.PageFileInfos.Count; pfIdx++)
                {
                    var fileInfo = tileSet.PageFileInfos[pfIdx];
                    actionProgressLabel.Text = fileInfo.Name;
                    actionProgress.Value = pfIdx * 100 / tileSet.PageFileInfos.Count;
                    Application.DoEvents();

                    for (var layer = 0; layer < tileSet.TileSetLayers.Length; layer++)
                    {
                        var tex = tileSet.ExtractPageFileTexture(pfIdx, 0, layer);
                        if (tex != null)
                        {
                            var outputPath = destinationPath.Text + Path.DirectorySeparator + Path.GetFileNameWithoutExtension(fileInfo.Name) + $"_{layer}.dds";
                            tex.SaveDDS(outputPath);
                        }
                    }

                    tileSet.ReleasePageFiles();
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
    }
}
