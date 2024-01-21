namespace LSLib.LS.Story;

public interface OsirisSerializable
{
    void Read(OsiReader reader);
    void Write(OsiWriter writer);
}

/// <summary>
/// Osiris file format version numbers
/// </summary>
public static class OsiVersion
{
    /// <summary>
    /// Initial version
    /// </summary>
    public const uint VerInitial = 0x0100;

    /// <summary>
    /// Added Init/Exit calls to goals
    /// </summary>
    public const uint VerAddInitExitCalls = 0x0101;

    /// <summary>
    /// Added version string at the beginning of the OSI file
    /// </summary>
    public const uint VerAddVersionString = 0x0102;

    /// <summary>
    /// Added debug flags in the header
    /// </summary>
    public const uint VerAddDebugFlags = 0x0103;

    /// <summary>
    /// Started scrambling strings by xor-ing with 0xAD
    /// </summary>
    public const uint VerScramble = 0x0104;

    /// <summary>
    /// Added custom (string) types
    /// </summary>
    public const uint VerAddTypeMap = 0x0105;

    /// <summary>
    /// Added Query nodes
    /// </summary>
    public const uint VerAddQuery = 0x0106;

    /// <summary>
    /// Types can be aliases of any builtin type, not just strings
    /// </summary>
    public const uint VerTypeAliases = 0x0109;

    /// <summary>
    /// Added INT64, GUIDSTRING types
    /// </summary>
    public const uint VerEnhancedTypes = 0x010a;

    /// <summary>
    /// Added external string table
    /// </summary>
    public const uint VerExternalStringTable = 0x010b;

    /// <summary>
    /// Removed external string table
    /// </summary>
    public const uint VerRemoveExternalStringTable = 0x010c;

    /// <summary>
    /// Added enumerations
    /// </summary>
    public const uint VerEnums = 0x010d;

    /// <summary>
    /// Last supported Osi version
    /// </summary>
    public const uint VerLastSupported = VerEnums;
}

public class OsiReader : BinaryReader
{
    public byte Scramble = 0x00;
    public UInt32 MinorVersion;
    public UInt32 MajorVersion;
    // Use 16-bit instead of 32-bit type IDs, BG3 Patch8+
    public bool? ShortTypeIds = null;
    public Dictionary<uint, uint> TypeAliases = new Dictionary<uint, uint>();
    // TODO: Make RO!
    public Story Story;

    public uint Ver
    {
        get { return ((uint)MajorVersion << 8) | (uint)MinorVersion; }
    }

    public OsiReader(Stream stream, Story story)
        : base(stream)
    {
        Story = story;
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
        if (b != 0 && b != 1)
        {
            throw new InvalidDataException("Invalid boolean value; expected 0 or 1.");
        }

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

    public List<T> ReadRefList<T, RefT>() where T : OsiReference<RefT>, new()
    {
        var items = new List<T>();
        ReadRefList<T, RefT>(items);
        return items;
    }

    public void ReadRefList<T, RefT>(List<T> items) where T : OsiReference<RefT>, new()
    {
        var count = ReadUInt32();
        while (count-- > 0)
        {
            var item = new T();
            item.BindStory(Story);
            item.Read(this);
            items.Add(item);
        }
    }

    public NodeReference ReadNodeRef()
    {
        var nodeRef = new NodeReference();
        nodeRef.BindStory(Story);
        nodeRef.Read(this);
        return nodeRef;
    }

    public AdapterReference ReadAdapterRef()
    {
        var adapterRef = new AdapterReference();
        adapterRef.BindStory(Story);
        adapterRef.Read(this);
        return adapterRef;
    }

    public DatabaseReference ReadDatabaseRef()
    {
        var databaseRef = new DatabaseReference();
        databaseRef.BindStory(Story);
        databaseRef.Read(this);
        return databaseRef;
    }

    public GoalReference ReadGoalRef()
    {
        var goalRef = new GoalReference();
        goalRef.BindStory(Story);
        goalRef.Read(this);
        return goalRef;
    }
}

public class OsiWriter : BinaryWriter
{
    public byte Scramble = 0x00;
    public UInt32 MinorVersion;
    public UInt32 MajorVersion;
    // Use 16-bit instead of 32-bit type IDs, BG3 Patch8+
    public bool ShortTypeIds;
    public Dictionary<uint, uint> TypeAliases = new Dictionary<uint, uint>();
    public Dictionary<uint, OsirisEnum> Enums = new Dictionary<uint, OsirisEnum>();

    public uint Ver
    {
        get { return ((uint)MajorVersion << 8) | (uint)MinorVersion; }
    }

    public OsiWriter(Stream stream, bool leaveOpen)
        : base(stream, Encoding.UTF8, leaveOpen)
    {
    }

    public override void Write(String s)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(bytes[i] ^ Scramble);
        }
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

public class SaveFileHeader : OsirisSerializable
{
    public string Version;
    public byte MajorVersion;
    public byte MinorVersion;
    public bool BigEndian;
    public byte Unused;
    public UInt32 DebugFlags;

