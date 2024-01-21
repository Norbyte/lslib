using LZ4;

namespace LSLib.VirtualTextures;

public enum TileCompressionMethod
{
    Raw,
    LZ4,
    LZ77
};

public enum TileCompressionPreference
{
    Uncompressed,
    Best,
    LZ4,
    LZ77
};

public class CompressedTile
{
    public TileCompressionMethod Method;
    public UInt32 ParameterBlockID;
    public byte[] Data;
}

public class TileCompressor
{
    public ParameterBlockContainer ParameterBlocks;
    public TileCompressionPreference Preference = TileCompressionPreference.Best;

    private byte[] GetRawBytes(BuildTile tile)
    {
        if (tile.EmbeddedMip == null)
        {
            return tile.Image.Data;
        }
        else
        {
            var data = new byte[tile.Image.Data.Length + tile.EmbeddedMip.Data.Length];
            Array.Copy(tile.Image.Data, 0, data, 0, tile.Image.Data.Length);
            Array.Copy(tile.EmbeddedMip.Data, 0, data, tile.Image.Data.Length, tile.EmbeddedMip.Data.Length);
            return data;
        }
    }

    public static byte[] CompressLZ4(byte[] raw)
    {
        return LZ4Codec.EncodeHC(raw, 0, raw.Length);
    }

    public static byte[] CompressLZ77(byte[] raw)
    {
        return Native.FastLZCompressor.Compress(raw, 2);
    }

    public byte[] Compress(byte[] uncompressed, out TileCompressionMethod method)
    {
        switch (Preference)
        {
            case TileCompressionPreference.Uncompressed:
                method = TileCompressionMethod.Raw;
                return uncompressed;

            case TileCompressionPreference.Best:
                var lz4 = CompressLZ4(uncompressed);
                var lz77 = CompressLZ77(uncompressed);
                if (lz4.Length <= lz77.Length)
                {
                    method = TileCompressionMethod.LZ4;
                    return lz4;
                }
                else
                {
                    method = TileCompressionMethod.LZ77;
                    return lz77;
                }

            case TileCompressionPreference.LZ4:
                method = TileCompressionMethod.LZ4;
                return CompressLZ4(uncompressed);

            case TileCompressionPreference.LZ77:
                method = TileCompressionMethod.LZ77;
                return CompressLZ77(uncompressed);

            default:
                throw new ArgumentException("Invalid compression preference");
        }
    }

    public CompressedTile Compress(BuildTile tile)
    {
        if (tile.Compressed != null)
        {
            return tile.Compressed;
        }

        var uncompressed = GetRawBytes(tile);
        var compressed = new CompressedTile();
        compressed.Data = Compress(uncompressed, out compressed.Method);

        var paramBlock = ParameterBlocks.GetOrAdd(tile.Codec, tile.DataType, compressed.Method);
        compressed.ParameterBlockID = paramBlock.ParameterBlockID;

        tile.Compressed = compressed;
        return compressed;
    }

    public TileCompressionMethod GetMethod(string method1, string method2)
    {
        if (method1 == "lz77" && method2 == "fastlz0.1.0")
        {
            return TileCompressionMethod.LZ77;
        }
        else if (method1 == "lz4" && method2 == "lz40.1.0")
        {
            return TileCompressionMethod.LZ4;
        }
        else if (method1 == "raw")
        {
            return TileCompressionMethod.Raw;
        }
        else
        {
            throw new InvalidDataException($"Unsupported compression format: '{method1}', '{method2}'");
        }
    }

    public byte[] Decompress(byte[] compressed, int outputSize, string method1, string method2)
    {
        return Decompress(compressed, outputSize, GetMethod(method1, method2));
    }

    public byte[] Decompress(byte[] compressed, int outputSize, TileCompressionMethod method)
    {
        switch (method)
        {
            case TileCompressionMethod.Raw:
                return compressed;
            case TileCompressionMethod.LZ4:
                var decompressed = new byte[outputSize];
                LZ4Codec.Decode(compressed, 0, compressed.Length, decompressed, 0, outputSize, true);
                return decompressed;
            case TileCompressionMethod.LZ77:
                return Native.FastLZCompressor.Decompress(compressed, outputSize);
            default:
                throw new ArgumentException();
        }
    }
}
