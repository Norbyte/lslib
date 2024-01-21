using LSLib.LS.Story.GoalParser;
using QUT.Gppg;
using System.Text.RegularExpressions;

namespace LSLib.LS.Stats.StatParser;

/// <summary>
/// A collection of sub-stats.
/// </summary>
using StatCollection = List<object>;

/// <summary>
/// Declarations node - contains every declaration from the story header file.
/// </summary>
using StatDeclarations = List<StatDeclaration>;


public abstract class StatScanBase : AbstractScanner<object, CodeLocation>
{
    protected String fileName;

    public override CodeLocation yylloc { get; set; }
    
    protected virtual bool yywrap() { return true; }

    protected string MakeLiteral(string lit) => lit;

    protected string MakeString(string lit)
    {
        return MakeLiteral(Regex.Unescape(lit.Substring(1, lit.Length - 2)));
    }

    protected StatProperty MakeDataProperty(int startLine, int startCol, int endLine, int endCol, string lit)
    {
        var re = new Regex(@"data\s+""([^""]+)""\s+""(.*)""\s*", RegexOptions.CultureInvariant);
        var matches = re.Match(lit);
        if (!matches.Success)
        {
            throw new Exception("Stat data entry match error");
        }

        return new StatProperty
        {
            Key = matches.Groups[1].Value,
            Value = matches.Groups[2].Value,
            Location = new CodeLocation(null, startLine, startCol, endLine, endCol),
            ValueLocation = new CodeLocation(null, startLine, startCol + matches.Groups[2].Index, endLine, startCol + matches.Groups[2].Index + matches.Groups[2].Value.Length)
        };
    }
}

public partial class StatScanner
{
    public StatScanner(String fileName)
    {
        this.fileName = fileName;
    }

    public CodeLocation LastLocation()
    {
        return new CodeLocation(null, tokLin, tokCol, tokELin, tokECol);
    }
}

public partial class StatParser
{
    public StatParser(StatScanner scnr) : base(scnr)
    {
    }

    public StatDeclarations GetDeclarations()
    {
        return (StatDeclarations)CurrentSemanticValue;
    }

    private StatDeclarations MakeDeclarationList() => new StatDeclarations();

    private StatDeclarations AddDeclaration(object declarations, object declaration)
    {
        var decls = (StatDeclarations)declarations;
        decls.Add((StatDeclaration)declaration);
        return decls;
    }

    private StatDeclaration MakeDeclaration() => new StatDeclaration();

    private StatDeclaration MakeDeclaration(CodeLocation location) => new StatDeclaration()
    {
        Location = location
    };

    private StatDeclaration MakeDeclaration(CodeLocation location, StatProperty[] properties)
    {
        var decl = new StatDeclaration()
        {
            Location = location
        };
        foreach (var prop in properties)
        {
            AddProperty(decl, prop);
        }

        return decl;
    }

    private StatDeclaration MakeDeclaration(StatProperty[] properties)
    {
        return MakeDeclaration(null, properties);
    }

    private StatDeclaration MergeItemCombo(object comboNode, object resultNode)
    {
        var combo = (StatDeclaration)comboNode;
        var result = (StatDeclaration)resultNode;
        foreach (var kv in result.Properties)
        {
            if (kv.Key != "EntityType" && kv.Key != "Name")
            {
                combo.Properties[kv.Key] = kv.Value;
            }
        }

        return combo;
    }

    private StatDeclaration AddProperty(object declaration, object property)
    {
        var decl = (StatDeclaration)declaration;
        if (property is StatProperty prop)
        {
            decl.Properties[prop.Key] = prop;
        }
        else if (property is StatElement ele)
        {
            if (!decl.Properties.TryGetValue(ele.Collection, out prop))
            {
                prop = new StatProperty
                {
                    Key = ele.Collection,
                    Value = new StatCollection(),
                    Location = ele.Location
                };
                decl.Properties[ele.Collection] = prop;
            }

            (prop.Value as StatCollection).Add(ele.Value);
        }
        else if (property is StatDeclaration otherDecl)
        {
            foreach (var kv in otherDecl.Properties)
            {
                decl.Properties[kv.Key] = kv.Value;
            }
        }
        else
        {
            throw new Exception("Unknown property type");
        }

        return decl;
    }

    private StatProperty MakeProperty(object key, object value) => new StatProperty()
    {
        Key = (string)key,
        Value = (string)value
    };

    private StatProperty MakeProperty(String key, object value) => new StatProperty()
    {
        Key = key,
        Value = (string)value
    };

    private StatProperty MakeProperty(String key, String value) => new StatProperty()
    {
        Key = key,
        Value = value
    };

    private StatProperty MakeProperty(CodeLocation location, object key, object value) => new StatProperty()
    {
        Key = (string)key,
        Value = (string)value,
        Location = location
    };

    private StatProperty MakeProperty(CodeLocation location, String key, object value) => new StatProperty()
    {
        Key = key,
        Value = (string)value,
        Location = location
    };

    private StatProperty MakeProperty(CodeLocation location, String key, String value) => new StatProperty()
    {
        Key = key,
        Value = value,
        Location = location
    };

    private StatElement MakeElement(String key, object value)
    {
        return new StatElement()
        {
            Collection = key,
            Value = value
        };
    }

    private StatElement MakeElement(String key, object value, CodeLocation location)
    {
        return new StatElement()
        {
            Location = location,
            Collection = key,
            Value = value
        };
    }

    private StatCollection MakeCollection() => new List<object>();

    private StatCollection AddElement(object collection, object element)
    {
        var coll = (StatCollection)collection;
        var ele = (string)element;
        coll.Add(ele);

        return coll;
    }

    private string Unwrap(object node) => (string)node;
}