using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
            var packageReader = new PackageReader();
            using var package = packageReader.Read(path);

            var abstractFileInfo = package.Files.FirstOrDefault(p => p.Name.ToLowerInvariant() == "globals.lsf");
            if (abstractFileInfo == null)
            {
                MessageBox.Show("The specified package is not a valid savegame (globals.lsf not found)", "Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            Resource resource;
            using var rsrcStream = abstractFileInfo.CreateContentReader();
            using var rsrcReader = new LSFReader(rsrcStream);
            resource = rsrcReader.Read();

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
                            _story = reader.Read(file);
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

        private void databaseGrid_CellContextMenuStripNeeded(object sender, DataGridViewCellContextMenuStripNeededEventArgs e)
        {
            if (databaseGrid.SelectedRows.Count == 1 && e.RowIndex != -1)
            {
                databaseGrid.ClearSelection();
                databaseGrid.Rows[e.RowIndex].Selected = true;
            }
            else if (databaseGrid.SelectedCells.Count == 1 && e.RowIndex != -1)
            {
                databaseGrid.ClearSelection();
                databaseGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected = true;
            }
        }
        private void databaseGrid_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (databaseGrid.Rows.Count.Equals(0))
                {
                    ContextMenuStrip m = new ContextMenuStrip();
                    ToolStripMenuItem MenuItem;

                    MenuItem = new ToolStripMenuItem { Name = "AddRow", Text = "Add Row" };
                    MenuItem.Click += new EventHandler(databaseGrid_MenuItemClickHandler);
                    m.Items.Add(MenuItem);
                    m.Show(databaseGrid, new Point(e.X, e.Y), ToolStripDropDownDirection.BelowRight);
                }
                else if (true)
                {
                    ContextMenuStrip m = new ContextMenuStrip();
                    ToolStripMenuItem MenuItem;

                    MenuItem = new ToolStripMenuItem { Name = "AddRow", Text = "Add Row" };
                    MenuItem.Click += new EventHandler(databaseGrid_MenuItemClickHandler);
                    m.Items.Add(MenuItem);
                    MenuItem = new ToolStripMenuItem { Name = "CopyTypes", Text = "Copy database column types" };
                    MenuItem.Click += new EventHandler(databaseGrid_MenuItemClickHandler);
                    m.Items.Add(MenuItem);
                    MenuItem = new ToolStripMenuItem { Name = "DeleteRow", Text = "Delete Row" };
                    MenuItem.Click += new EventHandler(databaseGrid_MenuItemClickHandler);
                    m.Items.Add(MenuItem);

                    m.Show(databaseGrid, new Point(e.X, e.Y), ToolStripDropDownDirection.BelowRight);
                }
            }
        }
        private void databaseGrid_MenuItemClickHandler(object sender, EventArgs e)
        {
            ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
            if (clickedItem.Name == "AddRow")
            {
                if (databaseSelectorCb.SelectedItem != null)
                {
                    var selectedIndex = ((KeyValuePair<uint, string>)databaseSelectorCb.SelectedItem).Key;
                    Database database = _story.Databases[selectedIndex + 1];

                    List<Boolean> ColumnTypeCheck = new List<Boolean>();
                    List<uint> ColumnTypeUInt = new List<uint>();
                    database.Parameters.Types.ForEach(type => ColumnTypeCheck.Add(false));
                    if (database.Facts.Count == 0)
                    {
                        string[] ColumnTypes = Clipboard.GetText().Split(";");
                        if (ColumnTypes.Length == database.Parameters.Types.Count)
                        {
                            for (int i = 0; i < ColumnTypes.Length; i++)
                            {
                                string[] ColumnType = ColumnTypes[i].Split(" ");
                                if (ColumnType.Length == 2)
                                {
                                    if (uint.TryParse(ColumnType[0], out uint result))
                                    {
                                        ColumnTypeUInt.Add(Convert.ToUInt32(ColumnType[0]));
                                        string ColumnTypeString;
                                        ColumnTypeString = ColumnType[1];
                                        if (ColumnTypeString == "{" + _story.Types[database.Parameters.Types[i]].Name + "}")
                                        {
                                            ColumnTypeCheck[i] = true;
                                        }
                                    }
                                }
                            }
                        }
                        /*
                        if (ColumnTypeCheck.Contains(false))
                        {
                            MessageBox.Show("Can't get database column type from clipboard." + Environment.NewLine +
                                "There is a bug the databaset hat the column type of the database is not the specified in the database parameters." + Environment.NewLine +
                                "This causes the program to crash with the wrong database column type." + Environment.NewLine +
                                 Environment.NewLine +
                                "Right-Click and use 'Copy database column types' from the same database of another Story/savegame to add the correct database column types!",
                                "Can't get database column types from clipboard",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        */
                    }
                    DialogResult ConfirmCreateEntry = DialogResult.No;
                    if (database.Facts.Count == 0 && ColumnTypeCheck.Contains(false))
                    {
                        ConfirmCreateEntry = MessageBox.Show("Story needs to saved and reloaded, otherwise the program might crash due to mismatch of the database column type." + Environment.NewLine +
                            "Creating a new entry in an empty database is even more dangerous, because for unkown reasons in some databases the columns have a different type than in the properties and this can corrupt your story file." + Environment.NewLine +
                             Environment.NewLine +
                            "Alternative: Use the function 'Copy database column types' from another story" + Environment.NewLine +
                             Environment.NewLine +
                             Environment.NewLine +
                            "Story export is an experimental feature and may corrupt your story files." + Environment.NewLine +
                            "Are you sure you want to do this?",
                            "Story needs to be saved and reloaded",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);
                    }
                    if (database.Facts.Count > 0 || !ColumnTypeCheck.Contains(false) || ConfirmCreateEntry == DialogResult.Yes)
                    {
                        Fact NewRow = new Fact { };
                        NewRow.Columns = new System.Collections.Generic.List<LSLib.LS.Story.Value>();
                        for (var i = 0; i < database.Parameters.Types.Count; i++)
                        {
                            LSLib.LS.Story.Value NewColumn;
                            NewColumn = new LSLib.LS.Story.Value();
                            if (database.Facts.Count > 0)
                            {
                                // Adds new column with TypeId from existing row (reason see further below)
                                NewColumn.TypeId = database.Facts[0].Columns[i].TypeId;
                            }
                            else
                            {
                                if (!ColumnTypeCheck.Contains(false))
                                {
                                    // Add new column with TypeId from a valid clipboard entry (reason see further below)
                                    NewColumn.TypeId = ColumnTypeUInt[i];
                                }
                                else
                                {
                                    // Add new column with TypeId from intended Parameter.Types
                                    // !!! Only use it in combination with saving and reloading the story (buggy, can cause crash, reason see below) !!!

                                    // This sometimes causes program to crash because some cells have a different TypeId than in the database.Parameters.Types (in Balgur's Gate 3)!
                                    // Sometimes cells for "DialogResource" (TypeId = 11) have the TypeId = 5 and sometimes the intended TypeId = 11, same goes for Characters (TypeId = 6).
                                    // This causes the program to crash when entering / editing the cell with wrong TypeId (unless story reloaded)
                                    NewColumn.TypeId = database.Parameters.Types[i];
                                }
                            }
                            NewRow.Columns.Add(NewColumn);
                        }
                        database.Facts.Add(NewRow);
                        RefreshDataGrid();
                        if (ConfirmCreateEntry == DialogResult.Yes)
                        {
                            SaveSavegameDatabase();
                            loadStoryBtn.PerformClick();
                        }
                    }
                }
            }
            else if (clickedItem.Name == "DeleteRow")
            {
                var selectedIndex = ((KeyValuePair<uint, string>)databaseSelectorCb.SelectedItem).Key;
                Database database = _story.Databases[selectedIndex + 1];
                // Creates sortable list of rows / indexes
                List<int> SelectedRows = new List<int>();
                for (var i = 0; i < databaseGrid.SelectedCells.Count; i++)
                {
                    SelectedRows.Add(databaseGrid.SelectedCells[i].RowIndex);
                }
                SelectedRows.Sort();
                SelectedRows = SelectedRows.Distinct().ToList(); // removes duplicates
                SelectedRows.Reverse(); // Reverse sorting to delete from bottom to top

                foreach (int index in SelectedRows)
                {
                    if (database.Facts.Count - 1 >= index)
                    {
                        Fact RemoveItem = database.Facts[index];
                        database.Facts.Remove(RemoveItem);
                    }
                }
                RefreshDataGrid();
            }
            else if (clickedItem.Name == "CopyTypes")
            {
                var selectedIndex = ((KeyValuePair<uint, string>)databaseSelectorCb.SelectedItem).Key;
                Database database = _story.Databases[selectedIndex + 1];
                List<string> ColumnTypes = new List<string>();
                for (var i = 0; i < database.Parameters.Types.Count; i++)
                {
                    ColumnTypes.Add(database.Facts[0].Columns[i].TypeId + " {" + _story.Types[database.Parameters.Types[i]].Name + "}");
                };
                Clipboard.SetText(string.Join(";", ColumnTypes));
                MessageBox.Show("Database column types copied");
                RefreshDataGrid();
            }
        }
    }
}