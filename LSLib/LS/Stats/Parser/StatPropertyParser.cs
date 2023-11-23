using LSLib.Granny;
using QUT.Gppg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LSLib.Granny.Model.CurveData.AnimationCurveData;

namespace LSLib.LS.Stats.Properties
{
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
        private StatDefinitionRepository Definitions;
        private StatValueParserFactory ParserFactory;
        private readonly ExpressionType ExprType;

        public delegate void ErrorReportingDelegate(string message);
        public event ErrorReportingDelegate OnError;

        public StatActionValidator(StatDefinitionRepository definitions, StatValueParserFactory parserFactory, ExpressionType type)
        {
            Definitions = definitions;
            ParserFactory = parserFactory;
            ExprType = type;
        }

        public void Validate(PropertyAction action)
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
                    OnError($"'{action.Action}' is not a valid {ExprType}");
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
                OnError($"Too many arguments to '{action.Action}'; {args.Count} passed, expected at most {functor.Args.Count}");
                return;
            }

            if (args.Count < functor.RequiredArgs)
            {
                OnError($"Not enough arguments to '{action.Action}'; {args.Count} passed, expected at least {functor.RequiredArgs}");
                return;
            }

            for (var i = 0; i < Math.Min(args.Count, functor.Args.Count); i++)
            {
                bool succeeded = false;
                string errorText = null;

                var arg = functor.Args[i];
                if (arg.Type.Length > 0)
                {
                    var parser = ParserFactory.CreateParser(arg.Type, null, null, Definitions);
                    parser.Parse(args[i], ref succeeded, ref errorText);
                    if (!succeeded)
                    {
                        OnError($"'{action.Action}' argument {i + 1}: {errorText}");
                    }
                }
            }
        }
    }

    public partial class StatPropertyParser
    {
        private IStatValueParser RequirementParser;
        private StatEnumeration RequirementsWithArgument;
        private int LiteralStart;
        private StatActionValidator ActionValidator;
        private byte[] Source;

        public delegate void ErrorReportingDelegate(string message);
        public event ErrorReportingDelegate OnError;

        private StatPropertyScanner StatScanner;

        public StatPropertyParser(StatPropertyScanner scnr, StatDefinitionRepository definitions,
            StatValueParserFactory parserFactory, byte[] source, ExpressionType type) : base(scnr)
        {
            StatScanner = scnr;
            Source = source;
            ActionValidator = new StatActionValidator(definitions, parserFactory, type);
            ActionValidator.OnError += (message) => { OnError(message); };
        }

        public object GetParsedObject()
        {
            return CurrentSemanticValue;
        }

        private List<Requirement> MakeRequirements() => new List<Requirement>();

        private List<Requirement> AddRequirement(object requirements, object requirement)
        {
            var req = requirements as List<Requirement>;
            req.Add(requirement as Requirement);
            return req;
        }

        private Requirement MakeNotRequirement(object requirement)
        {
            var req = requirement as Requirement;
            req.Not = true;
            return req;
        }

        private Requirement MakeRequirement(object name)
        {
            Validate(RequirementParser, name as string);

            return new Requirement
            {
                Not = false,
                RequirementName = name as string,
                IntParam = 0,
                TagParam = ""
            };
        }

        private Requirement MakeIntRequirement(object name, object intArg)
        {
            var reqmtName = name as string;
            Validate(RequirementParser, reqmtName);

            if (!RequirementsWithArgument.ValueToIndexMap.ContainsKey(reqmtName))
            {
                OnError?.Invoke($"Requirement '{reqmtName}' doesn't need any arguments");
            }

            return new Requirement
            {
                Not = false,
                RequirementName = reqmtName,
                IntParam = Int32.Parse(intArg as string),
                TagParam = ""
            };
        }

        private Requirement MakeTagRequirement(object name, object tag)
        {
            return new Requirement
            {
                Not = false,
                RequirementName = name as string,
                IntParam = 0,
                TagParam = tag as string
            };
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

        private PropertyAction MakeAction(object action, object arguments)
        {
            var act = new PropertyAction
            {
                Action = action as string,
                Arguments = arguments as List<string>
            };
            ActionValidator.Validate(act);
            return act;
        }

        private void Validate(IStatValueParser parser, string value)
        {
            if (parser != null)
            {
                bool succeeded = false;
                string errorText = null;
                parser.Parse(value, ref succeeded, ref errorText);
                if (!succeeded)
                {
                    errorText = $"'{value}': {errorText}";
                    OnError?.Invoke(errorText);
                }
            }
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
}