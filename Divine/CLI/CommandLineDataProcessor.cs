using System;
using Divine.Enums;
using LSLib.LS;

namespace Divine.CLI
{
    internal class CommandLineDataProcessor
    {
        public static void Convert() => ConvertResource(
            CommandLineActions.SourcePath,
            CommandLineActions.DestinationPath,
            CommandLineArguments.GetFileVersionByGame(CommandLineActions.Game)
        );

        public static void BatchConvert() => BatchConvertResource(
            CommandLineActions.SourcePath,
            CommandLineActions.DestinationPath,
            CommandLineActions.InputFormat,
            CommandLineActions.OutputFormat,
            CommandLineArguments.GetFileVersionByGame(CommandLineActions.Game)
        );

        private static void ConvertResource(string sourcePath, string destinationPath, int fileVersion)
        {
            try
            {
                Resource resource = ResourceUtils.LoadResource(sourcePath);
                ResourceFormat resourceFormat = ResourceUtils.ExtensionToResourceFormat(sourcePath);

                ResourceUtils.SaveResource(resource, destinationPath, resourceFormat, fileVersion);

                CommandLineLogger.LogInfo($"Wrote resource to: {destinationPath}");
            }
            catch (Exception e)
            {
                CommandLineLogger.LogFatal($"Failed to convert resource: {e.Message}{Environment.NewLine}{e.StackTrace}");
            }
        }

        private static void BatchConvertResource(string sourcePath, string destinationPath, ResourceFormat inputFormat, ResourceFormat outputFormat, int fileVersion)
        {
            try
            {
                ResourceUtils resourceUtils = new ResourceUtils();

                resourceUtils.ConvertResources(sourcePath, destinationPath, inputFormat, outputFormat, fileVersion);

                CommandLineLogger.LogInfo($"Wrote resources to: {destinationPath}");
            }
            catch (Exception e)
            {
                CommandLineLogger.LogFatal($"Failed to batch convert resources: {e.Message}{Environment.NewLine}{e.StackTrace}");
            }
        }
    }
}
