using LSLib.LS.Story.Compiler;
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace LSLib.LS.Story.GoalParser
{
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

    public abstract class GoalScanBase : AbstractScanner<ASTNode, CodeLocation>
    {
        protected String fileName;

        protected virtual bool yywrap() { return true; }

        protected ASTLiteral MakeLiteral(string lit) => new ASTLiteral()
        {
            Literal = lit
        };

        protected ASTLiteral MakeString(string lit)
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
    }

    public partial class GoalParser
    {
        public GoalParser(GoalScanner scnr) : base(scnr)
        {
        }

        public ASTGoal GetGoal()
        {
            return CurrentSemanticValue as ASTGoal;
        }

        private ASTGoal MakeGoal(ASTNode version, ASTNode subGoalCombiner, ASTNode initSection,
            ASTNode kbSection, ASTNode exitSection, ASTNode parentTargetEdges) => new ASTGoal()
        {
            // TODO verison, SGC
            InitSection = (initSection as ASTFactList).Facts,
            KBSection = (kbSection as ASTRuleList).Rules,
            ExitSection = (exitSection as ASTFactList).Facts,
            ParentTargetEdges = (parentTargetEdges as ASTParentTargetEdgeList).TargetEdges
        };

        private ASTParentTargetEdgeList MakeParentTargetEdgeList() => new ASTParentTargetEdgeList();

        private ASTParentTargetEdgeList MakeParentTargetEdgeList(ASTNode parentTargetEdgeList, ASTNode edge)
        {
            var edges = parentTargetEdgeList as ASTParentTargetEdgeList;
            edges.TargetEdges.Add(edge as ASTParentTargetEdge);
            return edges;
        }

        private ASTParentTargetEdge MakeParentTargetEdge(CodeLocation location, ASTNode goal) => new ASTParentTargetEdge()
        {
            Location = location,
            Goal = (goal as ASTLiteral).Literal
        };

        private ASTFactList MakeFactList() => new ASTFactList();
        
        private ASTFactList MakeFactList(ASTNode factList, ASTNode fact)
        {
            var facts = factList as ASTFactList;
            facts.Facts.Add(fact as ASTFact);
            return facts;
        }

        private ASTFact MakeNotFact(CodeLocation location, ASTNode fact)
        {
            var factStmt = fact as ASTFact;
            factStmt.Location = location;
            factStmt.Not = true;
            return factStmt;
        }

        private ASTFact MakeFactStatement(CodeLocation location, ASTNode database, ASTNode elements) => new ASTFact()
        {
            Location = location,
            Database = (database as ASTLiteral).Literal,
            Not = false,
            Elements = (elements as ASTFactElementList).Elements
        };

        private ASTFactElementList MakeFactElementList() => new ASTFactElementList();

        private ASTFactElementList MakeFactElementList(ASTNode element)
        {
            var elements = new ASTFactElementList();
            elements.Elements.Add(element as ASTConstantValue);
            return elements;
        }

        private ASTFactElementList MakeFactElementList(ASTNode elementList, ASTNode element)
        {
            var elements = elementList as ASTFactElementList;
            elements.Elements.Add(element as ASTConstantValue);
            return elements;
        }

        private ASTRuleList MakeRuleList() => new ASTRuleList();

        private ASTRuleList MakeRuleList(ASTNode ruleList, ASTNode rule)
        {
            var rules = ruleList as ASTRuleList;
            rules.Rules.Add(rule as ASTRule);
            return rules;
        }

        private ASTRule MakeRule(CodeLocation location, ASTNode ruleType, ASTNode conditions, ASTNode actions) => new ASTRule()
        {
            Location = location,
            Type = (ruleType as ASTRuleType).Type,
            Conditions = (conditions as ASTConditionList).Conditions,
            Actions = (actions as ASTActionList).Actions
        };

        private ASTRuleType MakeRuleType(RuleType type) => new ASTRuleType()
        {
            Type = type
        };

        private ASTConditionList MakeConditionList() => new ASTConditionList();

        private ASTConditionList MakeConditionList(ASTNode condition)
        {
            var conditions = new ASTConditionList();
            conditions.Conditions.Add(condition as ASTCondition);
            return conditions;
        }

        private ASTConditionList MakeConditionList(ASTNode conditionList, ASTNode condition)
        {
            var conditions = conditionList as ASTConditionList;
            conditions.Conditions.Add(condition as ASTCondition);
            return conditions;
        }

        private ASTFuncCondition MakeFuncCondition(CodeLocation location, ASTNode name, ASTNode paramList, bool not) => new ASTFuncCondition()
        {
            Location = location,
            Name = (name as ASTLiteral).Literal,
            Not = not,
            Params = (paramList as ASTConditionParamList).Params
        };

        private ASTFuncCondition MakeObjectFuncCondition(CodeLocation location, ASTNode thisValue, ASTNode name, ASTNode paramList, bool not)
        {
            var condParams = paramList as ASTConditionParamList;
            condParams.Params.Insert(0, thisValue as ASTRValue);
            return new ASTFuncCondition()
            {
                Location = location,
                Name = (name as ASTLiteral).Literal,
                Not = not,
                Params = condParams.Params
            };
        }

        private ASTBinaryCondition MakeNegatedBinaryCondition(CodeLocation location, ASTNode lvalue, ASTNode op, ASTNode rvalue)
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

        private ASTBinaryCondition MakeBinaryCondition(CodeLocation location, ASTNode lvalue, ASTNode op, ASTNode rvalue) => new ASTBinaryCondition()
        {
            Location = location,
            LValue = lvalue as ASTRValue,
            Op = (op as ASTOperator).Op,
            RValue = rvalue as ASTRValue
        };

        private ASTConditionParamList MakeConditionParamList() => new ASTConditionParamList();

        private ASTConditionParamList MakeConditionParamList(ASTNode param)
        {
            var list = new ASTConditionParamList();
            list.Params.Add(param as ASTRValue);
            return list;
        }

        private ASTConditionParamList MakeConditionParamList(ASTNode list, ASTNode param)
        {
            var conditionParamList = list as ASTConditionParamList;
            conditionParamList.Params.Add(param as ASTRValue);
            return conditionParamList;
        }

        private ASTOperator MakeOperator(RelOpType op) => new ASTOperator()
        {
            Op = op
        };

        private ASTActionList MakeActionList() => new ASTActionList();

        private ASTActionList MakeActionList(ASTNode actionList, ASTNode action)
        {
            var actions = actionList as ASTActionList;
            actions.Actions.Add(action as ASTAction);
            return actions;
        }

        private ASTAction MakeGoalCompletedAction(CodeLocation location) => new ASTGoalCompletedAction
        {
            Location = location
        };

        private ASTStatement MakeActionStatement(CodeLocation location, ASTNode name, ASTNode paramList, bool not) => new ASTStatement
        {
            Location = location,
            Name = (name as ASTLiteral).Literal,
            Not = not,
            Params = (paramList as ASTStatementParamList).Params
        };

        private ASTStatement MakeActionStatement(CodeLocation location, ASTNode thisValue, ASTNode name, ASTNode paramList, bool not)
        {
            var stmt = new ASTStatement
            {
                Location = location,
                Name = (name as ASTLiteral).Literal,
                Not = not,
                Params = (paramList as ASTStatementParamList).Params
            };
            stmt.Params.Insert(0, thisValue as ASTRValue);
            return stmt;
        }

        private ASTStatementParamList MakeActionParamList() => new ASTStatementParamList();

        private ASTStatementParamList MakeActionParamList(ASTNode param)
        {
            var list = new ASTStatementParamList();
            list.Params.Add(param as ASTRValue);
            return list;
        }

        private ASTStatementParamList MakeActionParamList(ASTNode list, ASTNode param)
        {
            var actionParamList = list as ASTStatementParamList;
            actionParamList.Params.Add(param as ASTRValue);
            return actionParamList;
        }

        private ASTLocalVar MakeLocalVar(CodeLocation location, ASTNode varName) => new ASTLocalVar()
        {
            Location = location,
            Name = (varName as ASTLiteral).Literal
        };

        private ASTLocalVar MakeLocalVar(CodeLocation location, ASTNode typeName, ASTNode varName) => new ASTLocalVar()
        {
            Location = location,
            Type = (typeName as ASTLiteral).Literal,
            Name = (varName as ASTLiteral).Literal
        };

        private ASTConstantValue MakeTypedConstant(CodeLocation location, ASTNode typeName, ASTNode constant)
        {
            var c = constant as ASTConstantValue;
            return new ASTConstantValue()
            {
                Location = location,
                TypeName = (typeName as ASTLiteral).Literal,
                Type = c.Type,
                StringValue = c.StringValue,
                FloatValue = c.FloatValue,
                IntegerValue = c.IntegerValue,
            };
        }

        private ASTConstantValue MakeConstGuidString(CodeLocation location, ASTNode val) => new ASTConstantValue()
        {
            Location = location,
            Type = IRConstantType.Name,
            StringValue = (val as ASTLiteral).Literal
        };

        private ASTConstantValue MakeConstString(CodeLocation location, ASTNode val) => new ASTConstantValue()
        {
            Location = location,
            Type = IRConstantType.String,
            StringValue = (val as ASTLiteral).Literal
        };

        private ASTConstantValue MakeConstInteger(CodeLocation location, ASTNode val) => new ASTConstantValue()
        {
            Location = location,
            Type = IRConstantType.Integer,
            IntegerValue = Int64.Parse((val as ASTLiteral).Literal, ParserConstants.ParserCulture.NumberFormat)
        };

        private ASTConstantValue MakeConstFloat(CodeLocation location, ASTNode val) => new ASTConstantValue()
        {
            Location = location,
            Type = IRConstantType.Float,
            FloatValue = Single.Parse((val as ASTLiteral).Literal, ParserConstants.ParserCulture.NumberFormat)
        };
    }
}