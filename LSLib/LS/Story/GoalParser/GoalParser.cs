using LSLib.LS.Story.Compiler;
using System.Globalization;
using System.Text.RegularExpressions;
using QUT.Gppg;

namespace LSLib.LS.Story.GoalParser;

/// <summary>
/// Parameter list of a statement in the THEN part of a rule.
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
using ASTStatementParamList = List<ASTRValue>;

/// <summary>
/// List of parent goals.
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
using ASTParentTargetEdgeList = List<ASTParentTargetEdge>;

/// <summary>
/// List of facts in an INIT or EXIT section.
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
using ASTFactList = List<ASTBaseFact>;

/// <summary>
/// List of scalar values in a fact tuple
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
using ASTFactElementList = List<ASTConstantValue>;

/// <summary>
/// List of production rules in the KB section
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
using ASTRuleList = List<ASTRule>;

/// <summary>
/// List of conditions/predicates in a production rule
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
using ASTConditionList = List<ASTCondition>;

/// <summary>
/// Condition query parameter / database tuple column list
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
using ASTConditionParamList = List<ASTRValue>;

/// <summary>
/// List of actions in the THEN part of a rule
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
using ASTActionList = List<ASTAction>;


internal class ParserConstants
{
    public static CultureInfo ParserCulture = new CultureInfo("en-US");
}

public class CodeLocation : IMerge<CodeLocation>
{
    private string fileName;
    private int startLine;   // start line
    private int startColumn; // start column
    private int endLine;     // end line
    private int endColumn;   // end column

    /// <summary>
    /// The line at which the text span starts.
    /// </summary>
    public string FileName { get { return fileName; } }

    /// <summary>
    /// The line at which the text span starts.
    /// </summary>
    public int StartLine { get { return startLine; } }

    /// <summary>
    /// The column at which the text span starts.
    /// </summary>
    public int StartColumn { get { return startColumn; } }

    /// <summary>
    /// The line on which the text span ends.
    /// </summary>
    public int EndLine { get { return endLine; } }

    /// <summary>
    /// The column of the first character
    /// beyond the end of the text span.
    /// </summary>
    public int EndColumn { get { return endColumn; } }

    /// <summary>
    /// Default no-arg constructor.
    /// </summary>
    public CodeLocation() { }

    /// <summary>
    /// Constructor for text-span with given start and end.
    /// </summary>
    /// <param name="sl">start line</param>
    /// <param name="sc">start column</param>
    /// <param name="el">end line </param>
    /// <param name="ec">end column</param>
    public CodeLocation(string fl, int sl, int sc, int el, int ec)
    {
        fileName = fl;
        startLine = sl;
        startColumn = sc;
        endLine = el;
        endColumn = ec;
    }

    /// <summary>
    /// Create a text location which spans from the
    /// start of "this" to the end of the argument "last"
    /// </summary>
    /// <param name="last">The last location in the result span</param>
    /// <returns>The merged span</returns>
    public CodeLocation Merge(CodeLocation last)
    {
        return new CodeLocation(this.fileName, this.startLine, this.startColumn, last.endLine, last.endColumn);
    }
}

public abstract class GoalScanBase : AbstractScanner<Object, CodeLocation>
{
    protected String fileName;

    public override CodeLocation yylloc { get; set; }

    protected virtual bool yywrap() { return true; }

    protected string MakeLiteral(string lit) => lit;

    protected string MakeString(string lit)
    {
        return MakeLiteral(Regex.Unescape(lit.Substring(1, lit.Length - 2)));
    }
}

public sealed partial class GoalScanner : GoalScanBase
{
    public GoalScanner(String fileName)
    {
        this.fileName = fileName;
    }

    public CodeLocation LastLocation()
    {
        return new CodeLocation(null, tokLin, tokCol, tokELin, tokECol);
    }
}

public partial class GoalParser
{
    public GoalParser(GoalScanner scnr) : base(scnr)
    {
    }

    public ASTGoal GetGoal()
    {
        return (ASTGoal)CurrentSemanticValue;
    }

    private ASTGoal MakeGoal(CodeLocation location, object version, object subGoalCombiner, object initSection,
        object kbSection, object exitSection, object parentTargetEdges) => new ASTGoal()
    {
        // TODO verison, SGC
        InitSection = (ASTFactList)initSection,
        KBSection = (ASTRuleList)kbSection,
        ExitSection = (ASTFactList)exitSection,
        ParentTargetEdges = (ASTParentTargetEdgeList)parentTargetEdges,
        Location = location
    };

