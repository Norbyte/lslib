using LZ4;
using System;
using System.IO;
using System.Runtime.InteropServices;
using LSLib.LS.Enums;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO.MemoryMappedFiles;

namespace LSLib.LS;


public class ReadOnlySubstream : Stream
{
    private readonly Stream SourceStream;
    private readonly long FileOffset;
    private readonly long Size;
    private long CurPosition = 0;

    public ReadOnlySubstream(Stream sourceStream, long offset, long size)
    {
        SourceStream = sourceStream;
        FileOffset = offset;
        Size = size;
    }

    public override bool CanRead { get { return true; } }
    public override bool CanSeek { get { return false; } }

    public override int Read(byte[] buffer, int offset, int count)
    {
        SourceStream.Seek(FileOffset + CurPosition, SeekOrigin.Begin);
        long readable = Size - CurPosition;
        int bytesToRead = (readable < count) ? (int)readable : count;
        var read = SourceStream.Read(buffer, offset, bytesToRead);
        CurPosition += read;
        return read;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        SourceStream.Seek(FileOffset + CurPosition, SeekOrigin.Begin);
        long readable = Size - CurPosition;
        int bytesToRead = (readable < count) ? (int)readable : count;
        CurPosition += bytesToRead;
        return SourceStream.ReadAsync(buffer, offset, bytesToRead, cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }


    public override long Position
    {
        get { return CurPosition; }
        set { throw new NotSupportedException(); }
    }

    public override bool CanTimeout { get { return SourceStream.CanTimeout; } }
    public override bool CanWrite { get { return false; } }
    public override long Length { get { return Size; } }
    public override void SetLength(long value) { throw new NotSupportedException(); }
    public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
    public override void Flush() { }
}


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
        LZ4Codec.Decode(compressed, 0, compressed.Length, decompressed, 0, DecompressedSize, true);
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

public static class BinUtils
{
    public static T ReadStruct<T>(BinaryReader reader)
    {
        T outStruct;
        int count = Marshal.SizeOf(typeof(T));
        byte[] readBuffer = reader.ReadBytes(count);
        GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
        outStruct = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        handle.Free();
        return outStruct;
    }

    public static void ReadStructs<T>(BinaryReader reader, T[] elements)
    {
        int elementSize = Marshal.SizeOf(typeof(T));
        int bytes = elementSize * elements.Length;
        byte[] readBuffer = reader.ReadBytes(bytes);
        GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
        var addr = handle.AddrOfPinnedObject();
        for (var i = 0; i < elements.Length; i++)
        {
            var elementAddr = new IntPtr(addr.ToInt64() + elementSize * i);
            elements[i] = Marshal.PtrToStructure<T>(elementAddr);
        }
        handle.Free();
    }

    public static void WriteStruct<T>(BinaryWriter writer, ref T inStruct)
    {
        int count = Marshal.SizeOf(typeof(T));
        byte[] writeBuffer = new byte[count];
        GCHandle handle = GCHandle.Alloc(writeBuffer, GCHandleType.Pinned);
        Marshal.StructureToPtr(inStruct, handle.AddrOfPinnedObject(), true);
        handle.Free();
        writer.Write(writeBuffer);
    }

    public static void WriteStructs<T>(BinaryWriter writer, T[] elements)
    {
        int elementSize = Marshal.SizeOf(typeof(T));
        int bytes = elementSize * elements.Length;
        byte[] writeBuffer = new byte[bytes];
        GCHandle handle = GCHandle.Alloc(writeBuffer, GCHandleType.Pinned);
        var addr = handle.AddrOfPinnedObject();
        for (var i = 0; i < elements.Length; i++)
        {
            var elementAddr = new IntPtr(addr.ToInt64() + elementSize * i);
            Marshal.StructureToPtr(elements[i], elementAddr, true);
        }
        handle.Free();
        writer.Write(writeBuffer);
    }

    public static String NullTerminatedBytesToString(byte[] b)
    {
        int len;
        for (len = 0; len < b.Length && b[len] != 0; len++) {}
        return Encoding.UTF8.GetString(b, 0, len);
    }

    public static byte[] StringToNullTerminatedBytes(string s, int length)
    {
        var b = new byte[length];
        int len = Encoding.UTF8.GetBytes(s, b);
        Array.Clear(b, len, b.Length - len);
        return b;
    }

