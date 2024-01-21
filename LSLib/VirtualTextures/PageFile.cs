using LSLib.LS;

namespace LSLib.VirtualTextures;

public class PageFile : IDisposable
{
    private readonly VirtualTileSet TileSet;
    private readonly FileStream Stream;
    private readonly BinaryReader Reader;
    public GTPHeader Header;
    private readonly List<UInt32[]> ChunkOffsets;

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

    private byte[] DoUnpackTileUniform(GTPChunkHeader header)
    {
        var parameterBlock = (GTSUniformParameterBlock)TileSet.ParameterBlocks[header.ParameterBlockID];

        byte[] img = new byte[TileSet.Header.TileWidth * TileSet.Header.TileHeight];
        Array.Clear(img, 0, img.Length);
        return img;
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

    public BC5Image UnpackTileBC5(int pageIndex, int chunkIndex, TileCompressor compressor)
    {
        var compressedSize = 16 * ((TileSet.Header.TileWidth + 3) / 4) * ((TileSet.Header.TileHeight + 3) / 4)
            + 16 * ((TileSet.Header.TileWidth/2 + 3) / 4) * ((TileSet.Header.TileHeight/2 + 3) / 4);
        var chunk = UnpackTile(pageIndex, chunkIndex, compressedSize, compressor);
        return new BC5Image(chunk, TileSet.Header.TileWidth, TileSet.Header.TileHeight);
    }
}
