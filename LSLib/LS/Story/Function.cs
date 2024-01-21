namespace LSLib.LS.Story;

public class FunctionSignature : OsirisSerializable
{
    public string Name;
    public List<byte> OutParamMask;
    public ParameterList Parameters;

    public void Read(OsiReader reader)
    {
        Name = reader.ReadString();
        OutParamMask = new List<byte>();
        var outParamBytes = reader.ReadUInt32();
        while (outParamBytes-- > 0)
        {
            OutParamMask.Add(reader.ReadByte());
        }

        Parameters = new ParameterList();
        Parameters.Read(reader);
    }

    public void Write(OsiWriter writer)
    {
        writer.Write(Name);

        writer.Write((UInt32)OutParamMask.Count);
        foreach (var b in OutParamMask)
        {
            writer.Write(b);
        }

        Parameters.Write(writer);
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        writer.Write(Name);
        writer.Write("(");
        for (var i = 0; i < Parameters.Types.Count; i++)
        {
            var type = story.Types[Parameters.Types[i]];
            var isOutParam = ((OutParamMask[i >> 3] << (i & 7)) & 0x80) == 0x80;
            if (isOutParam) writer.Write("out ");
            writer.Write(type.Name);
            if (i < Parameters.Types.Count - 1) writer.Write(", ");
        }
        writer.Write(")");
    }
}

public class ParameterList : OsirisSerializable
{
    public List<UInt32> Types;

    public void Read(OsiReader reader)
    {
        Types = new List<UInt32>();
        var count = reader.ReadByte();
        while (count-- > 0)
        {
            // BG3 heuristic: Patch 8 doesn't increment the version number but changes type ID format,
            // so we need to detect it by checking if a 32-bit type ID would be valid.
            if (reader.ShortTypeIds == null)
            {
                var id = reader.ReadUInt32();
                reader.BaseStream.Position -= 4;
                reader.ShortTypeIds = (id > 0xff);
            }

            if (reader.ShortTypeIds == true)
            {
                Types.Add(reader.ReadUInt16());
            }
            else
            {
                Types.Add(reader.ReadUInt32());
            }
        }
    }

    public void Write(OsiWriter writer)
    {
        writer.Write((byte)Types.Count);
        foreach (var type in Types)
        {
            if (writer.ShortTypeIds)
            {
                writer.Write((UInt16)type);
            }
            else
            {
                writer.Write(type);
            }
        }
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        for (var i = 0; i < Types.Count; i++)
        {
            writer.Write(story.Types[Types[i]].Name);
            if (i < Types.Count - 1) writer.Write(", ");
        }
    }
}

public enum FunctionType
{
    Event = 1,
    Query = 2,
    Call = 3,
    Database = 4,
    Proc = 5,
    SysQuery = 6,
    SysCall = 7,
    UserQuery = 8
}

public class Function : OsirisSerializable
{
    public UInt32 Line;
    public UInt32 ConditionReferences;
    public UInt32 ActionReferences;
    public NodeReference NodeRef;
    public FunctionType Type;
    public UInt32 Meta1;
    public UInt32 Meta2;
    public UInt32 Meta3;
    public UInt32 Meta4;
    public FunctionSignature Name;

    public void Read(OsiReader reader)
    {
        Line = reader.ReadUInt32();
        ConditionReferences = reader.ReadUInt32();
        ActionReferences = reader.ReadUInt32();
        NodeRef = reader.ReadNodeRef();
        Type = (FunctionType)reader.ReadByte();
        Meta1 = reader.ReadUInt32();
        Meta2 = reader.ReadUInt32();
        Meta3 = reader.ReadUInt32();
        Meta4 = reader.ReadUInt32();
        Name = new FunctionSignature();
        Name.Read(reader);
    }

    public void Write(OsiWriter writer)
    {
        writer.Write(Line);
        writer.Write(ConditionReferences);
        writer.Write(ActionReferences);
        NodeRef.Write(writer);
        writer.Write((byte)Type);
        writer.Write(Meta1);
        writer.Write(Meta2);
        writer.Write(Meta3);
        writer.Write(Meta4);
        Name.Write(writer);
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        writer.Write("{0} ", Type.ToString());
        Name.DebugDump(writer, story);
        if (NodeRef.IsValid)
        {
            var node = NodeRef.Resolve();
            writer.Write(" @ {0}({1})", node.Name, node.NumParams);
        }

        writer.Write(" CondRefs {0}, ActRefs {1}", ConditionReferences, ActionReferences);
        writer.WriteLine(" Meta ({0}, {1}, {2}, {3})", Meta1, Meta2, Meta3, Meta4);
    }
}
