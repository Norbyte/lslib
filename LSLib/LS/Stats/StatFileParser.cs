using LSLib.LS.Stats.StatParser;
using LSLib.LS.Story.GoalParser;
using System.Xml;

namespace LSLib.LS.Stats;

public class StatEntry
{
    public string Name;
    public StatEntryType Type;
    public StatEntry BasedOn;
    public CodeLocation Location;
    public Dictionary<string, object> Properties = [];
    public Dictionary<string, CodeLocation> PropertyLocations = [];
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
    public CodeLocation Location;
    public List<PropertyDiagnosticContext> Contexts;
}

public class StatLoadingContext
{
    public StatDefinitionRepository Definitions;
    public List<StatLoadingError> Errors = [];
    public Dictionary<string, Dictionary<string, StatDeclaration>> DeclarationsByType = [];
    public Dictionary<string, Dictionary<string, StatDeclaration>> ResolvedDeclarationsByType = [];
    public Dictionary<string, Dictionary<string, object>> GuidResources = [];
    public readonly HashSet<string> ObjectCategories = [];

    public void LogError(string code, string message, CodeLocation location = null, 
        List<PropertyDiagnosticContext> contexts = null)
    {
        Errors.Add(new StatLoadingError
        {
            Code = code,
            Message = message,
            Location = location,
            Contexts = contexts
        });
    }
}

class StatEntryReferenceResolver(StatLoadingContext context)
{
    public bool AllowMappingErrors = false;

    private class BaseClassMapping
    {
        public StatDeclaration Declaration;
        public StatDeclaration BaseClass;
    }

