using System.IO.Hashing;
using System.Security.Cryptography;
using LSLib.LS.Enums;
using LZ4;

namespace LSLib.LS;

public class PackageBuildTransientFile : PackagedFileInfoCommon
{
}

public class PackageWriter(PackageBuildData Build, string PackagePath) : IDisposable
{
    public delegate void WriteProgressDelegate(PackageBuildInputFile file, long numerator, long denominator);

    private readonly PackageHeaderCommon Metadata = new();
    private readonly List<Stream> Streams = [];
    public WriteProgressDelegate WriteProgress = delegate { };

    public void Dispose()
    {
        foreach (Stream stream in Streams)
        {
            stream.Dispose();
        }
    }

    private PackageBuildTransientFile WriteFile(PackageBuildInputFile input)
    {
        using var inputStream = input.MakeInputStream();

        var compression = Build.Compression;
        var compressionLevel = Build.CompressionLevel;

        if (input.Path.EndsWith(".gts") 
            || input.Path.EndsWith(".gtp") 
            || input.Path.EndsWith(".wem") 
            || input.Path.EndsWith(".bnk") 
            || inputStream.Length == 0)
        {
            compression = CompressionMethod.None;
            compressionLevel = LSCompressionLevel.Fast;
        }

        var uncompressed = new byte[inputStream.Length];
        inputStream.ReadExactly(uncompressed, 0, uncompressed.Length);
        var compressed = BinUtils.Compress(uncompressed, compression, compressionLevel);

        if (Streams.Last().Position + compressed.Length > Build.Version.MaxPackageSize())
        {
            // Start a new package file if the current one is full.
            string partPath = Package.MakePartFilename(PackagePath, Streams.Count);
            var nextPart = File.Open(partPath, FileMode.Create, FileAccess.Write);
            Streams.Add(nextPart);
        }

        Stream stream = Streams.Last();
        var packaged = new PackageBuildTransientFile
        {
            Name = input.Path.Replace('\\', '/'),
            UncompressedSize = (ulong)uncompressed.Length,
            SizeOnDisk = (ulong)compressed.Length,
            ArchivePart = (UInt32)(Streams.Count - 1),
            OffsetInFile = (ulong)stream.Position,
            Flags = BinUtils.MakeCompressionFlags(compression, compressionLevel)
        };

        stream.Write(compressed, 0, compressed.Length);

        if (Build.Version.HasCrc())
        {
            packaged.Crc = Crc32.HashToUInt32(compressed);
        }
        else
        {
            packaged.Crc = 0;
        }

        if (!Build.Flags.HasFlag(PackageFlags.Solid))
        {
            int padLength = Build.Version.PaddingSize();
            long alignTo;
            if (Build.Version >= PackageVersion.V16)
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
        // <= v9 packages don't support LZ4
        if ((Build.Version == PackageVersion.V7 || Build.Version == PackageVersion.V9) && Build.Compression == CompressionMethod.LZ4)
        {
            Build.Compression = CompressionMethod.Zlib;
        }

        Metadata.NumFiles = (uint)Build.Files.Count;
        Metadata.FileListSize = (UInt32)(Marshal.SizeOf(typeof(TFile)) * Build.Files.Count);

        using var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true);

        Metadata.DataOffset = (UInt32)Marshal.SizeOf(typeof(THeader)) + Metadata.FileListSize;
        if (Metadata.Version >= 10)
        {
            Metadata.DataOffset += 4;
        }

        int paddingLength = Build.Version.PaddingSize();
        if (Metadata.DataOffset % paddingLength > 0)
        {
            Metadata.DataOffset += (UInt32)(paddingLength - Metadata.DataOffset % paddingLength);
        }

        // Write a placeholder instead of the actual headers; we'll write them after we
        // compressed and flushed all files to disk
        var placeholder = new byte[Metadata.DataOffset];
        writer.Write(placeholder);

        var writtenFiles = PackFiles();

        mainStream.Seek(0, SeekOrigin.Begin);
        if (Metadata.Version >= 10)
        {
            writer.Write(PackageHeaderCommon.Signature);
        }
        Metadata.NumParts = (UInt16)Streams.Count;
        Metadata.Md5 = ComputeArchiveHash();

        var header = (THeader)THeader.FromCommonHeader(Metadata);
        BinUtils.WriteStruct(writer, ref header);

        WriteFileList<TFile>(writer, writtenFiles);
    }

    private void PackV13<THeader, TFile>(FileStream mainStream)
        where THeader : ILSPKHeader
        where TFile : ILSPKFile
    {
        var writtenFiles = PackFiles();

        using var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true);

        Metadata.FileListOffset = (UInt64)mainStream.Position;
        WriteCompressedFileList<TFile>(writer, writtenFiles);

        Metadata.FileListSize = (UInt32)(mainStream.Position - (long)Metadata.FileListOffset);
        Metadata.Md5 = ComputeArchiveHash();
        Metadata.NumParts = (UInt16)Streams.Count;

        var header = (THeader)THeader.FromCommonHeader(Metadata);
        BinUtils.WriteStruct(writer, ref header);

