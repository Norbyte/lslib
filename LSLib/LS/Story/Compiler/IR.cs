using LSLib.LS.Story.GoalParser;

namespace LSLib.LS.Story.Compiler;

/// <summary>
/// Parent class for IR (Intermediate Representation) references.
/// These are names that were passed on from the AST, but
/// may not be defined at the time of parsing.
/// </summary>
public abstract class IRReference<NameType, ReferencedType>
{
    public NameType Name;
    protected CompilationContext Context;

    public bool IsNull
    {
        get { return Name == null; }
    }

    public bool IsValid
    {
        get { return Name != null; }
    }

    public IRReference()
    {
    }

    public IRReference(NameType name)
    {
        Name = name;
    }

    public void Bind(CompilationContext context)
    {
        if (Context == null)
            Context = context;
        else
            throw new InvalidOperationException("Reference already bound to a compilation context!");
    }
    
    abstract public ReferencedType Resolve();
}

/// <summary>
/// Named reference to a story goal.
/// </summary>
public class IRGoalRef : IRReference<String, IRGoal>
{
    public IRGoalRef(String name)
        : base(name)
    {
    }

    public override IRGoal Resolve()
    {
        if (IsNull)
            return null;
        else
            return Context.LookupGoal(Name);
    }
}

/// <summary>
/// Named reference to a story symbol (proc, query, event).
/// </summary>
public class IRSymbolRef : IRReference<FunctionNameAndArity, FunctionSignature>
{
    public IRSymbolRef(FunctionNameAndArity name)
        : base(name)
    {
    }

    public override FunctionSignature Resolve()
    {
        if (IsNull)
            return null;
        else
            return Context.LookupSignature(Name);
    }
}

/// <summary>
/// Goal dependency edge from subgial to parent
/// </summary>
public class IRTargetEdge
{
    // Goal name
    public IRGoalRef Goal;
    // Location of code reference
    public CodeLocation Location;
}

/// <summary>
/// Goal node - contains everything from a goal file.
/// </summary>
public class IRGoal
{
    // Goal name (derived from filename)
    public String Name;
    // Facts in the INITSECTION part
    public List<IRFact> InitSection;
    // List of all production rules (including procs and queries) from the KBSECTION part
    public List<IRRule> KBSection;
    // Ffacts in the EXITSECTION part
    public List<IRFact> ExitSection;
    // Parent goals (if any)
    public List<IRTargetEdge> ParentTargetEdges;
    // Location of node in source code
    public CodeLocation Location;
}

/// <summary>
/// Osiris fact statement from the INIT or EXIT section.
/// </summary>
public class IRFact
{
    // Database we're inserting into / deleting from
    public IRSymbolRef Database;
    // Fact negation ("DB_Something(1)" vs. "NOT DB_Something(1)").
    public bool Not;
    // List of values in the fact tuple
    public List<IRConstant> Elements;
    // Goal that we're completing
    public IRGoal Goal;
    // Location of node in source code
    public CodeLocation Location;
}

/// <summary>
/// Describes a production rule in the KB section
/// </summary>
public class IRRule
{
    public IRGoal Goal;
    // Type of rule (if, proc or query)
    public RuleType Type;
    // Conditions/predicates
    public List<IRCondition> Conditions;
    // Actions to execute on tuples that satisfy the conditions
    public List<IRStatement> Actions;
    // Rule-local variables
    public List<IRRuleVariable> Variables;
    // Rule-local variables by name
    public Dictionary<String, IRRuleVariable> VariablesByName;
    // Location of node in source code
    public CodeLocation Location;

    public IRRuleVariable FindOrAddVariable(String name, ValueType type)
    {
        if (name.Length < 1 || name[0] != '_')
        {
            throw new ArgumentException("Local variable name must start with an underscore");
        }

        IRRuleVariable v = null;
        // Only resolve the variable if it has a name.
        // Unnamed variables are never resolved by name, and all references are assigned 
        // to a separate variable "slot"
        if (name.Length > 1)
        {
            VariablesByName.TryGetValue(name.ToLowerInvariant(), out v);
        }

        if (v == null)
        {
            // Allocate a new variable slot if no variable with the same name exists
            v = new IRRuleVariable
            {
                Index = Variables.Count,
                Name = name,
                Type = type,
                FirstBindingIndex = -1
            };

            Variables.Add(v);

            if (name.Length > 1)
            {
                VariablesByName.Add(name.ToLowerInvariant(), v);
            }
        }

        return v;
    }
}

