namespace LSLib.LS.Story;

public class Value : OsirisSerializable
{
    // Original Sin 2 (v1.11) Type ID-s
    public enum Type : uint
    {
        None = 0,
        Integer = 1,
        Integer64 = 2,
        Float = 3,
        String = 4,
        GuidString = 5
    }

    // Original Sin 1 (v1.0 - v1.7) Type ID-s
    public enum Type_OS1 : uint
    {
        None = 0,
        Integer = 1,
        Float = 2,
        String = 3
    }

    public UInt32 TypeId;
    public Int32 IntValue;
    public Int64 Int64Value;
    public Single FloatValue;
    public String StringValue;

    public override string ToString()
    {
        switch ((Type)TypeId)
        {
            case Type.None: return "";
            case Type.Integer: return IntValue.ToString();
            case Type.Integer64: return Int64Value.ToString();
            case Type.Float: return FloatValue.ToString();
            case Type.String: return StringValue;
            case Type.GuidString: return StringValue;
            default: return StringValue;
        }
    }

    public static uint ConvertOS1ToOS2Type(uint os1TypeId)
    {
        // Convert D:OS 1 type ID to D:OS 2 type ID
        switch ((Type_OS1)os1TypeId)
        {
            case Type_OS1.None:
                return (uint)Type.None;

            case Type_OS1.Integer:
                return (uint)Type.Integer;

            case Type_OS1.Float:
                return (uint)Type.Float;

            case Type_OS1.String:
                return (uint)Type.String;

            default:
                return os1TypeId;
        }
    }

    public static uint ConvertOS2ToOS1Type(uint os2TypeId)
    {
        // Convert D:OS 2 type ID to D:OS 1 type ID
        switch ((Type)os2TypeId)
        {
            case Type.None:
                return (uint)Type_OS1.None;

            case Type.Integer:
            case Type.Integer64:
                return (uint)Type_OS1.Integer;

            case Type.Float:
                return (uint)Type_OS1.Float;

            case Type.String:
            case Type.GuidString:
                return (uint)Type_OS1.String;

            default:
                return os2TypeId;
        }
    }

    public Type GetBuiltinTypeId(Story story)
    {
        var aliasId = story.FindBuiltinTypeId(TypeId);

        if (story.Version < OsiVersion.VerEnhancedTypes)
        {
            return (Type)ConvertOS1ToOS2Type(aliasId);
        }
        else
        {
            return (Type)aliasId;
        }
    }

    public virtual void Read(OsiReader reader)
    {
        // possibly isReference?
        var wtf = reader.ReadByte();
        if (wtf == '1')
        {
            if (reader.ShortTypeIds == true)
            {
                TypeId = reader.ReadUInt16();
            }
            else
            {
                TypeId = reader.ReadUInt32();
            }

            IntValue = reader.ReadInt32();
        }
        else if (wtf == '0')
        {
            if (reader.ShortTypeIds == true)
            {
                TypeId = reader.ReadUInt16();
            }
            else
            {
                TypeId = reader.ReadUInt32();
            }

            uint writtenTypeId = TypeId;

            uint alias;
            bool dos1alias = false;
            if (reader.TypeAliases.TryGetValue(writtenTypeId, out alias))
            {
                writtenTypeId = alias;
                if (reader.Ver < OsiVersion.VerEnhancedTypes)
                {
                    dos1alias = true;
                }
            }

            if (reader.Ver < OsiVersion.VerEnhancedTypes)
            {
                // Convert D:OS 1 type ID to D:OS 2 type ID
                writtenTypeId = ConvertOS1ToOS2Type(writtenTypeId);
            }

            switch ((Type)writtenTypeId)
            {
                case Type.None:
                    break;

                case Type.Integer:
                    IntValue = reader.ReadInt32();
                    break;

                case Type.Integer64:
                    Int64Value = reader.ReadInt64();
                    break;

                case Type.Float:
                    FloatValue = reader.ReadSingle();
                    break;

                case Type.GuidString:
                case Type.String:
                    // D:OS 1 aliased strings didn't have a flag byte
                    if (dos1alias)
                    {
                        StringValue = reader.ReadString();
                    }
                    else if (reader.ReadByte() > 0)
                    {
                        StringValue = reader.ReadString();
                    }
                    break;

                default:
                    StringValue = reader.ReadString();
                    break;
            }
        }
        else if (wtf == 'e')
        {
            TypeId = reader.ReadUInt16();

            OsirisEnum e;
            if (!reader.Story.Enums.TryGetValue(TypeId, out e))
            {
                throw new InvalidDataException($"Enum label serialized for a non-enum type: {TypeId}");
            }

            StringValue = reader.ReadString();
            var ele = e.Elements.Find(v => v.Name == StringValue);
            if (ele == null)
            {
                throw new InvalidDataException($"Enumeration {TypeId} has no label named '{StringValue}'");
            }
        }
        else
        {
            throw new InvalidDataException("Unrecognized value type");
        }
    }

