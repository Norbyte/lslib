using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS.Story
{
    public class NodeRef : OsirisSerializable
    {
        public UInt32 NodeIndex;

        public void Read(OsiReader reader)
        {
            NodeIndex = reader.ReadUInt32();
        }

        public void Write(OsiWriter writer)
        {
            writer.Write(NodeIndex);
        }

        public bool IsValid()
        {
            return NodeIndex != 0;
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            if (!IsValid())
            {
                writer.Write("(None)");
            }
            else
            {
                var node = story.Nodes[NodeIndex];
                if (node.Name.Length > 0)
                {
                    writer.Write("#{0} <{1}({2}) {3}>", NodeIndex, node.Name, node.NumParams, node.TypeName());
                }
                else
                {
                    writer.Write("#{0} <{1}>", NodeIndex, node.TypeName());
                }
            }
        }
    }

    public class AdapterRef : OsirisSerializable
    {
        public UInt32 AdapterIndex;

        public void Read(OsiReader reader)
        {
            AdapterIndex = reader.ReadUInt32();
        }

        public void Write(OsiWriter writer)
        {
            writer.Write(AdapterIndex);
        }

        public bool IsValid()
        {
            return AdapterIndex != 0;
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            if (!IsValid())
            {
                writer.Write("(None)");
            }
            else
            {
                writer.Write("#{0}", AdapterIndex);
            }
        }
    }

    public class DatabaseRef : OsirisSerializable
    {
        public UInt32 DatabaseIndex;

        public void Read(OsiReader reader)
        {
            DatabaseIndex = reader.ReadUInt32();
        }

        public void Write(OsiWriter writer)
        {
            writer.Write(DatabaseIndex);
        }

        public bool IsValid()
        {
            return DatabaseIndex != 0;
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            if (!IsValid())
            {
                writer.Write("(None)");
            }
            else
            {
                writer.Write("#{0}", DatabaseIndex);
            }
        }
    }
}
