using System.ComponentModel;

namespace LSLib.LS.Story;

public class Fact : OsirisSerializable
{
    public List<Value> Columns;

    public void Read(OsiReader reader)
    {
        Columns = new List<Value>();
        var count = reader.ReadByte();
        while (count-- > 0)
        {
            var value = new Value();
            value.Read(reader);
            Columns.Add(value);
        }
    }

    public void Write(OsiWriter writer)
    {
        writer.Write((byte)Columns.Count);
        foreach (var column in Columns)
        {
            column.Write(writer);
        }
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        writer.Write("(");
        for (var i = 0; i < Columns.Count; i++)
        {
            Columns[i].DebugDump(writer, story);
            if (i < Columns.Count - 1) writer.Write(", ");
        }
        writer.Write(")");
    }
}

internal class FactPropertyDescriptor : PropertyDescriptor
{
    public int Index { get; private set; }
    public Value.Type BaseType { get; private set; }
    public byte Type { get; private set; }

    public FactPropertyDescriptor(int index, Value.Type baseType, byte type)
        : base(index.ToString(), new Attribute[0])
    {
        Index = index;
        BaseType = baseType;
        Type = type;
    }

    public override bool CanResetValue(object component)
    {
        return false;
    }

    public override Type ComponentType
    {
        get { return typeof(Fact); }
    }

    public override object GetValue(object component)
    {
        Fact fact = (Fact)component;
        return fact.Columns[Index].ToString();
    }

    public override bool IsReadOnly
    {
        get { return false; }
    }

    public override Type PropertyType
    {
        get
        {
            switch (BaseType)
            {
                case Value.Type.Integer: return typeof(Int32);
                case Value.Type.Integer64: return typeof(Int64);
                case Value.Type.Float: return typeof(Single);
                case Value.Type.String:
                case Value.Type.GuidString: return typeof(String);
                case Value.Type.None:
                default: throw new InvalidOperationException("Cannot retrieve type of an unknown column");
            }
        }
    }

    public override void ResetValue(object component)
    {
        throw new NotImplementedException();
    }

    public override void SetValue(object component, object value)
    {
        Fact fact = (Fact)component;
        var column = fact.Columns[Index];

        switch (BaseType)
        {
            case Value.Type.Integer:
                {
                    if (value is String) column.IntValue = Int32.Parse((String)value);
                    else if (value is Int32) column.IntValue = (Int32)value;
                    else throw new ArgumentException("Invalid Int32 value");
                    break;
                }

            case Value.Type.Integer64:
                {
                    if (value is String) column.Int64Value = Int64.Parse((String)value);
                    else if (value is Int64) column.Int64Value = (Int64)value;
                    else throw new ArgumentException("Invalid Int64 value");
                    break;
                }

            case Value.Type.Float:
                {
                    if (value is String) column.FloatValue = Single.Parse((String)value);
                    else if (value is Single) column.FloatValue = (Single)value;
                    else throw new ArgumentException("Invalid float value");
                    break;
                }

            case Value.Type.String:
            case Value.Type.GuidString:
                {
                    column.StringValue = (String)value;
                    break;
                }

            case Value.Type.None:
            default:
                throw new InvalidOperationException("Cannot retrieve type of an unknown column");
        }
    }

    public override bool ShouldSerializeValue(object component)
    {
        return false;
    }
}


public class FactCollection : List<Fact>, ITypedList
{
    private Story Story;
    private Database Database;
    private PropertyDescriptorCollection Properties;

    public FactCollection(Database database, Story story)
        : base()
    {
        Database = database;
        Story = story;
    }

    public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
    {
        if (Properties == null)
        {
            var props = new List<PropertyDescriptor>();
            var types = Database.Parameters.Types;
            for (var i = 0; i < types.Count; i++)
            {
                var type = Story.Types[types[i]];
                Value.Type baseType;
                if (type.Alias != 0)
                    baseType = (Value.Type)type.Alias;
                else
                    baseType = (Value.Type)type.Index;
                props.Add(new FactPropertyDescriptor(i, baseType, type.Index));
            }

            Properties = new PropertyDescriptorCollection(props.ToArray(), true);
        }

        return Properties;
    }

    public string GetListName(PropertyDescriptor[] listAccessors)
    {
        return "";
    }
}

public class Database : OsirisSerializable
{
    public UInt32 Index;
    public ParameterList Parameters;
    public FactCollection Facts;
    public Node OwnerNode;
    public long FactsPosition;

    public void Read(OsiReader reader)
    {
        Index = reader.ReadUInt32();
        Parameters = new ParameterList();
        Parameters.Read(reader);

        FactsPosition = reader.BaseStream.Position;
        Facts = new FactCollection(this, reader.Story);
        reader.ReadList<Fact>(Facts);
    }

    public void Write(OsiWriter writer)
    {
        Parameters.Write(writer);
        writer.WriteList<Fact>(Facts);
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        if (OwnerNode != null && OwnerNode.Name.Length > 0)
        {
            writer.Write("{0}({1})", OwnerNode.Name, OwnerNode.NumParams);
        }
        else if (OwnerNode != null)
        {
            writer.Write("<{0}>", OwnerNode.TypeName());
        }
        else
        {
            writer.Write("(Not owned)");
        }

        writer.Write(" @ {0:X}: ", FactsPosition);
        Parameters.DebugDump(writer, story);

        writer.WriteLine("");
        writer.WriteLine("    Facts: ");
        foreach (var fact in Facts)
        {
            writer.Write("        ");
            fact.DebugDump(writer, story);
            writer.WriteLine();
        }
    }
}
