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
        public StatPropertyParser(StatPropertyScanner scnr) : base(scnr)
        {
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
            return new Requirement
            {
                Not = false,
                RequirementName = name as string,
                IntParam = Int32.Parse(intArg as string),
                TagParam = ""
            };
        }

        private Requirement MakeIntRequirement(object name)
        {
            return new Requirement
            {
                Not = false,
                RequirementName = name as string,
                IntParam = 0,
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

        private PropertyAction MakeStatusBoost(object boost, object action, object arguments) => new PropertyStatusBoost
        {
            Boost = boost as StatusBoost,
            Action = action as string,
            Arguments = arguments as List<object>
        };

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

        // FIXME - validate
        private string MakeSurfaceType(object type) => type as string;

        // FIXME - validate
        private string MakeSurfaceState(object type) => type as string;

        // FIXME - validate
        private string MakeSurface(object type) => type as string;

        private UnaryCondition MakeCondition(object type, object arg) => new UnaryCondition
        {
            ConditionType = type as string,
            Argument = arg as string
        };

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