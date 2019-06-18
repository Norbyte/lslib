using System;
using Alphaleonis.Win32.Filesystem;

namespace LSLib.LS
{
    public class FileManager
    {
        public static void TryToCreateDirectory(string path)
        {
            string outputPath = path;

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentNullException(nameof(path), "Cannot create directory without path");
            }

            // throw exception if path is relative
            Uri uri;
            try
            {
                Uri.TryCreate(outputPath, UriKind.RelativeOrAbsolute, out uri);
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException("Cannot create directory without absolute path", nameof(path));
            }

            if (!Path.IsPathRooted(outputPath) || !uri.IsFile)
            {
                throw new ArgumentException("Cannot create directory without absolute path", nameof(path));
            }

            // validate path
            outputPath = Path.GetFullPath(path);

            outputPath = Path.GetDirectoryName(outputPath);

            if (outputPath == null)
            {
                throw new NullReferenceException("Cannot create directory without non-null output path");
            }
            
            // if the directory does not exist, create the directory
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
        }
    }
}
