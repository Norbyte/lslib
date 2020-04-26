using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LSLib.LS;
using LSLib.LS.Enums;
using LSLib.LS.Story;
using Node = LSLib.LS.Story.Node;

namespace ConverterApp
{
    public partial class OsirisPane : UserControl
    {
        private Story _story;

        public OsirisPane(ISettingsDataSource settingsDataSource)
        {
            InitializeComponent();

            storyFilePath.DataBindings.Add("Text", settingsDataSource, "Settings.Story.InputPath", true, DataSourceUpdateMode.OnPropertyChanged);
            goalPath.DataBindings.Add("Text", settingsDataSource, "Settings.Story.OutputPath", true, DataSourceUpdateMode.OnPropertyChanged);
        }

        private void storyFileBrowseBtn_Click(object sender, EventArgs e)
        {
            if (storyPathDlg.ShowDialog(this) == DialogResult.OK)
            {
                storyFilePath.Text = storyPathDlg.FileName;
            }
        }

        private void goalPathBrowseBtn_Click(object sender, EventArgs e)
        {
            if (goalPathDlg.ShowDialog(this) == DialogResult.OK)
            {
                goalPath.Text = goalPathDlg.SelectedPath;
            }
        }

        private void LoadStory(Stream s)
        {
            var reader = new StoryReader();
            _story = reader.Read(s);

            databaseSelectorCb.Items.Clear();
            foreach (KeyValuePair<uint, Database> database in _story.Databases)
            {
                var name = "(Unnamed)";
                Node owner = database.Value.OwnerNode;
                if (owner != null)
                {
                    name = owner.Name.Length > 0 ? $"{owner.Name}({owner.NumParams})" : $"<{owner.TypeName()}>";
                }

                name += $" #{database.Key} ({database.Value.Facts.Count} rows)";

                databaseSelectorCb.Items.Add(name);

                if (databaseSelectorCb.Items.Count > 0)
                {
                    databaseSelectorCb.SelectedIndex = 0;
                }
            }
        }

