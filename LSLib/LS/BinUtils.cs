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
