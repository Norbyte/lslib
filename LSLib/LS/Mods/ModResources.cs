using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LSLib.LS;

public class ModInfo(string name)
{
    public string Name = name;
    public IAbstractFileInfo Meta;
    public Dictionary<string, IAbstractFileInfo> Scripts = [];
    public Dictionary<string, IAbstractFileInfo> Stats = [];
    public Dictionary<string, IAbstractFileInfo> Globals = [];
    public Dictionary<string, IAbstractFileInfo> LevelObjects = [];
    public IAbstractFileInfo OrphanQueryIgnoreList;
    public IAbstractFileInfo StoryHeaderFile;
    public IAbstractFileInfo TypeCoercionWhitelistFile;
    public IAbstractFileInfo ModifiersFile;
    public IAbstractFileInfo ValueListsFile;
    public IAbstractFileInfo ActionResourcesFile;
    public IAbstractFileInfo ActionResourceGroupsFile;
    public List<IAbstractFileInfo> TagFiles = [];
}

public class ModResources : IDisposable
{
    public Dictionary<string, ModInfo> Mods = [];
    public List<PackageReader> LoadedPackages = [];

    public void Dispose()
    {
        LoadedPackages.ForEach(p => p.Dispose());
        LoadedPackages.Clear();
    }
}

public partial class ModPathVisitor
{
    private static readonly Regex metaRe = MetaRegex();
    private static readonly Regex scriptRe = ScriptRegex();
    private static readonly Regex statRe = StatRegex();
    private static readonly Regex staticLsxRe = StaticLsxRegex();
    private static readonly Regex statStructureRe = StatStructureRegex();
    private static readonly Regex orphanQueryIgnoresRe = OrphanQueryIgnoresRegex();
    private static readonly Regex storyDefinitionsRe = StoryDefinitionsRegex();
    private static readonly Regex typeCoercionWhitelistRe = TypeCoercionWhitelistRegex();
    private static readonly Regex globalsRe = GlobalsRegex();
    private static readonly Regex levelObjectsRe = LevelObjectsRegex();
    // Pattern for excluding subsequent parts of a multi-part archive
    public static readonly Regex archivePartRe = ArchivePartRegex();

    public readonly ModResources Resources;

    public bool CollectStoryGoals = false;
    public bool CollectStats = false;
    public bool CollectGlobals = false;
    public bool CollectLevels = false;
    public bool CollectGuidResources = false;
    public bool LoadPackages = true;
    public TargetGame Game = TargetGame.DOS2;

    public ModPathVisitor(ModResources resources)
    {
        Resources = resources;
    }

