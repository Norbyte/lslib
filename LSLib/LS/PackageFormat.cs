using LSLib.LS.Enums;

namespace LSLib.LS;

public class PackageHeaderCommon
{
    public const PackageVersion CurrentVersion = PackageVersion.V18;
    public const UInt32 Signature = 0x4B50534C;

    public UInt32 Version;
    public UInt64 FileListOffset;
    // Size of file list; used for legacy (<= v10) packages only
    public UInt32 FileListSize;
    // Number of packed files; used for legacy (<= v10) packages only
    public UInt32 NumFiles;
    public UInt32 NumParts;
    // Offset of packed data in archive part 0; used for legacy (<= v10) packages only
    public UInt32 DataOffset;
    public PackageFlags Flags;
    public Byte Priority;
    public byte[] Md5;
}

internal interface ILSPKHeader
{
    public PackageHeaderCommon ToCommonHeader();
    abstract public static ILSPKHeader FromCommonHeader(PackageHeaderCommon h);
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSPKHeader7 : ILSPKHeader
{
    public UInt32 Version;
    public UInt32 DataOffset;
    public UInt32 NumParts;
    public UInt32 FileListSize;
    public Byte LittleEndian;
    public UInt32 NumFiles;

    public readonly PackageHeaderCommon ToCommonHeader()
    {
        return new PackageHeaderCommon
        {
            Version = Version,
            DataOffset = DataOffset,
            FileListOffset = (ulong)Marshal.SizeOf(typeof(LSPKHeader7)),
            FileListSize = FileListSize,
            NumFiles = NumFiles,
            NumParts = NumParts,
            Flags = 0,
            Priority = 0,
            Md5 = null
        };
    }

    public static ILSPKHeader FromCommonHeader(PackageHeaderCommon h)
    {
        return new LSPKHeader7
        {
            Version = h.Version,
            DataOffset = h.DataOffset,
            NumParts = h.NumParts,
            FileListSize = h.FileListSize,
            LittleEndian = 0,
            NumFiles = h.NumFiles
        };
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSPKHeader10 : ILSPKHeader
{
    public UInt32 Version;
    public UInt32 DataOffset;
    public UInt32 FileListSize;
    public UInt16 NumParts;
    public Byte Flags;
    public Byte Priority;
    public UInt32 NumFiles;

    public readonly PackageHeaderCommon ToCommonHeader()
    {
        return new PackageHeaderCommon
        {
            Version = Version,
            DataOffset = DataOffset,
            FileListOffset = (ulong)Marshal.SizeOf(typeof(LSPKHeader7)),
            FileListSize = FileListSize,
            NumFiles = NumFiles,
            NumParts = NumParts,
            Flags = (PackageFlags)Flags,
            Priority = Priority,
            Md5 = null
        };
    }

    public static ILSPKHeader FromCommonHeader(PackageHeaderCommon h)
    {
        return new LSPKHeader10
        {
            Version = h.Version,
            DataOffset = h.DataOffset,
            FileListSize = h.FileListSize,
            NumParts = (UInt16)h.NumParts,
            Flags = (byte)h.Flags,
            Priority = h.Priority,
            NumFiles = h.NumFiles
        };
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct LSPKHeader13 : ILSPKHeader
{
    public UInt32 Version;
    public UInt32 FileListOffset;
    public UInt32 FileListSize;
    public UInt16 NumParts;
    public Byte Flags;
    public Byte Priority;
    public fixed byte Md5[16];

    public readonly PackageHeaderCommon ToCommonHeader()
    {
        var header = new PackageHeaderCommon
        {
            Version = Version,
            DataOffset = 0,
            FileListOffset = FileListOffset,
            FileListSize = FileListSize,
            NumParts = NumParts,
            Flags = (PackageFlags)Flags,
            Priority = Priority,
            Md5 = new byte[16]
        };

        fixed (byte* md = Md5)
        {
            Marshal.Copy(new IntPtr(md), header.Md5, 0, 0x10);
        }

        return header;
    }

    public static ILSPKHeader FromCommonHeader(PackageHeaderCommon h)
    {
        var header = new LSPKHeader13
        {
            Version = h.Version,
            FileListOffset = (UInt32)h.FileListOffset,
            FileListSize = h.FileListSize,
            NumParts = (UInt16)h.NumParts,
            Flags = (byte)h.Flags,
            Priority = h.Priority
        };

        Marshal.Copy(h.Md5, 0, new IntPtr(header.Md5), 0x10);
        return header;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct LSPKHeader15 : ILSPKHeader
{
    public UInt32 Version;
    public UInt64 FileListOffset;
    public UInt32 FileListSize;
    public Byte Flags;
    public Byte Priority;
    public fixed byte Md5[16];

    public readonly PackageHeaderCommon ToCommonHeader()
    {
        var header = new PackageHeaderCommon
        {
            Version = Version,
            DataOffset = 0,
            FileListOffset = FileListOffset,
            FileListSize = FileListSize,
            NumParts = 1,
            Flags = (PackageFlags)Flags,
            Priority = Priority,
            Md5 = new byte[16]
        };

        fixed (byte* md = Md5)
        {
            Marshal.Copy(new IntPtr(md), header.Md5, 0, 0x10);
        }

        return header;
    }

    public static ILSPKHeader FromCommonHeader(PackageHeaderCommon h)
    {
        var header = new LSPKHeader15
        {
            Version = h.Version,
            FileListOffset = (UInt32)h.FileListOffset,
            FileListSize = h.FileListSize,
            Flags = (byte)h.Flags,
            Priority = h.Priority
        };

        Marshal.Copy(h.Md5, 0, new IntPtr(header.Md5), 0x10);
        return header;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct LSPKHeader16 : ILSPKHeader
{
    public UInt32 Version;
    public UInt64 FileListOffset;
    public UInt32 FileListSize;
    public Byte Flags;
    public Byte Priority;
    public fixed byte Md5[16];

    public UInt16 NumParts;

    public readonly PackageHeaderCommon ToCommonHeader()
    {
        var header = new PackageHeaderCommon
        {
            Version = Version,
            FileListOffset = FileListOffset,
            FileListSize = FileListSize,
            NumParts = NumParts,
            Flags = (PackageFlags)Flags,
            Priority = Priority,
            Md5 = new byte[16]
        };

        fixed (byte* md = Md5)
        {
            Marshal.Copy(new IntPtr(md), header.Md5, 0, 0x10);
        }

        return header;
    }

    public static ILSPKHeader FromCommonHeader(PackageHeaderCommon h)
    {
        var header = new LSPKHeader16
        {
            Version = h.Version,
            FileListOffset = (UInt32)h.FileListOffset,
            FileListSize = h.FileListSize,
            Flags = (byte)h.Flags,
            Priority = h.Priority,
            NumParts = (UInt16)h.NumParts
        };

        Marshal.Copy(h.Md5, 0, new IntPtr(header.Md5), 0x10);
        return header;
    }
}

[Flags]
public enum PackageFlags
{
    /// <summary>
    /// Allow memory-mapped access to the files in this archive.
    /// </summary>
    AllowMemoryMapping = 0x02,
    /// <summary>
    /// All files are compressed into a single LZ4 stream
    /// </summary>
    Solid = 0x04,
    /// <summary>
    /// Archive contents should be preloaded on game startup.
    /// </summary>
    Preload = 0x08
};



abstract public class PackagedFileInfoCommon
{
    public string Name;
    public UInt32 ArchivePart;
    public UInt32 Crc;
    public CompressionFlags Flags;
    public UInt64 OffsetInFile;
    public UInt64 SizeOnDisk;
    public UInt64 UncompressedSize;
}

internal interface ILSPKFile
{
    public void ToCommon(PackagedFileInfoCommon info);
    abstract public static ILSPKFile FromCommon(PackagedFileInfoCommon info);
    public UInt16 ArchivePartNumber();
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct FileEntry7 : ILSPKFile
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public byte[] Name;

    public UInt32 OffsetInFile;
    public UInt32 SizeOnDisk;
    public UInt32 UncompressedSize;
    public UInt32 ArchivePart;

    public readonly void ToCommon(PackagedFileInfoCommon info)
    {
        info.Name = BinUtils.NullTerminatedBytesToString(Name);
        info.ArchivePart = ArchivePart;
        info.Crc = 0;
        info.Flags = UncompressedSize > 0 ? BinUtils.MakeCompressionFlags(CompressionMethod.Zlib, LSCompressionLevel.Default) : 0;
        info.OffsetInFile = OffsetInFile;
        info.SizeOnDisk = SizeOnDisk;
        info.UncompressedSize = UncompressedSize;
    }

    public static ILSPKFile FromCommon(PackagedFileInfoCommon info)
    {
        return new FileEntry7
        {
            Name = BinUtils.StringToNullTerminatedBytes(info.Name, 256),
            OffsetInFile = (uint)info.OffsetInFile,
            SizeOnDisk = (uint)info.SizeOnDisk,
            UncompressedSize = info.Flags.Method() == CompressionMethod.None ? 0 : (uint)info.UncompressedSize,
            ArchivePart = info.ArchivePart
        };
    }

    public readonly UInt16 ArchivePartNumber() => (UInt16)ArchivePart;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct FileEntry10 : ILSPKFile
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public byte[] Name;

    public UInt32 OffsetInFile;
    public UInt32 SizeOnDisk;
    public UInt32 UncompressedSize;
    public UInt32 ArchivePart;
    public UInt32 Flags;
    public UInt32 Crc;

    public readonly void ToCommon(PackagedFileInfoCommon info)
    {
        info.Name = BinUtils.NullTerminatedBytesToString(Name);
        info.ArchivePart = ArchivePart;
        info.Crc = Crc;
        info.Flags = (CompressionFlags)Flags;
        info.OffsetInFile = OffsetInFile;
        info.SizeOnDisk = SizeOnDisk;
        info.UncompressedSize = UncompressedSize;
    }

    public static ILSPKFile FromCommon(PackagedFileInfoCommon info)
    {
        return new FileEntry10
        {
            Name = BinUtils.StringToNullTerminatedBytes(info.Name, 256),
            OffsetInFile = (uint)info.OffsetInFile,
            SizeOnDisk = (uint)info.SizeOnDisk,
            UncompressedSize = info.Flags.Method() == CompressionMethod.None ? 0 : (uint)info.UncompressedSize,
            ArchivePart = info.ArchivePart,
            Flags = (byte)info.Flags,
            Crc = info.Crc
        };
    }

    public readonly UInt16 ArchivePartNumber() => (UInt16)ArchivePart;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct FileEntry15 : ILSPKFile
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public byte[] Name;

    public UInt64 OffsetInFile;
    public UInt64 SizeOnDisk;
    public UInt64 UncompressedSize;
    public UInt32 ArchivePart;
    public UInt32 Flags;
    public UInt32 Crc;
    public UInt32 Unknown2;

    public readonly void ToCommon(PackagedFileInfoCommon info)
    {
        info.Name = BinUtils.NullTerminatedBytesToString(Name);
        info.ArchivePart = ArchivePart;
        info.Crc = Crc;
        info.Flags = (CompressionFlags)Flags;
        info.OffsetInFile = OffsetInFile;
        info.SizeOnDisk = SizeOnDisk;
        info.UncompressedSize = UncompressedSize;
    }

    public static ILSPKFile FromCommon(PackagedFileInfoCommon info)
    {
        return new FileEntry15
        {
            Name = BinUtils.StringToNullTerminatedBytes(info.Name, 256),
            OffsetInFile = (uint)info.OffsetInFile,
            SizeOnDisk = (uint)info.SizeOnDisk,
            UncompressedSize = info.Flags.Method() == CompressionMethod.None ? 0 : (uint)info.UncompressedSize,
            ArchivePart = info.ArchivePart,
            Flags = (Byte)info.Flags,
            Crc = info.Crc,
            Unknown2 = 0
        };
    }

    public readonly UInt16 ArchivePartNumber() => (UInt16)ArchivePart;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct FileEntry18 : ILSPKFile
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public byte[] Name;

    public UInt32 OffsetInFile1;
    public UInt16 OffsetInFile2;
    public Byte ArchivePart;
    public Byte Flags;
    public UInt32 SizeOnDisk;
    public UInt32 UncompressedSize;

    public readonly void ToCommon(PackagedFileInfoCommon info)
    {
        info.Name = BinUtils.NullTerminatedBytesToString(Name);
        info.ArchivePart = ArchivePart;
        info.Crc = 0;
        info.Flags = (CompressionFlags)Flags;
        info.OffsetInFile = OffsetInFile1 | ((ulong)OffsetInFile2 << 32);
        info.SizeOnDisk = SizeOnDisk;
        info.UncompressedSize = UncompressedSize;
    }

    public static ILSPKFile FromCommon(PackagedFileInfoCommon info)
    {
        return new FileEntry18
        {
            Name = BinUtils.StringToNullTerminatedBytes(info.Name, 256),
            OffsetInFile1 = (uint)(info.OffsetInFile & 0xffffffff),
            OffsetInFile2 = (ushort)((info.OffsetInFile >> 32) & 0xffff),
            ArchivePart = (byte)info.ArchivePart,
            Flags = (byte)info.Flags,
            SizeOnDisk = (uint)info.SizeOnDisk,
            UncompressedSize = info.Flags.Method() == CompressionMethod.None ? 0 : (uint)info.UncompressedSize
        };
    }

    public readonly UInt16 ArchivePartNumber() => ArchivePart;
}
