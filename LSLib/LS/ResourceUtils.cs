using LSLib.LS.LSF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS
{
    public enum ResourceFormat
    {
        LSX,
        LSB,
        LSF
    };


    public class ResourceUtils
    {
        public delegate void ProgressUpdateDelegate(string status, long numerator, long denominator);
        public ProgressUpdateDelegate progressUpdate = delegate { };

        public static ResourceFormat ExtensionToResourceFormat(string path)
        {
            var extension = Path.GetExtension(path).ToLower();

            switch (extension)
            {
                case ".lsx":
                    return ResourceFormat.LSX;

                case ".lsb":
                    return ResourceFormat.LSB;

                case ".lsf":
                    return ResourceFormat.LSF;

                default:
                    throw new ArgumentException("Unrecognized file extension: " + extension);
            }
        }

        public static Resource LoadResource(string inputPath)
        {
            return LoadResource(inputPath, ExtensionToResourceFormat(inputPath));
        }

        public static Resource LoadResource(string inputPath, ResourceFormat format)
        {
            var file = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
            switch (format)
            {
                case ResourceFormat.LSX:
                    {
                        using (var reader = new LSXReader(file))
                        {
                            return reader.Read();
                        }
                    }

                case ResourceFormat.LSB:
                    {
                        using (var reader = new LSBReader(file))
                        {
                            return reader.Read();
                        }
                    }

                case ResourceFormat.LSF:
                    {
                        using (var reader = new LSFReader(file))
                        {
                            return reader.Read();
                        }
                    }

                default:
                    throw new ArgumentException("Invalid resource format");
            }
        }

        public static void SaveResource(Resource resource, string outputPath)
        {
            SaveResource(resource, outputPath, ExtensionToResourceFormat(outputPath));
        }

        public static void SaveResource(Resource resource, string outputPath, ResourceFormat format)
        {
            var file = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            switch (format)
            {
                case ResourceFormat.LSX:
                    {
                        using (var writer = new LSXWriter(file))
                        {
                            writer.PrettyPrint = true;
                            writer.Write(resource);
                        }
                        break;
                    }

                case ResourceFormat.LSB:
                    {
                        using (var writer = new LSBWriter(file))
                        {
                            writer.Write(resource);
                        }
                        break;
                    }

                case ResourceFormat.LSF:
                    {
                        // Write in V2 format for D:OS EE compatibility
                        using (var writer = new LSFWriter(file, Header.VerChunkedCompress))
                        {
                            writer.Write(resource);
                        }
                        break;
                    }

                default:
                    throw new ArgumentException("Invalid resource format");
            }
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

        public void ConvertResources(string inputDir, string outputDir, ResourceFormat inputFormat, ResourceFormat outputFormat)
        {
            this.progressUpdate("Enumerating files ...", 0, 1);
            var paths = new List<string>();
            EnumerateFiles(paths, inputDir, inputDir, "." + inputFormat.ToString().ToLower());

            this.progressUpdate("Converting resources ...", 0, 1);
            for (var i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                var inPath = inputDir + "/" + path;
                var outPath = outputDir + "/" + Path.ChangeExtension(path, outputFormat.ToString().ToLower());
                var dirName = Path.GetDirectoryName(outPath);
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }

                this.progressUpdate("Converting: " + inPath, i, paths.Count);
                var resource = LoadResource(inPath, inputFormat);
                SaveResource(resource, outPath, outputFormat);
            }
        }
    }
}
