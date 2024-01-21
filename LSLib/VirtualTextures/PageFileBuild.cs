using LSLib.LS;
using System.Security.Cryptography;

namespace LSLib.VirtualTextures;


public class BuiltChunk
{
    public GTSCodec Codec;
    public UInt32 ParameterBlockID;
    public byte[] EncodedBlob;
    public int ChunkIndex;
    public UInt32 OffsetInPage;
}

public class PageBuilder
{
    public PageFileBuilder PageFile;
    public List<BuiltChunk> Chunks;
    public int PageFileIndex;
    public int PageIndex;
    public int Budget = 0;

    public PageBuilder()
    {
        Chunks = [];
    }

    public bool TryAdd(BuildTile tile)
    {
        if (tile.AddedToPageFile)
        {
            throw new InvalidOperationException("Tried to add tile to page file multiple times");
        }

        var chunkSize = 4 + Marshal.SizeOf(typeof(GTPChunkHeader)) + tile.Compressed.Data.Length;
        if (Budget + chunkSize > PageFile.Config.PageSize)
        {
            return false;
        }

        var chunk = new BuiltChunk
        {
            Codec = GTSCodec.BC,
            ParameterBlockID = tile.Compressed.ParameterBlockID,
            EncodedBlob = tile.Compressed.Data,
            ChunkIndex = Chunks.Count
        };

        tile.AddedToPageFile = true;
        tile.PageFileIndex = PageFileIndex;
        tile.PageIndex = PageIndex;
        tile.ChunkIndex = chunk.ChunkIndex;
        Chunks.Add(chunk);
        Budget += chunkSize;
        return true;
    }
}

public class PageFileBuilder(TileSetConfiguration config)
{
    public readonly TileSetConfiguration Config = config;
    public List<PageBuilder> Pages = [];
    public string Name;
    public string FileName;
    public Guid Checksum;
    public int PageFileIndex;

    public List<BuildTile> PendingTiles = [];
    public List<Tuple<BuildTile, BuildTile>> Duplicates = [];

    public void AddTile(BuildTile tile)
    {
        PendingTiles.Add(tile);
    }

    public void DeduplicateTiles()
    {
        var digests = new Dictionary<Guid, BuildTile>();

        foreach (var tile in PendingTiles)
        {
            var digest = new Guid(MD5.HashData(tile.Image.Data));
            if (!digests.TryAdd(digest, tile))
            {
                tile.DuplicateOf = digests[digest];
                Duplicates.Add(Tuple.Create(tile, digests[digest]));
            }
        }

        PendingTiles = [.. digests.Values];
    }

    public void CommitTiles()
    {
        foreach (var tile in PendingTiles)
        {
            CommitTile(tile);
        }

        PendingTiles.Clear();

        foreach (var dup in Duplicates)
        {
            dup.Item1.AddedToPageFile = true;
            dup.Item1.PageFileIndex = dup.Item2.PageFileIndex;
            dup.Item1.PageIndex = dup.Item2.PageIndex;
            dup.Item1.ChunkIndex = dup.Item2.ChunkIndex;
            dup.Item1.DuplicateOf = dup.Item2;
        }

        Duplicates.Clear();
    }

    private void CommitTile(BuildTile tile)
    {
        if (Config.BackfillPages)
        {
            foreach (var page in Pages)
            {
                if (page.TryAdd(tile))
                {
                    return;
                }
            }
        }

        if (Pages.Count == 0 || !Pages.Last().TryAdd(tile))
        {
            var newPage = new PageBuilder
            {
                PageFile = this,
                PageFileIndex = PageFileIndex,
                PageIndex = Pages.Count,
                Budget = 4
            };

            if (newPage.PageIndex == 0)
            {
                newPage.Budget += Marshal.SizeOf(typeof(GTPHeader));
            }

            Pages.Add(newPage);
            newPage.TryAdd(tile);
        }
    }

    public void Save(string path)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
        using var writer = new BinaryWriter(stream);
        Save(stream, writer);
    }

    public void SaveChunk(BinaryWriter writer, BuiltChunk chunk)
    {
        var header = new GTPChunkHeader
        {
            Codec = chunk.Codec,
            ParameterBlockID = chunk.ParameterBlockID,
            Size = (UInt32)chunk.EncodedBlob.Length
        };
        BinUtils.WriteStruct<GTPChunkHeader>(writer, ref header);
        writer.Write(chunk.EncodedBlob);
    }

    public void Save(Stream s, BinaryWriter writer)
    {
        var header = new GTPHeader
        {
            Magic = GTPHeader.HeaderMagic,
            Version = GTPHeader.DefaultVersion,
            GUID = Checksum
        };
        BinUtils.WriteStruct<GTPHeader>(writer, ref header);

        for (var i = 0; i < Pages.Count; i++)
        {
            var page = Pages[i];

            writer.Write((UInt32)page.Chunks.Count);
            foreach (var chunk in page.Chunks)
            {
                writer.Write(chunk.OffsetInPage);
            }

            foreach (var chunk in page.Chunks)
            {
                chunk.OffsetInPage = (uint)(s.Position % Config.PageSize);
                SaveChunk(writer, chunk);
            }

            if (s.Position > Config.PageSize * (i+1))
            {
                throw new Exception($"Overrun while writing page {i} of page file {Name}");
            }

            var padSize = (Config.PageSize - (s.Position % Config.PageSize)) % Config.PageSize;
            if (padSize > 0)
            {
                var pad = new byte[padSize];
                Array.Clear(pad, 0, (int)padSize);
                writer.Write(pad);
            }
        }

        for (var i = 0; i < Pages.Count; i++)
        {
            var page = Pages[i];
            s.Position = (i * Config.PageSize);
            if (i == 0)
            {
                s.Position += Marshal.SizeOf(typeof(GTPHeader));
            }

            writer.Write((UInt32)page.Chunks.Count);
            foreach (var chunk in page.Chunks)
            {
                writer.Write(chunk.OffsetInPage);
            }
        }
    }
}

