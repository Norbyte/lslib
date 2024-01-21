
namespace LSLib.LS.Story.Compiler;

public class DatabaseDebugInfo
{
    // ID of database in generated story file
    public UInt32 Id;
    public String Name;
    public List<UInt32> ParamTypes;
}

public class ActionDebugInfo
{
    // Location of action in source file
    public UInt32 Line;
}

public class GoalDebugInfo
{
    // ID of goal in generated story file
    public UInt32 Id;
    // Goal name
    public String Name;
    // Absolute path of goal source file
    public String Path;
    // Actions in INIT section
    public List<ActionDebugInfo> InitActions;
    // Actions in EXIT section
    public List<ActionDebugInfo> ExitActions;
}

public class RuleVariableDebugInfo
{
    // Index of rule variable in local tuple
    public UInt32 Index;
    // Name of rule variable
    public String Name;
    // Type ID of rule variable
    public UInt32 Type;
    // Is the variable slot unused? (i.e. not bound to a physical column)
    public bool Unused;
}

public class RuleDebugInfo
{
    // Local index of rule
    // (this is not stored in the story file and is only used by the debugger)
    public UInt32 Id;
    // ID of parent goal node
    public UInt32 GoalId;
    // Generated rule name (usually the name of the first condition)
    public String Name;
    // Rule local variables
    public List<RuleVariableDebugInfo> Variables;
    // Actions in THEN-part
    public List<ActionDebugInfo> Actions;
    // Line number of the beginning of the "IF" section
    public UInt32 ConditionsStartLine;
    // Line number of the end of the "IF" section
    public UInt32 ConditionsEndLine;
    // Line number of the beginning of the "THEN" section
    public UInt32 ActionsStartLine;
    // Line number of the end of the "THEN" section
    public UInt32 ActionsEndLine;
}

public class NodeDebugInfo
{
    // ID of node in generated story file
    public UInt32 Id;
    // Index of parent rule
    public UInt32 RuleId;
    // Location of action in source file
    public Int32 Line;
    // Local tuple to rule variable index mappings
    public Dictionary<Int32, Int32> ColumnToVariableMaps;
    // ID of associated database node
    public UInt32 DatabaseId;
    // Name of node
    public String Name;
    // Type of node
    public Node.Type Type;
    // ID of left parent node
    public UInt32 ParentNodeId;
    // Function (query, proc, etc.) attached to this node
    public FunctionNameAndArity FunctionName;
}

public class FunctionParamDebugInfo
{
    // Intrinsic type ID
    public UInt32 TypeId;
    // Name of parameter
    public String Name;
    // Is an out param (ie. return value)?
    public bool Out;
}

public class FunctionDebugInfo
{
    // Name of function
    public String Name;
    // Type of node
    public List<FunctionParamDebugInfo> Params;
    // Function type ID
    public UInt32 TypeId;
}

public class StoryDebugInfo
{
    /// <summary>
    /// Story debug info format version. Increment each time the format changes.
    /// </summary>
    public const UInt32 CurrentVersion = 2;

    public UInt32 Version;
    public Dictionary<UInt32, DatabaseDebugInfo> Databases = new Dictionary<UInt32, DatabaseDebugInfo>();
    public Dictionary<UInt32, GoalDebugInfo> Goals = new Dictionary<UInt32, GoalDebugInfo>();
    public Dictionary<UInt32, RuleDebugInfo> Rules = new Dictionary<UInt32, RuleDebugInfo>();
    public Dictionary<UInt32, NodeDebugInfo> Nodes = new Dictionary<UInt32, NodeDebugInfo>();
    public Dictionary<FunctionNameAndArity, FunctionDebugInfo> Functions = new Dictionary<FunctionNameAndArity, FunctionDebugInfo>();
}
