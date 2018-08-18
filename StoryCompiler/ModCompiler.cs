using LSLib.LS;
using LSLib.LS.Story;
using LSLib.LS.Story.Compiler;
using LSLib.LS.Story.GoalParser;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LSTools.StoryCompiler
{
    class ModCompiler
    {
        private Logger Logger;
        private String GameDataPath;
        private Compiler Compiler = new Compiler();

        private Dictionary<string, Dictionary<string, AbstractFileInfo>> ModFiles = new Dictionary<string, Dictionary<string, AbstractFileInfo>>();
        private AbstractFileInfo StoryHeaderFile;
        private List<AbstractFileInfo> GoalPaths = new List<AbstractFileInfo>();
        private Mutex FileReaderMutex = new Mutex();

        public ModCompiler(Logger logger, String gameDataPath)
        {
            Logger = logger;
            GameDataPath = gameDataPath;
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

        private void AddScriptToMod(string modName, string scriptName, AbstractFileInfo file)
        {
            Dictionary<string, AbstractFileInfo> modFiles;
            if (!ModFiles.TryGetValue(modName, out modFiles))
            {
                modFiles = new Dictionary<string, AbstractFileInfo>();
                ModFiles.Add(modName, modFiles);
            }

            modFiles[scriptName] = file;
        }

        private void DiscoverPackage(string packagePath)
        {
            var packageRe = new Regex("^Mods/(.*)/Story/RawFiles/Goals/(.*)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            var reader = new PackageReader(packagePath);
            var package = reader.Read();
                
            foreach (var file in package.Files)
            {
                if (file.Name.EndsWith(".txt", StringComparison.Ordinal) && file.Name.Contains("/Story/RawFiles/Goals"))
                {
                    var match = packageRe.Match(file.Name);
                    if (match != null && match.Success)
                    {
                        AddScriptToMod(match.Groups[1].Value, match.Groups[2].Value, file);
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

        private void DiscoverModDirectory(string modName, string modPath)
        {
            List<string> goalFiles = new List<string>();
            var goalPath = modPath + @"\Story\RawFiles\Goals";
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

        private void DiscoverMods()
        {
            if (GameDataPath != null)
            {
                Logger.TaskStarted("Looking for goal files");
                DiscoverPackages(GameDataPath);
                DiscoverMods(GameDataPath);
                Logger.TaskFinished();
            }
        }

        private void LoadStoryHeaders(Stream stream)
        {
            var hdrLoader = new StoryHeaderLoader(Compiler.Context);
            var declarations = hdrLoader.ParseHeader(stream);
            if (declarations == null)
            {
                throw new Exception("Failed to parse story header file");
            }

            hdrLoader.LoadHeader(declarations);
        }
        
        public void SetWarningOptions(Dictionary<string, bool> options)
        {
            foreach (var option in options)
            {
                Compiler.Context.Log.WarningSwitches[option.Key] = option.Value;
            }
        }

        class IRBuildTasks
        {
            public ConcurrentQueue<AbstractFileInfo> InputPaths = new ConcurrentQueue<AbstractFileInfo>();
            public ConcurrentQueue<IRGoal> IRs = new ConcurrentQueue<IRGoal>();
        }

        private void BuildIR(IRBuildTasks tasks)
        {
            var goalLoader = new IRGenerator(Compiler.Context);
            while (tasks.InputPaths.TryDequeue(out AbstractFileInfo path))
            {
                var goalName = Path.GetFileNameWithoutExtension(path.Name);
                try
                {
                    FileReaderMutex.WaitOne();
                    Stream stream;
                    try
                    {
                        stream = path.MakeStream();
                    }
                    finally
                    {
                        FileReaderMutex.ReleaseMutex();
                    }

                    var ast = goalLoader.ParseGoal(path.Name, stream);

                    var ir = goalLoader.GenerateGoalIR(ast);
                    ir.Name = goalName;
                    tasks.IRs.Enqueue(ir);
                }
                finally
                {
                    path.ReleaseStream();
                }
            }
        }

        private List<IRGoal> ParallelBuildIR()
        {
            var tasks = new IRBuildTasks();
            foreach (var path in GoalPaths)
            {
                tasks.InputPaths.Enqueue(path);
            }

            IRBuildTasks[] threadTasks = new[] { tasks, tasks, tasks, tasks };
            Task.WhenAll(threadTasks.Select(task => Task.Run(() => { BuildIR(task); }))).Wait();

            var sorted = new SortedDictionary<string, IRGoal>();
            while (tasks.IRs.TryDequeue(out IRGoal goal))
            {
                sorted[goal.Name] = goal;
            }

            return sorted.Values.ToList();
        }

        public bool Compile(string outputPath, List<string> mods)
        {
            Logger.CompilationStarted();
            if (mods.Count > 0)
            {
                DiscoverMods();

                foreach (var modName in mods)
                {
                    Dictionary<string, AbstractFileInfo> modFileList;
                    if (ModFiles.TryGetValue(modName, out modFileList))
                    {
                        foreach (var file in modFileList)
                        {
                            GoalPaths.Add(file.Value);
                        }
                    }
                    else
                    {
                        throw new Exception($"Mod not found: {modName}");
                    }
                }
            }

            var stream = StoryHeaderFile.MakeStream();
            LoadStoryHeaders(stream);
            StoryHeaderFile.ReleaseStream();

            var asts = new Dictionary<String, ASTGoal>();
            var goalLoader = new IRGenerator(Compiler.Context);

            Logger.TaskStarted("Generating IR");
            var orderedGoalAsts = ParallelBuildIR();
            foreach (var goal in orderedGoalAsts)
            {
                Compiler.AddGoal(goal);
            }
            Logger.TaskFinished();


            Logger.TaskStarted("Propagating rule types");
            // TODO - this should be changed to dynamic pass count detection
            Compiler.PropagateRuleTypes();
            Compiler.PropagateRuleTypes();
            Compiler.PropagateRuleTypes();
            Logger.TaskFinished();

            Logger.TaskStarted("Checking for unresolved references");
            Compiler.VerifyIR();
            Logger.TaskFinished();

            bool hasErrors = false;
            foreach (var message in Compiler.Context.Log.Log)
            {
                Logger.CompilationDiagnostic(message);
                if (message.Level == MessageLevel.Error)
                {
                    hasErrors = true;
                }
            }

            if (!hasErrors)
            {
                Logger.TaskStarted("Generating story nodes");
                var emitter = new StoryEmitter(Compiler.Context);
                var story = emitter.EmitStory();
                Logger.TaskFinished();

                Logger.TaskStarted("Saving story binary");
                using (var file = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    var writer = new StoryWriter();
                    writer.Write(file, story);
                }
                Logger.TaskFinished();
            }

            Logger.CompilationFinished(!hasErrors);
            return !hasErrors;
        }
    }
}
