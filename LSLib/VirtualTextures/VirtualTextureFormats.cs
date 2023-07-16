using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LSLib.VirtualTextures
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DDSHeader
    {
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
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GTSHeader
    {
        public UInt32 _Magic;
        public UInt32 _Version;
        public UInt32 _Unused;
        public Guid _GUID;
        public UInt32 NumLayers;
        public UInt64 LayersOffset;
        public UInt32 NumLevels;
        public UInt64 LevelsOffset;
        public Int32 TileWidth;
        public Int32 TileHeight;
        public Int32 TileBorder;

        public UInt32 I2;
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

        public UInt32 T; // 0x10000
        public UInt32 NumPageFiles;
        public UInt64 PageFileMetadataOffset;

        public UInt32 FourCCListSize;
        public UInt64 FourCCListOFfset;

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
        public UInt32 A;
        public UInt32 B;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GTSTileSetLevel
    {
        public UInt32 Width;
        public UInt32 Height;
        public UInt64 FlatTileIndicesOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GTSParameterBlockHeader
    {
        public UInt32 ParameterBlockID;
        public UInt32 CodecID;
        public UInt32 CodecHeaderSize;
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
        }

        public string CompressionName2
        {
            get
            {
                int len;
                for (len = 0; len < Compression2.Length && Compression2[len] != 0; len ++) {}
                return Encoding.UTF8.GetString(Compression2, 0, len);
            }
        }

        public UInt32 B;
        public Byte C1;
        public Byte C2;
        public Byte BCField3;
        public Byte BCField1;
        public UInt16 D;
        public UInt32 FourCC;
        public Byte E1;
        public Byte BCField2;
        public Byte E3;
        public Byte E4;
        public UInt32 F; 
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GTSPageFileInfo
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] NameBuf;

        public UInt32 NumPages;
        public Guid Checksum;
        public UInt32 F;

        public string Name
        {
            get
            {
                int nameLen;
                for (nameLen = 0; nameLen < NameBuf.Length && NameBuf[nameLen] != 0; nameLen += 2)
                {
                }
                return Encoding.Unicode.GetString(NameBuf, 0, nameLen);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GTSFourCCInfo
    {
        public UInt32 FourCC;
        public Byte Format;
        public Byte Unknown;
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
    public struct GTSPackedTileID
    {
        public UInt32 Val;

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

        public UInt32 C
        {
            get
            {
                return (Val >> 8) & 0x0FFF;
            }
        }

        public UInt32 D
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
        public UInt16 PageFileIndex;
        public UInt16 PageIndex;
        public UInt16 ChunkIndex;
        public UInt16 D;
        public UInt32 PackedTileIndex;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GTPHeader
    {
        public UInt32 _Magic;
        public UInt32 _Version;
        public Guid _GUID;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GTPChunkHeader
    {
        // Codec IDs:
        // 0 Uniform
        // 1 Color420
        // 2 Normal
        // 3 RawColor
        // 4 Binary
        // 5 Codec15 Color420
        // 6 Codec15 Normal
        // 7 RawNormal
        // 8 Half
        // 9 BC
        // 10 MultiChannel
        // 11 ASTC
        public UInt32 Codec;
        public UInt32 ParameterBlockID;
        public UInt32 Size;
    }
}
