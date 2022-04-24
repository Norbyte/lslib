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
                Constants.CONVERT_MODELS,
                Constants.CONVERT_RESOURCES
            };

            string[] packageActionsWhereGameCanBeAutoDetected =
            {
                Constants.EXTRACT_PACKAGE,
                Constants.EXTRACT_PACKAGES,
                Constants.EXTRACT_SINGLE_FILE,
                Constants.LIST_PACKAGE
            };

            string[] graphicsActions =
            {
                Constants.CONVERT_MODEL,
                Constants.CONVERT_MODELS
            };

            LogLevel = CommandLineArguments.GetLogLevelByString(args.LogLevel);
            CommandLineLogger.LogDebug($"Using log level: {LogLevel}");

            // validate all source paths
            SourcePath = TryToValidatePath(args.Source);

            if (string.Equals(args.Game, Constants.AUTODETECT, StringComparison.OrdinalIgnoreCase))
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

                if (!string.Equals(args.Action, Constants.EXTRACT_PACKAGES, StringComparison.OrdinalIgnoreCase))
                {
                    OutputFormat = CommandLineArguments.GetResourceFormatByString(args.OutputFormat);
                    CommandLineLogger.LogDebug($"Using output format: {OutputFormat}");
                }
            }

            if (string.Equals(args.Action, Constants.CREATE_PACKAGE, StringComparison.OrdinalIgnoreCase))
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
            if (string.Equals(args.Action, Constants.CREATE_PACKAGE, StringComparison.OrdinalIgnoreCase) || 
                string.Equals(args.Action, Constants.EXTRACT_PACKAGES, StringComparison.OrdinalIgnoreCase) ||
                batchActions.Any(args.Action.Contains))
            {
                if (string.IsNullOrWhiteSpace(args.Destination))
                {
                    DestinationPath = PathUtils.IsDir(SourcePath) ? SourcePath : Path.GetDirectoryName(SourcePath);
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
                case Constants.CREATE_PACKAGE:
                    CommandLinePackageProcessor.Create();
                    break;

                case Constants.EXTRACT_PACKAGE:
                    CommandLinePackageProcessor.Extract(filter);
                    break;

                case Constants.EXTRACT_SINGLE_FILE:
                    CommandLinePackageProcessor.ExtractSingleFile();
                    break;

                case Constants.LIST_PACKAGE:
                    CommandLinePackageProcessor.ListFiles(filter);
                    break;

                case Constants.CONVERT_MODEL:
                    CommandLineGR2Processor.UpdateExporterSettings();
                    CommandLineGR2Processor.Convert();
                    break;

                case Constants.CONVERT_RESOURCE:
                    CommandLineDataProcessor.Convert();
                    break;

                case Constants.EXTRACT_PACKAGES:
                    CommandLinePackageProcessor.BatchExtract(filter);
                    break;

                case Constants.CONVERT_MODELS:
                    CommandLineGR2Processor.BatchConvert();
                    break;

                case Constants.CONVERT_RESOURCES:
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
