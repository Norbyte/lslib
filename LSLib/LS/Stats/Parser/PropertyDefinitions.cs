namespace LSLib.LS.Stats.Properties;

public class Requirement
{
    // Requirement negation ("Immobile" vs. "!Immobile").
    public bool Not;
    // Textual name of requirement
    public string RequirementName;
    // Integer requirement parameter
    public int IntParam;
    // Tag name parameter ("Tag" requirement only)
    public string TagParam;
}

public class Property
{
    public string TextKey;
    public string Context;
    public object Condition;
    public PropertyAction Action;
}

public class PropertyAction
{
    public string Action;
    public List<string> Arguments;
    public int StartPos;
    public int EndPos;
}

public enum ConditionOperator
{
    And,
    Or
};

public class Condition
{
    public bool Not;
}

public class UnaryCondition : Condition
{
    public string ConditionType;
    public string Argument;
}

public class BinaryCondition : Condition
{
    public Condition Left;
    public Condition Right;
    public ConditionOperator Operator;
}
