using LSLib.Stats.Functors;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace LSLib.Stats;

public class StatEnumeration(string name)
{
    public readonly string Name = name;
    public readonly List<string> Values = [];
    public readonly Dictionary<string, int> ValueToIndexMap = [];

    public void AddItem(int index, string value)
    {
        if (Values.Count != index)
        {
            throw new Exception("Enumeration items must be added in order.");
        }

        Values.Add(value);

        // Some vanilla enums are bogus and contain names multiple times
        ValueToIndexMap.TryAdd(value, index);
    }

    public void AddItem(string value)
    {
        AddItem(Values.Count, value);
    }
}

public class StatField(string name, string type)
{
    public string Name = name;
    public string Type = type;
    public StatEnumeration? EnumType = null;
    public List<StatReferenceConstraint>? ReferenceTypes = null;

    private IStatValueValidator? Validator = null;

    public IStatValueValidator GetValidator(StatValueValidatorFactory factory, StatDefinitionRepository definitions)
    {
        Validator ??= factory.CreateValidator(this, definitions);
        return Validator;
    }
}

public class StatEntryType(string name, string nameProperty, string? basedOnProperty)
{
    public readonly string Name = name;
    public readonly string NameProperty = nameProperty;
    public readonly string? BasedOnProperty = basedOnProperty;
    public readonly Dictionary<string, StatField> Fields = [];
}

public class StatFunctorArgumentType(string name, string type)
{
    public string Name = name;
    public string Type = type;
}

public class StatFunctorType(string name, int requiredArgs, List<StatFunctorArgumentType> args)
{
    public string Name = name;
    public int RequiredArgs = requiredArgs;
    public List<StatFunctorArgumentType> Args = args;
}

public class StatDefinitionRepository
{
    public readonly Dictionary<string, StatEnumeration> Enumerations = [];
    public readonly Dictionary<string, StatEntryType> Types = [];
    public readonly Dictionary<string, StatFunctorType> Functors = [];
    public readonly Dictionary<string, StatFunctorType> Boosts = [];
    public readonly Dictionary<string, StatFunctorType> DescriptionParams = [];

    private StatField AddField(StatEntryType defn, string name, string typeName)
    {
        var field = new StatField(name, typeName);

        if (Enumerations.TryGetValue(typeName, out var enumType) && enumType.Values.Count > 0)
        {
            field.EnumType = enumType;
        }

        defn.Fields.Add(name, field);
        return field;
    }

    private void AddEnumeration(string name, List<string> labels)
    {
        var enumType = new StatEnumeration(name);
        foreach (var label in labels)
        {
            enumType.AddItem(label);
        }
        Enumerations.Add(name, enumType);
    }

    public void AddFunctor(Dictionary<string, StatFunctorType> dict, string name, int requiredArgs, List<string> argDescs)
    {
        var args = new List<StatFunctorArgumentType>();
        for (int i = 0; i < argDescs.Count; i += 2)
        {
            args.Add(new StatFunctorArgumentType(argDescs[i], argDescs[i + 1]));
        }

        AddFunctor(dict, name, requiredArgs, args);
    }

    public void AddFunctor(Dictionary<string, StatFunctorType> dict, string name, int requiredArgs, IEnumerable<StatFunctorArgumentType> args)
    {
        var functor = new StatFunctorType(name, requiredArgs, args.ToList());
        dict.Add(name, functor);
    }

    public void LoadCustomStatEntryType(XmlElement ele)
    {
        var entry = new StatEntryType(ele.GetAttribute("Name"), ele.GetAttribute("NameProperty"), null);
        Types.Add(entry.Name, entry);

        foreach (var field in ele.GetElementsByTagName("Field"))
        {
            var e = (XmlElement)field;
            AddField(entry, e.GetAttribute("Name"), e.GetAttribute("Type"));
        }
    }

    public void LoadCustomEnumeration(XmlElement ele)
    {
        var name = ele.GetAttribute("Name");
        var labels = new List<string>();

        foreach (var field in ele.GetElementsByTagName("Label"))
        {
            labels.Add(((XmlElement)field).InnerText);
        }

        AddEnumeration(name, labels);
    }

    public void LoadCustomFunction(XmlElement ele)
    {
        var name = ele.GetAttribute("Name");
        var type = ele.GetAttribute("Type");
        var requiredArgsStr = ele.GetAttribute("RequiredArgs");
        var requiredArgs = (requiredArgsStr == "") ? 0 : Int32.Parse(requiredArgsStr);
        var args = new List<string>();

        foreach (var arg in ele.GetElementsByTagName("Arg"))
        {
            var e = (XmlElement)arg;
            args.Add(e.GetAttribute("Name"));
            args.Add(e.GetAttribute("Type"));
        }

        switch (type)
        {
            case "Boost": AddFunctor(Boosts, name, requiredArgs, args); break;
            case "Functor": AddFunctor(Functors, name, requiredArgs, args); break;
            case "DescriptionParams": AddFunctor(DescriptionParams, name, requiredArgs, args); break;
            default: throw new InvalidDataException($"Unknown function type in definition file: {type}");
        }
    }

    public void LoadLSLibDefinitions(Stream stream)
    {
        var doc = new XmlDocument();
        doc.Load(stream);

        foreach (var node in doc.DocumentElement!.ChildNodes)
        {
            if (node is XmlElement element)
            {
                switch (element.Name)
                {
                    case "EntryType": LoadCustomStatEntryType(element); break;
                    case "Enumeration": LoadCustomEnumeration(element); break;
                    case "Function": LoadCustomFunction(element); break;
                    default: throw new InvalidDataException($"Unknown entry type in definition file: {element.Name}");
                }
            }
        }
    }

    public void LoadDefinitions(Stream stream)
    {
        StatEntryType? defn = null;
        string? line;

        using var reader = new StreamReader(stream);

        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0)
            {
                if (trimmed.StartsWith("modifier type "))
                {
                    var name = trimmed[15..^1];
                    defn = new StatEntryType(name, "Name", "Using");
                    Types.Add(defn.Name, defn);
                    AddField(defn, "Name", "FixedString");
                    var usingRef = AddField(defn, "Using", "StatReference");
                    usingRef.ReferenceTypes =
                    [
                        new StatReferenceConstraint
                        {
                            StatType = name
                        }
                    ];
                }
                else if (trimmed.StartsWith("modifier \""))
                {
                    var nameEnd = trimmed.IndexOf('"', 10);
                    var name = trimmed[10..nameEnd];
                    var typeName = trimmed.Substring(nameEnd + 3, trimmed.Length - nameEnd - 4);
                    AddField(defn!, name, typeName);
                }
            }
        }
    }

    public void LoadEnumerations(Stream stream)
    {
        StatEnumeration? curEnum = null;
        string? line;

        using var reader = new StreamReader(stream);
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0)
            {
                if (trimmed.StartsWith("valuelist "))
                {
                    var name = trimmed[11..^1];
                    curEnum = new StatEnumeration(name);
                    Enumerations.Add(curEnum.Name, curEnum);
                }
                else if (trimmed.StartsWith("value "))
                {
                    var label = trimmed[7..^1];
                    curEnum!.AddItem(label);
                }
            }
        }
    }
}
