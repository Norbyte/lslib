namespace LSLib.LS;

public class InvalidFormatException(string message) : Exception(message)
{
}

public struct PackedVersion
{
    public UInt32 Major;
    public UInt32 Minor;
    public UInt32 Revision;
    public UInt32 Build;

    public static PackedVersion FromInt64(Int64 packed)
    {
        return new PackedVersion
        {
            Major = (UInt32)((packed >> 55) & 0x7f),
            Minor = (UInt32)((packed >> 47) & 0xff),
            Revision = (UInt32)((packed >> 31) & 0xffff),
            Build = (UInt32)(packed & 0x7fffffff),
        };
    }

    public static PackedVersion FromInt32(Int32 packed)
    {
        return new PackedVersion
        {
            Major = (UInt32)((packed >> 28) & 0x0f),
            Minor = (UInt32)((packed >> 24) & 0x0f),
            Revision = (UInt32)((packed >> 16) & 0xff),
            Build = (UInt32)(packed & 0xffff),
        };
    }

    public readonly Int32 ToVersion32()
    {
        return (Int32)((Major & 0x0f) << 28 |
            (Minor & 0x0f) << 24 |
            (Revision & 0xff) << 16 |
            (Build & 0xffff) << 0);
    }

    public readonly Int64 ToVersion64()
    {
        return (Int64)(((Int64)Major & 0x7f) << 55 |
            ((Int64)Minor & 0xff) << 47 |
            ((Int64)Revision & 0xffff) << 31 |
            ((Int64)Build & 0x7fffffff) << 0);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct LSMetadata
{
    public const uint CurrentMajorVersion = 33;

    public UInt64 Timestamp;
    public UInt32 MajorVersion;
    public UInt32 MinorVersion;
    public UInt32 Revision;
    public UInt32 BuildNumber;
}

[StructLayout(LayoutKind.Sequential)]
public struct LSBHeader
{
    /// <summary>
    /// LSB file signature since BG3
    /// </summary>
    public readonly static byte[] SignatureBG3 = "LSFM"u8.ToArray();

    /// <summary>
    /// LSB signature up to FW3 (DOS2 DE)
    /// </summary>
    public const uint SignatureFW3 = 0x40000000;

    public UInt32 Signature;
    public UInt32 TotalSize;
    public UInt32 BigEndian;
    public UInt32 Unknown;
    public LSMetadata Metadata;
}

public static class AttributeTypeMaps
{
    public readonly static Dictionary<string, AttributeType> TypeToId = new()
    {
        { "None", AttributeType.None },
        { "uint8", AttributeType.Byte },
        { "int16", AttributeType.Short },
        { "uint16", AttributeType.UShort },
        { "int32", AttributeType.Int },
        { "uint32", AttributeType.UInt },
        { "float", AttributeType.Float },
        { "double", AttributeType.Double },
        { "ivec2", AttributeType.IVec2 },
        { "ivec3", AttributeType.IVec3 },
        { "ivec4", AttributeType.IVec4 },
        { "fvec2", AttributeType.Vec2 },
        { "fvec3", AttributeType.Vec3 },
        { "fvec4", AttributeType.Vec4 },
        { "mat2x2", AttributeType.Mat2 },
        { "mat3x3", AttributeType.Mat3 },
        { "mat3x4", AttributeType.Mat3x4 },
        { "mat4x3", AttributeType.Mat4x3 },
        { "mat4x4", AttributeType.Mat4 },
        { "bool", AttributeType.Bool },
        { "string", AttributeType.String },
        { "path", AttributeType.Path },
        { "FixedString", AttributeType.FixedString },
        { "LSString", AttributeType.LSString },
        { "uint64", AttributeType.ULongLong },
        { "ScratchBuffer", AttributeType.ScratchBuffer },
        { "old_int64", AttributeType.Long },
        { "int8", AttributeType.Int8 },
        { "TranslatedString", AttributeType.TranslatedString },
        { "WString", AttributeType.WString },
        { "LSWString", AttributeType.LSWString },
        { "guid", AttributeType.UUID },
        { "int64", AttributeType.Int64 },
        { "TranslatedFSString", AttributeType.TranslatedFSString },
    };

    public readonly static Dictionary<AttributeType, string> IdToType = new()
    {
        { AttributeType.None, "None" },
        { AttributeType.Byte, "uint8" },
        { AttributeType.Short, "int16" },
        { AttributeType.UShort, "uint16" },
        { AttributeType.Int, "int32" },
        { AttributeType.UInt, "uint32" },
        { AttributeType.Float, "float" },
        { AttributeType.Double, "double" },
        { AttributeType.IVec2, "ivec2" },
        { AttributeType.IVec3, "ivec3" },
        { AttributeType.IVec4, "ivec4" },
        { AttributeType.Vec2, "fvec2" },
        { AttributeType.Vec3, "fvec3" },
        { AttributeType.Vec4, "fvec4" },
        { AttributeType.Mat2, "mat2x2" },
        { AttributeType.Mat3, "mat3x3" },
        { AttributeType.Mat3x4, "mat3x4" },
        { AttributeType.Mat4x3, "mat4x3" },
        { AttributeType.Mat4, "mat4x4" },
        { AttributeType.Bool, "bool" },
        { AttributeType.String, "string" },
        { AttributeType.Path, "path" },
        { AttributeType.FixedString, "FixedString" },
        { AttributeType.LSString, "LSString" },
        { AttributeType.ULongLong, "uint64" },
        { AttributeType.ScratchBuffer, "ScratchBuffer" },
        { AttributeType.Long, "old_int64" },
        { AttributeType.Int8, "int8" },
        { AttributeType.TranslatedString, "TranslatedString" },
        { AttributeType.WString, "WString" },
        { AttributeType.LSWString, "LSWString" },
        { AttributeType.UUID, "guid" },
        { AttributeType.Int64, "int64" },
        { AttributeType.TranslatedFSString, "TranslatedFSString" },
    };
}

public class Resource
{
    public LSMetadata Metadata;
    public Dictionary<string, Region> Regions = [];

    public Resource()
    {
        Metadata.MajorVersion = 3;
    }
}

public class Region : Node
{
    public string RegionName;
}

public class Node
{
    public string Name;
    public Node Parent;
    public Dictionary<string, NodeAttribute> Attributes = [];
    public Dictionary<string, List<Node>> Children = [];
    public int? Line = null;

    public int ChildCount
    {
        get
        {
            return
                (from c in Children
                select c.Value.Count).Sum();
        }
    }

    public int TotalChildCount()
    {
        int count = 0;
        foreach (var key in Children)
        {
            foreach (var child in key.Value)
            {
                count += 1 + child.TotalChildCount();
            }
        }

        return count;
    }

    public void AppendChild(Node child)
    {
        if (!Children.TryGetValue(child.Name, out List<Node> children))
        {
            children = [];
            Children.Add(child.Name, children);
        }

        children.Add(child);
    }
}
