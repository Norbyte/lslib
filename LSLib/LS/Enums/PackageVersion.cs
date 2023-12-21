using System;

namespace LSLib.LS.Enums;

public enum PackageVersion
{
    V7 = 7, // D:OS 1
    V9 = 9, // D:OS 1 EE
    V10 = 10, // D:OS 2
    V13 = 13, // D:OS 2 DE
    V15 = 15, // BG3 EA
    V16 = 16, // BG3 EA Patch4
    V18 = 18 // BG3 Release
};
public static class PackageVersionExtensions
{
    public static bool HasCrc(this PackageVersion ver)
    {
        return ver >= PackageVersion.V10 && ver <= PackageVersion.V16;
    }

    public static long MaxPackageSize(this PackageVersion ver)
    {
        if (ver <= PackageVersion.V15)
        {
            return 0x40000000;
        }
        else
        {
            return 0x100000000;
        }
    }

    public static int PaddingSize(this PackageVersion ver)
    {
        if (ver <= PackageVersion.V9)
        {
            return 0x1000;
        }
        else
        {
            return 0x40;
        }
    }
}
