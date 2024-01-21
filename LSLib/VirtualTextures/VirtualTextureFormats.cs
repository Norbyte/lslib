namespace LSLib.VirtualTextures;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DDSHeader
{
    public const UInt32 DDSMagic = 0x20534444;
    public const UInt32 HeaderSize = 0x7c;
    public const UInt32 FourCC_DXT5 = 0x35545844;

    public UInt32 dwMagic;
    public UInt32 dwSize;
    public UInt32 dwFlags;
    public UInt32 dwHeight;
    public UInt32 dwWidth;
    public UInt32 dwPitchOrLinearSize;
    public UInt32 dwDepth;
    public UInt32 dwMipMapCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
    public UInt32[] dwReserved1;

    public UInt32 dwPFSize;
    public UInt32 dwPFFlags;
    public UInt32 dwFourCC;
    public UInt32 dwRGBBitCount;
    public UInt32 dwRBitMask;
    public UInt32 dwGBitMask;
    public UInt32 dwBBitMask;
    public UInt32 dwABitMask;

    public UInt32 dwCaps;
    public UInt32 dwCaps2;
    public UInt32 dwCaps3;
    public UInt32 dwCaps4;
    public UInt32 dwReserved2;

    public string FourCCName
    {
        get
        {
            return Char.ToString((char)(dwFourCC & 0xff))
                + Char.ToString((char)((dwFourCC >> 8) & 0xff))
                + Char.ToString((char)((dwFourCC >> 16) & 0xff))
                + Char.ToString((char)((dwFourCC >> 24) & 0xff));
        }

        set
        {
            dwFourCC = (uint)value[0]
                | ((uint)value[1] << 8)
                | ((uint)value[2] << 16)
                | ((uint)value[3] << 24);
        }
    }
};

public enum GTSDataType : UInt32
{
    R8G8B8_SRGB = 0,
    R8G8B8A8_SRGB = 1,
    X8Y8Z0_TANGENT = 2,
    R8G8B8_LINEAR = 3,
    R8G8B8A8_LINEAR = 4,
    X8 = 5,
    X8Y8 = 6,
    X8Y8Z8 = 7,
    X8Y8Z8W8 = 8,
    X16 = 9,
    X16Y16 = 10,
    X16Y16Z16 = 11,
    X16Y16Z16W16 = 12,
    X32 = 13,
    X32_FLOAT = 14,
    X32Y32 = 15,
    X32Y32_FLOAT = 16,
    X32Y32Z32 = 17,
    X32Y32Z32_FLOAT = 18,
    R32G32B32 = 19,
    R32G32B32_FLOAT = 20,
    X32Y32Z32W32 = 21,
    X32Y32Z32W32_FLOAT = 22,
    R32G32B32A32 = 23,
    R32G32B32A32_FLOAT = 24,
    R16G16B16_FLOAT = 25,
    R16G16B16A16_FLOAT = 26
};

