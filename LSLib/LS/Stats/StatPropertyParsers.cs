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
            else if (Int32.TryParse(value, out int valueIndex)
                && valueIndex >= 0 && valueIndex < Enumeration.Values.Count)
            {
                succeeded = true;
                return Enumeration.Values[valueIndex];
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
                var parser = new StatPropertyParser(scanner);
                succeeded = parser.Parse();
                if (!succeeded)
                {
                    var location = scanner.LastLocation();
                    var column = location.StartColumn - 10 - ExpressionType.Length + 1;
                    errorText = $"Syntax error at or near character {column}";
                    return null;
                }
                else
                {
                    return parser.GetParsedObject();
                }
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
        public static IStatValueParser CreateParser(StatField field)
        {
            switch (field.Type)
            {
                case "Requirements":
                    return new ExpressionParser("Requirements");

                case "Properties":
                    return new ExpressionParser("Properties");

                case "Conditions":
                    return new ConditionsParser();

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

                case "BaseClass":
                case "Name":
                case "String":
                case "TranslatedString":
                case "RootTemplate": // TODO - validate as NameGUID
                case "Comment":
                case "StatReference":
                case "Color":
                    return new StringParser();

                default:
                    throw new ArgumentException($"Could not create parser for type '{field.Type}'");
            }
        }
    }
}