    private static void EnumerateFiles(List<string> paths, string rootPath, string currentPath, string pattern)
    {
        foreach (string filePath in Directory.GetFiles(currentPath, pattern))
        {
            var relativePath = filePath[rootPath.Length..];
            if (relativePath[0] == '/' || relativePath[0] == '\\')
            {
                relativePath = relativePath[1..];
            }

            paths.Add(relativePath);
        }

        foreach (string directoryPath in Directory.GetDirectories(currentPath))
        {
            EnumerateFiles(paths, rootPath, directoryPath, pattern);
        }
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

    private void AddMetadataToMod(string modName, IAbstractFileInfo file)
    {
        GetMod(modName).Meta = file;
    }

    private void AddStatToMod(string modName, string path, IAbstractFileInfo file)
    {
        GetMod(modName).Stats[path] = file;
    }

    private void AddScriptToMod(string modName, string scriptName, IAbstractFileInfo file)
    {
        GetMod(modName).Scripts[scriptName] = file;
    }

    private void AddGlobalsToMod(string modName, string path, IAbstractFileInfo file)
    {
        GetMod(modName).Globals[path] = file;
    }

    private void AddLevelObjectsToMod(string modName, string path, IAbstractFileInfo file)
    {
        GetMod(modName).LevelObjects[path] = file;
    }

    private void DiscoverPackagedFile(IAbstractFileInfo file)
    {
        if (file.IsDeletion()) return;

        if (file.Name.EndsWith("meta.lsx", StringComparison.Ordinal))
        {
            var match = metaRe.Match(file.Name);
            if (match != null && match.Success)
            {
                AddMetadataToMod(match.Groups[1].Value, file);
            }
        }

        if (CollectStoryGoals)
        {
            if (file.Name.EndsWith(".txt", StringComparison.Ordinal) && file.Name.Contains("/Story/RawFiles/Goals"))
            {
                var match = scriptRe.Match(file.Name);
                if (match != null && match.Success)
                {
                    AddScriptToMod(match.Groups[1].Value, match.Groups[2].Value, file);
                }
            }

            if (file.Name.EndsWith("/Story/story_orphanqueries_ignore_local.txt", StringComparison.Ordinal))
            {
                var match = orphanQueryIgnoresRe.Match(file.Name);
                if (match != null && match.Success)
                {
                    GetMod(match.Groups[1].Value).OrphanQueryIgnoreList = file;
                }
            }

            if (file.Name.EndsWith("/Story/RawFiles/story_header.div", StringComparison.Ordinal))
            {
                var match = storyDefinitionsRe.Match(file.Name);
                if (match != null && match.Success)
                {
                    GetMod(match.Groups[1].Value).StoryHeaderFile = file;
                }
            }

            if (file.Name.EndsWith("/Story/RawFiles/TypeCoercionWhitelist.txt", StringComparison.Ordinal))
            {
                var match = typeCoercionWhitelistRe.Match(file.Name);
                if (match != null && match.Success)
                {
                    GetMod(match.Groups[1].Value).TypeCoercionWhitelistFile = file;
                }
            }
        }

        if (CollectStats)
        {
            if (file.Name.EndsWith(".txt", StringComparison.Ordinal))
            {
                if (file.Name.Contains("/Stats/Generated/Data"))
                {
                    var match = statRe.Match(file.Name);
                    if (match != null && match.Success)
                    {
                        AddStatToMod(match.Groups[1].Value, match.Groups[2].Value, file);
                    }
                }
                else if (file.Name.Contains("/Stats/Generated/Structure"))
                {
                    var match = statStructureRe.Match(file.Name);
                    if (match != null && match.Success)
                    {
                        if (file.Name.EndsWith("Modifiers.txt"))
                        {
                            GetMod(match.Groups[1].Value).ModifiersFile = file;
                        }
                        else if (file.Name.EndsWith("ValueLists.txt"))
                        {
                            GetMod(match.Groups[1].Value).ValueListsFile = file;
                        }
                    }
                }
            }
        }

        if (CollectGuidResources)
        {
            if (file.Name.EndsWith(".lsx", StringComparison.Ordinal))
            {
                var match = staticLsxRe.Match(file.Name);
                if (match != null && match.Success)
                {
                    if (match.Groups[2].Value == "ActionResourceDefinitions/ActionResourceDefinitions.lsx")
                    {
                        GetMod(match.Groups[1].Value).ActionResourcesFile = file;
                    }
                    else if (match.Groups[2].Value == "ActionResourceGroupDefinitions/ActionResourceGroupDefinitions.lsx")
                    {
                        GetMod(match.Groups[1].Value).ActionResourceGroupsFile = file;
                    }
                    else if (match.Groups[2].Value.StartsWith("Tags/"))
                    {
                        GetMod(match.Groups[1].Value).TagFiles.Add(file);
                    }
                }
            }
        }

        if (CollectGlobals)
        {
            if (file.Name.EndsWith(".lsf", StringComparison.Ordinal) && file.Name.Contains("/Globals/"))
            {
                var match = globalsRe.Match(file.Name);
                if (match != null && match.Success)
                {
                    AddGlobalsToMod(match.Groups[1].Value, match.Groups[0].Value, file);
                }
            }
        }

        if (CollectLevels)
        {
            if (file.Name.EndsWith(".lsf", StringComparison.Ordinal) && file.Name.Contains("/Levels/"))
            {
                var match = levelObjectsRe.Match(file.Name);
                if (match != null && match.Success)
                {
                    AddLevelObjectsToMod(match.Groups[1].Value, match.Groups[0].Value, file);
                }
            }
        }
    }

    public void DiscoverPackage(string packagePath)
    {
        var reader = new PackageReader(packagePath);
        Resources.LoadedPackages.Add(reader);
        var package = reader.Read();

        foreach (var file in package.Files)
        {
            DiscoverPackagedFile(file);
        }
    }

    public void DiscoverBuiltinPackages(string gameDataPath)
    {
        // List of packages we won't ever load
        // These packages don't contain any mod resources, but have a large
        // file table that makes loading unneccessarily slow.
        HashSet<string> packageBlacklist =
        [
            "Assets.pak",
            "Effects.pak",
            "Engine.pak",
            "EngineShaders.pak",
            "Game.pak",
            "GamePlatform.pak",
            "Gustav_NavCloud.pak",
            "Gustav_Textures.pak",
            "Gustav_Video.pak",
            "Icons.pak",
            "LowTex.pak",
            "Materials.pak",
            "Minimaps.pak",
            "Models.pak",
            "PsoCache.pak",
            "SharedSoundBanks.pak",
            "SharedSounds.pak",
            "Textures.pak",
            "VirtualTextures.pak"
        ];

        // Collect priority value from headers
        var packagePriorities = new List<Tuple<string, int>>();

        foreach (var path in Directory.GetFiles(gameDataPath, "*.pak"))
        {
            var baseName = Path.GetFileName(path);
            if (!packageBlacklist.Contains(baseName)
                // Don't load 2nd, 3rd, ... parts of a multi-part archive
                && !archivePartRe.IsMatch(baseName))
            {
                var reader = new PackageReader(path, true);
                var package = reader.Read();
                packagePriorities.Add(new Tuple<string, int>(path, package.Metadata.Priority));
            }
        }

        packagePriorities.Sort(
            delegate (Tuple<string, int> a, Tuple<string, int> b)
            {
                return a.Item2.CompareTo(b.Item2);
            }
        );

        // Load non-patch packages first
        foreach (var package in packagePriorities)
        {
             DiscoverPackage(package.Item1);
        }
    }

    public void DiscoverUserPackages(string gameDataPath)
    {
        foreach (var packagePath in Directory.GetFiles(gameDataPath, "*.pak"))
        {
            // Don't load 2nd, 3rd, ... parts of a multi-part archive
            if (!archivePartRe.IsMatch(packagePath))
            {
                DiscoverPackage(packagePath);
            }
        }
    }

    private void DiscoverModGoals(string modName, string modPath)
    {
        var goalPath = Path.Join(modPath, @"Story\RawFiles\Goals");
        if (!Directory.Exists(goalPath)) return;

        List<string> goalFiles = [];
        EnumerateFiles(goalFiles, goalPath, goalPath, "*.txt");

        foreach (var goalFile in goalFiles)
        {
            var fileInfo = new FilesystemFileInfo
            {
                FilesystemPath = Path.Join(goalPath, goalFile),
                FileName = goalFile
            };
            AddScriptToMod(modName, goalFile, fileInfo);
        }
    }

    private void DiscoverModStats(string modName, string modPublicPath)
    {
        var statsPath = Path.Join(modPublicPath, @"Stats\Generated\Data");
        if (!Directory.Exists(statsPath)) return;

        List<string> statFiles = [];
        EnumerateFiles(statFiles, statsPath, statsPath, "*.txt");

        foreach (var statFile in statFiles)
        {
            var fileInfo = new FilesystemFileInfo
            {
                FilesystemPath = Path.Join(statsPath, statFile),
                FileName = statFile
            };
            AddStatToMod(modName, statFile, fileInfo);
        }
    }

    private void DiscoverModGlobals(string modName, string modPath)
    {
        var globalsPath = Path.Join(modPath, "Globals");
        if (!Directory.Exists(globalsPath)) return;

        List<string> globalFiles = [];
        EnumerateFiles(globalFiles, globalsPath, globalsPath, "*.lsf");

        foreach (var globalFile in globalFiles)
        {
            var fileInfo = new FilesystemFileInfo
            {
                FilesystemPath = Path.Join(globalsPath, globalFile),
                FileName = globalFile
            };
            AddGlobalsToMod(modName, globalFile, fileInfo);
        }
    }

    private void DiscoverModLevelObjects(string modName, string modPath)
    {
        var levelsPath = Path.Join(modPath, "Levels");
        if (!Directory.Exists(levelsPath)) return;

        List<string> levelFiles = [];
        EnumerateFiles(levelFiles, levelsPath, levelsPath, "*.lsf");

        var levelObjectsRe = LevelObjectsLocalRegex();
        foreach (var levelFile in levelFiles)
        {
            var fileInfo = new FilesystemFileInfo
            {
                FilesystemPath = Path.Join(levelsPath, levelFile),
                FileName = levelFile
            };
            AddLevelObjectsToMod(modName, levelFile, fileInfo);
        }
    }

    public void DiscoverModDirectory(string modName, string modPath, string publicPath)
    {
        // Trigger mod entry creation even if there are no resources
        GetMod(modName);

        if (CollectStoryGoals)
        {
            DiscoverModGoals(modName, modPath);

            var headerPath = Path.Join(modPath, @"Story\RawFiles\story_header.div");
            if (File.Exists(headerPath))
            {
                var fileInfo = new FilesystemFileInfo
                {
                    FilesystemPath = headerPath,
                    FileName = headerPath
                };
                GetMod(modName).StoryHeaderFile = fileInfo;
            }

            var orphanQueryIgnoresPath = Path.Join(modPath, @"Story\story_orphanqueries_ignore_local.txt");
            if (File.Exists(orphanQueryIgnoresPath))
            {
                var fileInfo = new FilesystemFileInfo
                {
                    FilesystemPath = orphanQueryIgnoresPath,
                    FileName = orphanQueryIgnoresPath
                };
                GetMod(modName).OrphanQueryIgnoreList = fileInfo;
            }

            var typeCoercionWhitelistPath = Path.Join(modPath, @"Story\RawFiles\TypeCoercionWhitelist.txt");
            if (File.Exists(typeCoercionWhitelistPath))
            {
                var fileInfo = new FilesystemFileInfo
                {
                    FilesystemPath = typeCoercionWhitelistPath,
                    FileName = typeCoercionWhitelistPath
                };
                GetMod(modName).TypeCoercionWhitelistFile = fileInfo;
            }
        }

        if (CollectStats)
        {
            DiscoverModStats(modName, publicPath);
        }

        if (CollectGlobals)
        {
            DiscoverModGlobals(modName, modPath);
        }

        if (CollectLevels)
        {
            DiscoverModLevelObjects(modName, modPath);
        }
    }

    public void DiscoverMods(string gameDataPath)
    {
        var modsPath = Path.Combine(gameDataPath, "Mods");
        var publicPath = Path.Combine(gameDataPath, "Public");

        if (Directory.Exists(modsPath))
        {
            var modPaths = Directory.GetDirectories(modsPath);

            foreach (var modPath in modPaths)
            {
                if (File.Exists(Path.Combine(modPath, "meta.lsx")))
                {
                    var modName = Path.GetFileNameWithoutExtension(modPath);
                    var modPublicPath = Path.Combine(publicPath, Path.GetFileName(modPath));
                    DiscoverModDirectory(modName, modPath, modPublicPath);
                }
            }
        }
    }

    public void Discover(String gameDataPath)
    {
        if (LoadPackages)
        {
            DiscoverBuiltinPackages(gameDataPath);
        }

        DiscoverMods(gameDataPath);
    }

    [GeneratedRegex("^Mods/([^/]+)/meta\\.lsx$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex MetaRegex();

    [GeneratedRegex("^Mods/([^/]+)/Story/RawFiles/Goals/(.*\\.txt)$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex ScriptRegex();

    [GeneratedRegex("^Public/([^/]+)/Stats/Generated/Data/(.*\\.txt)$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex StatRegex();

    [GeneratedRegex("^Public/([^/]+)/(.*\\.lsx)$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex StaticLsxRegex();

    [GeneratedRegex("^Public/([^/]+)/Stats/Generated/Structure/(.*\\.txt)$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex StatStructureRegex();

    [GeneratedRegex("^Mods/([^/]+)/Story/story_orphanqueries_ignore_local\\.txt$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex OrphanQueryIgnoresRegex();

    [GeneratedRegex("^Mods/([^/]+)/Story/RawFiles/story_header\\.div$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex StoryDefinitionsRegex();

    [GeneratedRegex("^Mods/([^/]+)/Story/RawFiles/TypeCoercionWhitelist\\.txt$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex TypeCoercionWhitelistRegex();

    [GeneratedRegex("^Mods/([^/]+)/Globals/.*/.*/.*\\.lsf$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex GlobalsRegex();

    [GeneratedRegex("^Mods/([^/]+)/Levels/.*/(Characters|Items|Triggers)/.*\\.lsf$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex LevelObjectsRegex();

    [GeneratedRegex("^(.*)_[0-9]+\\.pak$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex ArchivePartRegex();

    [GeneratedRegex("^(Characters|Items|Triggers)/.*\\.lsf$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LevelObjectsLocalRegex();
}
