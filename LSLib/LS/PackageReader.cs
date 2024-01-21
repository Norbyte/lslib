using LSLib.LS.Enums;
using System.IO.MemoryMappedFiles;

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

public class Package : IDisposable
{
    public readonly string PackagePath;
    internal readonly MemoryMappedFile MetadataFile;
    internal readonly MemoryMappedViewAccessor MetadataView;

    internal MemoryMappedFile[] Parts;
    internal MemoryMappedViewAccessor[] Views;

    public PackageHeaderCommon Metadata;
    public List<PackagedFileInfo> Files = [];
    
    public PackageVersion Version
    {
        get { return (PackageVersion)Metadata.Version; }
    }

    public void OpenPart(int index, string path)
    {
        var file = File.OpenRead(path);
        Parts[index] = MemoryMappedFile.CreateFromFile(file, null, file.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
        Views[index] = MetadataFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
    }

    public void OpenStreams(int numParts)
    {
        // Open a stream for each file chunk
        Parts = new MemoryMappedFile[numParts];
        Views = new MemoryMappedViewAccessor[numParts];

        Parts[0] = MetadataFile;
        Views[0] = MetadataView;

        for (var part = 1; part < numParts; part++)
        {
            string partPath = Package.MakePartFilename(PackagePath, part);
            OpenPart(part, partPath);
        }
    }

    internal Package(string path)
    {
        PackagePath = path;
        var file = File.OpenRead(PackagePath);
        MetadataFile = MemoryMappedFile.CreateFromFile(file, null, file.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
        MetadataView = MetadataFile.CreateViewAccessor(0, file.Length, MemoryMappedFileAccess.Read);
    }

    public void Dispose()
    {
        MetadataView?.Dispose();
        MetadataFile?.Dispose();

        foreach (var view in Views ?? [])
        {
            view?.Dispose();
        }

        foreach (var file in Parts ?? [])
        {
            file?.Dispose();
        }
    }

    public static string MakePartFilename(string path, int part)
    {
        string dirName = Path.GetDirectoryName(path);
        string baseName = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        return Path.Join(dirName, $"{baseName}_{part}{extension}");
    }
}

public class PackageReader
{
    private bool MetadataOnly;
    private Package Pak;

    private void ReadCompressedFileList<TFile>(MemoryMappedViewAccessor view, long offset)
        where TFile : struct, ILSPKFile
    {
        int numFiles = view.ReadInt32(offset);
        byte[] compressed;
        if (Pak.Metadata.Version > 13)
        {
            int compressedSize = view.ReadInt32(offset + 4);
            compressed = new byte[compressedSize];
            view.ReadArray(offset + 8, compressed, 0, compressedSize);
        }
        else
        {
            compressed = new byte[(int)Pak.Metadata.FileListSize - 4];
            view.ReadArray(offset + 4, compressed, 0, (int)Pak.Metadata.FileListSize - 4);
        }

        int fileBufferSize = Marshal.SizeOf(typeof(TFile)) * numFiles;
        var fileBuf = BinUtils.Decompress(compressed, fileBufferSize, CompressionFlags.MethodLZ4);

        using var ms = new MemoryStream(fileBuf);
        using var msr = new BinaryReader(ms);

        var entries = new TFile[numFiles];
        BinUtils.ReadStructs(msr, entries);

        foreach (var entry in entries)
        {
            Pak.Files.Add(PackagedFileInfo.CreateFromEntry(Pak, entry, Pak.Parts[entry.ArchivePartNumber()], Pak.Views[entry.ArchivePartNumber()]));
        }
    }

    private void ReadFileList<TFile>(MemoryMappedViewAccessor view, long offset) 
        where TFile : struct, ILSPKFile
    {
        var entries = new TFile[Pak.Metadata.NumFiles];
        BinUtils.ReadStructs(view, offset, entries);

        foreach (var entry in entries)
        {
            var file = PackagedFileInfo.CreateFromEntry(Pak, entry, Pak.Parts[entry.ArchivePartNumber()], Pak.Views[entry.ArchivePartNumber()]);
            if (file.ArchivePart == 0)
            {
                file.OffsetInFile += Pak.Metadata.DataOffset;
            }

            Pak.Files.Add(file);
        }
    }

    private Package ReadHeaderAndFileList<THeader, TFile>(MemoryMappedViewAccessor view, long offset)
        where THeader : struct, ILSPKHeader 
        where TFile : struct, ILSPKFile
    {
        view.Read<THeader>(offset, out var header);

        Pak.Metadata = header.ToCommonHeader();

        if (MetadataOnly) return Pak;

        Pak.OpenStreams((int)Pak.Metadata.NumParts);

        if (Pak.Metadata.Version > 10)
        {
            Pak.Metadata.DataOffset = (uint)(offset + Marshal.SizeOf<THeader>());
            ReadCompressedFileList<TFile>(view, (long)Pak.Metadata.FileListOffset);
        }
        else
        {
            ReadFileList<TFile>(view, offset + Marshal.SizeOf<THeader>());
        }

        if (Pak.Metadata.Flags.HasFlag(PackageFlags.Solid) && Pak.Files.Count > 0)
        {
            UnpackSolidSegment(view);
        }

        return Pak;
    }

    private void UnpackSolidSegment(MemoryMappedViewAccessor view)
    {
        // Calculate compressed frame offset and bounds
        ulong totalUncompressedSize = 0;
        ulong totalSizeOnDisk = 0;
        ulong firstOffset = 0xffffffff;
        ulong lastOffset = 0;

        foreach (var entry in Pak.Files)
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

        if (firstOffset != Pak.Metadata.DataOffset + 7 || lastOffset - firstOffset != totalSizeOnDisk)
        {
            string msg = $"Incorrectly compressed solid archive; offsets {firstOffset}/{lastOffset}, bytes {totalSizeOnDisk}";
            throw new InvalidDataException(msg);
        }

        // Decompress all files as a single frame (solid)
        byte[] frame = new byte[lastOffset - Pak.Metadata.DataOffset];
        view.ReadArray(Pak.Metadata.DataOffset, frame, 0, (int)(lastOffset - Pak.Metadata.DataOffset));

        byte[] decompressed = Native.LZ4FrameCompressor.Decompress(frame);
        var decompressedStream = new MemoryStream(decompressed);

        // Update offsets to point to the decompressed chunk
        ulong offset = Pak.Metadata.DataOffset + 7;
        ulong compressedOffset = 0;
        foreach (var entry in Pak.Files)
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

    public Package ReadInternal(string path)
    {
        Pak = new Package(path);
        var view = Pak.MetadataView;

        // Check for v13 package headers
        var headerSize = view.ReadInt32(view.Capacity - 8);
        var signature = view.ReadUInt32(view.Capacity - 4);
        if (signature == PackageHeaderCommon.Signature)
        {
            return ReadHeaderAndFileList<LSPKHeader13, FileEntry10>(view, view.Capacity - headerSize);
        }

        // Check for v10 package headers
        signature = view.ReadUInt32(0);
        Int32 version;
        if (signature == PackageHeaderCommon.Signature)
        {
            version = view.ReadInt32(4);
            return version switch
            {
                10 => ReadHeaderAndFileList<LSPKHeader10, FileEntry10>(view, 4),
                15 => ReadHeaderAndFileList<LSPKHeader15, FileEntry15>(view, 4),
                16 => ReadHeaderAndFileList<LSPKHeader16, FileEntry15>(view, 4),
                18 => ReadHeaderAndFileList<LSPKHeader16, FileEntry18>(view, 4),
                _ => throw new InvalidDataException($"Package version v{version} not supported")
            };
        }

        // Check for v9 and v7 package headers
        version = view.ReadInt32(0);
        if (version == 7 || version == 9)
        {
            return ReadHeaderAndFileList<LSPKHeader7, FileEntry7>(view, 0);
        }

        throw new NotAPackageException("No valid signature found in package file");
    }

    public Package Read(string path, bool metadataOnly = false)
    {
        MetadataOnly = metadataOnly;

        try
        {
            return ReadInternal(path);
        }
        catch (Exception)
        {
            Pak?.Dispose();
            throw;
        }
    }
}
