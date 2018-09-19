using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LSLib.Granny.Model;
using LSLib.Granny.Model.CurveData;

namespace ConverterApp
{
    public partial class ExportItemSelection : ListView
    {
        private ListViewItem _currentItem;
        private ComboBox _currentItemCombo;

        public ExportItemSelection()
        {
            InitializeComponent();

            MouseUp += EventMouseUp;
        }

        private void PopulateCombo()
        {
            switch (_currentItem.SubItems[1].Text)
            {
                case "Mesh":
                {
                    _currentItemCombo.Items.Add("Automatic");
                    // TODO - add list of commonly used formats?
                    break;
                }
                case "Position Track":
                case "Rotation Track":
                case "Scale/Shear Track":
                {
                    _currentItemCombo.Items.Add("Automatic");
                    foreach (KeyValuePair<string, Type> defn in CurveRegistry.GetAllTypes())
                    {
                        _currentItemCombo.Items.Add(defn.Key);
                    }
                    break;
                }
            }
        }

        private void CreateItemSelectionCombo(Rectangle bounds)
        {
            _currentItemCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            PopulateCombo();
            if (_currentItemCombo.Items.Count == 0)
            {
                _currentItemCombo = null;
                return;
            }

            // Assign calculated bounds to the ComboBox.
            _currentItemCombo.Bounds = bounds;

            // Set default text for ComboBox to match the item that is clicked.
            _currentItemCombo.Text = _currentItem.SubItems[2].Text;

            // Display the ComboBox, and make sure that it is on top with focus.
            _currentItemCombo.Parent = this;
            _currentItemCombo.Visible = true;
            _currentItemCombo.BringToFront();
            _currentItemCombo.Focus();

            _currentItemCombo.SelectedValueChanged += FormatCombo_ValueChanged;
            _currentItemCombo.Leave += FormatCombo_ValueChanged;
            _currentItemCombo.KeyPress += FormatCombo_KeyPress;
        }

        private void FormatCombo_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Verify that the user presses ESC.
            switch (e.KeyChar)
            {
                case (char) (int) Keys.Escape:
                {
                    // Reset the original text value, and then hide the ComboBox.
                    FormatCombo_ValueChanged(sender, e);
                    break;
                }

                case (char) (int) Keys.Enter:
                {
                    // Hide the ComboBox.
                    _currentItemCombo.Hide();
                    _currentItemCombo.Parent = null;
                    _currentItemCombo = null;
                    break;
                }
            }
        }

        private void FormatCombo_ValueChanged(object sender, EventArgs e)
        {
            if (_currentItemCombo.Text.Length > 0)
            {
                _currentItem.SubItems[2].Text = _currentItemCombo.Text;
            }

            _currentItemCombo.Hide();
            _currentItemCombo = null;
        }

        private void EventMouseUp(object sender, MouseEventArgs e)
        {
            // Get the item on the row that is clicked.
            ListViewItem item = GetItemAt(e.X, e.Y);

            // Make sure that an item is clicked.
            if (item == null)
            {
                return;
            }

            // Get the bounds of the item that is clicked.
            Rectangle clickedItem = item.Bounds;

            // Verify that the column is completely scrolled off to the left.
            if (clickedItem.Left + Columns[2].Width < 0)
            {
                // If the cell is out of view to the left, do nothing.
                return;
            }

            // Verify that the column is partially scrolled off to the left.
            if (clickedItem.Left < 0)
            {
                // Determine if column extends beyond right side of ListView.
                if (clickedItem.Left + Columns[2].Width > Width)
                {
                    // Set width of column to match width of ListView.
                    clickedItem.Width = Width;
                    clickedItem.X = 0;
                }
                else
                {
                    // Right side of cell is in view.
                    clickedItem.Width = Columns[2].Width + clickedItem.Left;
                    clickedItem.X = 2;
                }
            }
            else if (Columns[2].Width > Width)
            {
                clickedItem.Width = Width;
            }
            else
            {
                clickedItem.Width = Columns[2].Width;
                clickedItem.X = 2 + Columns[0].Width + Columns[1].Width;
            }

            _currentItem = item;
            CreateItemSelectionCombo(clickedItem);
        }
    }
}
