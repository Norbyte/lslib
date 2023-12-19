using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using LZ4;
using LSLib.LS.Enums;

namespace LSLib.LS;

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

public class PackageReader(string path, bool metadataOnly = false) : IDisposable
{
    private Stream[] Streams;

    public void Dispose()
    {
        foreach (Stream stream in Streams ?? [])
        {
            stream?.Dispose();
        }
    }

    private void OpenStreams(FileStream mainStream, int numParts)
    {
        // Open a stream for each file chunk
        Streams = new Stream[numParts];
        Streams[0] = mainStream;

        for (var part = 1; part < numParts; part++)
        {
            string partPath = Package.MakePartFilename(path, part);
            Streams[part] = File.Open(partPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
    }

    private void ReadCompressedFileList<TFile>(BinaryReader reader, Package package) where TFile : ILSPKFile
    {
        int numFiles = reader.ReadInt32();
        int compressedSize;
        if (package.Metadata.Version > 13)
        {
            compressedSize = reader.ReadInt32();
        }
        else
        {
            compressedSize = (int)package.Metadata.FileListSize - 4;
        }

        byte[] compressedFileList = reader.ReadBytes(compressedSize);

        int fileBufferSize = Marshal.SizeOf(typeof(TFile)) * numFiles;
        var uncompressedList = new byte[fileBufferSize];
        int uncompressedSize = LZ4Codec.Decode(compressedFileList, 0, compressedFileList.Length, uncompressedList, 0, fileBufferSize, true);
        if (uncompressedSize != fileBufferSize)
        {
            string msg = $"LZ4 compressor disagrees about the size of file headers; expected {fileBufferSize}, got {uncompressedSize}";
            throw new InvalidDataException(msg);
        }

        var ms = new MemoryStream(uncompressedList);
        var msr = new BinaryReader(ms);

        var entries = new TFile[numFiles];
        BinUtils.ReadStructs(msr, entries);

        foreach (var entry in entries)
        {
            package.Files.Add(PackagedFileInfo.CreateFromEntry(entry, Streams[entry.ArchivePartNumber()]));
        }
    }

    private void ReadFileList<TFile>(BinaryReader reader, Package package) where TFile : ILSPKFile
    {
        var entries = new TFile[package.Metadata.NumFiles];
        BinUtils.ReadStructs(reader, entries);

        foreach (var entry in entries)
        {
            var file = PackagedFileInfo.CreateFromEntry(entry, Streams[entry.ArchivePartNumber()]);
            if (file.ArchivePart == 0)
            {
                file.OffsetInFile += package.Metadata.DataOffset;
            }

            package.Files.Add(file);
        }
    }

    private Package ReadHeaderAndFileList<THeader, TFile>(FileStream mainStream, BinaryReader reader)
        where THeader : ILSPKHeader 
        where TFile : ILSPKFile
    {
        var package = new Package();
        var header = BinUtils.ReadStruct<THeader>(reader);

        package.Metadata = header.ToCommonHeader();
        package.Version = (PackageVersion)package.Metadata.Version;

        if (metadataOnly) return package;

        OpenStreams(mainStream, (int)package.Metadata.NumParts);

        if (package.Metadata.Version > 10)
        {
            mainStream.Seek((long)package.Metadata.FileListOffset, SeekOrigin.Begin);
            ReadCompressedFileList<TFile>(reader, package);
        }
        else
        {
            ReadFileList<TFile>(reader, package);
        }

        if (((PackageFlags)package.Metadata.Flags).HasFlag(PackageFlags.Solid) && package.Files.Count > 0)
        {
            UnpackSolidSegment(mainStream, package);
        }

        return package;
    }

    private void UnpackSolidSegment(FileStream mainStream, Package package)
    {
        // Calculate compressed frame offset and bounds
        ulong totalUncompressedSize = 0;
        ulong totalSizeOnDisk = 0;
        ulong firstOffset = 0xffffffff;
        ulong lastOffset = 0;

        foreach (var entry in package.Files)
        {
            var file = entry as PackagedFileInfo;

            totalUncompressedSize += file.UncompressedSize;
            totalSizeOnDisk += file.SizeOnDisk;
            if (file.OffsetInFile < firstOffset)
            {
                firstOffset = file.OffsetInFile;
            }
            if (file.OffsetInFile + file.SizeOnDisk > lastOffset)
            {
                lastOffset = file.OffsetInFile + file.SizeOnDisk;
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
        ulong offset = 7;
        ulong compressedOffset = 0;
        foreach (var entry in package.Files)
        {
            var file = entry as PackagedFileInfo;

            if (file.OffsetInFile != offset)
            {
                throw new InvalidDataException("File list in solid archive not contiguous");
            }

            file.MakeSolid(compressedOffset, decompressedStream);

            offset += file.SizeOnDisk;
            compressedOffset += file.UncompressedSize;
        }
    }

    public Package Read()
    {
        var mainStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(mainStream, new UTF8Encoding(), true);

        // Check for v13 package headers
        mainStream.Seek(-8, SeekOrigin.End);
        Int32 headerSize = reader.ReadInt32();
        byte[] signature = reader.ReadBytes(4);
        if (Package.Signature.SequenceEqual(signature))
        {
            mainStream.Seek(-headerSize, SeekOrigin.End);
            return ReadHeaderAndFileList<LSPKHeader13, FileEntry10>(mainStream, reader);
        }

        // Check for v10 package headers
        mainStream.Seek(0, SeekOrigin.Begin);
        signature = reader.ReadBytes(4);
        Int32 version;
        if (Package.Signature.SequenceEqual(signature))
        {
            version = reader.ReadInt32();
            mainStream.Seek(4, SeekOrigin.Begin);
            return version switch
            {
                10 => ReadHeaderAndFileList<LSPKHeader10, FileEntry10>(mainStream, reader),
                15 => ReadHeaderAndFileList<LSPKHeader15, FileEntry15>(mainStream, reader),
                16 => ReadHeaderAndFileList<LSPKHeader16, FileEntry15>(mainStream, reader),
                18 => ReadHeaderAndFileList<LSPKHeader16, FileEntry18>(mainStream, reader),
                _ => throw new InvalidDataException($"Package version v{version} not supported")
            };
        }

        // Check for v9 and v7 package headers
        mainStream.Seek(0, SeekOrigin.Begin);
        version = reader.ReadInt32();
        if (version == 7 || version == 9)
        {
            mainStream.Seek(0, SeekOrigin.Begin);
            return ReadHeaderAndFileList<LSPKHeader7, FileEntry7>(mainStream, reader);
        }

        throw new NotAPackageException("No valid signature found in package file");
    }
}
