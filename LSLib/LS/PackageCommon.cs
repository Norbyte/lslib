using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using LSLib.LS.Enums;
using LSLib.Native;

namespace LSLib.LS
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct LSPKHeader7
    {
        public UInt32 Version;
        public UInt32 DataOffset;
        public UInt32 NumParts;
        public UInt32 FileListSize;
        public Byte LittleEndian;
        public UInt32 NumFiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct FileEntry7
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] Name;

        public UInt32 OffsetInFile;
        public UInt32 SizeOnDisk;
        public UInt32 UncompressedSize;
        public UInt32 ArchivePart;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct LSPKHeader10
    {
        public UInt32 Version;
        public UInt32 DataOffset;
        public UInt32 FileListSize;
        public UInt16 NumParts;
        public Byte Flags;
        public Byte Priority;
        public UInt32 NumFiles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct LSPKHeader13
    {
        public UInt32 Version;
        public UInt32 FileListOffset;
        public UInt32 FileListSize;
        public UInt16 NumParts;
        public Byte Flags;
        public Byte Priority;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Md5;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct LSPKHeader15
    {
        public UInt32 Version;
        public UInt64 FileListOffset;
        public UInt32 FileListSize;
        public Byte Flags;
        public Byte Priority;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Md5;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct LSPKHeader16
    {
        public UInt32 Version;
        public UInt64 FileListOffset;
        public UInt32 FileListSize;
        public Byte Flags;
        public Byte Priority;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Md5;

        public UInt16 NumParts;
    }

    [Flags]
    public enum PackageFlags
    {
        /// <summary>
        /// Allow memory-mapped access to the files in this archive.
        /// </summary>
        AllowMemoryMapping = 0x02,
        /// <summary>
        /// All files are compressed into a single LZ4 stream
        /// </summary>
        Solid = 0x04,
        /// <summary>
        /// Archive contents should be preloaded on game startup.
        /// </summary>
        Preload = 0x08
    };

    public class PackageMetadata
    {
        /// <summary>
        /// Package flags bitmask. Allowed values are in the PackageFlags enumeration.
        /// </summary>
        public PackageFlags Flags = 0;
        /// <summary>
        /// Load priority. Packages with higher priority are loaded later (i.e. they override earlier packages).
        /// </summary>
        public Byte Priority = 0;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct FileEntry13
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] Name;

        public UInt32 OffsetInFile;
        public UInt32 SizeOnDisk;
        public UInt32 UncompressedSize;
        public UInt32 ArchivePart;
        public UInt32 Flags;
        public UInt32 Crc;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct FileEntry15
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] Name;

        public UInt64 OffsetInFile;
        public UInt64 SizeOnDisk;
        public UInt64 UncompressedSize;
        public UInt32 ArchivePart;
        public UInt32 Flags;
        public UInt32 Crc;
        public UInt32 Unknown2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct FileEntry18
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] Name;

        public UInt32 OffsetInFile1;
        public UInt16 OffsetInFile2;
        public Byte ArchivePart;
        public Byte Flags;
        public UInt32 SizeOnDisk;
        public UInt32 UncompressedSize;
    }

    public abstract class AbstractFileInfo
    {
        public String Name;

        public abstract UInt64 Size();
        public abstract UInt32 CRC();
        public abstract Stream MakeStream();
        public abstract void ReleaseStream();
        public abstract bool IsDeletion();
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

    public class PackagedFileInfo : AbstractFileInfo, IDisposable
    {
        public UInt32 ArchivePart;
        public UInt32 Crc;
        public UInt32 Flags;
        public UInt64 OffsetInFile;
        public Stream PackageStream;
        public UInt64 SizeOnDisk;
        public UInt64 UncompressedSize;
        public bool Solid;
        public UInt32 SolidOffset;
        public Stream SolidStream;
        private Stream _uncompressedStream;

        public void Dispose()
        {
            ReleaseStream();
        }

        public override UInt64 Size() => (Flags & 0x0F) == 0 ? SizeOnDisk : UncompressedSize;

        public override UInt32 CRC() => Crc;

        public override Stream MakeStream()
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
                throw new InvalidDataException($"File '{Name}' is over 2GB ({SizeOnDisk} bytes), which is not supported yet!");
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
                UInt32 computedCrc = Crc32.Compute(compressed, 0);
                if (computedCrc != Crc)
                {
                    string msg = $"CRC check failed on file '{Name}', archive is possibly corrupted. Expected {Crc,8:X}, got {computedCrc,8:X}";
                    throw new InvalidDataException(msg);
                }
            }

            if (Solid)
            {
                SolidStream.Seek(SolidOffset, SeekOrigin.Begin);
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

        public override void ReleaseStream()
        {
            if (_uncompressedStream == null)
            {
                return;
            }

            _uncompressedStream.Dispose();
            _uncompressedStream = null;
        }

        internal static PackagedFileInfo CreateFromEntry(FileEntry13 entry, Stream dataStream)
        {
            var info = new PackagedFileInfo
            {
                PackageStream = dataStream,
                OffsetInFile = entry.OffsetInFile,
                SizeOnDisk = entry.SizeOnDisk,
                UncompressedSize = entry.UncompressedSize,
                ArchivePart = entry.ArchivePart,
                Flags = entry.Flags,
                Crc = entry.Crc,
                Solid = false
            };

            int nameLen;
            for (nameLen = 0; nameLen < entry.Name.Length && entry.Name[nameLen] != 0; nameLen++)
            {
            }
            info.Name = Encoding.UTF8.GetString(entry.Name, 0, nameLen);

            uint compressionMethod = entry.Flags & 0x0F;
            if (compressionMethod > 2 || (entry.Flags & ~0x7F) != 0)
            {
                string msg = $"File '{info.Name}' has unsupported flags: {entry.Flags}";
                throw new InvalidDataException(msg);
            }

            return info;
        }

        internal static PackagedFileInfo CreateFromEntry(FileEntry15 entry, Stream dataStream)
        {
            var info = new PackagedFileInfo
            {
                PackageStream = dataStream,
                OffsetInFile = entry.OffsetInFile,
                SizeOnDisk = entry.SizeOnDisk,
                UncompressedSize = entry.UncompressedSize,
                ArchivePart = entry.ArchivePart,
                Flags = entry.Flags,
                Crc = entry.Crc,
                Solid = false
            };

            int nameLen;
            for (nameLen = 0; nameLen < entry.Name.Length && entry.Name[nameLen] != 0; nameLen++)
            {
            }
            info.Name = Encoding.UTF8.GetString(entry.Name, 0, nameLen);

            uint compressionMethod = entry.Flags & 0x0F;
            if (compressionMethod > 2 || (entry.Flags & ~0x7F) != 0)
            {
                string msg = $"File '{info.Name}' has unsupported flags: {entry.Flags}";
                throw new InvalidDataException(msg);
            }

            return info;
        }

        internal static PackagedFileInfo CreateFromEntry(FileEntry18 entry, Stream dataStream)
        {
            var info = new PackagedFileInfo
            {
                PackageStream = dataStream,
                OffsetInFile = entry.OffsetInFile1 | ((ulong)entry.OffsetInFile2 << 32),
                SizeOnDisk = entry.SizeOnDisk,
                UncompressedSize = entry.UncompressedSize,
                ArchivePart = entry.ArchivePart,
                Flags = entry.Flags,
                Crc = 0,
                Solid = false
            };

            int nameLen;
            for (nameLen = 0; nameLen < entry.Name.Length && entry.Name[nameLen] != 0; nameLen++)
            {
            }
            info.Name = Encoding.UTF8.GetString(entry.Name, 0, nameLen);

            uint compressionMethod = (uint)entry.Flags & 0x0F;
            if (compressionMethod > 2 || ((uint)entry.Flags & ~0x7F) != 0)
            {
                string msg = $"File '{info.Name}' has unsupported flags: {entry.Flags}";
                throw new InvalidDataException(msg);
            }

            return info;
        }

        internal static PackagedFileInfo CreateSolidFromEntry(FileEntry13 entry, Stream dataStream, uint solidOffset, Stream solidStream)
        {
            var info = CreateFromEntry(entry, dataStream);
            info.Solid = true;
            info.SolidOffset = solidOffset;
            info.SolidStream = solidStream;
            return info;
        }

        internal static PackagedFileInfo CreateFromEntry(FileEntry7 entry, Stream dataStream)
        {
            var info = new PackagedFileInfo
            {
                PackageStream = dataStream
            };

            int nameLen;
            for (nameLen = 0; nameLen < entry.Name.Length && entry.Name[nameLen] != 0; nameLen++)
            {
            }
            info.Name = Encoding.UTF8.GetString(entry.Name, 0, nameLen);

            info.OffsetInFile = entry.OffsetInFile;
            info.SizeOnDisk = entry.SizeOnDisk;
            info.UncompressedSize = entry.UncompressedSize;
            info.ArchivePart = entry.ArchivePart;
            info.Crc = 0;

            info.Flags = entry.UncompressedSize > 0 ? BinUtils.MakeCompressionFlags(CompressionMethod.Zlib, CompressionLevel.DefaultCompression) : (uint) 0;

            return info;
        }

        internal FileEntry7 MakeEntryV7()
        {
            var entry = new FileEntry7
            {
                Name = new byte[256]
            };
            byte[] encodedName = Encoding.UTF8.GetBytes(Name.Replace('\\', '/'));
            Array.Copy(encodedName, entry.Name, encodedName.Length);

            entry.OffsetInFile = (uint)OffsetInFile;
            entry.SizeOnDisk = (uint)SizeOnDisk;
            entry.UncompressedSize = (Flags & 0x0F) == 0 ? 0 : (uint)UncompressedSize;
            entry.ArchivePart = ArchivePart;
            return entry;
        }

        internal FileEntry13 MakeEntryV13()
        {
            var entry = new FileEntry13
            {
                Name = new byte[256]
            };
            byte[] encodedName = Encoding.UTF8.GetBytes(Name.Replace('\\', '/'));
            Array.Copy(encodedName, entry.Name, encodedName.Length);

            entry.OffsetInFile = (uint)OffsetInFile;
            entry.SizeOnDisk = (uint)SizeOnDisk;
            entry.UncompressedSize = (Flags & 0x0F) == 0 ? 0 : (uint)UncompressedSize;
            entry.ArchivePart = ArchivePart;
            entry.Flags = Flags;
            entry.Crc = Crc;
            return entry;
        }

        internal FileEntry15 MakeEntryV15()
        {
            var entry = new FileEntry15
            {
                Name = new byte[256],
                OffsetInFile = OffsetInFile,
                SizeOnDisk = SizeOnDisk,
                UncompressedSize = (Flags & 0x0F) == 0 ? 0 : UncompressedSize,
                Flags = Flags,
                Crc = Crc,
                ArchivePart = ArchivePart,
                Unknown2 = 0
            };
            byte[] encodedName = Encoding.UTF8.GetBytes(Name.Replace('\\', '/'));
            Array.Copy(encodedName, entry.Name, encodedName.Length);

            return entry;
        }

        internal FileEntry18 MakeEntryV18()
        {
            var entry = new FileEntry18
            {
                Name = new byte[256],
                OffsetInFile1 = (uint)(OffsetInFile & 0xffffffff),
                OffsetInFile2 = (ushort)((OffsetInFile >> 32) & 0xffff),
                SizeOnDisk = (uint)SizeOnDisk,
                UncompressedSize = (Flags & 0x0F) == 0 ? 0 : (uint)UncompressedSize,
                Flags = (byte)Flags,
                ArchivePart = (byte)ArchivePart
            };
            byte[] encodedName = Encoding.UTF8.GetBytes(Name.Replace('\\', '/'));
            Array.Copy(encodedName, entry.Name, encodedName.Length);

            return entry;
        }

        public override bool IsDeletion()
        {
            return OffsetInFile == 0xdeadbeefdeadbeef;
        }
    }

    public class FilesystemFileInfo : AbstractFileInfo, IDisposable
    {
        public long CachedSize;
        public string FilesystemPath;
        private FileStream _stream;

        public void Dispose()
        {
            ReleaseStream();
        }

        public override UInt64 Size() => (UInt64) CachedSize;

        public override UInt32 CRC() => throw new NotImplementedException("!");

        public override Stream MakeStream() => _stream ?? (_stream = File.Open(FilesystemPath, FileMode.Open, FileAccess.Read, FileShare.Read));

        public override void ReleaseStream()
        {
            if (_stream == null)
            {
                return;
            }
            _stream.Dispose();
            _stream = null;
        }

        public static FilesystemFileInfo CreateFromEntry(string filesystemPath, string name)
        {
            var info = new FilesystemFileInfo
            {
                Name = name,
                FilesystemPath = filesystemPath
            };

            var fsInfo = new FileInfo(filesystemPath);
            info.CachedSize = fsInfo.Length;
            return info;
        }

        public override bool IsDeletion()
        {
            return false;
        }
    }

    public class StreamFileInfo : AbstractFileInfo
    {
        public Stream Stream;

        public override UInt64 Size() => (UInt64) Stream.Length;

        public override UInt32 CRC() => throw new NotImplementedException("!");

        public override Stream MakeStream() => Stream;

        public override void ReleaseStream()
        {
        }

        public static StreamFileInfo CreateFromStream(Stream stream, string name)
        {
            var info = new StreamFileInfo
            {
                Name = name,
                Stream = stream
            };
            return info;
        }

        public override bool IsDeletion()
        {
            return false;
        }
    }

    public class Package
    {
        public const PackageVersion CurrentVersion = PackageVersion.V18;

        public static byte[] Signature =
        {
            0x4C,
            0x53,
            0x50,
            0x4B
        };

        public PackageMetadata Metadata = new PackageMetadata();
        public List<AbstractFileInfo> Files = new List<AbstractFileInfo>();
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
        public delegate void ProgressUpdateDelegate(string status, long numerator, long denominator, AbstractFileInfo file);

        public ProgressUpdateDelegate ProgressUpdate = delegate { };

        private void WriteProgressUpdate(AbstractFileInfo file, long numerator, long denominator)
        {
            ProgressUpdate(file.Name, numerator, denominator, file);
        }

        public void UncompressPackage(Package package, string outputPath, Func<AbstractFileInfo, bool> filter = null)
        {
            if (outputPath.Length > 0 && !outputPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                outputPath += Path.DirectorySeparatorChar;
            }

            List<AbstractFileInfo> files = package.Files;

            if (filter != null)
            {
                files = files.FindAll(obj => filter(obj));
            }

            long totalSize = files.Sum(p => (long)p.Size());
            long currentSize = 0;

            var buffer = new byte[32768];
            foreach (AbstractFileInfo file in files)
            {
                ProgressUpdate(file.Name, currentSize, totalSize, file);
                currentSize += (long)file.Size();

                if (file.IsDeletion()) continue;

                string outPath = outputPath + file.Name;

                FileManager.TryToCreateDirectory(outPath);

                Stream inStream = file.MakeStream();

                try
                {
                    using (var inReader = new BinaryReader(inStream))
                    {
                        using (FileStream outFile = File.Open(outPath, FileMode.Create, FileAccess.Write))
                        {
                            int read;
                            while ((read = inReader.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                outFile.Write(buffer, 0, read);
                            }
                        }
                    }
                }
                finally
                {
                    file.ReleaseStream();
                }
            }
        }

        public void UncompressPackage(string packagePath, string outputPath, Func<AbstractFileInfo, bool> filter = null)
        {
            ProgressUpdate("Reading package headers ...", 0, 1, null);
            using (var reader = new PackageReader(packagePath))
            {
                Package package = reader.Read();
                UncompressPackage(package, outputPath, filter);
            }
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
            using (var writer = new PackageWriter(package, packagePath))
            {
                writer.WriteProgress += WriteProgressUpdate;
                writer.Version = options.Version;
                writer.Compression = options.Compression;
                writer.CompressionLevel = options.FastCompression ? CompressionLevel.FastCompression : CompressionLevel.DefaultCompression;
                writer.Write();
            }
        }
    }
}