        writer.Write((UInt32)(8 + Marshal.SizeOf(typeof(THeader))));
        writer.Write(PackageHeaderCommon.Signature);
    }

    private List<PackageBuildTransientFile> PackFiles()
    {
        long totalSize = Build.Files.Sum(p => (long)p.Size());
        long currentSize = 0;

        var writtenFiles = new List<PackageBuildTransientFile>();
        foreach (var file in Build.Files)
        {
            WriteProgress(file, currentSize, totalSize);
            writtenFiles.Add(WriteFile(file));
            currentSize += file.Size();
        }

        return writtenFiles;
    }

    private void WriteFileList<TFile>(BinaryWriter metadataWriter, List<PackageBuildTransientFile> files)
        where TFile : ILSPKFile
    {
        foreach (var file in files)
        {
            if (file.ArchivePart == 0)
            {
                file.OffsetInFile -= Metadata.DataOffset;
            }

            // <= v10 packages don't support compression level in the flags field
            file.Flags = (CompressionFlags)((byte)file.Flags & 0x0f);

            var entry = (TFile)TFile.FromCommon(file);
            BinUtils.WriteStruct(metadataWriter, ref entry);
        }
    }

    private void WriteCompressedFileList<TFile>(BinaryWriter metadataWriter, List<PackageBuildTransientFile> files)
        where TFile : ILSPKFile
    {
        byte[] fileListBuf;
        using (var fileList = new MemoryStream())
        using (var fileListWriter = new BinaryWriter(fileList))
        {
            foreach (var file in files)
            {
                var entry = (TFile)TFile.FromCommon(file);
                BinUtils.WriteStruct(fileListWriter, ref entry);
            }

            fileListBuf = fileList.ToArray();
        }

        byte[] compressedFileList = LZ4Codec.EncodeHC(fileListBuf, 0, fileListBuf.Length);

        metadataWriter.Write((UInt32)files.Count);

        if (Build.Version > PackageVersion.V13)
        {
            metadataWriter.Write((UInt32)compressedFileList.Length);
        }
        else
        {
            Metadata.FileListSize = (uint)compressedFileList.Length + 4;
        }

        metadataWriter.Write(compressedFileList);
    }

    private void PackV15<THeader, TFile>(FileStream mainStream)
        where THeader : ILSPKHeader
        where TFile : ILSPKFile
    {
        using (var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true))
        {
            writer.Write(PackageHeaderCommon.Signature);
            var header = (THeader)THeader.FromCommonHeader(Metadata);
            BinUtils.WriteStruct(writer, ref header);
        }

        var writtenFiles = PackFiles();

        using (var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true))
        {
            Metadata.FileListOffset = (UInt64)mainStream.Position;
            WriteCompressedFileList<TFile>(writer, writtenFiles);

            Metadata.FileListSize = (UInt32)(mainStream.Position - (long)Metadata.FileListOffset);
            if (Build.Hash)
            {
                Metadata.Md5 = ComputeArchiveHash();
            }
            else
            {
                Metadata.Md5 = new byte[0x10];
            }

            Metadata.NumParts = (UInt16)Streams.Count;

            mainStream.Seek(4, SeekOrigin.Begin);
            var header = (THeader)THeader.FromCommonHeader(Metadata);
            BinUtils.WriteStruct(writer, ref header);
        }
    }

    public byte[] ComputeArchiveHash()
    {
        // MD5 is computed over the contents of all files in an alphabetically sorted order
        var orderedFileList = Build.Files.Select(item => item).ToList();
        if (Build.Version < PackageVersion.V15)
        {
            orderedFileList.Sort((a, b) => String.CompareOrdinal(a.Path, b.Path));
        }

        using MD5 md5 = MD5.Create();
        foreach (var file in orderedFileList)
        {
            using var packagedStream = file.MakeInputStream();
            using var reader = new BinaryReader(packagedStream);

            byte[] uncompressed = reader.ReadBytes((int)reader.BaseStream.Length);
            md5.TransformBlock(uncompressed, 0, uncompressed.Length, uncompressed, 0);
        }

        md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
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
        var mainStream = File.Open(PackagePath, FileMode.Create, FileAccess.Write);
        Streams.Add(mainStream);

        Metadata.Version = (UInt32)Build.Version;
        Metadata.Flags = Build.Flags;
        Metadata.Priority = Build.Priority;
        Metadata.Md5 = new byte[16];

        switch (Build.Version)
        {
            case PackageVersion.V18: PackV15<LSPKHeader16, FileEntry18>(mainStream); break;
            case PackageVersion.V16: PackV15<LSPKHeader16, FileEntry15>(mainStream); break;
            case PackageVersion.V15: PackV15<LSPKHeader15, FileEntry18>(mainStream); break;
            case PackageVersion.V13: PackV13<LSPKHeader13, FileEntry10>(mainStream); break;
            case PackageVersion.V10: PackV7<LSPKHeader10, FileEntry10>(mainStream); break;
            case PackageVersion.V9:
            case PackageVersion.V7: PackV7<LSPKHeader7, FileEntry7>(mainStream); break;
            default: throw new ArgumentException($"Cannot write version {Build.Version} packages");
        }
    }
}