    public virtual void Write(OsiWriter writer)
    {
        if (writer.Enums.ContainsKey(TypeId))
        {
            writer.Write((byte)'e');
            writer.Write((UInt16)TypeId);
            writer.Write(StringValue);
            return;
        }

        // TODO: Is the == 0x31 case ever used when reading?
        writer.Write((byte)'0');

        uint writtenTypeId = TypeId;
        bool aliased = false;
        uint alias;
        if (writer.TypeAliases.TryGetValue(TypeId, out alias))
        {
            aliased = true;
            writtenTypeId = alias;
        }

        if (writer.ShortTypeIds)
        {
            writer.Write((UInt16)TypeId);
        }
        else
        {
            writer.Write(TypeId);
        }

        if (writer.Ver < OsiVersion.VerEnhancedTypes)
        {
            // Make sure that we're serializing using the D:OS2 type ID
            // (The alias map contains the D:OS 1 ID)
            writtenTypeId = ConvertOS1ToOS2Type(writtenTypeId);
        }

        switch ((Type)writtenTypeId)
        {
            case Type.None:
                break;

            case Type.Integer:
                writer.Write(IntValue);
                break;

            case Type.Integer64:
                // D:OS 1 aliased strings didn't have a flag byte
                if (writer.Ver >= OsiVersion.VerEnhancedTypes)
                {
                    writer.Write(Int64Value);
                }
                else
                {
                    writer.Write((int)Int64Value);
                }

                break;

            case Type.Float:
                writer.Write(FloatValue);
                break;

            case Type.String:
            case Type.GuidString:
                if (!aliased || (writer.Ver >= OsiVersion.VerEnhancedTypes))
                {
                    writer.Write(StringValue != null);
                }

                if (StringValue != null)
                    writer.Write(StringValue);
                break;

            default:
                writer.Write(StringValue);
                break;
        }
    }

    public virtual void DebugDump(TextWriter writer, Story story)
    {
        var builtinTypeId = GetBuiltinTypeId(story);

        switch (builtinTypeId)
        {
            case Type.None:
                writer.Write("<unknown>");
                break;

            case Type.Integer:
                writer.Write(IntValue);
                break;

            case Type.Integer64:
                writer.Write(Int64Value);
                break;

            case Type.Float:
                writer.Write(FloatValue);
                break;

            case Type.String:
                writer.Write("'{0}'", StringValue);
                break;

            case Type.GuidString:
                writer.Write(StringValue);
                break;

            default:
                throw new Exception("Unsupported builtin type ID");
        }
    }

    public virtual void MakeScript(TextWriter writer, Story story, Tuple tuple, bool printTypes = false)
    {
        var builtinTypeId = GetBuiltinTypeId(story);

        switch (builtinTypeId)
        {
            case Type.None:
                throw new InvalidDataException("Script cannot contain unknown values");

            case Type.Integer:
                writer.Write(IntValue);
                break;

            case Type.Integer64:
                writer.Write(IntValue);
                break;

            case Type.Float:
                writer.Write((decimal)FloatValue);
                break;

            case Type.String:
                writer.Write("\"{0}\"", StringValue);
                break;

            case Type.GuidString:
                writer.Write(StringValue);
                break;

            default:
                throw new Exception("Unsupported builtin type ID");
        }
    }
}