    public bool ResolveUsageRef(
        StatEntryType type,StatDeclaration declaration, 
        Dictionary<string, StatDeclaration> declarations,
        out StatDeclaration basedOn)
    {
        var props = declaration.Properties;
        var name = (string)props[type.NameProperty].Value;
        if (type.BasedOnProperty != null && props.TryGetValue(type.BasedOnProperty, out StatProperty prop))
        {
            var baseClass = (string)prop.Value;

            if (declarations.TryGetValue(baseClass, out StatDeclaration baseDeclaration))
            {
                basedOn = baseDeclaration;
                return true;
            }
            else
            {
                context.LogError(DiagnosticCode.StatBaseClassNotKnown, $"Stats entry '{name}' references nonexistent base '{baseClass}'",
                    declaration.Location);
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
            if (!descendant.Properties.ContainsKey(prop.Key)
                // Only propagate types that are required to determine properties of stats entry subtypes
                && (prop.Key == "SpellType" || prop.Key == "StatusType"))
            {
                descendant.Properties[prop.Key] = prop.Value;
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

    private void ResolveObjectCategories(StatDeclaration declaration)
    {
        if (declaration.Properties.TryGetValue("ObjectCategory", out var prop))
        {
            foreach (var category in ((string)prop.Value).Split(';'))
            {
                if (category.Length > 0)
                {
                    context.ObjectCategories.Add(category);
                }
            }
        }
    }

    public Dictionary<string, StatDeclaration> ResolveUsageRefs(StatEntryType type, Dictionary<string, StatDeclaration> declarations)
    {
        var mappings = new List<BaseClassMapping>();
        var resolved = new Dictionary<string, StatDeclaration>();

        foreach (var declaration in declarations)
        {
            if (declaration.Value.WasValidated) continue;

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
                ResolveObjectCategories(declaration.Value);
            }
        }

        PropagateInheritedProperties(mappings);

        return resolved;
    }
}

class StatLoaderReferenceValidator(StatLoadingContext ctx) : IStatReferenceValidator
{
    public bool IsValidReference(string reference, string statType)
    {
        if (statType == "ObjectCategory")
        {
            return ctx.ObjectCategories.Contains(reference);
        }
        else if (ctx.DeclarationsByType.TryGetValue(statType, out var stats))
        {
            return stats.TryGetValue(reference, out _);
        }

        return false;
    }

    public bool IsValidGuidResource(string name, string resourceType)
    {
        if (ctx.GuidResources.TryGetValue(resourceType, out var resources))
        {
            return resources.TryGetValue(name, out _);
        }

        return false;
    }
}

public interface IPropertyValidator
{
    public void ValidateEntry(StatEntryType type, string declarationName, StatDeclaration declaration, PropertyDiagnosticContainer errors);
}

public class StatLoader : IPropertyValidator
{
    private readonly StatLoadingContext Context;
    private readonly StatValueValidatorFactory ValidatorFactory;
    private readonly StatLoaderReferenceValidator ReferenceValidator;
    public readonly DiagnosticContext DiagContext;

    public StatLoader(StatLoadingContext ctx)
    {
        Context = ctx;
        ReferenceValidator = new(ctx);
        ValidatorFactory = new(ReferenceValidator, this);
        DiagContext = new();
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
            Context.LogError(DiagnosticCode.StatSyntaxError, $"Syntax error at or near line {location.StartLine}, column {location.StartColumn}", location);
        }

        return parsed ? parser.GetDeclarations() : null;
    }

    private void AddDeclarations(List<StatDeclaration> declarations)
    {
        foreach (var declaration in declarations)
        {
            // Fixup type
            if (!declaration.Properties.ContainsKey("EntityType"))
            {
                Context.LogError(DiagnosticCode.StatEntityTypeUnknown, "Unable to determine type of stat declaration", declaration.Location);
                continue;
            }
            
            var statType = declaration.Properties["EntityType"].Value.ToString();

            if (!Context.Definitions.Types.TryGetValue(statType, out StatEntryType type))
            {
                Context.LogError(DiagnosticCode.StatEntityTypeUnknown, $"No definition exists for stat type '{statType}'", declaration.Location);
                continue;
            }

            if (!declaration.Properties.ContainsKey(type.NameProperty))
            {
                Context.LogError(DiagnosticCode.StatNameMissing, $"Stat entry has no '{type.NameProperty}' property", declaration.Location);
                continue;
            }

            if (!Context.DeclarationsByType.TryGetValue(statType, out Dictionary<string, StatDeclaration> declarationsByType))
            {
                declarationsByType = [];
                Context.DeclarationsByType[statType] = declarationsByType;
            }

            // TODO - duplicate declaration check?
            var name = declaration.Properties[type.NameProperty].Value.ToString();
            declarationsByType[name] = declaration;
        }
    }

    public void LoadStatsFromStream(string path, Stream stream)
    {
        var stats = ParseStatStream(path, stream);
        if (stats != null)
        {
            AddDeclarations(stats);
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

    public void ValidateProperty(StatEntryType type, StatProperty property,
        string declarationName, PropertyDiagnosticContainer errors)
    {
        if (!type.Fields.TryGetValue(property.Key, out StatField field))
        {
            errors.Add($"Property '{property.Key}' is not supported on type {type.Name}");
            return;
        }

        if (property.Value is String && property.Key.Length + ((string)property.Value).Length > 4085)
        {
            errors.Add("Line cannot be longer than 4095 characters");
        }
        else if (field.Type != "Passthrough")
        {
            var validator = field.GetValidator(ValidatorFactory, Context.Definitions);
            validator.Validate(DiagContext, property.ValueLocation, property.Value, errors);
        }
    }

    public void ValidateEntry(StatEntryType type, string declarationName, StatDeclaration declaration, PropertyDiagnosticContainer entryErrors)
    {
        var errors = new PropertyDiagnosticContainer();
        foreach (var property in declaration.Properties)
        {
            if (property.Key == "EntityType")
            {
                continue;
            }

            var lastPropertySpan = DiagContext.PropertyValueSpan;
            DiagContext.PropertyValueSpan = property.Value.ValueLocation;
            ValidateProperty(type, property.Value, declarationName, errors);
            DiagContext.PropertyValueSpan = lastPropertySpan;

            if (!errors.Empty)
            {
                errors.AddContext(PropertyDiagnosticContextType.Property, property.Key, property.Value.ValueLocation ?? property.Value.Location);
                errors.MergeInto(entryErrors);
                errors.Clear();
            }
        }
    }

    public void ValidateEntries()
    {
        var errors = new PropertyDiagnosticContainer();
        foreach (var type in Context.ResolvedDeclarationsByType)
        {
            var typeDefn = Context.Definitions.Types[type.Key];
            foreach (var declaration in type.Value)
            {
                if (!declaration.Value.WasValidated)
                {
                    ValidateEntry(typeDefn, declaration.Key, declaration.Value, errors);
                    declaration.Value.WasValidated = true;

                    if (!errors.Empty)
                    {
                        errors.AddContext(PropertyDiagnosticContextType.Entry, declaration.Key, declaration.Value.Location);
                        errors.MergeInto(Context, declaration.Key);
                        errors.Clear();
                    }
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
        if (!Context.GuidResources.TryGetValue(typeName, out Dictionary<string, object> guidResources))
        {
            guidResources = [];
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
