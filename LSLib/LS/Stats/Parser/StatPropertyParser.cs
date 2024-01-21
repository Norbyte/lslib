using LSLib.LS.Story.GoalParser;
using QUT.Gppg;

namespace LSLib.LS.Stats.Properties;

public partial class StatPropertyScanner
{
    public LexLocation LastLocation()
    {
        return new LexLocation(tokLin, tokCol, tokELin, tokECol);
    }

    public int TokenStartPos()
    {
        return tokPos;
    }

    public int TokenEndPos()
    {
        return tokEPos;
    }

    private object MakeLiteral(string s) => s;
}

public abstract class StatPropertyScanBase : AbstractScanner<object, LexLocation>
{
    protected virtual bool yywrap() { return true; }
}

public class StatActionValidator
{
    private readonly StatDefinitionRepository Definitions;
    private readonly DiagnosticContext Context;
    private readonly StatValueValidatorFactory ValidatorFactory;
    private readonly ExpressionType ExprType;

    public StatActionValidator(StatDefinitionRepository definitions, DiagnosticContext ctx, StatValueValidatorFactory validatorFactory, ExpressionType type)
    {
        Definitions = definitions;
        Context = ctx;
        ValidatorFactory = validatorFactory;
        ExprType = type;
    }

    public void Validate(PropertyAction action, PropertyDiagnosticContainer errors)
    {
        Dictionary<string, StatFunctorType> functors = null;
        switch (ExprType)
        {
            case ExpressionType.Boost: functors = Definitions.Boosts; break;
            case ExpressionType.Functor: functors = Definitions.Functors; break;
            case ExpressionType.DescriptionParams: functors = Definitions.DescriptionParams; break;
        }

        if (!functors.TryGetValue(action.Action, out StatFunctorType functor))
        {
            if (ExprType != ExpressionType.DescriptionParams)
            {
                errors.Add($"'{action.Action}' is not a valid {ExprType}");
            }

            return;
        }

        // Strip property contexts
        var firstArg = 0;
        while (firstArg < action.Arguments.Count)
        {
            var arg = action.Arguments[firstArg];
            if (arg == "SELF" 
                || arg == "OWNER" 
                || arg == "SWAP" 
                || arg == "OBSERVER_OBSERVER" 
                || arg == "OBSERVER_TARGET"
                || arg == "OBSERVER_SOURCE")
            {
                firstArg++;
            }
            else
            {
                break;
            }
        }

        var args = action.Arguments.GetRange(firstArg, action.Arguments.Count - firstArg);

        if (args.Count > functor.Args.Count)
        {
            errors.Add($"Too many arguments to '{action.Action}'; {args.Count} passed, expected at most {functor.Args.Count}");
        }

        if (args.Count < functor.RequiredArgs)
        {
            errors.Add($"Not enough arguments to '{action.Action}'; {args.Count} passed, expected at least {functor.RequiredArgs}");
        }

        var argErrors = new PropertyDiagnosticContainer();
        for (var i = 0; i < Math.Min(args.Count, functor.Args.Count); i++)
        {
            var arg = functor.Args[i];
            if (arg.Type.Length > 0)
            {
                var validator = ValidatorFactory.CreateValidator(arg.Type, null, null, Definitions);
                // FIXME pass codelocation
                validator.Validate(Context, null, args[i], argErrors);
                if (!argErrors.Empty)
                {
                    argErrors.AddContext(PropertyDiagnosticContextType.Argument, $"argument {i + 1} ({arg.Name})");
                    argErrors.MergeInto(errors);
                    argErrors.Clear();
                }
            }
        }
    }
}

public partial class StatPropertyParser
{
    private readonly DiagnosticContext Context;
    private readonly StatActionValidator ActionValidator;
    private readonly byte[] Source;
    private readonly PropertyDiagnosticContainer Errors;
    private readonly CodeLocation RootLocation;
    private readonly StatPropertyScanner StatScanner;
    private readonly int TokenOffset;

    private int LiteralStart;
    private int ActionStart;

    public StatPropertyParser(StatPropertyScanner scnr, StatDefinitionRepository definitions,
        DiagnosticContext ctx, StatValueValidatorFactory validatorFactory, byte[] source, ExpressionType type,
        PropertyDiagnosticContainer errors, CodeLocation rootLocation, int tokenOffset) : base(scnr)
    {
        Context = ctx;
        StatScanner = scnr;
        Source = source;
        ActionValidator = new StatActionValidator(definitions, ctx, validatorFactory, type);
        Errors = errors;
        RootLocation = rootLocation;
        TokenOffset = tokenOffset;
    }

    public object GetParsedObject()
    {
        return CurrentSemanticValue;
    }

    private List<Property> MakePropertyList() => new List<Property>();

    private List<Property> SetTextKey(object properties, object textKey)
    {
        var props = properties as List<Property>;
        var tk = (string)textKey;
        foreach (var property in props)
        {
            property.TextKey = tk;
        }
        return props;
    }

    private List<Property> MergeProperties(object properties, object properties2)
    {
        var props = properties as List<Property>;
        props.Concat(properties2 as List<Property>);
        return props;
    }

    private List<Property> AddProperty(object properties, object property)
    {
        var props = properties as List<Property>;
        props.Add(property as Property);
        return props;
    }

    private Property MakeProperty(object context, object condition, object action) => new Property
    {
        Context = (string)context,
        Condition = condition as object,
        Action = action as PropertyAction
    };

    private List<string> MakeArgumentList() => new List<string>();

    private List<string> AddArgument(object arguments, object arg)
    {
        var args = arguments as List<string>;
        args.Add(arg == null ? "" : (string)arg);
        return args;
    }

    private object MarkActionStart()
    {
        ActionStart = StatScanner.TokenStartPos();
        return null;
    }

    private PropertyAction MakeAction(object action, object arguments)
    {
        var callErrors = new PropertyDiagnosticContainer();
        var act = new PropertyAction
        {
            Action = action as string,
            Arguments = arguments as List<string>,
            StartPos = ActionStart,
            EndPos = StatScanner.TokenEndPos()
        };
        ActionValidator.Validate(act, callErrors);

        CodeLocation location = null;
        if (RootLocation != null)
        {
            location = new CodeLocation(RootLocation.FileName, 
                RootLocation.StartLine, RootLocation.StartColumn + act.StartPos - TokenOffset, 
                RootLocation.StartLine, RootLocation.StartColumn + act.EndPos - TokenOffset);
        }

        callErrors.AddContext(PropertyDiagnosticContextType.Call, act.Action, location);
        callErrors.MergeInto(Errors);
        return act;
    }
    
    private object InitLiteral()
    {
        LiteralStart = StatScanner.TokenStartPos();
        return null;
    }

    private string MakeLiteral()
    {
        var val = Encoding.UTF8.GetString(Source, LiteralStart, StatScanner.TokenStartPos() - LiteralStart);
        return val;
    }
}