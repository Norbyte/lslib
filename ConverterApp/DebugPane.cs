using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LSLib.LS.Story;
using LSLib.LS;
using System.IO;

namespace ConverterApp
{
    public partial class DebugPane : UserControl
    {
        private Resource SaveRoot;
        private Story SaveStory;

        public DebugPane(ISettingsDataSource settingsDataSource)
        {
            InitializeComponent();

            saveFilePath.DataBindings.Add("Text", settingsDataSource, "Settings.Debugging.SavePath");
        }

        private void loadSaveBtn_Click(object sender, EventArgs e)
        {
            dumpVariablesBtn.Enabled = false;

            string extension = Path.GetExtension(saveFilePath.Text)?.ToLower();

            if (extension != ".lsv")
            {
                MessageBox.Show($"Savegame loading only supported for .LSV savegame files.", "Savegame load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // TODO - move save utilities to shared NS
            SaveRoot = OsirisPane.LoadResourceFromSave(saveFilePath.Text);
            if (SaveRoot == null) return;
            
            LSLib.LS.Node storyNode = SaveRoot.Regions["Story"].Children["Story"][0];
            var storyStream = new MemoryStream(storyNode.Attributes["Story"].Value as byte[]);
            var reader = new StoryReader();
            SaveStory = reader.Read(storyStream);

            dumpVariablesBtn.Enabled = true;
        }

        private void dumpVariablesBtn_Click(object sender, EventArgs e)
        {
            string dumpPath = Path.GetDirectoryName(saveFilePath.Text) + "\\" + Path.GetFileNameWithoutExtension(saveFilePath.Text) + ".Variables.log";

            using (var outputFile = new FileStream(dumpPath, FileMode.Create, FileAccess.Write))
            {
                var varDumper = new VariableDumper(outputFile);
                varDumper.IncludeDeletedVars = includeDeleted.Checked;
                varDumper.IncludeLocalScopes = includeLocalScopes.Checked;
                varDumper.Load(SaveRoot);

                if (dumpGlobalVars.Checked)
                {
                    varDumper.DumpGlobals();
                }

                if (dumpCharacterVars.Checked)
                {
                    varDumper.DumpCharacters();
                }

                if (dumpItemVars.Checked)
                {
                    varDumper.DumpItems();
                }

                if (dumpDatabases.Checked)
                {
                    var dbDumper = new DatabaseDumper(outputFile);
                    dbDumper.DumpUnnamedDbs = includeUnnamedDbs.Checked;
                    dbDumper.DumpAll(SaveStory);
                }
            }

            MessageBox.Show($"Variables dumped to {dumpPath}.");
        }

        private void saveFileBrowseBtn_Click(object sender, EventArgs e)
        {
            if (savePathDlg.ShowDialog(this) == DialogResult.OK)
            {
                saveFilePath.Text = savePathDlg.FileName;
            }
        }
    }
}
