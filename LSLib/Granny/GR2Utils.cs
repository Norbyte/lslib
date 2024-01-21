using LSLib.Granny.GR2;
using LSLib.Granny.Model;
using LSLib.LS;

namespace LSLib.Granny;

public class GR2Utils
{
    public delegate void ConversionErrorDelegate(string inputPath, string outputPath, Exception exc);

    public delegate void ProgressUpdateDelegate(string status, long numerator, long denominator);

    public ConversionErrorDelegate ConversionError = delegate { };
    public ProgressUpdateDelegate ProgressUpdate = delegate { };

    public static ExportFormat ExtensionToModelFormat(string path)
    {
        string extension = Path.GetExtension(path)?.ToLower();

        return extension switch
        {
            ".gr2" or ".lsm" => ExportFormat.GR2,
            ".dae" => ExportFormat.DAE,
            _ => throw new ArgumentException($"Unrecognized model file extension: {extension}"),
        };
    }

    public static Root LoadModel(string inputPath)
    {
        var options = new ExporterOptions
        {
            InputFormat = ExtensionToModelFormat(inputPath)
        };
        return LoadModel(inputPath, options);
    }

    public static Root LoadModel(string inputPath, ExporterOptions options)
    {
        switch (options.InputFormat)
        {
            case ExportFormat.GR2:
            {
                using var fs = File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var root = new Root();
                var gr2 = new GR2Reader(fs);
                gr2.Read(root);
                root.PostLoad(gr2.Tag);
                return root;
            }

            case ExportFormat.DAE:
            {
                var importer = new ColladaImporter
                {
                    Options = options
                };
                return importer.Import(inputPath);
            }

            default:
                throw new ArgumentException("Invalid model format");
        }
    }

    public static void SaveModel(Root model, string outputPath, Exporter exporter)
    {
        exporter.Options.InputPath = null;
        exporter.Options.Input = model;
        exporter.Options.OutputPath = outputPath;
        exporter.Export();
    }

    private static List<string> EnumerateFiles(string path, ExportFormat format)
    {
        if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            path += Path.DirectorySeparatorChar;
        }

        return Directory.EnumerateFiles(path, $"*.{format.ToString().ToLower()}", SearchOption.AllDirectories).ToList();
    }

    public void ConvertModels(string inputDirectoryPath, string outputDirectoryPath, Exporter exporter)
    {
        string outputExtension = exporter.Options.OutputFormat.ToString().ToLower();

        ProgressUpdate("Enumerating files ...", 0, 1);
        List<string> inputFilePaths = EnumerateFiles(inputDirectoryPath, exporter.Options.InputFormat);

        ProgressUpdate("Converting resources ...", 0, 1);
        for (var i = 0; i < inputFilePaths.Count; i++)
        {
            string inputFilePath = inputFilePaths[i];

            string outputFilePath = Path.ChangeExtension(inputFilePath.Replace(inputDirectoryPath, outputDirectoryPath), outputExtension);

            FileManager.TryToCreateDirectory(outputFilePath);

            ProgressUpdate($"Converting: {inputFilePath}", i, inputFilePaths.Count);
            try
            {
                Root model = LoadModel(inputFilePath, exporter.Options);
                SaveModel(model, outputFilePath, exporter);
            }
            catch (Exception exc)
            {
                ConversionError(inputFilePath, outputFilePath, exc);
            }
        }
    }
}
