using LSLib.LS.Story.Compiler;

namespace LSLib.LS.Story.GoalParser;

/// <summary>
/// Goal node - contains everything from a goal file.
/// </summary>
public class ASTGoal
{
    // Facts in the INITSECTION part
    public List<ASTBaseFact> InitSection;
    // List of all production rules (including procs and queries) from the KBSECTION part
    public List<ASTRule> KBSection;
    // Ffacts in the EXITSECTION part
    public List<ASTBaseFact> ExitSection;
    // Names of parent goals (if any)
    public List<ASTParentTargetEdge> ParentTargetEdges;
    // Location of node in source code
    public CodeLocation Location;
}

/// <summary>
/// Name of a single parent target edge (i.e. parent goal name).
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
public class ASTParentTargetEdge
{
    // Location of node in source code
    public CodeLocation Location;
    // Parent goal name
    public String Goal;
}

/// <summary>
/// Osiris statement from the INIT or EXIT section.
/// </summary>
public class ASTBaseFact
{
    // Location of fact in source code
    public CodeLocation Location;
}

/// <summary>
/// Osiris fact statement from the INIT or EXIT section.
/// </summary>
public class ASTFact : ASTBaseFact
{
    // Name of database we're inserting into / deleting from
    public String Database;
    // Fact negation ("DB_Something(1)" vs. "NOT DB_Something(1)").
    public bool Not;
    // List of values in the fact tuple
    public List<ASTConstantValue> Elements;
}

/// <summary>
/// Osiris GoalCompleted statement from the INIT or EXIT section.
/// </summary>
public class ASTGoalCompletedFact : ASTBaseFact
{
}

/// <summary>
/// Describes a production rule in the KB section
/// </summary>
public class ASTRule
{
    // Location of rule in source code
    public CodeLocation Location;
    // Type of rule (if, proc or query)
    public RuleType Type;
    // Conditions/predicates
    public List<ASTCondition> Conditions;
    // Actions to execute on tuples that satisfy the conditions
    public List<ASTAction> Actions;
}

/// <summary>
/// Production rule condition/predicate.
/// </summary>
public class ASTCondition
{
    // Location of condition in source code
    public CodeLocation Location;
}

/// <summary>
/// "Function call-like" predicate - a div query, a user query or a database filter.
/// (i.e. "AND SomeFunc(1, 2)" or "AND NOT SomeFunc(1, 2)")
/// </summary>
public class ASTFuncCondition : ASTCondition
{
    // Query/Database name
    // (We don't know yet whether this is a query or a database - this info will only be
    //  available during phase2 parsing)
    public String Name;
    // Condition negation ("AND DB_Something(1)" vs. "AND NOT DB_Something(1)").
    public bool Not;
    // List of query parameters / database tuple columns
    public List<ASTRValue> Params;
}

/// <summary>
/// Predicate with a binary operator (i.e. "A >= B", "A == B", ...)
/// </summary>
public class ASTBinaryCondition : ASTCondition
{
    // Left-hand value
    public ASTRValue LValue;
    // Operator
    public RelOpType Op;
    // Right-hand value
    public ASTRValue RValue;
}

public class ASTAction
{
    // Location of action in source code
    public CodeLocation Location;
}

public class ASTGoalCompletedAction : ASTAction
{
}

/// <summary>
/// Statement in the THEN part of a rule.
/// This is either a builtin PROC call, user PROC call, or a database insert/delete operation.
/// </summary>
public class ASTStatement : ASTAction
{
    // Proc/Database name
    // (We don't know yet whether this is a PROC or a DB - this info will only be
    //  available during phase2 parsing)
    public String Name;
    // Statement negation ("DB_Something(1)" vs. "NOT DB_Something(1)").
    public bool Not;
    // List of PROC parameters / database tuple columns
    public List<ASTRValue> Params;
}

public class ASTRValue
{
    // Location of node in source code
    public CodeLocation Location;
}

/// <summary>
/// Constant scalar value.
/// </summary>
public class ASTConstantValue : ASTRValue
{
    // Type of value, if specified in the code.
    // (e.g. "(INT64)123")
    public String TypeName;
    // Internal type of the constant
    // This is not the same as the Osiris type; e.g. a value of type CHARACTERGUID
    // will be stored with a constant type of "Name". It also doesn't differentiate
    // between INT and INT64 as we don't know the exact Osiris type without contextual
    // type inference, which will happen in later stages.
    public IRConstantType Type;
    // Value of this constant if the type is Integer.
    public Int64 IntegerValue;
    // Value of this constant if the type is Float.
    public Single FloatValue;
    // Value of this constant if the type is String or Name.
    public String StringValue;
}

/// <summary>
/// Rule-local variable name.
/// (Any variable that begins with an underscore in the IF or THEN part of a rule)
/// </summary>
public class ASTLocalVar : ASTRValue
{
    // Type of variable, if specified in the code.
    // (e.g. "(ITEMGUID)_Var")
    public String Type;
    // Name of variable.
    public String Name;
}
