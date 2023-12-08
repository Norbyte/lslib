using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using LSLib.LS.Enums;

namespace ConverterApp
{
    public partial class DebugPane : UserControl
    {
        public Game Game;

        public DebugPane(ISettingsDataSource settingsDataSource)
        {
            InitializeComponent();

            saveFilePath.DataBindings.Add("Text", settingsDataSource, "Settings.Debugging.SavePath", true, DataSourceUpdateMode.OnPropertyChanged);
        }

        private DebugDumperTask CreateDumperFromSettings()
        {
            string dumpPath = Path.Join(Path.GetDirectoryName(saveFilePath.Text), Path.GetFileNameWithoutExtension(saveFilePath.Text));

            var dumper = new DebugDumperTask
            {
                GameVersion = Game,
                ExtractionPath = Path.Join(dumpPath, "SaveArchive"),
                DataDumpPath = Path.Join(dumpPath, "Dumps"),

                SaveFilePath = saveFilePath.Text,

                ExtractAll = extractPackage.Checked,
                ConvertToLsx = convertLsf.Checked,
                DumpModList = exportModList.Checked,

                DumpGlobalVars = dumpGlobalVars.Checked,
                DumpCharacterVars = dumpCharacterVars.Checked,
                DumpItemVars = dumpItemVars.Checked,
                IncludeDeletedVars = includeDeleted.Checked,
                IncludeLocalScopes = includeLocalScopes.Checked,

                DumpStoryDatabases = dumpDatabases.Checked,
                IncludeUnnamedDatabases = includeUnnamedDbs.Checked
            };

            return dumper;
        }
        
        private void dumpVariablesBtn_Click(object sender, EventArgs e)
        {
            string extension = Path.GetExtension(saveFilePath.Text)?.ToLower();

            if (extension != ".lsv")
            {
                MessageBox.Show($"Savegame loading only supported for .LSV savegame files.", "Savegame load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            dumpVariablesBtn.Enabled = false;

            var dumper = CreateDumperFromSettings();

            var worker = new BackgroundWorker();
            dumper.ReportProgress += (percent, statusText) => worker.ReportProgress(percent, statusText);
            worker.WorkerReportsProgress = true;
            worker.DoWork += (pSender, pEvent) =>
            {
#if !DEBUG
                try
                {
#endif
                    dumper.Run();

#if !DEBUG
                }
                catch (Exception exc)
                {
                    string nl = Environment.NewLine;
                    MessageBox.Show($"Internal error!{nl}{nl}{exc}", "Dump Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
#endif
            };
            worker.ProgressChanged += (pSender, pEvent) => {
                dumpProgressBar.Value = pEvent.ProgressPercentage;
                lblProgressStatus.Text = (string)pEvent.UserState;
            };
            worker.RunWorkerCompleted += (pSender, pEvent) =>
            {
                dumpProgressBar.Value = 0;
                lblProgressStatus.Text = "";
                dumpVariablesBtn.Enabled = true;
            };
            worker.RunWorkerAsync();
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
