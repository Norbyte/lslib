using System;
using System.Collections.Generic;
using System.IO;
using Divine.Enums;
using LSLib.LS;
using LSLib.LS.Enums;

namespace Divine.CLI
{
    internal class CommandLinePackageProcessor
    {
        private static readonly CommandLineArguments Args = Program.argv;

        public static void Create() => CreatePackageResource();

        public static void Extract()
        {
            if (CommandLineActions.SourcePath == null)
            {
                CommandLineLogger.LogFatal("Cannot extract package without source path", 1);
            }
            else
            {
                string extractionPath = Path.Combine(CommandLineActions.DestinationPath, Path.GetFileNameWithoutExtension(CommandLineActions.SourcePath));
                ExtractPackageResource(CommandLineActions.SourcePath, extractionPath);
            }
        }

        public static void BatchExtract()
        {
            string[] files = Directory.GetFiles(CommandLineActions.SourcePath, $"*.{Args.InputFormat}");

            foreach (string file in files)
            {
                string packageName = Path.GetFileNameWithoutExtension(file);

                CommandLineLogger.LogDebug($"Extracting package: {file}");
                ExtractPackageResource(file, packageName);
            }
        }

        private static void CreatePackageResource(string file = "")
        {
            if (string.IsNullOrEmpty(file))
            {
                file = CommandLineActions.DestinationPath;
                CommandLineLogger.LogDebug($"Using destination path: {file}");
            }

            PackageVersion version = CommandLineActions.PackageVersion;
            Dictionary<string, object> compressionOptions = CommandLineArguments.GetCompressionOptions(Path.GetExtension(file)?.ToLower() == ".lsv" ? "zlib" : Args.CompressionMethod, version);

            var compressionMethod = (CompressionMethod) compressionOptions["Compression"];
            var compressionSpeed = (bool) compressionOptions["FastCompression"];

            CommandLineLogger.LogDebug($"Using compression method: {compressionMethod.ToString()}");
            CommandLineLogger.LogDebug($"Using fast compression: {compressionSpeed}");

            var packager = new Packager();
            packager.CreatePackage(file, CommandLineActions.SourcePath, (uint) version, compressionMethod, compressionSpeed);

            CommandLineLogger.LogInfo("Package created successfully.");
        }

        private static void ExtractPackageResource(string file = "", string folder = "")
        {
            if (string.IsNullOrEmpty(file))
            {
                file = CommandLineActions.SourcePath;
                CommandLineLogger.LogDebug($"Using source path: {file}");
            }

            try
            {
                var packager = new Packager();
                string extractionPath = Path.Combine(CommandLineActions.DestinationPath, folder);
                packager.UncompressPackage(file, extractionPath);

                CommandLineLogger.LogInfo($"Extracted package to: {extractionPath}");
            }
            catch (NotAPackageException)
            {
                CommandLineLogger.LogFatal("Failed to extract package because the package is not an Original Sin package or savegame archive", 1);
            }
            catch (Exception e)
            {
                CommandLineLogger.LogFatal($"Failed to extract package: {e.Message}", 2);
                CommandLineLogger.LogTrace($"{e.StackTrace}");
            }
        }
    }
}
