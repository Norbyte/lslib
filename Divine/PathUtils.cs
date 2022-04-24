using System;
using System.IO;

namespace Divine
{
    public static class PathUtils
    {
        public static bool IsDir(string path)
        {
            FileAttributes attr;
            
            try
            {
                attr = File.GetAttributes(path);
            }
            catch (Exception)
            {
                return false;
            }

            return attr.HasFlag(FileAttributes.Directory);
        }

        public static bool IsFile(string path)
        {
            FileAttributes attr;
            
            try
            {
                attr = File.GetAttributes(path);
            }
            catch (Exception)
            {
                return false;
            }

            return !attr.HasFlag(FileAttributes.Directory);
        }
    }
}