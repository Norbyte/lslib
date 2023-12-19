using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using LSLib.LS.Enums;

namespace LSLib.LS;

public interface IAbstractFileInfo
{
    public abstract String GetName();
    public abstract UInt64 Size();
    public abstract UInt32 CRC();
    public abstract Stream MakeStream();
    public abstract void ReleaseStream();
    public abstract bool IsDeletion();

    public string Name { get { return GetName(); } }
}


public class UncompressedPackagedFileStream : Stream
{
    private readonly Stream PackageStream;
    private readonly PackagedFileInfo FileInfo;

    public UncompressedPackagedFileStream(Stream packageStream, PackagedFileInfo fileInfo)
    {
        PackageStream = packageStream;
        FileInfo = fileInfo;
        PackageStream.Seek((long)fileInfo.OffsetInFile, SeekOrigin.Begin);
        
        if ((CompressionMethod)(FileInfo.Flags & 0x0F) != CompressionMethod.None)
        {
            throw new ArgumentException("We only support uncompressed files!");
        }
    }

    public override bool CanRead { get { return true; } }
    public override bool CanSeek { get { return false; } }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (PackageStream.Position < (long)FileInfo.OffsetInFile
            || PackageStream.Position > (long)FileInfo.OffsetInFile + (long)FileInfo.SizeOnDisk)
        {
            throw new Exception("Stream at unexpected position while reading packaged file?");
        }

        long readable = (long)FileInfo.SizeOnDisk - Position;
        int bytesToRead = (readable < count) ? (int)readable : count;
        return PackageStream.Read(buffer, offset, bytesToRead);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }


    public override long Position
    {
        get { return PackageStream.Position - (long)FileInfo.OffsetInFile; }
        set { throw new NotSupportedException(); }
    }

    public override bool CanTimeout { get { return PackageStream.CanTimeout; } }
    public override bool CanWrite { get { return false; } }
    public override long Length { get { return (long)FileInfo.SizeOnDisk; } }
    public override void SetLength(long value) { throw new NotSupportedException(); }
    public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
    public override void Flush() { }
}

public class PackagedFileInfo : PackagedFileInfoCommon, IAbstractFileInfo, IDisposable
{
    public Stream PackageStream;
    public bool Solid;
    public ulong SolidOffset;
    public Stream SolidStream;
    private Stream _uncompressedStream;

    public void Dispose()
    {
        ReleaseStream();
    }

    public String GetName() => FileName;

    public UInt64 Size() => (Flags & 0x0F) == 0 ? SizeOnDisk : UncompressedSize;

    public UInt32 CRC() => Crc;

    public Stream MakeStream()
    {
        if (IsDeletion())
        {
            throw new InvalidOperationException("Cannot open file stream for a deleted file");
        }

        if (_uncompressedStream != null)
        {
            return _uncompressedStream;
        }

        if ((CompressionMethod)(Flags & 0x0F) == CompressionMethod.None && !Solid)
        {
            // Use direct stream read for non-compressed files
            _uncompressedStream = new UncompressedPackagedFileStream(PackageStream, this);
            return _uncompressedStream;
        }

        if (SizeOnDisk > 0x7fffffff)
        {
            throw new InvalidDataException($"File '{FileName}' is over 2GB ({SizeOnDisk} bytes), which is not supported yet!");
        }

        var compressed = new byte[SizeOnDisk];

        PackageStream.Seek((long)OffsetInFile, SeekOrigin.Begin);
        int readSize = PackageStream.Read(compressed, 0, (int)SizeOnDisk);
        if (readSize != (long)SizeOnDisk)
        {
            string msg = $"Failed to read {SizeOnDisk} bytes from archive (only got {readSize})";
            throw new InvalidDataException(msg);
        }

        if (Crc != 0)
        {
            UInt32 computedCrc = Crc32.HashToUInt32(compressed);
            if (computedCrc != Crc)
            {
                string msg = $"CRC check failed on file '{FileName}', archive is possibly corrupted. Expected {Crc,8:X}, got {computedCrc,8:X}";
                throw new InvalidDataException(msg);
            }
        }

        if (Solid)
        {
            SolidStream.Seek((long)SolidOffset, SeekOrigin.Begin);
            byte[] uncompressed = new byte[UncompressedSize];
            SolidStream.Read(uncompressed, 0, (int)UncompressedSize);
            _uncompressedStream = new MemoryStream(uncompressed);
        }
        else
        {
            byte[] uncompressed = BinUtils.Decompress(compressed, (int)Size(), (byte)Flags);
            _uncompressedStream = new MemoryStream(uncompressed);
        }

        return _uncompressedStream;
    }

    public void ReleaseStream()
    {
        if (_uncompressedStream == null)
        {
            return;
        }

        _uncompressedStream.Dispose();
        _uncompressedStream = null;
    }

