using System;
using System.Collections.Generic;
using System.IO;
using LSLib.LS;
using LSLib.LS.Enums;

namespace Divine.CLI
{
    internal class CommandLinePackageProcessor
    {
        private static readonly CommandLineArguments Args = Program.argv;

        public static void Create()
        {
            CreatePackageResource();
        }

        public static void Extract()
        {
            if (CommandLineActions.SourcePath == null)
            {
                CommandLineLogger.LogFatal("Cannot extract package without source path", 1);
            }
            else
            {
                string extractionPath = GetExtractionPath(CommandLineActions.SourcePath, CommandLineActions.DestinationPath);

                CommandLineLogger.LogDebug($"Extracting package: {CommandLineActions.SourcePath}");

                ExtractPackageResource(CommandLineActions.SourcePath, extractionPath);
            }
        }

        public static void BatchExtract()
        {
            string[] files = Directory.GetFiles(CommandLineActions.SourcePath, $"*.{Args.InputFormat}");

            foreach (string file in files)
            {
                string extractionPath = GetExtractionPath(file, CommandLineActions.DestinationPath);

                CommandLineLogger.LogDebug($"Extracting package: {file}");

                ExtractPackageResource(file, extractionPath);
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

            PackageVersion packageVersion = CommandLineActions.PackageVersion;
            Dictionary<string, object> compressionOptions = CommandLineArguments.GetCompressionOptions(Path.GetExtension(file)?.ToLower() == ".lsv" ? "zlib" : Args.CompressionMethod, packageVersion);

            CompressionMethod compressionMethod = (CompressionMethod) compressionOptions["Compression"];
            bool compressionSpeed = (bool) compressionOptions["FastCompression"];

            CommandLineLogger.LogDebug($"Using compression method: {compressionMethod.ToString()}");
            CommandLineLogger.LogDebug($"Using fast compression: {compressionSpeed}");

            Packager packager = new Packager();
            packager.CreatePackage(file, CommandLineActions.SourcePath, packageVersion, compressionMethod, compressionSpeed);

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
                Packager packager = new Packager();

                string extractionPath = GetExtractionPath(folder, CommandLineActions.DestinationPath);

                CommandLineLogger.LogDebug($"Using extraction path: {extractionPath}");

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
