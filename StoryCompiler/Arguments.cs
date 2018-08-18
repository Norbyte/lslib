using CommandLineParser.Arguments;
using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;

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

        [SwitchArgument("json", false,
            Description = "Output results in JSON format",
            Optional = true
        )]
        public bool JsonOutput;

        [SwitchArgument("check-only", false,
            Description = "Validity check only, don't generate compiled story file",
            Optional = true
        )]
        public bool CheckOnly;

        [SwitchArgument("check-names", false,
            Description = "Check validity of game object names (slow!)",
            Optional = true
        )]
        public bool CheckGameObjects;

        [ValueArgument(typeof(string), "mod",
            Description = "Mod to add",
            AllowMultiple = true,
            ValueOptional = false,
            Optional = false
        )]
        public string[] Mods;

        [ValueArgument(typeof(string), "game-data-path",
            Description = "Game data path",
            ValueOptional = false,
            Optional = true
        )]
        public string GameDataPath;

        [ValueArgument(typeof(string), "output",
            Description = "Output path",
            DefaultValue = "story.div.osi",
            ValueOptional = false,
            Optional = true
        )]
        public string OutputPath;

        [ValueArgument(typeof(string), "debug-output",
            Description = "Debug output path",
            ValueOptional = false,
            Optional = true
        )]
        public string DebugOutputPath;

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
