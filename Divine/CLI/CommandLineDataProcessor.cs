using System;
using LSLib.LS;
using LSLib.LS.Enums;

namespace Divine.CLI
{
    internal class CommandLineDataProcessor
    {
        public static void Convert()
        {
            var conversionParams = ResourceConversionParameters.FromGameVersion(CommandLineActions.Game);
            var loadParams = ResourceLoadParameters.FromGameVersion(CommandLineActions.Game);
            loadParams.ByteSwapGuids = !CommandLineActions.LegacyGuids;
            ConvertResource(CommandLineActions.SourcePath, CommandLineActions.DestinationPath, loadParams, conversionParams);
        }

        public static void BatchConvert()
        {
            var conversionParams = ResourceConversionParameters.FromGameVersion(CommandLineActions.Game);
            var loadParams = ResourceLoadParameters.FromGameVersion(CommandLineActions.Game);
            loadParams.ByteSwapGuids = !CommandLineActions.LegacyGuids;
            BatchConvertResource(CommandLineActions.SourcePath, CommandLineActions.DestinationPath, CommandLineActions.InputFormat, CommandLineActions.OutputFormat, loadParams, conversionParams);
        }

        private static void ConvertResource(string sourcePath, string destinationPath,
            ResourceLoadParameters loadParams, ResourceConversionParameters conversionParams)
        {
            try
            {
                ResourceFormat resourceFormat = ResourceUtils.ExtensionToResourceFormat(destinationPath);
                CommandLineLogger.LogDebug($"Using destination extension: {resourceFormat}");

                Resource resource = ResourceUtils.LoadResource(sourcePath, loadParams);

                ResourceUtils.SaveResource(resource, destinationPath, resourceFormat, conversionParams);

                CommandLineLogger.LogInfo($"Wrote resource to: {destinationPath}");
            }
            catch (Exception e)
            {
                CommandLineLogger.LogFatal($"Failed to convert resource: {e.Message}", 2);
                CommandLineLogger.LogTrace($"{e.StackTrace}");
            }
        }

        public static void ConvertLoca()
        {
            ConvertLoca(CommandLineActions.SourcePath, CommandLineActions.DestinationPath);
        }

        private static void ConvertLoca(string sourcePath, string destinationPath)
        {
            try
            {
                var loca = LocaUtils.Load(sourcePath);
                LocaUtils.Save(loca, destinationPath);
                CommandLineLogger.LogInfo($"Wrote localization to: {destinationPath}");
            }
            catch (Exception e)
            {
                CommandLineLogger.LogFatal($"Failed to convert localization file: {e.Message}", 2);
                CommandLineLogger.LogTrace($"{e.StackTrace}");
            }
        }


        private static void BatchConvertResource(string sourcePath, string destinationPath, ResourceFormat inputFormat, ResourceFormat outputFormat, 
            ResourceLoadParameters loadParams, ResourceConversionParameters conversionParams)
        {
            try
            {
                CommandLineLogger.LogDebug($"Using destination extension: {outputFormat}");

                var resourceUtils = new ResourceUtils();
                resourceUtils.ConvertResources(sourcePath, destinationPath, inputFormat, outputFormat, loadParams, conversionParams);

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