public enum GTSCodec : UInt32
{
    Uniform = 0,
    Color420 = 1,
    Normal = 2,
    RawColor = 3,
    Binary = 4,
    Codec15Color420 = 5,
    Codec15Normal = 6,
    RawNormal = 7,
    Half = 8,
    BC = 9,
    MultiChannel = 10,
    ASTC = 11
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GTSHeader
{
    public const UInt32 GRPGMagic = 0x47505247; // 'GRPG'
    public const UInt32 CurrentVersion = 5;

    public UInt32 Magic;
    public UInt32 Version;
    public UInt32 Unused;
    public Guid GUID;
    public UInt32 NumLayers;
    public UInt64 LayersOffset;
    public UInt32 NumLevels;
    public UInt64 LevelsOffset;
    public Int32 TileWidth;
    public Int32 TileHeight;
    public Int32 TileBorder;

    public UInt32 I2; // Some tile count?
    public UInt32 NumFlatTileInfos;
    public UInt64 FlatTileInfoOffset;
    public UInt32 I6;
    public UInt32 I7;

    public UInt32 NumPackedTileIDs;
    public UInt64 PackedTileIDsOffset;

    public UInt32 M;
    public UInt32 N;
    public UInt32 O;
    public UInt32 P;
    public UInt32 Q;
    public UInt32 R;
    public UInt32 S;

    public UInt32 PageSize;
    public UInt32 NumPageFiles;
    public UInt64 PageFileMetadataOffset;

    public UInt32 FourCCListSize;
    public UInt64 FourCCListOffset;

    public UInt32 ParameterBlockHeadersCount;
    public UInt64 ParameterBlockHeadersOffset;

    public UInt64 ThumbnailsOffset;
    public UInt32 XJJ;
    public UInt32 XKK;
    public UInt32 XLL;
    public UInt32 XMM;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GTSTileSetLayer
{
    public GTSDataType DataType;
    public Int32 B; // -1
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GTSTileSetLevel
{
    public UInt32 Width; // Width in tiles
    public UInt32 Height; // Height in tiles
    public UInt64 FlatTileIndicesOffset; // Flat tiles offset in file
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GTSParameterBlockHeader
{
    public UInt32 ParameterBlockID;
    public GTSCodec Codec;
    public UInt32 ParameterBlockSize;
    public UInt64 FileInfoOffset;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GTSBCParameterBlock
{
    public UInt16 Version;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] Compression1;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] Compression2;

    public string CompressionName1
    {
        get
        {
            int len;
            for (len = 0; len < Compression1.Length && Compression1[len] != 0; len ++) {}
            return Encoding.UTF8.GetString(Compression1, 0, len);
        }
        set
        {
            Compression1 = new byte[0x10];
            Array.Clear(Compression1, 0, 0x10);
            byte[] encoded = Encoding.UTF8.GetBytes(value);
            Array.Copy(encoded, Compression1, encoded.Length);
        }
    }

    public string CompressionName2
    {
        get
        {
            int len;
            for (len = 0; len < Compression2.Length && Compression2[len] != 0; len ++) {}
            return Encoding.UTF8.GetString(Compression2, 0, len);
        }
        set
        {
            Compression2 = new byte[0x10];
            Array.Clear(Compression2, 0, 0x10);
            byte[] encoded = Encoding.UTF8.GetBytes(value);
            Array.Copy(encoded, Compression2, encoded.Length);
        }
    }

    public UInt32 B;
    public Byte C1;
    public Byte C2;
    public Byte BCField3;
    public Byte DataType;
    public UInt16 D;
    public UInt32 FourCC;
    public Byte E1;
    public Byte SaveMip;
    public Byte E3;
    public Byte E4;
    public UInt32 F;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GTSUniformParameterBlock
{
    public UInt16 Version;
    public UInt16 A_Unused;
    public UInt32 Width;
    public UInt32 Height;
    public GTSDataType DataType;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GTSPageFileInfo
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
    public byte[] FileNameBuf;

    public UInt32 NumPages;
    public Guid Checksum;
    public UInt32 F; // 2

    public string FileName
    {
        get
        {
            int nameLen;
            for (nameLen = 0; nameLen < FileNameBuf.Length && FileNameBuf[nameLen] != 0; nameLen += 2)
            {
            }
            return Encoding.Unicode.GetString(FileNameBuf, 0, nameLen);
        }
        set
        {
            FileNameBuf = new byte[512];
            Array.Clear(FileNameBuf, 0, 512);
            byte[] encoded = Encoding.Unicode.GetBytes(value);
            Array.Copy(encoded, FileNameBuf, encoded.Length);
        }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GTSFourCCMetadata
{
    public UInt32 FourCC;
    public Byte Format;
    public Byte ExtendedLength;
    public UInt16 Length;

    public string FourCCName
    {
        get
        {
            return Char.ToString((char)(FourCC & 0xff))
                + Char.ToString((char)((FourCC >> 8) & 0xff))
                + Char.ToString((char)((FourCC >> 16) & 0xff))
                + Char.ToString((char)((FourCC >> 24) & 0xff));
        }

        set
        {
            FourCC = (uint)value[0]
                | ((uint)value[1] << 8)
                | ((uint)value[2] << 16)
                | ((uint)value[3] << 24);
        }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GTSThumbnailInfoHeader
{
    public UInt32 NumThumbnails;
    public UInt32 A;
    public UInt32 B;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GTSThumbnailInfo
{
    public Guid GUID;
    public UInt64 OffsetInFile;
    public UInt32 CompressedSize;
    public UInt32 Unknown1;
    public UInt16 Unknown2;
    public UInt16 Unknown3;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GTSPackedTileID(UInt32 layer, UInt32 level, UInt32 x, UInt32 y)
{
    public UInt32 Val = (layer & 0xF)
            | ((level & 0xF) << 4)
            | ((y & 0xFFF) << 8)
            | ((x & 0xFFF) << 20);

    public UInt32 Layer
    {
        get
        {
            return Val & 0x0F;
        }
    }

    public UInt32 Level
    {
        get
        {
            return (Val >> 4) & 0x0F;
        }
    }

    public UInt32 Y
    {
        get
        {
            return (Val >> 8) & 0x0FFF;
        }
    }

    public UInt32 X
    {
        get
        {
            return Val >> 20;
        }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GTSFlatTileInfo
{
    public UInt16 PageFileIndex; // Index of file in PageFileInfos
    public UInt16 PageIndex; // Index of 1MB page
    public UInt16 ChunkIndex; // Index of entry within page
    public UInt16 D; // Always 1?
    public UInt32 PackedTileIndex; // Index of tile in PackedTileIDs
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GTPHeader
{
    public const UInt32 HeaderMagic = 0x50415247;
    public const UInt32 DefaultVersion = 4;

    public UInt32 Magic;
    public UInt32 Version;
    public Guid GUID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GTPChunkHeader
{
    public GTSCodec Codec;
    public UInt32 ParameterBlockID;
    public UInt32 Size;
}
