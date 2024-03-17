namespace LSLib.Stats.Functors;

public class Requirement
{
    // Requirement negation ("Immobile" vs. "!Immobile").
    public bool Not = false;
    // Textual name of requirement
    public string RequirementName = string.Empty;
    // Integer requirement parameter
    public int IntParam = 0;
    // Tag name parameter ("Tag" requirement only)
    public string? TagParam = null;
}

public class Functor
{
    public string? TextKey = null;
    public string? Context = null;
    public object Condition;
    public FunctorAction Action;
}

public class FunctorAction
{
    public string Action = string.Empty;
    public List<string> Arguments = [];
    public int StartPos = 0;
    public int EndPos = 0;
}
