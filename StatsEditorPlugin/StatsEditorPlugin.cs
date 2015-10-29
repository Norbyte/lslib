using EoCPlugin;
using LSToolFramework;
using LSToolFramework.P4;
// using LSLib.Stats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace StatsEditorPlugin
{
    public class StatsEditorPlugin : IPlugin
    {
        public void Debug(string s)
        {
            System.Console.WriteLine(s);
            // MessageBox.Show(s, "Sample");
        }

        public override bool Init()
        {
            return true;
        }

        public override bool Start()
        {
            System.Console.WriteLine("StatsEditorPlugin.Start");
            /*using (StatsDataLoader loader = new StatsDataLoader("D:\\Dev\\DOS\\UnpackedData\\Public\\Main\\Stats\\Generated\\Data\\Armor.txt"))
            {
                StatsModuleDatabase db = new StatsModuleDatabase("TestMod");
                List<StatDefinition> items = loader.ReadAll();
                foreach (var item in items)
                {
                    if (db.Definitions.ContainsKey(item.Name))
                        System.Console.WriteLine("Duplicate definition:" + item.Name);

                    db.Definitions.Add(item.Name, item);
                }
                System.Console.Write(items.Count);
                
            }*/

            return true;
        }

        public override bool Stop()
        {
            Debug("StatsEditorPlugin.Stop");
            return true;
        }

        public override void OnModuleLoaded(object _param1, EventArgs _param2)
        {
            Debug("StatsEditorPlugin.OnModuleLoaded");
            string dataPath = ToolFramework.Instance.Configuration.GameDataPath;
        }

        public override void OnModuleUnloaded(object _param1, EventArgs _param2)
        {
            Debug("StatsEditorPlugin.OnModuleUnloaded");
        }
    }
}
