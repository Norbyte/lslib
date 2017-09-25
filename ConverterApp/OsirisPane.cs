using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LSLib.LS;
using LSLib.LS.Osiris;
using System.IO;
using LSLib.LS.LSF;

namespace ConverterApp
{
    public partial class OsirisPane : UserControl
    {
        private Story Story;

        public OsirisPane()
        {
            InitializeComponent();
        }

        private void storyFileBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = storyPathDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                storyFilePath.Text = storyPathDlg.FileName;
            }
        }

        private void goalPathBrowseBtn_Click(object sender, EventArgs e)
        {
            var result = goalPathDlg.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                goalPath.Text = goalPathDlg.SelectedPath;
            }
        }

        private void loadStory(Stream s)
        {
            var reader = new StoryReader();
            Story = reader.Read(s);

            databaseSelectorCb.Items.Clear();
            foreach (var database in Story.Databases)
            {
                var name = "(Unnamed)";
                var owner = database.Value.OwnerNode;
                if (owner != null && owner.Name.Length > 0)
                {
                    name = String.Format("{0}({1})", owner.Name, owner.NumParams);
                }
                else if (owner != null)
                {
                    name = String.Format("<{0}>", owner.TypeName());
                }

                name += String.Format(" #{0} ({1} rows)", database.Key, database.Value.Facts.Count);

                databaseSelectorCb.Items.Add(name);

                if (databaseSelectorCb.Items.Count > 0)
                {
                    databaseSelectorCb.SelectedIndex = 0;
                }
            }
        }

        private void loadStoryBtn_Click(object sender, EventArgs e)
        {
            var extension = Path.GetExtension(storyFilePath.Text).ToLower();

            if (extension == ".lsv")
            {
                var packageReader = new PackageReader(storyFilePath.Text);
                var package = packageReader.Read();

                LSLib.LS.FileInfo file = package.Files.Where(p => p.Name == "globals.lsf").FirstOrDefault();
                if (file == null)
                {
                    MessageBox.Show("The specified package is not a valid savegame (globals.lsf not found)", "Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Resource resource;
                using (var rsrcStream = file.MakeStream())
                using (var rsrcReader = new LSFReader(rsrcStream))
                {
                    resource = rsrcReader.Read();
                }

                var storyNode = resource.Regions["Story"].Children["Story"][0];
                var storyStream = new MemoryStream(storyNode.Attributes["Story"].Value as byte[]);

                loadStory(storyStream);

                MessageBox.Show("Save game database loaded successfully.");
            }
            else if (extension == ".osi")
            {
                using (var file = new FileStream(storyFilePath.Text, FileMode.Open, FileAccess.Read))
                {
                    loadStory(file);
                }

                MessageBox.Show("Story file loaded successfully.");
            }
            else
            {
                MessageBox.Show("Unsupported file extension: " + extension, "Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void saveSavegameDatabase()
        {
            var packageReader = new PackageReader(storyFilePath.Text);
            var package = packageReader.Read();

            LSLib.LS.FileInfo globalsLsf = package.Files.Where(p => p.Name == "globals.lsf").FirstOrDefault();
            if (globalsLsf == null)
            {
                MessageBox.Show("The specified package is not a valid savegame (globals.lsf not found)", "Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Load globals.lsf
            Resource resource;
            using (var rsrcStream = globalsLsf.MakeStream())
            using (var rsrcReader = new LSFReader(rsrcStream))
            {
                resource = rsrcReader.Read();
            }

            // Save story resource and pack into the Story.Story attribute in globals.lsf
            using (var storyStream = new MemoryStream())
            {
                var storyWriter = new StoryWriter();
                storyWriter.Write(storyStream, Story);

                var storyNode = resource.Regions["Story"].Children["Story"][0];
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
            var globalsRepacked = StreamFileInfo.CreateFromStream(rewrittenStream, "globals.lsf");
            rewrittenPackage.Files.Add(globalsRepacked);

            foreach (var file in package.Files)
            {
                if (file.Name != "globals.lsf")
                {
                    rewrittenPackage.Files.Add(file);
                }
            }

            using (var packageWriter = new PackageWriter(rewrittenPackage, storyFilePath.Text + ".tmp"))
            {
                // TODO: Resave using original version and flags
                packageWriter.Version = 13;
                packageWriter.Compression = CompressionMethod.Zlib;
                packageWriter.CompressionLevel = CompressionLevel.DefaultCompression;
                packageWriter.Write();
            }

            rewrittenStream.Dispose();
            packageReader.Dispose();

            // Create a backup of the original .lsf
            var backupPath = storyFilePath.Text + ".backup";
            if (!File.Exists(backupPath))
            {
                File.Move(storyFilePath.Text, backupPath);
            }
            else
            {
                File.Delete(storyFilePath.Text);
            }

            // Replace original savegame with new one
            File.Move(storyFilePath.Text + ".tmp", storyFilePath.Text);
        }

        private void saveStory()
        {
            using (var file = new FileStream(storyFilePath.Text, FileMode.Create, FileAccess.Write))
            {
                var writer = new StoryWriter();
                writer.Write(file, Story);
            }
        }

        private void saveStoryBtn_Click(object sender, EventArgs e)
        {
            if (Story == null)
            {
                MessageBox.Show("No story file loaded.", "Story save failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Story export is an experimental feature and may corrupt your story files.\r\nAre you sure you want to continue?", "Save story", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            var extension = Path.GetExtension(storyFilePath.Text).ToLower();

            if (extension == ".lsv")
            {
                saveSavegameDatabase();
                MessageBox.Show("Save game database save successful.");
            }
            else if (extension == ".osi")
            {
                saveStory();
                MessageBox.Show("Story file save successful.");
            }
            else
            {
                MessageBox.Show("Unsupported file extension: " + extension, "Story save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void decompileStoryBtn_Click(object sender, EventArgs e)
        {
            if (Story == null)
            {
                MessageBox.Show("A story file must be loaded before exporting.", "Story export failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var debugPath = goalPath.Text + "/debug.log";
            using (var debugFile = new FileStream(debugPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(debugFile))
            {
                Story.DebugDump(writer);
            }

            var unassignedPath = goalPath.Text + "/UNASSIGNED_RULES.txt";
            using (var goalFile = new FileStream(unassignedPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(goalFile))
            {
                var dummyGoal = new Goal(Story);
                dummyGoal.ExitCalls = new List<Call>();
                dummyGoal.InitCalls = new List<Call>();
                dummyGoal.ParentGoals = new List<GoalReference>();
                dummyGoal.SubGoals = new List<GoalReference>();
                dummyGoal.Name = "UNASSIGNED_RULES";
                dummyGoal.Index = 0;
                dummyGoal.MakeScript(writer, Story);
            }

            foreach (var goal in Story.Goals)
            {
                var filePath = goalPath.Text + "/" + goal.Value.Name + ".txt";
                using (var goalFile = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(goalFile))
                {
                    goal.Value.MakeScript(writer, Story);
                }
            }

            MessageBox.Show("Story unpacked successfully.");
        }

        private void databaseSelectorCb_SelectedIndexChanged(object sender, EventArgs e)
        {
            databaseGrid.DataSource = null;
            databaseGrid.Columns.Clear();

            if (databaseSelectorCb.SelectedIndex != -1)
            {
                var database = Story.Databases[(uint)databaseSelectorCb.SelectedIndex + 1];
                databaseGrid.DataSource = database.Facts;

                for (var i = 0; i < database.Parameters.Types.Count; i++)
                {
                    databaseGrid.Columns[i].HeaderText = i.ToString() + " (" + Story.Types[database.Parameters.Types[i]].Name + ")";
                }
            }
        }
    }
}
