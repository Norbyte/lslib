using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LSLib.LS;
using LSLib.LS.Enums;

namespace Divine.CLI
{
    internal static class CommandLineActions
    {
        public static string SourcePath;
        public static string DestinationPath;
        public static string PackagedFilePath;
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
                "convert-models",
                "convert-resources"
            };

            string[] packageActionsWhereGameCanBeAutoDetected =
            {
                "extract-package",
                "extract-packages",
                "extract-single-file",
                "list-package"
            };

            string[] graphicsActions =
            {
                "convert-model",
                "convert-models"
            };

            LogLevel = CommandLineArguments.GetLogLevelByString(args.LogLevel);
            CommandLineLogger.LogDebug($"Using log level: {LogLevel}");

            // validate all source paths
            SourcePath = TryToValidatePath(args.Source);

            if (string.Equals(args.Game, "autodetect", StringComparison.OrdinalIgnoreCase))
            {
                if (!packageActionsWhereGameCanBeAutoDetected.Any(args.Action.Contains))
                {
                    CommandLineLogger.LogFatal("Cannot proceed without --game argument", 1);
                    return;
                }
            }
            else
            {
                Game = CommandLineArguments.GetGameByString(args.Game);
                CommandLineLogger.LogDebug($"Using game: {Game}");
            }
            
            // ensure these fields are populated with argument values
            DestinationPath = args.Destination;
            PackagedFilePath = args.PackagedPath;
            ConformPath = args.ConformPath;
            
            if (batchActions.Any(args.Action.Contains))
            {
                if (args.InputFormat == null || args.OutputFormat == null)
                {
                    if (args.InputFormat == null)
                    {
                        CommandLineLogger.LogFatal("Cannot perform batch action without --input-format and --output-format arguments", 1);
                    }
                }

                InputFormat = CommandLineArguments.GetResourceFormatByString(args.InputFormat);
                CommandLineLogger.LogDebug($"Using input format: {InputFormat}");

                if (!string.Equals(args.Action, "extract-packages", StringComparison.OrdinalIgnoreCase))
                {
                    OutputFormat = CommandLineArguments.GetResourceFormatByString(args.OutputFormat);
                    CommandLineLogger.LogDebug($"Using output format: {OutputFormat}");
                }
            }

            if (string.Equals(args.Action, "create-package", StringComparison.OrdinalIgnoreCase))
            {
                PackageVersion = CommandLinePackageProcessor.GetPackageVersionByGame(Game);
            }

            if (graphicsActions.Any(args.Action.Contains))
            {
                GR2Options = CommandLineArguments.GetGR2Options(args.Options);

                if (LogLevel == LogLevel.DEBUG || LogLevel == LogLevel.ALL)
                {
                    CommandLineLogger.LogDebug("Using graphics options:");

                    foreach (var x in GR2Options)
                    {
                        CommandLineLogger.LogDebug($"   {x.Key} = {x.Value}");
                    }

                }

                if (GR2Options["conform"])
                {
                    ConformPath = TryToValidatePath(args.ConformPath);
                }
            }

            // validate destination path for create-package action and batch actions
            if (args.Action == "create-package" || args.Action == "extract-packages" || batchActions.Any(args.Action.Contains))
            {
                if (string.IsNullOrWhiteSpace(args.Destination))
                {
                    FileAttributes attrs = File.GetAttributes(SourcePath);
                    DestinationPath = (attrs & FileAttributes.Directory) == FileAttributes.Directory ? SourcePath : Path.GetDirectoryName(SourcePath);
                }
                else
                {
                    DestinationPath = TryToValidatePath(args.Destination);
                }
            }
        }
        
        private static void Process(CommandLineArguments args)
        {
            Func<AbstractFileInfo, bool> filter;

            if (args.Expression != null)
            {
                Regex expression = null;
                if (args.UseRegex)
                {
                    try
                    {
                        expression = new Regex(args.Expression, RegexOptions.Singleline | RegexOptions.Compiled);
                    }
                    catch (ArgumentException)
                    {
                        CommandLineLogger.LogFatal($"Cannot parse RegEx expression: {args.Expression}", -1);
                    }
                }
                else
                {
                    expression = new Regex("^" + Regex.Escape(args.Expression).Replace(@"\*", ".*").Replace(@"\?", ".") + "$", RegexOptions.Singleline | RegexOptions.Compiled);
                }

                filter = obj => obj.Name.Like(expression);
            }
            else
            {
                filter = obj => true;
            }

            switch (args.Action)
            {
                case "create-package":
                    CommandLinePackageProcessor.Create();
                    break;

                case "extract-package":
                    CommandLinePackageProcessor.Extract(filter);
                    break;

                case "extract-single-file":
                    CommandLinePackageProcessor.ExtractSingleFile();
                    break;

                case "list-package":
                    CommandLinePackageProcessor.ListFiles(filter);
                    break;

                case "convert-model":
                    CommandLineGR2Processor.UpdateExporterSettings();
                    CommandLineGR2Processor.Convert();
                    break;

                case "convert-resource":
                    CommandLineDataProcessor.Convert();
                    break;

                case "extract-packages":
                    CommandLinePackageProcessor.BatchExtract(filter);
                    break;

                case "convert-models":
                    CommandLineGR2Processor.BatchConvert();
                    break;

                case "convert-resources":
                    CommandLineDataProcessor.BatchConvert();
                    break;

                default:
                    throw new ArgumentException($"Unhandled action: {args.Action}");
            }
        }

        private static string TryToValidatePath(string path)
        {
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
                CommandLineLogger.LogWarn($"Indeterminate path found, correcting: {path}");
            }

            if (uri != null && (!uri.IsAbsoluteUri || !uri.IsFile))
            {
                string cwd = Directory.GetCurrentDirectory();
                // ReSharper disable once AssignNullToNotNullAttribute
                path = Path.Combine(cwd, path);
                path = TryToValidatePath(path);
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            path = Path.GetFullPath(path);

            return path;
        }
    }
}
