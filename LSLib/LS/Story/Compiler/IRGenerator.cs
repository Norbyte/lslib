using LSLib.LS.Story.GoalParser;

namespace LSLib.LS.Story.Compiler;

/// <summary>
/// Generates IR from story AST.
/// </summary>
public class IRGenerator
{
    private CompilationContext Context;
    public CodeLocation LastLocation;

    public IRGenerator(CompilationContext context)
    {
        Context = context;
    }

    private IRGoal ASTGoalToIR(ASTGoal astGoal)
    {
        var goal = new IRGoal
        {
            InitSection = new List<IRFact>(astGoal.InitSection.Count),
            KBSection = new List<IRRule>(astGoal.KBSection.Count),
            ExitSection = new List<IRFact>(astGoal.ExitSection.Count),
            ParentTargetEdges = new List<IRTargetEdge>(astGoal.ParentTargetEdges.Count),
            Location = astGoal.Location
        };

        foreach (var fact in astGoal.InitSection)
        {
            goal.InitSection.Add(ASTFactToIR(goal, fact));
        }

        foreach (var rule in astGoal.KBSection)
        {
            goal.KBSection.Add(ASTRuleToIR(goal, rule));
        }

        foreach (var fact in astGoal.ExitSection)
        {
            goal.ExitSection.Add(ASTFactToIR(goal, fact));
        }

        foreach (var refGoal in astGoal.ParentTargetEdges)
        {
            var edge = new IRTargetEdge();
            edge.Goal = new IRGoalRef(refGoal.Goal);
            edge.Location = refGoal.Location;
            goal.ParentTargetEdges.Add(edge);
        }

        return goal;
    }

    private IRRule ASTRuleToIR(IRGoal goal, ASTRule astRule)
    {
        var rule = new IRRule
        {
            Goal = goal,
            Type = astRule.Type,
            Conditions = new List<IRCondition>(astRule.Conditions.Count),
            Actions = new List<IRStatement>(astRule.Actions.Count),
            Variables = new List<IRRuleVariable>(),
            VariablesByName = new Dictionary<String, IRRuleVariable>(),
            Location = astRule.Location
        };

        foreach (var condition in astRule.Conditions)
        {
            rule.Conditions.Add(ASTConditionToIR(rule, condition));
        }

        foreach (var action in astRule.Actions)
        {
            rule.Actions.Add(ASTActionToIR(rule, action));
        }

        return rule;
    }

    private IRStatement ASTActionToIR(IRRule rule, ASTAction astAction)
    {
        if (astAction is ASTGoalCompletedAction)
        {
            var astGoal = astAction as ASTGoalCompletedAction;
            return new IRStatement
            {
                Func = null,
                Goal = rule.Goal,
                Not = false,
                Params = new List<IRValue>(),
                Location = astAction.Location
            };
        }
        else if (astAction is ASTStatement)
        {
            var astStmt = astAction as ASTStatement;
            var stmt = new IRStatement
            {
                Func = new IRSymbolRef(new FunctionNameAndArity(astStmt.Name, astStmt.Params.Count)),
                Goal = null,
                Not = astStmt.Not,
                Params = new List<IRValue>(astStmt.Params.Count),
                Location = astAction.Location
            };

            foreach (var param in astStmt.Params)
            {
                stmt.Params.Add(ASTValueToIR(rule, param));
            }

            return stmt;
        }
        else
        {
            throw new InvalidOperationException("Cannot convert unknown AST condition type to IR");
        }
    }

    private IRCondition ASTConditionToIR(IRRule rule, ASTCondition astCondition)
    {
        if (astCondition is ASTFuncCondition)
        {
            var astFunc = astCondition as ASTFuncCondition;
            var func = new IRFuncCondition
            {
                Func = new IRSymbolRef(new FunctionNameAndArity(astFunc.Name, astFunc.Params.Count)),
                Not = astFunc.Not,
                Params = new List<IRValue>(astFunc.Params.Count),
                TupleSize = -1,
                Location = astCondition.Location
            };

            foreach (var param in astFunc.Params)
            {
                func.Params.Add(ASTValueToIR(rule, param));
            }

            return func;
        }
        else if (astCondition is ASTBinaryCondition)
        {
            var astBin = astCondition as ASTBinaryCondition;
            return new IRBinaryCondition
            {
                LValue = ASTValueToIR(rule, astBin.LValue),
                Op = astBin.Op,
                RValue = ASTValueToIR(rule, astBin.RValue),
                TupleSize = -1,
                Location = astCondition.Location
            };
        }
        else
        {
            throw new InvalidOperationException("Cannot convert unknown AST condition type to IR");
        }
    }

