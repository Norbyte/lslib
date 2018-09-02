using LSLib.LS;
using LSLib.LS.Story;
using LSLib.LS.Story.Compiler;
using LSLib.LS.Story.GoalParser;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LSTools.StoryCompiler
{
    class ModCompiler
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

        public bool CheckOnly = false;
        public bool CheckGameObjects = false;

        public ModCompiler(Logger logger, String gameDataPath)
        {
            Logger = logger;
            GameDataPath = gameDataPath;
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
                    var ir = goalLoader.GenerateGoalIR(ast);
                    ir.Name = script.Name;
                    tasks.IRs.Enqueue(ir);
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
            if (CheckGameObjects)
            {
                LoadGameObjects(mod);
            }
        }

        public bool Compile(string outputPath, string debugInfoPath, List<string> mods)
        {
            Logger.CompilationStarted();
            if (mods.Count > 0)
            {
                Logger.TaskStarted("Discovering module files");
                Mods.CollectNames = CheckGameObjects;
                Mods.Discover(GameDataPath);
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
                Logger.TaskFinished();
            }

            var stream = Mods.StoryHeaderFile.MakeStream();
            LoadStoryHeaders(stream);
            Mods.StoryHeaderFile.ReleaseStream();

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

            if (!hasErrors && !CheckOnly)
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

            Logger.CompilationFinished(!hasErrors);
            return !hasErrors;
        }
    }
}
