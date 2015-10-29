using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LSLib.Stats
{
    public class InvalidSyntaxException : Exception
    { 
        public InvalidSyntaxException(string message)
            : base(message)
        { }
    }

    public class StatsDataLoader : IDisposable
    {
        StreamReader reader;

        public StatsDataLoader(string filePath)
        {
            reader = new StreamReader(filePath);
        }

        private StatDefinition CreateObject(string name, string type)
        {
            // TODO: Use data obj factory
            if (type == "Armor")
                return new ArmorDefinition(name);

            throw new InvalidSyntaxException("Invalid object type: " + type);
        }

        private List<string> TokenizeLine(string line)
        {
            List<string> tokens = new List<string>();
            int position = 0;
            while (position < line.Length)
            {
                if (line[position] == ' ')
                {
                    // Whitespace, skip
                    position++;
                }
                else if (line[position] == '"')
                {
                    // Parse a quoted identifier: "something"
                    int endPos = line.IndexOf('"', position + 1);
                    if (endPos == -1)
                        throw new InvalidSyntaxException("Unterminated quoted string in stat data stream:" + Environment.NewLine + line);

                    tokens.Add(line.Substring(position + 1, endPos - position - 1));
                    position = endPos + 1;
                }
                else
                {
                    // Parse an unquoted identifier: something
                    int endPos = line.IndexOf(' ', position + 1);
                    if (endPos == -1)
                    {
                        tokens.Add(line.Substring(position));
                        position = line.Length;
                    }
                    else
                    {
                        tokens.Add(line.Substring(position, endPos - position));
                        position = endPos + 1;
                    }
                }
            }

            return tokens;
        }

        public List<StatDefinition> ReadAll()
        {
            var objects = new List<StatDefinition>();
            StatDefinition current = null;
            string name = "";

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (line.Length == 0)
                    continue;

                List<string> tokens = TokenizeLine(line);
                // This may occur if the line only contains whitespace characters
                if (!tokens.Any())
                    continue;

                if (tokens[0] == "new")
                {
                    if (tokens.Count != 3 || tokens[1] != "entry")
                        throw new InvalidSyntaxException("Invalid 'new' instruction syntax; expected: new entry \"<name>\":" + Environment.NewLine + line);

                    if (name.Length > 0 && current == null)
                        throw new InvalidSyntaxException("'new entry' instruction must be followed by 'type':" + Environment.NewLine + line);

                    if (current != null)
                    {
                        objects.Add(current);
                        current = null;
                    }

                    name = tokens[2];
                }
                else if (tokens[0] == "type")
                {
                    if (tokens.Count != 2)
                        throw new InvalidSyntaxException("Invalid 'type' instruction syntax; expected: type \"<type-name>\":" + Environment.NewLine + line);

                    if (current != null)
                        throw new InvalidSyntaxException("'type' should be specified exactly once per entry:" + Environment.NewLine + line);

                    current = CreateObject(name, tokens[1]);
                }
                else
                {
                    if (current == null)
                        throw new InvalidSyntaxException("Cannot add data items without an active entry:" + Environment.NewLine + line);

                    switch (tokens[0])
                    {
                        case "using":
                            if (tokens.Count != 2)
                                throw new InvalidSyntaxException("Invalid 'using' instruction syntax; expected: using \"<type-name>\":" + Environment.NewLine + line);

                            if (current.Parent != null)
                                throw new InvalidSyntaxException("'using' should be specified at most once per entry:" + Environment.NewLine + line);

                            // TODO? current.Parent = tokens[1];
                            break;

                        case "data":
                            if (tokens.Count != 3)
                                throw new InvalidSyntaxException("Invalid 'data' instruction syntax; expected: data \"<property-name>\" \"<property-value>\":" + Environment.NewLine + line);

                            if (current.Properties.ContainsKey(tokens[1]))
                                throw new InvalidSyntaxException("Data property specified multiple times:" + Environment.NewLine + line);

                            current.SetProperty(tokens[1], tokens[2]);
                            break;

                        default:
                            throw new InvalidSyntaxException("Invalid instruction:" + Environment.NewLine + line);
                    }

                }

            }

            if (current != null)
                objects.Add(current);

            return objects;
        }

        public void Dispose()
        {
            reader.Dispose();
        }
    }
}
