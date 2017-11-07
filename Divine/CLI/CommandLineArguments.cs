using System.Collections.Generic;
using System.Linq;
using CommandLineParser.Arguments;
using Divine.Enums;
using LSLib.Granny.Model;
using LSLib.LS;

namespace Divine.CLI
{
    public class CommandLineArguments
    {
        [EnumeratedValueArgument(typeof(string), 'l', "loglevel",
            Description = "Set verbosity level of log output",
            DefaultValue = "info",
            AllowedValues = "info;warn;error;fatal;debug;silent",
            ValueOptional = false,
            Optional = true
        )]
        public string LogLevel;

        [EnumeratedValueArgument(typeof(string), 'g', "game",
            Description = "Set target game when generating output",
            DefaultValue = "dos2",
            AllowedValues = "dos;dosee;dos2",
            ValueOptional = false,
            Optional = true
        )]
        public string Game;

        [EnumeratedValueArgument(typeof(string), 'a', "action",
            Description = "Set action to execute",
            DefaultValue = "extract-package",
            AllowedValues = "create-package;extract-package;extract-packages;convert-model;convert-models;convert-resource;convert-resources",
            ValueOptional = false,
            Optional = false
        )]
        public string Action;

        [ValueArgument(typeof(string), 's', "source",
            Description = "Set source file path or directory",
            DefaultValue = null,
            ValueOptional = false,
            Optional = false
        )]
        public string Source;

        [ValueArgument(typeof(string), 'd', "destination",
            Description = "Set destination file path or directory",
            DefaultValue = null,
            ValueOptional = false,
            Optional = false
        )]
        public string Destination;

        [EnumeratedValueArgument(typeof(string), 'i', "input-format",
            Description = "Set input format for batch operations",
            DefaultValue = null,
            AllowedValues = "dae;gr2;lsv;pak;lsj;lsx;lsb;lsf",
            ValueOptional = false,
            Optional = true
        )]
        public string InputFormat;

        [EnumeratedValueArgument(typeof(string), 'o', "output-format",
            Description = "Set output format for batch operations",
            DefaultValue = null,
            AllowedValues = "dae;gr2;lsv;pak;lsj;lsx;lsb;lsf",
            ValueOptional = false,
            Optional = true
        )]
        public string OutputFormat;

        [EnumeratedValueArgument(typeof(string), "gr2-options", 
            Description = "Set extra options for GR2/DAE conversion",
            AllowMultiple = true,
            AllowedValues = "export-normals;export-tangents;export-uvs;deduplicate-vertices;filter-uvs;recalculate-normals;recalculate-tangents;recalculate-iwt;flip-uvs;force-legacy-version;compact-tris;build-dummy-skeleton;apply-basis-transforms;conform",
            ValueOptional = false,
            Optional = true
        )]
        public string[] Options;

        [ValueArgument(typeof(string), "conform-path", 
            Description = "Set conform to original path", 
            DefaultValue = null,
            ValueOptional = false,
            Optional = true
        )]
        public string ConformPath;

        [EnumeratedValueArgument(typeof(string), 'p', "package-version",
            Description = "Set package version",
            DefaultValue = "v13",
            AllowedValues = "v7;v9;v10;v13",
            ValueOptional = false,
            Optional = true
        )]
        public string PackageVersion;

        [EnumeratedValueArgument(typeof(string), 'c', "compression-method",
            Description = "Set compression method",
            DefaultValue = "lz4hc",
            AllowedValues = "zlib;zlibfast;lz4;lz4hc;none",
            ValueOptional = false,
            Optional = true
        )]
        public string CompressionMethod;

        public static LogLevel GetLogLevelByString(string optionLogLevel)
        {
            LogLevel logLevel;

            switch (optionLogLevel)
            {
                case "silent":
                    logLevel = Enums.LogLevel.SILENT;
                    break;
                case "info":
                    logLevel = Enums.LogLevel.INFO;
                    break;
                case "warn":
                    logLevel = Enums.LogLevel.WARN;
                    break;
                case "error":
                    logLevel = Enums.LogLevel.ERROR;
                    break;
                case "fatal":
                    logLevel = Enums.LogLevel.FATAL;
                    break;
                case "debug":
                    logLevel = Enums.LogLevel.DEBUG;
                    break;
                default:
                    logLevel = Enums.LogLevel.INFO;
                    break;
            }

            return logLevel;
        }

