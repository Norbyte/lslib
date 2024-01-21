namespace LSLib.LS;

public enum CompressionMethod
{
    None,
    Zlib,
    LZ4,
    Zstd
};

public enum LSCompressionLevel
{
    Fast,
    Default,
    Max
};

public enum CompressionFlags : byte
{
    MethodNone = 0,
    MethodZlib = 1,
    MethodLZ4 = 2,
    MethodZstd = 3,
    FastCompress = 0x10,
    DefaultCompress = 0x20,
    MaxCompress = 0x40
};

public static class CompressionFlagExtensions
{
    public static CompressionMethod Method(this CompressionFlags f)
    {
        return (CompressionFlags)((byte)f & 0x0F) switch
        {
            CompressionFlags.MethodNone => CompressionMethod.None,
            CompressionFlags.MethodZlib => CompressionMethod.Zlib,
            CompressionFlags.MethodLZ4 => CompressionMethod.LZ4,
            CompressionFlags.MethodZstd => CompressionMethod.Zstd,
            _ => throw new NotSupportedException($"Unsupported compression method: {(byte)f & 0x0F}")
        };
    }

    public static LSCompressionLevel Level(this CompressionFlags f)
    {
        return (CompressionFlags)((byte)f & 0xF0) switch
        {
            CompressionFlags.FastCompress => LSCompressionLevel.Fast,
            CompressionFlags.DefaultCompress => LSCompressionLevel.Default,
            CompressionFlags.MaxCompress => LSCompressionLevel.Max,
            // Ignore unknown compression levels since they have no impact on actual decompression logic
            _ => LSCompressionLevel.Default
        };
    }

    public static CompressionFlags ToFlags(this CompressionMethod method)
    {
        return method switch
        {
            CompressionMethod.None => CompressionFlags.MethodNone,
            CompressionMethod.Zlib => CompressionFlags.MethodZlib,
            CompressionMethod.LZ4 => CompressionFlags.MethodLZ4,
            CompressionMethod.Zstd => CompressionFlags.MethodZstd,
            _ => throw new NotSupportedException($"Unsupported compression method: {method}")
        };
    }

    public static CompressionFlags ToFlags(this LSCompressionLevel level)
    {
        return level switch
        {
            LSCompressionLevel.Fast => CompressionFlags.FastCompress,
            LSCompressionLevel.Default => CompressionFlags.DefaultCompress,
            LSCompressionLevel.Max => CompressionFlags.MaxCompress,
            _ => throw new NotSupportedException($"Unsupported compression level: {level}")
        };
    }
}