    public static NodeAttribute ReadAttribute(NodeAttribute.DataType type, BinaryReader reader)
    {
        var attr = new NodeAttribute(type);
        switch (type)
        {
            case NodeAttribute.DataType.DT_None:
                break;

            case NodeAttribute.DataType.DT_Byte:
                attr.Value = reader.ReadByte();
                break;

            case NodeAttribute.DataType.DT_Short:
                attr.Value = reader.ReadInt16();
                break;

            case NodeAttribute.DataType.DT_UShort:
                attr.Value = reader.ReadUInt16();
                break;

            case NodeAttribute.DataType.DT_Int:
                attr.Value = reader.ReadInt32();
                break;

            case NodeAttribute.DataType.DT_UInt:
                attr.Value = reader.ReadUInt32();
                break;

            case NodeAttribute.DataType.DT_Float:
                attr.Value = reader.ReadSingle();
                break;

            case NodeAttribute.DataType.DT_Double:
                attr.Value = reader.ReadDouble();
                break;

            case NodeAttribute.DataType.DT_IVec2:
            case NodeAttribute.DataType.DT_IVec3:
            case NodeAttribute.DataType.DT_IVec4:
                {
                    int columns = attr.GetColumns();
                    var vec = new int[columns];
                    for (int i = 0; i < columns; i++)
                        vec[i] = reader.ReadInt32();
                    attr.Value = vec;
                    break;
                }

            case NodeAttribute.DataType.DT_Vec2:
            case NodeAttribute.DataType.DT_Vec3:
            case NodeAttribute.DataType.DT_Vec4:
                {
                    int columns = attr.GetColumns();
                    var vec = new float[columns];
                    for (int i = 0; i < columns; i++)
                        vec[i] = reader.ReadSingle();
                    attr.Value = vec;
                    break;
                }

            case NodeAttribute.DataType.DT_Mat2:
            case NodeAttribute.DataType.DT_Mat3:
            case NodeAttribute.DataType.DT_Mat3x4:
            case NodeAttribute.DataType.DT_Mat4x3:
            case NodeAttribute.DataType.DT_Mat4:
                {
                    int columns = attr.GetColumns();
                    int rows = attr.GetRows();
                    var mat = new Matrix(rows, columns);
                    attr.Value = mat;

                    for (int col = 0; col < columns; col++)
                    {
                        for (int row = 0; row < rows; row++)
                        {
                            mat[row, col] = reader.ReadSingle();
                        }
                    }
                    break;
                }

            case NodeAttribute.DataType.DT_Bool:
                attr.Value = reader.ReadByte() != 0;
                break;

            case NodeAttribute.DataType.DT_ULongLong:
                attr.Value = reader.ReadUInt64();
                break;

            case NodeAttribute.DataType.DT_Long:
            case NodeAttribute.DataType.DT_Int64:
                attr.Value = reader.ReadInt64();
                break;

            case NodeAttribute.DataType.DT_Int8:
                attr.Value = reader.ReadSByte();
                break;

            case NodeAttribute.DataType.DT_UUID:
                attr.Value = new Guid(reader.ReadBytes(16));
                break;

            default:
                // Strings are serialized differently for each file format and should be
                // handled by the format-specific ReadAttribute()
                throw new InvalidFormatException(String.Format("ReadAttribute() not implemented for type {0}", type));
        }

        return attr;
    }

