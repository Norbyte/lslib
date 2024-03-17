using LSLib.Parser;

namespace LSLib.Stats.StatParser;

/// <summary>
/// List of stat properties
/// </summary>
public class StatDeclaration
{
    public CodeLocation? Location = null;
    public Dictionary<String, StatProperty> Properties = [];
    public bool WasValidated = false;
}

/// <summary>
/// A string property of a stat entry (Key/value pair)
/// </summary>
public class StatProperty(string key, object value, CodeLocation? location = null, CodeLocation? valueLocation = null)
{
    public string Key = key;
    public object Value = value;
    public CodeLocation? Location = location;
    public CodeLocation? ValueLocation = valueLocation;
}

/// <summary>
/// An element of collection of a stat entry (Key/value pair)
/// </summary>
public class StatElement(string collection, object value, CodeLocation? location = null)
{
    public string Collection = collection;
    public object Value = value;
    public CodeLocation? Location = location;
}
