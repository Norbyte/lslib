using System;
using System.Collections.Generic;
using System.IO;
using LSLib.Granny.GR2;
using LSLib.Granny.Model;
using LSLib.LS.Enums;

namespace Divine.CLI
{
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
            ExporterOptions exporterOptions = new ExporterOptions ()
            {
                InputPath = CommandLineActions.SourcePath,
                OutputPath = CommandLineActions.DestinationPath,
                InputFormat = CommandLineArguments.GetExportFormatByString(Program.argv.InputFormat),
                OutputFormat = CommandLineArguments.GetExportFormatByString(Program.argv.OutputFormat),
                ExportNormals = GR2Options["export-normals"],
                ExportTangents = GR2Options["export-tangents"],
                ExportUVs = GR2Options["export-uvs"],
                ExportColors = GR2Options["export-colors"],
                FlipUVs = GR2Options["flip-uvs"],
                RecalculateNormals = GR2Options["recalculate-normals"],
                RecalculateTangents = GR2Options["recalculate-tangents"],
                RecalculateIWT = GR2Options["recalculate-iwt"],
                BuildDummySkeleton = GR2Options["build-dummy-skeleton"],
                CompactIndices = GR2Options["compact-tris"],
                DeduplicateVertices = GR2Options["deduplicate-vertices"],
                DeduplicateUVs = GR2Options["deduplicate-uvs"],
                ApplyBasisTransforms = GR2Options["apply-basis-transforms"],
                UseObsoleteVersionTag = GR2Options["force-legacy-version"],
                ConformGR2Path = GR2Options["conform"] && !string.IsNullOrEmpty(CommandLineActions.ConformPath) ? CommandLineActions.ConformPath : null
            };

			exporterOptions.LoadGameSettings(CommandLineActions.Game);

            return exporterOptions;
        }

        private static void ConvertResource(string file)
        {
            Exporter exporter = new Exporter
            {
                Options = UpdateExporterSettings()
            };

            if (!string.IsNullOrEmpty(file))
            {
                exporter.Options.InputPath = file;
            }

            try
            {
                exporter.Export();

                CommandLineLogger.LogInfo("Export completed successfully.");
            }
            catch (Exception e)
            {
                CommandLineLogger.LogFatal($"Export failed: {e.Message}", 2);
                CommandLineLogger.LogTrace($"{e.StackTrace}");
            }
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
}
