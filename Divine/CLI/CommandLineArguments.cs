using System.Collections.Generic;
using System.Linq;
using CommandLineParser.Arguments;
using LSLib.Granny.Model;
using LSLib.LS.Enums;

namespace Divine.CLI
{
    public class CommandLineArguments
    {
        // @formatter:off
        [EnumeratedValueArgument(typeof(string), 'l', "loglevel",
            Description = "Set verbosity level of log output",
            DefaultValue = "info",
            AllowedValues = "off;fatal;error;warn;info;debug;trace;all",
            ValueOptional = false,
            Optional = true
        )]
        public string LogLevel;

        // @formatter:off
        [EnumeratedValueArgument(typeof(string), 'g', "game",
            Description = "Set target game when generating output",
            DefaultValue = "dos2",
            AllowedValues = "dos;dosee;dos2",
            ValueOptional = false,
            Optional = true
        )]
        public string Game;

        // @formatter:off
        [ValueArgument(typeof(string), 's', "source",
            Description = "Set source file path or directory",
            DefaultValue = null,
            ValueOptional = false,
            Optional = false
        )]
        public string Source;

        // @formatter:off
        [ValueArgument(typeof(string), 'd', "destination",
            Description = "Set destination file path or directory",
            DefaultValue = null,
            ValueOptional = false,
            Optional = false
        )]
        public string Destination;

        // @formatter:off
        [EnumeratedValueArgument(typeof(string), 'i', "input-format",
            Description = "Set input format for batch operations",
            DefaultValue = null,
            AllowedValues = "dae;gr2;lsv;pak;lsj;lsx;lsb;lsf",
            ValueOptional = false,
            Optional = true
        )]
        public string InputFormat;

        // @formatter:off
        [EnumeratedValueArgument(typeof(string), 'o', "output-format",
            Description = "Set output format for batch operations",
            DefaultValue = null,
            AllowedValues = "dae;gr2;lsv;pak;lsj;lsx;lsb;lsf",
            ValueOptional = false,
            Optional = true
        )]
        public string OutputFormat;

        // @formatter:off
        [EnumeratedValueArgument(typeof(string), 'a', "action",
            Description = "Set action to execute",
            DefaultValue = "extract-package",
            AllowedValues = "create-package;extract-package;extract-packages;convert-model;convert-models;convert-resource;convert-resources",
            ValueOptional = false,
            Optional = false
        )]
        public string Action;

        // @formatter:off
        [EnumeratedValueArgument(typeof(string), 'p', "package-version",
            Description = "Set package version",
            DefaultValue = "v13",
            AllowedValues = "v7;v9;v10;v13",
            ValueOptional = false,
            Optional = true
        )]
        public string PackageVersion;

        // @formatter:off
        [EnumeratedValueArgument(typeof(string), 'c', "compression-method",
            Description = "Set compression method",
            DefaultValue = "lz4hc",
            AllowedValues = "zlib;zlibfast;lz4;lz4hc;none",
            ValueOptional = false,
            Optional = true
        )]
        public string CompressionMethod;

        // @formatter:off
        [EnumeratedValueArgument(typeof(string), "gr2-options",
            Description = "Set extra options for GR2/DAE conversion",
            AllowMultiple = true,
            AllowedValues = "export-normals;export-tangents;export-uvs;export-colors;deduplicate-vertices;filter-uvs;recalculate-normals;recalculate-tangents;recalculate-iwt;flip-uvs;force-legacy-version;compact-tris;build-dummy-skeleton;apply-basis-transforms;conform",
            ValueOptional = false,
            Optional = true
        )]
        public string[] Options;

        // @formatter:off
        [ValueArgument(typeof(string), "conform-path",
            Description = "Set conform to original path",
            DefaultValue = null,
            ValueOptional = false,
            Optional = true
        )]
        public string ConformPath;

        // @formatter:on

