using CommandLineParser.Arguments;
using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSTools.StoryCompiler
{
    public class CommandLineArguments
    {
        [ValueArgument(typeof(string), "no-warn",
            Description = "Disable specific warnings",
            AllowMultiple = true,
            ValueOptional = false,
            Optional = true
        )]
        public string[] Warnings;

        [ValueArgument(typeof(string), "input",
            Description = "Input mod directories",
            AllowMultiple = true,
            ValueOptional = false,
            Optional = false
        )]
        public string[] InputPaths;

        [ValueArgument(typeof(string), "output",
            Description = "Output path",
            DefaultValue = "story.div.osi",
            ValueOptional = false,
            Optional = true
        )]
        public string OutputPath;

        public static Dictionary<string, bool> GetWarningOptions(string[] options)
        {
            Dictionary<string, string> codeMaps = new Dictionary<string, string>
            {
                { "alias-mismatch", DiagnosticCode.GuidAliasMismatch },
                { "guid-prefix", DiagnosticCode.GuidPrefixNotKnown },
                { "string-lt", DiagnosticCode.StringLtGtComparison },
                { "rule-naming", DiagnosticCode.RuleNamingStyle },
                { "db-naming", DiagnosticCode.DbNamingStyle },
                { "unused-db", DiagnosticCode.UnusedDatabase }
            };

            var results = new Dictionary<string, bool>();
            if (options != null)
            {
                foreach (string option in options)
                {
                    if (codeMaps.TryGetValue(option, out string diagnosticCode))
                    {
                        results[diagnosticCode] = false;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"Warning class \"{option}\" does not exist.");
                        Console.ResetColor();
                    }
                }
            }
            
            return results;
        }
    }
}
