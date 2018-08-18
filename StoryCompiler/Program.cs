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

            var mods = new List<string>(args.Mods);
            if (!modCompiler.Compile(args.OutputPath, mods))
            {
                Environment.Exit(3);
            }
        }

        static void Main(string[] args)
        {
            CommandLineParser.CommandLineParser parser = new CommandLineParser.CommandLineParser
            {
                ShowUsageOnEmptyCommandline = true
            };

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
            }
            catch (CommandLineException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ResetColor();
            }

            if (parser.ParsingSucceeded)
            {
                Run(argv);
            }
        }
    }
}
