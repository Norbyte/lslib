using LSLib.LS;
using LSLib.LS.Enums;
using LSLib.LS.Story;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ConverterApp;

public delegate void DumugDumperReportProgress(int percentage, string statusText);

public class DebugDumperTask
{
    private Package SavePackage;
    private Resource SaveMeta;
    private Resource SaveGlobals;
    private Story SaveStory;

    public Game GameVersion { get; set; }
    public string SaveFilePath { get; set; }
    public string ExtractionPath { get; set; }
    public string DataDumpPath { get; set; }

    // General savegame dumping settings
    public bool ExtractAll { get; set; }
    public bool ConvertToLsx { get; set; }
    public bool DumpModList { get; set; }

    // Behavior variable dumping settings
    public bool DumpGlobalVars { get; set; }
    public bool DumpCharacterVars { get; set; }
    public bool DumpItemVars { get; set; }
    public bool IncludeDeletedVars { get; set; }
    public bool IncludeLocalScopes { get; set; }

    // Story dump settings
    public bool DumpStoryDatabases { get; set; }
    public bool DumpStoryGoals { get; set; }
    public bool IncludeUnnamedDatabases { get; set; }

    public event DumugDumperReportProgress ReportProgress;

    public DebugDumperTask()
    {
        ExtractAll = true;
        ConvertToLsx = true;
        DumpModList = true;

        DumpGlobalVars = true;
        DumpCharacterVars = true;
        DumpItemVars = true;
        IncludeDeletedVars = false;
        IncludeLocalScopes = false;
        // TODO ------------------ RE-IMPORTABLE VARS/DBS FORMAT ----------------------

        DumpStoryDatabases = true;
        DumpStoryGoals = true;
        IncludeUnnamedDatabases = false;
    }

    private void DoExtractPackage()
    {
        var packager = new Packager();
        packager.ProgressUpdate = (file, numerator, denominator) => {
            ReportProgress(5 + (int)(numerator * 15 / denominator), "Extracting: " + file);
        };
        packager.UncompressPackage(SavePackage, ExtractionPath);
    }

    private void DoLsxConversion()
    {
        var conversionParams = ResourceConversionParameters.FromGameVersion(GameVersion);
        var loadParams = ResourceLoadParameters.FromGameVersion(GameVersion);

        var lsfList = SavePackage.Files.Where(p => p.Name.EndsWith(".lsf"));
        var numProcessed = 0;
        foreach (var lsf in lsfList)
        {
            var lsfPath = Path.Combine(ExtractionPath, lsf.Name);
            var lsxPath = Path.Combine(ExtractionPath, lsf.Name.Substring(0, lsf.Name.Length - 4) + ".lsx");

            ReportProgress(20 + (numProcessed * 30 / lsfList.Count()), "Converting to LSX: " + lsf.Name);
            var resource = ResourceUtils.LoadResource(lsfPath, ResourceFormat.LSF, loadParams);
            ResourceUtils.SaveResource(resource, lsxPath, ResourceFormat.LSX, conversionParams);
            numProcessed++;
        }
    }

    private Resource LoadPackagedResource(string path)
    {
        var fileInfo = SavePackage.Files.FirstOrDefault(p => p.Name.ToLowerInvariant() == path);
        if (fileInfo == null)
        {
            throw new ArgumentException($"Could not locate file in package: '{path}");
        }

        Resource resource;
        using var rsrcStream = fileInfo.CreateContentReader();
        using (var rsrcReader = new LSFReader(rsrcStream))
        {
            resource = rsrcReader.Read();
        }

        return resource;
    }

    private void DumpMods(string outputPath)
    {
        using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        using (var writer = new StreamWriter(outputStream))
        {
            var meta = SaveMeta.Regions["MetaData"].Children["MetaData"][0];
            var moduleDescs = meta.Children["ModuleSettings"][0].Children["Mods"][0].Children["ModuleShortDesc"];
            foreach (var modDesc in moduleDescs)
            {
                var folder = (string)modDesc.Attributes["Folder"].Value;
                var name = (string)modDesc.Attributes["Name"].Value;
                PackedVersion version;
                if (modDesc.Attributes.ContainsKey("Version64"))
                {
                    var versionNum = (Int64)modDesc.Attributes["Version64"].Value;
                    version = PackedVersion.FromInt64(versionNum);
                }
                else
                {
                    var versionNum = (Int32)modDesc.Attributes["Version"].Value;
                    version = PackedVersion.FromInt32(versionNum);
                }

                writer.WriteLine($"{name} (v{version.Major}.{version.Minor}.{version.Revision}.{version.Build}) @ {folder}");
            }
        }
    }

