using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS.Story
{
    public class Value : OsirisSerializable
    {
        public enum Type : uint
        {
            Unknown = 0,
            Integer = 1,
            Float = 2,
            String = 3
        }

        public UInt32 TypeId;
        public Int32 IntValue;
        public Single FloatValue;
        public String StringValue;

        public override string ToString()
        {
            switch ((Type)TypeId)
            {
                case Type.Unknown: return "";
                case Type.Integer: return IntValue.ToString();
                case Type.Float: return FloatValue.ToString();
                case Type.String: return StringValue;
                default: return StringValue;
            }
        }

        public virtual void Read(OsiReader reader)
        {
            var wtf = reader.ReadByte();
            if (wtf == 49)
            {
                TypeId = reader.ReadUInt32();
                IntValue = reader.ReadInt32();
            }
            else if (wtf == 48)
            {
                TypeId = reader.ReadUInt32();
                switch ((Type)TypeId)
                {
                    case Type.Unknown:
                        break;

                    case Type.Integer:
                        IntValue = reader.ReadInt32();
                        break;

                    case Type.Float:
                        FloatValue = reader.ReadSingle();
                        break;

                    case Type.String:
                        if (reader.ReadByte() > 0)
                        {
                            StringValue = reader.ReadString();
                        }
                        break;

                    default:
                        StringValue = reader.ReadString();
                        break;
                }
            }
            else
            {
                throw new InvalidDataException("Unrecognized value type");
            }
        }

        public virtual void Write(OsiWriter writer)
        {
            writer.Write((byte)48);
            writer.Write(TypeId);
            switch ((Type)TypeId)
            {
                case Type.Unknown:
                    break;

                case Type.Integer:
                    writer.Write(IntValue);
                    break;

                case Type.Float:
                    writer.Write(FloatValue);
                    break;

                case Type.String:
                    writer.Write(StringValue != null);
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
            switch ((Type)TypeId)
            {
                case Type.Unknown:
                    writer.Write("<unknown>");
                    break;

                case Type.Integer:
                    writer.Write(IntValue);
                    break;

                case Type.Float:
                    writer.Write(FloatValue);
                    break;

                case Type.String:
                    writer.Write("'{0}'", StringValue);
                    break;

                default:
                    writer.Write(StringValue);
                    break;
            }
        }

        public virtual void MakeScript(TextWriter writer, Story story, Tuple tuple, bool printTypes = false)
        {
            switch ((Type)TypeId)
            {
                case Type.Unknown:
                    throw new InvalidDataException("Script cannot contain unknown values");

                case Type.Integer:
                    writer.Write(IntValue);
                    break;

                case Type.Float:
                    writer.Write(FloatValue);
                    break;

                case Type.String:
                    writer.Write("\"{0}\"", StringValue);
                    break;

                default:
                    writer.Write(StringValue);
                    break;
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
                writer.Write("_");
            }
            else if (Adapted)
            {
                if (VariableName != null && VariableName.Length > 0)
                {
                    if (printTypes)
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
}
