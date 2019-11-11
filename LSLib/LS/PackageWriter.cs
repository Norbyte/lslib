using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using LSLib.LS.Enums;
using LSLib.Native;
using LZ4;
using Alphaleonis.Win32.Filesystem;
using File = Alphaleonis.Win32.Filesystem.File;

namespace LSLib.LS
{
    public class PackageWriter : IDisposable
    {
        public delegate void WriteProgressDelegate(AbstractFileInfo abstractFile, long numerator, long denominator);

        private const long MaxPackageSize = 0x40000000;
        public CompressionMethod Compression = CompressionMethod.None;
        public CompressionLevel CompressionLevel = CompressionLevel.DefaultCompression;

        private readonly Package _package;
        private readonly String _path;
        private readonly List<Stream> _streams = new List<Stream>();
        public PackageVersion Version = Package.CurrentVersion;
        public WriteProgressDelegate WriteProgress = delegate { };

        public PackageWriter(Package package, string path)
        {
            this._package = package;
            this._path = path;
        }

        public void Dispose()
        {
            foreach (Stream stream in _streams)
            {
                stream.Dispose();
            }
        }

        public int PaddingLength() => Version <= PackageVersion.V9 ? 0x8000 : 0x40;

        public PackagedFileInfo WriteFile(AbstractFileInfo info)
        {
            // Assume that all files are written uncompressed (worst-case) when calculating package sizes
            uint size = info.Size();
            if (_streams.Last().Position + size > MaxPackageSize)
            {
                // Start a new package file if the current one is full.
                string partPath = Package.MakePartFilename(_path, _streams.Count);
                var nextPart = File.Open(partPath, FileMode.Create, FileAccess.Write);
                _streams.Add(nextPart);
            }

            Stream stream = _streams.Last();
            var packaged = new PackagedFileInfo
            {
                PackageStream = stream,
                Name = info.Name,
                UncompressedSize = size,
                ArchivePart = (UInt32) (_streams.Count - 1),
                OffsetInFile = (UInt32) stream.Position,
                Flags = BinUtils.MakeCompressionFlags(Compression, CompressionLevel)
            };

            Stream packagedStream = info.MakeStream();
            byte[] compressed;
            try
            {
                using (var reader = new BinaryReader(packagedStream, Encoding.UTF8, true))
                {
                    byte[] uncompressed = reader.ReadBytes((int) reader.BaseStream.Length);
                    compressed = BinUtils.Compress(uncompressed, Compression, CompressionLevel);
                    stream.Write(compressed, 0, compressed.Length);
                }
            }
            finally
            {
                info.ReleaseStream();
            }

            packaged.SizeOnDisk = (UInt32) (stream.Position - packaged.OffsetInFile);
            packaged.Crc = Crc32.Compute(compressed, 0);

            int padLength = PaddingLength();
            if (stream.Position % padLength <= 0)
            {
                return packaged;
            }

            if ((_package.Metadata.Flags & PackageFlags.Solid) == 0)
            {
                // Pad the file to a multiple of 64 bytes
                var pad = new byte[padLength - stream.Position % padLength];
                for (var i = 0; i < pad.Length; i++)
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
                var header = new LSPKHeader7
                {
                    Version = (uint) Version,
                    NumFiles = (UInt32) _package.Files.Count,
                    FileListSize = (UInt32) (Marshal.SizeOf(typeof(FileEntry7)) * _package.Files.Count)
                };
                header.DataOffset = (UInt32) Marshal.SizeOf(typeof(LSPKHeader7)) + header.FileListSize;
                int paddingLength = PaddingLength();
                if (header.DataOffset % paddingLength > 0)
                {
                    header.DataOffset += (UInt32) (paddingLength - header.DataOffset % paddingLength);
                }

                // Write a placeholder instead of the actual headers; we'll write them after we
                // compressed and flushed all files to disk
                var placeholder = new byte[header.DataOffset];
                writer.Write(placeholder);

                long totalSize = _package.Files.Sum(p => (long) p.Size());
                long currentSize = 0;
                var writtenFiles = new List<PackagedFileInfo>();
                foreach (AbstractFileInfo file in _package.Files)
                {
                    WriteProgress(file, currentSize, totalSize);
                    writtenFiles.Add(WriteFile(file));
                    currentSize += file.Size();
                }

                mainStream.Seek(0, SeekOrigin.Begin);
                header.LittleEndian = 0;
                header.NumParts = (UInt16) _streams.Count;
                BinUtils.WriteStruct(writer, ref header);

                foreach (PackagedFileInfo file in writtenFiles)
                {
                    FileEntry7 entry = file.MakeEntryV7();
                    if (entry.ArchivePart == 0)
                    {
                        entry.OffsetInFile -= header.DataOffset;
                    }

                    BinUtils.WriteStruct(writer, ref entry);
                }
            }
        }

