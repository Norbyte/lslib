using LSLib.LS;
using LSLib.LS.Stats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace LSTools.StatParser
{
    class StatChecker : IDisposable
    {
        private string GameDataPath;
        private ModResources Mods = new ModResources();
        private StatDefinitionRepository Definitions;
        private StatLoadingContext Context;
        private StatLoader Loader;

        public bool LoadPackages = true;


        public StatChecker(string gameDataPath)
        {
            GameDataPath = gameDataPath;
        }

        public void Dispose()
        {
            Mods.Dispose();
        }

        private void LoadStats(ModInfo mod)
        {
            foreach (var file in mod.Stats)
            {
                var statStream = file.Value.MakeStream();
                try
                {
                    Loader.LoadStatsFromStream(file.Key, statStream);
                }
                finally
                {
                    file.Value.ReleaseStream();
                }
            }
        }

        private XmlDocument LoadXml(AbstractFileInfo file)
        {
            if (file == null) return null;

            var stream = file.MakeStream();
            try
            {
                var doc = new XmlDocument();
                doc.Load(stream);
                return doc;
            }
            finally
            {
                file.ReleaseStream();
            }
        }

        private void LoadGuidResources(ModInfo mod)
        {
            var actionResources = LoadXml(mod.ActionResourcesFile);
            if (actionResources != null)
            {
                Loader.LoadActionResources(actionResources);
            }

            var actionResourceGroups = LoadXml(mod.ActionResourceGroupsFile);
            if (actionResourceGroups != null)
            {
                Loader.LoadActionResourceGroups(actionResourceGroups);
            }
        }

        private void LoadMod(string modName)
        {
            if (!Mods.Mods.TryGetValue(modName, out ModInfo mod))
            {
                throw new Exception($"Mod not found: {modName}");
            }

            LoadStats(mod);
            LoadGuidResources(mod);
        }

        private void LoadStatDefinitions(ModResources resources)
        {
            Definitions = new StatDefinitionRepository();
            Definitions.LoadEnumerations(resources.Mods["Shared"].ValueListsFile.MakeStream());
            Definitions.LoadDefinitions(resources.Mods["Shared"].ModifiersFile.MakeStream());
        }

        private void CompilationDiagnostic(StatLoadingError message)
        {
            if (message.Code == DiagnosticCode.StatSyntaxError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("ERR! ");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("WARN ");
            }

            if (message.Path != null)
            {
                var baseName = Path.GetFileName(message.Path);
                Console.Write($"{baseName}:{message.Line}: ");
            }

            Console.WriteLine("[{0}] {1}", message.Code, message.Message);
            Console.ResetColor();
        }

        public void Check(List<string> mods, List<string> dependencies, List<string> packagePaths)
        {
            Context = new StatLoadingContext();

            Loader = new StatLoader(Context);
            
            var visitor = new ModPathVisitor(Mods)
            {
                Game = LSLib.LS.Story.Compiler.TargetGame.DOS2DE,
                CollectStats = true,
                CollectGuidResources = true,
                LoadPackages = LoadPackages
            };
            visitor.Discover(GameDataPath);
            packagePaths.ForEach(path => visitor.DiscoverUserPackages(path));

            LoadStatDefinitions(visitor.Resources);
            Context.Definitions = Definitions;

            foreach (var modName in dependencies)
            {
                LoadMod(modName);
            }

            Loader.ResolveUsageRef();
            Loader.InstantiateEntries();

            Context.Errors.Clear();

            foreach (var modName in mods)
            {
                LoadMod(modName);
            }

            Loader.ResolveUsageRef();
            Loader.InstantiateEntries();

            foreach (var message in Context.Errors)
            {
                CompilationDiagnostic(message);
            }
        }
    }
}
