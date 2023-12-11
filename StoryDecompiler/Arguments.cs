using CommandLineParser.Arguments;
using System;

namespace LSTools.StoryDecompiler;

public class CommandLineArguments
{
    [ValueArgument(typeof(string), "input",
        Description = "Compiled story/savegame file path",
        ValueOptional = false,
        Optional = false
    )]
    public string InputPath;

    [ValueArgument(typeof(string), "output",
        Description = "Goal output directory",
        ValueOptional = false,
        Optional = false
    )]
    public string OutputPath;

    [SwitchArgument("debug-log", false,
        Description = "Generate story debug log",
        Optional = true
    )]
    public bool DebugLog;
}
