using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LSLib.LS;
using LSLib.LS.Enums;

namespace Divine.CLI;

internal class CommandLineActions
{
    public static string SourcePath;
    public static string DestinationPath;
    public static string PackagedFilePath;
    public static string ConformPath;
    public static string VTConfigPath;
    public static string VTRootPath;

    public static Game Game;
    public static LogLevel LogLevel;
    public static ResourceFormat InputFormat;
    public static ResourceFormat OutputFormat;
    public static PackageVersion PackageVersion;
    public static int PackagePriority;
    public static bool LegacyGuids;
    public static bool FastBuild;
    public static bool VTValidate;
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

        LegacyGuids = args.LegacyGuids;
        FastBuild = args.FastBuild;
        VTValidate = args.VTValidate;

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
            PackagePriority = args.PackagePriority;
            PackageVersion = Game.PAKVersion();
            CommandLineLogger.LogDebug($"Using package version: {PackageVersion}");
        }

        if (args.Action == "build-vt")
        {
            VTConfigPath = TryToValidatePath(args.Source);
            VTRootPath = TryToValidatePath(args.VTRoot);
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

            if (args.ConformPath != null && args.ConformPath != "")
            {
                ConformPath = TryToValidatePath(args.ConformPath);
                if (!Path.Exists(ConformPath))
                {
                    CommandLineLogger.LogFatal($"Skeleton source GR2 does not exist: {args.ConformPath}", 1);
                }
            }
        }

        SourcePath = TryToValidatePath(args.Source);
        if (args.Action != "list-package" && args.Action != "build-vt")
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
        Func<PackagedFileInfo, bool> filter;

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

            case "convert-loca":
            {
                CommandLineDataProcessor.ConvertLoca();
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

            case "build-vt":
            {
                CommandLineDataProcessor.BuildVirtualTextureSet();
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

        // Path.IsPathRooted() is the real cross-platform "is this absolute" check.
        // The previous implementation additionally required Uri.TryCreate(path,
        // UriKind.RelativeOrAbsolute, ...) to parse the path as an absolute file
        // URI, which silently assumed Windows-style paths: on Linux, a perfectly
        // valid absolute path like "/tmp/foo" parses as a *relative* URI
        // (IsAbsoluteUri == false), and reading uri.IsFile on a relative URI
        // throws InvalidOperationException - crashing Divine on every action that
        // takes -s/-d when run on Linux, regardless of how valid the path is.
        if (!Path.IsPathRooted(path))
        {
            CommandLineLogger.LogFatal($"Cannot proceed without absolute path [E2]: {path}", 1);
        }

        // ReSharper disable once AssignNullToNotNullAttribute
        path = Path.GetFullPath(path);

        return path;
    }
}