        public static Game GetGameByString(string optionGame)
        {
            Game game = Enums.Game.DivinityOriginalSin2;

            switch (optionGame)
            {
                case "dos":
                    game = Enums.Game.DivinityOriginalSin;
                    break;
                case "dosee":
                    game = Enums.Game.DivinityOriginalSinEE;
                    break;
                case "dos2":
                    game = Enums.Game.DivinityOriginalSin2;
                    break;
            }

            return game;
        }

        public static int GetFileVersionByGame(Game divinityGame)
        {
            return divinityGame == Enums.Game.DivinityOriginalSin2
                ? (int)LSLib.LS.LSF.FileVersion.VerExtendedNodes
                : (int)LSLib.LS.LSF.FileVersion.VerChunkedCompress;
        }

        public static ExportFormat GetExportFormatByString(string optionExportFormat)
        {
            return optionExportFormat == "gr2" ? ExportFormat.GR2 : ExportFormat.DAE;
        }

        public static ResourceFormat GetResourceFormatByString(string optionResourceFormat)
        {
            ResourceFormat resourceFormat = ResourceFormat.LSX;

            switch (optionResourceFormat)
            {
                case "lsb":
                    resourceFormat = ResourceFormat.LSB;
                    break;
                case "lsf":
                    resourceFormat = ResourceFormat.LSF;
                    break;
                case "lsj":
                    resourceFormat = ResourceFormat.LSJ;
                    break;
                case "lsx":
                    resourceFormat = ResourceFormat.LSX;
                    break;
            }

            return resourceFormat;
        }

        public static PackageVersion GetPackageVersion(string versionOption)
        {
            PackageVersion version;
            
            switch (versionOption)
            {
                case "v7":
                    version = Enums.PackageVersion.v7;
                    break;

                case "v9":
                    version = Enums.PackageVersion.v9;
                    break;

                case "v10":
                    version = Enums.PackageVersion.v10;
                    break;

                case "v13":
                    version = Enums.PackageVersion.v13;
                    break;

                default:
                    version = Enums.PackageVersion.v13;
                    break;
            }

            return version;
        }

        public static Dictionary<string, object> GetCompressionOptions(string compressionOption, PackageVersion version)
        {
            CompressionMethod compression;
            bool fastCompression;

            switch (compressionOption)
            {
                case "zlibfast":
                    compression = LSLib.LS.CompressionMethod.Zlib;
                    fastCompression = true;
                    break;

                case "zlib":
                    compression = LSLib.LS.CompressionMethod.Zlib;
                    fastCompression = false;
                    break;

                case "lz4":
                    compression = LSLib.LS.CompressionMethod.LZ4;
                    fastCompression = true;
                    break;

                case "lz4hc":
                    compression = LSLib.LS.CompressionMethod.LZ4;
                    fastCompression = false;
                    break;

                default:
                    compression = LSLib.LS.CompressionMethod.None;
                    fastCompression = false;
                    break;
            }

            // fallback to zlib, if the package version doesn't support lz4
            if (compression == LSLib.LS.CompressionMethod.LZ4 && version <= (PackageVersion) 9)
            {
                compression = LSLib.LS.CompressionMethod.Zlib;
                fastCompression = false;
            }

            var compressionOptions = new Dictionary<string, object>
            {
                { "Compression", compression },
                { "FastCompression", fastCompression }
            };

            return compressionOptions;
        }

        public static Dictionary<string, bool> GetGraphicsOptions(string[] options)
        {
            Dictionary<string, bool> results = new Dictionary<string, bool>
            {
                { "export-normals", false },
                { "export-tangents", false },
                { "export-uvs", false },
                { "deduplicate-vertices", false },
                { "filter-uvs", false },
                { "recalculate-normals", false },
                { "recalculate-tangents", false },
                { "recalculate-iwt", false },
                { "flip-uvs", false },
                { "force-legacy-version", false },
                { "compact-tris", false },
                { "build-dummy-skeleton", false },
                { "apply-basis-transforms", false },
                { "conform", false }
            };

            if (options == null)
            {
                return results;
            }

            foreach (string option in options)
            {
                if (results.Keys.Contains(option))
                {
                    results[option] = true;
                }
            }

            return results;
        }
    }
}
