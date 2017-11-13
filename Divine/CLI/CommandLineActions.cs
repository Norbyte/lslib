using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Divine.Enums;
using LSLib.LS.Enums;

namespace Divine.CLI
{
    internal class CommandLineActions
    {
        public static string SourcePath;
        public static string DestinationPath;
        public static string ConformPath;

        public static Game Game;
        public static LogLevel LogLevel;
        public static ResourceFormat InputFormat;
        public static ResourceFormat OutputFormat;
        public static PackageVersion PackageVersion;
        public static Dictionary<string, bool> GR2Options;

        // TODO: OSI support

        public static void Run(CommandLineArguments args)
        {
            SetUpAndValidate(args);
            Process(args);
        }

        private static void SetUpAndValidate(CommandLineArguments args)
        {
            string[] batchActions =
            {
                "extract-packages",
                "convert-models",
                "convert-resources"
            };

            string[] packageActions =
            {
                "create-package",
                "extract-package",
                "extract-packages"
            };

            string[] graphicsActions =
            {
                "convert-model",
                "convert-models"
            };

            LogLevel = CommandLineArguments.GetLogLevelByString(args.LogLevel);
            CommandLineLogger.LogDebug($"Using log level: {LogLevel}");

            Game = CommandLineArguments.GetGameByString(args.Game);
            CommandLineLogger.LogDebug($"Using game: {Game}");

            if (batchActions.Any(args.Action.Contains))
            {
                if (args.InputFormat == null || args.OutputFormat == null)
                {
                    if (args.InputFormat == null && args.Action != "extract-packages")
                    {
                        CommandLineLogger.LogFatal("Cannot perform batch action without --input-format and --output-format arguments", 1);
                    }
                }

                InputFormat = CommandLineArguments.GetResourceFormatByString(args.InputFormat);
                CommandLineLogger.LogDebug($"Using input format: {InputFormat}");

                if (args.Action != "extract-packages")
                {
                    OutputFormat = CommandLineArguments.GetResourceFormatByString(args.OutputFormat);
                    CommandLineLogger.LogDebug($"Using output format: {OutputFormat}");
                }
            }

            if (packageActions.Any(args.Action.Contains))
            {
                PackageVersion = CommandLineArguments.GetPackageVersion(args.PackageVersion);
                CommandLineLogger.LogDebug($"Using package version: {PackageVersion}");
            }

            if (graphicsActions.Any(args.Action.Contains))
            {
                GR2Options = CommandLineArguments.GetGR2Options(args.Options);
                CommandLineLogger.LogDebug($"Using graphics options: {GR2Options}");

                if (GR2Options["conform"])
                {
                    ConformPath = TryToValidatePath(args.ConformPath);
                }
            }

            SourcePath = TryToValidatePath(args.Source);
            DestinationPath = TryToValidatePath(args.Destination);
        }

        private static void Process(CommandLineArguments args)
        {
            switch (args.Action)
            {
                case "create-package":
                    CommandLinePackageProcessor.Create();
                    break;

                case "extract-package":
                    CommandLinePackageProcessor.Extract();
                    break;

                case "convert-model":
                    CommandLineGR2Processor.UpdateExporterSettings();
                    CommandLineGR2Processor.Convert();
                    break;

                case "convert-resource":
                    CommandLineDataProcessor.Convert();
                    break;

                case "extract-packages":
                    CommandLinePackageProcessor.BatchExtract();
                    break;

                case "convert-models":
                    CommandLineGR2Processor.BatchConvert();
                    break;

                case "convert-resources":
                    CommandLineDataProcessor.BatchConvert();
                    break;
            }
        }

        public static string TryToValidatePath(string path)
        {
            const int maxPath = 248;

            CommandLineLogger.LogDebug($"Using path: {path}");

            if (string.IsNullOrWhiteSpace(path))
            {
                CommandLineLogger.LogFatal($"Cannot parse path from input: {path}", 1);
            }

            Uri uri = null;
            try
            {
                Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out uri);
            }
            catch (InvalidOperationException)
            {
                CommandLineLogger.LogFatal($"Cannot proceed without absolute path [E1]: {path}", 1);
            }

            if (uri != null && (!Path.IsPathRooted(path) || !uri.IsFile))
            {
                CommandLineLogger.LogFatal($"Cannot proceed without absolute path [E2]: {path}", 1);
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            path = Path.GetFullPath(path);

            if (path.Length > maxPath)
            {
                CommandLineLogger.LogFatal($"Cannot proceed with path exceeding {maxPath} characters: {path}", 1);
            }

            return path;
        }
    }
}