public class PageFileSetBuilder(TileSetBuildData buildData, TileSetConfiguration config)
{
    private readonly TileSetBuildData BuildData = buildData;
    private readonly TileSetConfiguration Config = config;
    private List<PageFileBuilder> PageFiles = [];

    private void BuildPageFile(PageFileBuilder file, int level, int minTileX, int minTileY, int maxTileX, int maxTileY)
    {
        for (var y = minTileY; y <= maxTileY; y++)
        {
            for (var x = minTileX; x <= maxTileX; x++)
            {
                for (var layer = 0; layer < BuildData.Layers.Count; layer++)
                {
                    var tile = BuildData.Layers[layer].Levels[level].Get(x, y);
                    if (tile != null)
                    {
                        file.AddTile(tile);
                    }
                }
            }
        }
    }

    private void BuildPageFile(PageFileBuilder file, BuildTexture texture)
    {
        for (var level = 0; level < BuildData.MipFileStartLevel; level++)
        {
            var x = texture.X >> level;
            var y = texture.Y >> level;
            var width = texture.Width >> level;
            var height = texture.Height >> level;

            var minTileX = x / BuildData.RawTileWidth;
            var minTileY = y / BuildData.RawTileHeight;
            var maxTileX = (x + width - 1) / BuildData.RawTileWidth;
            var maxTileY = (y + height - 1) / BuildData.RawTileHeight;

            BuildPageFile(file, level, minTileX, minTileY, maxTileX, maxTileY);
        }
    }

    private void BuildMipPageFile(PageFileBuilder file)
    {
        for (var level = BuildData.MipFileStartLevel; level < BuildData.PageFileLevels; level++)
        {
            var lvl = BuildData.Layers[0].Levels[level];
            BuildPageFile(file, level, 0, 0, lvl.TilesX - 1, lvl.TilesY - 1);
        }
    }

    private void BuildFullPageFile(PageFileBuilder file)
    {
        for (var level = 0; level < BuildData.PageFileLevels; level++)
        {
            var lvl = BuildData.Layers[0].Levels[level];
            BuildPageFile(file, level, 0, 0, lvl.TilesX - 1, lvl.TilesY - 1);
        }
    }

    public List<PageFileBuilder> BuildFilePerGTex(List<BuildTexture> textures)
    {
        uint firstPageIndex = 0;
        foreach (var texture in textures)
        {
            var file = new PageFileBuilder(Config)
            {
                Name = texture.Name,
                FileName = BuildData.GTSName + "_" + texture.Name + ".gtp",
                Checksum = Guid.NewGuid(),
                PageFileIndex = PageFiles.Count
            };
            PageFiles.Add(file);
            BuildPageFile(file, texture);

            firstPageIndex += (uint)file.Pages.Count;
        }

        if (BuildData.MipFileStartLevel < BuildData.PageFileLevels)
        {
            var file = new PageFileBuilder(Config)
            {
                Name = "Mips",
                FileName = BuildData.GTSName + "_Mips.gtp",
                Checksum = Guid.NewGuid(),
                PageFileIndex = PageFiles.Count
            };
            PageFiles.Add(file);
            BuildMipPageFile(file);
        }

        return PageFiles;
    }

    public List<PageFileBuilder> BuildSingleFile()
    {
        var file = new PageFileBuilder(Config)
        {
            Name = "Global",
            FileName = BuildData.GTSName + ".gtp",
            Checksum = Guid.NewGuid(),
            PageFileIndex = PageFiles.Count
        };
        PageFiles.Add(file);
        BuildFullPageFile(file);

        return PageFiles;
    }

    public void DeduplicateTiles()
    {
        if (Config.DeduplicateTiles)
        {
            foreach (var file in PageFiles)
            {
                file.DeduplicateTiles();
            }
        }
    }

    public List<PageFileBuilder> CommitPageFiles()
    {
        foreach (var file in PageFiles)
        {
            file.CommitTiles();
        }

        return PageFiles;
    }
}
