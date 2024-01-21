using System.Xml;

namespace LSLib.VirtualTextures;

public class TextureDescriptor
{
    public string Name;
    public List<string> Layers;
}

public class TileSetDescriptor
{
    public string Name;
    public List<TextureDescriptor> Textures = [];
    public TileSetConfiguration Config = new();
    public string RootPath;
    public string SourceTexturePath;
    public string VirtualTexturePath;

    public void Load(string path)
    {
        using var f = new FileStream(path, FileMode.Open, FileAccess.Read);
        var doc = new XmlDocument();
        doc.Load(f);
        Load(doc);
    }

    public void Load(XmlDocument doc)
    {
        var version = doc.DocumentElement.GetAttribute("Version");
        if (version == null || !Int32.TryParse(version, out int versionNum) || versionNum != 2)
        {
            throw new InvalidDataException("Expected TileSet XML descriptor version 2");
        }

        Name = doc.DocumentElement.GetAttribute("Name");
        Config.GTSName = Name;
        Config.Layers = [];

        var tileSetConfig = doc.DocumentElement.GetElementsByTagName("TileSetConfig");
        foreach (var node in (tileSetConfig[0] as XmlElement).ChildNodes)
        {
            if (node is XmlElement)
            {
                var key = (node as XmlElement).Name;
                var value = (node as XmlElement).InnerText;

                switch (key)
                {
                    case "TileWidth": Config.TileWidth = Int32.Parse(value); break;
                    case "TileHeight": Config.TileHeight = Int32.Parse(value); break;
                    case "TileBorder": Config.TileBorder = Int32.Parse(value); break;
                    case "Compression": Config.Compression = (TileCompressionPreference)Enum.Parse(typeof(TileCompressionPreference), value); break;
                    case "PageSize": Config.PageSize = Int32.Parse(value); break;
                    case "OneFilePerGTex": Config.OneFilePerGTex = Boolean.Parse(value); break;
                    case "BackfillPages": Config.BackfillPages = Boolean.Parse(value); break;
                    case "DeduplicateTiles": Config.DeduplicateTiles = Boolean.Parse(value); break;
                    case "EmbedMips": Config.EmbedMips = Boolean.Parse(value); break;
                    case "EmbedTopLevelMips": Config.EmbedTopLevelMips = Boolean.Parse(value); break;
                    case "ZeroBorders": Config.ZeroBorders = Boolean.Parse(value); break;
                    default: throw new InvalidDataException($"Unsupported configuration key: {key}");
                }
            }
        }

        var paths = doc.DocumentElement.GetElementsByTagName("Paths");
        foreach (var node in (paths[0] as XmlElement).ChildNodes)
        {
            if (node is XmlElement)
            {
                var key = (node as XmlElement).Name;
                var value = (node as XmlElement).InnerText;

                switch (key)
                {
                    case "SourceTextures": SourceTexturePath = Path.Combine(RootPath, value); break;
                    case "VirtualTextures": VirtualTexturePath = Path.Combine(RootPath, value); break;
                    default: throw new InvalidDataException($"Unsupported path type: {key}");
                }
            }
        }

        var layers = doc.DocumentElement.GetElementsByTagName("Layers");
        foreach (var node in (layers[0] as XmlElement).GetElementsByTagName("Layer"))
        {
            Config.Layers.Add(new BuildLayer
            {
                DataType = (GTSDataType)Enum.Parse(typeof(GTSDataType), (node as XmlElement).GetAttribute("Type")),
                Name = (node as XmlElement).GetAttribute("Name")
            });
        }

        if (Config.Layers.Count == 0)
        {
            throw new InvalidDataException("No tile set layers specified");
        }

        var textures = doc.DocumentElement.GetElementsByTagName("Texture");
        foreach (var texture in textures)
        {
            var tex = new TextureDescriptor()
            {
                Name = (texture as XmlElement).GetAttribute("Name"),
                Layers = []
            };
            Textures.Add(tex);

            foreach (var layer in Config.Layers)
            {
                tex.Layers.Add(null);
            }

            var texLayers = (texture as XmlElement).GetElementsByTagName("Layer");
            foreach (var layerNode in texLayers)
            {
                var name = (layerNode as XmlElement).GetAttribute("Name");
                var index = Config.Layers.FindIndex(ly => ly.Name == name);
                if (index == -1)
                {
                    throw new InvalidDataException($"Layer does not exist: '{name}'");
                }

                tex.Layers[index] = (layerNode as XmlElement).GetAttribute("Source");
            }
        }
    }
}

public class BuildTile
{
    public BC5Image Image;
    public BC5Image EmbeddedMip;
    public CompressedTile Compressed;

    // Set during initialization
    public int Layer;
    public GTSCodec Codec;
    public GTSDataType DataType;

    // Set during layout
    public int Level;
    public int X;
    public int Y;

