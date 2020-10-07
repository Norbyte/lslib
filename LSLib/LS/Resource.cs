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
        public Byte Major;
        public Byte Minor;
        public Byte Revision;
        public UInt16 Build;

        public static PackedVersion FromInt(Int32 packed)
        {
            return new PackedVersion
            {
                Major = (byte)((packed >> 28) & 0x0f),
                Minor = (byte)((packed >> 24) & 0x0f),
                Revision = (byte)((packed >> 16) & 0xff),
                Build = (UInt16)(packed & 0xffff),
            };
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
