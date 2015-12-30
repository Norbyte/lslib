using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS.Story
{
    public interface OsirisSerializable
    {
        void Read(OsiReader reader);
    }

    public class OsiReader : BinaryReader
    {
        public byte Scramble = 0x00;
        public UInt32 MinorVersion;
        public UInt32 MajorVersion;

        public OsiReader(Stream stream)
            : base(stream)
        {
        }

        public override string ReadString()
        {
            List<byte> bytes = new List<byte>();
            while (true)
            {
                var b = (byte)(ReadByte() ^ Scramble);
                if (b != 0)
                {
                    bytes.Add(b);
                }
                else
                {
                    break;
                }
            }

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public Guid ReadGuid()
        {
            var guid = ReadBytes(16);
            return new Guid(guid);
        }

        public List<T> ReadList<T>() where T : OsirisSerializable, new()
        {
            var count = ReadUInt32();
            var items = new List<T>();
            while (count-- > 0)
            {
                var item = new T();
                item.Read(this);
                items.Add(item);
            }

            return items;
        }

        public NodeRef ReadNodeRef()
        {
            var nodeRef = new NodeRef();
            nodeRef.Read(this);
            return nodeRef;
        }

        public AdapterRef ReadAdapterRef()
        {
            var adapterRef = new AdapterRef();
            adapterRef.Read(this);
            return adapterRef;
        }

        public DatabaseRef ReadDatabaseRef()
        {
            var databaseRef = new DatabaseRef();
            databaseRef.Read(this);
            return databaseRef;
        }
    }

    public class OsirisHeader : OsirisSerializable
    {
        public string Version;
        public byte MajorVersion;
        public byte MinorVersion;
        public bool BigEndian;
        public byte Unused;
        public string StoryFileVersion;
        public UInt32 DebugFlags;

        public void Read(OsiReader reader)
        {
            reader.ReadByte();
            Version = reader.ReadString();
            MajorVersion = reader.ReadByte();
            MinorVersion = reader.ReadByte();
            BigEndian = reader.ReadByte() == 1;
            Unused = reader.ReadByte();

            if (MajorVersion >= 1 && MinorVersion >= 2)
                reader.ReadBytes(0x80); // Version string buffer

            if (MajorVersion >= 1 && MinorVersion >= 3)
                DebugFlags = reader.ReadUInt32();
            else
                DebugFlags = 0;
        }
    }

    public class OsirisType : OsirisSerializable
    {
        public byte Index;
        public string Name;

        public void Read(OsiReader reader)
        {
            Name = reader.ReadString();
            Index = reader.ReadByte();
        }

        public void DebugDump(TextWriter writer)
        {
            writer.WriteLine("{0}: {1}", Index, Name);
        }
    }

    public class OsirisDivObject : OsirisSerializable
    {
        public string Name;
        public byte Type;
        public UInt32 Key1;
        public UInt32 Key2; // Some ref?
        public UInt32 Key3; // Type again?
        public UInt32 Key4;

        public void Read(OsiReader reader)
        {
            Name = reader.ReadString();
            Type = reader.ReadByte();
            Key1 = reader.ReadUInt32();
            Key2 = reader.ReadUInt32();
            Key3 = reader.ReadUInt32();
            Key4 = reader.ReadUInt32();
        }

        public void DebugDump(TextWriter writer)
        {
            writer.WriteLine("{0} {1} ({2}, {3}, {4}, {5})", Type, Name, Key1, Key2, Key3, Key4);
        }
    }

    enum NodeType : byte
    {
        Database = 1,
        Proc = 2,
        DivQuery = 3,
        And = 4,
        NotAnd = 5,
        RelOp = 6,
        Rule = 7,
        InternalQuery = 8,
        UserQuery = 9
    };

    public class NodeEntryItem : OsirisSerializable
    {
        public NodeRef NodeRef;
        public UInt32 EntryPoint;
        public UInt32 GoalId;

        public void Read(OsiReader reader)
        {
            NodeRef = reader.ReadNodeRef();
            EntryPoint = reader.ReadUInt32();
            GoalId = reader.ReadUInt32();
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            if (NodeRef.IsValid())
            {
                writer.Write("(");
                NodeRef.DebugDump(writer, story);
                writer.Write(", Entry Point {0}, Goal {1})", EntryPoint, story.Goals[GoalId].Name);
            }
            else
            {
                writer.Write("(none)");
            }
        }
    }


    abstract public class TreeNode : Node
    {
        public NodeEntryItem NextNode;

        public override void Read(OsiReader reader)
        {
            base.Read(reader);
            NextNode = new NodeEntryItem();
            NextNode.Read(reader);
        }

        public override void DebugDump(TextWriter writer, Story story)
        {
            base.DebugDump(writer, story);

            writer.Write("    Next: ");
            NextNode.DebugDump(writer, story);
            writer.WriteLine("");
        }
    }
}
