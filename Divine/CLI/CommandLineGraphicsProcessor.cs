using System;
using System.Collections.Generic;
using System.IO;
using Divine.Enums;
using LSLib.Granny.Model;

namespace Divine.CLI
{
    internal class CommandLineGraphicsProcessor
    {
        private static readonly Dictionary<string, bool> GraphicsOptions = CommandLineActions.GraphicsOptions;

        public static void Convert(string file = "") => ConvertResource(file);

        public static void BatchConvert() => BatchConvertResources(CommandLineActions.SourcePath, Program.argv.InputFormat);

        public static ExporterOptions UpdateExporterSettings()
        {
            ExporterOptions exporterOptions = new ExporterOptions
            {
                InputPath = CommandLineActions.SourcePath,
                OutputPath = CommandLineActions.DestinationPath,
                InputFormat = CommandLineArguments.GetExportFormatByString(Program.argv.InputFormat),
                OutputFormat = CommandLineArguments.GetExportFormatByString(Program.argv.OutputFormat),
                ExportNormals = GraphicsOptions["export-normals"],
                ExportTangents = GraphicsOptions["export-tangents"],
                ExportUVs = GraphicsOptions["export-uvs"],
                FlipUVs = GraphicsOptions["flip-uvs"],
                RecalculateNormals = GraphicsOptions["recalculate-normals"],
                RecalculateTangents = GraphicsOptions["recalculate-tangents"],
                RecalculateIWT = GraphicsOptions["recalculate-iwt"],
                BuildDummySkeleton = GraphicsOptions["build-dummy-skeleton"],
                CompactIndices = GraphicsOptions["compact-tris"],
                DeduplicateVertices = GraphicsOptions["deduplicate-vertices"],
                DeduplicateUVs = GraphicsOptions["deduplicate-uvs"],
                ApplyBasisTransforms = GraphicsOptions["apply-basis-transforms"],
                UseObsoleteVersionTag = GraphicsOptions["force-legacy-version"],
                ConformGR2Path = GraphicsOptions["conform"] && !string.IsNullOrEmpty(CommandLineActions.ConformPath) ? CommandLineActions.ConformPath : null
            };

            if (CommandLineActions.Game == Game.DivinityOriginalSin)
            {
                exporterOptions.Is64Bit = false;
                exporterOptions.AlternateSignature = false;
                exporterOptions.VersionTag = LSLib.Granny.GR2.Header.Tag_DOS;
            }
            else
            {
                exporterOptions.Is64Bit = true;
                exporterOptions.AlternateSignature = true;
                exporterOptions.VersionTag = LSLib.Granny.GR2.Header.Tag_DOSEE;
            }

            return exporterOptions;
        }

        private static void ConvertResource(string file)
        {
            Exporter exporter = new Exporter { Options = UpdateExporterSettings() };

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
                CommandLineLogger.LogFatal($"Export failed: {e.Message}{Environment.NewLine}{e.StackTrace}");
            }
        }

        private static void BatchConvertResources(string sourcePath, string inputFormat)
        {
            string[] files = Directory.GetFiles(sourcePath, $"*.{inputFormat}");

            if (files.Length == 0)
            {
                CommandLineLogger.LogFatal($"Batch convert failed: *.{inputFormat} not found in source path");
            }

            foreach (string file in files)
            {
                UpdateExporterSettings();
                Convert(file);
            }
        }
    }
}
