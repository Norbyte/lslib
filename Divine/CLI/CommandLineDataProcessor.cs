using System;
using LSLib.LS;
using LSLib.LS.Enums;

namespace Divine.CLI
{
    internal static class CommandLineDataProcessor
    {
        public static void Convert()
        {
            ResourceConversionParameters conversionParams = ResourceConversionParameters.FromGameVersion(CommandLineActions.Game);
            ConvertResource(CommandLineActions.SourcePath, CommandLineActions.DestinationPath, conversionParams);
        }

        public static void BatchConvert()
        {
            ResourceConversionParameters conversionParams = ResourceConversionParameters.FromGameVersion(CommandLineActions.Game);
            BatchConvertResource(CommandLineActions.SourcePath, CommandLineActions.DestinationPath, CommandLineActions.InputFormat, CommandLineActions.OutputFormat, conversionParams);
        }

        private static void ConvertResource(string sourcePath, string destinationPath, ResourceConversionParameters conversionParams)
        {
            try
            {
                ResourceFormat resourceFormat = ResourceUtils.ExtensionToResourceFormat(destinationPath);
                CommandLineLogger.LogDebug($"Using destination extension: {resourceFormat}");

                Resource resource = ResourceUtils.LoadResource(sourcePath);

                ResourceUtils.SaveResource(resource, destinationPath, resourceFormat, conversionParams);

                CommandLineLogger.LogInfo($"Wrote resource to: {destinationPath}");
            }
            catch (Exception e)
            {
                CommandLineLogger.LogFatal($"Failed to convert resource: {e.Message}", 2);
                CommandLineLogger.LogTrace($"{e.StackTrace}");
            }
        }

        private static void BatchConvertResource(string sourcePath, string destinationPath, ResourceFormat inputFormat, ResourceFormat outputFormat, ResourceConversionParameters conversionParams)
        {
            try
            {
                CommandLineLogger.LogDebug($"Using destination extension: {outputFormat}");

                var resourceUtils = new ResourceUtils();
                resourceUtils.ConvertResources(sourcePath, destinationPath, inputFormat, outputFormat, conversionParams);

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
