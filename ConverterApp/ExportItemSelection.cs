using System;
using System.Drawing;
using System.Windows.Forms;
using LSLib.Granny.Model;
using LSLib.Granny.Model.CurveData;

namespace ConverterApp
{
    public partial class ExportItemSelection : System.Windows.Forms.ListView
    {
        private ComboBox CurrentItemCombo;
        private ListViewItem CurrentItem;

        public ExportItemSelection()
        {
            InitializeComponent();

            this.MouseUp += EventMouseUp;
        }

        void PopulateCombo()
        {
            if (CurrentItem.SubItems[1].Text == "Mesh")
            {
                CurrentItemCombo.Items.Add("Automatic");
                foreach (var defn in VertexFormatRegistry.GetAllTypes())
                {
                    CurrentItemCombo.Items.Add(defn.Key);
                }
            }
            else if (CurrentItem.SubItems[1].Text == "Position Track" ||
                CurrentItem.SubItems[1].Text == "Rotation Track" ||
                CurrentItem.SubItems[1].Text == "Scale/Shear Track")
            {
                CurrentItemCombo.Items.Add("Automatic");
                foreach (var defn in CurveRegistry.GetAllTypes())
                {
                    CurrentItemCombo.Items.Add(defn.Key);
                }
            }
        }

        void CreateItemSelectionCombo(Rectangle bounds)
        {
            CurrentItemCombo = new ComboBox();
            CurrentItemCombo.DropDownStyle = ComboBoxStyle.DropDownList;

            PopulateCombo();
            if (CurrentItemCombo.Items.Count == 0)
            {
                CurrentItemCombo = null;
                return;
            }

            // Assign calculated bounds to the ComboBox.
            CurrentItemCombo.Bounds = bounds;

            // Set default text for ComboBox to match the item that is clicked.
            CurrentItemCombo.Text = CurrentItem.SubItems[2].Text;

            // Display the ComboBox, and make sure that it is on top with focus.
            CurrentItemCombo.Parent = this;
            CurrentItemCombo.Visible = true;
            CurrentItemCombo.BringToFront();
            CurrentItemCombo.Focus();

            CurrentItemCombo.SelectedValueChanged += FormatCombo_ValueChanged;
            CurrentItemCombo.Leave += FormatCombo_ValueChanged;
            CurrentItemCombo.KeyPress += FormatCombo_KeyPress;
        }

        void FormatCombo_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Verify that the user presses ESC.
            switch (e.KeyChar)
            {
                case (char)(int)Keys.Escape:
                    {
                        // Reset the original text value, and then hide the ComboBox.
                        FormatCombo_ValueChanged(sender, e);
                        break;
                    }

                case (char)(int)Keys.Enter:
                    {
                        // Hide the ComboBox.
                        CurrentItemCombo.Hide();
                        CurrentItemCombo.Parent = null;
                        CurrentItemCombo = null;
                        break;
                    }
            }
        }

        void FormatCombo_ValueChanged(object sender, EventArgs e)
        {
            if (CurrentItemCombo.Text.Length > 0)
            {
                CurrentItem.SubItems[2].Text = CurrentItemCombo.Text;
            }

            CurrentItemCombo.Hide();
            CurrentItemCombo = null;
        }

        void EventMouseUp(object sender, MouseEventArgs e)
        {
            // Get the item on the row that is clicked.
            var Item = GetItemAt(e.X, e.Y);

            // Make sure that an item is clicked.
            if (Item != null)
            {
                // Get the bounds of the item that is clicked.
                Rectangle ClickedItem = Item.Bounds;

                // Verify that the column is completely scrolled off to the left.
                if ((ClickedItem.Left + Columns[2].Width) < 0)
                {
                    // If the cell is out of view to the left, do nothing.
                    return;
                }

                // Verify that the column is partially scrolled off to the left.
                else if (ClickedItem.Left < 0)
                {
                    // Determine if column extends beyond right side of ListView.
                    if ((ClickedItem.Left + Columns[2].Width) > Width)
                    {
                        // Set width of column to match width of ListView.
                        ClickedItem.Width = Width;
                        ClickedItem.X = 0;
                    }
                    else
                    {
                        // Right side of cell is in view.
                        ClickedItem.Width = Columns[2].Width + ClickedItem.Left;
                        ClickedItem.X = 2;
                    }
                }
                else if (Columns[2].Width > Width)
                {
                    ClickedItem.Width = Width;
                }
                else
                {
                    ClickedItem.Width = Columns[2].Width;
                    ClickedItem.X = 2 + Columns[0].Width + Columns[1].Width;
                }

                CurrentItem = Item;
                CreateItemSelectionCombo(ClickedItem);
            }
        }
    }
}
