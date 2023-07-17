using zlib;
using LZ4;
using System;
using System.IO;
using System.Runtime.InteropServices;
using LSLib.LS.Enums;

namespace LSLib.LS
{
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

        public static CompressionMethod CompressionFlagsToMethod(byte flags)
        {
            switch (flags & 0x0f)
            {
                case (int)CompressionMethod.None:
                    return CompressionMethod.None;

                case (int)CompressionMethod.Zlib:
                    return CompressionMethod.Zlib;

                case (int)CompressionMethod.LZ4:
                    return CompressionMethod.LZ4;

                default:
                    throw new ArgumentException("Invalid compression method");
            }
        }

        public static CompressionLevel CompressionFlagsToLevel(byte flags)
        {
            switch (flags & 0xf0)
            {
                case (int)CompressionFlags.FastCompress:
                    return CompressionLevel.FastCompression;

                case (int)CompressionFlags.DefaultCompress:
                    return CompressionLevel.DefaultCompression;

                case (int)CompressionFlags.MaxCompressionLevel:
                    return CompressionLevel.MaxCompression;

                default:
                    throw new ArgumentException("Invalid compression flags");
            }
        }

        public static byte MakeCompressionFlags(CompressionMethod method, CompressionLevel level)
        {
            if (method == CompressionMethod.None)
            {
                return 0;
            }

            byte flags = 0;
            if (method == CompressionMethod.Zlib)
                flags = 0x1;
            else if (method == CompressionMethod.LZ4)
                flags = 0x2;

            if (level == CompressionLevel.FastCompression)
                flags |= 0x10;
            else if (level == CompressionLevel.DefaultCompression)
                flags |= 0x20;
            else if (level == CompressionLevel.MaxCompression)
                flags |= 0x40;

            return flags;
        }

        public static byte[] Decompress(byte[] compressed, int decompressedSize, byte compressionFlags, bool chunked = false)
        {
            switch ((CompressionMethod)(compressionFlags & 0x0F))
            {
                case CompressionMethod.None:
                    return compressed;

                case CompressionMethod.Zlib:
                    {
                        using (var compressedStream = new MemoryStream(compressed))
                        using (var decompressedStream = new MemoryStream())
                        using (var stream = new ZInputStream(compressedStream))
                        {
                            byte[] buf = new byte[0x10000];
                            int length = 0;
                            while ((length = stream.read(buf, 0, buf.Length)) > 0)
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
                        LZ4Codec.Decode(compressed, 0, compressed.Length, decompressed, 0, decompressedSize, true);
                        return decompressed;
                    }

                default:
                    {
                        var msg = String.Format("No decompressor found for this format: {0}", compressionFlags);
                        throw new InvalidDataException(msg);
                    }
            }
        }

        public static byte[] Compress(byte[] uncompressed, byte compressionFlags)
        {
            return Compress(uncompressed, (CompressionMethod)(compressionFlags & 0x0F), CompressionFlagsToLevel(compressionFlags));
        }

        public static byte[] Compress(byte[] uncompressed, CompressionMethod method, CompressionLevel compressionLevel, bool chunked = false)
        {
            switch (method)
            {
                case CompressionMethod.None:
                    return uncompressed;

                case CompressionMethod.Zlib:
                    return CompressZlib(uncompressed, compressionLevel);

                case CompressionMethod.LZ4:
                    return CompressLZ4(uncompressed, compressionLevel, chunked);

                default:
                    throw new ArgumentException("Invalid compression method specified");
            }
        }

        public static byte[] CompressZlib(byte[] uncompressed, CompressionLevel compressionLevel)
        {
            int level = zlib.zlibConst.Z_DEFAULT_COMPRESSION;
            switch (compressionLevel)
            {
                case CompressionLevel.FastCompression:
                    level = zlib.zlibConst.Z_BEST_SPEED;
                    break;

                case CompressionLevel.DefaultCompression:
                    level = zlib.zlibConst.Z_DEFAULT_COMPRESSION;
                    break;

                case CompressionLevel.MaxCompression:
                    level = zlib.zlibConst.Z_BEST_COMPRESSION;
                    break;
            }

            using (var outputStream = new MemoryStream())
            using (var compressor = new ZOutputStream(outputStream, level))
            {
                compressor.Write(uncompressed, 0, uncompressed.Length);
                compressor.finish();
                return outputStream.ToArray();
            }
        }

        public static byte[] CompressLZ4(byte[] uncompressed, CompressionLevel compressionLevel, bool chunked = false)
        {
            if (chunked)
            {
                return Native.LZ4FrameCompressor.Compress(uncompressed);
            }
            else if (compressionLevel == CompressionLevel.FastCompression)
            {
                return LZ4Codec.Encode(uncompressed, 0, uncompressed.Length);
            }
            else
            {
                return LZ4Codec.EncodeHC(uncompressed, 0, uncompressed.Length);
            }
        }
    }
}