    internal static PackagedFileInfo CreateFromEntry(ILSPKFile entry, Stream dataStream)
    {
        var info = new PackagedFileInfo
        {
            PackageStream = dataStream,
            Solid = false
        };

        entry.ToCommon(info);

        var compressionMethod = info.Flags & 0x0F;
        if (compressionMethod > 2 || (info.Flags & ~0x7F) != 0)
        {
            string msg = $"File '{info.FileName}' has unsupported flags: {info.Flags}";
            throw new InvalidDataException(msg);
        }

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

public class FilesystemFileInfo : IAbstractFileInfo, IDisposable
{
    public long CachedSize;
    public string FilesystemPath;
    public string FileName;
    private FileStream _stream;

    public void Dispose()
    {
        ReleaseStream();
    }

    public String GetName() => FileName;

    public UInt64 Size() => (UInt64) CachedSize;

    public UInt32 CRC() => throw new NotImplementedException("!");

    public Stream MakeStream() => _stream ??= File.Open(FilesystemPath, FileMode.Open, FileAccess.Read, FileShare.Read);

    public void ReleaseStream()
    {
        _stream?.Dispose();
        _stream = null;
    }

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

    public Stream MakeStream() => Stream;

    public void ReleaseStream()
    {
    }

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

public class Package
{
    public const PackageVersion CurrentVersion = PackageVersion.V18;

    public readonly static byte[] Signature = [ 0x4C, 0x53, 0x50, 0x4B ];

    public PackageHeaderCommon Metadata = new();
    public List<IAbstractFileInfo> Files = [];
    public PackageVersion Version;

    public static string MakePartFilename(string path, int part)
    {
        string dirName = Path.GetDirectoryName(path);
        string baseName = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        return $"{dirName}/{baseName}_{part}{extension}";
    }
}

public class PackageCreationOptions
{
    public PackageVersion Version = PackageVersion.V16;
    public CompressionMethod Compression = CompressionMethod.None;
    public bool FastCompression = true;
    public PackageFlags Flags = 0;
    public byte Priority = 0;
}

public class Packager
{
    public delegate void ProgressUpdateDelegate(string status, long numerator, long denominator, IAbstractFileInfo file);

    public ProgressUpdateDelegate ProgressUpdate = delegate { };

    private void WriteProgressUpdate(IAbstractFileInfo file, long numerator, long denominator)
    {
        ProgressUpdate(file.Name, numerator, denominator, file);
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

        var buffer = new byte[32768];
        foreach (var file in files)
        {
            ProgressUpdate(file.Name, currentSize, totalSize, file);
            currentSize += (long)file.Size();

            if (file.IsDeletion()) continue;

            string outPath = Path.Join(outputPath, file.Name);

            FileManager.TryToCreateDirectory(outPath);

            Stream inStream = file.MakeStream();

            try
            {
                using var inReader = new BinaryReader(inStream);
                using var outFile = File.Open(outPath, FileMode.Create, FileAccess.Write);
                int read;
                while ((read = inReader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    outFile.Write(buffer, 0, read);
                }
            }
            finally
            {
                file.ReleaseStream();
            }
        }
    }

    public void UncompressPackage(string packagePath, string outputPath, Func<IAbstractFileInfo, bool> filter = null)
    {
        ProgressUpdate("Reading package headers ...", 0, 1, null);
        using var reader = new PackageReader(packagePath);
        Package package = reader.Read();
        UncompressPackage(package, outputPath, filter);
    }

    private static Package CreatePackageFromPath(string path)
    {
        var package = new Package();

        if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.InvariantCultureIgnoreCase))
        {
            path += Path.DirectorySeparatorChar;
        }

        Dictionary<string, string> files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
            .ToDictionary(k => k.Replace(path, string.Empty), v => v);

        foreach (KeyValuePair<string, string> file in files)
			{
				FilesystemFileInfo fileInfo = FilesystemFileInfo.CreateFromEntry(file.Value, file.Key);
				package.Files.Add(fileInfo);
				fileInfo.Dispose();
			}

			return package;
    }

    public void CreatePackage(string packagePath, string inputPath, PackageCreationOptions options)
    {
        FileManager.TryToCreateDirectory(packagePath);

        ProgressUpdate("Enumerating files ...", 0, 1, null);
        Package package = CreatePackageFromPath(inputPath);
        package.Metadata.Flags = options.Flags;
        package.Metadata.Priority = options.Priority;

        ProgressUpdate("Creating archive ...", 0, 1, null);
        using var writer = new PackageWriter(package, packagePath);
        writer.WriteProgress += WriteProgressUpdate;
        writer.Version = options.Version;
        writer.Compression = options.Compression;
        writer.LSCompressionLevel = options.FastCompression ? LSCompressionLevel.FastCompression : LSCompressionLevel.DefaultCompression;
        writer.Write();
    }
}