public class TypedValue : Value
{
    public bool IsValid;
    public bool OutParam;
    public bool IsAType;

    public override void Read(OsiReader reader)
    {
        base.Read(reader);
        IsValid = reader.ReadBoolean();
        OutParam = reader.ReadBoolean();
        IsAType = reader.ReadBoolean();
    }

    public override void Write(OsiWriter writer)
    {
        base.Write(writer);
        writer.Write(IsValid);
        writer.Write(OutParam);
        writer.Write(IsAType);
    }

    public override void DebugDump(TextWriter writer, Story story)
    {
        if (IsValid) writer.Write("valid ");
        if (OutParam) writer.Write("out ");
        if (IsAType) writer.Write("type ");

        if (IsValid)
        {
            base.DebugDump(writer, story);
        }
        else
        {
            writer.Write("<{0}>", story.Types[TypeId].Name);
        }
    }
}

public class Variable : TypedValue
{
    public sbyte Index;
    public bool Unused;
    public bool Adapted;
    public string VariableName;

    public override void Read(OsiReader reader)
    {
        base.Read(reader);
        Index = reader.ReadSByte();
        Unused = reader.ReadBoolean();
        Adapted = reader.ReadBoolean();
    }

    public override void Write(OsiWriter writer)
    {
        base.Write(writer);
        writer.Write(Index);
        writer.Write(Unused);
        writer.Write(Adapted);
    }

    public override void DebugDump(TextWriter writer, Story story)
    {
        writer.Write("#{0} ", Index);
        if (VariableName != null && VariableName.Length > 0) writer.Write("'{0}' ", VariableName);
        if (Unused) writer.Write("unused ");
        if (Adapted) writer.Write("adapted ");
        base.DebugDump(writer, story);
    }

    public override void MakeScript(TextWriter writer, Story story, Tuple tuple, bool printTypes = false)
    {
        if (Unused)
        {
            if (printTypes && TypeId > 0)
            {
                writer.Write("({0})", story.Types[TypeId].Name);
            }

            writer.Write("_");
        }
        else if (Adapted)
        {
            if (VariableName != null && VariableName.Length > 0)
            {
                if (printTypes && TypeId > 0)
                {
                    writer.Write("({0})", story.Types[TypeId].Name);
                }

                writer.Write(VariableName);
            }
            else
            {
                tuple.Logical[Index].MakeScript(writer, story, null);
            }
        }
        else
        {
            base.MakeScript(writer, story, tuple);
        }
    }
}

public class Tuple : OsirisSerializable
{
    public List<Value> Physical = new List<Value>();
    public Dictionary<int, Value> Logical = new Dictionary<int, Value>();

    public void Read(OsiReader reader)
    {
        Physical.Clear();
        Logical.Clear();

        var count = reader.ReadByte();
        while (count-- > 0)
        {
            var index = reader.ReadByte();
            var value = new Value();
            value.Read(reader);

            Physical.Add(value);
            Logical.Add(index, value);
        }
    }

    public void Write(OsiWriter writer)
    {
        writer.Write((byte)Logical.Count);
        foreach (var logical in Logical)
        {
            writer.Write((byte)logical.Key);
            logical.Value.Write(writer);
        }
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        writer.Write("(");
        var keys = Logical.Keys.ToArray();
        for (var i = 0; i < Logical.Count; i++)
        {
            writer.Write("{0}: ", keys[i]);
            Logical[keys[i]].DebugDump(writer, story);
            if (i < Logical.Count - 1) writer.Write(", ");
        }
        writer.Write(")");
    }

    public void MakeScript(TextWriter writer, Story story, bool printTypes = false)
    {
        for (var i = 0; i < Physical.Count; i++)
        {
            var value = Physical[i];
            value.MakeScript(writer, story, null, printTypes);
            if (i < Physical.Count - 1)
                writer.Write(", ");
        }
    }
}
