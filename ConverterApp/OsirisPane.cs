using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LSLib.LS.Osiris;
using System.IO;

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

        private void loadStoryBtn_Click(object sender, EventArgs e)
        {
            using (var file = new FileStream(storyFilePath.Text, FileMode.Open, FileAccess.Read))
            {
                var reader = new StoryReader();
                Story = reader.Read(file);

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
                }
            }

            if (databaseSelectorCb.Items.Count > 0)
            {
                databaseSelectorCb.SelectedIndex = 0;
            }

            MessageBox.Show("Story file loaded successfully.");
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

            using (var file = new FileStream(storyFilePath.Text, FileMode.Create, FileAccess.Write))
            {
                var writer = new StoryWriter();
                writer.Write(file, Story);
            }

            MessageBox.Show("Story file save successful.");
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