    private IRValue ASTValueToIR(IRRule rule, ASTRValue astValue)
    {
        if (astValue is ASTConstantValue)
        {
            return ASTConstantToIR(astValue as ASTConstantValue);
        }
        else if (astValue is ASTLocalVar)
        {
            var astVar = astValue as ASTLocalVar;
            // TODO - compiler error if type resolution fails
            ValueType type;
            if (astVar.Type != null)
            {
                type = Context.LookupType(astVar.Type);
                if (type == null)
                {
                    Context.Log.Error(astVar.Location, DiagnosticCode.UnresolvedType,
                        String.Format("Type \"{0}\" does not exist", astVar.Type));
                }
            }
            else
            {
                type = null;
            }

            var ruleVar = rule.FindOrAddVariable(astVar.Name, type);

            return new IRVariable
            {
                Index = ruleVar.Index,
                Type = type,
                Location = astValue.Location
            };
        }
        else
        {
            throw new InvalidOperationException("Cannot convert unknown AST value type to IR");
        }
    }

    private IRFact ASTFactToIR(IRGoal goal, ASTBaseFact astFact)
    {
        if (astFact is ASTFact)
        {
            var f = astFact as ASTFact;
            var fact = new IRFact
            {
                Database = new IRSymbolRef(new FunctionNameAndArity(f.Database, f.Elements.Count)),
                Not = f.Not,
                Elements = new List<IRConstant>(f.Elements.Count),
                Goal = null,
                Location = f.Location
            };

            foreach (var element in f.Elements)
            {
                fact.Elements.Add(ASTConstantToIR(element));
            }

            return fact;
        }
        else if (astFact is ASTGoalCompletedFact)
        {
            var f = astFact as ASTGoalCompletedFact;
            return new IRFact
            {
                Database = null,
                Not = false,
                Elements = new List<IRConstant>(),
                Goal = goal,
                Location = f.Location
            };
        }
        else
        {
            throw new InvalidOperationException("Cannot convert unknown AST fact type to IR");
        }
    }

    // TODO - un-copy + move to constant code?
    private ValueType ConstantTypeToValueType(IRConstantType type)
    {
        switch (type)
        {
            case IRConstantType.Unknown: return null;
            // TODO - lookup type ID from enum
            case IRConstantType.Integer: return Context.TypesById[1];
            case IRConstantType.Float: return Context.TypesById[3];
            case IRConstantType.String: return Context.TypesById[4];
            case IRConstantType.Name: return Context.TypesById[5];
            default: throw new ArgumentException("Invalid IR constant type");
        }
    }

    private IRConstant ASTConstantToIR(ASTConstantValue astConstant)
    {
        ValueType type;
        if (astConstant.TypeName != null)
        {
            type = Context.LookupType(astConstant.TypeName);
            if (type == null)
            {
                Context.Log.Error(astConstant.Location, DiagnosticCode.UnresolvedType,
                    String.Format("Type \"{0}\" does not exist", astConstant.TypeName));
            }
        }
        else
        {
            type = ConstantTypeToValueType(astConstant.Type);
        }

        return new IRConstant
        {
            ValueType = astConstant.Type,
            Type = type,
            InferredType = astConstant.TypeName != null,
            IntegerValue = astConstant.IntegerValue,
            FloatValue = astConstant.FloatValue,
            StringValue = astConstant.StringValue,
            Location = astConstant.Location
        };
    }

    public ASTGoal ParseGoal(String path, Stream stream)
    {
        var scanner = new GoalScanner(path);
        scanner.SetSource(stream);
        var parser = new GoalParser.GoalParser(scanner);
        bool parsed = parser.Parse();

        if (parsed)
        {
            return parser.GetGoal();
        }
        else
        {
            this.LastLocation = scanner.LastLocation();
            return null;
        }
    }

    public IRGoal GenerateGoalIR(ASTGoal goal)
    {
        return ASTGoalToIR(goal);
    }
}
