using LSLib.LS.Stats.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LSLib.LS.Stats
{
    public interface IStatValueParser
    {
        object Parse(string value, ref bool succeeded, ref string errorText);
    }

    public class StatReferenceConstraint
    {
        public string StatType;
        public string StatSubtype;
    }

    public interface IStatReferenceValidator
    {
        bool IsValidReference(string reference, string statType, string statSubtype);
    }

    public class BooleanParser : IStatValueParser
    {
        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            if (value == "Yes" || value == "No")
            {
                succeeded = true;
                return (value == "Yes");
            }
            else
            {
                succeeded = false;
                errorText = "expected boolean value 'Yes' or 'No'";
                return null;
            }
        }
    }

    public class Int32Parser : IStatValueParser
    {
        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            if (Int32.TryParse(value, out int intval))
            {
                succeeded = true;
                return intval;
            }
            else
            {
                succeeded = false;
                errorText = "expected an integer value";
                return null;
            }
        }
    }

    public class FloatParser : IStatValueParser
    {
        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            if (Single.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatval))
            {
                succeeded = true;
                return floatval;
            }
            else
            {
                succeeded = false;
                errorText = "expected a float value";
                return null;
            }
        }
    }

    public class EnumParser : IStatValueParser
    {
        private readonly StatEnumeration Enumeration;

        public EnumParser(StatEnumeration enumeration)
        {
            Enumeration = enumeration ?? throw new ArgumentNullException();
        }

        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            if (value == null || value == "")
            {
                value = "None";
            }

            if (Enumeration.ValueToIndexMap.ContainsKey(value))
            {
                succeeded = true;
                return value;
            }
            else
            {
                succeeded = false;
                errorText = "expected one of: " + String.Join(", ", Enumeration.Values.Take(4)) + ", ...";
                return null;
            }
        }
    }

    public class MultiValueEnumParser : IStatValueParser
    {
        private readonly EnumParser Parser;

        public MultiValueEnumParser(StatEnumeration enumeration)
        {
            Parser = new EnumParser(enumeration);
        }

        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            succeeded = true;

            foreach (var item in value.Split(new char[] { ';' }))
            {
                Parser.Parse(item.Trim(new char[] { ' ' }), ref succeeded, ref errorText);
                if (!succeeded)
                {
                    errorText = $"Value '{item}' not supported; {errorText}";
                    return null;
                }
            }

            return value;
        }
    }

    public class StringParser : IStatValueParser
    {
        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            succeeded = true;
            return value;
        }
    }

    public class UUIDParser : IStatValueParser
    {
        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            if (Guid.TryParseExact(value, "D", out Guid parsed))
            {
                succeeded = true;
                return parsed;
            }
            else
            {
                errorText = $"'{value}' is not a valid UUID";
                succeeded = false;
                return null;
            }
        }
    }

    public class StatReferenceParser : IStatValueParser
    {
        private IStatReferenceValidator Validator;
        private List<StatReferenceConstraint> Constraints;

        public StatReferenceParser(IStatReferenceValidator validator, List<StatReferenceConstraint> constraints)
        {
            Validator = validator;
            Constraints = constraints;
        }
        
        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            foreach (var constraint in Constraints)
            {
                if (Validator.IsValidReference(value, constraint.StatType, constraint.StatSubtype))
                {
                    succeeded = true;
                    return value;
                }
            }

            var refTypes = String.Join("/", Constraints.Select(c => c.StatType));
            errorText = $"'{value}' is not a valid {refTypes} reference";
            succeeded = false;
            return null;
        }
    }

    public class MultiValueStatReferenceParser : IStatValueParser
    {
        private readonly StatReferenceParser Parser;

        public MultiValueStatReferenceParser(IStatReferenceValidator validator, List<StatReferenceConstraint> constraints)
        {
            Parser = new StatReferenceParser(validator, constraints);
        }

        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            succeeded = true;

            foreach (var item in value.Split(new char[] { ';' }))
            {
                var trimmed = item.Trim(new char[] { ' ' });
                if (trimmed.Length > 0)
                {
                    Parser.Parse(trimmed, ref succeeded, ref errorText);
                    if (!succeeded)
                    {
                        return null;
                    }
                }
            }

            return value;
        }
    }

    public class ExpressionParser : IStatValueParser
    {
        private readonly String ExpressionType;
        private readonly StatDefinitionRepository Definitions;
        private readonly StatValueParserFactory ParserFactory;

        public ExpressionParser(String expressionType, StatDefinitionRepository definitions,
            StatValueParserFactory parserFactory)
        {
            ExpressionType = expressionType;
            Definitions = definitions;
            ParserFactory = parserFactory;
        }
        
        public virtual object Parse(string value, ref bool succeeded, ref string errorText)
        {
            var valueBytes = Encoding.UTF8.GetBytes("__TYPE_" + ExpressionType + "__ " + value);
            using (var buf = new MemoryStream(valueBytes))
            {
                List<string> errorTexts = new List<string>();

                var scanner = new StatPropertyScanner();
                scanner.SetSource(buf);
                var parser = new StatPropertyParser(scanner, Definitions, ParserFactory);
                parser.OnError += (string message) => errorTexts.Add(message);
                succeeded = parser.Parse();
                if (!succeeded)
                {
                    var location = scanner.LastLocation();
                    var column = location.StartColumn - 10 - ExpressionType.Length + 1;
                    errorText = $"Syntax error at or near character {column}";
                    return null;
                }
                else if (errorTexts.Count > 0)
                {
                    succeeded = false;
                    errorText = String.Join("; ", errorTexts);
                    return null;
                }
                else
                {
                    succeeded = true;
                    return parser.GetParsedObject();
                }
            }
        }
    }

    public class ConditionsParser : IStatValueParser
    {
        private readonly ExpressionParser ExprParser;

        public ConditionsParser(StatDefinitionRepository definitions, StatValueParserFactory parserFactory)
        {
            ExprParser = new ExpressionParser("Conditions", definitions, parserFactory);
        }

        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            value = value
                .Replace(" ", "")
                .Replace(";", "&")
                .Trim(new char[] { '&' });

            return ExprParser.Parse(value, ref succeeded, ref errorText);
        }
    }

    public class StatValueParserFactory
    {
        private readonly IStatReferenceValidator ReferenceValidator;

        public StatValueParserFactory(IStatReferenceValidator referenceValidator)
        {
            ReferenceValidator = referenceValidator;
        }

        public IStatValueParser CreateReferenceParser(List<StatReferenceConstraint> constraints)
        {
            return new StatReferenceParser(ReferenceValidator, constraints);
        }

        public IStatValueParser CreateParser(StatField field, StatDefinitionRepository definitions)
        {
            switch (field.Type)
            {
                case "Requirements":
                    return new ExpressionParser("Requirements", definitions, this);

                case "Properties":
                    return new ExpressionParser("Properties", definitions, this);

                case "Conditions":
                    return new ConditionsParser(definitions, this);

                case "Enumeration":
                    return new EnumParser(field.EnumType);

                case "EnumerationList":
                    return new MultiValueEnumParser(field.EnumType);
                    
                case "Boolean":
                    return new BooleanParser();

                case "Integer":
                    return new Int32Parser();

                case "Float":
                    return new FloatParser();

                case "UUID":
                case "RootTemplate":
                    return new UUIDParser();

                case "StatReference":
                    return new StatReferenceParser(ReferenceValidator, field.ReferenceTypes);

                case "StatReferences":
                    return new MultiValueStatReferenceParser(ReferenceValidator, field.ReferenceTypes);

                case "BaseClass":
                case "Name":
                case "String":
                case "TranslatedString":
                case "Comment":
                case "Color":
                    return new StringParser();

                default:
                    throw new ArgumentException($"Could not create parser for type '{field.Type}'");
            }
        }
    }
}