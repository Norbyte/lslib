using LSLib.LS.Enums;
using LSLib.LS;
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
    public partial class LocalizationPane : UserControl
    {
        public LocalizationPane()
        {
            InitializeComponent();
        }

        private void resourceInputBrowseBtn_Click(object sender, EventArgs e)
        {
            if (locaInputFileDlg.ShowDialog(this) == DialogResult.OK)
            {
                locaInputPath.Text = locaInputFileDlg.FileName;
            }
        }

        private void locaOutputBrowseBtn_Click(object sender, EventArgs e)
        {
            if (locaOutputFileDlg.ShowDialog(this) == DialogResult.OK)
            {
                locaOutputPath.Text = locaOutputFileDlg.FileName;
            }
        }

        private void locaConvertBtn_Click(object sender, EventArgs e)
        {
            try
            {
                var resource = LocaUtils.Load(locaInputPath.Text);
                var format = LocaUtils.ExtensionToFileFormat(locaOutputPath.Text);
                LocaUtils.Save(resource, locaOutputPath.Text, format);

                MessageBox.Show("Localization file saved successfully.");
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Internal error!{Environment.NewLine}{Environment.NewLine}{exc}", "Conversion Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }
    }
}
