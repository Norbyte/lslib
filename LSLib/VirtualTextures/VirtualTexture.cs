using LSLib.LS;
using System.Diagnostics;

namespace LSLib.VirtualTextures;

public struct PageFileInfo
{
    public GTSPageFileInfo Meta;
    public uint FirstPageIndex;
    public string FileName;
}

public enum FourCCElementType
{
    Node,
    Int,
    String,
    BinaryInt,
    BinaryGuid
};

public class FourCCElement
{
    public FourCCElementType Type;
    public string FourCC;
    public string Str;
    public uint UInt;
    public byte[] Blob;
    public List<FourCCElement> Children;

    public static FourCCElement Make(string fourCC)
    {
        return new FourCCElement
        {
            Type = FourCCElementType.Node,
            FourCC = fourCC,
            Children = []
        };
    }

    public static FourCCElement Make(string fourCC, uint value)
    {
        return new FourCCElement
        {
            Type = FourCCElementType.Int,
            FourCC = fourCC,
            UInt = value
        };
    }

    public static FourCCElement Make(string fourCC, string value)
    {
        return new FourCCElement
        {
            Type = FourCCElementType.String,
            FourCC = fourCC,
            Str = value
        };
    }

    public static FourCCElement Make(string fourCC, FourCCElementType type, byte[] value)
    {
        return new FourCCElement
        {
            Type = type,
            FourCC = fourCC,
            Blob = value
        };
    }

    public FourCCElement GetChild(string fourCC)
    {
        foreach (var child in Children)
        {
            if (child.FourCC == fourCC)
            {
                return child;
            }
        }

        return null;
    }
}

public class FourCCTextureMeta
{
    public string Name;
    public int X;
    public int Y;
    public int Width;
    public int Height;
}

public class TileSetFourCC
{
    public FourCCElement Root;

    public void Read(Stream fs, BinaryReader reader, long length)
    {
        var fourCCs = new List<FourCCElement>();
        Read(fs, reader, length, fourCCs);
        Root = fourCCs[0];
    }

    public void Read(Stream fs, BinaryReader reader, long length, List<FourCCElement> elements)
    {
        var end = fs.Position + length;
        while (fs.Position < end)
        {
            var cc = new FourCCElement();
            var header = BinUtils.ReadStruct<GTSFourCCMetadata>(reader);
            cc.FourCC = header.FourCCName;

            Int32 valueSize = header.Length;
            if (header.ExtendedLength == 1)
            {
                valueSize |= ((int)reader.ReadUInt32() << 16);
            }

            switch (header.Format)
            {
                case 1:
                    {
                        cc.Type = FourCCElementType.Node;
                        cc.Children = [];
                        Read(fs, reader, valueSize, cc.Children);
                        break;
                    }

                case 2:
                    {
                        cc.Type = FourCCElementType.String;

                        var str = reader.ReadBytes(valueSize - 2);
                        cc.Str = Encoding.Unicode.GetString(str);
                        var nullterm = reader.ReadUInt16(); // null terminator
                        Debug.Assert(nullterm == 0);
                        break;
                    }

                case 3:
                    {
                        cc.Type = FourCCElementType.Int;
                        Debug.Assert(valueSize == 4);
                        cc.UInt = reader.ReadUInt32();
                        break;
                    }

                case 8:
                    {
                        cc.Type = FourCCElementType.BinaryInt;
                        cc.Blob = reader.ReadBytes(valueSize);
                        break;
                    }

                case 0x0D:
                    {
                        cc.Type = FourCCElementType.BinaryGuid;
                        cc.Blob = reader.ReadBytes(valueSize);
                        break;
                    }

                default:
                    throw new Exception($"Unrecognized FourCC type tag: {header.Format}");
            }

            if ((fs.Position % 4) != 0)
            {
                fs.Position += 4 - (fs.Position % 4);
            }

            elements.Add(cc);
        }

        Debug.Assert(fs.Position == end);
    }


    public List<FourCCTextureMeta> ExtractTextureMetadata()
    {
        var metaList = new List<FourCCTextureMeta>();
        var textures = Root.GetChild("ATLS").GetChild("TXTS").Children;
        foreach (var tex in textures)
        {
            var meta = new FourCCTextureMeta
            {
                Name = tex.GetChild("NAME").Str,
                Width = (int)tex.GetChild("WDTH").UInt,
                Height = (int)tex.GetChild("HGHT").UInt,
                X = (int)tex.GetChild("XXXX").UInt,
                Y = (int)tex.GetChild("YYYY").UInt
            };
            metaList.Add(meta);
        }

        return metaList;
    }