        public static LogLevel GetLogLevelByString(string logLevel)
        {
            switch (logLevel)
            {
                case "off":
                {
                    return LSLib.LS.Enums.LogLevel.OFF;
                }
                case "fatal":
                {
                    return LSLib.LS.Enums.LogLevel.FATAL;
                }
                case "error":
                {
                    return LSLib.LS.Enums.LogLevel.ERROR;
                }
                case "warn":
                {
                    return LSLib.LS.Enums.LogLevel.WARN;
                }
                case "info":
                {
                    return LSLib.LS.Enums.LogLevel.INFO;
                }
                case "debug":
                {
                    return LSLib.LS.Enums.LogLevel.DEBUG;
                }
                case "trace":
                {
                    return LSLib.LS.Enums.LogLevel.TRACE;
                }
                case "all":
                {
                    return LSLib.LS.Enums.LogLevel.ALL;
                }
                default:
                {
                    return LSLib.LS.Enums.LogLevel.INFO;
                }
            }
        }

        // ReSharper disable once RedundantCaseLabel
        public static Game GetGameByString(string game)
        {
            switch (game)
            {
                case "dos":
                {
                    return LSLib.LS.Enums.Game.DivinityOriginalSin;
                }
                case "dosee":
                {
                    return LSLib.LS.Enums.Game.DivinityOriginalSinEE;
                }
                case "dos2":
                default:
                {
                    return LSLib.LS.Enums.Game.DivinityOriginalSin2;
                }
            }
        }

        public static FileVersion GetFileVersionByGame(Game divinityGame) => divinityGame == LSLib.LS.Enums.Game.DivinityOriginalSin2 ? FileVersion.VerExtendedNodes : FileVersion.VerChunkedCompress;

        public static ExportFormat GetExportFormatByString(string optionExportFormat) => optionExportFormat == "gr2" ? ExportFormat.GR2 : ExportFormat.DAE;

        // ReSharper disable once RedundantCaseLabel
        public static ResourceFormat GetResourceFormatByString(string resourceFormat)
        {
            switch (resourceFormat)
            {
                case "lsb":
                {
                    return ResourceFormat.LSB;
                }
                case "lsf":
                {
                    return ResourceFormat.LSF;
                }
                case "lsj":
                {
                    return ResourceFormat.LSJ;
                }
                case "lsx":
                default:
                {
                    return ResourceFormat.LSX;
                }
            }
        }

        // ReSharper disable once RedundantCaseLabel
        public static PackageVersion GetPackageVersion(string packageVersion)
        {
            switch (packageVersion)
            {
                case "v7":
                {
                    return LSLib.LS.Enums.PackageVersion.V7;
                }
                case "v9":
                {
                    return LSLib.LS.Enums.PackageVersion.V9;
                }
                case "v10":
                {
                    return LSLib.LS.Enums.PackageVersion.V10;
                }
                case "v13":
                default:
                {
                    return LSLib.LS.Enums.PackageVersion.V13;
                }
            }
        }

        public static Dictionary<string, object> GetCompressionOptions(string compressionOption, PackageVersion packageVersion)
        {
            CompressionMethod compression;
            var fastCompression = true;

            switch (compressionOption)
            {
                case "zlibfast":
                {
                    compression = LSLib.LS.Enums.CompressionMethod.Zlib;
                    break;
                }

                case "zlib":
                {
                    compression = LSLib.LS.Enums.CompressionMethod.Zlib;
                    fastCompression = false;
                    break;
                }

                case "lz4":
                {
                    compression = LSLib.LS.Enums.CompressionMethod.LZ4;
                    break;
                }

                case "lz4hc":
                {
                    compression = LSLib.LS.Enums.CompressionMethod.LZ4;
                    fastCompression = false;
                    break;
                }

                // ReSharper disable once RedundantCaseLabel
                case "none":
                default:
                {
                    compression = LSLib.LS.Enums.CompressionMethod.None;
                    break;
                }
            }

            // fallback to zlib, if the package version doesn't support lz4
            if (compression == LSLib.LS.Enums.CompressionMethod.LZ4 && packageVersion <= LSLib.LS.Enums.PackageVersion.V9)
            {
                compression = LSLib.LS.Enums.CompressionMethod.Zlib;
                fastCompression = false;
            }

            var compressionOptions = new Dictionary<string, object>
            {
                { "Compression", compression },
                { "FastCompression", fastCompression }
            };

            return compressionOptions;
        }

        public static Dictionary<string, bool> GetGR2Options(string[] options)
        {
            var results = new Dictionary<string, bool>
            {
                { "export-normals", false },
                { "export-tangents", false },
                { "export-uvs", false },
                { "export-colors", false },
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

            foreach (string option in options.Where(option => results.Keys.Contains(option)))
            {
                results[option] = true;
            }

            return results;
        }
    }
}
