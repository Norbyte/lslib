using System;
using System.Collections.Generic;
using System.Linq;

namespace LSLib.LS;

public class TranslatedString
{
    public UInt16 Version = 0;
    public string Value;
    public string Handle;

    public override string ToString()
    {
        if (Value != null && Value != "")
        {
            return Value;
        }
        else
        {
            return Handle + ";" + Version;
        }
    }
}

public class TranslatedFSStringArgument
{
    public string Key;
    public TranslatedFSString String;
    public string Value;
}

public class TranslatedFSString : TranslatedString
{
    public List<TranslatedFSStringArgument> Arguments;
}

public class NodeSerializationSettings
{
    public bool DefaultByteSwapGuids = true;
    public bool ByteSwapGuids = true;

    public void InitFromMeta(string meta)
    {
        if (meta.Length == 0)
        {
            // No metadata available, use defaults
            ByteSwapGuids = DefaultByteSwapGuids;
        }
        else
        {
            var tags = meta.Split(',');
            ByteSwapGuids = tags.Contains("bswap_guids");
        }
    }

    public string BuildMeta()
    {
        List<string> tags = [ "v1" ];
        if (ByteSwapGuids)
        {
            tags.Add("bswap_guids");
        }

        return String.Join(",", tags);
    }
}
public enum AttributeType
{
    None = 0,
    Byte = 1,
    Short = 2,
    UShort = 3,
    Int = 4,
    UInt = 5,
    Float = 6,
    Double = 7,
    IVec2 = 8,
    IVec3 = 9,
    IVec4 = 10,
    Vec2 = 11,
    Vec3 = 12,
    Vec4 = 13,
    Mat2 = 14,
    Mat3 = 15,
    Mat3x4 = 16,
    Mat4x3 = 17,
    Mat4 = 18,
    Bool = 19,
    String = 20,
    Path = 21,
    FixedString = 22,
    LSString = 23,
    ULongLong = 24,
    ScratchBuffer = 25,
    // Seems to be unused?
    Long = 26,
    Int8 = 27,
    TranslatedString = 28,
    WString = 29,
    LSWString = 30,
    UUID = 31,
    Int64 = 32,
    TranslatedFSString = 33,
    // Last supported datatype, always keep this one at the end
    Max = TranslatedFSString
};

public static class AttributeTypeExtensions
{
    public static int GetRows(this AttributeType type)
    {
        switch (type)
        {
            case AttributeType.IVec2:
            case AttributeType.IVec3:
            case AttributeType.IVec4:
            case AttributeType.Vec2:
            case AttributeType.Vec3:
            case AttributeType.Vec4:
                return 1;

            case AttributeType.Mat2:
                return 2;

            case AttributeType.Mat3:
            case AttributeType.Mat3x4:
                return 3;

            case AttributeType.Mat4x3:
            case AttributeType.Mat4:
                return 4;

            default:
                throw new NotSupportedException("Data type does not have rows");
        }
    }

    public static int GetColumns(this AttributeType type)
    {
        switch (type)
        {
            case AttributeType.IVec2:
            case AttributeType.Vec2:
            case AttributeType.Mat2:
                return 2;

            case AttributeType.IVec3:
            case AttributeType.Vec3:
            case AttributeType.Mat3:
            case AttributeType.Mat4x3:
                return 3;

            case AttributeType.IVec4:
            case AttributeType.Vec4:
            case AttributeType.Mat3x4:
            case AttributeType.Mat4:
                return 4;

            default:
                throw new NotSupportedException("Data type does not have columns");
        }
    }

    public static bool IsNumeric(this AttributeType type)
    {
        return type == AttributeType.Byte
            || type == AttributeType.Short
            || type == AttributeType.Short
            || type == AttributeType.Int
            || type == AttributeType.UInt
            || type == AttributeType.Float
            || type == AttributeType.Double
            || type == AttributeType.ULongLong
            || type == AttributeType.Long
            || type == AttributeType.Int8;
    }
}

public class NodeAttribute(AttributeType type)
{
    private readonly AttributeType type = type;
    private object value;
    public int? Line = null;

    public AttributeType Type
    {
        get { return type; }
    }

    public object Value
    {
        get { return value; }
        set { this.value = value; }
    }

    public override string ToString()
    {
        throw new NotImplementedException("ToString() is not safe to use anymore, AsString(settings) instead");
    }

    public static Guid ByteSwapGuid(Guid g)
    {
        var bytes = g.ToByteArray();
        for (var i = 8; i < 16; i += 2)
        {
            (bytes[i + 1], bytes[i]) = (bytes[i], bytes[i + 1]);
        }

        return new Guid(bytes);
    }

    public string AsString(NodeSerializationSettings settings)
    {
        switch (type)
        {
            case AttributeType.ScratchBuffer:
                // ScratchBuffer is a special case, as its stored as byte[] and ToString() doesn't really do what we want
                return Convert.ToBase64String((byte[])value);

            case AttributeType.IVec2:
            case AttributeType.IVec3:
            case AttributeType.IVec4:
                return String.Join(" ", new List<int>((int[])value).ConvertAll(i => i.ToString()).ToArray());

            case AttributeType.Vec2:
            case AttributeType.Vec3:
            case AttributeType.Vec4:
                return String.Join(" ", new List<float>((float[])value).ConvertAll(i => i.ToString()).ToArray());

            case AttributeType.UUID:
                if (settings.ByteSwapGuids)
                {
                    return ByteSwapGuid((Guid)value).ToString();
                }
                else
                {
                    return value.ToString();
                }

            default:
                return value.ToString();
        }
    }

