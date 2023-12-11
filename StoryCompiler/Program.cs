using LSLib.LS.Story;
using System;
using System.IO;
using CommandLineParser.Exceptions;
using System.Collections.Generic;
using LSLib.LS.Story.Compiler;

namespace LSTools.StoryCompiler;

class Program
{
    static void DebugDump(string storyPath, string debugPath)
    {
        Story story;
        using (var file = new FileStream(storyPath, FileMode.Open, FileAccess.Read, FileShare.Read))
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

    static int Run(CommandLineArguments args)
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

        using (var modCompiler = new ModCompiler(logger, args.GameDataPath))
        {
            modCompiler.SetWarningOptions(CommandLineArguments.GetWarningOptions(args.Warnings));
            modCompiler.CheckGameObjects = args.CheckGameObjects;
            modCompiler.CheckOnly = args.CheckOnly;
            modCompiler.LoadPackages = !args.NoPackages;
            modCompiler.AllowTypeCoercion = args.AllowTypeCoercion;
            modCompiler.OsiExtender = args.OsiExtender;
            if (args.Game == "dos2")
            {
                modCompiler.Game = TargetGame.DOS2;
            }
            else if (args.Game == "dos2de")
            {
                modCompiler.Game = TargetGame.DOS2DE;
            }
            else if (args.Game == "bg3")
            {
                modCompiler.Game = TargetGame.BG3;
            }
            else
            {
                throw new ArgumentException("Unsupported game type");
            }

            var mods = new List<string>(args.Mods);
            if (!modCompiler.Compile(args.OutputPath, args.DebugInfoOutputPath, mods))
            {
                return 3;
            }

            if (args.DebugLogOutputPath != null && !args.CheckOnly)
            {
                DebugDump(args.OutputPath, args.DebugLogOutputPath);
            }
        }

        return 0;
    }

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: StoryCompiler <args>");
            Console.WriteLine("    --game-data-path <path> - Location of the game Data folder");
            Console.WriteLine("    --game <dos2|dos2de>    - Which game to target during compilation");
            Console.WriteLine("    --output <path>         - Compiled story output path");
            Console.WriteLine("    --debug-info <path>     - Debugging symbols path");
            Console.WriteLine("    --debug-log <path>      - Debug output log path");
            Console.WriteLine("    --mod <name>            - Check and compile all goals from the specified mod");
            Console.WriteLine("    --no-warn <code>        - Suppress warnings with diagnostic code <code>");
            Console.WriteLine("    --check-only            - Only check scripts for errors, don't generate compiled story file");
            Console.WriteLine("    --check-names           - Verify game object names (slow!)");
            Console.WriteLine("    --no-packages           - Don't load files from packages");
            Console.WriteLine("    --allow-type-coercion   - Allow \"casting\" between unrelated types");
            Console.WriteLine("    --osi-extender          - Compile using Osiris Extender features");
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
            var exitCode = Run(argv);
            Environment.Exit(exitCode);
        }
    }
}