/// <summary>
/// Rule-level local variable.
/// </summary>
public class IRRuleVariable
{
    // Index of the variable within the rule.
    // Indices start from zero.
    public Int32 Index;
    // Local name of the variable.
    // This is only used during compilation and is discarded
    // when emitting the final story file.
    public String Name;
    // Type of the rule variable
    public ValueType Type;
    // Index of condition that first bound this variable
    public Int32 FirstBindingIndex;
    // TODO - add inferred type marker!

    public bool IsUnused()
    {
        return Name.Length == 1;
    }
}

/// <summary>
/// Production rule condition/predicate.
/// </summary>
public class IRCondition
{
    // Number of columns in the output tuple of this condition.
    public Int32 TupleSize;
    // Location of node in source code
    public CodeLocation Location;
}

/// <summary>
/// "Function call-like" predicate - a div query, a user query or a database filter.
/// (i.e. "AND SomeFunc(1, 2)" or "AND NOT SomeFunc(1, 2)")
/// </summary>
public class IRFuncCondition : IRCondition
{
    // Query/Database name
    // (We don't know yet whether this is a query or a database - this info will only be
    //  available during phase2 parsing)
    public IRSymbolRef Func;
    // Condition negation ("AND DB_Something(1)" vs. "AND NOT DB_Something(1)").
    public bool Not;
    // List of query parameters / database tuple columns
    public List<IRValue> Params;
}

/// <summary>
/// Predicate with a binary operator (i.e. "A >= B", "A == B", ...)
/// </summary>
public class IRBinaryCondition : IRCondition
{
    // Left-hand value
    public IRValue LValue;
    // Operator
    public RelOpType Op;
    // Right-hand value
    public IRValue RValue;
}

/// <summary>
/// Statement in the THEN part of a rule.
/// This is either a builtin PROC call, user PROC call, a database insert/delete operation,
/// or a goal completion statement.
/// </summary>
public class IRStatement
{
    // Proc/Database name
    // (We don't know yet whether this is a PROC or a DB - this info will only be
    //  available during phase2 parsing)
    public IRSymbolRef Func;
    // Goal to complete
    // (Reference is empty if this statement doesn't trigger a goal completion)
    public IRGoal Goal;
    // Statement negation ("DB_Something(1)" vs. "NOT DB_Something(1)").
    public bool Not;
    // List of PROC parameters / database tuple columns
    public List<IRValue> Params;
    // Location of node in source code
    public CodeLocation Location;
}

public class IRValue
{
    // Type of variable, if specified in the code.
    // (e.g. "(ITEMGUID)_Var")
    public ValueType Type;
    // Location of node in source code
    public CodeLocation Location;
}

/// <summary>
/// Constant value type. This is the type we see during story
/// script parsing, which is not necessarily the same as the
/// Osiris type.
/// </summary>
public enum IRConstantType
{
    Unknown = 0,
    Integer = 1,
    Float = 2,
    String = 3,
    Name = 4
}

/// <summary>
/// Constant scalar value.
/// </summary>
public class IRConstant : IRValue
{
    // Internal type of the constant
    // This is not the same as the Osiris type; e.g. a value of type CHARACTERGUID
    // will be stored with a constant type of "Name". It also doesn't differentiate
    // between INT and INT64 as we don't know the exact Osiris type without contextual
    // type inference, which will happen in later stages.
    public IRConstantType ValueType;
    // Was the type info retrieved from the AST or inferred?
    public bool InferredType;
    // Value of this constant if the type is Integer.
    public Int64 IntegerValue;
    // Value of this constant if the type is Float.
    public Single FloatValue;
    // Value of this constant if the type is String or Name.
    public String StringValue;

    public override string ToString()
    {
        switch (ValueType)
        {
            case IRConstantType.Unknown: return "(unknown)";
            case IRConstantType.Integer: return IntegerValue.ToString();
            case IRConstantType.Float: return FloatValue.ToString();
            case IRConstantType.String: return "\"" + StringValue + "\"";
            case IRConstantType.Name: return StringValue;
            default: return "(unknown type)";
        }
    }
}

/// <summary>
/// Rule-local variable name.
/// (Any variable that begins with an underscore in the IF or THEN part of a rule)
/// </summary>
public class IRVariable : IRValue
{
    // Index of variable in the rule variable list
    public Int32 Index;
}
