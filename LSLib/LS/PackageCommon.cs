using System.IO.MemoryMappedFiles;
using LSLib.LS.Enums;

namespace LSLib.LS;

public class PackagedFileInfo : PackagedFileInfoCommon
{
    public Package Package;
    public MemoryMappedFile PackageFile;
    public MemoryMappedViewAccessor PackageView;
    public bool Solid;
    public ulong SolidOffset;
    public Stream SolidStream;

    public UInt64 Size() => Flags.Method() == CompressionMethod.None ? SizeOnDisk : UncompressedSize;

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
            return CompressionHelpers.Decompress(PackageFile, PackageView, (long)OffsetInFile, (int)SizeOnDisk, (int)UncompressedSize, Flags);
        }
    }

    internal static PackagedFileInfo CreateFromEntry(Package package, ILSPKFile entry, MemoryMappedFile file, MemoryMappedViewAccessor view)
    {
        var info = new PackagedFileInfo
        {
            Package = package,
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

    public void UncompressPackage(Package package, string outputPath, Func<PackagedFileInfo, bool> filter = null)
    {
        if (outputPath.Length > 0 && !outputPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.InvariantCultureIgnoreCase))
        {
            outputPath += Path.DirectorySeparatorChar;
        }

        List<PackagedFileInfo> files = package.Files;

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

    public void UncompressPackage(string packagePath, string outputPath, Func<PackagedFileInfo, bool> filter = null)
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
        using var writer = PackageWriterFactory.Create(build, packagePath);
        writer.WriteProgress += WriteProgressUpdate;
        writer.Write();
    }
}
