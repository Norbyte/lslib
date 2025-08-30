using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using SharpGLTF.Runtime;
using System.Buffers;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;

namespace LSLib.LS;


public class LZ4DecompressionStream : Stream
{
    private readonly MemoryMappedViewAccessor View;
    private readonly long Offset;
    private readonly int Size;
    private readonly int DecompressedSize;
    private MemoryStream Decompressed;

    public LZ4DecompressionStream(MemoryMappedViewAccessor view, long offset, int size, int decompressedSize)
    {
        View = view;
        Offset = offset;
        Size = size;
        DecompressedSize = decompressedSize;
    }

    private void DoDecompression()
    {
        var compressed = new byte[Size];
        View.ReadArray(Offset, compressed, 0, Size);

        var decompressed = new byte[DecompressedSize];
        int length = LZ4Codec.Decode(compressed, 0, compressed.Length, decompressed, 0, DecompressedSize);
        if (length != DecompressedSize)
        {
            throw new Exception("Failed to decompress LZ4 stream");
        }
        Decompressed = new MemoryStream(decompressed);
    }

    public override bool CanRead { get { return true; } }
    public override bool CanSeek { get { return false; } }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (Decompressed == null)
        {
            DoDecompression();
        }

        return Decompressed.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }


    public override long Position
    {
        get { return Decompressed?.Position ?? 0; }
        set { throw new NotSupportedException(); }
    }

    public override bool CanTimeout { get { return false; } }
    public override bool CanWrite { get { return false; } }
    public override long Length { get { return DecompressedSize; } }
    public override void SetLength(long value) { throw new NotSupportedException(); }
    public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
    public override void Flush() { }
}

public static class CompressionHelpers
{
    public static CompressionFlags MakeCompressionFlags(CompressionMethod method, LSCompressionLevel level)
    {
        // Avoid setting compression level bits if there is no compression
        if (method == CompressionMethod.None) return 0;

        return method.ToFlags() | level.ToFlags();
    }

