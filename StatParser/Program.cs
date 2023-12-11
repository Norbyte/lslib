using CommandLineParser.Exceptions;
using System;
using System.Collections.Generic;

namespace LSTools.StatParser;

class Program
{
    static int Run(CommandLineArguments args)
    {
        using (var statChecker = new StatChecker(args.GameDataPath))
        {
            statChecker.LoadPackages = !args.NoPackages;

            var mods = new List<string>(args.Mods);
            var dependencies = new List<string>(args.Dependencies);
            var packagePaths = new List<string>(args.PackagePaths);
            statChecker.Check(mods, dependencies, packagePaths);
        }

        return 0;
    }

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: StatParser <args>");
            Console.WriteLine("    --game-data-path <path> - Location of the game Data folder");
            Console.WriteLine("    --package-paths <path>  - Location of additional packages to load");
            Console.WriteLine("    --dependency <name>     - Load stat files from the specified mod as a dependency");
            Console.WriteLine("    --mod <name>            - Check all stat files from the specified mod");
            Console.WriteLine("    --no-packages           - Don't load files from packages");
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
