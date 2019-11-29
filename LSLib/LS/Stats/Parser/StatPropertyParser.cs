using QUT.Gppg;
using System;
using System.Collections.Generic;

namespace LSLib.LS.Stats.Properties
{
    public partial class StatPropertyScanner
    {
        public LexLocation LastLocation()
        {
            return new LexLocation(tokLin, tokCol, tokELin, tokECol);
        }

        private object MakeLiteral(string s) => s;
    }

    public abstract class StatPropertyScanBase : AbstractScanner<object, LexLocation>
    {
        protected virtual bool yywrap() { return true; }
    }

    public partial class StatPropertyParser
    {
        private IStatValueParser SurfaceTypeParser;
        private IStatValueParser ConditionSurfaceTypeParser;
        private IStatValueParser SurfaceStateParser;
        private IStatValueParser SkillTargetConditionParser;
        private StatEnumeration SkillConditionsWithArgument;
        private IStatValueParser RequirementParser;
        private StatEnumeration RequirementsWithArgument;
        private IStatValueParser StatusParser;
        private StatEnumeration EngineStatuses;

        public delegate void ErrorReportingDelegate(string message);
        public event ErrorReportingDelegate OnError;

        private StatPropertyScanner StatScanner;

        public StatPropertyParser(StatPropertyScanner scnr, StatDefinitionRepository definitions,
            StatValueParserFactory parserFactory) : base(scnr)
        {
            StatScanner = scnr;

            if (definitions != null)
            {
                var surfaceTypeEnum = definitions.Enumerations["Surface Type"];
                SurfaceTypeParser = new EnumParser(surfaceTypeEnum);

                var conditionSurfaceTypeEnum = definitions.Enumerations["CUSTOM_ConditionSurfaceType"];
                ConditionSurfaceTypeParser = new EnumParser(conditionSurfaceTypeEnum);

                var surfaceStateEnum = definitions.Enumerations["CUSTOM_SurfaceState"];
                SurfaceStateParser = new EnumParser(surfaceStateEnum);

                var skillTargetConditionEnum = definitions.Enumerations["SkillTargetCondition"];
                SkillTargetConditionParser = new EnumParser(skillTargetConditionEnum);

                SkillConditionsWithArgument = definitions.Enumerations["CUSTOM_SkillCondition_1arg"];

                var requirementEnum = definitions.Enumerations["CUSTOM_Requirement"];
                RequirementParser = new EnumParser(requirementEnum);

                RequirementsWithArgument = definitions.Enumerations["CUSTOM_Requirement_1arg"];
                EngineStatuses = definitions.Enumerations["CUSTOM_EngineStatus"];

                StatusParser = parserFactory.CreateReferenceParser(new List<StatReferenceConstraint>
                {
                    new StatReferenceConstraint
                    {
                        StatType = "StatusData"
                    }
                });
            }
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

        private List<Property> AddProperty(object properties, object property)
        {
            var props = properties as List<Property>;
            props.Add(property as Property);
            return props;
        }

        private Property MakeProperty(object context, object condition, object action) => new Property
        {
            Context = (PropertyContext)context,
            Condition = condition as object,
            Action = action as PropertyAction
        };

        private PropertyAction MakeAction(object action, object arguments) => new PropertyAction
        {
            Action = action as string,
            Arguments = arguments as List<object>
        };

        private PropertyAction MakeStatusBoost(object boost, object status, object arguments)
        {
            var statusName = status as string;
            if (!EngineStatuses.ValueToIndexMap.ContainsKey(statusName))
            {
                Validate(StatusParser, statusName);
            }

            return new PropertyStatusBoost
            {
                Boost = boost as StatusBoost,
                Action = statusName,
                Arguments = arguments as List<object>
            };
        }

        private List<object> MakeArgumentList(params object[] args) => new List<object>(args);

        private List<object> PrependArgumentList(object argument, object arguments)
        {
            var args = arguments as List<object>;
            args.Insert(0, argument);
            return args;
        }

        private StatusBoost MakeStatusBoostType(object type, object surfaces) => new StatusBoost
        {
            Type = (StatusBoostType)type,
            SurfaceTypes = surfaces as List<string>
        };

        private List<string> MakeSurfaceList() => new List<string>();

        private List<string> AddSurface(object surfaces, object surface)
        {
            var surfs = surfaces as List<string>;
            surfs.Add(surface as string);
            return surfs;
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
        
        private string MakeSurfaceType(object type)
        {
            var surfaceType = type as string;
            Validate(ConditionSurfaceTypeParser, surfaceType);
            return surfaceType;
        }
        
        private string MakeSurfaceState(object state)
        {
            var surfaceState = state as string;
            Validate(SurfaceStateParser, surfaceState);
            return surfaceState;
        }

        private string MakeSurface(object type)
        {
            var surfaceType = type as string;
            Validate(SurfaceTypeParser, surfaceType);
            return surfaceType;
        }

        private UnaryCondition MakeCondition(object type, object arg)
        {
            var conditionType = type as string;
            var conditionArg = arg as string;

            Validate(SkillTargetConditionParser, conditionType);

            var hasArg = SkillConditionsWithArgument.ValueToIndexMap.ContainsKey(conditionType);
            if (hasArg && arg == null)
            {
                OnError?.Invoke($"Condition '{conditionType}' needs an argument");
            }
            else if (!hasArg && arg != null)
            {
                OnError?.Invoke($"Condition '{conditionType}' doesn't need any arguments");
            }
            else
            {
                switch (conditionType)
                {
                    case "InSurface":
                        Validate(ConditionSurfaceTypeParser, conditionArg);
                        break;

                    case "Surface":
                        Validate(SurfaceStateParser, conditionArg);
                        break;

                    case "HasStatus":
                        // FIXME - add status name validation
                        break;
                }
            }

            return new UnaryCondition
            {
                ConditionType = conditionType,
                Argument = conditionArg
            };
        }

        private Condition MakeNotCondition(object condition)
        {
            var cond = condition as Condition;
            cond.Not = true;
            return cond;
        }

        private BinaryCondition MakeBinaryCondition(object lhs, object oper, object rhs) => new BinaryCondition
        {
            Left = lhs as Condition,
            Operator = (ConditionOperator)oper,
            Right = rhs as Condition
        };
    }
}