        public void WriteV10(FileStream mainStream)
        {
            using (var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true))
            {
                var header = new LSPKHeader10
                {
                    Version = (uint) Version,
                    NumFiles = (UInt32) _package.Files.Count,
                    FileListSize = (UInt32) (Marshal.SizeOf(typeof(FileEntry13)) * _package.Files.Count)
                };
                header.DataOffset = (UInt32) Marshal.SizeOf(typeof(LSPKHeader10)) + 4 + header.FileListSize;
                int paddingLength = PaddingLength();
                if (header.DataOffset % paddingLength > 0)
                {
                    header.DataOffset += (UInt32) (paddingLength - header.DataOffset % paddingLength);
                }

                // Write a placeholder instead of the actual headers; we'll write them after we
                // compressed and flushed all files to disk
                var placeholder = new byte[header.DataOffset];
                writer.Write(placeholder);

                long totalSize = _package.Files.Sum(p => (long) p.Size());
                long currentSize = 0;
                var writtenFiles = new List<PackagedFileInfo>();
                foreach (AbstractFileInfo file in _package.Files)
                {
                    WriteProgress(file, currentSize, totalSize);
                    writtenFiles.Add(WriteFile(file));
                    currentSize += file.Size();
                }

                mainStream.Seek(0, SeekOrigin.Begin);
                writer.Write(Package.Signature);
                header.NumParts = (UInt16) _streams.Count;
                header.Priority = _package.Metadata.Priority;
                header.Flags = (byte)_package.Metadata.Flags;
                BinUtils.WriteStruct(writer, ref header);

                foreach (PackagedFileInfo file in writtenFiles)
                {
                    FileEntry13 entry = file.MakeEntryV13();
                    if (entry.ArchivePart == 0)
                    {
                        entry.OffsetInFile -= header.DataOffset;
                    }

                    // v10 packages don't support compression level in the flags field
                    entry.Flags &= 0x0f;
                    BinUtils.WriteStruct(writer, ref entry);
                }
            }
        }

        public void WriteV13(FileStream mainStream)
        {
            long totalSize = _package.Files.Sum(p => (long) p.Size());
            long currentSize = 0;

            var writtenFiles = new List<PackagedFileInfo>();
            foreach (AbstractFileInfo file in _package.Files)
            {
                WriteProgress(file, currentSize, totalSize);
                writtenFiles.Add(WriteFile(file));
                currentSize += file.Size();
            }

            using (var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true))
            {
                var header = new LSPKHeader13
                {
                    Version = (uint) Version,
                    FileListOffset = (UInt32) mainStream.Position
                };

                writer.Write((UInt32) writtenFiles.Count);

                var fileList = new MemoryStream();
                var fileListWriter = new BinaryWriter(fileList);
                foreach (PackagedFileInfo file in writtenFiles)
                {
                    FileEntry13 entry = file.MakeEntryV13();
                    BinUtils.WriteStruct(fileListWriter, ref entry);
                }

                byte[] fileListBuf = fileList.ToArray();
                fileListWriter.Dispose();
                byte[] compressedFileList = LZ4Codec.EncodeHC(fileListBuf, 0, fileListBuf.Length);

                writer.Write(compressedFileList);

                header.FileListSize = (UInt32) mainStream.Position - header.FileListOffset;
                header.NumParts = (UInt16) _streams.Count;
                header.Priority = _package.Metadata.Priority;
                header.Flags = (byte)_package.Metadata.Flags;
                header.Md5 = ComputeArchiveHash();
                BinUtils.WriteStruct(writer, ref header);

                writer.Write((UInt32) (8 + Marshal.SizeOf(typeof(LSPKHeader13))));
                writer.Write(Package.Signature);
            }
        }

        public byte[] ComputeArchiveHash()
        {
            // MD5 is computed over the contents of all files in an alphabetically sorted order
            List<AbstractFileInfo> orderedFileList = _package.Files.Select(item => item).ToList();
            orderedFileList.Sort((a, b) => String.CompareOrdinal(a.Name, b.Name));

            using (MD5 md5 = MD5.Create())
            {
                foreach (AbstractFileInfo file in orderedFileList)
                {
                    Stream packagedStream = file.MakeStream();
                    try
                    {
                        using (var reader = new BinaryReader(packagedStream))
                        {
                            byte[] uncompressed = reader.ReadBytes((int) reader.BaseStream.Length);
                            md5.TransformBlock(uncompressed, 0, uncompressed.Length, uncompressed, 0);
                        }
                    }
                    finally
                    {
                        file.ReleaseStream();
                    }
                }

                md5.TransformFinalBlock(new byte[0], 0, 0);
                byte[] hash = md5.Hash;

                // All hash bytes are incremented by 1
                for (var i = 0; i < hash.Length; i++)
                {
                    hash[i] += 1;
                }

                return hash;
            }
        }

        public void Write()
        {
            var mainStream = File.Open(_path, FileMode.Create, FileAccess.Write);
            _streams.Add(mainStream);

            switch (Version)
            {
                case PackageVersion.V13:
                {
                    WriteV13(mainStream);
                    break;
                }
                case PackageVersion.V10:
                {
                    WriteV10(mainStream);
                    break;
                }
                case PackageVersion.V9:
                case PackageVersion.V7:
                {
                    WriteV7(mainStream);
                    break;
                }
                default:
                {
                    throw new ArgumentException($"Cannot write version {Version} packages");
                }
            }
        }
    }
}
