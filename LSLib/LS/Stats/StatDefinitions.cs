using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace LSLib.LS.Stats
{
    public class StatEnumeration
    {
        public readonly string Name;
        public readonly List<string> Values;
        public readonly Dictionary<string, int> ValueToIndexMap;

        public StatEnumeration(string name)
        {
            Name = name;
            Values = new List<string>();
            ValueToIndexMap = new Dictionary<string, int>();
        }

        public void AddItem(int index, string value)
        {
            if (Values.Count != index)
            {
                throw new Exception("Enumeration items must be added in order.");
            }

            Values.Add(value);
            ValueToIndexMap.Add(value, index);
        }
    }

    public class StatField
    {
        public string Name;
        public string Type;
        public StatEnumeration EnumType;
        public List<StatReferenceConstraint> ReferenceTypes;

        private IStatValueParser parser;

        public IStatValueParser GetParser(StatValueParserFactory factory, StatDefinitionRepository definitions)
        {
            if (parser == null)
            {
                parser = factory.CreateParser(this, definitions);
            }

            return parser;
        }
    }

    public class StatSubtypeDefinition
    {
        public readonly StatTypeDefinition Type;
        public readonly string Name;
        public readonly Dictionary<string, StatField> Fields;
        public readonly Dictionary<string, StatSubtypeDefinition> SubObjects;

        public StatSubtypeDefinition(StatTypeDefinition type, string name)
        {
            Type = type;
            Name = name;
            Fields = new Dictionary<string, StatField>();
            SubObjects = new Dictionary<string, StatSubtypeDefinition>();
        }
    }

    public class StatTypeDefinition
    {
        public readonly string Name;
        public readonly string SubtypeProperty;
        public string NameProperty;
        public string BaseClassProperty;
        public readonly Dictionary<string, StatSubtypeDefinition> Subtypes;

        public bool CanInstantiate
        {
            get
            {
                return NameProperty != null;
            }
        }

        public StatTypeDefinition(string name, string subtypeProperty)
        {
            Name = name;
            SubtypeProperty = subtypeProperty;
            Subtypes = new Dictionary<string, StatSubtypeDefinition>();
        }
    }

    public class StatDefinitionRepository
    {
        // Version of modified Enumerations.xml and StatObjectDefinitions.sod we expect
        public const string CustomizationsVersion = "1";

        public readonly Dictionary<string, StatEnumeration> Enumerations = new Dictionary<string, StatEnumeration>();
        public readonly Dictionary<string, StatTypeDefinition> Definitions = new Dictionary<string, StatTypeDefinition>();

        private void AddField(StatTypeDefinition definition, StatSubtypeDefinition subtype, XElement field)
        {
            if (field.Attribute("export_name").Value == "")
            {
                return;
            }

            var fieldName = field.Attribute("export_name").Value;
            var typeName = field.Attribute("type").Value;
            StatEnumeration enumeration = null;
            List<StatReferenceConstraint> referenceConstraints = null;

            switch (typeName)
            {
                case "Enumeration":
                case "EnumerationList":
                    var enumName = field.Attribute("enumeration_type_name").Value;
                    enumeration = Enumerations[enumName];
                    break;

                case "Name":
                    if (definition.NameProperty == null)
                    {
                        definition.NameProperty = fieldName;
                    }
                    else if (definition.NameProperty != fieldName)
                    {
                        throw new Exception($"Conflicting Name property for type '{definition.Name}': First seen using '{definition.NameProperty}', now seen using '{fieldName}'.");
                    }
                    break;

                case "BaseClass":
                    if (definition.BaseClassProperty == null)
                    {
                        definition.BaseClassProperty = fieldName;
                    }
                    else if (definition.BaseClassProperty != fieldName)
                    {
                        throw new Exception($"Conflicting BaseClass for type '{definition.Name}': First seen using '{definition.BaseClassProperty}', now seen using '{fieldName}'.");
                    }
                    break;

                case "StatReference":
                case "StatReferences":
                    referenceConstraints = new List<StatReferenceConstraint>();
                    var descriptions = field.Element("stat_descriptions");
                    if (descriptions == null)
                    {
                        throw new Exception("Field of type 'StatReference' must have a list of stat types in the <stat_descriptions> node");
                    }

                    var descs = descriptions.Elements("description");
                    foreach (var desc in descs)
                    {
                        var constraint = new StatReferenceConstraint
                        {
                            StatType = desc.Attribute("stat_type").Value,
                            StatSubtype = desc.Attribute("stat_subtype")?.Value ?? null
                        };
                        referenceConstraints.Add(constraint);
                    }

                    break;

                case "Boolean":
                case "Integer":
                case "Float":
                case "String":
                case "TranslatedString":
                case "RootTemplate":
                case "Comment":
                case "Color":
                case "Requirements":
                case "Properties":
                case "Conditions":
                case "Passthrough":
                case "UUID":
                    break;

                default:
                    throw new Exception($"Unsupported stat field type: '{typeName}'");
            }

            var statField = new StatField
            {
                Name = fieldName,
                Type = typeName,
                EnumType = enumeration,
                ReferenceTypes = referenceConstraints
            };
            subtype.Fields.Add(fieldName, statField);

            if (typeName == "TranslatedString")
            {
                var translatedKeyRefField = new StatField
                {
                    Name = fieldName + "Ref",
                    Type = typeName,
                    EnumType = enumeration
                };
                subtype.Fields.Add(fieldName + "Ref", translatedKeyRefField);
            }
        }

        private void AddSubtype(StatTypeDefinition definition, string subtypeName, IEnumerable<XElement> fields)
        {
            var subtype = new StatSubtypeDefinition(definition, subtypeName);

            foreach (var field in fields)
            {
                AddField(definition, subtype, field);
            }
            
            definition.Subtypes.Add(subtypeName, subtype);
        }

        private void AddDefinition(XElement defn)
        {
            var name = defn.Attribute("name").Value;
            var parentName = defn.Attribute("export_type")?.Value ?? name;

            if (!Definitions.TryGetValue(parentName, out StatTypeDefinition definition))
            {
                var subtypeProperty = defn.Attribute("subtype_property")?.Value ?? null;
                definition = new StatTypeDefinition(parentName, subtypeProperty);
                Definitions.Add(parentName, definition);
            }

            var fields = defn.Element("field_definitions").Elements("field_definition");
            AddSubtype(definition, name, fields);
        }

        private void AddEnumeration(XElement enumEle)
        {
            var name = enumEle.Attribute("name").Value;
            if (Enumerations.ContainsKey(name))
            {
                throw new Exception($"Enumeration '{name}' defined multiple times!");
            }

            var enumType = new StatEnumeration(name);
            
            var items = enumEle.Element("items").Elements("item");

            foreach (var item in items)
            {
                var index = Int32.Parse(item.Attribute("index").Value);
                var value = item.Attribute("value").Value;
                enumType.AddItem(index, value);
            }

            Enumerations.Add(name, enumType);
        }

        public void LoadDefinitions(string definitionsPath)
        {
            // NOTE: This function uses a modified version of StatObjectDefinitions.sod as there
            // are too many deviations in the vanilla .sod file from the actual .txt format, and
            // vital stat fields are sometimes missing.
            // The changes required are:
            // 1) Add "subtype_property" attribute to StatusData and SkillData types
            // 2) Fix export_type of ExtraData to Data
            // 3) Add SkillType and StatusType fields to StatusData and SkillData types
            // 4) Add type="Passthrough" fields for subobjects where required
            // 5) Adjusted fields to use proper enumeration labels in many places
            // etc etc.

            var root = XElement.Load(definitionsPath);
            var customizationVer = root.Attribute("lslib_customizations")?.Value;
            if (customizationVer == null)
            {
                throw new Exception("Can only load StatObjectDefinitions.sod with LSLib-specific modifications");
            }
            else if (customizationVer != CustomizationsVersion)
            {
                throw new Exception($"Needs StatObjectDefinitions.sod with customization version '{CustomizationsVersion}'; got version '{customizationVer}'");
            }

            var defnRoot = root.Element("stat_object_definitions");
            var defns = defnRoot.Elements("stat_object_definition");

            foreach (var defn in defns)
            {
                AddDefinition(defn);
            }
        }

        public void LoadEnumerations(string enumsPath)
        {
            var root = XElement.Load(enumsPath);
            var customizationVer = root.Attribute("lslib_customizations")?.Value;
            if (customizationVer == null)
            {
                throw new Exception("Can only load Enumerations.xml with LSLib-specific modifications");
            }
            else if (customizationVer != CustomizationsVersion)
            {
                throw new Exception($"Needs Enumerations.xml with customization version '{CustomizationsVersion}'; got version '{customizationVer}'");
            }

            var defnRoot = root.Element("enumerations");
            var enums = defnRoot.Elements("enumeration");

            foreach (var enum_ in enums)
            {
                AddEnumeration(enum_);
            }
        }
    }
}
