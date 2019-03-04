using LSLib.LS.Stats.StatParser;
using LSLib.LS.Stats.StatPropertyParser;
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
        private readonly ISet<string> EnumLabels;
        private readonly Type EnumType;

        public EnumParser(Type enumType)
        {
            string[] names = Enum.GetNames(enumType);
            EnumLabels = new HashSet<string>(names);
            EnumType = enumType;
        }

        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            if (value == null || value == "")
            {
                value = "None";
            }

            if (EnumLabels.Contains(value))
            {
                succeeded = true;
                return Enum.Parse(EnumType, value);
            }
            else
            {
                succeeded = false;
                errorText = "expected one of: " + String.Join(", ", EnumLabels.Take(4)) + ", ...";
                return null;
            }
        }
    }

    public class MultiValueEnumParser : IStatValueParser
    {
        private readonly EnumParser Parser;

        public MultiValueEnumParser(Type enumType)
        {
            Parser = new EnumParser(enumType);
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
                    // Keep the value in the database even if it's incorrect
                    return value;
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

    public class ExpressionParser : IStatValueParser
    {
        private readonly String ExpressionType;

        public ExpressionParser(String expressionType)
        {
            ExpressionType = expressionType;
        }

        public virtual object Parse(string value, ref bool succeeded, ref string errorText)
        {
            var valueBytes = Encoding.UTF8.GetBytes("__TYPE_" + ExpressionType + "__ " + value);
            using (var buf = new MemoryStream(valueBytes))
            {
                var scanner = new StatPropertyScanner();
                scanner.SetSource(buf);
                var parser = new StatPropertyParser.StatPropertyParser(scanner);
                succeeded = parser.Parse();
                if (!succeeded)
                {
                    var location = scanner.LastLocation();
                    var column = location.StartColumn - 10 - ExpressionType.Length + 1;
                    errorText = $"Syntax error at or near character {column}";
                }

                // Keep the value in the database even if it's incorrect
                return value;
            }
        }
    }

    public class ConditionsParser : IStatValueParser
    {
        private readonly ExpressionParser ExprParser = new ExpressionParser("Conditions");

        public object Parse(string value, ref bool succeeded, ref string errorText)
        {
            value = value
                .Replace(" ", "")
                .Replace(";", "&")
                .Trim(new char[] { '&' });

            return ExprParser.Parse(value, ref succeeded, ref errorText);
        }
    }

    public static class StatValueParserFactory
    {
        public static IStatValueParser CreateParser(Type type, String parserName = null)
        {
            if (parserName != null)
            {
                switch (parserName)
                {
                    case "Requirements": return new ExpressionParser("Requirements");
                    case "MemorizationRequirements": return new ExpressionParser("Requirements");
                    case "Properties": return new ExpressionParser("Properties");
                    case "Conditions": return new ConditionsParser();
                    case "AttributeFlags": return new MultiValueEnumParser(type);
                    case "AIFlags": return new EnumParser(type);
                    default:
                        throw new ArgumentException($"Parser not supported: {parserName}");
                }
            }

            if (type.IsEnum)
            {
                return new EnumParser(type);
            }
            else if (type == typeof(Boolean) || type == typeof(Boolean?))
            {
                return new BooleanParser();
            }
            else if (type == typeof(Int32) || type == typeof(Int32?))
            {
                return new Int32Parser();
            }
            else if (type == typeof(Single) || type == typeof(Single?))
            {
                return new FloatParser();
            }
            else if (type == typeof(String))
            {
                return new StringParser();
            }
            else
            {
                throw new ArgumentException($"Could not create parser for type {type.Name}");
            }
        }
    }
}