using System;
using System.IO;
using System.Linq;
using LSLib.LS;
using LSLib.LS.Enums;

namespace Divine.CLI
{
    internal static class CommandLinePackageProcessor
    {
        private static readonly CommandLineArguments Args = Program.argv;

        public static void Create() => CreatePackageResource();

        public static void ListFiles(Func<AbstractFileInfo, bool> filter = null)
        {
            if (string.IsNullOrWhiteSpace(CommandLineActions.SourcePath))
            {
                CommandLineLogger.LogFatal("Cannot list package without source path", 1);
                return;
            }

            ListPackageFiles(CommandLineActions.SourcePath, filter);
        }

        public static void ExtractSingleFile() => ExtractSingleFile(CommandLineActions.SourcePath, CommandLineActions.DestinationPath, CommandLineActions.PackagedFilePath);

        private static void ExtractSingleFile(string packagePath, string destinationPath, string packagedPath)
        {
            if (string.Equals(Args.Game, Constants.AUTODETECT, StringComparison.OrdinalIgnoreCase))
            {
                CommandLineActions.Game = GetGameByPackageVersion(packagePath);
            }
            
            try
            {
                using (var reader = new PackageReader(packagePath))
                {
                    Package package = reader.Read();
                    // Try to match by full path
                    AbstractFileInfo file = package.Files.Find(fileInfo => string.Compare(fileInfo.Name, packagedPath, StringComparison.OrdinalIgnoreCase) == 0 && !fileInfo.IsDeletion());
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

                    using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                    {
                        try
                        {
                            Stream stream = file.MakeStream();
                            stream.CopyTo(fs);
                        }
                        finally
                        {
                            file.ReleaseStream();
                        }

                    }
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

        private static void ListPackageFiles(string packagePath, Func<AbstractFileInfo, bool> filter = null)
        {
            if (string.Equals(Args.Game, Constants.AUTODETECT, StringComparison.OrdinalIgnoreCase))
            {
                CommandLineActions.Game = GetGameByPackageVersion(packagePath);
            }
            
            try
            {
                using (var reader = new PackageReader(packagePath))
                {
                    Package package = reader.Read();

	                var files = package.Files;

	                if (filter != null)
	                {
		                files = files.FindAll(obj => filter(obj));
	                }

                    foreach (AbstractFileInfo fileInfo in files.OrderBy(obj => obj.Name))
                    {
                        Console.WriteLine($"{fileInfo.Name}\t{fileInfo.Size()}\t{fileInfo.CRC()}");
                    }
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

        public static void Extract(Func<AbstractFileInfo, bool> filter = null)
        {
            if (string.IsNullOrWhiteSpace(CommandLineActions.SourcePath))
            {
                CommandLineLogger.LogFatal("Cannot extract package without source path", 1);
                return;
            }

            string extractionPath = GetExtractionPath(CommandLineActions.SourcePath, CommandLineActions.DestinationPath);

            CommandLineLogger.LogInfo($"Extracting package: {CommandLineActions.SourcePath}");

            ExtractPackageResource(CommandLineActions.SourcePath, extractionPath, filter);
        }

        public static void BatchExtract(Func<AbstractFileInfo, bool> filter = null)
        {
            string[] files;
            
            if (string.Equals(Args.Action, Constants.EXTRACT_PACKAGES, StringComparison.OrdinalIgnoreCase))
            {
                files = Directory.GetFiles(CommandLineActions.SourcePath, "*.pak");
            }
            else
            {
                files = Directory.GetFiles(CommandLineActions.SourcePath, $"*.{Args.InputFormat}");
            }

            foreach (string file in files)
            {
                string extractionPath = GetExtractionPath(file, CommandLineActions.DestinationPath);
                
                CommandLineLogger.LogInfo($"Extracting package: {file}");

                ExtractPackageResource(file, extractionPath, filter);
            }
        }

        private static string GetExtractionPath(string sourcePath, string destinationPath)
        {
            if (Args.UsePackageName)
            {
                if (string.IsNullOrWhiteSpace(destinationPath))
                {
                    destinationPath = Path.GetDirectoryName(sourcePath);
                }

                if (string.IsNullOrWhiteSpace(destinationPath))
                {
                    destinationPath = Directory.GetCurrentDirectory();
                }
                
                return Path.GetFullPath(Path.Combine(destinationPath, Path.GetFileNameWithoutExtension(sourcePath)));
            }

            return CommandLineActions.DestinationPath;
        }

        private static void CreatePackageResource(string file = "")
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                file = CommandLineActions.DestinationPath;
                CommandLineLogger.LogDebug($"Using destination path: {file}");
            }

            var options = new PackageCreationOptions();
            options.Version = CommandLineActions.PackageVersion;

            var compressionOptions = CommandLineArguments.GetCompressionOptions(Path.GetExtension(file)?.ToLower() == ".lsv" ? "zlib" : Args.CompressionMethod, options.Version);

            options.Compression = (CompressionMethod)compressionOptions["Compression"];
            options.FastCompression = (bool)compressionOptions["FastCompression"];

            string fast = options.FastCompression ? "Fast" : "Normal";
            CommandLineLogger.LogDebug($"Using compression method: {options.Compression.ToString()} ({fast})");

            var packager = new Packager();
            packager.CreatePackage(file, CommandLineActions.SourcePath, options);

            CommandLineLogger.LogInfo("Package created successfully.");
        }

        private static void ExtractPackageResource(string file = "", string folder = "", Func<AbstractFileInfo, bool> filter = null)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                file = CommandLineActions.SourcePath;
                CommandLineLogger.LogDebug($"Using source path: {file}");
            }
            
            if (string.Equals(Args.Game, Constants.AUTODETECT, StringComparison.OrdinalIgnoreCase))
            {
                CommandLineActions.Game = GetGameByPackageVersion(file);
            }

            try
            {
                var packager = new Packager();

                string extractionPath = GetExtractionPath(folder, CommandLineActions.DestinationPath);

                CommandLineLogger.LogDebug($"Using extraction path: {extractionPath}");
                
                packager.UncompressPackage(file, extractionPath, filter);

                CommandLineLogger.LogInfo($"Extracted package to: {extractionPath}");
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
        }
        
        public static PackageVersion GetPackageVersionByGame(Game gameVersion)
        {
            PackageVersion packageVersion;
            
            switch (gameVersion)
            {
                case Game.DivinityOriginalSin:
                    packageVersion = PackageVersion.V7;
                    break;
                case Game.DivinityOriginalSinEE:
                    packageVersion = PackageVersion.V9;
                    break;
                case Game.DivinityOriginalSin2:
                    packageVersion = PackageVersion.V10;
                    break;
                case Game.DivinityOriginalSin2DE:
                    packageVersion = PackageVersion.V13;
                    break;
                case Game.BaldursGate3:
                    packageVersion = PackageVersion.V16;
                    break;
                default:
                    throw new ArgumentException($"Unknown game: \"{gameVersion}\"");
            }
            
            CommandLineLogger.LogDebug($"Using package version: {packageVersion}");

            return packageVersion;
        }

        public static Game GetGameByPackageVersion(string packagePath)
        {
            Game gameVersion;
            
            try
            {
                using (var reader = new PackageReader(packagePath))
                {
                    Package package = reader.Read();
                    switch (package.Version)
                    {
                        case PackageVersion.V7:
                            gameVersion = Game.DivinityOriginalSin;
                            break;
                        case PackageVersion.V9:
                            gameVersion = Game.DivinityOriginalSinEE;
                            break;
                        case PackageVersion.V10:
                            gameVersion = Game.DivinityOriginalSin2;
                            break;
                        case PackageVersion.V13:
                            gameVersion = Game.DivinityOriginalSin2DE;
                            break;
                        case PackageVersion.V15:
                        case PackageVersion.V16:
                            gameVersion = Game.BaldursGate3;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            catch (Exception e)
            {
                CommandLineLogger.LogFatal($"Cannot determine game from package version: {e.Message}", 3);
                throw;
            }
            
            CommandLineLogger.LogDebug($"Detected game from package version: \"{gameVersion}\"");

            return gameVersion;
        }
    }
}
