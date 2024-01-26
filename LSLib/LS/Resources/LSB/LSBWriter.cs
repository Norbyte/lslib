namespace LSLib.LS;

public class LSBWriter(Stream stream)
{
    private BinaryWriter writer;
    private Dictionary<string, UInt32> staticStrings = [];
    private UInt32 nextStaticStringId = 0;
    private UInt32 Version;

    public void Write(Resource rsrc)
    {
        Version = rsrc.Metadata.MajorVersion;
        using (this.writer = new BinaryWriter(stream))
        {
            var header = new LSBHeader
            {
                TotalSize = 0, // Total size of file, will be updater after we finished serializing
                BigEndian = 0, // Little-endian format
                Unknown = 0, // Unknown
                Metadata = rsrc.Metadata
            };

            if (rsrc.Metadata.MajorVersion >= 4)
            {
                header.Signature = BitConverter.ToUInt32(LSBHeader.SignatureBG3, 0);
            }
            else
            {
                header.Signature = LSBHeader.SignatureFW3;
            }

            BinUtils.WriteStruct(writer, ref header);

            CollectStaticStrings(rsrc);
            WriteStaticStrings();

            WriteRegions(rsrc);

            header.TotalSize = (UInt32)stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            BinUtils.WriteStruct(writer, ref header);
        }
    }

    private void WriteRegions(Resource rsrc)
    {
        writer.Write((UInt32)rsrc.Regions.Count);
        var regionMapOffset = stream.Position;
        foreach (var rgn in rsrc.Regions)
        {
            writer.Write(staticStrings[rgn.Key]);
            writer.Write((UInt32)0); // Offset of region, will be updater after we finished serializing
        }

        List<UInt32> regionPositions = [];
        foreach (var rgn in rsrc.Regions)
        {
            regionPositions.Add((UInt32)stream.Position);
            WriteNode(rgn.Value);
        }

        var endOffset = stream.Position;
        stream.Seek(regionMapOffset, SeekOrigin.Begin);
        foreach (var position in regionPositions)
        {
            stream.Seek(4, SeekOrigin.Current);
            writer.Write(position);
        }

        stream.Seek(endOffset, SeekOrigin.Begin);
    }

    private void WriteNode(Node node)
    {
        writer.Write(staticStrings[node.Name]);
        writer.Write((UInt32)node.Attributes.Count);
        writer.Write((UInt32)node.ChildCount);

        foreach (var attribute in node.Attributes)
        {
            writer.Write(staticStrings[attribute.Key]);
            writer.Write((UInt32)attribute.Value.Type);
            WriteAttribute(attribute.Value);
        }

        foreach (var children in node.Children)
        {
            foreach (var child in children.Value)
                WriteNode(child);
        }
    }

    private void WriteAttribute(NodeAttribute attr)
    {
        switch (attr.Type)
        {
            case AttributeType.String:
            case AttributeType.Path:
            case AttributeType.FixedString:
            case AttributeType.LSString:
                WriteString((string)attr.Value, true);
                break;

            case AttributeType.WString:
            case AttributeType.LSWString:
                WriteWideString((string)attr.Value, true);
                break;

            case AttributeType.TranslatedString:
                {
                    var str = (TranslatedString)attr.Value;
                    if (Version >= 4 && str.Value == null)
                    {
                        writer.Write(str.Version);
                    }
                    else
                    {
                        WriteString(str.Value ?? "", true);
                    }

                    WriteString(str.Handle, true);
                    break;
                }

            case AttributeType.ScratchBuffer:
                {
                    var buffer = (byte[])attr.Value;
                    writer.Write((UInt32)buffer.Length);
                    writer.Write(buffer);
                    break;
                }

            // DT_TranslatedFSString not supported in LSB
            default:
                BinUtils.WriteAttribute(writer, attr);
                break;
        }
    }

    private void CollectStaticStrings(Resource rsrc)
    {
        staticStrings.Clear();
        foreach (var rgn in rsrc.Regions)
        {
            AddStaticString(rgn.Key);
            CollectStaticStrings(rgn.Value);
        }
    }

    private void CollectStaticStrings(Node node)
    {
        AddStaticString(node.Name);

        foreach (var attr in node.Attributes)
        {
            AddStaticString(attr.Key);
        }

        foreach (var children in node.Children)
        {
            foreach (var child in children.Value)
                CollectStaticStrings(child);
        }
    }

    private void AddStaticString(string s)
    {
        if (!staticStrings.ContainsKey(s))
        {
            staticStrings.Add(s, nextStaticStringId++);
        }
    }

    private void WriteStaticStrings()
    {
        writer.Write((UInt32)staticStrings.Count);
        foreach (var s in staticStrings)
        {
            WriteString(s.Key, false);
            writer.Write(s.Value);
        }
    }

    private void WriteString(string s, bool nullTerminated)
    {
        byte[] utf = System.Text.Encoding.UTF8.GetBytes(s);
        int length = utf.Length + (nullTerminated ? 1 : 0);
        writer.Write(length);
        writer.Write(utf);
        if (nullTerminated)
            writer.Write((Byte)0);
    }

    private void WriteWideString(string s, bool nullTerminated)
    {
        byte[] unicode = System.Text.Encoding.Unicode.GetBytes(s);
        int length = (unicode.Length / 2) + (nullTerminated ? 1 : 0);
        writer.Write(length);
        writer.Write(unicode);
        if (nullTerminated)
            writer.Write((UInt16)0);
    }
}