    // Set during page file build
    public bool AddedToPageFile = false;
    public int PageFileIndex;
    public int PageIndex;
    public int ChunkIndex;
    public BuildTile DuplicateOf;
}

public class BuildLayer
{
    public GTSDataType DataType;
    public string Name;

    public List<BuildLevel> Levels;
}

public class TileSetConfiguration
{
    public string GTSName;
    public Int32 TileWidth = 0x80;
    public Int32 TileHeight = 0x80;
    public Int32 TileBorder = 8;
    public List<BuildLayer> Layers;
    public TileCompressionPreference Compression = TileCompressionPreference.Best;
    public Int32 PageSize = 0x100000;
    public bool OneFilePerGTex = false;
    public bool BackfillPages = true;
    public bool DeduplicateTiles = true;
    public bool EmbedMips = true;
    public bool EmbedTopLevelMips = true;
    public bool ZeroBorders = false;
}

public class BuildLayerTexture
{
    public string Path;
    public int FirstMip;
    public BC5Mips Mips;
}

public class BuildLevel
{
    public int Level; // Level index (0..n)
    public int Width;
    public int Height;
    public int TilesX;
    public int TilesY;
    public int PaddedTileWidth;
    public int PaddedTileHeight;
    public BuildTile[] Tiles;

    public BuildTile Get(int x, int y)
    {
        if (x >= TilesX || y >= TilesY)
        {
            throw new ArgumentException("Invalid tile index");
        }

        var off = x + TilesX * y;
        return Tiles[off];
    }

    public BuildTile GetOrCreateTile(int x, int y, int layer, GTSCodec codec, GTSDataType dataType)
    {
        if (x >= TilesX || y >= TilesY)
        {
            throw new ArgumentException("Invalid tile index");
        }

        var off = x + TilesX * y;
        if (Tiles[off] == null)
        {
            Tiles[off] = new BuildTile
            {
                Image = new BC5Image(PaddedTileWidth, PaddedTileHeight),
                Layer = layer,
                Codec = codec,
                DataType = dataType
            };
        }

        return Tiles[off];
    }
}

public class BuildTexture
{
    public string Name;
    public int Width;
    public int Height;
    // Position at level 0 (including FirstMip)
    public int X;
    public int Y;
    public List<BuildLayerTexture> Layers;
}

public class TileSetBuildData
{
    public List<BuildLayer> Layers;
    public string GTSName;
    // Size of tile including borders
    public int PaddedTileWidth;
    public int PaddedTileHeight;
    // Size of tile excluding borders from adjacent tiles
    public int RawTileWidth;
    public int RawTileHeight;
    // Size of tile border
    public int TileBorder;
    // Total size of tileset in pixels
    public int TotalWidth;
    public int TotalHeight;
    // Number of mip levels to save in page files
    public int PageFileLevels;
    // Number of mip levels to generate
    public int BuildLevels;
    // First mip level to save in a separate mip page file
    public int MipFileStartLevel;
}

public class ParameterBlock
{
    public GTSCodec Codec;
    public GTSDataType DataType;
    public TileCompressionMethod Compression;
    public UInt32 ParameterBlockID;
}

public class ParameterBlockContainer
{
    public List<ParameterBlock> ParameterBlocks = [];
    private UInt32 NextParameterBlockID = 1;

    public ParameterBlock GetOrAdd(GTSCodec codec, GTSDataType dataType, TileCompressionMethod compression)
    {
        foreach (var block in ParameterBlocks)
        {
            if (block.Codec == codec && block.DataType == dataType && block.Compression == compression)
            {
                return block;
            }
        }

        var newBlock = new ParameterBlock
        {
            Codec = codec,
            DataType = dataType,
            Compression = compression,
            ParameterBlockID = NextParameterBlockID++
        };
        ParameterBlocks.Add(newBlock);

        return newBlock;
    }
}

public class TileSetBuilder
{
    private readonly TileSetBuildData BuildData;
    private readonly TileSetConfiguration Config;
    private readonly TileCompressor Compressor;
    private readonly ParameterBlockContainer ParameterBlocks;
    private PageFileSetBuilder SetBuilder;

    public VirtualTileSet TileSet;
    public List<BuildTexture> Textures;
    public List<PageFileBuilder> PageFiles;

    public delegate void BuildStepDelegate(string step);
    public BuildStepDelegate OnStepStarted = delegate { };
    public delegate void BuildStepProgressDelegate(int numerator, int denumerator);
    public BuildStepProgressDelegate OnStepProgress = delegate { };

    private List<BuildTile[]> PerLevelFlatTiles;

