using LSLib.LS.Story;
using System;
using System.IO;
using CommandLineParser.Exceptions;
using System.Collections.Generic;

namespace LSTools.StoryCompiler
{
    class Program
    {
        static void DebugDump(string storyPath, string debugPath)
        {
            Story story;
            using (var file = new FileStream(storyPath, FileMode.Open, FileAccess.Read))
            {
                var reader = new StoryReader();
                story = reader.Read(file);
            }
            
            using (var debugFile = new FileStream(debugPath, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(debugFile))
                {
                    story.DebugDump(writer);
                }
            }
        }

        static void Run(CommandLineArguments args)
        {
            Logger logger;
            if (args.JsonOutput)
            {
                logger = new JsonLogger();
            }
            else
            {
                logger = new ConsoleLogger();
            }
            
            var modCompiler = new ModCompiler(logger, args.GameDataPath);
            modCompiler.SetWarningOptions(CommandLineArguments.GetWarningOptions(args.Warnings));
            modCompiler.CheckGameObjects = args.CheckGameObjects;
            modCompiler.CheckOnly = args.CheckOnly;
            modCompiler.LoadPackages = !args.NoPackages;

            var mods = new List<string>(args.Mods);
            if (!modCompiler.Compile(args.OutputPath, args.DebugInfoOutputPath, mods))
            {
                Environment.Exit(3);
            }

            if (args.DebugLogOutputPath != null)
            {
                DebugDump(args.OutputPath, args.DebugLogOutputPath);
            }
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: StoryCompiler <args>");
                Console.WriteLine("    --game-data-path <path> - Location of the game Data folder");
                Console.WriteLine("    --output <path>         - Compiled story output path");
                Console.WriteLine("    --debug-info <path>     - Debugging symbols path");
                Console.WriteLine("    --debug-log <path>      - Debug output log path");
                Console.WriteLine("    --mod <name>            - Check and compile all goals from the specified mod");
                Console.WriteLine("    --no-warn <code>        - Suppress warnings with diagnostic code <code>");
                Console.WriteLine("    --check-only            - Only check scripts for errors, don't generate compiled story file");
                Console.WriteLine("    --check-names           - Verify game object names (slow!)");
                Environment.Exit(1);
            }

            CommandLineParser.CommandLineParser parser = new CommandLineParser.CommandLineParser();

            var argv = new CommandLineArguments();

            parser.ExtractArgumentAttributes(argv);

            try
            {
                parser.ParseCommandLine(args);
            }
            catch (CommandLineArgumentException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Argument --{e.Argument}: {e.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }
            catch (CommandLineException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ResetColor();
                Environment.Exit(1);
            }

            if (parser.ParsingSucceeded)
            {
                Run(argv);
            }
        }
    }
}
