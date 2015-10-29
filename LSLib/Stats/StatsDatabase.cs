using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LSLib.Stats
{
    /// <summary>
    /// Contains all stat entries that were loaded for a module.
    /// </summary>
    public class StatsModuleDatabase
    {
        private string module;
        public string Module
        {
            get { return module; }
        }

        private Dictionary<string, StatDefinition> definitions = new Dictionary<string,StatDefinition>();
        public Dictionary<string, StatDefinition> Definitions
        {
            get { return definitions; }
        }


        public StatsModuleDatabase(string module)
        {
            this.module = module;
        }


    }



    /// <summary>
    /// Contains all stat entries that were loaded for a module.
    /// </summary>
    public class StatsDatabase
    {
        private List<StatsModuleDatabase> modules = new List<StatsModuleDatabase>();
        public List<StatsModuleDatabase> Modules
        {
            get { return Modules; }
        }


        public StatDefinition GetDefinition(string name)
        {
            StatDefinition defn = null;
            foreach (var db in modules)
            {
                if (db.Definitions.TryGetValue(name, out defn))
                    break;
            }

            return defn;
        }
    }


}
