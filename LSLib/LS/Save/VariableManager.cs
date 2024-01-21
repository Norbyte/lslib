using OpenTK.Mathematics;

namespace LSLib.LS.Save;

public class OsirisVariableHelper
{
    private Int32 NumericStringId;
    private Dictionary<string, Int32> IdentifierToKey = [];
    private Dictionary<Int32, string> KeyToIdentifier = [];

    public void Load(Node helper)
    {
        NumericStringId = (Int32)helper.Attributes["NumericStringId"].Value;

        foreach (var mapping in helper.Children["IdentifierTable"])
        {
            string name = (string)mapping.Attributes["MapKey"].Value;
            Int32 index = (Int32)mapping.Attributes["MapValue"].Value;
            IdentifierToKey.Add(name, index);
            KeyToIdentifier.Add(index, name);
        }
    }

    public Int32 GetKey(string variableName)
    {
        return IdentifierToKey[variableName];
    }

    public string GetName(Int32 variableIndex)
    {
        return KeyToIdentifier[variableIndex];
    }
}

abstract public class VariableHolder<TValue>
{
    protected List<TValue> Values = [];
    private List<UInt16> Remaps = [];
    
    public TValue GetRaw(int index)
    {
        if (index == 0)
        {
            return default;
        }

        var valueSlot = Remaps[index - 1];
        return Values[valueSlot];
    }

    public void Load(Node variableList)
    {
        LoadVariables(variableList);

        var remaps = (byte[])variableList.Attributes["Remaps"].Value;

        Remaps.Clear();
        Remaps.Capacity = remaps.Length / 2;

        using var ms = new MemoryStream(remaps);
        using var reader = new BinaryReader(ms);
        for (var i = 0; i < remaps.Length / 2; i++)
        {
            Remaps.Add(reader.ReadUInt16());
        }
    }

    abstract protected void LoadVariables(Node variableList);
}

public class IntVariableHolder : VariableHolder<Int32>
{
    public Int32? Get(int index)
    {
        var raw = GetRaw(index);
        if (raw == -1163005939) /* 0xbaadf00d */
        {
            return null;
        }
        else
        {
            return raw;
        }
    }

    override protected void LoadVariables(Node variableList)
    {
        var variables = (byte[])variableList.Attributes["Variables"].Value;
        var numVars = variables.Length / 4;

        Values.Clear();
        Values.Capacity = numVars;

        using var ms = new MemoryStream(variables);
        using var reader = new BinaryReader(ms);
        for (var i = 0; i < numVars; i++)
        {
            Values.Add(reader.ReadInt32());
        }
    }
}

public class Int64VariableHolder : VariableHolder<Int64>
{
    public Int64? Get(int index)
    {
        var raw = GetRaw(index);
        if (raw == -4995072469926809587) /* 0xbaadf00dbaadf00d */
        {
            return null;
        }
        else
        {
            return raw;
        }
    }

    override protected void LoadVariables(Node variableList)
    {
        var variables = (byte[])variableList.Attributes["Variables"].Value;
        var numVars = variables.Length / 8;

        Values.Clear();
        Values.Capacity = numVars;

        using var ms = new MemoryStream(variables);
        using var reader = new BinaryReader(ms);
        for (var i = 0; i < numVars; i++)
        {
            Values.Add(reader.ReadInt64());
        }
    }
}

public class FloatVariableHolder : VariableHolder<float>
{
    public float? Get(int index)
    {
        var raw = GetRaw(index);
        var intFloat = BitConverter.ToUInt32(BitConverter.GetBytes(raw), 0);
        if (intFloat == 0xbaadf00d)
        {
            return null;
        }
        else
        {
            return raw;
        }
    }

    override protected void LoadVariables(Node variableList)
    {
        var variables = (byte[])variableList.Attributes["Variables"].Value;
        var numVars = variables.Length / 4;

        Values.Clear();
        Values.Capacity = numVars;

        using var ms = new MemoryStream(variables);
        using var reader = new BinaryReader(ms);
        for (var i = 0; i < numVars; i++)
        {
            Values.Add(reader.ReadSingle());
        }
    }
}

public class StringVariableHolder : VariableHolder<string>
{
    public string Get(int index)
    {
        var raw = GetRaw(index);
        if (raw == "0xbaadf00d")
        {
            return null;
        }
        else
        {
            return raw;
        }
    }

    override protected void LoadVariables(Node variableList)
    {
        var variables = (byte[])variableList.Attributes["Variables"].Value;

        using var ms = new MemoryStream(variables);
        using var reader = new BinaryReader(ms);
        var numVars = reader.ReadInt32();

        Values.Clear();
        Values.Capacity = numVars;

        for (var i = 0; i < numVars; i++)
        {
            var length = reader.ReadUInt16();
            var bytes = reader.ReadBytes(length);
            var str = Encoding.UTF8.GetString(bytes);
            Values.Add(str);
        }
    }
}

public class Float3VariableHolder : VariableHolder<Vector3>
{
    public Vector3? Get(int index)
    {
        var raw = GetRaw(index);
        var intFloat = BitConverter.ToUInt32(BitConverter.GetBytes(raw.X), 0);
        if (intFloat == 0xbaadf00d)
        {
            return null;
        }
        else
        {
            return raw;
        }
    }

