using System;
using LSLib.LS;
using LSLib.LS.Enums;

namespace Divine.CLI
{
    internal class CommandLineDataProcessor
    {
        public static void Convert()
        {
            ConvertResource(CommandLineActions.SourcePath, CommandLineActions.DestinationPath, CommandLineArguments.GetFileVersionByGame(CommandLineActions.Game));
        }

        public static void BatchConvert()
        {
            BatchConvertResource(CommandLineActions.SourcePath, CommandLineActions.DestinationPath, CommandLineActions.InputFormat, CommandLineActions.OutputFormat, CommandLineArguments.GetFileVersionByGame(CommandLineActions.Game));
        }

        private static void ConvertResource(string sourcePath, string destinationPath, FileVersion fileVersion)
        {
            try
            {
                ResourceFormat resourceFormat = ResourceUtils.ExtensionToResourceFormat(destinationPath);
                CommandLineLogger.LogDebug($"Using destination extension: {resourceFormat}");

                Resource resource = ResourceUtils.LoadResource(sourcePath);

                ResourceUtils.SaveResource(resource, destinationPath, resourceFormat, fileVersion);

                CommandLineLogger.LogInfo($"Wrote resource to: {destinationPath}");
            }
            catch (Exception e)
            {
                CommandLineLogger.LogFatal($"Failed to convert resource: {e.Message}", 2);
                CommandLineLogger.LogTrace($"{e.StackTrace}");
            }
        }

        private static void BatchConvertResource(string sourcePath, string destinationPath, ResourceFormat inputFormat, ResourceFormat outputFormat, FileVersion fileVersion)
        {
            try
            {
                CommandLineLogger.LogDebug($"Using destination extension: {outputFormat}");

                var resourceUtils = new ResourceUtils();
                resourceUtils.ConvertResources(sourcePath, destinationPath, inputFormat, outputFormat, fileVersion);

                CommandLineLogger.LogInfo($"Wrote resources to: {destinationPath}");
            }
            catch (Exception e)
            {
                CommandLineLogger.LogFatal($"Failed to batch convert resources: {e.Message}", 2);
                CommandLineLogger.LogTrace($"{e.StackTrace}");
            }
        }
    }
}
