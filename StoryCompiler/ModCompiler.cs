using LSLib.LS;
using LSLib.LS.Story;
using LSLib.LS.Story.Compiler;
using LSLib.LS.Story.GoalParser;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LSTools.StoryCompiler
{
    class ModCompiler : IDisposable
    {
        class GoalScript
        {
            public string Name;
            public string Path;
            public byte[] ScriptBody;
        }

        private Logger Logger;
        private String GameDataPath;
        private Compiler Compiler = new Compiler();
        private ModResources Mods = new ModResources();
        private List<GoalScript> GoalScripts = new List<GoalScript>();
        private List<byte[]> GameObjectLSFs = new List<byte[]>();
        private bool HasErrors = false;
        private HashSet<string> TypeCoercionWhitelist;

        public bool CheckOnly = false;
        public bool CheckGameObjects = false;
        public bool LoadPackages = true;
        public bool AllowTypeCoercion = false;
        public bool OsiExtender = false;
        public TargetGame Game = TargetGame.DOS2;

        public ModCompiler(Logger logger, String gameDataPath)
        {
            Logger = logger;
            GameDataPath = gameDataPath;
        }

        public void Dispose()
        {
            Mods.Dispose();
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

        private void LoadTypeCoercionWhitelist(Stream stream)
        {
            TypeCoercionWhitelist = new HashSet<string>();
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var func = reader.ReadLine().Trim();
                    if (func.Length > 0)
                    {
                        TypeCoercionWhitelist.Add(func);
                    }
                }
            }
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
            public ConcurrentQueue<GoalScript> Inputs = new ConcurrentQueue<GoalScript>();
            public ConcurrentQueue<IRGoal> IRs = new ConcurrentQueue<IRGoal>();
        }

        private void BuildIR(IRBuildTasks tasks)
        {
            var goalLoader = new IRGenerator(Compiler.Context);
            while (tasks.Inputs.TryDequeue(out GoalScript script))
            {
                using (var stream = new MemoryStream(script.ScriptBody))
                {
                    var ast = goalLoader.ParseGoal(script.Path, stream);

                    if (ast != null)
                    {
                        var ir = goalLoader.GenerateGoalIR(ast);
                        ir.Name = script.Name;
                        tasks.IRs.Enqueue(ir);
                    }
                    else
                    {
                        var msg = new Diagnostic(goalLoader.LastLocation, MessageLevel.Error, "X00", $"Could not parse goal file " + script.Name);
                        Logger.CompilationDiagnostic(msg);
                        HasErrors = true;
                    }
                }
            }
        }

        private List<IRGoal> ParallelBuildIR()
        {
            var tasks = new IRBuildTasks();
            foreach (var script in GoalScripts)
            {
                tasks.Inputs.Enqueue(script);
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

        class PreprocessTasks
        {
            public ConcurrentQueue<GoalScript> Inputs = new ConcurrentQueue<GoalScript>();
        }

        private void Preprocess(PreprocessTasks tasks)
        {
            var preprocessor = new Preprocessor();
            while (tasks.Inputs.TryDequeue(out GoalScript script))
            {
                var scriptText = Encoding.UTF8.GetString(script.ScriptBody);
                string preprocessed = null;
                if (preprocessor.Preprocess(scriptText, ref preprocessed))
                {
                    script.ScriptBody = Encoding.UTF8.GetBytes(preprocessed);
                }
            }
        }

        private void ParallelPreprocess()
        {
            var tasks = new PreprocessTasks();
            foreach (var script in GoalScripts)
            {
                tasks.Inputs.Enqueue(script);
            }

            PreprocessTasks[] threadTasks = new[] { tasks, tasks, tasks, tasks };
            Task.WhenAll(threadTasks.Select(task => Task.Run(() => { Preprocess(task); }))).Wait();
        }

        private void LoadGameObjects(Resource resource)
        {
            if (!resource.Regions.TryGetValue("Templates", out Region templates))
            {
                // TODO - log error
                return;
            }

            if (!templates.Children.TryGetValue("GameObjects", out List<LSLib.LS.Node> gameObjects))
            {
                // TODO - log error
                return;
            }

            foreach (var gameObject in gameObjects)
            {
                if (gameObject.Attributes.TryGetValue("MapKey", out NodeAttribute objectGuid)
                    && gameObject.Attributes.TryGetValue("Name", out NodeAttribute objectName)
                    && gameObject.Attributes.TryGetValue("Type", out NodeAttribute objectType))
                {
                    LSLib.LS.Story.Compiler.ValueType type = null;
                    switch ((string)objectType.Value)
                    {
                        case "item": type = Compiler.Context.LookupType("ITEMGUID"); break;
                        case "character": type = Compiler.Context.LookupType("CHARACTERGUID"); break;
                        case "trigger": type = Compiler.Context.LookupType("TRIGGERGUID"); break;
                        default:
                            // TODO - log unknown type
                            break;
                    }

                    if (type != null)
                    {
                        var gameObjectInfo = new GameObjectInfo
                        {
                            Name = objectName.Value + "_" + objectGuid.Value,
                            Type = type
                        };
                        Compiler.Context.GameObjects[(string)objectGuid.Value] = gameObjectInfo;
                    }
                }
            }
        }

        private void LoadGlobals()
        {
            foreach (var lsf in GameObjectLSFs)
            {
                using (var stream = new MemoryStream(lsf))
                using (var reader = new LSFReader(stream))
                {
                    var resource = reader.Read();
                    LoadGameObjects(resource);
                }
            }
        }

        private void LoadGoals(ModInfo mod)
        {
            foreach (var file in mod.Scripts)
            {
                var scriptStream = file.Value.MakeStream();
                try
                {
                    using (var reader = new BinaryReader(scriptStream))
                    {
                        string path;
                        if (file.Value is PackagedFileInfo)
                        {
                            var pkgd = file.Value as PackagedFileInfo;
                            path = (pkgd.PackageStream as FileStream).Name + ":/" + pkgd.Name;
                        }
                        else
                        {
                            var fs = file.Value as FilesystemFileInfo;
                            path = fs.FilesystemPath;
                        }

                        var script = new GoalScript
                        {
                            Name = Path.GetFileNameWithoutExtension(file.Value.Name),
                            Path = path,
                            ScriptBody = reader.ReadBytes((int)scriptStream.Length)
                        };
                        GoalScripts.Add(script);
                    }
                }
                finally
                {
                    file.Value.ReleaseStream();
                }
            }
        }

        private void LoadOrphanQueryIgnores(ModInfo mod)
        {
            if (mod.OrphanQueryIgnoreList == null) return;
            
            var ignoreStream = mod.OrphanQueryIgnoreList.MakeStream();
            try
            {
                using (var reader = new StreamReader(ignoreStream))
                {
                    var ignoreRe = new Regex("^([a-zA-Z0-9_]+)\\s+([0-9]+)$");

                    while (!reader.EndOfStream)
                    {
                        string ignoreLine = reader.ReadLine();
                        var match = ignoreRe.Match(ignoreLine);
                        if (match.Success)
                        {
                            var signature = new FunctionNameAndArity(
                                match.Groups[1].Value, Int32.Parse(match.Groups[2].Value));
                            Compiler.IgnoreUnusedDatabases.Add(signature);
                        }
                    }
                }
            }
            finally
            {
                mod.OrphanQueryIgnoreList.ReleaseStream();
            }
        }

        private void LoadGameObjects(ModInfo mod)
        {
            foreach (var file in mod.Globals)
            {
                var globalStream = file.Value.MakeStream();
                try
                {
                    using (var reader = new BinaryReader(globalStream))
                    {
                        var globalLsf = reader.ReadBytes((int)globalStream.Length);
                        GameObjectLSFs.Add(globalLsf);
                    }
                }
                finally
                {
                    file.Value.ReleaseStream();
                }
            }

            foreach (var file in mod.LevelObjects)
            {
                var objectStream = file.Value.MakeStream();
                try
                {
                    using (var reader = new BinaryReader(objectStream))
                    {
                        var levelLsf = reader.ReadBytes((int)objectStream.Length);
                        GameObjectLSFs.Add(levelLsf);
                    }
                }
                finally
                {
                    file.Value.ReleaseStream();
                }
            }
        }

        private void LoadMod(string modName)
        {
            if (!Mods.Mods.TryGetValue(modName, out ModInfo mod))
            {
                throw new Exception($"Mod not found: {modName}");
            }

            LoadGoals(mod);
            LoadOrphanQueryIgnores(mod);

            if (CheckGameObjects)
            {
                LoadGameObjects(mod);
            }
        }

        public bool Compile(string outputPath, string debugInfoPath, List<string> mods)
        {
            Logger.CompilationStarted();
            HasErrors = false;
            Compiler.Game = Game;
            Compiler.AllowTypeCoercion = AllowTypeCoercion;

            if (mods.Count > 0)
            {
                Logger.TaskStarted("Discovering module files");
                var visitor = new ModPathVisitor(Mods)
                {
                    Game = Game,
                    CollectStoryGoals = true,
                    CollectGlobals = CheckGameObjects,
                    CollectLevels = CheckGameObjects,
                    LoadPackages = LoadPackages
                };
                visitor.Discover(GameDataPath);
                Logger.TaskFinished();

                Logger.TaskStarted("Loading module files");
                if (CheckGameObjects)
                {
                    var nullGameObject = new GameObjectInfo
                    {
                        Name = "NULL_00000000-0000-0000-0000-000000000000",
                        Type = Compiler.Context.LookupType("GUIDSTRING")
                    };
                    Compiler.Context.GameObjects.Add("00000000-0000-0000-0000-000000000000", nullGameObject);
                }

                foreach (var modName in mods)
                {
                    LoadMod(modName);
                }

                AbstractFileInfo storyHeaderFile = null;
                AbstractFileInfo typeCoercionWhitelistFile = null;
                var modsSearchPath = mods.ToList();
                modsSearchPath.Reverse();
                foreach (var modName in modsSearchPath)
                {
                    if (storyHeaderFile == null && Mods.Mods[modName].StoryHeaderFile != null)
                    {
                        storyHeaderFile = Mods.Mods[modName].StoryHeaderFile;
                    }

                    if (typeCoercionWhitelistFile == null && Mods.Mods[modName].TypeCoercionWhitelistFile != null)
                    {
                        typeCoercionWhitelistFile = Mods.Mods[modName].TypeCoercionWhitelistFile;
                    }
                }

                if (storyHeaderFile != null)
                {
                    var storyStream = storyHeaderFile.MakeStream();
                    LoadStoryHeaders(storyStream);
                    storyHeaderFile.ReleaseStream();
                }
                else
                {
                    Logger.CompilationDiagnostic(new Diagnostic(null, MessageLevel.Error, "X00", "Unable to locate story header file (story_header.div)"));
                    HasErrors = true;
                }

                if (typeCoercionWhitelistFile != null)
                {
                    var typeCoercionStream = typeCoercionWhitelistFile.MakeStream();
                    LoadTypeCoercionWhitelist(typeCoercionStream);
                    typeCoercionWhitelistFile.ReleaseStream();
                    Compiler.TypeCoercionWhitelist = TypeCoercionWhitelist;
                }

                Logger.TaskFinished();
            }

            if (CheckGameObjects)
            {
                Logger.TaskStarted("Loading game objects");
                LoadGlobals();
                Logger.TaskFinished();
            }
            else
            {
                Compiler.Context.Log.WarningSwitches[DiagnosticCode.UnresolvedGameObjectName] = false;
            }

            if (OsiExtender)
            {
                Logger.TaskStarted("Precompiling scripts");
                ParallelPreprocess();
                Logger.TaskFinished();
            }

            var asts = new Dictionary<String, ASTGoal>();
            var goalLoader = new IRGenerator(Compiler.Context);

            Logger.TaskStarted("Generating IR");
            var orderedGoalAsts = ParallelBuildIR();
            foreach (var goal in orderedGoalAsts)
            {
                Compiler.AddGoal(goal);
            }
            Logger.TaskFinished();


            bool updated;
            var iter = 1;
            do
            {
                Logger.TaskStarted($"Propagating rule types {iter}");
                updated = Compiler.PropagateRuleTypes();
                Logger.TaskFinished();

                if (iter++ > 10)
                {
                    Compiler.Context.Log.Error(null, DiagnosticCode.InternalError, 
                        "Maximal number of rule propagation retries exceeded");
                    break;
                }
            } while (updated);

            Logger.TaskStarted("Checking for unresolved references");
            Compiler.VerifyIR();
            Logger.TaskFinished();

            foreach (var message in Compiler.Context.Log.Log)
            {
                Logger.CompilationDiagnostic(message);
                if (message.Level == MessageLevel.Error)
                {
                    HasErrors = true;
                }
            }

            if (!HasErrors && !CheckOnly)
            {
                Logger.TaskStarted("Generating story nodes");
                var emitter = new StoryEmitter(Compiler.Context);
                if (debugInfoPath != null)
                {
                    emitter.EnableDebugInfo();
                }

                var story = emitter.EmitStory();
                Logger.TaskFinished();

                Logger.TaskStarted("Saving story binary");
                using (var file = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    var writer = new StoryWriter();
                    writer.Write(file, story);
                }
                Logger.TaskFinished();

                if (debugInfoPath != null)
                {
                    Logger.TaskStarted("Saving debug info");
                    using (var file = new FileStream(debugInfoPath, FileMode.Create, FileAccess.Write))
                    {
                        var writer = new DebugInfoSaver();
                        writer.Save(file, emitter.DebugInfo);
                    }
                    Logger.TaskFinished();
                }
            }

            Logger.CompilationFinished(!HasErrors);
            return !HasErrors;
        }
    }
}