    public TileSetBuilder(TileSetConfiguration config)
    {
        BuildData = new TileSetBuildData
        {
            Layers = config.Layers,
            GTSName = config.GTSName,
            PaddedTileWidth = config.TileWidth + 2 * config.TileBorder,
            PaddedTileHeight = config.TileHeight + 2 * config.TileBorder,
            RawTileWidth = config.TileWidth,
            RawTileHeight = config.TileHeight,
            TileBorder = config.TileBorder
        };
        Config = config;

        Compressor = new TileCompressor();
        ParameterBlocks = new ParameterBlockContainer();
        Compressor.Preference = Config.Compression;
        Compressor.ParameterBlocks = ParameterBlocks;

        Textures = [];
    }

    public void AddTexture(string name, List<string> texturePaths)
    {
        var tex = new BuildTexture
        {
            Name = name,
            Width = 0,
            Height = 0,
            X = 0,
            Y = 0,
            Layers = []
        };

        foreach (var path in texturePaths)
        {
            if (path != null)
            {
                var mips = new BC5Mips();
                mips.LoadDDS(path);
                if (mips.Mips.Count <= 1)
                {
                    throw new InvalidDataException($"Texture must include mipmaps: {path}");
                }

                var mip = mips.Mips[0];
                if ((mip.Width % BuildData.RawTileWidth) != 0
                    || (mip.Height % BuildData.RawTileHeight) != 0)
                {
                    throw new InvalidDataException($"Texture {path} size ({mip.Width}x{mip.Height}) must be a multiple of the virtual tile size ({BuildData.RawTileWidth}x{BuildData.RawTileHeight})");
                }

                if ((mip.Width & (mip.Width - 1)) != 0
                    || (mip.Height & (mip.Height - 1)) != 0)
                {
                    throw new InvalidDataException($"Texture {path} size ({mip.Width}x{mip.Height}) must be a multiple of two");
                }

                tex.Layers.Add(new BuildLayerTexture
                {
                    Path = path,
                    FirstMip = 0,
                    Mips = mips
                });
            }
            else
            {
                tex.Layers.Add(null);
            }
        }

        // Figure out top-level size for texture across all layers
        foreach (var layer in tex.Layers)
        {
            if (layer == null) continue;

            tex.Width = Math.Max(tex.Width, layer.Mips.Mips[0].Width);
            tex.Height = Math.Max(tex.Height, layer.Mips.Mips[0].Height);
        }

        // Adjust first layer index for textures
        foreach (var layer in tex.Layers)
        {
            if (layer == null) continue;

            var mip = layer.Mips.Mips[0];
            if (mip.Width > tex.Width || mip.Height > tex.Height)
            {
                throw new InvalidDataException($"Top-level texture size mismatch; texture {layer.Path} is {mip.Width}x{mip.Height}, size across all layers is {tex.Width}x{tex.Height}");
            }

            var mulW = tex.Width / mip.Width;
            var mulH = tex.Height / mip.Height;

            if ((tex.Width % mip.Width) != 0 || (tex.Height % mip.Height) != 0
                || mulW != mulH 
                // Check if total layer size size is a power-of-two of the texture size
                || (mulW & (mulW - 1)) != 0)
            {
                throw new InvalidDataException($"Texture sizes within all layers should be multiples of each other; texture {layer.Path} is {mip.Width}x{mip.Height}, size across all layers is {tex.Width}x{tex.Height}");
            }

            // Adjust first mip index based on texture size
            while (mulW > 1)
            {
                mulW >>= 1;
                layer.FirstMip++;
            }
        }

        Console.WriteLine($"Added GTex {tex.Name} ({tex.Width}x{tex.Height})");
        Textures.Add(tex);
    }