    public static void WriteAttribute(BinaryWriter writer, NodeAttribute attr)
    {
        switch (attr.Type)
        {
            case NodeAttribute.DataType.DT_None:
                break;

            case NodeAttribute.DataType.DT_Byte:
                writer.Write((Byte)attr.Value);
                break;

            case NodeAttribute.DataType.DT_Short:
                writer.Write((Int16)attr.Value);
                break;

            case NodeAttribute.DataType.DT_UShort:
                writer.Write((UInt16)attr.Value);
                break;

            case NodeAttribute.DataType.DT_Int:
                writer.Write((Int32)attr.Value);
                break;

            case NodeAttribute.DataType.DT_UInt:
                writer.Write((UInt32)attr.Value);
                break;

            case NodeAttribute.DataType.DT_Float:
                writer.Write((float)attr.Value);
                break;

            case NodeAttribute.DataType.DT_Double:
                writer.Write((Double)attr.Value);
                break;

            case NodeAttribute.DataType.DT_IVec2:
            case NodeAttribute.DataType.DT_IVec3:
            case NodeAttribute.DataType.DT_IVec4:
                foreach (var item in (int[])attr.Value)
                {
                    writer.Write(item);
                }
                break;

            case NodeAttribute.DataType.DT_Vec2:
            case NodeAttribute.DataType.DT_Vec3:
            case NodeAttribute.DataType.DT_Vec4:
                foreach (var item in (float[])attr.Value)
                {
                    writer.Write(item);
                }
                break;

            case NodeAttribute.DataType.DT_Mat2:
            case NodeAttribute.DataType.DT_Mat3:
            case NodeAttribute.DataType.DT_Mat3x4:
            case NodeAttribute.DataType.DT_Mat4x3:
            case NodeAttribute.DataType.DT_Mat4:
                {
                    var mat = (Matrix)attr.Value;
                    for (int col = 0; col < mat.cols; col++)
                    {
                        for (int row = 0; row < mat.rows; row++)
                        {
                            writer.Write((float)mat[row, col]);
                        }
                    }
                    break;
                }

            case NodeAttribute.DataType.DT_Bool:
                writer.Write((Byte)((Boolean)attr.Value ? 1 : 0));
                break;

            case NodeAttribute.DataType.DT_ULongLong:
                writer.Write((UInt64)attr.Value);
                break;

            case NodeAttribute.DataType.DT_Long:
            case NodeAttribute.DataType.DT_Int64:
                writer.Write((Int64)attr.Value);
                break;

            case NodeAttribute.DataType.DT_Int8:
                writer.Write((SByte)attr.Value);
                break;

            case NodeAttribute.DataType.DT_UUID:
                writer.Write(((Guid)attr.Value).ToByteArray());
                break;

            default:
                throw new InvalidFormatException(String.Format("WriteAttribute() not implemented for type {0}", attr.Type));
        }
    }

    public static CompressionFlags MakeCompressionFlags(CompressionMethod method, LSCompressionLevel level)
    {
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
                    using (var compressedStream = new MemoryStream(compressed))
                    using (var decompressedStream = new MemoryStream())
                    using (var stream = new ZLibStream(compressedStream, CompressionMode.Decompress))
                    {
                        byte[] buf = new byte[0x10000];
                        int length = 0;
                        while ((length = stream.Read(buf, 0, buf.Length)) > 0)
                        {
                            decompressedStream.Write(buf, 0, length);
                        }

                        return decompressedStream.ToArray();
                    }
                }

            case CompressionMethod.LZ4:
                if (chunked)
                {
                    var decompressed = Native.LZ4FrameCompressor.Decompress(compressed);
                    return decompressed;
                }
                else
                {
                    var decompressed = new byte[decompressedSize];
                    var resultSize = LZ4Codec.Decode(compressed, 0, compressed.Length, decompressed, 0, decompressedSize, true);
                    if (resultSize != decompressedSize)
                    {
                        string msg = $"LZ4 compressor disagrees about the size of compressed buffer; expected {decompressedSize}, got {resultSize}";
                        throw new InvalidDataException(msg);
                    }
                    return decompressed;
                }

            default:
                throw new InvalidDataException($"No decompressor found for this format: {compression}");
        }
    }

    public static Stream Decompress(MemoryMappedFile file, MemoryMappedViewAccessor view, long sourceOffset, 
        int sourceSize, int decompressedSize, CompressionFlags compression)
    {
        switch (compression.Method())
        {
            case CompressionMethod.None:
                return file.CreateViewStream(sourceOffset, sourceSize, MemoryMappedFileAccess.Read);

            case CompressionMethod.Zlib:
                var sourceStream = file.CreateViewStream(sourceOffset, sourceSize, MemoryMappedFileAccess.Read);
                return new ZLibStream(sourceStream, CompressionMode.Decompress);

            case CompressionMethod.LZ4:
                return new LZ4DecompressionStream(view, sourceOffset, sourceSize, decompressedSize);

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
        if (chunked)
        {
            return Native.LZ4FrameCompressor.Compress(uncompressed);
        }
        else if (compressionLevel == LSCompressionLevel.Fast)
        {
            return LZ4Codec.Encode(uncompressed, 0, uncompressed.Length);
        }
        else
        {
            return LZ4Codec.EncodeHC(uncompressed, 0, uncompressed.Length);
        }
    }
}
