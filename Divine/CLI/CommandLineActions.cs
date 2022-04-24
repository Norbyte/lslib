using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LSLib.LS;
using LSLib.LS.Enums;

namespace Divine.CLI
{
    internal class CommandLineActions
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

            if (args.Game == "autodetect")
            {
                if (!packageActionsWhereGameCanBeAutoDetected.Any(args.Action.Contains))
                {
                    CommandLineLogger.LogFatal("Cannot proceed without --game argument", 1);
                }
            }
            else
            {
                Game = CommandLineArguments.GetGameByString(args.Game);
                CommandLineLogger.LogDebug($"Using game: {Game}");
            }
            
            // ensure these fields are set to passed argv
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

                if (args.Action != "extract-packages")
                {
                    OutputFormat = CommandLineArguments.GetResourceFormatByString(args.OutputFormat);
                    CommandLineLogger.LogDebug($"Using output format: {OutputFormat}");
                }
            }

            if (args.Action == "create-package")
            {
                switch (Game)
                {
                    case Game.DivinityOriginalSin:
                        PackageVersion = PackageVersion.V7;
                        break;
                    case Game.DivinityOriginalSinEE:
                        PackageVersion = PackageVersion.V9;
                        break;
                    case Game.DivinityOriginalSin2:
                        PackageVersion = PackageVersion.V10;
                        break;
                    case Game.DivinityOriginalSin2DE:
                        PackageVersion = PackageVersion.V13;
                        break;
                    case Game.BaldursGate3:
                        PackageVersion = PackageVersion.V16;
                        break;
                    default:
                        throw new ArgumentException($"Unknown game: \"{Game}\"");
                }

                CommandLineLogger.LogDebug($"Using package version: {PackageVersion}");
            }

            if (graphicsActions.Any(args.Action.Contains))
            {
                GR2Options = CommandLineArguments.GetGR2Options(args.Options);

                if(LogLevel == LogLevel.DEBUG || LogLevel == LogLevel.ALL)
                {
                    CommandLineLogger.LogDebug("Using graphics options:");

                    foreach (KeyValuePair<string, bool> x in GR2Options)
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
                    var attrs = File.GetAttributes(SourcePath);
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
                {
                    // requires:
                    //   SourcePath      = directory
                    //   DestinationPath = new file path - no validation
                    CommandLinePackageProcessor.Create();
                    break;
                }

                case "extract-package":
                {
                    // requires:
                    //   SourcePath      = file path (package)
                    //   DestinationPath = directory path - no validation
                    CommandLinePackageProcessor.Extract(filter);
                    break;
                }

                case "extract-single-file":
                {
                    // requires:
                    //   SourcePath      = file path (package)
                    //   DestinationPath = file path (file to write) - no validation
                    CommandLinePackageProcessor.ExtractSingleFile();
                    break;
                }

                case "list-package":
                {
                    // requires:
                    //   SourcePath      = file path (package)
                    CommandLinePackageProcessor.ListFiles(filter);
                    break;
                }

                case "convert-model":
                {
                    // requires:
                    //   SourcePath      = original file path
                    //   DestinationPath = new file path - no validation
                    CommandLineGR2Processor.UpdateExporterSettings();
                    CommandLineGR2Processor.Convert();
                    break;
                }

                case "convert-resource":
                {
                    // requires:
                    //   SourcePath      = original file path
                    //   DestinationPath = new file path - no validation
                    CommandLineDataProcessor.Convert();
                    break;
                }

                case "extract-packages":
                {
                    // requires:
                    //   SourcePath      = directory path (contains packages)
                    //   DestinationPath = directory path - no validation
                    CommandLinePackageProcessor.BatchExtract(filter);
                    break;
                }

                case "convert-models":
                {
                    // requires:
                    //   SourcePath      = directory path (contains models)
                    //   DestinationPath = directory path - no validation
                    CommandLineGR2Processor.BatchConvert();
                    break;
                }

                case "convert-resources":
                {
                    // requires:
                    //   SourcePath      = directory path (contains resources)
                    //   DestinationPath = directory path - no validation
                    CommandLineDataProcessor.BatchConvert();
                    break;
                }

                default:
                {
                    throw new ArgumentException($"Unhandled action: {args.Action}");
                }
            }
        }

        public static string TryToValidatePath(string path)
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
                CommandLineLogger.LogDebug($"Indeterminate path found, correcting: {path}");
            }

            if (uri != null && (!uri.IsAbsoluteUri || !uri.IsFile))
            {
                var cwd = Directory.GetCurrentDirectory();
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
