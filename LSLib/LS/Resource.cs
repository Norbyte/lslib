using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LSLib.LS
{
    public class InvalidFormatException : Exception
    {
        public InvalidFormatException(string message)
            : base(message)
        { }
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

        public Int32 ToVersion32()
        {
            return (Int32)((Major & 0x0f) << 28 |
                (Minor & 0x0f) << 24 |
                (Revision & 0xff) << 16 |
                (Build & 0xffff) << 0);
        }

        public Int64 ToVersion64()
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
        public static byte[] SignatureBG3 = new byte[] { 0x4C, 0x53, 0x46, 0x4D };

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
        public static Dictionary<string, NodeAttribute.DataType> TypeToId = new Dictionary<string, NodeAttribute.DataType>
        {
            { "None", NodeAttribute.DataType.DT_None },
            { "uint8", NodeAttribute.DataType.DT_Byte },
            { "int16", NodeAttribute.DataType.DT_Short },
            { "uint16", NodeAttribute.DataType.DT_UShort },
            { "int32", NodeAttribute.DataType.DT_Int },
            { "uint32", NodeAttribute.DataType.DT_UInt },
            { "float", NodeAttribute.DataType.DT_Float },
            { "double", NodeAttribute.DataType.DT_Double },
            { "ivec2", NodeAttribute.DataType.DT_IVec2 },
            { "ivec3", NodeAttribute.DataType.DT_IVec3 },
            { "ivec4", NodeAttribute.DataType.DT_IVec4 },
            { "fvec2", NodeAttribute.DataType.DT_Vec2 },
            { "fvec3", NodeAttribute.DataType.DT_Vec3 },
            { "fvec4", NodeAttribute.DataType.DT_Vec4 },
            { "mat2x2", NodeAttribute.DataType.DT_Mat2 },
            { "mat3x3", NodeAttribute.DataType.DT_Mat3 },
            { "mat3x4", NodeAttribute.DataType.DT_Mat3x4 },
            { "mat4x3", NodeAttribute.DataType.DT_Mat4x3 },
            { "mat4x4", NodeAttribute.DataType.DT_Mat4 },
            { "bool", NodeAttribute.DataType.DT_Bool },
            { "string", NodeAttribute.DataType.DT_String },
            { "path", NodeAttribute.DataType.DT_Path },
            { "FixedString", NodeAttribute.DataType.DT_FixedString },
            { "LSString", NodeAttribute.DataType.DT_LSString },
            { "uint64", NodeAttribute.DataType.DT_ULongLong },
            { "ScratchBuffer", NodeAttribute.DataType.DT_ScratchBuffer },
            { "old_int64", NodeAttribute.DataType.DT_Long },
            { "int8", NodeAttribute.DataType.DT_Int8 },
            { "TranslatedString", NodeAttribute.DataType.DT_TranslatedString },
            { "WString", NodeAttribute.DataType.DT_WString },
            { "LSWString", NodeAttribute.DataType.DT_LSWString },
            { "guid", NodeAttribute.DataType.DT_UUID },
            { "int64", NodeAttribute.DataType.DT_Int64 },
            { "TranslatedFSString", NodeAttribute.DataType.DT_TranslatedFSString },
        };

        public static Dictionary<NodeAttribute.DataType, string> IdToType = new Dictionary<NodeAttribute.DataType, string>
        {
            { NodeAttribute.DataType.DT_None, "None" },
            { NodeAttribute.DataType.DT_Byte, "uint8" },
            { NodeAttribute.DataType.DT_Short, "int16" },
            { NodeAttribute.DataType.DT_UShort, "uint16" },
            { NodeAttribute.DataType.DT_Int, "int32" },
            { NodeAttribute.DataType.DT_UInt, "uint32" },
            { NodeAttribute.DataType.DT_Float, "float" },
            { NodeAttribute.DataType.DT_Double, "double" },
            { NodeAttribute.DataType.DT_IVec2, "ivec2" },
            { NodeAttribute.DataType.DT_IVec3, "ivec3" },
            { NodeAttribute.DataType.DT_IVec4, "ivec4" },
            { NodeAttribute.DataType.DT_Vec2, "fvec2" },
            { NodeAttribute.DataType.DT_Vec3, "fvec3" },
            { NodeAttribute.DataType.DT_Vec4, "fvec4" },
            { NodeAttribute.DataType.DT_Mat2, "mat2x2" },
            { NodeAttribute.DataType.DT_Mat3, "mat3x3" },
            { NodeAttribute.DataType.DT_Mat3x4, "mat3x4" },
            { NodeAttribute.DataType.DT_Mat4x3, "mat4x3" },
            { NodeAttribute.DataType.DT_Mat4, "mat4x4" },
            { NodeAttribute.DataType.DT_Bool, "bool" },
            { NodeAttribute.DataType.DT_String, "string" },
            { NodeAttribute.DataType.DT_Path, "path" },
            { NodeAttribute.DataType.DT_FixedString, "FixedString" },
            { NodeAttribute.DataType.DT_LSString, "LSString" },
            { NodeAttribute.DataType.DT_ULongLong, "uint64" },
            { NodeAttribute.DataType.DT_ScratchBuffer, "ScratchBuffer" },
            { NodeAttribute.DataType.DT_Long, "old_int64" },
            { NodeAttribute.DataType.DT_Int8, "int8" },
            { NodeAttribute.DataType.DT_TranslatedString, "TranslatedString" },
            { NodeAttribute.DataType.DT_WString, "WString" },
            { NodeAttribute.DataType.DT_LSWString, "LSWString" },
            { NodeAttribute.DataType.DT_UUID, "guid" },
            { NodeAttribute.DataType.DT_Int64, "int64" },
            { NodeAttribute.DataType.DT_TranslatedFSString, "TranslatedFSString" },
        };
    }

    public class Resource
    {
        public LSMetadata Metadata;
        public Dictionary<string, Region> Regions = new Dictionary<string,Region>();

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
        public Dictionary<string, NodeAttribute> Attributes = new Dictionary<string, NodeAttribute>();
        public Dictionary<string, List<Node>> Children = new Dictionary<string, List<Node>>();

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
            List<Node> children;
            if (!Children.TryGetValue(child.Name, out children))
            {
                children = new List<Node>();
                Children.Add(child.Name, children);
            }

            children.Add(child);
        }
    }
}