    public Guid AsGuid(NodeSerializationSettings settings)
    {
        return AsGuid(settings.ByteSwapGuids);
    }

    public Guid AsGuid()
    {
        return AsGuid(true);
    }

    public Guid AsGuid(bool byteSwapGuids)
    {
        switch (type)
        {
            case AttributeType.UUID:
                return (Guid)value;

            case AttributeType.String:
            case AttributeType.FixedString:
            case AttributeType.LSString:
                if (byteSwapGuids)
                {
                    return ByteSwapGuid(Guid.Parse((string)value));
                }
                else
                {
                    return Guid.Parse((string)value);
                }

            default:
                throw new NotSupportedException("Type not convertible to GUID");
        }
    }

    public void FromString(string str, NodeSerializationSettings settings)
    {
        value = ParseFromString(str, type, settings);
    }

    public static object ParseFromString(string str, AttributeType type, NodeSerializationSettings settings)
    {
        if (type.IsNumeric())
        {
            // Workaround: Some XML files use empty strings, instead of "0" for zero values.
            if (str == "")
            {
                str = "0";
            }
            // Handle hexadecimal integers in XML files
            else if (str.Length > 2 && str[..2] == "0x")
            {
                str = Convert.ToUInt64(str[2..], 16).ToString();
            }
        }

        switch (type)
        {
            case AttributeType.None:
                // This is a null type, cannot have a value
                return null;

            case AttributeType.Byte:
                return Convert.ToByte(str);

            case AttributeType.Short:
                return Convert.ToInt16(str);

            case AttributeType.UShort:
                return Convert.ToUInt16(str);

            case AttributeType.Int:
                return Convert.ToInt32(str);

            case AttributeType.UInt:
                return Convert.ToUInt32(str);

            case AttributeType.Float:
                return Convert.ToSingle(str);

            case AttributeType.Double:
                return Convert.ToDouble(str);

            case AttributeType.IVec2:
            case AttributeType.IVec3:
            case AttributeType.IVec4:
                {
                    string[] nums = str.Split(' ');
                    int length = type.GetColumns();
                    if (length != nums.Length)
                        throw new FormatException(String.Format("A vector of length {0} was expected, got {1}", length, nums.Length));

                    int[] vec = new int[length];
                    for (int i = 0; i < length; i++)
                        vec[i] = int.Parse(nums[i]);

                    return vec;
                }

            case AttributeType.Vec2:
            case AttributeType.Vec3:
            case AttributeType.Vec4:
                {
                    string[] nums = str.Split(' ');
                    int length = type.GetColumns();
                    if (length != nums.Length)
                        throw new FormatException(String.Format("A vector of length {0} was expected, got {1}", length, nums.Length));

                    float[] vec = new float[length];
                    for (int i = 0; i < length; i++)
                        vec[i] = float.Parse(nums[i]);

                    return vec;
                }

            case AttributeType.Mat2:
            case AttributeType.Mat3:
            case AttributeType.Mat3x4:
            case AttributeType.Mat4x3:
            case AttributeType.Mat4:
                var mat = Matrix.Parse(str);
                if (mat.cols != type.GetColumns() || mat.rows != type.GetRows())
                    throw new FormatException("Invalid column/row count for matrix");
                return mat;

            case AttributeType.Bool:
                if (str == "0") return false;
                else if (str == "1") return true;
                else return Convert.ToBoolean(str);

            case AttributeType.String:
            case AttributeType.Path:
            case AttributeType.FixedString:
            case AttributeType.LSString:
            case AttributeType.WString:
            case AttributeType.LSWString:
                return str;

            case AttributeType.TranslatedString:
                {
                    // We'll only set the value part of the translated string, not the TranslatedStringKey / Handle part
                    // That can be changed separately via attribute.Value.Handle
                    var value = new TranslatedString
                    {
                        Value = str
                    };
                    return value;
                }

            case AttributeType.TranslatedFSString:
                {
                    // We'll only set the value part of the translated string, not the TranslatedStringKey / Handle part
                    // That can be changed separately via attribute.Value.Handle
                    var value = new TranslatedFSString
                    {
                        Value = str
                    };
                    return value;
                }

            case AttributeType.ULongLong:
                return Convert.ToUInt64(str);

            case AttributeType.ScratchBuffer:
                return Convert.FromBase64String(str);

            case AttributeType.Long:
            case AttributeType.Int64:
                return Convert.ToInt64(str);

            case AttributeType.Int8:
                return Convert.ToSByte(str);

            case AttributeType.UUID:
                if (settings.ByteSwapGuids)
                {
                    return ByteSwapGuid(new Guid(str));
                }
                else
                {
                    return new Guid(str);
                }

            default:
                // This should not happen!
                throw new NotImplementedException(String.Format("FromString() not implemented for type {0}", type));
        }
    }
}
