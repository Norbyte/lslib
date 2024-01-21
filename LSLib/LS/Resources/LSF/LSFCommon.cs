namespace LSLib.LS;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSFMagic
{
    /// <summary>
    /// LSOF file signature
    /// </summary>
    public readonly static byte[] Signature = "LSOF"u8.ToArray();

    /// <summary>
    /// LSOF file signature; should be the same as LSFHeader.Signature
    /// </summary>
    public UInt32 Magic;

    /// <summary>
    /// Version of the LSOF file; D:OS EE is version 1/2, D:OS 2 is version 3
    /// </summary>
    public UInt32 Version;
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSFHeader
{
    /// <summary>
    /// Possibly version number? (major, minor, rev, build)
    /// </summary>
    public Int32 EngineVersion;
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSFHeaderV5
{
    /// <summary>
    /// Possibly version number? (major, minor, rev, build)
    /// </summary>
    public Int64 EngineVersion;
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSFMetadataV5
{
    /// <summary>
    /// Total uncompressed size of the string hash table
    /// </summary>
    public UInt32 StringsUncompressedSize;
    /// <summary>
    /// Compressed size of the string hash table
    /// </summary>
    public UInt32 StringsSizeOnDisk;
    /// <summary>
    /// Total uncompressed size of the node list
    /// </summary>
    public UInt32 NodesUncompressedSize;
    /// <summary>
    /// Compressed size of the node list
    /// </summary>
    public UInt32 NodesSizeOnDisk;
    /// <summary>
    /// Total uncompressed size of the attribute list
    /// </summary>
    public UInt32 AttributesUncompressedSize;
    /// <summary>
    /// Compressed size of the attribute list
    /// </summary>
    public UInt32 AttributesSizeOnDisk;
    /// <summary>
    /// Total uncompressed size of the raw value buffer
    /// </summary>
    public UInt32 ValuesUncompressedSize;
    /// <summary>
    /// Compressed size of the raw value buffer
    /// </summary>
    public UInt32 ValuesSizeOnDisk;
    /// <summary>
    /// Compression method and level used for the string, node, attribute and value buffers.
    /// Uses the same format as packages (see BinUtils.MakeCompressionFlags)
    /// </summary>
    public CompressionFlags CompressionFlags;
    /// <summary>
    /// Possibly unused, always 0
    /// </summary>
    public Byte Unknown2;
    public UInt16 Unknown3;
    /// <summary>
    /// Extended node/attribute format indicator, 0 for V2, 0/1 for V3
    /// </summary>
    public UInt32 HasSiblingData;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSFMetadataV6
{
    /// <summary>
    /// Total uncompressed size of the string hash table
    /// </summary>
    public UInt32 StringsUncompressedSize;
    /// <summary>
    /// Compressed size of the string hash table
    /// </summary>
    public UInt32 StringsSizeOnDisk;
    public UInt64 Unknown;
    /// <summary>
    /// Total uncompressed size of the node list
    /// </summary>
    public UInt32 NodesUncompressedSize;
    /// <summary>
    /// Compressed size of the node list
    /// </summary>
    public UInt32 NodesSizeOnDisk;
    /// <summary>
    /// Total uncompressed size of the attribute list
    /// </summary>
    public UInt32 AttributesUncompressedSize;
    /// <summary>
    /// Compressed size of the attribute list
    /// </summary>
    public UInt32 AttributesSizeOnDisk;
    /// <summary>
    /// Total uncompressed size of the raw value buffer
    /// </summary>
    public UInt32 ValuesUncompressedSize;
    /// <summary>
    /// Compressed size of the raw value buffer
    /// </summary>
    public UInt32 ValuesSizeOnDisk;
    /// <summary>
    /// Compression method and level used for the string, node, attribute and value buffers.
    /// Uses the same format as packages (see BinUtils.MakeCompressionFlags)
    /// </summary>
    public CompressionFlags CompressionFlags;
    /// <summary>
    /// Possibly unused, always 0
    /// </summary>
    public Byte Unknown2;
    public UInt16 Unknown3;
    /// <summary>
    /// Extended node/attribute format indicator, 0 for V2, 0/1 for V3
    /// </summary>
    public UInt32 HasSiblingData;
}

/// <summary>
/// Node (structure) entry in the LSF file
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSFNodeEntryV2
{
    /// <summary>
    /// Name of this node
    /// (16-bit MSB: index into name hash table, 16-bit LSB: offset in hash chain)
    /// </summary>
    public UInt32 NameHashTableIndex;
    /// <summary>
    /// Index of the first attribute of this node
    /// (-1: node has no attributes)
    /// </summary>
    public Int32 FirstAttributeIndex;
    /// <summary>
    /// Index of the parent node
    /// (-1: this node is a root region)
    /// </summary>
    public Int32 ParentIndex;

    /// <summary>
    /// Index into name hash table
    /// </summary>
    public int NameIndex
    {
        get { return (int)(NameHashTableIndex >> 16); }
    }

    /// <summary>
    /// Offset in hash chain
    /// </summary>
    public int NameOffset
    {
        get { return (int)(NameHashTableIndex & 0xffff); }
    }
};

/// <summary>
/// Node (structure) entry in the LSF file
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSFNodeEntryV3
{
    /// <summary>
    /// Name of this node
    /// (16-bit MSB: index into name hash table, 16-bit LSB: offset in hash chain)
    /// </summary>
    public UInt32 NameHashTableIndex;
    /// <summary>
    /// Index of the parent node
    /// (-1: this node is a root region)
    /// </summary>
    public Int32 ParentIndex;
    /// <summary>
    /// Index of the next sibling of this node
    /// (-1: this is the last node)
    /// </summary>
    public Int32 NextSiblingIndex;
    /// <summary>
    /// Index of the first attribute of this node
    /// (-1: node has no attributes)
    /// </summary>
    public Int32 FirstAttributeIndex;

    /// <summary>
    /// Index into name hash table
    /// </summary>
    public int NameIndex
    {
        get { return (int)(NameHashTableIndex >> 16); }
    }

    /// <summary>
    /// Offset in hash chain
    /// </summary>
    public int NameOffset
    {
        get { return (int)(NameHashTableIndex & 0xffff); }
    }
};

/// <summary>
/// Processed node information for a node in the LSF file
/// </summary>
internal class LSFNodeInfo
{
    /// <summary>
    /// Index of the parent node
    /// (-1: this node is a root region)
    /// </summary>
    public int ParentIndex;
    /// <summary>
    /// Index into name hash table
    /// </summary>
    public int NameIndex;
    /// <summary>
    /// Offset in hash chain
    /// </summary>
    public int NameOffset;
    /// <summary>
    /// Index of the first attribute of this node
    /// (-1: node has no attributes)
    /// </summary>
    public int FirstAttributeIndex;
};

/// <summary>
/// V2 attribute extension in the LSF file
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSFAttributeEntryV2
{
    /// <summary>
    /// Name of this attribute
    /// (16-bit MSB: index into name hash table, 16-bit LSB: offset in hash chain)
    /// </summary>
    public UInt32 NameHashTableIndex;

    /// <summary>
    /// 6-bit LSB: Type of this attribute (see NodeAttribute.DataType)
    /// 26-bit MSB: Length of this attribute
    /// </summary>
    public UInt32 TypeAndLength;

    /// <summary>
    /// Index of the node that this attribute belongs to
    /// Note: These indexes are assigned seemingly arbitrarily, and are not neccessarily indices into the node list
    /// </summary>
    public Int32 NodeIndex;

    /// <summary>
    /// Index into name hash table
    /// </summary>
    public int NameIndex
    {
        get { return (int)(NameHashTableIndex >> 16); }
    }

    /// <summary>
    /// Offset in hash chain
    /// </summary>
    public int NameOffset
    {
        get { return (int)(NameHashTableIndex & 0xffff); }
    }

    /// <summary>
    /// Type of this attribute (see NodeAttribute.DataType)
    /// </summary>
    public uint TypeId
    {
        get { return TypeAndLength & 0x3f; }
    }

    /// <summary>
    /// Length of this attribute
    /// </summary>
    public uint Length
    {
        get { return TypeAndLength >> 6; }
    }
};

/// <summary>
/// V3 attribute extension in the LSF file
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSFAttributeEntryV3
{
    /// <summary>
    /// Name of this attribute
    /// (16-bit MSB: index into name hash table, 16-bit LSB: offset in hash chain)
    /// </summary>
    public UInt32 NameHashTableIndex;

    /// <summary>
    /// 6-bit LSB: Type of this attribute (see NodeAttribute.DataType)
    /// 26-bit MSB: Length of this attribute
    /// </summary>
    public UInt32 TypeAndLength;

    /// <summary>
    /// Index of the node that this attribute belongs to
    /// Note: These indexes are assigned seemingly arbitrarily, and are not neccessarily indices into the node list
    /// </summary>
    public Int32 NextAttributeIndex;

    /// <summary>
    /// Absolute position of attribute value in the value stream
    /// </summary>
    public UInt32 Offset;

    /// <summary>
    /// Index into name hash table
    /// </summary>
    public int NameIndex
    {
        get { return (int)(NameHashTableIndex >> 16); }
    }

    /// <summary>
    /// Offset in hash chain
    /// </summary>
    public int NameOffset
    {
        get { return (int)(NameHashTableIndex & 0xffff); }
    }

    /// <summary>
    /// Type of this attribute (see NodeAttribute.DataType)
    /// </summary>
    public uint TypeId
    {
        get { return TypeAndLength & 0x3f; }
    }

    /// <summary>
    /// Length of this attribute
    /// </summary>
    public uint Length
    {
        get { return TypeAndLength >> 6; }
    }
};

internal class LSFAttributeInfo
{
    /// <summary>
    /// Index into name hash table
    /// </summary>
    public int NameIndex;
    /// <summary>
    /// Offset in hash chain
    /// </summary>
    public int NameOffset;
    /// <summary>
    /// Type of this attribute (see NodeAttribute.DataType)
    /// </summary>
    public uint TypeId;
    /// <summary>
    /// Length of this attribute
    /// </summary>
    public uint Length;
    /// <summary>
    /// Absolute position of attribute data in the values section
    /// </summary>
    public uint DataOffset;
    /// <summary>
    /// Index of the next attribute in this node
    /// (-1: this is the last attribute)
    /// </summary>
    public int NextAttributeIndex;
};