        public static Resource LoadResourceFromSave(string path)
        {
            var packageReader = new PackageReader(path);
            Package package = packageReader.Read();

            AbstractFileInfo abstractFileInfo = package.Files.FirstOrDefault(p => p.Name == "globals.lsf");
            if (abstractFileInfo == null)
            {
                MessageBox.Show("The specified package is not a valid savegame (globals.lsf not found)", "Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            Resource resource;
            Stream rsrcStream = abstractFileInfo.MakeStream();
            try
            {
                using (var rsrcReader = new LSFReader(rsrcStream))
                {
                    resource = rsrcReader.Read();
                }
            }
            finally
            {
                abstractFileInfo.ReleaseStream();
            }

            return resource;
        }

        private void loadStoryBtn_Click(object sender, EventArgs e)
        {
            string extension = Path.GetExtension(storyFilePath.Text)?.ToLower();

            switch (extension)
            {
                case ".lsv":
                {
                    var resource = LoadResourceFromSave(storyFilePath.Text);
                    if (resource == null) return;

                    LSLib.LS.Node storyNode = resource.Regions["Story"].Children["Story"][0];
                    var storyStream = new MemoryStream(storyNode.Attributes["Story"].Value as byte[] ?? throw new InvalidOperationException("Cannot proceed with null Story node"));

                    LoadStory(storyStream);

                    MessageBox.Show("Save game database loaded successfully.");
                    break;
                }
                case ".osi":
                {
                    using (var file = new FileStream(storyFilePath.Text, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        LoadStory(file);
                    }

                    MessageBox.Show("Story file loaded successfully.");
                    break;
                }
                default:
                {
                    MessageBox.Show($"Unsupported file extension: {extension}", "Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                }
            }
        }

        private void SaveSavegameDatabase()
        {
            var packageReader = new PackageReader(storyFilePath.Text);
            Package package = packageReader.Read();

            AbstractFileInfo globalsLsf = package.Files.FirstOrDefault(p => p.Name == "globals.lsf");
            if (globalsLsf == null)
            {
                MessageBox.Show("The specified package is not a valid savegame (globals.lsf not found)", "Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Load globals.lsf
            Resource resource;
            Stream rsrcStream = globalsLsf.MakeStream();
            try
            {
                using (var rsrcReader = new LSFReader(rsrcStream))
                {
                    resource = rsrcReader.Read();
                }
            }
            finally
            {
                globalsLsf.ReleaseStream();
            }

            // Save story resource and pack into the Story.Story attribute in globals.lsf
            using (var storyStream = new MemoryStream())
            {
                var storyWriter = new StoryWriter();
                storyWriter.Write(storyStream, _story);

                LSLib.LS.Node storyNode = resource.Regions["Story"].Children["Story"][0];
                storyNode.Attributes["Story"].Value = storyStream.ToArray();
            }

            // Save globals.lsf
            var rewrittenStream = new MemoryStream();
            // TODO: Resave using original version
            var rsrcWriter = new LSFWriter(rewrittenStream, FileVersion.CurrentVersion);
            rsrcWriter.Write(resource);
            rewrittenStream.Seek(0, SeekOrigin.Begin);

            // Re-package global.lsf
            var rewrittenPackage = new Package();
            StreamFileInfo globalsRepacked = StreamFileInfo.CreateFromStream(rewrittenStream, "globals.lsf");
            rewrittenPackage.Files.Add(globalsRepacked);

            List<AbstractFileInfo> files = package.Files.Where(x => x.Name != "globals.lsf").ToList();
            rewrittenPackage.Files.AddRange(files);

            using (var packageWriter = new PackageWriter(rewrittenPackage, $"{storyFilePath.Text}.tmp"))
            {
                // TODO: Resave using original version and flags
                packageWriter.Version = PackageVersion.V13;
                packageWriter.Compression = CompressionMethod.Zlib;
                packageWriter.CompressionLevel = CompressionLevel.DefaultCompression;
                packageWriter.Write();
            }

            rewrittenStream.Dispose();
            packageReader.Dispose();

            // Create a backup of the original .lsf
            string backupPath = $"{storyFilePath.Text}.backup";
            if (!File.Exists(backupPath))
            {
                File.Move(storyFilePath.Text, backupPath);
            }
            else
            {
                File.Delete(storyFilePath.Text);
            }

            // Replace original savegame with new one
            File.Move($"{storyFilePath.Text}.tmp", storyFilePath.Text);
        }

        private void SaveStory()
        {
            using (var file = new FileStream(storyFilePath.Text, FileMode.Create, FileAccess.Write))
            {
                var writer = new StoryWriter();
                writer.Write(file, _story);
            }
        }

        private void saveStoryBtn_Click(object sender, EventArgs e)
        {
            if (_story == null)
            {
                MessageBox.Show("No story file loaded.", "Story save failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"Story export is an experimental feature and may corrupt your story files.{Environment.NewLine}Are you sure you want to continue?", "Save story", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            string extension = Path.GetExtension(storyFilePath.Text)?.ToLower();

            switch (extension)
            {
                case ".lsv":
                {
                    SaveSavegameDatabase();
                    MessageBox.Show("Save game database save successful.");
                    break;
                }
                case ".osi":
                {
                    SaveStory();
                    MessageBox.Show("Story file save successful.");
                    break;
                }
                default:
                {
                    MessageBox.Show($"Unsupported file extension: {extension}", "Story save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                }
            }
        }

        private void decompileStoryBtn_Click(object sender, EventArgs e)
        {
            if (_story == null)
            {
                MessageBox.Show("A story file must be loaded before exporting.", "Story export failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string debugPath = Path.Combine(goalPath.Text, "debug.log");
            using (var debugFile = new FileStream(debugPath, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(debugFile))
                {
                    _story.DebugDump(writer);
                }
            }

            string unassignedPath = Path.Combine(goalPath.Text, "UNASSIGNED_RULES.txt");
            using (var goalFile = new FileStream(unassignedPath, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(goalFile))
                {
                    var dummyGoal = new Goal(_story)
                    {
                        ExitCalls = new List<Call>(),
                        InitCalls = new List<Call>(),
                        ParentGoals = new List<GoalReference>(),
                        SubGoals = new List<GoalReference>(),
                        Name = "UNASSIGNED_RULES",
                        Index = 0
                    };
                    dummyGoal.MakeScript(writer, _story);
                }
            }

            foreach (KeyValuePair<uint, Goal> goal in _story.Goals)
            {
                string filePath = Path.Combine(goalPath.Text, $"{goal.Value.Name}.txt");
                using (var goalFile = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    using (var writer = new StreamWriter(goalFile))
                    {
                        goal.Value.MakeScript(writer, _story);
                    }
                }
            }

            MessageBox.Show("Story unpacked successfully.");
        }

        private void databaseSelectorCb_SelectedIndexChanged(object sender, EventArgs e)
        {
            databaseGrid.DataSource = null;
            databaseGrid.Columns.Clear();

            if (databaseSelectorCb.SelectedIndex == -1)
            {
                return;
            }

            Database database = _story.Databases[(uint) databaseSelectorCb.SelectedIndex + 1];
            databaseGrid.DataSource = database.Facts;

            for (var i = 0; i < database.Parameters.Types.Count; i++)
            {
                databaseGrid.Columns[i].HeaderText = $"{i} ({_story.Types[database.Parameters.Types[i]].Name})";
            }
        }

        private void btnDebugExport_Click(object sender, EventArgs e)
        {
            string filePath = Path.Combine(goalPath.Text, "debug.json");
            using (var debugFileStream = new FileStream(filePath, FileMode.Create))
            {
                var sev = new StoryDebugExportVisitor(debugFileStream);
                sev.Visit(_story);
            }
        }
    }
}
