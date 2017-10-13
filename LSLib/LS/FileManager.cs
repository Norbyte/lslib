using System.IO;

namespace LSLib.LS
{
    internal class FileManager
    {
        public static bool TryToCreateDirectory(string path)
        {
            if (path.Length <= 0)
            {
                return false;
            }

            try
            {
                // verify this is a path
                string outputPath = Path.GetFullPath(path);

                // get only path to directory if a filename was found
                if (Path.GetExtension(outputPath) != string.Empty)
                {
                    outputPath = Path.GetDirectoryName(outputPath);
                }

                if (outputPath == null)
                {
                    return false;
                }

                // create directory only if the directory does not exist
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}
