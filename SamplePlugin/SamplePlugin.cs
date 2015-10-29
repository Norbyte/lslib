using LSToolFramework;
using LSToolFramework.P4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SamplePlugin
{
    public class SamplePlugin : IPlugin
    {
        private SelectionSet selection;

        public override bool Init()
        {
            System.Console.WriteLine("SamplePlugin.Init");
            this.RegisterPanel();
            return true;
        }

        public override bool Start()
        {
            System.Console.WriteLine("SamplePlugin.Start");
            SelectionManager.Instance.ObjectsSelected += new EventHandler<SelectionEventArgs>(this.SelectManager_ObjectsSelected);
            SelectionManager.Instance.ObjectsDeselected += new EventHandler<SelectionEventArgs>(this.SelectManager_ObjectsDeselected);
            return true;
        }

        public override bool Stop()
        {
            System.Console.WriteLine("SamplePlugin.Stop");
            return true;
        }

        public void RegisterPanel()
        {
            MenuBarService menuBarService = (MenuBarService)ToolFramework.Instance.ServiceManagerInstance.GetService(typeof(MenuBarService));
            if (menuBarService == null)
                return;

            ToolStripMenuItem menuItem = new ToolStripMenuItem();
            menuItem.Name = "SAMPLE MENU";
            menuItem.Text = "SAMPLE PLUGIN MENU";
            menuItem.Click += new EventHandler(this.SampleMenuHandler);
            menuBarService.InsertSubMenuItem("Modding", menuItem);

            ToolStripMenuItem unlockItem = new ToolStripMenuItem();
            unlockItem.Name = "Unlock Selected Objects";
            unlockItem.Text = "Unlock Selected Objects";
            unlockItem.Click += new EventHandler(this.UnlockMenuHandler);
            menuBarService.InsertSubMenuItem("Modding", unlockItem);
        }

        public void SampleMenuHandler(object sender, EventArgs e)
        {
            MessageBox.Show("I'm a sample plugin!", "Sample");
        }

        public void UnlockMenuHandler(object sender, EventArgs e)
        {
            if (this.selection == null || this.selection.Count == 0)
            {
                MessageBox.Show("No objects to unlock!", "Unlock");
                return;
            }

            foreach (var obj in this.selection)
            {
                var editable = (EditableObject)obj;
                if (editable != null && editable.Locked)
                {
                    editable.Locked = false;
                    if (editable.Locked)
                    {
                        if (editable.P4Status == EP4Status.InSync || editable.P4Status == EP4Status.OutSync)
                            MessageBox.Show(String.Format("{0} unlock failed: Bad P4 status", editable.Name), "Unlock");
                        else if (editable.FileRights != EFileRights.Write)
                                MessageBox.Show(String.Format("{0} unlock failed: No write access", editable.Name), "Unlock");
                        else if (!LSToolFramework.FileAccess.Instance.CanSave(editable.FileName))
                            MessageBox.Show(String.Format("{0} unlock failed: File access error", editable.Name), "Unlock");
                        else
                            MessageBox.Show(String.Format("{0} unlock failed: Unknown reason", editable.Name), "Unlock");
                    }
                    else
                        MessageBox.Show(String.Format("Object {0} unlocked! Changes will take effect when the object is reselected.", editable.Name), "Unlock");
                }
            }
        }

        private void SelectManager_ObjectsDeselected(object sender, SelectionEventArgs e)
        {
            this.selection = null;
        }

        private void SelectManager_ObjectsSelected(object sender, SelectionEventArgs e)
        {
            this.selection = e.Selection;
        }
    }
}
