using LSLib.LS.Story.Compiler;
using System.Text.RegularExpressions;

namespace LSLib.LS;

public class ModInfo(string name)
{
    public string Name = name;

    public string ModsPath;
    public string PublicPath;

    public string Meta;
    public List<string> Scripts = [];
    public List<string> Stats = [];
    public List<string> Globals = [];
    public List<string> LevelObjects = [];
    public string OrphanQueryIgnoreList;
    public string StoryHeaderFile;
    public string TypeCoercionWhitelistFile;
    public string ModifiersFile;
    public string ValueListsFile;
    public string ActionResourcesFile;
    public string ActionResourceGroupsFile;
    public List<string> TagFiles = [];
}

public class ModResources : IDisposable
{
    public Dictionary<string, ModInfo> Mods = [];
    public List<Package> LoadedPackages = [];

    public void Dispose()
    {
        LoadedPackages.ForEach(p => p.Dispose());
        LoadedPackages.Clear();
    }
}

public partial class ModPathVisitor
{
    // Pattern for excluding subsequent parts of a multi-part archive
    public static readonly Regex archivePartRe = ArchivePartRegex();

    public const string ModsPath = "Mods";
    public const string PublicPath = "Public";

    public readonly ModResources Resources;

    public bool CollectStoryGoals = false;
    public bool CollectStats = false;
    public bool CollectGlobals = false;
    public bool CollectLevels = false;
    public bool CollectGuidResources = false;
    public TargetGame Game = TargetGame.DOS2;
    public VFS FS;

    public ModPathVisitor(ModResources resources, VFS fs)
    {
        Resources = resources;
        FS = fs;
    }

    private ModInfo GetMod(string modName)
    {
        if (!Resources.Mods.TryGetValue(modName, out ModInfo mod))
        {
            mod = new ModInfo(modName);
            Resources.Mods[modName] = mod;
        }

        return mod;
    }

    private void AddGlobalsToMod(string modName, string path)
    {
        GetMod(modName).Globals.Add(path);
    }

    private void AddLevelObjectsToMod(string modName, string path)
    {
        GetMod(modName).LevelObjects.Add(path);
    }

    private void DiscoverModGoals(ModInfo mod)
    {
        var goalPath = Path.Join(mod.ModsPath, @"Story/RawFiles/Goals");
        if (!FS.DirectoryExists(goalPath)) return;

        var goalFiles = FS.EnumerateFiles(goalPath, false, p => Path.GetExtension(p) == ".txt");

        foreach (var goalFile in goalFiles)
        {
            mod.Scripts.Add(goalFile);
        }
    }

    private void DiscoverModStats(ModInfo mod)
    {
        var statsPath = Path.Join(mod.PublicPath, @"Stats/Generated/Data");
        if (!FS.DirectoryExists(statsPath)) return;

        var statFiles = FS.EnumerateFiles(statsPath, false, p => Path.GetExtension(p) == ".txt");

        foreach (var statFile in statFiles)
        {
            mod.Stats.Add(statFile);
        }

        var treasurePath = Path.Join(mod.PublicPath, @"Stats/Generated/TreasureTable.txt");
        if (FS.FileExists(treasurePath))
        {
            mod.Stats.Add(treasurePath);
        }
    }

    private void DiscoverModStatsStructure(ModInfo mod)
    {
        var modifiersPath = Path.Join(mod.PublicPath, @"Stats/Generated/Structure/Modifiers.txt");
        if (FS.FileExists(modifiersPath))
        {
            mod.ModifiersFile = modifiersPath;
        }
        
        var valueListsPath = Path.Join(mod.PublicPath, @"Stats/Generated/Structure/Base/ValueLists.txt");
        if (FS.FileExists(valueListsPath))
        {
            mod.ValueListsFile = valueListsPath;
        }
    }

