using LSLib.LS;
using System.Diagnostics;
using BCnEncoder.Shared;

namespace LSLib.VirtualTextures;

public class PageFile : IDisposable
{
    private readonly VirtualTileSet TileSet;
    private readonly FileStream Stream;
    private readonly BinaryReader Reader;
    public GTPHeader Header;
    internal readonly List<UInt32[]> ChunkOffsets;

    public PageFile(VirtualTileSet tileset, string path)
    {
        TileSet = tileset;
        Stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        Reader = new BinaryReader(Stream);

        Header = BinUtils.ReadStruct<GTPHeader>(Reader);

        var numPages = Stream.Length / tileset.Header.PageSize;
        ChunkOffsets = [];

        for (var page = 0; page < numPages; page++)
        {
            var numOffsets = Reader.ReadUInt32();
            var offsets = new UInt32[numOffsets];
            BinUtils.ReadStructs<UInt32>(Reader, offsets);
            ChunkOffsets.Add(offsets);

            Stream.Position = (page + 1) * tileset.Header.PageSize;
        }
    }

    public void Dispose()
    {
        Reader.Dispose();
        Stream.Dispose();
    }

    private byte[] DoUnpackTileBC(GTPChunkHeader header, int outputSize, TileCompressor compressor)
    {
        var parameterBlock = (GTSBCParameterBlock)TileSet.ParameterBlocks[header.ParameterBlockID];
        var compressed = Reader.ReadBytes((int)header.Size);
        return compressor.Decompress(compressed, outputSize, parameterBlock.CompressionName1, parameterBlock.CompressionName2);
    }

    private byte[] BuildUniformBC3Tile(ColorRgba32 color)
    {
        var block = BC3Image.BuildUniformBC3Block(color);

        var tileSize = TileSet.Header.TileWidth * TileSet.Header.TileHeight;
        byte[] img = new byte[tileSize];

        for (var i = 0; i < tileSize/16; i++)
        {
            Array.Copy(block, 0, img, i*16, 16);
        }

        return img;
    }

    private byte[] DoUnpackTileUniform(GTPChunkHeader header)
    {
        var parameterBlock = (GTSUniformParameterBlock)TileSet.ParameterBlocks[header.ParameterBlockID];

        Debug.Assert(header.Size == 4);
        Debug.Assert(parameterBlock.Version == 0x42);
        Debug.Assert(parameterBlock.Width == 4);
        Debug.Assert(parameterBlock.Height == 1);
        Debug.Assert(parameterBlock.DataType == GTSDataType.X8Y8Z8W8 || parameterBlock.DataType == GTSDataType.R8G8B8A8_SRGB);

        var r = Reader.ReadByte();
        var g = Reader.ReadByte();
        var b = Reader.ReadByte();
        var a = Reader.ReadByte();
        var color = new ColorRgba32(r, g, b, a);

        return BuildUniformBC3Tile(color);
    }

    public byte[] UnpackTile(int pageIndex, int chunkIndex, int outputSize, TileCompressor compressor)
    {
        Stream.Position = ChunkOffsets[pageIndex][chunkIndex] + (pageIndex * TileSet.Header.PageSize);
        var chunkHeader = BinUtils.ReadStruct<GTPChunkHeader>(Reader);
        return chunkHeader.Codec switch
        {
            GTSCodec.Uniform => DoUnpackTileUniform(chunkHeader),
            GTSCodec.BC => DoUnpackTileBC(chunkHeader, outputSize, compressor),
            _ => throw new InvalidDataException($"Unsupported codec: {chunkHeader.Codec}"),
        };
    }

    public BC3Image UnpackTileBC3(int pageIndex, int chunkIndex, TileCompressor compressor)
    {
        var compressedSize = 16 * ((TileSet.Header.TileWidth + 3) / 4) * ((TileSet.Header.TileHeight + 3) / 4)
            + 16 * ((TileSet.Header.TileWidth/2 + 3) / 4) * ((TileSet.Header.TileHeight/2 + 3) / 4);
        var chunk = UnpackTile(pageIndex, chunkIndex, compressedSize, compressor);
        return new BC3Image(chunk, TileSet.Header.TileWidth, TileSet.Header.TileHeight);
    }
}
