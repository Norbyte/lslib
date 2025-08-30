using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LSLib.LS;
using LSLib.LS.Enums;

namespace Divine.CLI;

internal class CommandLinePackageProcessor
{
    private static readonly CommandLineArguments Args = Program.argv;

    public static void Create()
    {
        CreatePackageResource();
    }

    public static void ListFiles(Func<PackagedFileInfo, bool> filter = null)
    {
        if (CommandLineActions.SourcePath == null)
        {
            CommandLineLogger.LogFatal("Cannot list package without source path", 1);
        }
        else
        {
            ListPackageFiles(CommandLineActions.SourcePath, filter);
        }
    }

    public static void ExtractSingleFile()
    {
        ExtractSingleFile(CommandLineActions.SourcePath, CommandLineActions.DestinationPath, CommandLineActions.PackagedFilePath);
    }

    private static void ExtractSingleFile(string packagePath, string destinationPath, string packagedPath)
    {
        try
        {
            var reader = new PackageReader();
            using var package = reader.Read(packagePath);

            // Try to match by full path
            var file = package.Files.Find(fileInfo => string.Compare(fileInfo.Name, packagedPath, StringComparison.OrdinalIgnoreCase) == 0 && !fileInfo.IsDeletion());
            if (file == null)
            {
                // Try to match by filename only
                file = package.Files.Find(fileInfo => string.Compare(Path.GetFileName(fileInfo.Name), packagedPath, StringComparison.OrdinalIgnoreCase) == 0);
                if (file == null)
                {
                    CommandLineLogger.LogError($"Package doesn't contain file named '{packagedPath}'");
                    return;
                }
            }

            using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
            using var source = file.CreateContentReader();
            source.CopyTo(fs);
        }
        catch (NotAPackageException)
        {
            CommandLineLogger.LogError("Failed to list package contents because the package is not an Original Sin package or savegame archive");
        }
        catch (Exception e)
        {
            CommandLineLogger.LogFatal($"Failed to list package: {e.Message}", 2);
            CommandLineLogger.LogTrace($"{e.StackTrace}");
        }
    }

    private static void ListPackageFiles(string packagePath, Func<PackagedFileInfo, bool> filter = null)
    {
        try
        {
            var reader = new PackageReader();
            using var package = reader.Read(packagePath);
            var files = package.Files;

	        if (filter != null)
	        {
		        files = files.FindAll(obj => filter(obj));
	        }

            foreach (var fileInfo in files.OrderBy(obj => obj.Name))
            {
                Console.WriteLine($"{fileInfo.Name}\t{fileInfo.Size()}\t{fileInfo.Crc}");
            }
        }
        catch (NotAPackageException)
        {
            CommandLineLogger.LogError("Failed to list package contents because the package is not an Original Sin package or savegame archive");
        }
        catch (Exception e)
        {
            CommandLineLogger.LogFatal($"Failed to list package: {e.Message}", 2);
            CommandLineLogger.LogTrace($"{e.StackTrace}");
        }
    }

    public static void Extract(Func<PackagedFileInfo, bool> filter = null)
    {
        if (CommandLineActions.SourcePath == null)
        {
            CommandLineLogger.LogFatal("Cannot extract package without source path", 1);
        }
        else
        {
            string extractionPath = GetExtractionPath(CommandLineActions.SourcePath, CommandLineActions.DestinationPath);

            CommandLineLogger.LogInfo($"Extracting package: {CommandLineActions.SourcePath}");

            ExtractPackageResource(CommandLineActions.SourcePath, extractionPath, filter);
        }
    }

    public static void BatchExtract(Func<PackagedFileInfo, bool> filter = null)
    {
        string[] files = Directory.GetFiles(CommandLineActions.SourcePath, $"*.{Args.InputFormat}");

        foreach (string file in files)
        {
            string extractionPath = GetExtractionPath(file, CommandLineActions.DestinationPath);

            CommandLineLogger.LogInfo($"Extracting package: {file}");

            ExtractPackageResource(file, extractionPath, filter);
        }
    }

    private static string GetExtractionPath(string sourcePath, string destinationPath)
    {
        return Args.UsePackageName ? Path.Combine(destinationPath, Path.GetFileNameWithoutExtension(sourcePath) ?? throw new InvalidOperationException()) : CommandLineActions.DestinationPath;
    }

    private static void CreatePackageResource(string file = "")
    {
        if (string.IsNullOrEmpty(file))
        {
            file = CommandLineActions.DestinationPath;
            CommandLineLogger.LogDebug($"Using destination path: {file}");
        }

        var build = new PackageBuildData();
        build.Version = CommandLineActions.PackageVersion;
        build.Priority = (byte)CommandLineActions.PackagePriority;

        Dictionary<string, object> compressionOptions = CommandLineArguments.GetCompressionOptions(Path.GetExtension(file)?.ToLower() == ".lsv" ? "zlib" : Args.PakCompressionMethod, build.Version);
        build.Compression = (CompressionMethod)compressionOptions["Compression"];
        build.CompressionLevel = (LSCompressionLevel)compressionOptions["CompressionLevel"];

        CommandLineLogger.LogDebug($"Using compression method: {build.Compression} (build.CompressionLevel)");

        var packager = new Packager();
        packager.CreatePackage(file, CommandLineActions.SourcePath, build).Wait();

        CommandLineLogger.LogInfo("Package created successfully.");
    }

    private static void ExtractPackageResource(string file = "", string folder = "", Func<PackagedFileInfo, bool> filter = null)
    {
        if (string.IsNullOrEmpty(file))
        {
            file = CommandLineActions.SourcePath;
            CommandLineLogger.LogDebug($"Using source path: {file}");
        }

#if !DEBUG
        try
        {
#endif
            var packager = new Packager();

            string extractionPath = GetExtractionPath(folder, CommandLineActions.DestinationPath);

            CommandLineLogger.LogDebug($"Using extraction path: {extractionPath}");

            packager.UncompressPackage(file, extractionPath, filter);

            CommandLineLogger.LogInfo($"Extracted package to: {extractionPath}");
#if !DEBUG
        }
        catch (NotAPackageException)
        {
            CommandLineLogger.LogError("Failed to extract package because the package is not an Original Sin package or savegame archive");
        }
        catch (Exception e)
        {
            CommandLineLogger.LogFatal($"Failed to extract package: {e.Message}", 2);
            CommandLineLogger.LogTrace($"{e.StackTrace}");
        }
#endif
    }
}
