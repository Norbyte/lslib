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
        void Write(OsiWriter writer);
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

        public override bool ReadBoolean()
        {
            var b = ReadByte();
            return b == 1;
        }

        public Guid ReadGuid()
        {
            var guid = ReadBytes(16);
            return new Guid(guid);
        }

        public List<T> ReadList<T>() where T : OsirisSerializable, new()
        {
            var items = new List<T>();
            ReadList<T>(items);
            return items;
        }

        public void ReadList<T>(List<T> items) where T : OsirisSerializable, new()
        {
            var count = ReadUInt32();
            while (count-- > 0)
            {
                var item = new T();
                item.Read(this);
                items.Add(item);
            }
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

    public class OsiWriter : BinaryWriter
    {
        public byte Scramble = 0x00;
        public UInt32 MinorVersion;
        public UInt32 MajorVersion;

        public OsiWriter(Stream stream)
            : base(stream)
        {
        }

        public override void Write(String s)
        {
            var bytes = Encoding.UTF8.GetBytes(s).Select(b => (byte)(b ^ Scramble)).ToArray();
            Write(bytes, 0, bytes.Length);
            Write(Scramble);
        }

        public override void Write(bool b)
        {
            Write((byte)(b ? 1 : 0));
        }

        public void Write(Guid guid)
        {
            var bytes = guid.ToByteArray();
            Write(bytes, 0, bytes.Length);
        }

        public void WriteList<T>(List<T> list) where T : OsirisSerializable
        {
            Write((UInt32)list.Count);
            foreach (var item in list)
            {
                item.Write(this);
            }
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
            BigEndian = reader.ReadBoolean();
            Unused = reader.ReadByte();

            if (MajorVersion > 1 || (MajorVersion == 1 && MinorVersion >= 2))
                reader.ReadBytes(0x80); // Version string buffer

            if (MajorVersion > 1 || (MajorVersion == 1 && MinorVersion >= 3))
                DebugFlags = reader.ReadUInt32();
            else
                DebugFlags = 0;
        }

        public void Write(OsiWriter writer)
        {
            writer.Write((byte)0);
            writer.Write(Version);
            writer.Write(MajorVersion);
            writer.Write(MinorVersion);
            writer.Write(BigEndian);
            writer.Write(Unused);

            if (MajorVersion > 1 || (MajorVersion == 1 && MinorVersion >= 2))
            {
                var versionString = String.Format("{0}.{1}", MajorVersion, MinorVersion);
                var versionBytes = Encoding.UTF8.GetBytes(versionString);
                byte[] version = new byte[0x80];
                versionBytes.CopyTo(version, 0);
                writer.Write(version, 0, version.Length);
            }

            if (MajorVersion > 1 || (MajorVersion == 1 && MinorVersion >= 3))
                writer.Write(DebugFlags);
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

        public void Write(OsiWriter writer)
        {
            writer.Write(Name);
            writer.Write(Index);
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

        public void Write(OsiWriter writer)
        {
            writer.Write(Name);
            writer.Write(Type);
            writer.Write(Key1);
            writer.Write(Key2);
            writer.Write(Key3);
            writer.Write(Key4);
        }

        public void DebugDump(TextWriter writer)
        {
            writer.WriteLine("{0} {1} ({2}, {3}, {4}, {5})", Type, Name, Key1, Key2, Key3, Key4);
        }
    }

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

        public void Write(OsiWriter writer)
        {
            NodeRef.Write(writer);
            writer.Write(EntryPoint);
            writer.Write(GoalId);
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
}