    public static byte[] Decompress(byte[] compressed, int decompressedSize, CompressionFlags compression, bool chunked = false)
    {
        switch (compression.Method())
        {
            case CompressionMethod.None:
                return compressed;

            case CompressionMethod.Zlib:
                {
                    using var compressedStream = new MemoryStream(compressed);
                    using var decompressedStream = new MemoryStream();
                    using var stream = new ZLibStream(compressedStream, CompressionMode.Decompress);
                    stream.CopyTo(decompressedStream);
                    return decompressedStream.ToArray();
                }

            case CompressionMethod.LZ4:
                if (chunked)
                {
                    using var input = new MemoryStream(compressed);
                    using var output = new MemoryStream();
                    using var decompressor = LZ4Stream.Decode(input);
                    var temp = ArrayPool<byte>.Shared.Rent(0x10000);

                    try
                    {
                        while (decompressedSize > 0)
                        {
                            int count = decompressor.Read(temp, 0, Math.Min(decompressedSize, temp.Length));
                            output.Write(temp, 0, count);
                            decompressedSize -= count;
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(temp);
                    }
                    return output.ToArray();
                }
                else
                {
                    var decompressed = new byte[decompressedSize];
                    var resultSize = LZ4Codec.Decode(compressed, 0, compressed.Length, decompressed, 0, decompressedSize);
                    if (resultSize != decompressedSize)
                    {
                        string msg = $"LZ4 compressor disagrees about the size of compressed buffer; expected {decompressedSize}, got {resultSize}";
                        throw new InvalidDataException(msg);
                    }
                    return decompressed;
                }

            case CompressionMethod.Zstd:
                {
                    using var compressedStream = new MemoryStream(compressed);
                    using var decompressedStream = new MemoryStream();
                    using var stream = new ZstdSharp.DecompressionStream(compressedStream);
                    stream.CopyTo(decompressedStream);
                    return decompressedStream.ToArray();
                }

            default:
                throw new InvalidDataException($"No decompressor found for this format: {compression}");
        }
    }

    public static Stream Decompress(MemoryMappedFile file, MemoryMappedViewAccessor view, long sourceOffset,
        int sourceSize, int decompressedSize, CompressionFlags compression)
    {
        // MemoryMappedView considers a size of 0 to mean "entire stream"
        if (sourceSize == 0)
        {
            return new MemoryStream();
        }

        switch (compression.Method())
        {
            case CompressionMethod.None:
                return file.CreateViewStream(sourceOffset, sourceSize, MemoryMappedFileAccess.Read);

            case CompressionMethod.Zlib:
                var sourceStream = file.CreateViewStream(sourceOffset, sourceSize, MemoryMappedFileAccess.Read);
                return new ZLibStream(sourceStream, CompressionMode.Decompress);

            case CompressionMethod.LZ4:
                return new LZ4DecompressionStream(view, sourceOffset, sourceSize, decompressedSize);

            case CompressionMethod.Zstd:
                var zstdStream = file.CreateViewStream(sourceOffset, sourceSize, MemoryMappedFileAccess.Read);
                return new ZstdSharp.DecompressionStream(zstdStream);

            default:
                throw new InvalidDataException($"No decompressor found for this format: {compression}");
        }
    }

    public static byte[] Compress(byte[] uncompressed, CompressionFlags compression)
    {
        return Compress(uncompressed, compression.Method(), compression.Level());
    }

    public static byte[] Compress(byte[] uncompressed, CompressionMethod method, LSCompressionLevel level, bool chunked = false)
    {
        return method switch
        {
            CompressionMethod.None => uncompressed,
            CompressionMethod.Zlib => CompressZlib(uncompressed, level),
            CompressionMethod.LZ4 => CompressLZ4(uncompressed, level, chunked),
            CompressionMethod.Zstd => CompressZstd(uncompressed, level),
            _ => throw new ArgumentException("Invalid compression method specified")
        };
    }

    public static byte[] CompressZlib(byte[] uncompressed, LSCompressionLevel level)
    {
        var zLevel = level switch
        {
            LSCompressionLevel.Fast => CompressionLevel.Fastest,
            LSCompressionLevel.Default => CompressionLevel.Optimal,
            LSCompressionLevel.Max => CompressionLevel.SmallestSize,
            _ => throw new ArgumentException()
        };

        using var outputStream = new MemoryStream();
        using (var compressor = new ZLibStream(outputStream, zLevel, true))
        {
            compressor.Write(uncompressed, 0, uncompressed.Length);
        }


        return outputStream.ToArray();
    }

    public static byte[] CompressLZ4(byte[] uncompressed, LSCompressionLevel compressionLevel, bool chunked = false)
    {
        var level = compressionLevel switch
        {
            LSCompressionLevel.Fast => LZ4Level.L00_FAST,
            LSCompressionLevel.Default => LZ4Level.L10_OPT,
            LSCompressionLevel.Max => LZ4Level.L12_MAX,
            _ => throw new ArgumentException("compressionLevel")
        };

        if (chunked)
        {
            var settings = new LZ4EncoderSettings
            {
                CompressionLevel = level
            };

            using var input = new MemoryStream(uncompressed);
            using var output = new MemoryStream(uncompressed);
            using var compressor = LZ4Stream.Encode(input, settings);
            compressor.CopyTo(output);
            return output.ToArray();
        }
        else 
        {
            var compressed = new byte[LZ4Codec.MaximumOutputSize(uncompressed.Length)];
            var length = LZ4Codec.Encode(uncompressed, compressed, level);
            if (length < 0)
            {
                throw new Exception($"LZ4 compression failed: {length}");
            }

            var final = new byte[length];
            Array.Copy(compressed, final, length);
            return final;
        }
    }

    public static byte[] CompressZstd(byte[] uncompressed, LSCompressionLevel level)
    {
        var zLevel = level switch
        {
            LSCompressionLevel.Fast => 3,
            LSCompressionLevel.Default => 9,
            LSCompressionLevel.Max => 22,
            _ => throw new ArgumentException()
        };

        using var outputStream = new MemoryStream();
        using (var compressor = new ZstdSharp.CompressionStream(outputStream, zLevel, 0, true))
        {
            compressor.Write(uncompressed, 0, uncompressed.Length);
        }

        return outputStream.ToArray();
    }
}