    private void BuildParameterBlocks()
    {
        var blocks = ParameterBlocks.ParameterBlocks;
        TileSet.ParameterBlockHeaders = new GTSParameterBlockHeader[blocks.Count];
        TileSet.ParameterBlocks = [];

        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            ref var header = ref TileSet.ParameterBlockHeaders[i];

            header.ParameterBlockID = block.ParameterBlockID;
            header.Codec = block.Codec;

            switch (block.Codec)
            {
                case GTSCodec.BC:
                    header.ParameterBlockSize = (uint)Marshal.SizeOf(typeof(GTSBCParameterBlock));

                    string compression1, compression2;
                    switch (block.Compression)
                    {
                        case TileCompressionMethod.Raw:
                            compression1 = "raw";
                            compression2 = "";
                            break;
                            
                        case TileCompressionMethod.LZ4:
                            compression1 = "lz4";
                            compression2 = "lz40.1.0";
                            break;
                            
                        case TileCompressionMethod.LZ77:
                            compression1 = "lz77";
                            compression2 = "fastlz0.1.0";
                            break;

                        default:
                            throw new ArgumentException("Unsupported compression method");
                    }

                    TileSet.ParameterBlocks[block.ParameterBlockID] = new GTSBCParameterBlock
                    {
                        Version = 0x238e,
                        CompressionName1 = compression1,
                        CompressionName2 = compression2,
                        B = 0,
                        C1 = 0,
                        C2 = 0,
                        BCField3 = 0,
                        DataType = (Byte)block.DataType,
                        D = 0,
                        FourCC = 0x20334342,
                        E1 = 0,
                        SaveMip = 1,
                        E3 = 0,
                        E4 = 0,
                        F = 0
                    };
                    break;

                case GTSCodec.Uniform:
                    header.ParameterBlockSize = (uint)Marshal.SizeOf(typeof(GTSUniformParameterBlock));
                    TileSet.ParameterBlocks[block.ParameterBlockID] = new GTSUniformParameterBlock
                    {
                        Version = 0x42,
                        A_Unused = 0,
                        Width = 4,
                        Height = 1,
                        DataType = block.DataType
                    };
                    break;

                default:
                    throw new ArgumentException("Unsupported codec type");
            }
        }
    }

    private void BuildFourCC()
    {
        var fourCC = new TileSetFourCC();
        var meta = FourCCElement.Make("META");
        fourCC.Root = meta;

        var atlas = FourCCElement.Make("ATLS");
        meta.Children.Add(atlas);

        var textures = FourCCElement.Make("TXTS");
        atlas.Children.Add(textures);

        foreach (var texture in Textures)
        {
            var tex = FourCCElement.Make("TXTR");
            textures.Children.Add(tex);
            tex.Children.Add(FourCCElement.Make("NAME", texture.Name));
            tex.Children.Add(FourCCElement.Make("WDTH", (uint)texture.Width));
            tex.Children.Add(FourCCElement.Make("HGHT", (uint)texture.Height));
            tex.Children.Add(FourCCElement.Make("XXXX", (uint)texture.X));
            tex.Children.Add(FourCCElement.Make("YYYY", (uint)texture.Y));
            tex.Children.Add(FourCCElement.Make("ADDR", "None"));
            tex.Children.Add(FourCCElement.Make("SRGB", FourCCElementType.BinaryInt, [ 
                0x01, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 
                0x00, 0x00, 0x00, 0x00
            ]));
            tex.Children.Add(FourCCElement.Make("THMB", FourCCElementType.BinaryGuid, [ 
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
            ]));
        }

        var project = FourCCElement.Make("PROJ", "");
        meta.Children.Add(project);

        var layers = FourCCElement.Make("LINF");
        meta.Children.Add(layers);

        for (var i = 0; i < BuildData.Layers.Count; i++)
        {
            var layerInfo = FourCCElement.Make("LAYR");
            layers.Children.Add(layerInfo);
            layerInfo.Children.Add(FourCCElement.Make("INDX", (uint)i));
            layerInfo.Children.Add(FourCCElement.Make("TYPE", "BC3"));
            layerInfo.Children.Add(FourCCElement.Make("NAME", BuildData.Layers[i].Name));
        }

        var info = FourCCElement.Make("INFO");
        meta.Children.Add(info);

        var compiler = FourCCElement.Make("COMP");
        info.Children.Add(compiler);

        var compVer = FourCCElement.Make("CMPW");
        compiler.Children.Add(compVer);
        compVer.Children.Add(FourCCElement.Make("MAJR", 5));
        compVer.Children.Add(FourCCElement.Make("MINR", 0));

        var buildVer = FourCCElement.Make("BLDV");
        compiler.Children.Add(buildVer);
        buildVer.Children.Add(FourCCElement.Make("MAJR", 5));
        buildVer.Children.Add(FourCCElement.Make("MINR", 1));
        buildVer.Children.Add(FourCCElement.Make("BINF", "LSLib"));

        info.Children.Add(FourCCElement.Make("DATE", "02-08-2023 07:49:30.7662814 PM +02:00"));
        info.Children.Add(FourCCElement.Make("BLKS", "4096"));
        info.Children.Add(FourCCElement.Make("TILE", "Software"));
        info.Children.Add(FourCCElement.Make("BDPR", "default"));
        info.Children.Add(FourCCElement.Make("LTMP", 0));

        TileSet.FourCCMetadata = fourCC;
    }

    private void CalculateGeometry()
    {
        var geom = new TileSetGeometryCalculator
        {
            BuildData = BuildData,
            Textures = Textures
        };
        geom.Update();

        Console.WriteLine($"Tile set geometry: {BuildData.TotalWidth}x{BuildData.TotalHeight} ({BuildData.TotalWidth/BuildData.RawTileWidth}x{BuildData.TotalHeight/BuildData.RawTileHeight} tiles), {BuildData.RawTileWidth}x{BuildData.RawTileHeight} tile size, {BuildData.PaddedTileWidth}x{BuildData.PaddedTileHeight} tile size with adjacency data");
    }

    private static int Clamp(int x, int min, int max)
    {
        return Math.Min(max, Math.Max(x, min));
    }

    private void StitchPartialTile(BuildTile tile, BC5Image source, int tileX, int tileY, int sourceX, int sourceY, int width, int height)
    {
        source.CopyTo(
            tile.Image,
            sourceX, sourceY,
            tileX + BuildData.TileBorder,
            tileY + BuildData.TileBorder,
            width, height
        );
    }

    private void StitchTiles(BuildLevel level, int layer, int x, int y, BC5Image mip)
    {
        var layerInfo = BuildData.Layers[layer];
        var firstTileX = x / BuildData.RawTileWidth;
        var firstTileY = y / BuildData.RawTileHeight;
        var lastTileX = (x + mip.Width - 1) / BuildData.RawTileWidth;
        var lastTileY = (y + mip.Height - 1) / BuildData.RawTileHeight;

        int sourceY = 0;
        for (var tileY = firstTileY; tileY <= lastTileY; tileY++)
        {
            var tileYPixelsMin = tileY * BuildData.RawTileHeight;
            var tileYPixelsMax = tileYPixelsMin + BuildData.RawTileHeight;

            var stitchYMin = Clamp(y, tileYPixelsMin, tileYPixelsMax);
            var stitchYMax = Clamp(y + mip.Height, tileYPixelsMin, tileYPixelsMax);

            var stitchH = stitchYMax - stitchYMin;

            int sourceX = 0;
            for (var tileX = firstTileX; tileX <= lastTileX; tileX++)
            {
                var tileXPixelsMin = tileX * BuildData.RawTileWidth;
                var tileXPixelsMax = tileXPixelsMin + BuildData.RawTileWidth;

                var stitchXMin = Clamp(x, tileXPixelsMin, tileXPixelsMax);
                var stitchXMax = Clamp(x + mip.Width, tileXPixelsMin, tileXPixelsMax);

                var stitchW = stitchXMax - stitchXMin;

                // GIGA JANK
                if (stitchW >= 4 && stitchH >= 4)
                {
                    var tile = level.GetOrCreateTile(tileX, tileY, layer, GTSCodec.BC, layerInfo.DataType);
                    StitchPartialTile(tile, mip,
                        stitchXMin - tileXPixelsMin,
                        stitchYMin - tileYPixelsMin,
                        sourceX, sourceY,
                        stitchXMax - stitchXMin,
                        stitchYMax - stitchYMin
                    );
                }

                sourceX += stitchW;
            }

            sourceY += stitchH;
        }
    }

    private void BuildTextureTiles(BuildTexture texture, int level, int layerIndex, BuildLayer layer, BC5Image mip)
    {
        var x = texture.X >> level;
        var y = texture.Y >> level;
        StitchTiles(layer.Levels[level], layerIndex, x, y, mip);
    }

    private void BuildTextureTiles(BuildTexture texture, int layerIndex, BuildLayerTexture texLayer, BuildLayer layer)
    {
        if (texLayer.FirstMip + texLayer.Mips.Mips.Count < BuildData.BuildLevels)
        {
            throw new InvalidDataException($"Insufficient mip layers in texture '{texture.Name}', layer '{layer.Name}'; got {texLayer.FirstMip}+{texLayer.Mips.Mips.Count}, virtual texture has {BuildData.BuildLevels}");
        }

        for (var i = texLayer.FirstMip; i < BuildData.BuildLevels; i++)
        {
            BuildTextureTiles(texture, i, layerIndex, layer, texLayer.Mips.Mips[i - texLayer.FirstMip]);
        }
    }

    private void BuildTiles()
    {
        foreach (var texture in Textures)
        {
            for (var layerIdx = 0; layerIdx < texture.Layers.Count; layerIdx++)
            {
                if (texture.Layers[layerIdx] != null)
                {
                    BuildTextureTiles(texture, layerIdx, texture.Layers[layerIdx], BuildData.Layers[layerIdx]);
                }
            }
        }
    }

    private void BuildTileBorders(BuildLevel level)
    {
        for (var y = 0; y < level.TilesY; y++)
        {
            for (var x = 0; x < level.TilesX; x++)
            {
                var tile = level.Get(x, y);
                if (tile == null) continue;

                // Left
                if (x > 0)
                {
                    level.Get(x - 1, y)?.Image.CopyTo(tile.Image,
                        BuildData.RawTileWidth, 0,
                        0, 0,
                        BuildData.TileBorder, BuildData.PaddedTileHeight);
                }

                // Right
                if (x + 1 < level.TilesX)
                {
                    level.Get(x + 1, y)?.Image.CopyTo(tile.Image,
                        BuildData.TileBorder, 0,
                        BuildData.RawTileWidth + BuildData.TileBorder, 0,
                        BuildData.TileBorder, BuildData.PaddedTileHeight);
                }

                // Top
                if (y > 0)
                {
                    level.Get(x, y - 1)?.Image.CopyTo(tile.Image,
                        0, BuildData.RawTileHeight,
                        0, 0,
                        BuildData.PaddedTileWidth, BuildData.TileBorder);
                }

                // Bottom
                if (y + 1 < level.TilesY)
                {
                    level.Get(x, y + 1)?.Image.CopyTo(tile.Image,
                        0, BuildData.TileBorder,
                        0, BuildData.RawTileHeight + BuildData.TileBorder,
                        BuildData.PaddedTileWidth, BuildData.TileBorder);

                    // Bottom Left corner
                    if (x > 0)
                    {
                        level.Get(x - 1, y + 1)?.Image.CopyTo(tile.Image,
                            BuildData.RawTileWidth, BuildData.TileBorder,
                            0, BuildData.RawTileHeight + BuildData.TileBorder,
                            BuildData.TileBorder, BuildData.TileBorder);
                    }

                    // Bottom Right corner
                    if (x + 1 < level.TilesX)
                    {
                        level.Get(x + 1, y + 1)?.Image.CopyTo(tile.Image,
                            BuildData.TileBorder, BuildData.TileBorder,
                            BuildData.RawTileWidth + BuildData.TileBorder, BuildData.RawTileHeight + BuildData.TileBorder,
                            BuildData.TileBorder, BuildData.TileBorder);
                    }
                }
            }
        }
    }

    private void BuildTileBorders()
    {
        foreach (var layer in BuildData.Layers)
        {
            foreach (var level in layer.Levels)
            {
                BuildTileBorders(level);
            }
        }
    }

    private void EmbedTileMips(BuildLayer layer, BuildLevel level)
    {
        for (var y = 0; y < level.TilesY; y++)
        {
            for (var x = 0; x < level.TilesX; x++)
            {
                var tile = level.Get(x, y);
                if (tile != null)
                {
                    if (level.Level + 1 < BuildData.BuildLevels)
                    {
                        var nextLevelTile = layer.Levels[level.Level + 1].Get(x / 2, y / 2);
                        if (nextLevelTile != null)
                        {
                            var nextMip = new BC5Image(BuildData.PaddedTileWidth / 2, BuildData.PaddedTileHeight / 2);
                            var mipX = (x & 1) * (BuildData.RawTileWidth / 2) + BuildData.TileBorder / 2;
                            var mipY = (y & 1) * (BuildData.RawTileHeight / 2) + BuildData.TileBorder / 2;
                            nextLevelTile.Image.CopyTo(nextMip, mipX, mipY, 0, 0, BuildData.PaddedTileWidth / 2, BuildData.PaddedTileHeight / 2);
                            tile.EmbeddedMip = nextMip;
                        }
                    }
                }
            }
        }
    }

    private void EmbedTileMips()
    {
        foreach (var layer in BuildData.Layers)
        {
            foreach (var level in layer.Levels)
            {
                if (level.Level > 0 || Config.EmbedTopLevelMips)
                {
                    EmbedTileMips(layer, level);
                }
            }
        }
    }

    private void BuildGTSHeaders()
    {
        // Configuration-independent defaults
        ref GTSHeader header = ref TileSet.Header;
        header.Magic = GTSHeader.GRPGMagic;
        header.Version = GTSHeader.CurrentVersion;
        header.Unused = 0;
        header.GUID = Guid.NewGuid();
        header.I6 = 0;
        header.I7 = 0;
        header.M = 0;
        header.N = 0;
        header.O = 0;
        header.P = 0;
        header.Q = 0;
        header.R = 0;
        header.S = 0;
        header.PageSize = (UInt32)Config.PageSize;
        header.XJJ = 0;
        header.XKK = 0;
        header.XLL = 0;
        header.XMM = 0;

        header.TileWidth = BuildData.PaddedTileWidth;
        header.TileHeight = BuildData.PaddedTileHeight;
        header.TileBorder = BuildData.TileBorder;
    }

    private void PreparePageFiles()
    {
        SetBuilder = new PageFileSetBuilder(BuildData, Config);
        if (Config.OneFilePerGTex)
        {
            PageFiles = SetBuilder.BuildFilePerGTex(Textures);
        }
        else
        {
            PageFiles = SetBuilder.BuildSingleFile();
        }
    }

    private void BuildPageFileMetadata()
    {
        TileSet.PageFileInfos = [];
        uint firstPageIndex = 0;
        foreach (var file in PageFiles)
        {
            var fileInfo = new PageFileInfo
            {
                Meta = new GTSPageFileInfo
                {
                    FileName = file.FileName,
                    NumPages = (uint)file.Pages.Count,
                    Checksum = file.Checksum,
                    F = 2
                },
                FirstPageIndex = firstPageIndex,
                FileName = file.FileName
            };
            TileSet.PageFileInfos.Add(fileInfo);
            firstPageIndex += (uint)file.Pages.Count;
        }
    }

    private void BuildGTS()
    {
        TileSet = new VirtualTileSet();
        BuildGTSHeaders();

        TileSet.TileSetLayers = new GTSTileSetLayer[BuildData.Layers.Count];
        for (int i = 0; i < BuildData.Layers.Count; i++)
        {
            var layer = BuildData.Layers[i];
            ref var gtsLayer = ref TileSet.TileSetLayers[i];
            gtsLayer.DataType = layer.DataType;
            gtsLayer.B = -1;
        }

        var levels = BuildData.Layers[0].Levels;

        TileSet.TileSetLevels = new GTSTileSetLevel[BuildData.PageFileLevels];
        for (int i = 0; i < BuildData.PageFileLevels; i++)
        {
            var level = levels[i];
            ref var gtsLevel = ref TileSet.TileSetLevels[i];
            gtsLevel.Width = (uint)level.TilesX;
            gtsLevel.Height = (uint)level.TilesY;
        }

        OnStepStarted("Preparing page files");
        PreparePageFiles();

        OnStepStarted("Deduplicating tiles");
        SetBuilder.DeduplicateTiles();

        OnStepStarted("Encoding tiles");
        CompressTiles();

        OnStepStarted("Building page files");
        SetBuilder.CommitPageFiles();

        OnStepStarted("Generating tile lists");
        BuildPageFileMetadata();
        BuildFlatTileList();

        OnStepStarted("Building metadata");
        BuildTileInfos();
        BuildTileDownsampleInfos();

        BuildParameterBlocks();
        BuildFourCC();
    }

    public void BuildFlatTileList()
    {
        PerLevelFlatTiles = new List<BuildTile[]>(BuildData.PageFileLevels);

        for (var level = 0; level < BuildData.PageFileLevels; level++)
        {
            var levelInfo = BuildData.Layers[0].Levels[level];
            var flatTiles = new BuildTile[levelInfo.TilesX * levelInfo.TilesY * BuildData.Layers.Count];
            PerLevelFlatTiles.Add(flatTiles);

            var tileIdx = 0;
            for (var y = 0; y < levelInfo.TilesY; y++)
            {
                for (var x = 0; x < levelInfo.TilesX; x++)
                {
                    for (var layer = 0; layer < BuildData.Layers.Count; layer++)
                    {
                        var tile = BuildData.Layers[layer].Levels[level].Get(x, y);
                        if (tile != null)
                        {
                            tile.Layer = layer;
                            tile.Level = level;
                            tile.X = x;
                            tile.Y = y;
                            flatTiles[tileIdx] = tile;
                        }
                        else
                        {
                            flatTiles[tileIdx] = null;
                        }

                        tileIdx++;
                    }
                }
            }
        }
    }

    public void CompressTiles()
    {
        var numTiles = PageFiles.Sum(pf => pf.PendingTiles.Count);
        var nextTile = 0;

        foreach (var file in PageFiles)
        {
            foreach (var tile in file.PendingTiles)
            {
                OnStepProgress(nextTile++, numTiles);
                if (tile.DuplicateOf == null)
                {
                    Compressor.Compress(tile);
                }
            }
        }
    }

    public void BuildTileInfos()
    {
        TileSet.PerLevelFlatTileIndices = new List<UInt32[]>(BuildData.PageFileLevels);
        PerLevelFlatTiles = new List<BuildTile[]>(BuildData.PageFileLevels);

        var flatTileMap = new Dictionary<long, uint>();
        var flatTileInfos = new List<GTSFlatTileInfo>();
        var packedTileIds = new List<GTSPackedTileID>();

        for (var level = 0; level < BuildData.PageFileLevels; level++)
        {
            var levelInfo = BuildData.Layers[0].Levels[level];
            var flatTileIndices = new UInt32[levelInfo.TilesX * levelInfo.TilesY * BuildData.Layers.Count];
            TileSet.PerLevelFlatTileIndices.Add(flatTileIndices);

            var flatTiles = new BuildTile[levelInfo.TilesX * levelInfo.TilesY * BuildData.Layers.Count];
            PerLevelFlatTiles.Add(flatTiles);

            var tileIdx = 0;
            for (var y = 0; y < levelInfo.TilesY; y++)
            {
                for (var x = 0; x < levelInfo.TilesX; x++)
                {
                    for (var layer = 0; layer < BuildData.Layers.Count; layer++)
                    {
                        var tile = BuildData.Layers[layer].Levels[level].Get(x, y);
                        if (tile != null)
                        {
                            uint flatTileIdx;
                            var packedTileIdx = (uint)packedTileIds.Count;

                            var packedTile = new GTSPackedTileID((uint)layer, (uint)level, (uint)x, (uint)y);
                            packedTileIds.Add(packedTile);

                            var tileKey = (long)tile.ChunkIndex
                                | ((long)tile.PageIndex << 16)
                                | ((long)tile.ChunkIndex << 32);
                            if (flatTileMap.TryGetValue(tileKey, out uint dupTileIdx))
                            {
                                flatTileIdx = dupTileIdx;
                            }
                            else
                            {
                                flatTileIdx = (uint)flatTileInfos.Count;
                                flatTileMap[tileKey] = flatTileIdx;
                                var tileInfo = new GTSFlatTileInfo
                                {
                                    PageFileIndex = (UInt16)tile.PageFileIndex,
                                    PageIndex = (UInt16)tile.PageIndex,
                                    ChunkIndex = (UInt16)tile.ChunkIndex,
                                    D = 1,
                                    PackedTileIndex = packedTileIdx
                                };
                                flatTileInfos.Add(tileInfo);
                            }

                            flatTileIndices[tileIdx] = flatTileIdx;
                            flatTiles[tileIdx] = tile;
                        }
                        else
                        {
                            flatTileIndices[tileIdx] = 0xFFFFFFFF;
                            flatTiles[tileIdx] = null;
                        }

                        tileIdx++;
                    }
                }
            }
        }

        TileSet.PackedTileIDs = packedTileIds.ToArray();
        TileSet.FlatTileInfos = flatTileInfos.ToArray();
    }

    public void BuildTileDownsampleInfos()
    {
        for (var level = 0; level < BuildData.PageFileLevels; level++)
        {
            var levelInfo = BuildData.Layers[0].Levels[level];
            var flatTileIndices = TileSet.PerLevelFlatTileIndices[level];

            var tileIdx = 0;
            for (var y = 0; y < levelInfo.TilesY; y++)
            {
                for (var x = 0; x < levelInfo.TilesX; x++)
                {
                    for (var layer = 0; layer < BuildData.Layers.Count; layer++)
                    {
                        if (flatTileIndices[tileIdx] == 0xFFFFFFFF)
                        {
                            for (var downsampleLevel = level + 1; downsampleLevel < BuildData.PageFileLevels; downsampleLevel++)
                            {
                                var downsampleX = x >> (downsampleLevel - level);
                                var downsampleY = y >> (downsampleLevel - level);

                                var dsIndices = TileSet.PerLevelFlatTileIndices[downsampleLevel];
                                var dsIndex = dsIndices[layer + BuildData.Layers.Count * (downsampleX + downsampleY * BuildData.Layers[layer].Levels[downsampleLevel].TilesX)];
                                if ((dsIndex & 0x80000000) == 0)
                                {
                                    flatTileIndices[tileIdx] = dsIndex | 0x80000000;
                                    break;
                                }
                            }
                        }

                        tileIdx++;
                    }
                }
            }
        }
    }

    public void Build(string dir)
    {
        OnStepStarted("Calculating geometry");
        CalculateGeometry();

        OnStepStarted("Building tiles");
        BuildTiles();

        if (BuildData.TileBorder > 0 && !Config.ZeroBorders)
        {
            OnStepStarted("Building tile borders");
            BuildTileBorders();
        }

        OnStepStarted("Embedding tile mipmaps");
        if (Config.EmbedMips)
        {
            EmbedTileMips();
        }

        BuildGTS();

        long tileBytes = 0, embeddedMipBytes = 0, tileCompressedBytes = 0, pages = 0, chunks = 0, levelTiles = 0, duplicates = 0;

        foreach (var pageFile in PageFiles)
        {
            pages += pageFile.Pages.Count;
            foreach (var page in pageFile.Pages)
            {
                chunks += page.Chunks.Count;
            }
        }

        foreach (var level in PerLevelFlatTiles)
        {
            levelTiles += level.Length;
            foreach (var tile in level)
            {
                if (tile != null)
                {
                    if (tile.DuplicateOf == null)
                    {
                        tileBytes += tile.Image.Data.Length;
                        if (tile.EmbeddedMip != null)
                        {
                            embeddedMipBytes += tile.EmbeddedMip.Data.Length;
                        }

                        tileCompressedBytes += tile.Compressed.Data.Length;
                    }
                    else
                    {
                        duplicates++;
                    }
                }
            }
        }

        Console.WriteLine($"Tile map: {levelTiles} total, {TileSet.FlatTileInfos.Length} in use, {duplicates} duplicates");
        Console.WriteLine($"Generated {PageFiles.Count} page files, {pages} pages, {chunks} chunks");
        Console.WriteLine($"Raw tile data: {tileBytes / 1024} KB tiles, {embeddedMipBytes / 1024} KB embedded mips, {tileCompressedBytes / 1024} KB transcoded, {pages*Config.PageSize/1024} KB pages total");

        OnStepStarted("Saving tile set");
        TileSet.Save(Path.Join(dir, BuildData.GTSName + ".gts"));

        foreach (var file in PageFiles)
        {
            OnStepStarted($"Saving page file: {file.FileName}");
            file.Save(Path.Join(dir, file.FileName));
        }
    }
}
