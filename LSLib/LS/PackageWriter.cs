using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using LSLib.Granny.GR2;
using LSLib.LS.Enums;
using LSLib.VirtualTextures;
using LZ4;

namespace LSLib.LS;

public class PackageWriter(Package package, string path) : IDisposable
{
    public delegate void WriteProgressDelegate(IAbstractFileInfo abstractFile, long numerator, long denominator);

    private const long MaxPackageSizeDOS = 0x40000000;
    private const long MaxPackageSizeBG3 = 0x100000000;
    public CompressionMethod Compression = CompressionMethod.None;
    public LSCompressionLevel LSCompressionLevel = LSCompressionLevel.DefaultCompression;
    private readonly List<Stream> Streams = [];
    public PackageVersion Version = Package.CurrentVersion;
    public WriteProgressDelegate WriteProgress = delegate { };

    public void Dispose()
    {
        foreach (Stream stream in Streams)
        {
            stream.Dispose();
        }
    }

    public int PaddingLength() => Version <= PackageVersion.V9 ? 0x8000 : 0x40;

    public PackagedFileInfo WriteFile(IAbstractFileInfo info)
    {
        // Assume that all files are written uncompressed (worst-case) when calculating package sizes
        long size = (long)info.Size();
        if ((Version < PackageVersion.V15 && Streams.Last().Position + size > MaxPackageSizeDOS)
            || (Version >= PackageVersion.V16 && Streams.Last().Position + size > MaxPackageSizeBG3))
        {
            // Start a new package file if the current one is full.
            string partPath = Package.MakePartFilename(path, Streams.Count);
            var nextPart = File.Open(partPath, FileMode.Create, FileAccess.Write);
            Streams.Add(nextPart);
        }

        var compression = Compression;
        var compressionLevel = LSCompressionLevel;

        if (info.Name.EndsWith(".gts") || info.Name.EndsWith(".gtp") || size == 0)
        {
            compression = CompressionMethod.None;
            compressionLevel = LSCompressionLevel.FastCompression;
        }

        Stream stream = Streams.Last();
        var packaged = new PackagedFileInfo
        {
            PackageStream = stream,
            FileName = info.Name,
            UncompressedSize = (ulong)size,
            ArchivePart = (UInt32) (Streams.Count - 1),
            OffsetInFile = (UInt32) stream.Position,
            Flags = BinUtils.MakeCompressionFlags(compression, compressionLevel)
        };

        Stream packagedStream = info.MakeStream();
        byte[] compressed;
        try
        {
            using var reader = new BinaryReader(packagedStream, Encoding.UTF8, true);
            byte[] uncompressed = reader.ReadBytes((int)reader.BaseStream.Length);
            compressed = BinUtils.Compress(uncompressed, compression, compressionLevel);
            stream.Write(compressed, 0, compressed.Length);
        }
        finally
        {
            info.ReleaseStream();
        }

        packaged.SizeOnDisk = (UInt64) (stream.Position - (long)packaged.OffsetInFile);
        packaged.Crc = Crc32.HashToUInt32(compressed);

        if (!package.Metadata.Flags.HasFlag(PackageFlags.Solid))
        {
            int padLength = PaddingLength();
            long alignTo;
            if (Version >= PackageVersion.V16)
            {
                alignTo = stream.Position - Marshal.SizeOf(typeof(LSPKHeader16)) - 4;
            }
            else
            {
                alignTo = stream.Position;
            }

            // Pad the file to a multiple of 64 bytes
            var padBytes = (padLength - alignTo % padLength) % padLength;
            var pad = new byte[padBytes];
            for (var i = 0; i < pad.Length; i++)
            {
                pad[i] = 0xAD;
            }

            stream.Write(pad, 0, pad.Length);
        }

        return packaged;
    }

    private void PackV7<THeader, TFile>(FileStream mainStream)
        where THeader : ILSPKHeader
        where TFile : ILSPKFile
    {
        package.Metadata.NumFiles = (uint)package.Files.Count;
        package.Metadata.FileListSize = (UInt32)(Marshal.SizeOf(typeof(TFile)) * package.Files.Count);

        using var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true);

        package.Metadata.DataOffset = 4 + (UInt32)Marshal.SizeOf(typeof(THeader)) + package.Metadata.FileListSize;

        int paddingLength = PaddingLength();
        if (package.Metadata.DataOffset % paddingLength > 0)
        {
            package.Metadata.DataOffset += (UInt32)(paddingLength - package.Metadata.DataOffset % paddingLength);
        }

        // Write a placeholder instead of the actual headers; we'll write them after we
        // compressed and flushed all files to disk
        var placeholder = new byte[package.Metadata.DataOffset];
        writer.Write(placeholder);

        var writtenFiles = PackFiles();

        mainStream.Seek(0, SeekOrigin.Begin);
        writer.Write(Package.Signature);
        package.Metadata.NumParts = (UInt16)Streams.Count;
        package.Metadata.Md5 = ComputeArchiveHash();

        var header = (THeader)THeader.FromCommonHeader(package.Metadata);
        BinUtils.WriteStruct(writer, ref header);

