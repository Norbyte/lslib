using LSLib.LS;
using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace LSTools.StoryCompiler
{
    public class ModInfo
    {
        public String Name;
        public Dictionary<string, AbstractFileInfo> Scripts = new Dictionary<string, AbstractFileInfo>();
        public Dictionary<string, AbstractFileInfo> Globals = new Dictionary<string, AbstractFileInfo>();
        public Dictionary<string, AbstractFileInfo> LevelObjects = new Dictionary<string, AbstractFileInfo>();
        public AbstractFileInfo OrphanQueryIgnoreList;

        public ModInfo(String name)
        {
            Name = name;
        }
    }

    public class ModResources : IDisposable
    {
        private static readonly Regex scriptRe = new Regex("^Mods/(.*)/Story/RawFiles/Goals/(.*\\.txt)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex orphanQueryIgnoresRe = new Regex("^Mods/(.*)/Story/story_orphanqueries_ignore_local\\.txt$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex globalsRe = new Regex("^Mods/(.*)/Globals/.*/.*/.*\\.lsf$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex levelObjectsRe = new Regex("^Mods/(.*)/Levels/.*/(Characters|Items|Triggers)/.*\\.lsf$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        // Pattern for excluding subsequent parts of a multi-part archive
        private static readonly Regex archivePartRe = new Regex("^(.*)_[1-9]\\.pak$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public Dictionary<string, ModInfo> Mods = new Dictionary<string, ModInfo>();
        public AbstractFileInfo StoryHeaderFile;
        public TargetGame Game = TargetGame.DOS2;
        public bool CollectNames = false;
        public bool LoadPackages = true;
        private List<PackageReader> LoadedPackages = new List<PackageReader>();

        public void Dispose()
        {
            LoadedPackages.ForEach(p => p.Dispose());
            LoadedPackages.Clear();
        }

        private static void EnumerateFiles(List<string> paths, string rootPath, string currentPath, string pattern)
        {
            foreach (string filePath in Directory.GetFiles(currentPath, pattern))
            {
                var relativePath = filePath.Substring(rootPath.Length);
                if (relativePath[0] == '/' || relativePath[0] == '\\')
                {
                    relativePath = relativePath.Substring(1);
                }

                paths.Add(relativePath);
            }

            foreach (string directoryPath in Directory.GetDirectories(currentPath))
            {
                EnumerateFiles(paths, rootPath, directoryPath, pattern);
            }
        }

        private static void EnumerateScripts(List<string> paths, string rootPath)
        {
            var localPaths = new List<string>();
            EnumerateFiles(localPaths, rootPath, rootPath, "*.txt");
            foreach (var path in localPaths)
            {
                paths.Add(rootPath + "\\" + path);
            }
        }

        private ModInfo GetMod(string modName)
        {
            if (!Mods.TryGetValue(modName, out ModInfo mod))
            {
                mod = new ModInfo(modName);
                Mods[modName] = mod;
            }

            return mod;
        }

        private void AddScriptToMod(string modName, string scriptName, AbstractFileInfo file)
        {
            GetMod(modName).Scripts[scriptName] = file;
        }

        private void AddGlobalsToMod(string modName, string path, AbstractFileInfo file)
        {
            GetMod(modName).Globals[path] = file;
        }

        private void AddLevelObjectsToMod(string modName, string path, AbstractFileInfo file)
        {
            GetMod(modName).LevelObjects[path] = file;
        }

        private void DiscoverPackagedFile(AbstractFileInfo file)
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

            if (CollectNames)
            {
                if (file.Name.EndsWith(".lsf", StringComparison.Ordinal) && file.Name.Contains("/Globals/"))
                {
                    var match = globalsRe.Match(file.Name);
                    if (match != null && match.Success)
                    {
                        AddGlobalsToMod(match.Groups[1].Value, match.Groups[0].Value, file);
                    }
                }

                if (file.Name.EndsWith(".lsf", StringComparison.Ordinal) && file.Name.Contains("/Levels/"))
                {
                    var match = levelObjectsRe.Match(file.Name);
                    if (match != null && match.Success)
                    {
                        AddLevelObjectsToMod(match.Groups[1].Value, match.Groups[0].Value, file);
                    }
                }
            }

            if (file.Name.EndsWith("/Story/RawFiles/story_header.div", StringComparison.Ordinal))
            {
                StoryHeaderFile = new FilesystemFileInfo
                {
                    FilesystemPath = @"C:\Program Files (x86)\Steam\steamapps\common\Divinity Original Sin 2\DefEd\Data\Mods\DebugDummy_fbe41258-13a8-4f1c-b5ae-c47aa18607be\Story\RawFiles\story_header.div",
                    Name = @"C:\Program Files (x86)\Steam\steamapps\common\Divinity Original Sin 2\DefEd\Data\Mods\DebugDummy_fbe41258-13a8-4f1c-b5ae-c47aa18607be\Story\RawFiles\story_header.div"
                };
            }
        }

        private void DiscoverPackage(string packagePath)
        {
            var reader = new PackageReader(packagePath);
            LoadedPackages.Add(reader);
            var package = reader.Read();

            foreach (var file in package.Files)
            {
                DiscoverPackagedFile(file);
            }
        }

        private void DiscoverPackages(string gameDataPath)
        {
            // List of packages we won't ever load
            // These packages don't contain any mod resources, but have a large
            // file table that makes loading unneccessarily slow.
            HashSet<string> packageBlacklist = new HashSet<string>
            {
                "Effects.pak",
                "Engine.pak",
                "EngineShaders.pak",
                "Game.pak",
                "GamePlatform.pak",
                "Icons.pak",
                "LowTex.pak",
                "Materials.pak",
                "Minimaps.pak",
                "SharedSoundBanks.pak",
                "SharedSounds.pak",
                "Textures.pak"
            };
            
            // Load non-patch packages first
            foreach (var path in Directory.GetFiles(gameDataPath, "*.pak"))
            {
                var baseName = Path.GetFileName(path);
                if (!baseName.StartsWith("Patch")
                    && !packageBlacklist.Contains(baseName)
                    // Don't load 2nd, 3rd, ... parts of a multi-part archive
                    && !archivePartRe.IsMatch(baseName))
                {
                    DiscoverPackage(path);
                }
            }

            // ... and add patch files later
            foreach (var path in Directory.GetFiles(gameDataPath, "Patch*.pak"))
            {
                DiscoverPackage(path);
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
            var goalPath = modPath + @"\Story\RawFiles\Goals";
            if (!Directory.Exists(goalPath)) return;

            List<string> goalFiles = new List<string>();
            EnumerateFiles(goalFiles, goalPath, goalPath, "*.txt");

            foreach (var goalFile in goalFiles)
            {
                var fileInfo = new FilesystemFileInfo
                {
                    FilesystemPath = goalPath + "\\" + goalFile,
                    Name = goalFile
                };
                AddScriptToMod(modName, goalFile, fileInfo);
            }
        }

        private void DiscoverModGlobals(string modName, string modPath)
        {
            var globalsPath = modPath + @"\Globals";
            if (!Directory.Exists(globalsPath)) return;

            List<string> globalFiles = new List<string>();
            EnumerateFiles(globalFiles, globalsPath, globalsPath, "*.lsf");

            foreach (var globalFile in globalFiles)
            {
                var fileInfo = new FilesystemFileInfo
                {
                    FilesystemPath = globalsPath + "\\" + globalFile,
                    Name = globalFile
                };
                AddGlobalsToMod(modName, globalFile, fileInfo);
            }
        }

        private void DiscoverModLevelObjects(string modName, string modPath)
        {
            var levelsPath = modPath + @"\Levels";
            if (!Directory.Exists(levelsPath)) return;

            List<string> levelFiles = new List<string>();
            EnumerateFiles(levelFiles, levelsPath, levelsPath, "*.lsf");

            var levelObjectsRe = new Regex("^(Characters|Items|Triggers)/.*\\.lsf$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            foreach (var levelFile in levelFiles)
            {
                var fileInfo = new FilesystemFileInfo
                {
                    FilesystemPath = levelsPath + "\\" + levelFile,
                    Name = levelFile
                };
                AddLevelObjectsToMod(modName, levelFile, fileInfo);
            }
        }

        private void DiscoverModDirectory(string modName, string modPath)
        {
            DiscoverModGoals(modName, modPath);
            if (CollectNames)
            {
                DiscoverModGlobals(modName, modPath);
                DiscoverModLevelObjects(modName, modPath);
            }

            if (!LoadPackages)
            {
                var headerPath = modPath + @"\Story\RawFiles\story_header.div";
                if (File.Exists(headerPath))
                {
                    var fileInfo = new FilesystemFileInfo
                    {
                        FilesystemPath = headerPath,
                        Name = headerPath
                    };
                    StoryHeaderFile = fileInfo;
                }
            }

            var orphanQueryIgnoresPath = modPath + @"\Story\story_orphanqueries_ignore_local.txt";
            if (File.Exists(orphanQueryIgnoresPath))
            {
                var fileInfo = new FilesystemFileInfo
                {
                    FilesystemPath = orphanQueryIgnoresPath,
                    Name = orphanQueryIgnoresPath
                };
                GetMod(modName).OrphanQueryIgnoreList = fileInfo;
            }
        }

        private void DiscoverMods(string gameDataPath)
        {
            var modPaths = Directory.GetDirectories(gameDataPath + @"\\Mods");

            foreach (var modPath in modPaths)
            {
                if (Directory.Exists(modPath + @"\Story\RawFiles\Goals"))
                {
                    var modName = Path.GetFileNameWithoutExtension(modPath);
                    DiscoverModDirectory(modName, modPath);
                }
            }
        }

        public void Discover(String gameDataPath)
        {
            if (LoadPackages)
            {
                DiscoverPackages(gameDataPath);
            }

            DiscoverMods(gameDataPath);
        }
    }
}