    override protected void LoadVariables(Node variableList)
    {
        var variables = (byte[])variableList.Attributes["Variables"].Value;
        var numVars = variables.Length / 12;

        Values.Clear();
        Values.Capacity = numVars;

        using var ms = new MemoryStream(variables);
        using var reader = new BinaryReader(ms);
        for (var i = 0; i < numVars; i++)
        {
            Vector3 vec = new()
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle()
            };
            Values.Add(vec);
        }
    }
}

internal enum VariableType
{
    Int = 0,
    Int64 = 1,
    Float = 2,
    String = 3,
    FixedString = 4,
    Float3 = 5
};

/// <summary>
/// Node (structure) entry in the LSF file
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct Key2TableEntry
{
    /// <summary>
    /// Index of variable from OsirisVariableHelper.IdentifierTable
    /// </summary>
    public UInt32 NameIndex;
    /// <summary>
    /// Index and type of value
    /// </summary>
    public UInt32 ValueIndexAndType;
    /// <summary>
    /// Handle of the object that this variable is assigned to.
    /// </summary>
    public UInt64 Handle;

    /// <summary>
    /// Index of value in the appropriate variable list
    /// </summary>
    public int ValueIndex
    {
        get { return (int)((ValueIndexAndType >> 3) & 0x3ff); }
    }

    /// <summary>
    /// Type of value
    /// </summary>
    public VariableType ValueType
    {
        get { return (VariableType)(ValueIndexAndType & 7); }
    }
};

public class VariableManager(OsirisVariableHelper variableHelper)
{
    private readonly Dictionary<int, Key2TableEntry> Keys = [];
    private readonly IntVariableHolder IntList = new();
    private readonly Int64VariableHolder Int64List = new();
    private readonly FloatVariableHolder FloatList = new();
    private readonly StringVariableHolder StringList = new();
    private readonly StringVariableHolder FixedStringList = new();
    private readonly Float3VariableHolder Float3List = new();

    public Dictionary<string, object> GetAll(bool includeDeleted = false)
    {
        var variables = new Dictionary<string, object>();
        foreach (var key in Keys.Values)
        {
            var name = variableHelper.GetName((int)key.NameIndex);
            var value = includeDeleted ? GetRaw(key.ValueType, key.ValueIndex) : Get(key.ValueType, key.ValueIndex);
            if (value != null)
            {
                variables.Add(name, value);
            }
        }

        return variables;
    }

    public object Get(string name)
    {
        var index = variableHelper.GetKey(name);
        var key = Keys[index];
        return Get(key.ValueType, key.ValueIndex);
    }

    private object Get(VariableType type, int index)
    {
        return type switch
        {
            VariableType.Int => IntList.Get(index),
            VariableType.Int64 => Int64List.Get(index),
            VariableType.Float => FloatList.Get(index),
            VariableType.String => StringList.Get(index),
            VariableType.FixedString => FixedStringList.Get(index),
            VariableType.Float3 => Float3List.Get(index),
            _ => throw new ArgumentException("Unsupported variable type"),
        };
    }

    public object GetRaw(string name)
    {
        var index = variableHelper.GetKey(name);
        var key = Keys[index];
        return GetRaw(key.ValueType, key.ValueIndex);
    }

    private object GetRaw(VariableType type, int index)
    {
        return type switch
        {
            VariableType.Int => IntList.GetRaw(index),
            VariableType.Int64 => Int64List.GetRaw(index),
            VariableType.Float => FloatList.GetRaw(index),
            VariableType.String => StringList.GetRaw(index),
            VariableType.FixedString => FixedStringList.GetRaw(index),
            VariableType.Float3 => Float3List.GetRaw(index),
            _ => throw new ArgumentException("Unsupported variable type"),
        };
    }

    private void LoadKeys(byte[] handleList)
    {
        Keys.Clear();

        using var ms = new MemoryStream(handleList);
        using var reader = new BinaryReader(ms);
        var numHandles = reader.ReadInt32();
        for (var i = 0; i < numHandles; i++)
        {
            var entry = BinUtils.ReadStruct<Key2TableEntry>(reader);
            Keys.Add((int)entry.NameIndex, entry);
        }
    }

    public void Load(Node variableManager)
    {
        List<Node> nodes;
        if (variableManager.Children.TryGetValue("IntList", out nodes))
        {
            IntList.Load(nodes[0]);
        }

        if (variableManager.Children.TryGetValue("Int64List", out nodes))
        {
            Int64List.Load(nodes[0]);
        }

        if (variableManager.Children.TryGetValue("FloatList", out nodes))
        {
            FloatList.Load(nodes[0]);
        }

        if (variableManager.Children.TryGetValue("StringList", out nodes))
        {
            StringList.Load(nodes[0]);
        }

        if (variableManager.Children.TryGetValue("FixedStringList", out nodes))
        {
            FixedStringList.Load(nodes[0]);
        }

        if (variableManager.Children.TryGetValue("Float3List", out nodes))
        {
            Float3List.Load(nodes[0]);
        }

        if (variableManager.Children.TryGetValue("Key2TableList", out nodes))
        {
            var handleList = (byte[])nodes[0].Attributes["HandleList"].Value;
            LoadKeys(handleList);
        }
    }
}
