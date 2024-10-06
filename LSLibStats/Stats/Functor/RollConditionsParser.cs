using QUT.Gppg;

namespace LSLib.Stats.RollConditions;

public class RollCondition
{
    public string TextKey;
    public string Expression;
}

public partial class RollConditionScanner
{
    public LexLocation LastLocation()
    {
        return new LexLocation(tokLin, tokCol, tokELin, tokECol);
    }
}

public abstract class RollConditionScanBase : AbstractScanner<object, LexLocation>
{
    protected virtual bool yywrap() { return true; }
}

public partial class RollConditionParser
{
    private readonly IStatValueValidator ExpressionValidator;
    private readonly DiagnosticContext Ctx;
    private readonly PropertyDiagnosticContainer Errors;

    public RollConditionParser(RollConditionScanner scnr, IStatValueValidator expressionValidator,
        DiagnosticContext ctx, PropertyDiagnosticContainer errors) : base(scnr)
    {
        ExpressionValidator = expressionValidator;
        Ctx = ctx;
        Errors = errors;
    }

    private string ConcatExpression(object a, object b)
    {
        return (string)a + " " + (string)b;
    }

    private List<RollCondition> MakeConditions() => new List<RollCondition>();

    private List<RollCondition> AddCondition(object conditions, object condition)
    {
        var conds = conditions as List<RollCondition>;
        if (condition is string)
        {
            conds.Add(MakeCondition("", condition));
        }
        else
        {
            conds.Add((RollCondition)condition);
        }
        return conds;
    }

    private RollCondition MakeCondition(object textKey, object expression)
    {
        ExpressionValidator.Validate(Ctx, null, expression, Errors);

        return new RollCondition
        {
            TextKey = (string)textKey,
            Expression = (string)expression
        };
    }
}
