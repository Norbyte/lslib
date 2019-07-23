using LSLib.LS;
using LSLib.LS.Stats;
using System;
using System.Collections.Generic;
using System.IO;

namespace LSTools.StatParser
{
    class StatChecker : IDisposable
    {
        private string GameDataPath;
        private string SODPath;
        private ModResources Mods = new ModResources();
        private StatDefinitionRepository Definitions;
        private StatLoadingContext Context;
        private StatLoader Loader;

        public bool LoadPackages = true;


        public StatChecker(string gameDataPath, string sodPath)
        {
            GameDataPath = gameDataPath;
            SODPath = sodPath;
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

        private void LoadMod(string modName)
        {
            if (!Mods.Mods.TryGetValue(modName, out ModInfo mod))
            {
                throw new Exception($"Mod not found: {modName}");
            }

            LoadStats(mod);
        }

        private void LoadStatDefinitions()
        {
            Definitions = new StatDefinitionRepository();
            var enumerationsPath = Path.Combine(SODPath, "Enumerations.xml");
            Definitions.LoadEnumerations(enumerationsPath);
            var sodPath = Path.Combine(SODPath, "StatObjectDefinitions.sod");
            Definitions.LoadDefinitions(sodPath);
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

        public void Check(List<string> mods)
        {
            Context = new StatLoadingContext();

            LoadStatDefinitions();
            Context.Definitions = Definitions;

            Loader = new StatLoader(Context);

            if (mods.Count > 0)
            {
                var visitor = new ModPathVisitor(Mods)
                {
                    Game = LSLib.LS.Story.Compiler.TargetGame.DOS2DE,
                    CollectStats = true,
                    LoadPackages = LoadPackages
                };
                visitor.Discover(GameDataPath);
                
                foreach (var modName in mods)
                {
                    LoadMod(modName);
                }
            }
            
            Loader.ResolveBaseClasses();
            Loader.InstantiateEntities();

            foreach (var message in Context.Errors)
            {
                CompilationDiagnostic(message);
            }
        }
    }
}
