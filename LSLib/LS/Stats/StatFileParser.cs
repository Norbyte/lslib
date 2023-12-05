using LSLib.LS.Stats.StatParser;
using LSLib.LS.Story.GoalParser;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;

namespace LSLib.LS.Stats
{
    public class StatEntry
    {
        public string Name;
        public StatEntryType Type;
        public StatEntry BasedOn;
        public CodeLocation Location;
        public Dictionary<string, object> Properties = new Dictionary<string, object>();
        public Dictionary<string, CodeLocation> PropertyLocations = new Dictionary<string, CodeLocation>();
    }

    /// <summary>
    /// Holder for stat loader diagnostic codes.
    /// </summary>
    public class DiagnosticCode
    {
        /// <summary>
        /// Syntax error in stat file.
        /// </summary>
        public const string StatSyntaxError = "S00";
        /// <summary>
        /// Unable to determine type of stat declaration.
        /// (Either the type was not specified, or no handler exists for the specified type)
        /// </summary>
        public const string StatEntityTypeUnknown = "S01";
        /// <summary>
        /// The SkillType/StatusType specified was missing.
        /// </summary>
        public const string StatSubtypeMissing = "S02";
        /// <summary>
        /// The base class specified in the "Using" section does not exist.
        /// </summary>
        public const string StatBaseClassNotKnown = "S03";
        /// <summary>
        /// A stat declaration with the same key already exists.
        /// </summary>
        public const string StatNameDuplicate = "S04";
        /// <summary>
        /// The property is not supported by the current stat type.
        /// </summary>
        public const string StatPropertyUnsupported = "S05";
        /// <summary>
        /// Invalid property value.
        /// </summary>
        public const string StatPropertyValueInvalid = "S06";
        /// <summary>
        /// The stat declaration has no name property.
        /// </summary>
        public const string StatNameMissing = "S07";
    }

    public class StatLoadingError
    {
        public string Code;
        public string Message;
        public string Path;
        public Int32 Line;
        public string StatObjectName;
    }

    public class StatLoadingContext
    {
        public StatDefinitionRepository Definitions;
        public List<StatLoadingError> Errors = new List<StatLoadingError>();
        public Dictionary<string, Dictionary<string, StatDeclaration>> DeclarationsByType = new Dictionary<string, Dictionary<string, StatDeclaration>>();
        public Dictionary<string, Dictionary<string, StatDeclaration>> ResolvedDeclarationsByType = new Dictionary<string, Dictionary<string, StatDeclaration>>();
        public Dictionary<string, Dictionary<string, object>> GuidResources = new Dictionary<string, Dictionary<string, object>>();

        public void LogError(string code, string message, string path = null, int line = 0, string statObjectName = null)
        {
            Errors.Add(new StatLoadingError
            {
                Code = code,
                Message = message,
                Path = path,
                Line = line > 0 ? (line + 1) : 0,
                StatObjectName = statObjectName
            });
        }
    }

    class StatEntryReferenceResolver
    {
        private readonly StatLoadingContext Context;
        public bool AllowMappingErrors = false;

        private class BaseClassMapping
        {
            public StatDeclaration Declaration;
            public StatDeclaration BaseClass;
        }
        
        public StatEntryReferenceResolver(StatLoadingContext context)
        {
            Context = context;
        }

        public bool ResolveUsageRef(
            StatEntryType type,StatDeclaration declaration, 
            Dictionary<string, StatDeclaration> declarations,
            out StatDeclaration basedOn)
        {
            var props = declaration.Properties;
            var name = (string)props[type.NameProperty];
            if (type.BasedOnProperty != null && props.ContainsKey(type.BasedOnProperty))
            {
                var baseClass = (string)props[type.BasedOnProperty];

                if (declarations.TryGetValue(baseClass, out StatDeclaration baseDeclaration))
                {
                    basedOn = baseDeclaration;
                    return true;
                }
                else
                {
                    Context.LogError(DiagnosticCode.StatBaseClassNotKnown, $"Stats entry '{name}' references nonexistent base '{baseClass}'",
                        declaration.Location.FileName, declaration.Location.StartLine, name);
                    basedOn = null;
                    return false;
                }
            }

            basedOn = null;
            return true;
        }

        private void PropagateInheritedProperties(StatDeclaration parent, StatDeclaration descendant)
        {
            foreach (var prop in parent.Properties)
            {
                if (!descendant.Properties.ContainsKey(prop.Key))
                {
                    descendant.Properties[prop.Key] = prop.Value;
                    descendant.PropertyLocations[prop.Key] = parent.PropertyLocations[prop.Key];
                }
            }
        }

