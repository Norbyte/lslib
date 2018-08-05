using System;
using System.Collections.Generic;

namespace LSLib.LS.Story.Parser
{
    /// <summary>
    /// Base class for all AST nodes.
    /// (This doesn't do anything meaningful, it is needed only to 
    /// provide the GPPG parser a semantic value base class.)
    /// </summary>
    public class ASTNode
    {
    }

    /// <summary>
    /// Goal node - contains everything from a goal file.
    /// </summary>
    public class ASTGoal : ASTNode
    {
        // Facts in the INITSECTION part
        public List<ASTFact> InitSection;
        // List of all production rules (including procs and queries) from the KBSECTION part
        public List<ASTRule> KBSection;
        // Ffacts in the EXITSECTION part
        public List<ASTFact> ExitSection;
        // Names of parent goals (if any)
        public List<String> ParentTargetEdges;
    }

    /// <summary>
    /// List of parent goals.
    /// This is discarded during parsing and does not appear in the final AST.
    /// </summary>
    public class ASTParentTargetEdgeList : ASTNode
    {
        public List<String> TargetEdges = new List<String>();
    }

    /// <summary>
    /// Name of a single parent target edge (i.e. parent goal name).
    /// This is discarded during parsing and does not appear in the final AST.
    /// </summary>
    public class ASTParentTargetEdge : ASTNode
    {
        public String Goal;
    }

    /// <summary>
    /// List of facts in an INIT or EXIT section.
    /// This is discarded during parsing and does not appear in the final AST.
    /// </summary>
    public class ASTFactList : ASTNode
    {
        public List<ASTFact> Facts = new List<ASTFact>();
    }

    /// <summary>
    /// Osiris fact statement from the INIT or EXIT section.
    /// </summary>
    public class ASTFact : ASTNode
    {
        // Name of database we're inserting into / deleting from
        public String Database;
        // Fact negation ("DB_Something(1)" vs. "NOT DB_Something(1)").
        public bool Not;
        // List of values in the fact tuple
        public List<ASTConstantValue> Elements;
    }

    /// <summary>
    /// List of scalar values in a fact tuple
    /// This is discarded during parsing and does not appear in the final AST.
    /// </summary>
    public class ASTFactElementList : ASTNode
    {
        public List<ASTConstantValue> Elements = new List<ASTConstantValue>();
    }

    /// <summary>
    /// List of production rules in the KB section
    /// This is discarded during parsing and does not appear in the final AST.
    /// </summary>
    public class ASTRuleList : ASTNode
    {
        public List<ASTRule> Rules = new List<ASTRule>();
    }

    /// <summary>
    /// Describes a production rule in the KB section
    /// </summary>
    public class ASTRule : ASTNode
    {
        // Type of rule (if, proc or query)
        public RuleType Type;
        // Conditions/predicates
        public List<ASTCondition> Conditions;
        // Actions to execute on tuples that satisfy the conditions
        public List<ASTAction> Actions;
    }

    /// <summary>
    /// Type of rule (if, proc or query)
    /// This is discarded during parsing and does not appear in the final AST.
    /// </summary>
    public class ASTRuleType : ASTNode
    {
        public RuleType Type;
    }

    /// <summary>
    /// List of conditions/predicates in a production rule
    /// This is discarded during parsing and does not appear in the final AST.
    /// </summary>
    public class ASTConditionList : ASTNode
    {
        public List<ASTCondition> Conditions = new List<ASTCondition>();
    }

    /// <summary>
    /// Production rule condition/predicate.
    /// </summary>
    public class ASTCondition : ASTNode
    {
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

    /// <summary>
    /// Condition query parameter / database tuple column list
    /// This is discarded during parsing and does not appear in the final AST.
    /// </summary>
    public class ASTConditionParamList : ASTNode
    {
        public List<ASTRValue> Params = new List<ASTRValue>();
    }

    /// <summary>
    /// Binary predicate operator
    /// This is discarded during parsing and does not appear in the final AST.
    /// </summary>
    public class ASTOperator : ASTNode
    {
        public RelOpType Op;
    }

    /// <summary>
    /// List of actions in the THEN part of a rule
    /// This is discarded during parsing and does not appear in the final AST.
    /// </summary>
    public class ASTActionList : ASTNode
    {
        public List<ASTAction> Actions = new List<ASTAction>();
    }

    public class ASTAction : ASTNode
    {
    }
    
    public class ASTGoalCompletedAction : ASTAction
    {
    }

    /// <summary>
    /// Parameter list of a statement in the THEN part of a rule.
    /// This is discarded during parsing and does not appear in the final AST.
    /// </summary>
    public class ASTStatementParamList : ASTNode
    {
        public List<ASTRValue> Params = new List<ASTRValue>();
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

    public class ASTRValue : ASTNode
    {
    }

    public enum ASTConstantType
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
        public ASTConstantType Type;
        // Value of this constant of the type is Integer.
        public Int64 IntegerValue;
        // Value of this constant of the type is Float.
        public Single FloatValue;
        // Value of this constant of the type is String or Name.
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

    /// <summary>
    /// String literal from lexing stage (yytext).
    /// This is discarded during parsing and does not appear in the final AST.
    /// </summary>
    public class ASTLiteral : ASTNode
    {
        public String Literal;
    }
}
