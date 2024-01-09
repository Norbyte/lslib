using LSLib.LS;
using LSLib.LS.Stats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace LSTools.StatParser;

class StatChecker : IDisposable
{
    private string GameDataPath;
    private VFS FS;
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
            using var statStream = FS.Open(file);
            Loader.LoadStatsFromStream(file, statStream);
        }
    }

    private XmlDocument LoadXml(string path)
    {
        if (path == null) return null;

        using var stream = FS.Open(path);

        var doc = new XmlDocument();
        doc.Load(stream);
        return doc;
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
        Definitions.LoadEnumerations(FS.Open(resources.Mods["Shared"].ValueListsFile));
        Definitions.LoadDefinitions(FS.Open(resources.Mods["Shared"].ModifiersFile));
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

        if (message.Location != null)
        {
            var baseName = Path.GetFileName(message.Location.FileName);
            Console.Write($"{baseName}:{message.Location.StartLine}: ");
        }

        Console.WriteLine("[{0}] {1}", message.Code, message.Message);
        Console.ResetColor();
    }

    public void Check(List<string> mods, List<string> dependencies, List<string> packagePaths)
    {
        Context = new StatLoadingContext();

        Loader = new StatLoader(Context);

        FS = new VFS();
        if (LoadPackages)
        {
            FS.AttachGameDirectory(GameDataPath);
        }
        else
        {
            FS.AttachRoot(GameDataPath);
        }
        packagePaths.ForEach(path => FS.AttachPackage(path));
        FS.FinishBuild();

        var visitor = new ModPathVisitor(Mods, FS)
        {
            Game = LSLib.LS.Story.Compiler.TargetGame.DOS2DE,
            CollectStats = true,
            CollectGuidResources = true
        };
        visitor.Discover();

        LoadStatDefinitions(visitor.Resources);
        Context.Definitions = Definitions;

        foreach (var modName in dependencies)
        {
            LoadMod(modName);
        }

        Loader.ResolveUsageRef();
        Loader.ValidateEntries();

        Context.Errors.Clear();

        foreach (var modName in mods)
        {
            LoadMod(modName);
        }

        Loader.ResolveUsageRef();
        Loader.ValidateEntries();

        foreach (var message in Context.Errors)
        {
            CompilationDiagnostic(message);
        }
    }
}
