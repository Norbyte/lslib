using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Alphaleonis.Win32.Filesystem;
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
                "extract-packages",
                "convert-models",
                "convert-resources"
            };

            string[] packageActions =
            {
                "create-package",
                "list-package",
                "extract-single-file",
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

            if (args.Action == "create-package")
            {
                PackageVersion = CommandLineArguments.GetPackageVersion(args.PackageVersion);
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

            SourcePath = TryToValidatePath(args.Source);
            if (args.Action != "list-package")
            {
                DestinationPath = TryToValidatePath(args.Destination);
            }
            if (args.Action == "extract-single-file")
            {
                PackagedFilePath = args.PackagedPath;
            }
        }

        private static void Process(CommandLineArguments args)
        {
	        var expression = new Regex("^" + Regex.Escape(args.Expression).Replace(@"\*", ".*").Replace(@"\?", ".") + "$", RegexOptions.Singleline | RegexOptions.Compiled);

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

	        Func<AbstractFileInfo, bool> filter = obj => obj.Name.Like(expression);

	        switch (args.Action)
            {
                case "create-package":
                {
                    CommandLinePackageProcessor.Create();
                    break;
                }

                case "extract-package":
                {
                    CommandLinePackageProcessor.Extract(filter);
                    break;
                }

                case "extract-single-file":
                {
                    CommandLinePackageProcessor.ExtractSingleFile();
                    break;
                }

                case "list-package":
                {
                    CommandLinePackageProcessor.ListFiles(filter);
                    break;
                }

                case "convert-model":
                {
                    CommandLineGR2Processor.UpdateExporterSettings();
                    CommandLineGR2Processor.Convert();
                    break;
                }

                case "convert-resource":
                {
                    CommandLineDataProcessor.Convert();
                    break;
                }

                case "extract-packages":
                {
                    CommandLinePackageProcessor.BatchExtract(filter);
                    break;
                }

                case "convert-models":
                {
                    CommandLineGR2Processor.BatchConvert();
                    break;
                }

                case "convert-resources":
                {
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
                CommandLineLogger.LogFatal($"Cannot proceed without absolute path [E1]: {path}", 1);
            }

            if (uri != null && (!Path.IsPathRooted(path) || !uri.IsFile))
            {
                CommandLineLogger.LogFatal($"Cannot proceed without absolute path [E2]: {path}", 1);
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            path = Path.GetFullPath(path);

            return path;
        }
    }
}
