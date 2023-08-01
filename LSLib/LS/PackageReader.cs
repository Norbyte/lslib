using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using LZ4;
using File = Alphaleonis.Win32.Filesystem.File;
using LSLib.LS.Enums;

namespace LSLib.LS
{
    public class NotAPackageException : Exception
    {
        public NotAPackageException()
        {
        }

        public NotAPackageException(string message) : base(message)
        {
        }

        public NotAPackageException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class PackageReader : IDisposable
    {
        private readonly String _path;
        private readonly bool _metadataOnly;
        private Stream[] _streams;

        public PackageReader(string path, bool metadataOnly = false)
        {
            this._path = path;
            this._metadataOnly = metadataOnly;
        }

        public void Dispose()
        {
            if (_streams != null)
            {
                foreach (Stream stream in _streams)
                {
                    stream.Dispose();
                }
            }
        }

        private void OpenStreams(FileStream mainStream, int numParts)
        {
            // Open a stream for each file chunk
            _streams = new Stream[numParts];
            _streams[0] = mainStream;

            for (var part = 1; part < numParts; part++)
            {
                string partPath = Package.MakePartFilename(_path, part);
                _streams[part] = File.Open(partPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
        }

        private Package ReadPackageV7(FileStream mainStream, BinaryReader reader)
        {
            var package = new Package();
            mainStream.Seek(0, SeekOrigin.Begin);
            var header = BinUtils.ReadStruct<LSPKHeader7>(reader);

            package.Metadata.Flags = 0;
            package.Metadata.Priority = 0;
            package.Version = PackageVersion.V7;

            if (_metadataOnly) return package;

            OpenStreams(mainStream, (int) header.NumParts);
            for (uint i = 0; i < header.NumFiles; i++)
            {
                var entry = BinUtils.ReadStruct<FileEntry7>(reader);
                if (entry.ArchivePart == 0)
                {
                    entry.OffsetInFile += header.DataOffset;
                }
                package.Files.Add(PackagedFileInfo.CreateFromEntry(entry, _streams[entry.ArchivePart]));
            }

            return package;
        }

        private Package ReadPackageV10(FileStream mainStream, BinaryReader reader)
        {
            var package = new Package();
            mainStream.Seek(4, SeekOrigin.Begin);
            var header = BinUtils.ReadStruct<LSPKHeader10>(reader);

            package.Metadata.Flags = (PackageFlags)header.Flags;
            package.Metadata.Priority = header.Priority;
            package.Version = PackageVersion.V10;

            if (_metadataOnly) return package;

            OpenStreams(mainStream, header.NumParts);
            for (uint i = 0; i < header.NumFiles; i++)
            {
                var entry = BinUtils.ReadStruct<FileEntry13>(reader);
                if (entry.ArchivePart == 0)
                {
                    entry.OffsetInFile += header.DataOffset;
                }

                // Add missing compression level flags
                entry.Flags = (entry.Flags & 0x0f) | 0x20;
                package.Files.Add(PackagedFileInfo.CreateFromEntry(entry, _streams[entry.ArchivePart]));
            }

            return package;
        }

        private Package ReadPackageV13(FileStream mainStream, BinaryReader reader)
        {
            var package = new Package();
            var header = BinUtils.ReadStruct<LSPKHeader13>(reader);

            if (header.Version != (ulong) PackageVersion.V13)
            {
                string msg = $"Unsupported package version {header.Version}; this package layout is only supported for {PackageVersion.V13}";
                throw new InvalidDataException(msg);
            }

            package.Metadata.Flags = (PackageFlags)header.Flags;
            package.Metadata.Priority = header.Priority;
            package.Version = PackageVersion.V13;

            if (_metadataOnly) return package;

            OpenStreams(mainStream, header.NumParts);
            mainStream.Seek(header.FileListOffset, SeekOrigin.Begin);
            int numFiles = reader.ReadInt32();
            int fileBufferSize = Marshal.SizeOf(typeof(FileEntry13)) * numFiles;
            byte[] compressedFileList = reader.ReadBytes((int) header.FileListSize - 4);

            var uncompressedList = new byte[fileBufferSize];
            int uncompressedSize = LZ4Codec.Decode(compressedFileList, 0, compressedFileList.Length, uncompressedList, 0, fileBufferSize, true);
            if (uncompressedSize != fileBufferSize)
            {
                string msg = $"LZ4 compressor disagrees about the size of file headers; expected {fileBufferSize}, got {uncompressedSize}";
                throw new InvalidDataException(msg);
            }

            var ms = new MemoryStream(uncompressedList);
            var msr = new BinaryReader(ms);

            var entries = new FileEntry13[numFiles];
            BinUtils.ReadStructs(msr, entries);

            if ((package.Metadata.Flags & PackageFlags.Solid) == PackageFlags.Solid && numFiles > 0)
            {
                // Calculate compressed frame offset and bounds
                uint totalUncompressedSize = 0;
                uint totalSizeOnDisk = 0;
                uint firstOffset = 0xffffffff;
                uint lastOffset = 0;

                foreach (var entry in entries)
                {
                    totalUncompressedSize += entry.UncompressedSize;
                    totalSizeOnDisk += entry.SizeOnDisk;
                    if (entry.OffsetInFile < firstOffset)
                    {
                        firstOffset = entry.OffsetInFile;
                    }
                    if (entry.OffsetInFile + entry.SizeOnDisk > lastOffset)
                    {
                        lastOffset = entry.OffsetInFile + entry.SizeOnDisk;
                    }
                }

                if (firstOffset != 7 || lastOffset - firstOffset != totalSizeOnDisk)
                {
                    string msg = $"Incorrectly compressed solid archive; offsets {firstOffset}/{lastOffset}, bytes {totalSizeOnDisk}";
                    throw new InvalidDataException(msg);
                }

                // Decompress all files as a single frame (solid)
                byte[] frame = new byte[lastOffset];
                mainStream.Seek(0, SeekOrigin.Begin);
                mainStream.Read(frame, 0, (int)lastOffset);

                byte[] decompressed = Native.LZ4FrameCompressor.Decompress(frame);
                var decompressedStream = new MemoryStream(decompressed);

                // Update offsets to point to the decompressed chunk
                uint offset = 7;
                uint compressedOffset = 0;
                foreach (var entry in entries)
                {
                    if (entry.OffsetInFile != offset)
                    {
                        throw new InvalidDataException("File list in solid archive not contiguous");
                    }

                    var file = PackagedFileInfo.CreateSolidFromEntry(entry, _streams[entry.ArchivePart], compressedOffset, decompressedStream);
                    package.Files.Add(file);

                    offset += entry.SizeOnDisk;
                    compressedOffset += entry.UncompressedSize;
                }
            }
            else
            {
                foreach (var entry in entries)
                {
                    package.Files.Add(PackagedFileInfo.CreateFromEntry(entry, _streams[entry.ArchivePart]));
                }
            }

            return package;
        }

        private void ReadFileListV15(BinaryReader reader, Package package)
        {
            int numFiles = reader.ReadInt32();
            int compressedSize = reader.ReadInt32();
            byte[] compressedFileList = reader.ReadBytes(compressedSize);

            int fileBufferSize = Marshal.SizeOf(typeof(FileEntry15)) * numFiles;
            var uncompressedList = new byte[fileBufferSize];
            int uncompressedSize = LZ4Codec.Decode(compressedFileList, 0, compressedFileList.Length, uncompressedList, 0, fileBufferSize, true);
            if (uncompressedSize != fileBufferSize)
            {
                string msg = $"LZ4 compressor disagrees about the size of file headers; expected {fileBufferSize}, got {uncompressedSize}";
                throw new InvalidDataException(msg);
            }

            var ms = new MemoryStream(uncompressedList);
            var msr = new BinaryReader(ms);

            var entries = new FileEntry15[numFiles];
            BinUtils.ReadStructs(msr, entries);

            foreach (var entry in entries)
            {
                package.Files.Add(PackagedFileInfo.CreateFromEntry(entry, _streams[entry.ArchivePart]));
            }
        }

        private void ReadFileListV18(BinaryReader reader, Package package)
        {
            int numFiles = reader.ReadInt32();
            int compressedSize = reader.ReadInt32();
            byte[] compressedFileList = reader.ReadBytes(compressedSize);

            int fileBufferSize = Marshal.SizeOf(typeof(FileEntry18)) * numFiles;
            var uncompressedList = new byte[fileBufferSize];
            int uncompressedSize = LZ4Codec.Decode(compressedFileList, 0, compressedFileList.Length, uncompressedList, 0, fileBufferSize, false);
            if (uncompressedSize != fileBufferSize)
            {
                string msg = $"LZ4 compressor disagrees about the size of file headers; expected {fileBufferSize}, got {uncompressedSize}";
                throw new InvalidDataException(msg);
            }

            var ms = new MemoryStream(uncompressedList);
            var msr = new BinaryReader(ms);

            var entries = new FileEntry18[numFiles];
            BinUtils.ReadStructs(msr, entries);

            foreach (var entry in entries)
            {
                package.Files.Add(PackagedFileInfo.CreateFromEntry(entry, _streams[entry.ArchivePart]));
            }
        }

        private Package ReadPackageV15(FileStream mainStream, BinaryReader reader)
        {
            var package = new Package();
            var header = BinUtils.ReadStruct<LSPKHeader15>(reader);

            if (header.Version != (ulong)PackageVersion.V15)
            {
                string msg = $"Unsupported package version {header.Version}; this layout is only supported for V15";
                throw new InvalidDataException(msg);
            }

            package.Metadata.Flags = (PackageFlags)header.Flags;
            package.Metadata.Priority = header.Priority;
            package.Version = PackageVersion.V15;

            if (_metadataOnly) return package;

            OpenStreams(mainStream, 1);
            mainStream.Seek((long)header.FileListOffset, SeekOrigin.Begin);
            ReadFileListV15(reader, package);

            return package;
        }

        private Package ReadPackageV16(FileStream mainStream, BinaryReader reader)
        {
            var package = new Package();
            var header = BinUtils.ReadStruct<LSPKHeader16>(reader);

            if (header.Version != (ulong)PackageVersion.V16)
            {
                string msg = $"Unsupported package version {header.Version}; this layout is only supported for V16";
                throw new InvalidDataException(msg);
            }

            package.Metadata.Flags = (PackageFlags)header.Flags;
            package.Metadata.Priority = header.Priority;
            package.Version = PackageVersion.V16;

            if (_metadataOnly) return package;

            OpenStreams(mainStream, header.NumParts);
            mainStream.Seek((long)header.FileListOffset, SeekOrigin.Begin);
            ReadFileListV15(reader, package);

            return package;
        }

        private Package ReadPackageV18(FileStream mainStream, BinaryReader reader)
        {
            var package = new Package();
            var header = BinUtils.ReadStruct<LSPKHeader16>(reader);

            if (header.Version != (ulong)PackageVersion.V18)
            {
                string msg = $"Unsupported package version {header.Version}; this layout is only supported for V18";
                throw new InvalidDataException(msg);
            }

            package.Metadata.Flags = (PackageFlags)header.Flags;
            package.Metadata.Priority = header.Priority;
            package.Version = PackageVersion.V18;

            if (_metadataOnly) return package;

            OpenStreams(mainStream, header.NumParts);
            mainStream.Seek((long)header.FileListOffset, SeekOrigin.Begin);
            ReadFileListV18(reader, package);

            return package;
        }

        public Package Read()
        {
            var mainStream = File.Open(_path, FileMode.Open, FileAccess.Read, FileShare.Read);

            using (var reader = new BinaryReader(mainStream, new UTF8Encoding(), true))
            {
                // Check for v13 package headers
                mainStream.Seek(-8, SeekOrigin.End);
                Int32 headerSize = reader.ReadInt32();
                byte[] signature = reader.ReadBytes(4);
                if (Package.Signature.SequenceEqual(signature))
                {
                    mainStream.Seek(-headerSize, SeekOrigin.End);
                    return ReadPackageV13(mainStream, reader);
                }

                // Check for v10 package headers
                mainStream.Seek(0, SeekOrigin.Begin);
                signature = reader.ReadBytes(4);
                Int32 version;
                if (Package.Signature.SequenceEqual(signature))
                {
                    version = reader.ReadInt32();
                    if (version == 10)
                    {
                        return ReadPackageV10(mainStream, reader);
                    }
                    else if (version == 15)
                    {
                        mainStream.Seek(4, SeekOrigin.Begin);
                        return ReadPackageV15(mainStream, reader);
                    }
                    else if (version == 16)
                    {
                        mainStream.Seek(4, SeekOrigin.Begin);
                        return ReadPackageV16(mainStream, reader);
                    }
                    else if (version == 18)
                    {
                        mainStream.Seek(4, SeekOrigin.Begin);
                        return ReadPackageV18(mainStream, reader);
                    }
                    else
                    {
                        throw new InvalidDataException($"Package version v{version} not supported");
                    }
                }

                // Check for v9 and v7 package headers
                mainStream.Seek(0, SeekOrigin.Begin);
                version = reader.ReadInt32();
                if (version == 7 || version == 9)
                {
                    return ReadPackageV7(mainStream, reader);
                }

                throw new NotAPackageException("No valid signature found in package file");
            }
        }
    }
}