    private void DumpVariables(string outputPath, bool globals, bool characters, bool items)
    {
        using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            var varDumper = new VariableDumper(outputStream);
            varDumper.IncludeDeletedVars = IncludeDeletedVars;
            varDumper.IncludeLocalScopes = IncludeLocalScopes;
            if (varDumper.Load(SaveGlobals))
            {
                if (globals)
                {
                    varDumper.DumpGlobals();
                }

                if (characters)
                {
                    varDumper.DumpCharacters();
                }

                if (items)
                {
                    varDumper.DumpItems();
                }
            }
        }
    }

    private void DumpGoals()
    {
        ReportProgress(80, "Dumping story ...");
        string debugPath = Path.Combine(DataDumpPath, "GoalsDebug.log");
        using (var debugFile = new FileStream(debugPath, FileMode.Create, FileAccess.Write))
        using (var writer = new StreamWriter(debugFile))
        {
            SaveStory.DebugDump(writer);
        }

        ReportProgress(85, "Dumping story goals ...");
        string goalsPath = Path.Combine(DataDumpPath, "Goals");
        FileManager.TryToCreateDirectory(Path.Combine(goalsPath, "Dummy"));

        string unassignedPath = Path.Combine(goalsPath, "UNASSIGNED_RULES.txt");
        using (var goalFile = new FileStream(unassignedPath, FileMode.Create, FileAccess.Write))
        using (var writer = new StreamWriter(goalFile))
        {
            var dummyGoal = new Goal(SaveStory)
            {
                ExitCalls = new List<Call>(),
                InitCalls = new List<Call>(),
                ParentGoals = new List<GoalReference>(),
                SubGoals = new List<GoalReference>(),
                Name = "UNASSIGNED_RULES",
                Index = 0
            };
            dummyGoal.MakeScript(writer, SaveStory);
        }

        foreach (KeyValuePair<uint, Goal> goal in SaveStory.Goals)
        {
            string filePath = Path.Combine(goalsPath, $"{goal.Value.Name}.txt");
            using (var goalFile = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(goalFile))
            {
                goal.Value.MakeScript(writer, SaveStory);
            }
        }
    }

    private void RunTasks()
    {
        if (ExtractAll)
        {
            DoExtractPackage();
        }

        if (ConvertToLsx)
        {
            DoLsxConversion();
        }

        FileManager.TryToCreateDirectory(Path.Combine(DataDumpPath, "Dummy"));

        ReportProgress(50, "Loading meta.lsf ...");
        SaveMeta = LoadPackagedResource("meta.lsf");

        ReportProgress(52, "Loading globals.lsf ...");
        SaveGlobals = LoadPackagedResource("globals.lsf");

        ReportProgress(60, "Dumping mod list ...");
        if (DumpModList)
        {
            var modListPath = Path.Combine(DataDumpPath, "ModList.txt");
            DumpMods(modListPath);
        }

        ReportProgress(62, "Dumping variables ...");
        if (DumpGlobalVars)
        {
            var varsPath = Path.Combine(DataDumpPath, "GlobalVars.txt");
            DumpVariables(varsPath, true, false, false);
        }

        if (DumpCharacterVars)
        {
            var varsPath = Path.Combine(DataDumpPath, "CharacterVars.txt");
            DumpVariables(varsPath, false, true, false);
        }

        if (DumpItemVars)
        {
            var varsPath = Path.Combine(DataDumpPath, "ItemVars.txt");
            DumpVariables(varsPath, false, false, true);
        }

        ReportProgress(70, "Loading story ...");
        var storySave = SavePackage.Files.FirstOrDefault(p => p.Name == "StorySave.bin");
        Stream storyStream;
        if (storySave != null)
        {
            var bin = storySave.CreateContentReader();
            storyStream = new MemoryStream();
            bin.CopyTo(storyStream);
            storyStream.Position = 0;
        }
        else
        {
            LSLib.LS.Node storyNode = SaveGlobals.Regions["Story"].Children["Story"][0];
            storyStream = new MemoryStream(storyNode.Attributes["Story"].Value as byte[]);
        }

        var reader = new StoryReader();
        SaveStory = reader.Read(storyStream);

        if (DumpStoryGoals)
        {
            DumpGoals();
        }

        if (DumpStoryDatabases)
        {
            ReportProgress(90, "Dumping databases ...");
            var dbDumpPath = Path.Combine(DataDumpPath, "Databases.txt");
            using (var dbDumpStream = new FileStream(dbDumpPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var dbDumper = new DatabaseDumper(dbDumpStream);
                dbDumper.DumpUnnamedDbs = IncludeUnnamedDatabases;
                dbDumper.DumpAll(SaveStory);
            }
        }

        ReportProgress(100, "");
    }

    public void Run()
    {
        ReportProgress(0, "Reading package ...");

        var packageReader = new PackageReader();
        using var savePackage = packageReader.Read(SaveFilePath);

        SavePackage = savePackage;
        var abstractFileInfo = SavePackage.Files.FirstOrDefault(p => p.Name.ToLowerInvariant() == "globals.lsf");
        if (abstractFileInfo == null)
        {
            MessageBox.Show("The specified package is not a valid savegame (globals.lsf not found)", "Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        RunTasks();
        SavePackage = null;

        MessageBox.Show($"Savegame dumped to {DataDumpPath}.");
    }
}
