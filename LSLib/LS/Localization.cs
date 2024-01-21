using System.Xml;

namespace LSLib.LS;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LocaHeader
{
    public static UInt32 DefaultSignature = 0x41434f4c; // 'LOCA'

    public UInt32 Signature;
    public UInt32 NumEntries;
    public UInt32 TextsOffset;
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LocaEntry
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public byte[] Key;

    public UInt16 Version;
    public UInt32 Length;

    public string KeyString
    {
        get
        {
            int nameLen;
            for (nameLen = 0; nameLen < Key.Length && Key[nameLen] != 0; nameLen++) ;
            return Encoding.UTF8.GetString(Key, 0, nameLen);
        }

        set
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            Key = new byte[64];
            Array.Clear(Key, 0, Key.Length);
            Array.Copy(bytes, Key, bytes.Length);
        }
    }
}


public class LocalizedText
{
    public string Key;
    public UInt16 Version;
    public string Text;
}


public class LocaResource
{
    public List<LocalizedText> Entries;
}


public class LocaReader(Stream stream) : IDisposable
{
    private readonly Stream Stream = stream;

    public void Dispose()
    {
        Stream.Dispose();
    }

    public LocaResource Read()
    {
        using var reader = new BinaryReader(Stream);
        var loca = new LocaResource
        {
            Entries = []
        };
        var header = BinUtils.ReadStruct<LocaHeader>(reader);

        if (header.Signature != (ulong)LocaHeader.DefaultSignature)
        {
            throw new InvalidDataException("Incorrect signature in localization file");
        }

        var entries = new LocaEntry[header.NumEntries];
        BinUtils.ReadStructs<LocaEntry>(reader, entries);

        if (Stream.Position != header.TextsOffset)
        {
            Stream.Position = header.TextsOffset;
        }

        foreach (var entry in entries)
        {
            var text = Encoding.UTF8.GetString(reader.ReadBytes((int)entry.Length - 1));
            loca.Entries.Add(new LocalizedText
            {
                Key = entry.KeyString,
                Version = entry.Version,
                Text = text
            });
            reader.ReadByte();
        }

        return loca;
    }
}


public class LocaWriter(Stream stream)
{
    private readonly Stream stream = stream;

    public void Write(LocaResource res)
    {
        using var writer = new BinaryWriter(stream);
        var header = new LocaHeader
        {
            Signature = LocaHeader.DefaultSignature,
            NumEntries = (uint)res.Entries.Count,
            TextsOffset = (uint)(Marshal.SizeOf(typeof(LocaHeader)) + Marshal.SizeOf(typeof(LocaEntry)) * res.Entries.Count)
        };
        BinUtils.WriteStruct<LocaHeader>(writer, ref header);

        var entries = new LocaEntry[header.NumEntries];
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = res.Entries[i];
            entries[i] = new LocaEntry
            {
                KeyString = entry.Key,
                Version = entry.Version,
                Length = (uint)Encoding.UTF8.GetByteCount(entry.Text) + 1
            };
        }

        BinUtils.WriteStructs<LocaEntry>(writer, entries);

        foreach (var entry in res.Entries)
        {
            var bin = Encoding.UTF8.GetBytes(entry.Text);
            writer.Write(bin);
            writer.Write((Byte)0);
        }
    }
}
public class LocaXmlReader(Stream stream) : IDisposable
{
    private readonly Stream stream = stream;
    private XmlReader reader;
    private LocaResource resource;

    public void Dispose()
    {
        stream.Dispose();
    }

    private void ReadElement()
    {
        switch (reader.Name)
        {
            case "contentList":
                // Root element
                break;

            case "content":
                var key = reader["contentuid"];
                var version = reader["version"] != null ? UInt16.Parse(reader["version"]) : (UInt16)1;
                var text = reader.ReadString();

                resource.Entries.Add(new LocalizedText
                {
                    Key = key,
                    Version = version,
                    Text = text
                });
                break;

            default:
                throw new InvalidFormatException(String.Format("Unknown element encountered: {0}", reader.Name));
        }
    }

    public LocaResource Read()
    {
        resource = new LocaResource
        {
            Entries = []
        };

        using (this.reader = XmlReader.Create(stream))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    ReadElement();
                }
            }
        }

        return resource;
    }
}


public class LocaXmlWriter(Stream stream)
{
    private readonly Stream stream = stream;

    public void Write(LocaResource res)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "\t"
        };

        using var writer = XmlWriter.Create(stream, settings);
        writer.WriteStartElement("contentList");

        foreach (var entry in res.Entries)
        {
            writer.WriteStartElement("content");
            writer.WriteAttributeString("contentuid", entry.Key);
            writer.WriteAttributeString("version", entry.Version.ToString());
            writer.WriteString(entry.Text);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.Flush();
    }
}

public enum LocaFormat
{
    Loca,
    Xml
};

public static class LocaUtils
{
    public static LocaFormat ExtensionToFileFormat(string path)
    {
        var extension = Path.GetExtension(path).ToLower();

        return extension switch
        {
            ".loca" => LocaFormat.Loca,
            ".xml" => LocaFormat.Xml,
            _ => throw new ArgumentException("Unrecognized file extension: " + extension),
        };
    }

    public static LocaResource Load(string inputPath)
    {
        return Load(inputPath, ExtensionToFileFormat(inputPath));
    }

    public static LocaResource Load(string inputPath, LocaFormat format)
    {
        using var stream = File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Load(stream, format);
    }

    public static LocaResource Load(Stream stream, LocaFormat format)
    {
        switch (format)
        {
            case LocaFormat.Loca:
                {
                    using var reader = new LocaReader(stream);
                    return reader.Read();
                }

            case LocaFormat.Xml:
                {
                    using var reader = new LocaXmlReader(stream);
                    return reader.Read();
                }

            default:
                throw new ArgumentException("Invalid loca format");
        }
    }

    public static void Save(LocaResource resource, string outputPath)
    {
        Save(resource, outputPath, ExtensionToFileFormat(outputPath));
    }

    public static void Save(LocaResource resource, string outputPath, LocaFormat format)
    {
        FileManager.TryToCreateDirectory(outputPath);

        using var file = File.Open(outputPath, FileMode.Create, FileAccess.Write);
        switch (format)
        {
            case LocaFormat.Loca:
                {
                    var writer = new LocaWriter(file);
                    writer.Write(resource);
                    break;
                }

            case LocaFormat.Xml:
                {
                    var writer = new LocaXmlWriter(file);
                    writer.Write(resource);
                    break;
                }

            default:
                throw new ArgumentException("Invalid loca format");
        }
    }
}
