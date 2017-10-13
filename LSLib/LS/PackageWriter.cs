using zlib;
using LZ4;
using LSLib.Granny;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography;

namespace LSLib.LS
{
    public class PackageWriter : IDisposable
    {
        public delegate void WriteProgressDelegate(CommonPackageInfo commonPackage, long numerator, long denominator);
        public WriteProgressDelegate writeProgress = delegate { };
        public CompressionMethod Compression = CompressionMethod.None;
        public CompressionLevel CompressionLevel = CompressionLevel.DefaultCompression;
        public UInt32 Version = Package.CurrentVersion;

        private static long MaxPackageSize = 0x40000000;

        private Package package;
        private String path;
        private List<Stream> streams = new List<Stream>();

        public PackageWriter(Package package, string path)
        {
            this.package = package;
            this.path = path;
        }

        public void Dispose()
        {
            foreach (var stream in streams)
            {
                stream.Dispose();
            }
        }

        public int PaddingLength()
        {
            if (Version <= 9)
            {
                return 0x8000;
            }
            else
            {
                return 0x40;
            }
        }

        public PackagedFileInfo WriteFile(CommonPackageInfo info)
        {
            // Assume that all files are written uncompressed (worst-case) when calculating package sizes
            var size = info.Size();
            if (streams.Last().Position + size > MaxPackageSize)
            {
                // Start a new package file if the current one is full.
                var partPath = Package.MakePartFilename(path, streams.Count);
                var nextPart = new FileStream(partPath, FileMode.Create, FileAccess.Write);
                streams.Add(nextPart);
            }

            var stream = streams.Last();
            var packaged = new PackagedFileInfo();
            packaged.PackageStream = stream;
            packaged.Name = info.Name;
            packaged.UncompressedSize = size;
            packaged.ArchivePart = (UInt32)(streams.Count - 1);
            packaged.OffsetInFile = (UInt32)stream.Position;
            packaged.Flags = BinUtils.MakeCompressionFlags(Compression, CompressionLevel);

            var packagedStream = info.MakeStream();
            byte[] compressed;
            try
            {
                using (var reader = new BinaryReader(packagedStream, Encoding.UTF8, true))
                {
                    var uncompressed = reader.ReadBytes((int)reader.BaseStream.Length);
                    compressed = BinUtils.Compress(uncompressed, Compression, CompressionLevel);
                    stream.Write(compressed, 0, compressed.Length);
                }
            }
            finally
            {
                info.ReleaseStream();
            }

            packaged.SizeOnDisk = (UInt32)(stream.Position - packaged.OffsetInFile);
            packaged.Crc = Native.Crc32.Compute(compressed, 0);

            var padLength = PaddingLength();
            if (stream.Position % padLength > 0)
            {
                // Pad the file to a multiple of 64 bytes
                byte[] pad = new byte[padLength - (stream.Position % padLength)];
                for (int i = 0; i < pad.Length; i++)
                {
                    pad[i] = 0xAD;
                }

                stream.Write(pad, 0, pad.Length);
            }

            return packaged;
        }

        public void WriteV7(FileStream mainStream)
        {
            if (Compression == CompressionMethod.LZ4)
            {
                throw new ArgumentException("LZ4 compression is only supported by V10 and later package versions");
            }

            using (var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true))
            {
                var header = new LSPKHeader7();
                header.Version = Version;
                header.NumFiles = (UInt32)package.Files.Count;
                header.FileListSize = (UInt32)(Marshal.SizeOf(typeof(FileEntry7)) * package.Files.Count);
                header.DataOffset = (UInt32)Marshal.SizeOf(typeof(LSPKHeader7)) + header.FileListSize;
                var paddingLength = PaddingLength();
                if (header.DataOffset % paddingLength > 0)
                {
                    header.DataOffset += (UInt32)(paddingLength - (header.DataOffset % paddingLength));
                }

                // Write a placeholder instead of the actual headers; we'll write them after we 
                // compressed and flushed all files to disk
                var placeholder = new byte[header.DataOffset];
                writer.Write(placeholder);

                long totalSize = package.Files.Sum(p => (long)p.Size());
                long currentSize = 0;
                List<PackagedFileInfo> writtenFiles = new List<PackagedFileInfo>();
                foreach (var file in this.package.Files)
                {
                    writeProgress(file, currentSize, totalSize);
                    writtenFiles.Add(WriteFile(file));
                    currentSize += file.Size();
                }

                mainStream.Seek(0, SeekOrigin.Begin);
                header.LittleEndian = 0;
                header.NumParts = (UInt16)streams.Count;
                BinUtils.WriteStruct<LSPKHeader7>(writer, ref header);

                foreach (var file in writtenFiles)
                {
                    var entry = file.MakeEntryV7();
                    if (entry.ArchivePart == 0)
                    {
                        entry.OffsetInFile -= header.DataOffset;
                    }

                    BinUtils.WriteStruct<FileEntry7>(writer, ref entry);
                }
            }
        }

