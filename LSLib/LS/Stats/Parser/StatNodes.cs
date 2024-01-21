using LSLib.LS.Story.GoalParser;

namespace LSLib.LS.Stats.StatParser;

/// <summary>
/// List of stat properties
/// </summary>
public class StatDeclaration
{
    public CodeLocation Location;
    public Dictionary<String, StatProperty> Properties = [];
    public bool WasValidated = false;
}

/// <summary>
/// A string property of a stat entry (Key/value pair)
/// </summary>
public class StatProperty
{
    public String Key;
    public object Value;
    public CodeLocation Location;
    public CodeLocation ValueLocation;
}

/// <summary>
/// An element of collection of a stat entry (Key/value pair)
/// </summary>
public class StatElement
{
    public String Collection;
    public object Value;
    public CodeLocation Location;
}
