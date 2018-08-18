using LSLib.LS;
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

        public ModInfo(String name)
        {
            Name = name;
        }
    }

    public class ModResources
    {
        public Dictionary<string, ModInfo> Mods = new Dictionary<string, ModInfo>();
        public AbstractFileInfo StoryHeaderFile;

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

        private void DiscoverPackage(string packagePath)
        {
            var scriptRe = new Regex("^Mods/(.*)/Story/RawFiles/Goals/(.*\\.txt)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            var globalsRe = new Regex("^Mods/(.*)/Globals/.*/(.*)/.*\\.lsf$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            var levelObjectsRe = new Regex("^Mods/(.*)/Levels/(.*)/(Characters|Items|Triggers)/.*\\.lsf$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            var reader = new PackageReader(packagePath);
            var package = reader.Read();

            foreach (var file in package.Files)
            {
                if (file.Name.EndsWith(".txt", StringComparison.Ordinal) && file.Name.Contains("/Story/RawFiles/Goals"))
                {
                    var match = scriptRe.Match(file.Name);
                    if (match != null && match.Success)
                    {
                        AddScriptToMod(match.Groups[1].Value, match.Groups[2].Value, file);
                    }
                }

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

                if (file.Name.EndsWith("/Story/RawFiles/story_header.div", StringComparison.Ordinal))
                {
                    StoryHeaderFile = file;
                }
            }
        }

        private void DiscoverPackages(string gameDataPath)
        {
            // We load main packages first
            List<string> packagePaths = new List<string>
            {
                "Arena.pak",
                "GameMaster.pak",
                "Origins.pak",
                "Shared.pak"
            };

            // ... and add patch files later
            foreach (var path in Directory.GetFiles(gameDataPath, "Patch*.pak"))
            {
                packagePaths.Add(Path.GetFileName(path));
            }

            foreach (var relativePath in packagePaths)
            {
                var baseName = Path.GetFileNameWithoutExtension(relativePath);
                // Skip parts 2, 3, etc. of multi-part packages
                if (baseName[baseName.Length - 2] == '_') continue;

                var packagePath = gameDataPath + "\\" + relativePath;
                DiscoverPackage(packagePath);
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
            DiscoverModGlobals(modName, modPath);
            DiscoverModLevelObjects(modName, modPath);
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
            DiscoverPackages(gameDataPath);
            DiscoverMods(gameDataPath);
        }
    }
}
