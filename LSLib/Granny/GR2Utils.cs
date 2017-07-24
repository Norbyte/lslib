using LSLib.Granny.Model;
using LSLib.LS.LSF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.Granny
{
    public class GR2Utils
    {
        public delegate void ProgressUpdateDelegate(string status, long numerator, long denominator);
        public ProgressUpdateDelegate progressUpdate = delegate { };

        public delegate void ConversionErrorDelegate(string inputPath, string outputPath, Exception exc);
        public ConversionErrorDelegate conversionError = delegate { };

        public static ExportFormat ExtensionToModelFormat(string path)
        {
            var extension = Path.GetExtension(path).ToLower();

            switch (extension)
            {
                case ".gr2":
                    return ExportFormat.GR2;

                case ".dae":
                    return ExportFormat.DAE;

                default:
                    throw new ArgumentException("Unrecognized model file extension: " + extension);
            }
        }

        public static Root LoadModel(string inputPath)
        {
            return LoadModel(inputPath, ExtensionToModelFormat(inputPath));
        }

        public static Root LoadModel(string inputPath, ExportFormat format)
        {
            switch (format)
            {
                case ExportFormat.GR2:
                    {
                        using (var fs = new FileStream(inputPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite))
                        {
                            var root = new LSLib.Granny.Model.Root();
                            var gr2 = new LSLib.Granny.GR2.GR2Reader(fs);
                            gr2.Read(root);
                            root.PostLoad();
                            return root;
                        }
                    }

                case ExportFormat.DAE:
                    {
                        var importer = new ColladaImporter();
                        var root = importer.Import(inputPath);
                        return root;
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

        private void EnumerateFiles(List<string> paths, string rootPath, string currentPath, string extension)
        {
            foreach (string filePath in Directory.GetFiles(currentPath))
            {
                var fileExtension = Path.GetExtension(filePath);
                if (fileExtension.ToLower() == extension)
                {
                    var relativePath = filePath.Substring(rootPath.Length);
                    if (relativePath[0] == '/' || relativePath[0] == '\\')
                    {
                        relativePath = relativePath.Substring(1);
                    }

                    paths.Add(relativePath);
                }
            }

            foreach (string directoryPath in Directory.GetDirectories(currentPath))
            {
                EnumerateFiles(paths, rootPath, directoryPath, extension);
            }
        }

        public void ConvertModels(string inputDir, string outputDir, Exporter exporter)
        {
            this.progressUpdate("Enumerating files ...", 0, 1);
            var paths = new List<string>();
            EnumerateFiles(paths, inputDir, inputDir, "." + exporter.Options.InputFormat.ToString().ToLower());

            this.progressUpdate("Converting resources ...", 0, 1);
            for (var i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                var inPath = inputDir + "/" + path;
                var outPath = outputDir + "/" + Path.ChangeExtension(path, exporter.Options.OutputFormat.ToString().ToLower());
                var dirName = Path.GetDirectoryName(outPath);
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }

                this.progressUpdate("Converting: " + inPath, i, paths.Count);
                try
                {
                    var model = LoadModel(inPath, exporter.Options.InputFormat);
                    SaveModel(model, outPath, exporter);
                }
                catch (Exception exc)
                {
                    conversionError(inPath, outPath, exc);
                }
            }
        }
    }
}