    private void DiscoverModGuidResources(ModInfo mod)
    {
        var actionResGrpPath = Path.Join(mod.PublicPath, @"ActionResourceGroupDefinitions/ActionResourceGroupDefinitions.lsx");
        if (FS.FileExists(actionResGrpPath))
        {
            mod.ActionResourceGroupsFile = actionResGrpPath;
        }

        var actionResPath = Path.Join(mod.PublicPath, @"ActionResourceDefinitions/ActionResourceDefinitions.lsx");
        if (FS.FileExists(actionResPath))
        {
            mod.ActionResourcesFile = actionResPath;
        }

        var tagPath = Path.Join(mod.PublicPath, @"Tags");
        if (FS.DirectoryExists(tagPath))
        {
            var tagFiles = FS.EnumerateFiles(tagPath, false, p => Path.GetExtension(p) == ".lsf");

            foreach (var tagFile in tagFiles)
            {
                mod.TagFiles.Add(tagFile);
            }
        }
    }

    private void DiscoverModGlobals(ModInfo mod)
    {
        var globalsPath = Path.Join(mod.ModsPath, "Globals");
        if (!FS.DirectoryExists(globalsPath)) return;

        var globalFiles = FS.EnumerateFiles(globalsPath, false, p => Path.GetExtension(p) == ".lsf");

        foreach (var globalFile in globalFiles)
        {
            mod.Globals.Add(globalFile);
        }
    }

    private void DiscoverModLevelObjects(ModInfo mod)
    {
        var levelsPath = Path.Join(mod.ModsPath, "Levels");
        if (!FS.DirectoryExists(levelsPath)) return;

        var levelFiles = FS.EnumerateFiles(levelsPath, false, p => Path.GetExtension(p) == ".lsf");

        foreach (var levelFile in levelFiles)
        {
            mod.LevelObjects.Add(levelFile);
        }
    }

    public void DiscoverModDirectory(ModInfo mod)
    {
        if (CollectStoryGoals)
        {
            DiscoverModGoals(mod);

            var headerPath = Path.Join(mod.ModsPath, @"Story/RawFiles/story_header.div");
            if (FS.FileExists(headerPath))
            {
                mod.StoryHeaderFile = headerPath;
            }

            var orphanQueryIgnoresPath = Path.Join(mod.ModsPath, @"Story/story_orphanqueries_ignore_local.txt");
            if (FS.FileExists(orphanQueryIgnoresPath))
            {
                mod.OrphanQueryIgnoreList = orphanQueryIgnoresPath;
            }

            var typeCoercionWhitelistPath = Path.Join(mod.ModsPath, @"Story/RawFiles/TypeCoercionWhitelist.txt");
            if (FS.FileExists(typeCoercionWhitelistPath))
            {
                mod.TypeCoercionWhitelistFile = typeCoercionWhitelistPath;
            }
        }

        if (CollectStats)
        {
            DiscoverModStats(mod);
            DiscoverModStatsStructure(mod);
        }

        if (CollectGuidResources)
        {
            DiscoverModGuidResources(mod);
        }

        if (CollectGlobals)
        {
            DiscoverModGlobals(mod);
        }

        if (CollectLevels)
        {
            DiscoverModLevelObjects(mod);
        }
    }

    public void DiscoverMods()
    {
        var modPaths = FS.EnumerateDirectories(ModsPath);

        foreach (var modPath in modPaths)
        {
            var modName = Path.GetFileName(modPath);
            var metaPath = Path.Combine(modPath, "meta.lsx");

            if (FS.FileExists(metaPath))
            {
                var mod = GetMod(modName);
                mod.ModsPath = modPath;
                mod.PublicPath = Path.Combine(PublicPath, Path.GetFileName(modPath));
                mod.Meta = metaPath;

                DiscoverModDirectory(mod);
            }
        }
    }

    public void Discover()
    {
        DiscoverMods();
    }


    [GeneratedRegex("^(.*)_[0-9]+\\.pak$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex ArchivePartRegex();
}

public class GameDataContext
{
    public VFS FS;
    public ModResources Resources;

    public GameDataContext(string path, TargetGame game = TargetGame.BG3, bool excludeAssets = true)
    {
        FS = new VFS();
        FS.AttachGameDirectory(path, excludeAssets);
        FS.FinishBuild();

        Resources = new ModResources();
        var visitor = new ModPathVisitor(Resources, FS)
        {
            Game = game,
            CollectStoryGoals = true,
            CollectGlobals = false,
            CollectLevels = false
        };
        visitor.Discover();
    }
}
