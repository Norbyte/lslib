using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LSLib.Stats
{
    public class ArmorDefinition : StatDefinition
    {
        public ArmorDefinition(string name)
            : base(name)
        {
        }

        protected override Dictionary<string, PropertyDefinition> GetPropertyDefinitions()
        {
            return new Dictionary<string, PropertyDefinition>
            {
                {"asd", new PropertyDefinition("asd"){Name = "x"}
                }
            };
        }
    }
}
