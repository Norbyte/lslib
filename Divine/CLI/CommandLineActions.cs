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
            string[] packageActionsWhereGameCanBeAutoDetected =
            {
                Constants.EXTRACT_PACKAGE,
                Constants.EXTRACT_PACKAGES,
                Constants.EXTRACT_SINGLE_FILE,
                Constants.LIST_PACKAGE
            };

            LogLevel = CommandLineArguments.GetLogLevelByString(args.LogLevel);
            CommandLineLogger.LogDebug($"Using log level: {LogLevel}");

            // source path must be validated for every action
            SourcePath = TryToValidatePath(args.Source);
            
            // ensure these fields are populated with argument values
            DestinationPath = args.Destination;
            PackagedFilePath = args.PackagedPath;
            ConformPath = args.ConformPath;
            
            // validate source path type for actions
            switch (args.Action.ToLowerInvariant())
            {
                case Constants.CONVERT_MODEL:
                case Constants.CONVERT_RESOURCE:
                case Constants.EXTRACT_PACKAGE:
                case Constants.EXTRACT_SINGLE_FILE:
                case Constants.LIST_PACKAGE:
                    if (!PathUtils.IsFile(SourcePath))
                        CommandLineLogger.LogFatal("Source path must point to an existing file", 1);
                    break;
                case Constants.CONVERT_MODELS:
                case Constants.CONVERT_RESOURCES:
                case Constants.CREATE_PACKAGE:
                case Constants.EXTRACT_PACKAGES:
                    if (!PathUtils.IsDir(SourcePath))
                        CommandLineLogger.LogFatal("Source path must point to an existing directory", 1);
                    break;
            }
            
            // handle destination paths
            if (!string.Equals(args.Action, Constants.LIST_PACKAGE, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(args.Destination))
                    DestinationPath = TryToValidatePath(args.Destination);
                else
                {
                    if (PathUtils.IsDir(SourcePath))
                        DestinationPath = SourcePath;
                    else
                        DestinationPath = Path.GetDirectoryName(SourcePath);

                    // do not require --destination argument for create-package action
                    if (DestinationPath != null && string.Equals(args.Action, Constants.CREATE_PACKAGE, StringComparison.OrdinalIgnoreCase))
                        DestinationPath += ".pak";
                }

                if (string.IsNullOrWhiteSpace(DestinationPath))
                    CommandLineLogger.LogFatal("Cannot proceed without a valid destination path", 1);
            }
            
            // handle game setting
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
            
            // only the create-package action requires a package version
            if (string.Equals(args.Action, Constants.CREATE_PACKAGE, StringComparison.OrdinalIgnoreCase))
                PackageVersion = CommandLinePackageProcessor.GetPackageVersionByGame(Game);

            switch (args.Action.ToLowerInvariant())
            {
                case Constants.CONVERT_MODEL:
                case Constants.CONVERT_MODELS:
                case Constants.CONVERT_RESOURCE:
                case Constants.CONVERT_RESOURCES:
                    string resourceFormat = !string.IsNullOrWhiteSpace(args.InputFormat) ? args.InputFormat : Path.GetExtension(SourcePath).TrimStart('.');

                    try
                    {
                        InputFormat = CommandLineArguments.GetResourceFormatByString(resourceFormat);
                    }
                    catch (ArgumentException e)
                    {
                        CommandLineLogger.LogFatal("Cannot proceed without input format", 1);
                    }

                    resourceFormat = !string.IsNullOrWhiteSpace(args.OutputFormat) ? args.OutputFormat : Path.GetExtension(DestinationPath).TrimStart('.');

                    try
                    {
                        OutputFormat = CommandLineArguments.GetResourceFormatByString(resourceFormat);
                    }
                    catch (ArgumentException e)
                    {
                        CommandLineLogger.LogFatal("Cannot proceed without output format", 1);
                    }

                    CommandLineLogger.LogDebug($"Using input format: {InputFormat}");
                    CommandLineLogger.LogDebug($"Using output format: {OutputFormat}");

                    break;
            }

            switch (args.Action.ToLowerInvariant())
            {
                case Constants.CONVERT_MODEL:
                case Constants.CONVERT_MODELS:
                    GR2Options = CommandLineArguments.GetGR2Options(args.Options);

#if DEBUG
                    CommandLineLogger.LogDebug("Using graphics options:");

                    foreach (var x in GR2Options)
                    {
                        CommandLineLogger.LogDebug($"   {x.Key} = {x.Value}");
                    }
#endif

                    if (GR2Options["conform"])
                        ConformPath = TryToValidatePath(args.ConformPath);
                    break;
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
