using System;
using System.Collections.Generic;
using System.IO;
using LSLib.Granny;
using LSLib.Granny.GR2;
using LSLib.Granny.Model;
using LSLib.LS.Enums;

namespace Divine.CLI;

internal class CommandLineGR2Processor
{
    private static readonly Dictionary<string, bool> GR2Options = CommandLineActions.GR2Options;

    public static void Convert(string file = "")
    {
        ConvertResource(file);
    }

    public static void BatchConvert()
    {
        BatchConvertResources(CommandLineActions.SourcePath, Program.argv.InputFormat);
    }

    public static ExporterOptions UpdateExporterSettings()
    {
        var exporterOptions = new ExporterOptions()
        {
            InputPath = CommandLineActions.SourcePath,
            OutputPath = CommandLineActions.DestinationPath,
            InputFormat = Program.argv.InputFormat != null ? GR2Utils.FileExtensionToModelFormat("." + Program.argv.InputFormat) : GR2Utils.PathExtensionToModelFormat(CommandLineActions.SourcePath),
            OutputFormat = Program.argv.OutputFormat != null ? GR2Utils.FileExtensionToModelFormat("." + Program.argv.OutputFormat) : GR2Utils.PathExtensionToModelFormat(CommandLineActions.DestinationPath),
            FlipUVs = GR2Options["flip-uvs"],
            BuildDummySkeleton = GR2Options["build-dummy-skeleton"],
            CompactIndices = GR2Options["compact-tris"],
            DeduplicateVertices = GR2Options["deduplicate-vertices"],
            ApplyBasisTransforms = GR2Options["apply-basis-transforms"],
            UseObsoleteVersionTag = GR2Options["force-legacy-version"],
            ConformGR2Path = !string.IsNullOrEmpty(CommandLineActions.ConformPath) ? CommandLineActions.ConformPath : null,
            FlipSkeleton = GR2Options["x-flip-skeletons"],
            FlipMesh = GR2Options["x-flip-meshes"],
            TransformSkeletons = GR2Options["y-up-skeletons"],
            IgnoreUVNaN = GR2Options["ignore-uv-nan"],
            EnableQTangents = !GR2Options["disable-qtangents"]
        };

        if (exporterOptions.ConformGR2Path != null)
        {
            if(GR2Options["conform-copy"])
            {
                exporterOptions.ConformSkeletons = false;
                exporterOptions.ConformSkeletonsCopy = true;
            }
        }

        exporterOptions.LoadGameSettings(CommandLineActions.Game);

        return exporterOptions;
    }

    private static void ConvertResource(string file)
    {
        var exporter = new Exporter
        {
            Options = UpdateExporterSettings()
        };

        if (!string.IsNullOrEmpty(file))
        {
            exporter.Options.InputPath = file;
        }

#if !DEBUG
        try
        {
#endif
            exporter.Export();

            CommandLineLogger.LogInfo("Export completed successfully.");
#if !DEBUG
    }
        catch (Exception e)
        {
            CommandLineLogger.LogFatal($"Export failed: {e.Message + Environment.NewLine + e.StackTrace}", 2);
        }
#endif
    }

    private static void BatchConvertResources(string sourcePath, string inputFormat)
    {
        string[] files = Directory.GetFiles(sourcePath, $"*.{inputFormat}");

        if (files.Length == 0)
        {
            CommandLineLogger.LogFatal($"Batch convert failed: *.{inputFormat} not found in source path", 1);
        }

        foreach (string file in files)
        {
            UpdateExporterSettings();
            Convert(file);
        }
    }
}
