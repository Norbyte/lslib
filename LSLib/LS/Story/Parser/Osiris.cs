using System;
using System.Text.RegularExpressions;

namespace LSLib.LS.Story.Parser
{
    public abstract class GoalScanBase : AbstractScanner<ASTNode, LexLocation>
    {
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

    public partial class GoalParser
    {
        public GoalParser(GoalScanner scnr) : base(scnr)
        {
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
            edges.TargetEdges.Add((edge as ASTParentTargetEdge).Goal);
            return edges;
        }

        private ASTParentTargetEdge MakeParentTargetEdge(ASTNode goal) => new ASTParentTargetEdge()
        {
            Goal = (goal as ASTLiteral).Literal
        };

        private ASTFactList MakeFactList() => new ASTFactList();
        
        private ASTFactList MakeFactList(ASTNode factList, ASTNode fact)
        {
            var facts = factList as ASTFactList;
            facts.Facts.Add(fact as ASTFact);
            return facts;
        }

        private ASTFact MakeNotFact(ASTNode fact)
        {
            var factStmt = fact as ASTFact;
            factStmt.Not = true;
            return factStmt;
        }

        private ASTFact MakeFactStatement(ASTNode database, ASTNode elements) => new ASTFact()
        {
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

        private ASTRule MakeRule(ASTNode ruleType, ASTNode conditions, ASTNode actions) => new ASTRule()
        {
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

        private ASTFuncCondition MakeFuncCondition(ASTNode name, ASTNode paramList, bool not) => new ASTFuncCondition()
        {
            Name = (name as ASTLiteral).Literal,
            Not = not,
            Params = (paramList as ASTConditionParamList).Params
        };

        private ASTFuncCondition MakeObjectFuncCondition(ASTNode thisValue, ASTNode name, ASTNode paramList, bool not)
        {
            var condParams = paramList as ASTConditionParamList;
            condParams.Params.Insert(0, thisValue as ASTRValue);
            return new ASTFuncCondition()
            {
                Name = (name as ASTLiteral).Literal,
                Not = not,
                Params = condParams.Params
            };
        }

        private ASTBinaryCondition MakeBinaryCondition(ASTNode lvalue, ASTNode op, ASTNode rvalue) => new ASTBinaryCondition()
        {
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

        private ASTAction MakeGoalCompletedAction() => new ASTGoalCompletedAction();

        private ASTStatement MakeActionStatement(ASTNode name, ASTNode paramList, bool not) => new ASTStatement
        {
            Name = (name as ASTLiteral).Literal,
            Not = not,
            Params = (paramList as ASTStatementParamList).Params
        };

        private ASTStatement MakeActionStatement(ASTNode thisValue, ASTNode name, ASTNode paramList, bool not)
        {
            var stmt = new ASTStatement
            {
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

        private ASTLocalVar MakeLocalVar(ASTNode varName) => new ASTLocalVar()
        {
            Name = (varName as ASTLiteral).Literal
        };

        private ASTLocalVar MakeLocalVar(ASTNode typeName, ASTNode varName)
        {
            return new ASTLocalVar()
            {
                Type = (typeName as ASTLiteral).Literal,
                Name = (varName as ASTLiteral).Literal
            };
        }

        private ASTConstantValue MakeTypedConstant(ASTNode typeName, ASTNode constant)
        {
            var c = constant as ASTConstantValue;
            return new ASTConstantValue()
            {
                TypeName = (typeName as ASTLiteral).Literal,
                Type = c.Type,
                StringValue = c.StringValue,
                FloatValue = c.FloatValue,
                IntegerValue = c.IntegerValue,
            };
        }

        private ASTConstantValue MakeConstIdentifier(ASTNode val) => new ASTConstantValue()
        {
            Type = ASTConstantType.Name,
            StringValue = (val as ASTLiteral).Literal
        };

        private ASTConstantValue MakeConstString(ASTNode val) => new ASTConstantValue()
        {
            Type = ASTConstantType.String,
            StringValue = (val as ASTLiteral).Literal
        };

        private ASTConstantValue MakeConstInteger(ASTNode val) => new ASTConstantValue()
        {
            Type = ASTConstantType.Integer,
            IntegerValue = Int64.Parse((val as ASTLiteral).Literal)
        };

        private ASTConstantValue MakeConstFloat(ASTNode val) => new ASTConstantValue()
        {
            Type = ASTConstantType.Float,
            FloatValue = Single.Parse((val as ASTLiteral).Literal)
        };
    }
}