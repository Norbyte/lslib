using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LSLib.Stats
{
    abstract public class PropertyDefinition
    {
        private string name;
        public string Name { get { return name; } }

        public PropertyDefinition(string name)
        {
            this.name = name;
        }


        abstract public bool validate(string value);
    }


    public class IntPropertyDefinition : PropertyDefinition
    {
        public int MinValue = Int32.MinValue;
        public int MaxValue = Int32.MaxValue;

        public override bool validate(string value)
        {
            try
            {
                int intval = Convert.ToInt32(value);
                return (intval >= MinValue && intval <= MaxValue);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }


    public class FloatPropertyDefinition : PropertyDefinition
    {
        public float MinValue = float.MinValue;
        public float MaxValue = float.MaxValue;

        public override bool validate(string value)
        {
            try
            {
                float floatval = Convert.ToInt32(value);
                return (floatval >= MinValue && floatval <= MaxValue);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }


    public class StringPropertyDefinition : PropertyDefinition
    {
        public override bool validate(string value)
        {
            return true;
        }
    }
}
