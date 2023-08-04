using LSLib.LS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.VirtualTextures
{
    public struct PageFileInfo
    {
        public GTSPageFileInfo Meta;
        public uint FirstPageIndex;
        public string Name;
    }

    public enum FourCCElementType
    {
        Node,
        Int,
        String,
        Binary
    };

    public struct FourCCElement
    {
        public FourCCElementType Type;
        public string FourCC;
        public string Str;
        public uint UInt;
        public byte[] Blob;
        public List<FourCCElement> Children;
    }

    public class VirtualTileSet : IDisposable
    {
        public String PagePath;
        public GTSHeader Header;
        public GTSTileSetLayer[] TileSetLayers;
        public GTSTileSetLevel[] TileSetLevels;
        public List<Int32[]> PerLevelFlatTileIndices;
        public GTSParameterBlockHeader[] ParameterBlockHeaders;
        public Dictionary<UInt32, object> ParameterBlocks;
        public List<PageFileInfo> PageFileInfos;
        public FourCCElement FourCCMetadata;
        public GTSThumbnailInfo[] ThumbnailInfos;
        public GTSPackedTileID[] PackedTileIDs;
        public GTSFlatTileInfo[] FlatTileInfos;

        private Dictionary<int, PageFile> PageFiles = new Dictionary<int, PageFile>();

        public VirtualTileSet(string path, string pagePath)
        {
            PagePath = pagePath;

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                LoadFromStream(fs, reader, false);
            }
        }

        public VirtualTileSet(string path) : this(path, Path.GetDirectoryName(path))
        {
        }

        public void Save(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(fs))
            {
                SaveToStream(fs, writer);
            }
        }

        public void Dispose()
        {
            foreach (var pageFile in PageFiles)
            {
                pageFile.Value.Dispose();
            }
        }

        private void ParseFourCCMeta(Stream fs, BinaryReader reader, long length, List<FourCCElement> elements)
        {
            var end = fs.Position + length;
            while (fs.Position < end)
            {
                var cc = new FourCCElement();
                var header = BinUtils.ReadStruct<GTSFourCCMetadata>(reader);
                cc.FourCC = header.FourCCName;

                switch (header.Format)
                {
                    case 1:
                        {
                            cc.Type = FourCCElementType.Node;
                            cc.Children = new List<FourCCElement>();
                            var elementBytes = reader.ReadUInt32();
                            reader.ReadUInt16();
                            ParseFourCCMeta(fs, reader, elementBytes, cc.Children);
                            break;
                        }

                    case 2:
                        {
                            Int32 len;
                            cc.Type = FourCCElementType.String;
                            if (header.Subformat == 1)
                            {
                                len = (int)reader.ReadUInt32();
                                var unk = reader.ReadUInt16();
                                Debug.Assert(unk == 0);
                            }
                            else
                            {
                                len = reader.ReadUInt16();
                            }

                            var str = reader.ReadBytes(len - 2);
                            cc.Str = Encoding.Unicode.GetString(str);
                            var nullterm = reader.ReadUInt16(); // null terminator
                            Debug.Assert(nullterm == 0);
                            break;
                        }

                    case 3:
                        {
                            cc.Type = FourCCElementType.Int;
                            var len = reader.ReadUInt16();
                            Debug.Assert(len == 4);
                            cc.UInt = reader.ReadUInt32();
                            break;
                        }

                    case 8:
                    case 0x0D:
                        {
                            cc.Type = FourCCElementType.Binary;
                            var len = reader.ReadUInt16();
                            cc.Blob = reader.ReadBytes(len);
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
        }

        private void WriteFourCCMeta(Stream fs, BinaryWriter writer, FourCCElement element)
        {
            var header = new GTSFourCCMetadata();
            header.FourCCName = element.FourCC;

            switch (element.Type)
            {
                case FourCCElementType.Node:
                    header.Format = 1;
                    break;

                case FourCCElementType.Int:
                    header.Format = 3;
                    break;


                case FourCCElementType.String:
                    header.Format = 2;
                    header.Subformat = (byte)((element.Str.Length >= 0x7fff) ? 1 : 0);
                    break;


                case FourCCElementType.Binary:
                    header.Format = 0xD;
                    break;

                default:
                    throw new InvalidDataException($"Unsupported FourCC value type: {element.Type}");
            }

            BinUtils.WriteStruct<GTSFourCCMetadata>(writer, ref header);

            switch (element.Type)
            {
                case FourCCElementType.Node:
                    {
                        var headerOffset = fs.Position;
                        writer.Write((UInt32)0);
                        writer.Write((UInt16)0); // Padding

                        var childrenOffset = fs.Position;
                        foreach (var child in element.Children)
                        {
                            WriteFourCCMeta(fs, writer, child);
                        }
                        var endOffset = fs.Position;
                        var childrenSize = (UInt32)(endOffset - childrenOffset);

                        // Re-write node header with final node size
                        fs.Position = headerOffset;
                        writer.Write((UInt32)childrenSize);
                        fs.Position = endOffset;

                        break;
                    }

                case FourCCElementType.Int:
                    writer.Write((UInt16)4);
                    writer.Write(element.UInt);
                    break;

                case FourCCElementType.String:
                    if (header.Subformat == 1)
                    {
                        writer.Write((UInt32)(element.Str.Length * 2 + 2));
                        writer.Write((UInt16)0); // Padding
                    }
                    else
                    {
                        writer.Write((UInt16)(element.Str.Length * 2 + 2));
                    }

                    writer.Write(Encoding.Unicode.GetBytes(element.Str));
                    writer.Write((UInt16)0); // null terminator
                    break;

                // TODO - 0x08 vs 0x0D type ID?
                case FourCCElementType.Binary:
                    writer.Write((UInt16)element.Blob.Length);
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
                var outb = new byte[Math.Max(thumb.Unknown2, thumb.Unknown3) * 0x100];
                reader.Read(inb, 0, inb.Length);
                var thumbnailBlob = FastLZ.fastlz1_decompress(inb, inb.Length, outb);

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

            PerLevelFlatTileIndices = new List<Int32[]>();
            foreach (var level in TileSetLevels)
            {
                fs.Position = (uint)level.FlatTileIndicesOffset;
                var tileIndices = new Int32[level.Height * level.Width * Header.NumLayers];
                BinUtils.ReadStructs<Int32>(reader, tileIndices);
                PerLevelFlatTileIndices.Add(tileIndices);
            }

            fs.Position = (uint)Header.ParameterBlockHeadersOffset;
            ParameterBlockHeaders = new GTSParameterBlockHeader[Header.ParameterBlockHeadersCount];
            BinUtils.ReadStructs<GTSParameterBlockHeader>(reader, ParameterBlockHeaders);

            ParameterBlocks = new Dictionary<UInt32, object>();
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
                    Debug.Assert(bc.DataType == (Byte)GTSDataType.R8G8B8A8_SRGB || bc.DataType == (Byte)GTSDataType.X8Y8Z8W8);
                    Debug.Assert(bc.BCField3 == 0);
                    Debug.Assert(bc.E1 == 0);
                    Debug.Assert(bc.E3 == 0);
                    Debug.Assert(bc.SaveMip == 1);
                    Debug.Assert(bc.E4 == 0);
                    Debug.Assert(bc.D == 0);
                    Debug.Assert(bc.F == 0);
                    Debug.Assert(bc.FourCC == 0x20334342);
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

            PageFileInfos = new List<PageFileInfo>();
            uint nextPageIndex = 0;
            foreach (var info in pageFileInfos)
            {
                PageFileInfos.Add(new PageFileInfo
                {
                    Meta = info,
                    FirstPageIndex = nextPageIndex,
                    Name = info.Name
                });
                nextPageIndex += info.NumPages;
            }

            fs.Position = (long)Header.FourCCListOffset;
            var fourCCs = new List<FourCCElement>();
            ParseFourCCMeta(fs, reader, Header.FourCCListSize, fourCCs);
            FourCCMetadata = fourCCs[0];

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

                BinUtils.WriteStructs<Int32>(writer, tileIndices);
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
                    hdr.ParameterBlockSize = 0x38;
                    block.Version = 0x238e;
                    var comp1 = Encoding.UTF8.GetBytes("lz77");
                    Array.Copy(comp1, block.Compression1, comp1.Length);
                    var comp2 = Encoding.UTF8.GetBytes("fastlz0.1.0");
                    Array.Copy(comp2, block.Compression2, comp2.Length);
                    block.DataType = (Byte)GTSDataType.R8G8B8A8_SRGB; // X8Y8Z8W8 for normal/phys
                    block.SaveMip = 1;
                    block.FourCC = 0x20334342;
                    BinUtils.WriteStruct<GTSBCParameterBlock>(writer, ref block);
                }
                else
                {
                    Debug.Assert(hdr.Codec == GTSCodec.Uniform);
                    hdr.ParameterBlockSize = 0x10;

                    var block = (GTSUniformParameterBlock)ParameterBlocks[hdr.ParameterBlockID];
                    block.Version = 0x42;
                    block.A_Unused = 0;
                    block.Width = 4;
                    block.Height = 1;
                    block.DataType = GTSDataType.R8G8B8A8_SRGB; // X8Y8Z8W8 for normal/phys
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
            WriteFourCCMeta(fs, writer, FourCCMetadata);
            Header.FourCCListSize = (uint)((ulong)fs.Position - Header.FourCCListOffset);

            Header.ThumbnailsOffset = (ulong)fs.Position;
            var thumbHdr = new GTSThumbnailInfoHeader();
            thumbHdr.NumThumbnails = 0;
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
            if (tileIndex >= 0)
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
            PageFile file;
            if (!PageFiles.TryGetValue(pageFileIdx, out file))
            {
                var meta = PageFileInfos[pageFileIdx];
                file = new PageFile(this, PagePath + Path.DirectorySeparatorChar + meta.Name);
                PageFiles.Add(pageFileIdx, file);
            }

            return file;
        }

        public void StitchTexture(int level, int layer, int minX, int minY, int maxX, int maxY, BC5Image output)
        {
            var tileWidth = Header.TileWidth - Header.TileBorder * 2;
            var tileHeight = Header.TileHeight - Header.TileBorder * 2;
            GTSFlatTileInfo tileInfo = new GTSFlatTileInfo();
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    if (GetTileInfo(level, layer, x, y, ref tileInfo))
                    {
                        var pageFile = GetOrLoadPageFile(tileInfo.PageFileIndex);
                        var tile = pageFile.UnpackTileBC5(tileInfo.PageIndex, tileInfo.ChunkIndex);
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
                if (PageFileInfos[i].Name.Contains(name))
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

        public BC5Image ExtractPageFileTexture(int pageFileIndex, int levelIndex, int layer)
        {
            int minX = 0, maxX = 0, minY = 0, maxY = 0;
            bool foundPages = false;

            GTSFlatTileInfo tile = new GTSFlatTileInfo();
            var level = TileSetLevels[levelIndex];
            for (var x = 0; x < level.Width; x++)
            {
                for (var y = 0; y < level.Height; y++)
                {
                    if (GetTileInfo(levelIndex, layer, x, y, ref tile))
                    {
                        if (tile.PageFileIndex == pageFileIndex)
                        {
                            if (!foundPages)
                            {
                                minX = x;
                                maxX = x;
                                minY = y;
                                maxY = y;
                                foundPages = true;
                            }
                            else
                            {
                                minX = Math.Min(minX, x);
                                maxX = Math.Max(maxX, x);
                                minY = Math.Min(minY, y);
                                maxY = Math.Max(maxY, y);
                            }
                        }
                    }
                }
            }

            // Temporary workaround for page files that contain split textures
            if (!foundPages || (maxX - minX) > 16 || (maxY - minY) > 16)
            {
                return null;
            }
            else
            {
                return ExtractTexture(levelIndex, layer, minX, minY, maxX, maxY);
            }
        }
    }
}
