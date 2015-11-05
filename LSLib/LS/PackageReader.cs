using zlib;
using LZ4;
using LSLib.Granny;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LSLib.LS
{
    public class PackageReader
    {
        private String path;
        private Stream[] streams;

        public PackageReader(string path)
        {
            this.path = path;
        }

        public void Dispose()
        {
            foreach (var stream in streams)
            {
                stream.Dispose();
            }
        }

        private void OpenStreams(FileStream mainStream, int numParts)
        {
            // Open a stream for each file chunk
            streams = new Stream[numParts];
            streams[0] = mainStream;

            for (int part = 1; part < numParts; part++)
            {
                var partPath = Package.MakePartFilename(path, part);
                streams[part] = new FileStream(partPath, FileMode.Open, FileAccess.Read);
            }
        }

        private Package ReadPackageV7(FileStream mainStream, BinaryReader reader)
        {
            var package = new Package();
            mainStream.Seek(0, SeekOrigin.Begin);
            var header = BinUtils.ReadStruct<LSPKHeader7>(reader);

            OpenStreams(mainStream, (int)header.NumParts);
            for (uint i = 0; i < header.NumFiles; i++)
            {
                var entry = BinUtils.ReadStruct<FileEntry7>(reader);
                if (entry.ArchivePart == 0)
                {
                    entry.OffsetInFile += header.DataOffset;
                }
                package.Files.Add(PackagedFileInfo.CreateFromEntry(entry, streams[entry.ArchivePart]));
            }

            return package;
        }

        private Package ReadPackageV10(FileStream mainStream, BinaryReader reader)
        {
            var package = new Package();
            mainStream.Seek(4, SeekOrigin.Begin);
            var header = BinUtils.ReadStruct<LSPKHeader10>(reader);

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
                package.Files.Add(PackagedFileInfo.CreateFromEntry(entry, streams[entry.ArchivePart]));
            }

            return package;
        }

        private Package ReadPackageV13(FileStream mainStream, BinaryReader reader)
        {
            var package = new Package();
            var header = BinUtils.ReadStruct<LSPKHeader13>(reader);

            if (header.Version != Package.CurrentVersion)
            {
                var msg = String.Format("Unsupported package version {0}; this extractor only supports {1}", header.Version, Package.CurrentVersion);
                throw new InvalidDataException(msg);
            }

            OpenStreams(mainStream, header.NumParts);
            mainStream.Seek(header.FileListOffset, SeekOrigin.Begin);
            int numFiles = reader.ReadInt32();
            int fileBufferSize = Marshal.SizeOf(typeof(FileEntry13)) * numFiles;
            byte[] compressedFileList = reader.ReadBytes((int)header.FileListSize - 4);

            var uncompressedList = new byte[fileBufferSize];
            var uncompressedSize = LZ4Codec.Decode(compressedFileList, 0, compressedFileList.Length, uncompressedList, 0, fileBufferSize, true);
            if (uncompressedSize != fileBufferSize)
            {
                var msg = String.Format("LZ4 compressor disagrees about the size of file headers; expected {0}, got {1}", fileBufferSize, uncompressedSize);
                throw new InvalidDataException(msg);
            }

            var ms = new MemoryStream(uncompressedList);
            var msr = new BinaryReader(ms);

            for (int i = 0; i < numFiles; i++)
            {
                var entry = BinUtils.ReadStruct<FileEntry13>(msr);
                package.Files.Add(PackagedFileInfo.CreateFromEntry(entry, streams[entry.ArchivePart]));
            }

            return package;
        }

        public Package Read()
        {
            var mainStream = new FileStream(path, FileMode.Open, FileAccess.Read);

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
                }

                // Check for v9 and v7 package headers
                mainStream.Seek(0, SeekOrigin.Begin);
                version = reader.ReadInt32();
                if (version == 7 || version == 9)
                {
                    return ReadPackageV7(mainStream, reader);
                }

                throw new InvalidDataException("No valid signature found in package file");
            }
        }
    }
}
