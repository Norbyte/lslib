namespace LSLib.LS;

public class LSBReader(Stream stream) : IDisposable
{
    private BinaryReader reader;
    private Dictionary<UInt32, string> staticStrings = [];
    private bool IsBG3;

    public void Dispose()
    {
        stream.Dispose();
    }

    public Resource Read()
    {
        using (this.reader = new BinaryReader(stream))
        {
            // Check for BG3 header
            var header = BinUtils.ReadStruct<LSBHeader>(reader);
            if (header.Signature != BitConverter.ToUInt32(LSBHeader.SignatureBG3, 0) && header.Signature != LSBHeader.SignatureFW3)
                throw new InvalidFormatException(String.Format("Illegal signature in LSB header ({0})", header.Signature));

            if (stream.Length != header.TotalSize)
                throw new InvalidFormatException(String.Format("Invalid LSB file size; expected {0}, got {1}", header.TotalSize, stream.Length));

            // The game only uses little-endian files on all platforms currently and big-endian support isn't worth the hassle
            if (header.BigEndian != 0)
                throw new InvalidFormatException("Big-endian LSB files are not supported");

            IsBG3 = (header.Signature == BitConverter.ToUInt32(LSBHeader.SignatureBG3, 0));
            ReadStaticStrings();

            Resource rsrc = new Resource
            {
                Metadata = header.Metadata
            };
            ReadRegions(rsrc);
            return rsrc;
        }
    }

    private void ReadRegions(Resource rsrc)
    {
        UInt32 regions = reader.ReadUInt32();
        for (UInt32 i = 0; i < regions; i++)
        {
            UInt32 regionNameId = reader.ReadUInt32();
            UInt32 regionOffset = reader.ReadUInt32();

            Region rgn = new Region
            {
                RegionName = staticStrings[regionNameId]
            };
            var lastRegionPos = stream.Position;

            stream.Seek(regionOffset, SeekOrigin.Begin);
            ReadNode(rgn);
            rsrc.Regions[rgn.RegionName] = rgn;
            stream.Seek(lastRegionPos, SeekOrigin.Begin);
        }
    }

    private void ReadNode(Node node)
    {
        UInt32 nodeNameId = reader.ReadUInt32();
        UInt32 attributeCount = reader.ReadUInt32();
        UInt32 childCount = reader.ReadUInt32();
        node.Name = staticStrings[nodeNameId];

        for (UInt32 i = 0; i < attributeCount; i++)
        {
            UInt32 attrNameId = reader.ReadUInt32();
            UInt32 attrTypeId = reader.ReadUInt32();
            if (attrTypeId > (int)AttributeType.Max)
                throw new InvalidFormatException(String.Format("Unsupported attribute data type: {0}", attrTypeId));

            node.Attributes[staticStrings[attrNameId]] = ReadAttribute((AttributeType)attrTypeId);
        }

        for (UInt32 i = 0; i < childCount; i++)
        {
            Node child = new Node
            {
                Parent = node
            };
            ReadNode(child);
            node.AppendChild(child);
        }
    }

    private NodeAttribute ReadAttribute(AttributeType type)
    {
        switch (type)
        {
            case AttributeType.String:
            case AttributeType.Path:
            case AttributeType.FixedString:
            case AttributeType.LSString:
                {
                    var attr = new NodeAttribute(type)
                    {
                        Value = ReadString(true)
                    };
                    return attr;
                }

            case AttributeType.WString:
            case AttributeType.LSWString:
                {
                    var attr = new NodeAttribute(type)
                    {
                        Value = ReadWideString(true)
                    };
                    return attr;
                }

            case AttributeType.TranslatedString:
                {
                    var attr = new NodeAttribute(type);
                    var str = new TranslatedString();

                    if (IsBG3)
                    {
                        str.Version = reader.ReadUInt16();

                        // Sometimes BG3 string keys still contain the value?
                        // Weird heuristic to find these cases
                        var test = reader.ReadUInt16();
                        if (test == 0)
                        {
                            stream.Seek(-4, SeekOrigin.Current);
                            str.Version = 0;
                            str.Value = ReadString(true);
                        }
                        else
                        {
                            stream.Seek(-2, SeekOrigin.Current);
                            str.Value = null;
                        }
                    }
                    else
                    {
                        str.Version = 0;
                        str.Value = ReadString(true);
                    }

                    str.Handle = ReadString(true);
                    attr.Value = str;
                    return attr;
                }

            case AttributeType.ScratchBuffer:
                {
                    var attr = new NodeAttribute(type);
                    var bufferLength = reader.ReadInt32();
                    attr.Value = reader.ReadBytes(bufferLength);
                    return attr;
                }

            // DT_TranslatedFSString not supported in LSB
            default:
                return BinUtils.ReadAttribute(type, reader);
        }
    }

    private void ReadStaticStrings()
    {
        UInt32 strings = reader.ReadUInt32();
        for (UInt32 i = 0; i < strings; i++)
        {
            string s = ReadString(false);
            UInt32 index = reader.ReadUInt32();
            if (staticStrings.ContainsKey(index))
                throw new InvalidFormatException(String.Format("String ID {0} duplicated in static string map", index));
            staticStrings.Add(index, s);
        }
    }

    private string ReadString(bool nullTerminated)
    {
        int length = reader.ReadInt32() - (nullTerminated ? 1 : 0);
        byte[] bytes = reader.ReadBytes(length);

        // Remove stray null bytes at the end of the string
        // Some LSB files seem to save translated string keys incurrectly, and append two NULL bytes
        // (or one null byte and another stray byte) to the end of the value.
        bool hasBogusNullBytes = false;
        while (length > 0 && bytes[length - 1] == 0)
        {
            length--;
            hasBogusNullBytes = true;
        }

        string str = System.Text.Encoding.UTF8.GetString(bytes, 0, length);

        if (nullTerminated)
        {
            if (reader.ReadByte() != 0 && !hasBogusNullBytes)
                throw new InvalidFormatException("Illegal null terminated string");
        }

        return str;
    }

    private string ReadWideString(bool nullTerminated)
    {
        int length = reader.ReadInt32() - (nullTerminated ? 1 : 0);
        byte[] bytes = reader.ReadBytes(length * 2);
        string str = System.Text.Encoding.Unicode.GetString(bytes);
        if (nullTerminated)
        {
            if (reader.ReadUInt16() != 0)
                throw new InvalidFormatException("Illegal null terminated widestring");
        }

        return str;
    }
}