    public void Write(Stream fs, BinaryWriter writer)
    {
        Write(fs, writer, Root);
    }

    public void Write(Stream fs, BinaryWriter writer, FourCCElement element)
    {
        var header = new GTSFourCCMetadata
        {
            FourCCName = element.FourCC
        };

        var length = element.Type switch
        {
            FourCCElementType.Node => (uint)0x10000000,
            FourCCElementType.Int => (uint)4,
            FourCCElementType.String => (UInt32)Encoding.Unicode.GetBytes(element.Str).Length + 2,
            FourCCElementType.BinaryInt or FourCCElementType.BinaryGuid => (UInt32)element.Blob.Length,
            _ => throw new InvalidDataException($"Unsupported FourCC value type: {element.Type}"),
        };

        header.Format = element.Type switch
        {
            FourCCElementType.Node => 1,
            FourCCElementType.Int => 3,
            FourCCElementType.String => 2,
            FourCCElementType.BinaryInt => 8,
            FourCCElementType.BinaryGuid => 0xD,
            _ => throw new InvalidDataException($"Unsupported FourCC value type: {element.Type}"),
        };

        header.Length = (UInt16)(length & 0xffff);
        if (length > 0xffff)
        {
            header.ExtendedLength = 1;
        }

        BinUtils.WriteStruct<GTSFourCCMetadata>(writer, ref header);

        if (length > 0xffff)
        {
            UInt32 extraLength = length >> 16;
            writer.Write(extraLength);
        }

        switch (element.Type)
        {
            case FourCCElementType.Node:
                {
                    var lengthOffset = fs.Position - 6;
                    var childrenOffset = fs.Position;
                    foreach (var child in element.Children)
                    {
                        Write(fs, writer, child);
                    }
                    var endOffset = fs.Position;
                    var childrenSize = (UInt32)(endOffset - childrenOffset);

                    // Re-write node header with final node size
                    fs.Position = lengthOffset;
                    writer.Write((UInt32)childrenSize);
                    fs.Position = endOffset;

                    break;
                }

            case FourCCElementType.Int:
                writer.Write(element.UInt);
                break;

            case FourCCElementType.String:
                writer.Write(Encoding.Unicode.GetBytes(element.Str));
                writer.Write((UInt16)0); // null terminator
                break;

            case FourCCElementType.BinaryInt:
            case FourCCElementType.BinaryGuid:
                writer.Write(element.Blob);
                break;

            default:
                throw new InvalidDataException($"Unsupported FourCC value type: {element.Type}");
        }

        while ((fs.Position % 4) != 0)
        {
            writer.Write((Byte)0);
        }
    }
}

public class VirtualTileSet : IDisposable
{
    public String PagePath;
    public GTSHeader Header;
    public GTSTileSetLayer[] TileSetLayers;
    public GTSTileSetLevel[] TileSetLevels;
    public List<UInt32[]> PerLevelFlatTileIndices;
    public GTSParameterBlockHeader[] ParameterBlockHeaders;
    public Dictionary<UInt32, object> ParameterBlocks;
    public List<PageFileInfo> PageFileInfos;
    public TileSetFourCC FourCCMetadata;
    public GTSThumbnailInfo[] ThumbnailInfos;
    public GTSPackedTileID[] PackedTileIDs;
    public GTSFlatTileInfo[] FlatTileInfos;

    private readonly Dictionary<int, PageFile> PageFiles = [];
    private readonly TileCompressor Compressor;