        public void WriteV10(FileStream mainStream)
        {
            using (var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true))
            {
                var header = new LSPKHeader10();
                header.Version = Version;
                header.NumFiles = (UInt32)package.Files.Count;
                header.FileListSize = (UInt32)(Marshal.SizeOf(typeof(FileEntry13)) * package.Files.Count);
                header.DataOffset = (UInt32)Marshal.SizeOf(typeof(LSPKHeader10)) + 4 + header.FileListSize;
                var paddingLength = PaddingLength();
                if (header.DataOffset % paddingLength > 0)
                {
                    header.DataOffset += (UInt32)(paddingLength - (header.DataOffset % paddingLength));
                }

                // Write a placeholder instead of the actual headers; we'll write them after we 
                // compressed and flushed all files to disk
                var placeholder = new byte[header.DataOffset];
                writer.Write(placeholder);

                long totalSize = package.Files.Sum(p => (long)p.Size());
                long currentSize = 0;
                List<PackagedFileInfo> writtenFiles = new List<PackagedFileInfo>();
                foreach (var file in this.package.Files)
                {
                    writeProgress(file, currentSize, totalSize);
                    writtenFiles.Add(WriteFile(file));
                    currentSize += file.Size();
                }

                mainStream.Seek(0, SeekOrigin.Begin);
                writer.Write(Package.Signature); 
                header.NumParts = (UInt16)streams.Count;
                header.SomePartVar = 0; // ???
                BinUtils.WriteStruct<LSPKHeader10>(writer, ref header);

                foreach (var file in writtenFiles)
                {
                    var entry = file.MakeEntryV13();
                    if (entry.ArchivePart == 0)
                    {
                        entry.OffsetInFile -= header.DataOffset;
                    }

                    // v10 packages don't support compression level in the flags field
                    entry.Flags &= 0x0f;
                    BinUtils.WriteStruct<FileEntry13>(writer, ref entry);
                }
            }
        }

        public void WriteV13(FileStream mainStream)
        {
            long totalSize = package.Files.Sum(p => (long)p.Size());
            long currentSize = 0;

            List<PackagedFileInfo> writtenFiles = new List<PackagedFileInfo>();
            foreach (var file in this.package.Files)
            {
                writeProgress(file, currentSize, totalSize);
                writtenFiles.Add(WriteFile(file));
                currentSize += file.Size();
            }

            using (var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true))
            {
                var header = new LSPKHeader13();
                header.Version = Version;
                header.FileListOffset = (UInt32)mainStream.Position;

                writer.Write((UInt32)writtenFiles.Count);

                var fileList = new MemoryStream();
                var fileListWriter = new BinaryWriter(fileList);
                foreach (var file in writtenFiles)
                {
                    var entry = file.MakeEntryV13();
                    BinUtils.WriteStruct<FileEntry13>(fileListWriter, ref entry);
                }

                var fileListBuf = fileList.ToArray();
                fileListWriter.Dispose();
                var compressedFileList = LZ4Codec.EncodeHC(fileListBuf, 0, fileListBuf.Length);

                writer.Write(compressedFileList);

                header.FileListSize = (UInt32)mainStream.Position - header.FileListOffset;
                header.NumParts = (UInt16)streams.Count;
                header.SomePartVar = 0; // ???
                header.Md5 = ComputeArchiveHash();
                BinUtils.WriteStruct<LSPKHeader13>(writer, ref header);

                writer.Write((UInt32)(8 + Marshal.SizeOf(typeof(LSPKHeader13))));
                writer.Write(Package.Signature);
            }
        }

        public byte[] ComputeArchiveHash()
        {
            // MD5 is computed over the contents of all files in an alphabetically sorted order
            var orderedFileList = this.package.Files.Select(item => item).ToList();
            orderedFileList.Sort(delegate (CommonPackageInfo a, CommonPackageInfo b)
            {
                return String.Compare(a.Name, b.Name);
            });

            using (var md5 = MD5.Create())
            {
                foreach (var file in orderedFileList)
                {
                    var packagedStream = file.MakeStream();
                    try
                    {
                        using (var reader = new BinaryReader(packagedStream))
                        {
                            var uncompressed = reader.ReadBytes((int)reader.BaseStream.Length);
                            md5.TransformBlock(uncompressed, 0, uncompressed.Length, uncompressed, 0);
                        }
                    }
                    finally
                    {
                        file.ReleaseStream();
                    }
                }

                md5.TransformFinalBlock(new byte[0], 0, 0);
                var hash = md5.Hash;

                // All hash bytes are incremented by 1
                for (var i = 0; i < hash.Length; i++) hash[i] += 1;

                return hash;
            }
        }

        public void Write()
        {
            var mainStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            streams.Add(mainStream);

            if (Version == 13)
            {
                WriteV13(mainStream);
            }
            else if (Version == 10)
            {
                WriteV10(mainStream);
            }
            else if (Version == 9 || Version == 7)
            {
                WriteV7(mainStream);
            }
            else
            {
                var msg = String.Format("Cannot write version {0} packages", Version);
                throw new ArgumentException(msg);
            }
        }
    }
}
