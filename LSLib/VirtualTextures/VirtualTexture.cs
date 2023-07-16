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

    public class VirtualTileSet : IDisposable
    {
        public String PagePath;
        public GTSHeader Header;
        public GTSTileSetLayer[] TileSetLayers;
        public GTSTileSetLevel[] TileSetLevels;
        public List<Int32[]> PerLevelFlatTileIndices;
        public GTSParameterBlockHeader[] ParameterBlockHeaders;
        public Dictionary<UInt32, GTSBCParameterBlock> ParameterBlocks;
        public List<PageFileInfo> PageFileInfos;
        public GTSThumbnailInfo[] ThumbnailInfos;
        public GTSPackedTileID[] PackedTileIDs;
        public GTSFlatTileInfo[] FlatTileInfos;

        private Dictionary<int, PageFile> PageFiles = new Dictionary<int, PageFile>();

        public VirtualTileSet(string path)
        {
            PagePath = Path.GetDirectoryName(path);

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                LoadFromStream(fs, reader, false);
            }
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

            ParameterBlocks = new Dictionary<UInt32, GTSBCParameterBlock>();
            foreach (var hdr in ParameterBlockHeaders)
            {
                fs.Position = (uint)hdr.FileInfoOffset;
                if (hdr.CodecID == 9)
                {
                    Debug.Assert(hdr.CodecHeaderSize == 0x38);
                    var bc = BinUtils.ReadStruct<GTSBCParameterBlock>(reader);
                    ParameterBlocks.Add(hdr.ParameterBlockID, bc);
                    Debug.Assert(bc.Version == 0x238e);
                    Debug.Assert(bc.B == 0);
                    Debug.Assert(bc.C1 == 0);
                    Debug.Assert(bc.C2 == 0);
                    Debug.Assert(bc.BCField1 == 1 || bc.BCField1 == 8);
                    Debug.Assert(bc.BCField3 == 0);
                    Debug.Assert(bc.E1 == 0);
                    Debug.Assert(bc.E3 == 0);
                    Debug.Assert(bc.BCField2 == 1);
                    Debug.Assert(bc.E4 == 0);
                    Debug.Assert(bc.D == 0);
                    Debug.Assert(bc.F == 0);
                    Debug.Assert(bc.FourCC == 0x20334342);
                }
                else
                {
                    Debug.Assert(hdr.CodecID == 0);
                    Debug.Assert(hdr.CodecHeaderSize == 0x10);
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

            // TODO ... decode fourCCs ...

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

            if (!foundPages)
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
