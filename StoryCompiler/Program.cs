using LSLib.LS.Story;
using System;
using System.IO;
using System.Diagnostics;
using CommandLineParser.Exceptions;

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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var storyHeadersPath = args.InputPaths[0] + @"\Story\RawFiles\story_header.div";
            if (!File.Exists(storyHeadersPath))
            {
                Console.WriteLine("Story header file not found: {0}", storyHeadersPath);
                Environment.Exit(2);
                return;
            }

            var modCompiler = new ModCompiler();
            modCompiler.SetWarningOptions(CommandLineArguments.GetWarningOptions(args.Warnings));

            modCompiler.LoadStoryHeaders(storyHeadersPath);
            foreach (var modPath in args.InputPaths)
            {
                modCompiler.AddMod(modPath + @"\Story\RawFiles\Goals");
            }

            if (!modCompiler.Compile(args.OutputPath))
            {
                Environment.Exit(3);
            }

            Console.WriteLine("Compilation took {0}s {1} ms", sw.Elapsed.Seconds, sw.Elapsed.Milliseconds);
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
