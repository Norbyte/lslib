using LSLib.Granny;
using LSLib.Granny.GR2;
using LSLib.Granny.Model;
using LSLib.LS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ConverterApp
{
    public partial class ClothPane : UserControl
    {
        private readonly List<Mesh> physicsMeshes = [];
        private readonly List<Mesh> targetMeshes = [];

        public ClothPane(ISettingsDataSource settingsDataSource)
        {
            InitializeComponent();

            inputPath.DataBindings.Add("Text", settingsDataSource, "Settings.Cloth.InputPath", true, DataSourceUpdateMode.OnPropertyChanged);
        }

        private void browseBtn_Click(object sender, EventArgs e)
        {
            if (inputFileDlg.ShowDialog(this) == DialogResult.OK)
            {
                inputPath.Text = inputFileDlg.FileName;
            }
        }

        private void loadBtn_Click(object sender, EventArgs e)
        {
            string nl = Environment.NewLine;

            try
            {
                LoadFile();
            }
            catch (ParsingException exc)
            {
                MessageBox.Show($"Import failed!{nl}{nl}{exc.Message}", "Import Failed");
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Internal error!{nl}{nl}{exc}", "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void physicsMeshComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (physicsMeshComboBox.SelectedIndex >= physicsMeshes.Count)
            {
                return;
            }

            string name = physicsMeshes[physicsMeshComboBox.SelectedIndex].Name;
            Match match = LODRegex().Match(name);

            var items = targetMeshesListView.Items;

            if (!match.Success)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].Checked = !LODRegex().IsMatch(targetMeshes[i].Name);
                }
            }
            else
            {
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].Checked = targetMeshes[i].Name.EndsWith(match.Value);
                }
            }
        }

        private void generateBtn_Click(object sender, EventArgs e)
        {
            string nl = Environment.NewLine;

            try
            {
                Generate();
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Internal error!{nl}{nl}{exc}", "Generation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadFile()
        {
            var root = GR2Utils.LoadModel(inputPath.Text, new ExporterOptions
            {
                InputFormat = ExportFormat.GR2,
            });

            resourceNameTextBox.Text = Path.GetFileNameWithoutExtension(inputPath.Text);

            var physicsItems = physicsMeshComboBox.Items;
            var targetItems = targetMeshesListView.Items;

            physicsMeshes.Clear();
            physicsItems.Clear();

            targetMeshes.Clear();
            targetItems.Clear();

            foreach (var mesh in root.Meshes)
            {
                if (!mesh.ExtendedData.UserMeshProperties.MeshFlags.IsCloth())
                {
                    continue;
                }

                if (mesh.ExtendedData.UserMeshProperties.ClothFlags.HasClothPhysics())
                {
                    physicsMeshes.Add(mesh);
                    physicsItems.Add(mesh.Name);
                }
                else
                {
                    targetMeshes.Add(mesh);
                    targetItems.Add(new ListViewItem(new[] { mesh.Name }));
                }
            }

            physicsMeshComboBox.SelectedIndex = physicsItems.Count - 1;
        }

        private void Generate()
        {
            if (physicsMeshComboBox.SelectedIndex < 0)
            {
                return;
            }

            Mesh physicsMesh = physicsMeshes[physicsMeshComboBox.SelectedIndex];
            string physicsName = GetMeshName(physicsMesh);

            var (root, children) = CreateObject(physicsName);
            var doc = new XDocument(root);

            foreach (int index in targetMeshesListView.CheckedIndices)
            {
                Mesh targetMesh = targetMeshes[index];

                ClothUtils.Triplet[] items = ClothUtils.Generate(physicsMesh, targetMesh);
                children.Add(CreateElement(GetMeshName(targetMesh), items));
            }

            generatedTextBox.Text = doc.ToString();
        }

        private string GetMeshName(Mesh mesh) => $"{resourceNameTextBox.Text}.{mesh.Name}.{mesh.ExportOrder}";

        private static XElement CreateElement(string targetName, ClothUtils.Triplet[] data)
        {
            /*
             * <node id="MapValue">
             *   <children>
             *     <node id="Object">
             *       <attribute id="ClosestVertices" type="ScratchBuffer" value="{closestVertices}" />
             *       <attribute id="Name" type="FixedString" value="{targetName}" />
             *       <attribute id="NbClosestVertices" type="int32" value="{nbClosestVertices}" />
             *     </node>
             *   </children>
             * </node>
            */

            string base64 = ClothUtils.Serialize(data);

            return new XElement("node",
                new XAttribute("id", "MapValue"),
                new XElement("children",
                    new XElement("node",
                    new XAttribute("id", "Object"),
                        new XElement("attribute", new XAttribute("id", "ClosestVertices"), new XAttribute("type", AttributeTypeMaps.IdToType[AttributeType.ScratchBuffer]), new XAttribute("value", base64)),
                        new XElement("attribute", new XAttribute("id", "Name"), new XAttribute("type", AttributeTypeMaps.IdToType[AttributeType.FixedString]), new XAttribute("value", targetName)),
                        new XElement("attribute", new XAttribute("id", "NbClosestVertices"), new XAttribute("type", AttributeTypeMaps.IdToType[AttributeType.Int]), new XAttribute("value", data.Length * 3)))));
        }

        private static (XElement Root, XElement Children) CreateObject(string physicsName)
        {
            /*
             * <node id="Object">
             *   <attribute id="MapKey" type="FixedString" value="{physicsName}" />
             *   <children />
             * </node>
            */

            var children = new XElement("children");
            var root = new XElement("node",
                new XAttribute("id", "Object"),
                new XElement("attribute", new XAttribute("id", "MapKey"), new XAttribute("type", AttributeTypeMaps.IdToType[AttributeType.FixedString]), new XAttribute("value", physicsName)),
                children);

            return (root, children);
        }

        [GeneratedRegex(@"_LOD\d+$")]
        private static partial Regex LODRegex();
    }
}
