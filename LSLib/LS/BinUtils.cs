using zlib;
using LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace LSLib.LS
{
    public enum CompressionMethod
    {
        None = 0,
        Zlib = 1,
        LZ4 = 2
    };

    public enum CompressionFlags
    {
        FastCompress = 0x10,
        DefaultCompress = 0x20,
        MaxCompressionLevel = 0x40
    };

    static class BinUtils
    {
        public static T ReadStruct<T>(BinaryReader reader)
        {
            T outStruct;
            int count = Marshal.SizeOf(typeof(T));
            byte[] readBuffer = new byte[count];
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            outStruct = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return outStruct;
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
                    attr.Value = reader.ReadInt64();
                    break;

                case NodeAttribute.DataType.DT_Int8:
                    attr.Value = reader.ReadSByte();
                    break;

                default:
                    // Strings are serialized differently for each file format and should be
                    // handled by the format-specific ReadAttribute()
                    throw new InvalidFormatException(String.Format("ReadAttribute() not implemented for type {0}", type));
            }

            return attr;
        }

        public static byte[] Decompress(byte[] compressed, int decompressedSize, byte compressionFlags)
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
                    var decompressed = new byte[decompressedSize];
                    var uncompressedSize = LZ4Codec.Decode(compressed, 0, compressed.Length, decompressed, 0, decompressedSize, true);
                    if (uncompressedSize != decompressedSize)
                    {
                        var msg = String.Format("LZ4 compressor disagrees about the size of a compressed file; expected {0}, got {1}", decompressedSize, uncompressedSize);
                        throw new InvalidDataException(msg);
                    }

                    return decompressed;

                default:
                    {
                        var msg = String.Format("No decompressor found for this format: {0}", compressionFlags);
                        throw new InvalidDataException(msg);
                    }
            }
        }
    }
}
