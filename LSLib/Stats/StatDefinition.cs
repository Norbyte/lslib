using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LSLib.Stats
{
    public class PropertyValidationException : Exception
    {
        public PropertyValidationException(string message)
            : base(message)
        { }
    }


    abstract public class StatDefinition
    {
        public Dictionary<string, string> properties = new Dictionary<string, string>();

        public string Name { get; set; }
        public StatDefinition Parent { get; set; }
        public Dictionary<string, string> Properties
        { 
            get { return properties; }
        }

        public StatDefinition(string name)
        {
            this.Name = name;
        }


        public void SetProperty(string name, string value)
        {
            var defn = GetPropertyDefinition(name);
            if (defn != null)
            {
                if (!defn.validate(value))
                    throw new PropertyValidationException(String.Format("Invalid value for property '{0}': '{1}'", name, value));
            }
            else
                throw new PropertyValidationException(String.Format("Property '{0}' has no definition", name));

            properties[name] = value;
        }


        public string GetProperty(string name)
        {
            if (Properties.ContainsKey(name))
                return Properties[name];
            else if (Parent != null)
                return Parent.GetProperty(name);
            else
                return null;
        }


        abstract protected Dictionary<string, PropertyDefinition> GetPropertyDefinitions();


        protected PropertyDefinition GetPropertyDefinition(string property)
        {
            var types = GetPropertyDefinitions();
            PropertyDefinition defn = null;
            types.TryGetValue(property, out defn);
            return defn;
        }
    }
}
