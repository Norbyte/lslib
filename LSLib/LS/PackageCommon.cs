using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading.Tasks;
using LSLib.LS.Enums;

namespace LSLib.LS;

public interface IAbstractFileInfo
{
    public abstract String GetName();
    public abstract UInt64 Size();
    public abstract UInt32 CRC();
    public abstract Stream CreateContentReader();
    public abstract bool IsDeletion();

    public string Name { get { return GetName(); } }
}


public class PackagedFileInfo : PackagedFileInfoCommon, IAbstractFileInfo
{
    public MemoryMappedFile PackageFile;
    public MemoryMappedViewAccessor PackageView;
    public bool Solid;
    public ulong SolidOffset;
    public Stream SolidStream;

    public String GetName() => FileName;

    public UInt64 Size() => Flags.Method() == CompressionMethod.None ? SizeOnDisk : UncompressedSize;

    public UInt32 CRC() => Crc;

    public Stream CreateContentReader()
    {
        if (IsDeletion())
        {
            throw new InvalidOperationException("Cannot open file stream for a deleted file");
        }

        if (Solid)
        {
            SolidStream.Seek((long)SolidOffset, SeekOrigin.Begin);
            return new ReadOnlySubstream(SolidStream, (long)SolidOffset, (long)UncompressedSize);
        }
        else
        {
            return BinUtils.Decompress(PackageFile, PackageView, (long)OffsetInFile, (int)SizeOnDisk, (int)UncompressedSize, Flags);
        }
    }

    internal static PackagedFileInfo CreateFromEntry(ILSPKFile entry, MemoryMappedFile file, MemoryMappedViewAccessor view)
    {
        var info = new PackagedFileInfo
        {
            PackageFile = file,
            PackageView = view,
            Solid = false
        };

        entry.ToCommon(info);
        return info;
    }

    internal void MakeSolid(ulong solidOffset, Stream solidStream)
    {
        Solid = true;
        SolidOffset = solidOffset;
        SolidStream = solidStream;
    }

    public bool IsDeletion()
    {
        return (OffsetInFile & 0x0000ffffffffffff) == 0xbeefdeadbeef;
    }
}

public class FilesystemFileInfo : IAbstractFileInfo
{
    public long CachedSize;
    public string FilesystemPath;
    public string FileName;

    public String GetName() => FileName;

    public UInt64 Size() => (UInt64) CachedSize;

    public UInt32 CRC() => throw new NotImplementedException("!");

    public Stream CreateContentReader() => File.Open(FilesystemPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

    public static FilesystemFileInfo CreateFromEntry(string filesystemPath, string name)
    {
        var info = new FilesystemFileInfo
        {
            FileName = name,
            FilesystemPath = filesystemPath
        };

        var fsInfo = new FileInfo(filesystemPath);
        info.CachedSize = fsInfo.Length;
        return info;
    }

    public bool IsDeletion()
    {
        return false;
    }
}

public class StreamFileInfo : IAbstractFileInfo
{
    public Stream Stream;
    public String FileName;

    public String GetName() => FileName;

    public UInt64 Size() => (UInt64) Stream.Length;

    public UInt32 CRC() => throw new NotImplementedException("!");

    public Stream CreateContentReader() => Stream;

    public static StreamFileInfo CreateFromStream(Stream stream, string name)
    {
        var info = new StreamFileInfo
        {
            FileName = name,
            Stream = stream
        };
        return info;
    }

    public bool IsDeletion()
    {
        return false;
    }
}

public class PackageBuildInputFile
{
    public string Path;
    public string FilesystemPath;
    public byte[] Body;

    public Stream MakeInputStream()
    {
        if (Body != null)
        {
            return new MemoryStream(Body);
        }
        else
        {
            return new FileStream(FilesystemPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
    }

    public long Size()
    {
        if (Body != null)
        {
            return Body.Length;
        }
        else
        {
            return new FileInfo(FilesystemPath).Length;
        }
    }

    public static PackageBuildInputFile CreateFromBlob(byte[] body, string path)
    {
        return new PackageBuildInputFile
        {
            Path = path,
            Body = body
        };
    }

    public static PackageBuildInputFile CreateFromFilesystem(string filesystemPath, string path)
    {
        return new PackageBuildInputFile
        {
            Path = path,
            FilesystemPath = filesystemPath
        };
    }
}

public class PackageBuildData
{
    public PackageVersion Version = PackageHeaderCommon.CurrentVersion;
    public CompressionMethod Compression = CompressionMethod.None;
    public LSCompressionLevel CompressionLevel = LSCompressionLevel.Default;
    public PackageFlags Flags = 0;
    public byte Priority = 0;
    // Calculate full archive checksum?
    public bool Hash = false;
    public List<PackageBuildInputFile> Files = [];
}

public class Packager
{
    public delegate void ProgressUpdateDelegate(string status, long numerator, long denominator);

    public ProgressUpdateDelegate ProgressUpdate = delegate { };

    private void WriteProgressUpdate(PackageBuildInputFile file, long numerator, long denominator)
    {
        ProgressUpdate(file.Path, numerator, denominator);
    }

    public void UncompressPackage(Package package, string outputPath, Func<IAbstractFileInfo, bool> filter = null)
    {
        if (outputPath.Length > 0 && !outputPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.InvariantCultureIgnoreCase))
        {
            outputPath += Path.DirectorySeparatorChar;
        }

        List<IAbstractFileInfo> files = package.Files;

        if (filter != null)
        {
            files = files.FindAll(obj => filter(obj));
        }

        long totalSize = files.Sum(p => (long)p.Size());
        long currentSize = 0;

        foreach (var file in files)
        {
            ProgressUpdate(file.Name, currentSize, totalSize);
            currentSize += (long)file.Size();

            if (file.IsDeletion()) continue;

            string outPath = Path.Join(outputPath, file.Name);

            FileManager.TryToCreateDirectory(outPath);

            using var inStream = file.CreateContentReader();
            using var outFile = File.Open(outPath, FileMode.Create, FileAccess.Write);
            inStream.CopyTo(outFile);
        }
    }

    public void UncompressPackage(string packagePath, string outputPath, Func<IAbstractFileInfo, bool> filter = null)
    {
        ProgressUpdate("Reading package headers ...", 0, 1);
        var reader = new PackageReader();
        using var package = reader.Read(packagePath);
        UncompressPackage(package, outputPath, filter);
    }

    private static void AddFilesFromPath(PackageBuildData build, string path)
    {
        if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.InvariantCultureIgnoreCase))
        {
            path += Path.DirectorySeparatorChar;
        }

        foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
        {
            var name = Path.GetRelativePath(path, file);
            build.Files.Add(PackageBuildInputFile.CreateFromFilesystem(file, name));
        }
    }

    public async Task CreatePackage(string packagePath, string inputPath, PackageBuildData build)
    {
        FileManager.TryToCreateDirectory(packagePath);

        ProgressUpdate("Enumerating files ...", 0, 1);
        AddFilesFromPath(build, inputPath);

        ProgressUpdate("Creating archive ...", 0, 1);
        using var writer = new PackageWriter(build, packagePath);
        writer.WriteProgress += WriteProgressUpdate;
        writer.Write();
    }
}