        private void PropagateInheritedProperties(List<BaseClassMapping> mappings)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.BaseClass != null)
                {
                    PropagateInheritedProperties(mapping.BaseClass, mapping.Declaration);
                }
            }
        }

        public Dictionary<string, StatDeclaration> ResolveUsageRefs(StatEntryType type, Dictionary<string, StatDeclaration> declarations)
        {
            var mappings = new List<BaseClassMapping>();
            var resolved = new Dictionary<string, StatDeclaration>();

            foreach (var declaration in declarations)
            {
                if (declaration.Value.WasInstantiated) continue;

                var succeeded = ResolveUsageRef(type, declaration.Value, declarations, out StatDeclaration baseClass);
                if (succeeded && baseClass != null)
                {
                    mappings.Add(new BaseClassMapping
                    {
                        Declaration = declaration.Value,
                        BaseClass = baseClass
                    });
                }

                if (succeeded || AllowMappingErrors)
                {
                    resolved.Add(declaration.Key, declaration.Value);
                }
            }

            PropagateInheritedProperties(mappings);

            return resolved;
        }
    }

    class StatLoaderReferenceValidator : IStatReferenceValidator
    {
        private readonly StatLoadingContext Context;

        public StatLoaderReferenceValidator(StatLoadingContext ctx)
        {
            Context = ctx;
        }

        public bool IsValidReference(string reference, string statType)
        {
            if (Context.DeclarationsByType.TryGetValue(statType, out var stats))
            {
                return stats.TryGetValue(reference, out var stat);
            }

            return false;
        }

        public bool IsValidGuidResource(string name, string resourceType)
        {
            if (Context.GuidResources.TryGetValue(resourceType, out var resources))
            {
                return resources.TryGetValue(name, out var resource);
            }

            return false;
        }
    }

    public class StatLoader
    {
        private readonly StatLoadingContext Context;
        private StatValueParserFactory ParserFactory;
        private StatLoaderReferenceValidator ReferenceValidator;

        public StatLoader(StatLoadingContext ctx)
        {
            Context = ctx;
            ReferenceValidator = new StatLoaderReferenceValidator(ctx);
            ParserFactory = new StatValueParserFactory(ReferenceValidator);
        }

        private List<StatDeclaration> ParseStatStream(string path, Stream stream)
        {
            var scanner = new StatScanner(path);
            scanner.SetSource(stream);
            var parser = new StatParser.StatParser(scanner);
            bool parsed = parser.Parse();
            if (!parsed)
            {
                var location = scanner.LastLocation();
                Context.LogError(DiagnosticCode.StatSyntaxError, $"Syntax error at or near line {location.StartLine}, column {location.StartColumn}", path, location.StartLine);
            }

            return parsed ? parser.GetDeclarations() : null;
        }

        private void AddDeclarations(string path, List<StatDeclaration> declarations)
        {
            foreach (var declaration in declarations)
            {
                // Fixup type
                if (!declaration.Properties.ContainsKey("EntityType"))
                {
                    Context.LogError(DiagnosticCode.StatEntityTypeUnknown, "Unable to determine type of stat declaration", declaration.Location.FileName, declaration.Location.StartLine);
                    continue;
                }
                
                var statType = declaration.Properties["EntityType"].ToString();

                if (!Context.Definitions.Types.TryGetValue(statType, out StatEntryType type))
                {
                    Context.LogError(DiagnosticCode.StatEntityTypeUnknown, $"No definition exists for stat type '{statType}'", declaration.Location.FileName, declaration.Location.StartLine);
                    continue;
                }

                if (!declaration.Properties.ContainsKey(type.NameProperty))
                {
                    Context.LogError(DiagnosticCode.StatNameMissing, $"Stat entry has no '{type.NameProperty}' property", declaration.Location.FileName, declaration.Location.StartLine);
                    continue;
                }
                
                Dictionary<String, StatDeclaration> declarationsByType;
                if (!Context.DeclarationsByType.TryGetValue(statType, out declarationsByType))
                {
                    declarationsByType = new Dictionary<string, StatDeclaration>();
                    Context.DeclarationsByType[statType] = declarationsByType;
                }

                // TODO - duplicate declaration check?
                var name = declaration.Properties[type.NameProperty].ToString();
                declarationsByType[name] = declaration;
            }
        }

        public void LoadStatsFromStream(string path, Stream stream)
        {
            var stats = ParseStatStream(path, stream);
            if (stats != null)
            {
                AddDeclarations(path, stats);
            }
        }

        public void ResolveUsageRef()
        {
            var resolver = new StatEntryReferenceResolver(Context);
            foreach (var type in Context.DeclarationsByType)
            {
                var typeDefn = Context.Definitions.Types[type.Key];
                Context.ResolvedDeclarationsByType[type.Key] = resolver.ResolveUsageRefs(typeDefn, type.Value);
            }
        }

        private object ParseProperty(StatEntryType type, string propertyName, object value, CodeLocation location,
            string declarationName)
        {
            if (!type.Fields.TryGetValue(propertyName, out StatField field))
            {
                Context.LogError(DiagnosticCode.StatPropertyUnsupported, $"Property '{propertyName}' is not supported on {type.Name} '{declarationName}'",
                    location?.FileName, location?.StartLine ?? 0, declarationName);
                return null;
            }

            bool succeeded = false;
            string errorText = null;
            object parsed;

            if (value is String && propertyName.Length + ((string)value).Length > 4085)
            {
                parsed = null;
                Context.LogError(DiagnosticCode.StatPropertyValueInvalid, $"{type.Name} '{declarationName}' has invalid {propertyName}: Line cannot be longer than 4095 characters",
                    location?.FileName, location?.StartLine ?? 0, declarationName);
            }
            else if (field.Type != "Passthrough")
            {
                var parser = field.GetParser(ParserFactory, Context.Definitions);
                parsed = parser.Parse((string)value, ref succeeded, ref errorText);
            }
            else
            {
                parsed = value;
                succeeded = true;
            }

            if (errorText != null)
            {
                if (value is string && ((string)value).Length > 500)
                {
                    Context.LogError(DiagnosticCode.StatPropertyValueInvalid, $"{type.Name} '{declarationName}' has invalid {propertyName}: {errorText}",
                        location?.FileName, location?.StartLine ?? 0, declarationName);
                }
                else
                {
                    Context.LogError(DiagnosticCode.StatPropertyValueInvalid, $"{type.Name} '{declarationName}' has invalid {propertyName}: '{value}' ({errorText})",
                        location?.FileName, location?.StartLine ?? 0, declarationName);
                }
            }

            if (succeeded)
            {
                return parsed;
            }
            else
            {
                return null;
            }
        }

        private StatEntry InstantiateEntry(StatEntryType type, string declarationName, StatDeclaration declaration)
        {
            return InstantiateEntryInternal(type, declarationName, declaration.Location,
                declaration.Properties, declaration.PropertyLocations);
        }

        private StatEntry InstantiateEntryInternal(StatEntryType type, string declarationName, 
            CodeLocation location, Dictionary<string, object> properties, Dictionary<string, CodeLocation> propertyLocations)
        {
            var entity = new StatEntry
            {
                Name = declarationName,
                Type = type,
                BasedOn = null, // FIXME
                Location = location,
                Properties = new Dictionary<string, object>(),
                PropertyLocations = propertyLocations
            };

            foreach (var property in properties)
            {
                if (property.Key == "EntityType")
                {
                    continue;
                }

                propertyLocations.TryGetValue(property.Key, out CodeLocation propLocation);
                var parsed = ParseProperty(type, property.Key, property.Value, propLocation, declarationName);
                if (parsed != null)
                {
                    entity.Properties.Add(property.Key, parsed);
                }
            }

            return entity;
        }

        public void InstantiateEntries()
        {
            foreach (var type in Context.ResolvedDeclarationsByType)
            {
                var typeDefn = Context.Definitions.Types[type.Key];
                foreach (var declaration in type.Value)
                {
                    if (!declaration.Value.WasInstantiated)
                    {
                        InstantiateEntry(typeDefn, declaration.Key, declaration.Value);
                        declaration.Value.WasInstantiated = true;
                    }
                }
            }
        }

        private void LoadGuidResources(Dictionary<string, object> guidResources, XmlNodeList nodes)
        {
            foreach (var node in nodes)
            {
                var attributes = (node as XmlElement).GetElementsByTagName("attribute");
                foreach (var attribute in attributes)
                {
                    var attr = attribute as XmlElement;
                    if (attr.GetAttribute("id") == "Name")
                    {
                        var name = attr.GetAttribute("value");
                        guidResources[name] = name;
                        break;
                    }
                }
            }
        }

        public void LoadGuidResources(XmlDocument doc, string typeName, string regionName)
        {
            Dictionary<string, object> guidResources;
            if (!Context.GuidResources.TryGetValue(typeName, out guidResources))
            {
                guidResources = new Dictionary<string, object>();
                Context.GuidResources[typeName] = guidResources;
            }

            var regions = doc.DocumentElement.GetElementsByTagName("region");
            foreach (var region in regions)
            {
                if ((region as XmlElement).GetAttribute("id") == regionName)
                {
                    var root = (region as XmlElement).GetElementsByTagName("node");
                    if (root.Count > 0)
                    {
                        var children = (root[0] as XmlElement).GetElementsByTagName("children");
                        if (children.Count > 0)
                        {
                            var resources = (children[0] as XmlElement).GetElementsByTagName("node");
                            LoadGuidResources(guidResources, resources);
                        }
                    }
                }
            }
        }

        public void LoadActionResources(XmlDocument doc)
        {
            LoadGuidResources(doc, "ActionResource", "ActionResourceDefinitions");
        }

        public void LoadActionResourceGroups(XmlDocument doc)
        {
            LoadGuidResources(doc, "ActionResourceGroup", "ActionResourceGroupDefinitions");
        }
    }

}
