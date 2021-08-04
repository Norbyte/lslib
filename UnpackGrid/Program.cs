using LSLib.LS;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace UnpackGrid
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AIGridHeader
    {
        public UInt32 Version;
        public Int32 Width;
        public Int32 Height;
        public Single OffsetX;
        public Single OffsetY;
        public Single OffsetZ;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AIGridCompressionHeader
    {
        public Int32 UncompressedSize;
        public Int32 CompressedSize;
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: UnpackGrid <source path> <destination path>");
                return;
            }

            UnpackAiGrid(args[0], args[1]);
        }

        private static void UnpackAiGrid(string sourcePath, string destinationPath)
        {
            using (var fs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(fs))
            {
                var header = BinUtils.ReadStruct<AIGridHeader>(reader);
                if (header.Version != 4)
                    throw new InvalidFormatException(String.Format("Can only decompress version 4 AI grid files; this file is v{0}", header.Version));

                var compHeader = BinUtils.ReadStruct<AIGridCompressionHeader>(reader);

                if (fs.Length != compHeader.CompressedSize + fs.Position)
                    throw new InvalidFormatException(String.Format("Invalid AI grid file size; expected {0}, got {1}", compHeader.CompressedSize + fs.Position, fs.Length));

                var compressedBlob = reader.ReadBytes(compHeader.CompressedSize);
                var uncompressed = BinUtils.Decompress(compressedBlob, compHeader.UncompressedSize, 0x21);
                var uncompressed2 = BinUtils.Decompress(uncompressed, 16 * header.Width * header.Height, 0x42);

                header.Version = 2;
                using (var unpackedFs = new FileStream(destinationPath, FileMode.Create))
                using (var writer = new BinaryWriter(unpackedFs))
                {
                    BinUtils.WriteStruct<AIGridHeader>(writer, ref header);
                    writer.Write(uncompressed2);
                }
            }

            Console.WriteLine($"Wrote resource to: {destinationPath}");
        }
    }
}
