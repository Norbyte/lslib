using LSLib.LS.Story;
using System;
using System.IO;
using CommandLineParser.Exceptions;
using LSLib.LS;
using System.Linq;
using System.Collections.Generic;

namespace LSTools.StoryDecompiler;

class Program
{
    private static MemoryStream LoadStoryStreamFromSave(String path)
    {
        var reader = new PackageReader();
        using (var package = reader.Read(path))
        {
            var globalsFile = package.Files.FirstOrDefault(p => p.Name.ToLowerInvariant() == "globals.lsf");
            if (globalsFile == null)
            {
                throw new Exception("Could not find globals.lsf in savegame archive.");
            }

            Resource resource;
            using (var rsrcStream = globalsFile.CreateContentReader())
            using (var rsrcReader = new LSFReader(rsrcStream))
            {
                resource = rsrcReader.Read();
            }

            LSLib.LS.Node storyNode = resource.Regions["Story"].Children["Story"][0];
            var storyBlob = storyNode.Attributes["Story"].Value as byte[];
            var storyStream = new MemoryStream(storyBlob);
            return storyStream;
        }
    }

    private static Stream LoadStoryStreamFromFile(String path)
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    private static Story LoadStory(String path)
    {
        string extension = Path.GetExtension(path).ToLower();

        Stream storyStream;
        switch (extension)
        {
            case ".lsv":
                storyStream = LoadStoryStreamFromSave(path);
                break;

            case ".osi":
                storyStream = LoadStoryStreamFromFile(path);
                break;

            default:
                throw new Exception($"Unsupported story/save extension: {extension}");
        }

        using (storyStream)
        {
            var reader = new StoryReader();
            return reader.Read(storyStream);
        }
    }

    private static void DebugDumpStory(Story story, String debugLogPath)
    {
        using (var debugFile = new FileStream(debugLogPath, FileMode.Create, FileAccess.Write))
        {
            using (var writer = new StreamWriter(debugFile))
            {
                story.DebugDump(writer);
            }
        }
    }

    private static void DecompileStoryGoals(Story story, String outputDir)
    {
        foreach (KeyValuePair<uint, Goal> goal in story.Goals)
        {
            string filePath = Path.Combine(outputDir, $"{goal.Value.Name}.txt");
            using (var goalFile = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(goalFile))
                {
                    goal.Value.MakeScript(writer, story);
                }
            }
        }
    }

    private static void Run(CommandLineArguments args)
    {
        Console.WriteLine($"Loading story from {args.InputPath} ...");
        var story = LoadStory(args.InputPath);

        if (args.DebugLog)
        {
            Console.WriteLine($"Exporting debug log ...");
            string debugLogPath = Path.Combine(args.OutputPath, "debug.log");
            DebugDumpStory(story, debugLogPath);
        }

        Console.WriteLine($"Exporting goals ...");
        DecompileStoryGoals(story, args.OutputPath);
    }

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: StoryDecompiler <args>");
            Console.WriteLine("    --input <path>   - Compiled story/savegame file path");
            Console.WriteLine("    --output <path>  - Goal output directory");
            Console.WriteLine("    --debug-log      - Generate story debug log");
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