        WriteFileList<TFile>(writer, writtenFiles);
    }

    private void PackV13<THeader, TFile>(FileStream mainStream)
        where THeader : ILSPKHeader
        where TFile : ILSPKFile
    {
        var writtenFiles = PackFiles();

        using var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true);

        package.Metadata.FileListOffset = (UInt64)mainStream.Position;
        WriteCompressedFileList<TFile>(writer, writtenFiles);

        package.Metadata.FileListSize = (UInt32)(mainStream.Position - (long)package.Metadata.FileListOffset);
        package.Metadata.Md5 = ComputeArchiveHash();
        package.Metadata.NumParts = (UInt16)Streams.Count;

        var header = (THeader)THeader.FromCommonHeader(package.Metadata);
        BinUtils.WriteStruct(writer, ref header);

        writer.Write((UInt32)(8 + Marshal.SizeOf(typeof(THeader))));
        writer.Write(Package.Signature);
    }

    private List<PackagedFileInfo> PackFiles()
    {
        long totalSize = package.Files.Sum(p => (long)p.Size());
        long currentSize = 0;

        var writtenFiles = new List<PackagedFileInfo>();
        foreach (var file in package.Files)
        {
            WriteProgress(file, currentSize, totalSize);
            writtenFiles.Add(WriteFile(file));
            currentSize += (long)file.Size();
        }

        return writtenFiles;
    }

    private void WriteFileList<TFile>(BinaryWriter metadataWriter, List<PackagedFileInfo> files)
        where TFile : ILSPKFile
    {
        foreach (var file in files)
        {
            if (file.ArchivePart == 0)
            {
                file.OffsetInFile -= package.Metadata.DataOffset;
            }

            // <= v10 packages don't support compression level in the flags field
            file.Flags &= 0x0f;

            var entry = (TFile)TFile.FromCommon(file);
            BinUtils.WriteStruct(metadataWriter, ref entry);
        }
    }

    private void WriteCompressedFileList<TFile>(BinaryWriter metadataWriter, List<PackagedFileInfo> files)
        where TFile : ILSPKFile
    {
        byte[] fileListBuf;
        using (var fileList = new MemoryStream())
        using (var fileListWriter = new BinaryWriter(fileList))
        {
            foreach (PackagedFileInfo file in files)
            {
                var entry = (TFile)TFile.FromCommon(file);
                BinUtils.WriteStruct(fileListWriter, ref entry);
            }

            fileListBuf = fileList.ToArray();
        }

        byte[] compressedFileList = LZ4Codec.EncodeHC(fileListBuf, 0, fileListBuf.Length);

        metadataWriter.Write((UInt32)files.Count);

        if (Version > PackageVersion.V13)
        {
            metadataWriter.Write((UInt32)compressedFileList.Length);
        }
        else
        {
            package.Metadata.FileListSize = (uint)compressedFileList.Length + 4;
        }

        metadataWriter.Write(compressedFileList);
    }

    private void PackV15<THeader, TFile>(FileStream mainStream)
        where THeader : ILSPKHeader
        where TFile : ILSPKFile
    {
        using (var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true))
        {
            writer.Write(Package.Signature);
            var header = (THeader)THeader.FromCommonHeader(package.Metadata);
            BinUtils.WriteStruct(writer, ref header);
        }

        var writtenFiles = PackFiles();

        using (var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true))
        {
            package.Metadata.FileListOffset = (UInt64)mainStream.Position;
            WriteCompressedFileList<TFile>(writer, writtenFiles);

            package.Metadata.FileListSize = (UInt32)(mainStream.Position - (long)package.Metadata.FileListOffset);
            package.Metadata.Md5 = ComputeArchiveHash();
            package.Metadata.NumParts = (UInt16)Streams.Count;

            mainStream.Seek(4, SeekOrigin.Begin);
            var header = (THeader)THeader.FromCommonHeader(package.Metadata);
            BinUtils.WriteStruct(writer, ref header);
        }
    }

    public byte[] ComputeArchiveHash()
    {
        // MD5 is computed over the contents of all files in an alphabetically sorted order
        var orderedFileList = package.Files.Select(item => item).ToList();
        if (Version < PackageVersion.V15)
        {
            orderedFileList.Sort((a, b) => String.CompareOrdinal(a.Name, b.Name));
        }

        using MD5 md5 = MD5.Create();
        foreach (var file in orderedFileList)
        {
            Stream packagedStream = file.MakeStream();
            try
            {
                using (var reader = new BinaryReader(packagedStream))
                {
                    byte[] uncompressed = reader.ReadBytes((int)reader.BaseStream.Length);
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

    public void Write()
    {
        var mainStream = File.Open(path, FileMode.Create, FileAccess.Write);
        Streams.Add(mainStream);

        switch (Version)
        {
            case PackageVersion.V18: PackV15<LSPKHeader16, FileEntry18>(mainStream); break;
            case PackageVersion.V16: PackV15<LSPKHeader16, FileEntry15>(mainStream); break;
            case PackageVersion.V15: PackV15<LSPKHeader15, FileEntry18>(mainStream); break;
            case PackageVersion.V13: PackV13<LSPKHeader13, FileEntry10>(mainStream); break;
            case PackageVersion.V10: PackV7<LSPKHeader10, FileEntry10>(mainStream); break;
            case PackageVersion.V9:
            case PackageVersion.V7: PackV7<LSPKHeader7, FileEntry7>(mainStream); break;
            default: throw new ArgumentException($"Cannot write version {Version} packages");
        }
    }
}