    private ASTParentTargetEdgeList MakeParentTargetEdgeList() => new ASTParentTargetEdgeList();

    private ASTParentTargetEdgeList MakeParentTargetEdgeList(object parentTargetEdgeList, object edge)
    {
        var edges = (ASTParentTargetEdgeList)parentTargetEdgeList;
        edges.Add((ASTParentTargetEdge)edge);
        return edges;
    }

    private ASTParentTargetEdge MakeParentTargetEdge(CodeLocation location, object goal) => new ASTParentTargetEdge()
    {
        Location = location,
        Goal = (string)goal
    };

    private ASTFactList MakeFactList() => new ASTFactList();
    
    private ASTFactList MakeFactList(object factList, object fact)
    {
        var facts = (ASTFactList)factList;
        facts.Add((ASTBaseFact)fact);
        return facts;
    }

    private ASTFact MakeNotFact(CodeLocation location, object fact)
    {
        var factStmt = (ASTFact)fact;
        factStmt.Location = location;
        factStmt.Not = true;
        return factStmt;
    }

    private ASTFact MakeFactStatement(CodeLocation location, object database, object elements) => new ASTFact()
    {
        Location = location,
        Database = (string)database,
        Not = false,
        Elements = (ASTFactElementList)elements
    };

    private ASTGoalCompletedFact MakeGoalCompletedFact(CodeLocation location) => new ASTGoalCompletedFact
    {
        Location = location
    };

    private ASTFactElementList MakeFactElementList() => new ASTFactElementList();

    private ASTFactElementList MakeFactElementList(object element)
    {
        var elements = new ASTFactElementList();
        elements.Add((ASTConstantValue)element);
        return elements;
    }

    private ASTFactElementList MakeFactElementList(object elementList, object element)
    {
        var elements = (ASTFactElementList)elementList;
        elements.Add((ASTConstantValue)element);
        return elements;
    }

    private ASTRuleList MakeRuleList() => new ASTRuleList();

    private ASTRuleList MakeRuleList(object ruleList, object rule)
    {
        var rules = (ASTRuleList)ruleList;
        rules.Add((ASTRule)rule);
        return rules;
    }

    private ASTRule MakeRule(CodeLocation location, object ruleType, object conditions, object actions) => new ASTRule()
    {
        Location = location,
        Type = (RuleType)ruleType,
        Conditions = (ASTConditionList)conditions,
        Actions = (ASTActionList)actions
    };

    private RuleType MakeRuleType(RuleType type) => type;

    private ASTConditionList MakeConditionList() => new ASTConditionList();

    private ASTConditionList MakeConditionList(object condition)
    {
        var conditions = new ASTConditionList
        {
            (ASTCondition)condition
        };
        return conditions;
    }

    private ASTConditionList MakeConditionList(object conditionList, object condition)
    {
        var conditions = (ASTConditionList)conditionList;
        conditions.Add((ASTCondition)condition);
        return conditions;
    }

    private ASTFuncCondition MakeFuncCondition(CodeLocation location, object name, object paramList, bool not) => new ASTFuncCondition()
    {
        Location = location,
        Name = (string)name,
        Not = not,
        Params = (ASTConditionParamList)paramList
    };

    private ASTFuncCondition MakeObjectFuncCondition(CodeLocation location, object thisValue, object name, object paramList, bool not)
    {
        var condParams = (ASTConditionParamList)paramList;
        condParams.Insert(0, (ASTRValue)thisValue);
        return new ASTFuncCondition()
        {
            Location = location,
            Name = (string)name,
            Not = not,
            Params = condParams
        };
    }

    private ASTBinaryCondition MakeNegatedBinaryCondition(CodeLocation location, object lvalue, object op, object rvalue)
    {
        var cond = MakeBinaryCondition(location, lvalue, op, rvalue);
        switch (cond.Op)
        {
            case RelOpType.Less: cond.Op = RelOpType.GreaterOrEqual; break;
            case RelOpType.LessOrEqual: cond.Op = RelOpType.Greater; break;
            case RelOpType.Greater: cond.Op = RelOpType.LessOrEqual; break;
            case RelOpType.GreaterOrEqual: cond.Op = RelOpType.Less; break;
            case RelOpType.Equal: cond.Op = RelOpType.NotEqual; break;
            case RelOpType.NotEqual: cond.Op = RelOpType.Equal; break;
            default: throw new InvalidOperationException("Cannot negate unknown binary operator");
        }

        return cond;
    }

