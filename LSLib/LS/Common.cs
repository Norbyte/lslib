using System;

namespace LSLib.LS
{
    public static class Common
    {
        public const int MajorVersion = 1;
        public const int MinorVersion = 8;
        public const int PatchVersion = 6;

        /// <summary>
        /// Returns the version number of the LSLib library
        /// </summary>
        public static string LibraryVersion()
        {
            return String.Format("{0}.{1}.{2}", MajorVersion, MinorVersion, PatchVersion);
        }
    }
}
