using LSLib.Parser;
using LSLib.Stats;
using LSLib.Stats.Functors;
using QUT.Gppg;
using System.Text;

namespace LSLib.Stats.Functors;

public partial class FunctorScanner
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

public abstract class FunctorScanBase : AbstractScanner<object, LexLocation>
{
    protected virtual bool yywrap() { return true; }
}

public class FunctorActionValidator
{
    private readonly StatDefinitionRepository Definitions;
    private readonly DiagnosticContext Context;
    private readonly StatValueValidatorFactory ValidatorFactory;
    private readonly ExpressionType ExprType;

    public FunctorActionValidator(StatDefinitionRepository definitions, DiagnosticContext ctx, StatValueValidatorFactory validatorFactory, ExpressionType type)
    {
        Definitions = definitions;
        Context = ctx;
        ValidatorFactory = validatorFactory;
        ExprType = type;
    }

    public void Validate(FunctorAction action, PropertyDiagnosticContainer errors)
    {
        var functors = (ExprType) switch
        {
            ExpressionType.Boost => Definitions.Boosts,
            ExpressionType.Functor => Definitions.Functors,
            ExpressionType.DescriptionParams => Definitions.DescriptionParams,
            _ => throw new NotImplementedException("Cannot validate expressions of this type")
        };

        if (!functors.TryGetValue(action.Action, out StatFunctorType? functor))
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

public partial class FunctorParser
{
    private readonly DiagnosticContext Context;
    private readonly FunctorActionValidator ActionValidator;
    private readonly byte[] Source;
    private readonly PropertyDiagnosticContainer Errors;
    private readonly CodeLocation RootLocation;
    private readonly FunctorScanner StatScanner;
    private readonly int TokenOffset;

    private int LiteralStart;
    private int ActionStart;

    public FunctorParser(FunctorScanner scnr, StatDefinitionRepository definitions,
        DiagnosticContext ctx, StatValueValidatorFactory validatorFactory, byte[] source, ExpressionType type,
        PropertyDiagnosticContainer errors, CodeLocation rootLocation, int tokenOffset) : base(scnr)
    {
        Context = ctx;
        StatScanner = scnr;
        Source = source;
        ActionValidator = new FunctorActionValidator(definitions, ctx, validatorFactory, type);
        Errors = errors;
        RootLocation = rootLocation;
        TokenOffset = tokenOffset;
    }

    public object GetParsedObject()
    {
        return CurrentSemanticValue;
    }

    private List<Functor> MakeFunctorList() => new List<Functor>();

    private List<Functor> SetTextKey(object functors, object textKey)
    {
        var props = functors as List<Functor>;
        var tk = (string)textKey;
        foreach (var property in props)
        {
            property.TextKey = tk;
        }
        return props;
    }

    private List<Functor> MergeFunctors(object functors, object functors2)
    {
        var props = functors as List<Functor>;
        props.Concat(functors2 as List<Functor>);
        return props;
    }

    private List<Functor> AddFunctor(object functorss, object functors)
    {
        var props = functorss as List<Functor>;
        props.Add(functors as Functor);
        return props;
    }

    private Functor MakeFunctor(object context, object condition, object action) => new Functor
    {
        Context = (string)context,
        Condition = condition as object,
        Action = action as FunctorAction
    };

    private List<string> MakeArgumentList() => new();

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

    private FunctorAction MakeAction(object action, object arguments)
    {
        var callErrors = new PropertyDiagnosticContainer();
        var act = new FunctorAction
        {
            Action = (string)action,
            Arguments = (List<string>)arguments,
            StartPos = ActionStart,
            EndPos = StatScanner.TokenEndPos()
        };
        ActionValidator.Validate(act, callErrors);

        CodeLocation? location = null;
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
