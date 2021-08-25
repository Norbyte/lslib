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
