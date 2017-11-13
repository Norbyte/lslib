using System;
using System.Collections.Generic;
using System.Linq;

namespace LSLib.LS
{
    public class InvalidFormatException : Exception
    {
        public InvalidFormatException(string message)
            : base(message)
        { }
    }

    public struct LSMetadata
    {
        public const uint CurrentMajorVersion = 33;

        public UInt64 timestamp;
        public UInt32 majorVersion;
        public UInt32 minorVersion;
        public UInt32 revision;
        public UInt32 buildNumber;
    }

    public struct LSBHeader
    {
        public const uint Signature = 0x40000000;
        public const uint CurrentMajorVersion = 1;
        public const uint CurrentMinorVersion = 3;

        public UInt32 signature;
        public UInt32 totalSize;
        public UInt32 bigEndian;
        public UInt32 unknown;
        public LSMetadata metadata;
    }

    public class Resource
    {
        public LSMetadata Metadata;
        public Dictionary<string, Region> Regions = new Dictionary<string,Region>();

        public Resource()
        {
            Metadata.majorVersion = LSMetadata.CurrentMajorVersion;
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
