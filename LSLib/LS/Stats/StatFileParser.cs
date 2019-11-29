using LSLib.LS.Stats.StatParser;
using LSLib.LS.Story.GoalParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LSLib.LS.Stats
{
    public class StatEntity
    {
        public string Name;
        public StatSubtypeDefinition Type;
        public StatEntity BaseClass;
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

        public void LogError(string code, string message, string path = null, int line = 0, string statObjectName = null)
        {
            Errors.Add(new StatLoadingError
            {
                Code = code,
                Message = message,
                Path = path,
                Line = line,
                StatObjectName = statObjectName
            });
        }
    }

    class StatBaseClassResolver
    {
        private readonly StatLoadingContext Context;
        public bool AllowMappingErrors = false;

        private class BaseClassMapping
        {
            public StatDeclaration Declaration;
            public StatDeclaration BaseClass;
        }
        
        public StatBaseClassResolver(StatLoadingContext context)
        {
            Context = context;
        }

        public bool ResolveBaseClass(
            StatTypeDefinition definition, StatDeclaration declaration, 
            Dictionary<string, StatDeclaration> declarations,
            out StatDeclaration baseClassDeclaration)
        {
            var props = declaration.Properties;
            var name = (string)props[definition.NameProperty];
            if (definition.BaseClassProperty != null && props.ContainsKey(definition.BaseClassProperty))
            {
                var baseClass = (string)props[definition.BaseClassProperty];

                if (declarations.TryGetValue(baseClass, out StatDeclaration baseDeclaration))
                {
                    baseClassDeclaration = baseDeclaration;
                    return true;
                }
                else
                {
                    Context.LogError(DiagnosticCode.StatBaseClassNotKnown, $"Stat declaration '{name}' references nonexistent base class '{baseClass}'",
                        declaration.Location.FileName, declaration.Location.StartLine, name);
                    baseClassDeclaration = null;
                    return false;
                }
            }

            baseClassDeclaration = null;
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

        public Dictionary<string, StatDeclaration> ResolveBaseClasses(StatTypeDefinition definition, Dictionary<string, StatDeclaration> declarations)
        {
            var mappings = new List<BaseClassMapping>();
            var resolved = new Dictionary<string, StatDeclaration>();

            foreach (var declaration in declarations)
            {
                var succeeded = ResolveBaseClass(definition, declaration.Value, declarations, out StatDeclaration baseClass);
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

        public bool IsValidReference(string reference, string statType, string statSubtype)
        {
            if (Context.DeclarationsByType.TryGetValue(statType, out var stats))
            {
                if (stats.TryGetValue(reference, out var stat))
                {
                    if (statSubtype == null)
                    {
                        return true;
                    }
                    else
                    {
                        var subtypeProperty = Context.Definitions.Definitions[statType].SubtypeProperty;
                        if (subtypeProperty == null)
                        {
                            throw new Exception($"Reference constraint found for stat type '{statType}' that has no subtype.");
                        }

                        var subtype = (string)stat.Properties[subtypeProperty];
                        if (statSubtype == subtype)
                        {
                            return true;
                        }
                    }
                }
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
                if (statType == "CraftingStations")
                {
                    statType = "CraftingStationsItemComboPreviewData";
                }
                if (statType == "ObjectCategories")
                {
                    statType = "ObjectCategoriesItemComboPreviewData";
                }

                if (!Context.Definitions.Definitions.TryGetValue(statType, out StatTypeDefinition definition))
                {
                    Context.LogError(DiagnosticCode.StatEntityTypeUnknown, $"No definition exists for stat type '{statType}'", declaration.Location.FileName, declaration.Location.StartLine);
                    continue;
                }

                if (!declaration.Properties.ContainsKey(definition.NameProperty))
                {
                    Context.LogError(DiagnosticCode.StatNameMissing, $"Stat entry has no '{definition.NameProperty}' property", declaration.Location.FileName, declaration.Location.StartLine);
                    continue;
                }
                
                Dictionary<String, StatDeclaration> declarationsByType;
                if (!Context.DeclarationsByType.TryGetValue(statType, out declarationsByType))
                {
                    declarationsByType = new Dictionary<string, StatDeclaration>();
                    Context.DeclarationsByType[statType] = declarationsByType;
                }

                // TODO - duplicate declaration check?
                var name = declaration.Properties[definition.NameProperty].ToString();
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

        public void ResolveBaseClasses()
        {
            foreach (var type in Context.DeclarationsByType)
            {
                var resolver = new StatBaseClassResolver(Context);
                var definition = Context.Definitions.Definitions[type.Key];
                Context.ResolvedDeclarationsByType[type.Key] = resolver.ResolveBaseClasses(definition, type.Value);
            }
        }

        private StatSubtypeDefinition FindSubtype(StatTypeDefinition type, string declarationName, StatDeclaration declaration)
        {
            if (type.SubtypeProperty == null)
            {
                return type.Subtypes.Values.First();
            }

            if (declaration.Properties.TryGetValue(type.SubtypeProperty, out object subtypeName))
            {
                var name = (string)subtypeName;
                if (type.Subtypes.TryGetValue(name, out StatSubtypeDefinition subtype))
                {
                    return subtype;
                }
                else
                {
                    Context.LogError(DiagnosticCode.StatSubtypeMissing, $"Stat declaration '{declarationName}' references unknown subtype '{name}'", 
                        declaration.Location.FileName, declaration.Location.StartLine);
                    return null;
                }
            }
            else
            {
                Context.LogError(DiagnosticCode.StatSubtypeMissing, $"Stat declaration '{declarationName}' is missing subtype property '{type.SubtypeProperty}'",
                    declaration.Location.FileName, declaration.Location.StartLine);
                return null;
            }
        }

        private object ParseProperty(StatSubtypeDefinition subtype, string propertyName, object value, CodeLocation location,
            string declarationName)
        {
            if (!subtype.Fields.TryGetValue(propertyName, out StatField field))
            {
                Context.LogError(DiagnosticCode.StatPropertyUnsupported, $"Property '{propertyName}' is not supported on {subtype.Name} '{declarationName}'",
                    location?.FileName, location?.StartLine ?? 0, declarationName);
                return null;
            }

            bool succeeded = false;
            string errorText = null;
            object parsed;

            if (field.Type != "Passthrough")
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
                Context.LogError(DiagnosticCode.StatPropertyValueInvalid, $"{subtype.Name} '{declarationName}' has invalid {propertyName}: '{value}' ({errorText})",
                    location?.FileName, location?.StartLine ?? 0, declarationName);
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

        private StatEntity InstantiateEntity(StatSubtypeDefinition subtype, string declarationName, StatDeclaration declaration)
        {
            return InstantiateEntityInternal(subtype, declarationName, declaration.Location,
                declaration.Properties, declaration.PropertyLocations);
        }

        private StatEntity InstantiateEntityInternal(StatSubtypeDefinition subtype, string declarationName, 
            CodeLocation location, Dictionary<string, object> properties, Dictionary<string, CodeLocation> propertyLocations)
        {
            var entity = new StatEntity
            {
                Name = declarationName,
                Type = subtype,
                BaseClass = null, // FIXME
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
                var parsed = ParseProperty(subtype, property.Key, property.Value, propLocation, declarationName);
                if (parsed != null)
                {
                    entity.Properties.Add(property.Key, parsed);
                }
            }

            return entity;
        }

        public void InstantiateEntities()
        {
            foreach (var type in Context.ResolvedDeclarationsByType)
            {
                var definition = Context.Definitions.Definitions[type.Key];
                foreach (var declaration in type.Value)
                {
                    var subtype = FindSubtype(definition, declaration.Key, declaration.Value);
                    if (subtype != null)
                    {
                        InstantiateEntity(subtype, declaration.Key, declaration.Value);
                    }
                }
            }
        }
    }

}
