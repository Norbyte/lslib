using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLineParser.Arguments;
using LSLib.Granny.Model;
using LSLib.LS;
using LSLib.LS.Enums;

namespace Divine.CLI;

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
        DefaultValue = null,
        AllowedValues = "dos;dosee;dos2;dos2de;bg3",
        ValueOptional = false,
        Optional = false
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
        AllowedValues = "dae;glb;gltf;gr2;lsv;pak;lsj;lsx;lsb;lsf",
        ValueOptional = false,
        Optional = true
    )]
    public string InputFormat;

    // @formatter:off
    [EnumeratedValueArgument(typeof(string), 'o', "output-format",
        Description = "Set output format for batch operations",
        DefaultValue = null,
        AllowedValues = "dae;glb;gltf;gr2;lsv;pak;lsj;lsx;lsb;lsf",
        ValueOptional = false,
        Optional = true
    )]
    public string OutputFormat;

    // @formatter:off
    [EnumeratedValueArgument(typeof(string), 'a', "action",
        Description = "Set action to execute",
        DefaultValue = "extract-package",
        AllowedValues = "create-package;list-package;extract-single-file;extract-package;extract-packages;convert-model;convert-models;convert-resource;convert-resources;convert-loca",
        ValueOptional = false,
        Optional = false
    )]
    public string Action;

    // @formatter:off
    [EnumeratedValueArgument(typeof(string), 'c', "compression-method",
        Description = "Set compression method",
        DefaultValue = "lz4hc",
        AllowedValues = "zlib;zlibfast;lz4;lz4hc;none",
        ValueOptional = false,
        Optional = true
    )]
    public string PakCompressionMethod;

    // @formatter:off
    [EnumeratedValueArgument(typeof(string), 'e', "gr2-options",
        Description = "Set extra options for GR2/DAE conversion",
        AllowMultiple = true,
        AllowedValues = "export-normals;export-tangents;export-uvs;export-colors;deduplicate-vertices;deduplicate-uvs;recalculate-normals;recalculate-tangents;recalculate-iwt;flip-uvs;ignore-uv-nan;disable-qtangents;y-up-skeletons;force-legacy-version;compact-tris;build-dummy-skeleton;apply-basis-transforms;x-flip-skeletons;x-flip-meshes;conform;conform-copy",
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
    [ValueArgument(typeof(int), "package-priority",
        Description = "Set a custom package priority",
        DefaultValue = 0,
        ValueOptional = true,
        Optional = true
    )]
    public int PackagePriority;

    // @formatter:off
    [SwitchArgument("legacy-guids", false,
        Description = "Use legacy GUID serialization format when serializing LSX/LSJ files",
        Optional = true
    )]
    public bool LegacyGuids;

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
            case "bg3":
            {
                return LSLib.LS.Enums.Game.BaldursGate3;
            }
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
            {
                return LSLib.LS.Enums.Game.DivinityOriginalSin2DE;
             }
            case "unset":
            {
                return LSLib.LS.Enums.Game.Unset;
            }
            default:
            {
                throw new ArgumentException($"Unknown game: \"{game}\"");
            }
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
            {
                return ResourceFormat.LSX;
            }
            default:
            {
                throw new ArgumentException($"Unknown resource format: \"{resourceFormat}\"");
            }
        }
    }

    public static Dictionary<string, object> GetCompressionOptions(string compressionOption, PackageVersion packageVersion)
    {
        CompressionMethod compression;
        LSCompressionLevel level;

        switch (compressionOption)
        {
            case "zlibfast":
            {
                compression = CompressionMethod.Zlib;
                level = LSCompressionLevel.Fast;
                break;
            }

            case "zlib":
            {
                compression = CompressionMethod.Zlib;
                level = LSCompressionLevel.Default;
                break;
            }

            case "lz4":
            {
                compression = CompressionMethod.LZ4;
                level = LSCompressionLevel.Fast;
                break;
            }

            case "lz4hc":
            {
                compression = CompressionMethod.LZ4;
                level = LSCompressionLevel.Default;
                break;
            }

            // ReSharper disable once RedundantCaseLabel
            case "none":
            default:
            {
                compression = CompressionMethod.None;
                level = LSCompressionLevel.Default;
                break;
            }
        }

        // fallback to zlib, if the package version doesn't support lz4
        if (compression == CompressionMethod.LZ4 && packageVersion <= PackageVersion.V9)
        {
            compression = CompressionMethod.Zlib;
            level = LSCompressionLevel.Default;
        }

        var compressionOptions = new Dictionary<string, object>
        {
            { "Compression", compression },
            { "CompressionLevel", level }
        };

        return compressionOptions;
    }

    public static Dictionary<string, bool> GetGR2Options(string[] options)
    {
        var results = new Dictionary<string, bool>
        {
            { "deduplicate-vertices", true },
            { "flip-uvs", true },
            { "ignore-uv-nan", true },
            { "disable-qtangents", false },
            { "y-up-skeletons", true },
            { "force-legacy-version", false },
            { "compact-tris", true },
            { "build-dummy-skeleton", true },
            { "apply-basis-transforms", true },
            { "x-flip-skeletons", false },
            { "x-flip-meshes", false },
            { "conform", false },
            { "conform-copy", false },
            
            // Deprecated options, no longer in use
            { "export-normals", true },
            { "export-tangents", true },
            { "export-uvs", true },
            { "export-colors", true },
            { "recalculate-normals", false },
            { "recalculate-tangents", false },
            { "recalculate-iwt", false },
            { "deduplicate-uvs", true }
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