    private ASTBinaryCondition MakeBinaryCondition(CodeLocation location, object lvalue, object op, object rvalue) => new ASTBinaryCondition()
    {
        Location = location,
        LValue = (ASTRValue)lvalue,
        Op = (RelOpType)op,
        RValue = (ASTRValue)rvalue
    };

    private ASTConditionParamList MakeConditionParamList() => new ASTConditionParamList();

    private ASTConditionParamList MakeConditionParamList(object param)
    {
        var list = new ASTConditionParamList
        {
            (ASTRValue)param
        };
        return list;
    }

    private ASTConditionParamList MakeConditionParamList(object list, object param)
    {
        var conditionParamList = (ASTConditionParamList)list;
        conditionParamList.Add((ASTRValue)param);
        return conditionParamList;
    }

    private RelOpType MakeOperator(RelOpType op) => op;

    private ASTActionList MakeActionList() => new ASTActionList();

    private ASTActionList MakeActionList(object actionList, object action)
    {
        var actions = (ASTActionList)actionList;
        actions.Add((ASTAction)action);
        return actions;
    }

    private ASTAction MakeGoalCompletedAction(CodeLocation location) => new ASTGoalCompletedAction
    {
        Location = location
    };

    private ASTStatement MakeActionStatement(CodeLocation location, object name, object paramList, bool not) => new ASTStatement
    {
        Location = location,
        Name = (string)name,
        Not = not,
        Params = (ASTStatementParamList)paramList
    };

    private ASTStatement MakeActionStatement(CodeLocation location, object thisValue, object name, object paramList, bool not)
    {
        var stmt = new ASTStatement
        {
            Location = location,
            Name = (string)name,
            Not = not,
            Params = (ASTStatementParamList)paramList
        };
        stmt.Params.Insert(0, (ASTRValue)thisValue);
        return stmt;
    }

    private ASTStatementParamList MakeActionParamList() => new ASTStatementParamList();

    private ASTStatementParamList MakeActionParamList(object param)
    {
        var list = new ASTStatementParamList
        {
            (ASTRValue)param
        };
        return list;
    }

    private ASTStatementParamList MakeActionParamList(object list, object param)
    {
        var actionParamList = (ASTStatementParamList)list;
        actionParamList.Add((ASTRValue)param);
        return actionParamList;
    }

    private ASTLocalVar MakeLocalVar(CodeLocation location, object varName) => new ASTLocalVar()
    {
        Location = location,
        Name = (string)varName
    };

    private ASTLocalVar MakeLocalVar(CodeLocation location, object typeName, object varName) => new ASTLocalVar()
    {
        Location = location,
        Type = (string)typeName,
        Name = (string)varName
    };

    private ASTConstantValue MakeTypedConstant(CodeLocation location, object typeName, object constant)
    {
        var c = (ASTConstantValue)constant;
        return new ASTConstantValue()
        {
            Location = location,
            TypeName = (string)typeName,
            Type = c.Type,
            StringValue = c.StringValue,
            FloatValue = c.FloatValue,
            IntegerValue = c.IntegerValue,
        };
    }

    private ASTConstantValue MakeConstGuidString(CodeLocation location, object val) => new ASTConstantValue()
    {
        Location = location,
        Type = IRConstantType.Name,
        StringValue = (string)val
    };

    private ASTConstantValue MakeConstString(CodeLocation location, object val) => new ASTConstantValue()
    {
        Location = location,
        Type = IRConstantType.String,
        StringValue = (string)val
    };

    private ASTConstantValue MakeConstInteger(CodeLocation location, object val) => new ASTConstantValue()
    {
        Location = location,
        Type = IRConstantType.Integer,
        IntegerValue = Int64.Parse((string)val, ParserConstants.ParserCulture.NumberFormat)
    };

    private ASTConstantValue MakeConstFloat(CodeLocation location, object val) => new ASTConstantValue()
    {
        Location = location,
        Type = IRConstantType.Float,
        FloatValue = Single.Parse((string)val, ParserConstants.ParserCulture.NumberFormat)
    };
}