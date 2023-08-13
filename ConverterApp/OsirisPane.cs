using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using LSLib.LS;
using LSLib.LS.Enums;
using LSLib.LS.Save;
using LSLib.LS.Story;
using Node = LSLib.LS.Story.Node;

namespace ConverterApp
{
    public partial class OsirisPane : UserControl
    {
        private Story _story;
        public Game Game;
        
        private ConverterAppSettings Settings { get; }

        private readonly List<KeyValuePair<uint, string>> _databaseItems = new List<KeyValuePair<uint, string>>();
        
        public OsirisPane(ISettingsDataSource settingsDataSource)
        {
            InitializeComponent();

            Settings = settingsDataSource.Settings;

            storyFilePath.DataBindings.Add("Text", settingsDataSource, "Settings.Story.InputPath", true, DataSourceUpdateMode.OnPropertyChanged);
            goalPath.DataBindings.Add("Text", settingsDataSource, "Settings.Story.OutputPath", true, DataSourceUpdateMode.OnPropertyChanged);
            tbFilter.DataBindings.Add("Text", settingsDataSource, "Settings.Story.FilterText", true, DataSourceUpdateMode.OnPropertyChanged);
            btnFilterMatchCase.DataBindings.Add("Tag", settingsDataSource, "Settings.Story.FilterMatchCase", true, DataSourceUpdateMode.OnPropertyChanged);
            
            btnFilterMatchCase.BackColor = Color.FromKnownColor(Settings.Story.FilterMatchCase ? KnownColor.MenuHighlight : KnownColor.Control);
            
            databaseSelectorCb.DisplayMember = "Value";
            databaseSelectorCb.ValueMember = "Key";
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

        private void LoadStory()
        {
            _databaseItems.Clear();

            uint index = 0;
            foreach (KeyValuePair<uint, Database> database in _story.Databases)
            {
                var name = "(Unnamed)";
                Node owner = database.Value.OwnerNode;
                if (owner != null)
                {
                    name = owner.Name.Length > 0 ? $"{owner.Name}({owner.NumParams})" : $"<{owner.TypeName()}>";
                }

                name += $" #{database.Key} ({database.Value.Facts.Count} rows)";

                _databaseItems.Add(new KeyValuePair<uint, string>(index, name));
                index += 1;
            }
            
            databaseSelectorCb_FilterDropdownList();
            RefreshDataGrid();
        }

        public Resource LoadResourceFromSave(string path)
        {
            var packageReader = new PackageReader(path);
            Package package = packageReader.Read();
            
            AbstractFileInfo abstractFileInfo = package.Files.FirstOrDefault(p => p.Name.ToLowerInvariant() == "globals.lsf");
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
                    using (var saveHelpers = new SavegameHelpers(storyFilePath.Text))
                    {
                        _story = saveHelpers.LoadStory();
                        LoadStory();
                    }

                    MessageBox.Show("Save game database loaded successfully.");
                    break;
                }
                case ".osi":
                {
                    using (var file = new FileStream(storyFilePath.Text, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var reader = new StoryReader();
                        reader.Read(file);
                        LoadStory();
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
            using (var saveHelpers = new SavegameHelpers(storyFilePath.Text))
            {
                saveHelpers.ResaveStory(_story, Game, $"{storyFilePath.Text}.tmp");
            }

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
                writer.Write(file, _story, false);
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

        private void databaseSelectorCb_FilterDropdownList()
        {
            tbFilter.BackColor = Color.FromKnownColor(KnownColor.Window);

            if (tbFilter.Text.Trim().Length == 0)
            {
                tbFilter.Text = string.Empty;
                databaseSelectorCb.DataSource = _databaseItems;
                return;
            }

            List<KeyValuePair<uint, string>> queryResults;

            if (Settings.Story.FilterMatchCase == false)
            {
                queryResults = _databaseItems
                    .Where(s => s.Value.ToLowerInvariant().Contains(tbFilter.Text.ToLowerInvariant()))
                    .ToList();
            }
            else
            {
                queryResults = _databaseItems
                    .Where(s => s.Value.Contains(tbFilter.Text))
                    .ToList();
            }

            if (queryResults.Any())
            {
                databaseSelectorCb.DataSource = queryResults;
                databaseSelectorCb.SelectedIndex = 0;
            }
            else
            {
                tbFilter.BackColor = Color.LightCoral;
            }
        }

        private void RefreshDataGrid()
        {
            databaseGrid.DataSource = null;
            databaseGrid.Columns.Clear();

            if (databaseSelectorCb.SelectedItem != null)
            {
                var selectedIndex = ((KeyValuePair<uint, string>)databaseSelectorCb.SelectedItem).Key;
                Database database = _story.Databases[selectedIndex + 1];
                databaseGrid.DataSource = database.Facts;

                for (var i = 0; i < database.Parameters.Types.Count; i++)
                {
                    databaseGrid.Columns[i].HeaderText = $"{i} ({_story.Types[database.Parameters.Types[i]].Name})";
                }
            }
        }

        private void databaseSelectorCb_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshDataGrid();
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

        private void databaseFilter_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                databaseSelectorCb_FilterDropdownList();
        }

        private void btnDatabaseFilterMatchCase_Click(object sender, EventArgs e)
        {
            Settings.Story.FilterMatchCase = !Settings.Story.FilterMatchCase;

            btnFilterMatchCase.BackColor = Color.FromKnownColor(Settings.Story.FilterMatchCase ? KnownColor.MenuHighlight : KnownColor.Control);

            if (tbFilter.Text.Trim().Length > 0)
                databaseSelectorCb_FilterDropdownList();
        }
    }
}