    public VirtualTileSet(string path, string pagePath)
    {
        PagePath = pagePath;
        Compressor = new TileCompressor();

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fs);
        LoadFromStream(fs, reader, false);
    }

    public VirtualTileSet(string path) : this(path, Path.GetDirectoryName(path))
    {
    }

    public VirtualTileSet()
    {
    }

    public void Save(string path)
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(fs);
        SaveToStream(fs, writer);
    }

    public void Dispose()
    {
        foreach (var pageFile in PageFiles)
        {
            pageFile.Value.Dispose();
        }
    }

    private void LoadThumbnails(Stream fs, BinaryReader reader)
    {
        fs.Position = (long)Header.ThumbnailsOffset;
        var thumbHdr = BinUtils.ReadStruct<GTSThumbnailInfoHeader>(reader);
        ThumbnailInfos = new GTSThumbnailInfo[thumbHdr.NumThumbnails];
        BinUtils.ReadStructs<GTSThumbnailInfo>(reader, ThumbnailInfos);

        foreach (var thumb in ThumbnailInfos)
        {
            // Decompress thumbnail blob
            fs.Position = (uint)thumb.OffsetInFile;
            var inb = new byte[thumb.CompressedSize];
            reader.Read(inb, 0, inb.Length);
            var thumbnailBlob = Native.FastLZCompressor.Decompress(inb, Math.Max(thumb.Unknown2, thumb.Unknown3) * 0x100);

            var numSections = reader.ReadUInt32();
            var parameterBlockSize = reader.ReadUInt32();
            reader.ReadUInt32();
            var e4 = BinUtils.ReadStruct<GTSBCParameterBlock>(reader);
            int sectionNo = 0;
            numSections -= 2;

            while (numSections-- > 0)
            {
                var mipLevelSize = reader.ReadUInt32();
                if (mipLevelSize > 0x10000)
                {
                    fs.Position -= 4;
                    break;
                }

                var inf = new byte[mipLevelSize];
                reader.Read(inf, 0, inf.Length);

                sectionNo++;
            }
        }
    }

    public void LoadFromStream(Stream fs, BinaryReader reader, bool loadThumbnails)
    {
        Header = BinUtils.ReadStruct<GTSHeader>(reader);

        fs.Position = (uint)Header.LayersOffset;
        TileSetLayers = new GTSTileSetLayer[Header.NumLayers];
        BinUtils.ReadStructs<GTSTileSetLayer>(reader, TileSetLayers);

        fs.Position = (uint)Header.LevelsOffset;
        TileSetLevels = new GTSTileSetLevel[Header.NumLevels];
        BinUtils.ReadStructs<GTSTileSetLevel>(reader, TileSetLevels);

        PerLevelFlatTileIndices = [];
        foreach (var level in TileSetLevels)
        {
            fs.Position = (uint)level.FlatTileIndicesOffset;
            var tileIndices = new UInt32[level.Height * level.Width * Header.NumLayers];
            BinUtils.ReadStructs<UInt32>(reader, tileIndices);
            PerLevelFlatTileIndices.Add(tileIndices);
        }

        fs.Position = (uint)Header.ParameterBlockHeadersOffset;
        ParameterBlockHeaders = new GTSParameterBlockHeader[Header.ParameterBlockHeadersCount];
        BinUtils.ReadStructs<GTSParameterBlockHeader>(reader, ParameterBlockHeaders);

        ParameterBlocks = [];
        foreach (var hdr in ParameterBlockHeaders)
        {
            fs.Position = (uint)hdr.FileInfoOffset;
            if (hdr.Codec == GTSCodec.BC)
            {
                Debug.Assert(hdr.ParameterBlockSize == 0x38);
                var bc = BinUtils.ReadStruct<GTSBCParameterBlock>(reader);
                ParameterBlocks.Add(hdr.ParameterBlockID, bc);
                Debug.Assert(bc.Version == 0x238e);
                Debug.Assert(bc.B == 0);
                Debug.Assert(bc.C1 == 0);
                Debug.Assert(bc.C2 == 0);
                Debug.Assert(bc.BCField3 == 0);
                Debug.Assert(bc.DataType == (Byte)GTSDataType.R8G8B8A8_SRGB || bc.DataType == (Byte)GTSDataType.X8Y8Z8W8);
                Debug.Assert(bc.D == 0);
                Debug.Assert(bc.FourCC == 0x20334342);
                Debug.Assert(bc.E1 == 0);
                Debug.Assert(bc.SaveMip == 1);
                Debug.Assert(bc.E3 == 0);
                Debug.Assert(bc.E4 == 0);
                Debug.Assert(bc.F == 0);
            }
            else
            {
                Debug.Assert(hdr.Codec == GTSCodec.Uniform);
                Debug.Assert(hdr.ParameterBlockSize == 0x10);

                var blk = BinUtils.ReadStruct<GTSUniformParameterBlock>(reader);
                Debug.Assert(blk.Version == 0x42);
                Debug.Assert(blk.A_Unused == 0);
                Debug.Assert(blk.Width == 4);
                Debug.Assert(blk.Height == 1);
                Debug.Assert(blk.DataType == GTSDataType.R8G8B8A8_SRGB || blk.DataType == GTSDataType.X8Y8Z8W8);
                ParameterBlocks.Add(hdr.ParameterBlockID, blk);
            }
        }

        fs.Position = (long)Header.PageFileMetadataOffset;
        var pageFileInfos = new GTSPageFileInfo[Header.NumPageFiles];
        BinUtils.ReadStructs<GTSPageFileInfo>(reader, pageFileInfos);

        PageFileInfos = [];
        uint nextPageIndex = 0;
        foreach (var info in pageFileInfos)
        {
            PageFileInfos.Add(new PageFileInfo
            {
                Meta = info,
                FirstPageIndex = nextPageIndex,
                FileName = info.FileName
            });
            nextPageIndex += info.NumPages;
        }

        fs.Position = (long)Header.FourCCListOffset;
        FourCCMetadata = new TileSetFourCC();
        FourCCMetadata.Read(fs, reader, Header.FourCCListSize);

        if (loadThumbnails)
        {
            LoadThumbnails(fs, reader);
        }

        fs.Position = (long)Header.PackedTileIDsOffset;
        PackedTileIDs = new GTSPackedTileID[Header.NumPackedTileIDs];
        BinUtils.ReadStructs<GTSPackedTileID>(reader, PackedTileIDs);

        fs.Position = (long)Header.FlatTileInfoOffset;
        FlatTileInfos = new GTSFlatTileInfo[Header.NumFlatTileInfos];
        BinUtils.ReadStructs<GTSFlatTileInfo>(reader, FlatTileInfos);
    }

    public void SaveToStream(Stream fs, BinaryWriter writer)
    {
        BinUtils.WriteStruct<GTSHeader>(writer, ref Header);

        Header.LayersOffset = (ulong)fs.Position;
        Header.NumLayers = (uint)TileSetLayers.Length;
        BinUtils.WriteStructs<GTSTileSetLayer>(writer, TileSetLayers);

        for (var i = 0; i < TileSetLevels.Length; i++)
        {
            ref var level = ref TileSetLevels[i];
            level.FlatTileIndicesOffset = (ulong)fs.Position;

            var tileIndices = PerLevelFlatTileIndices[i];
            Debug.Assert(tileIndices.Length == level.Height * level.Width * Header.NumLayers);

            BinUtils.WriteStructs<UInt32>(writer, tileIndices);
        }

        Header.LevelsOffset = (ulong)fs.Position;
        Header.NumLevels = (uint)TileSetLevels.Length;
        BinUtils.WriteStructs<GTSTileSetLevel>(writer, TileSetLevels);

        Header.ParameterBlockHeadersOffset = (ulong)fs.Position;
        Header.ParameterBlockHeadersCount = (uint)ParameterBlockHeaders.Length;
        BinUtils.WriteStructs<GTSParameterBlockHeader>(writer, ParameterBlockHeaders);

        for (var i = 0; i < ParameterBlockHeaders.Length; i++)
        {
            ref var hdr = ref ParameterBlockHeaders[i];
            hdr.FileInfoOffset = (ulong)fs.Position;

            if (hdr.Codec == GTSCodec.BC)
            {
                var block = (GTSBCParameterBlock)ParameterBlocks[hdr.ParameterBlockID];
                BinUtils.WriteStruct<GTSBCParameterBlock>(writer, ref block);
            }
            else
            {
                Debug.Assert(hdr.Codec == GTSCodec.Uniform);
                hdr.ParameterBlockSize = 0x10;

                var block = (GTSUniformParameterBlock)ParameterBlocks[hdr.ParameterBlockID];
                BinUtils.WriteStruct<GTSUniformParameterBlock>(writer, ref block);
            }
        }

        Header.PageFileMetadataOffset = (ulong)fs.Position;
        Header.NumPageFiles = (uint)PageFileInfos.Count;

        for (var i = 0; i < PageFileInfos.Count; i++)
        {
            var pageFile = PageFileInfos[i];
            BinUtils.WriteStruct<GTSPageFileInfo>(writer, ref pageFile.Meta);
        }

        Header.FourCCListOffset = (ulong)fs.Position;
        FourCCMetadata.Write(fs, writer);
        Header.FourCCListSize = (uint)((ulong)fs.Position - Header.FourCCListOffset);

        Header.ThumbnailsOffset = (ulong)fs.Position;
        var thumbHdr = new GTSThumbnailInfoHeader
        {
            NumThumbnails = 0
        };
        BinUtils.WriteStruct<GTSThumbnailInfoHeader>(writer, ref thumbHdr);

        Header.PackedTileIDsOffset = (ulong)fs.Position;
        Header.NumPackedTileIDs = (uint)PackedTileIDs.Length;
        BinUtils.WriteStructs<GTSPackedTileID>(writer, PackedTileIDs);

        Header.FlatTileInfoOffset = (ulong)fs.Position;
        Header.NumFlatTileInfos = (uint)FlatTileInfos.Length;
        BinUtils.WriteStructs<GTSFlatTileInfo>(writer, FlatTileInfos);

        // Re-write structures that contain offset information
        fs.Position = 0;
        BinUtils.WriteStruct<GTSHeader>(writer, ref Header);

        fs.Position = (long)Header.ParameterBlockHeadersOffset;
        BinUtils.WriteStructs<GTSParameterBlockHeader>(writer, ParameterBlockHeaders);
    }

    public bool GetTileInfo(int level, int layer, int x, int y, ref GTSFlatTileInfo tile)
    {
        var tileIndices = PerLevelFlatTileIndices[level];
        var tileIndex = tileIndices[layer + Header.NumLayers * (x + y * TileSetLevels[level].Width)];
        if ((tileIndex & 0x80000000) == 0)
        {
            tile = FlatTileInfos[tileIndex];
            return true;
        }
        else
        {
            return false;
        }
    }

    public PageFile GetOrLoadPageFile(int pageFileIdx)
    {
        if (!PageFiles.TryGetValue(pageFileIdx, out PageFile file))
        {
            var meta = PageFileInfos[pageFileIdx];
            file = new PageFile(this, Path.Join(PagePath, meta.FileName));
            PageFiles.Add(pageFileIdx, file);
        }

        return file;
    }

    public void StitchTexture(int level, int layer, int minX, int minY, int maxX, int maxY, BC5Image output)
    {
        var tileWidth = Header.TileWidth - Header.TileBorder * 2;
        var tileHeight = Header.TileHeight - Header.TileBorder * 2;
        GTSFlatTileInfo tileInfo = new();
        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                if (GetTileInfo(level, layer, x, y, ref tileInfo))
                {
                    var pageFile = GetOrLoadPageFile(tileInfo.PageFileIndex);
                    var tile = pageFile.UnpackTileBC5(tileInfo.PageIndex, tileInfo.ChunkIndex, Compressor);
                    tile.CopyTo(output, 8, 8, (x - minX) * tileWidth, (y - minY) * tileHeight, tileWidth, tileHeight);
                }
            }
        }
    }

    public BC5Image ExtractTexture(int level, int layer, int minX, int minY, int maxX, int maxY)
    {
        var width = (maxX - minX + 1) * (Header.TileWidth - Header.TileBorder * 2);
        var height = (maxY - minY + 1) * (Header.TileHeight - Header.TileBorder * 2);
        var stitched = new BC5Image(width, height);
        StitchTexture(level, layer, minX, minY, maxX, maxY, stitched);
        return stitched;
    }

    public int FindPageFile(string name)
    {
        for (var i = 0; i < PageFileInfos.Count; i++)
        {
            if (PageFileInfos[i].FileName.Contains(name))
            {
                return i;
            }
        }

        return -1;
    }

    public void ReleasePageFiles()
    {
        this.PageFiles.Clear();
    }

    public BC5Image ExtractTexture(int level, int layer, FourCCTextureMeta tex)
    {
        var tlW = Header.TileWidth - Header.TileBorder * 2;
        var tlH = Header.TileHeight - Header.TileBorder * 2;
        var tX = tex.X / tlW;
        var tY = tex.Y / tlH;
        var tW = tex.Width / tlW;
        var tH = tex.Height / tlH;
        var lv = (1 << level);

        var minX = (tX / lv) + ((tX % lv) > 0 ? 1 : 0);
        var minY = (tY / lv) + ((tY % lv) > 0 ? 1 : 0);
        var maxX = ((tX+tW) / lv) + (((tX + tW) % lv) > 0 ? 1 : 0) - 1;
        var maxY = ((tY+tH) / lv) + (((tY + tH) % lv) > 0 ? 1 : 0) - 1;

         return ExtractTextureIfExists(level, layer, minX, minY, maxX, maxY);
    }

    public BC5Image ExtractTextureIfExists(int levelIndex, int layer, int minX, int minY, int maxX, int maxY)
    {
        GTSFlatTileInfo tile = new();
        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                if (!GetTileInfo(levelIndex, layer, x, y, ref tile))
                {
                    return null;
                }
            }
        }

        return ExtractTexture(levelIndex, layer, minX, minY, maxX, maxY);
    }
}