    public uint Ver
    {
        get { return ((uint)MajorVersion << 8) | (uint)MinorVersion; }
    }

    public void Read(OsiReader reader)
    {
        reader.ReadByte();
        Version = reader.ReadString();
        MajorVersion = reader.ReadByte();
        MinorVersion = reader.ReadByte();
        BigEndian = reader.ReadBoolean();
        Unused = reader.ReadByte();

        if (Ver >= OsiVersion.VerAddVersionString)
            reader.ReadBytes(0x80); // Version string buffer

        if (Ver >= OsiVersion.VerAddDebugFlags)
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

        if (Ver >= OsiVersion.VerAddVersionString)
        {
            var versionString = String.Format("{0}.{1}", MajorVersion, MinorVersion);
            var versionBytes = Encoding.UTF8.GetBytes(versionString);
            byte[] version = new byte[0x80];
            versionBytes.CopyTo(version, 0);
            writer.Write(version, 0, version.Length);
        }

        if (Ver >= OsiVersion.VerAddDebugFlags)
            writer.Write(DebugFlags);
    }
}

public class OsirisType : OsirisSerializable
{
    public byte Index;
    public byte Alias;
    public string Name;
    public bool IsBuiltin;

    public static OsirisType MakeBuiltin(byte index, string name)
    {
        var type = new OsirisType();
        type.Index = index;
        type.Alias = 0;
        type.Name = name;
        type.IsBuiltin = true;
        return type;
    }

    public void Read(OsiReader reader)
    {
        Name = reader.ReadString();
        Index = reader.ReadByte();
        IsBuiltin = false;

        if (reader.Ver >= OsiVersion.VerTypeAliases)
        {
            Alias = reader.ReadByte();
        }
        else
        {
            // D:OS 1 only supported string aliases
            Alias = (int)Value.Type_OS1.String;
        }
    }

    public void Write(OsiWriter writer)
    {
        writer.Write(Name);
        writer.Write(Index);

        if (writer.Ver >= OsiVersion.VerTypeAliases)
        {
            writer.Write(Alias);
        }
    }

    public void DebugDump(TextWriter writer)
    {
        if (Alias == 0)
        {
            writer.WriteLine("{0}: {1}", Index, Name);
        }
        else
        {
            writer.WriteLine("{0}: {1} (Alias: {2})", Index, Name, Alias);
        }
    }
}

public class OsirisEnumElement : OsirisSerializable
{
    public String Name;
    public UInt64 Value;


    public void Read(OsiReader reader)
    {
        Name = reader.ReadString();
        Value = reader.ReadUInt64();
    }

    public void Write(OsiWriter writer)
    {
        writer.Write(Name);
        writer.Write(Value);
    }

    public void DebugDump(TextWriter writer)
    {
        writer.WriteLine("{0}: {1}", Name, Value);
    }
}

public class OsirisEnum : OsirisSerializable
{
    public UInt16 UnderlyingType;
    public List<OsirisEnumElement> Elements;


    public void Read(OsiReader reader)
    {
        UnderlyingType = reader.ReadUInt16();
        var elements = reader.ReadUInt32();
        Elements = new List<OsirisEnumElement>();
        while (elements-- > 0)
        {
            var e = new OsirisEnumElement();
            e.Read(reader);
            Elements.Add(e);
        }
    }

    public void Write(OsiWriter writer)
    {
        writer.Write(UnderlyingType);
        writer.Write((UInt32)Elements.Count);

        foreach (var e in Elements)
        {
            e.Write(writer);
        }
    }

    public void DebugDump(TextWriter writer)
    {
        writer.WriteLine("Type {0}", UnderlyingType);
        foreach (var e in Elements)
        {
            e.DebugDump(writer);
        }
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

public enum EntryPoint : UInt32
{
    // The next node is not an AND/NOT AND expression
    None = 0,
    // This node is on the left side of the next AND/NOT AND expression
    Left = 1,
    // This node is on the right side of the next AND/NOT AND expression
    Right = 2
};

public class NodeEntryItem : OsirisSerializable
{
    public NodeReference NodeRef;
    public EntryPoint EntryPoint;
    public GoalReference GoalRef;

    public void Read(OsiReader reader)
    {
        NodeRef = reader.ReadNodeRef();
        EntryPoint = (EntryPoint)reader.ReadUInt32();
        GoalRef = reader.ReadGoalRef();
    }

    public void Write(OsiWriter writer)
    {
        NodeRef.Write(writer);
        writer.Write((UInt32)EntryPoint);
        GoalRef.Write(writer);
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        if (NodeRef.IsValid)
        {
            writer.Write("(");
            NodeRef.DebugDump(writer, story);
            if (GoalRef.IsValid)
            {
                writer.Write(", Entry Point {0}, Goal {1})", EntryPoint, GoalRef.Resolve().Name);
            }
            else
            {
                writer.Write(")");
            }
        }
        else
        {
            writer.Write("(none)");
        }
    }
}
