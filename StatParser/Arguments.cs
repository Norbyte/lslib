using CommandLineParser.Arguments;

namespace LSTools.StatParser;

public class CommandLineArguments
{
    [SwitchArgument("no-packages", false,
        Description = "Don't look for goal files inside packages",
        Optional = true
    )]
    public bool NoPackages;

    [ValueArgument(typeof(string), "mod",
        Description = "Mod to add",
        AllowMultiple = true,
        ValueOptional = false,
        Optional = false
    )]
    public string[] Mods;

    [ValueArgument(typeof(string), "dependency",
        Description = "Dependencies to add",
        AllowMultiple = true,
        ValueOptional = false,
        Optional = true
    )]
    public string[] Dependencies;

    [ValueArgument(typeof(string), "game-data-path",
        Description = "Game data path",
        ValueOptional = false,
        Optional = true
    )]
    public string GameDataPath;

    [ValueArgument(typeof(string), "package-paths",
        Description = "Additional package path(s)",
        AllowMultiple = true,
        ValueOptional = false,
        Optional = true
    )]
    public string[] PackagePaths;
}
