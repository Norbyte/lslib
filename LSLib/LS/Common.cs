using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS
{
    public static class Common
    {
        public const int MajorVersion = 1;
        public const int MinorVersion = 6;
        public const int PatchVersion = 2;

        /// <summary>
        /// Returns the version number of the LSLib library
        /// </summary>
        public static string LibraryVersion()
        {
            return String.Format("{0}.{1}.{2}", MajorVersion, MinorVersion, PatchVersion);
        }
    }
}
