using System;
using System.Collections.Generic;
using System.IO;
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
            AllowedValues = "dos;dosee;dos2;dos2de",
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
            ValueOptional = true,
            Optional = true
        )]
        public string Destination;

        // @formatter:off
        [ValueArgument(typeof(string), 'f', "packaged-path",
            Description = "File to extract from package",
            DefaultValue = null,
            ValueOptional = true,
            Optional = true
        )]
        public string PackagedPath;

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
            AllowedValues = "create-package;list-package;extract-single-file;extract-package;extract-packages;convert-model;convert-models;convert-resource;convert-resources",
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
        [EnumeratedValueArgument(typeof(string), 'e', "gr2-options",
            Description = "Set extra options for GR2/DAE conversion",
            AllowMultiple = true,
            AllowedValues = "export-normals;export-tangents;export-uvs;export-colors;deduplicate-vertices;deduplicate-uvs;recalculate-normals;recalculate-tangents;recalculate-iwt;flip-uvs;ignore-uv-nan;y-up-skeletons;force-legacy-version;compact-tris;build-dummy-skeleton;apply-basis-transforms;x-flip-skeletons;x-flip-meshes;conform;conform-copy",
            ValueOptional = false,
            Optional = true
        )]
        public string[] Options;

		// @formatter:off
		[ValueArgument(typeof(string), 'x', "expression",
            Description = "Set glob expression for extract and list actions",
            DefaultValue = "*",
            ValueOptional = false,
            Optional = true
        )]
        public string Expression;

        // @formatter:off
        [ValueArgument(typeof(string), "conform-path",
            Description = "Set conform to original path",
            DefaultValue = null,
            ValueOptional = false,
            Optional = true
        )]
        public string ConformPath;

        // @formatter:off
        [SwitchArgument("use-package-name", false,
            Description = "Use package name for destination folder",
            Optional = true
        )]
        public bool UsePackageName;

		// @formatter:off
        [SwitchArgument("use-regex", false,
            Description = "Use Regular Expressions for expression type",
            Optional = true
        )]
        public bool UseRegex;

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
                {
                    return LSLib.LS.Enums.Game.DivinityOriginalSin2;
                }
                case "dos2de":
                default:
                {
                    return LSLib.LS.Enums.Game.DivinityOriginalSin2DE;
                }
            }
        }

        public static FileVersion GetFileVersionByGame(Game divinityGame)
        {
            return divinityGame.IsFW3() ? FileVersion.VerExtendedNodes : FileVersion.VerChunkedCompress;
        }

        public static ExportFormat GetModelFormatByString(string format)
        {
            switch (format.ToLower())
            {
                case "gr2": return ExportFormat.GR2;
                case "dae": return ExportFormat.DAE;
                default: throw new ArgumentException($"Unknown model format: {format}");
            }
        }

        public static ExportFormat GetModelFormatByPath(string path)
        {
            string extension = Path.GetExtension(path);
            if (extension != null)
            {
                return GetModelFormatByString(extension.Substring(1));
            }
            else
            {
                throw new ArgumentException($"Could not determine model format from filename: {path}");
            }
        }

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
                { "export-normals", true },
                { "export-tangents", true },
                { "export-uvs", true },
                { "export-colors", true },
                { "deduplicate-vertices", true },
                { "deduplicate-uvs", true },
                { "recalculate-normals", false },
                { "recalculate-tangents", false },
                { "recalculate-iwt", false },
                { "flip-uvs", true },
                { "ignore-uv-nan", true },
                { "y-up-skeletons", true },
                { "force-legacy-version", false },
                { "compact-tris", true },
                { "build-dummy-skeleton", true },
                { "apply-basis-transforms", true },
                { "x-flip-skeletons", false },
                { "x-flip-meshes", false },
                { "conform", false },
                { "conform-copy", false }
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
