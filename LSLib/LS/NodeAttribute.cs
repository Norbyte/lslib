using System;
using System.Collections.Generic;

namespace LSLib.LS
{
    public class TranslatedString
    {
        public UInt16 Version = 0;
        public string Value;
        public string Handle;

        public override string ToString()
        {
            return Value;
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

    public class NodeAttribute
    {
        public enum DataType
        {
            DT_None = 0,
            DT_Byte = 1,
            DT_Short = 2,
            DT_UShort = 3,
            DT_Int = 4,
            DT_UInt = 5,
            DT_Float = 6,
            DT_Double = 7,
            DT_IVec2 = 8,
            DT_IVec3 = 9,
            DT_IVec4 = 10,
            DT_Vec2 = 11,
            DT_Vec3 = 12,
            DT_Vec4 = 13,
            DT_Mat2 = 14,
            DT_Mat3 = 15,
            DT_Mat3x4 = 16,
            DT_Mat4x3 = 17,
            DT_Mat4 = 18,
            DT_Bool = 19,
            DT_String = 20,
            DT_Path = 21,
            DT_FixedString = 22,
            DT_LSString = 23,
            DT_ULongLong = 24,
            DT_ScratchBuffer = 25,
            // Seems to be unused?
            DT_Long = 26,
            DT_Int8 = 27,
            DT_TranslatedString = 28,
            DT_WString = 29,
            DT_LSWString = 30,
            DT_UUID = 31,
            DT_Int64 = 32,
            DT_TranslatedFSString = 33,
            // Last supported datatype, always keep this one at the end
            DT_Max = DT_TranslatedFSString
        };

        private DataType type;
        private object value;

        public DataType Type
        {
            get { return type; }
        }

        public object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public NodeAttribute(DataType type)
        {
            this.type = type;
        }

        public override string ToString()
        {
            switch (this.type)
            {
                case DataType.DT_ScratchBuffer:
                    // ScratchBuffer is a special case, as its stored as byte[] and ToString() doesn't really do what we want
                    return Convert.ToBase64String((byte[])this.value);

                case DataType.DT_IVec2:
                case DataType.DT_IVec3:
                case DataType.DT_IVec4:
                    return String.Join(" ", new List<int>((int[])this.value).ConvertAll(i => i.ToString()).ToArray());

                case DataType.DT_Vec2:
                case DataType.DT_Vec3:
                case DataType.DT_Vec4:
                    return String.Join(" ", new List<float>((float[])this.value).ConvertAll(i => i.ToString()).ToArray());

                default:
                    return this.value.ToString();
            }
        }

        public int GetRows()
        {
            switch (this.type)
            {
                case DataType.DT_IVec2:
                case DataType.DT_IVec3:
                case DataType.DT_IVec4:
                case DataType.DT_Vec2:
                case DataType.DT_Vec3:
                case DataType.DT_Vec4:
                    return 1;

                case DataType.DT_Mat2:
                    return 2;

                case DataType.DT_Mat3:
                case DataType.DT_Mat3x4:
                    return 3;

                case DataType.DT_Mat4x3:
                case DataType.DT_Mat4:
                    return 4;

                default:
                    throw new NotSupportedException("Data type does not have rows");
            }
        }

        public int GetColumns()
        {
            switch (this.type)
            {
                case DataType.DT_IVec2:
                case DataType.DT_Vec2:
                case DataType.DT_Mat2:
                    return 2;

                case DataType.DT_IVec3:
                case DataType.DT_Vec3:
                case DataType.DT_Mat3:
                case DataType.DT_Mat4x3:
                    return 3;

                case DataType.DT_IVec4:
                case DataType.DT_Vec4:
                case DataType.DT_Mat3x4:
                case DataType.DT_Mat4:
                    return 4;

                default:
                    throw new NotSupportedException("Data type does not have columns");
            }
        }

        public bool IsNumeric()
        {
            return this.type == DataType.DT_Byte
                || this.type == DataType.DT_Short
                || this.type == DataType.DT_Short
                || this.type == DataType.DT_Int
                || this.type == DataType.DT_UInt
                || this.type == DataType.DT_Float
                || this.type == DataType.DT_Double
                || this.type == DataType.DT_ULongLong
                || this.type == DataType.DT_Long
                || this.type == DataType.DT_Int8;
        }

        public void FromString(string str)
        {
            if (IsNumeric())
            {
                // Workaround: Some XML files use empty strings, instead of "0" for zero values.
                if (str == "")
                {
                    str = "0";
                }
                // Handle hexadecimal integers in XML files
                else if (str.Length > 2 && str.Substring(0, 2) == "0x")
                {
                    str = Convert.ToUInt64(str.Substring(2), 16).ToString();
                }
            }

            switch (this.type)
            {
                case DataType.DT_None:
                    // This is a null type, cannot have a value
                    break;

                case DataType.DT_Byte:
                    value = Convert.ToByte(str);
                    break;

                case DataType.DT_Short:
                    value = Convert.ToInt16(str);
                    break;

                case DataType.DT_UShort:
                    value = Convert.ToUInt16(str);
                    break;

                case DataType.DT_Int:
                    value = Convert.ToInt32(str);
                    break;

                case DataType.DT_UInt:
                    value = Convert.ToUInt32(str);
                    break;

                case DataType.DT_Float:
                    value = Convert.ToSingle(str);
                    break;

                case DataType.DT_Double:
                    value = Convert.ToDouble(str);
                    break;

                case DataType.DT_IVec2:
                case DataType.DT_IVec3:
                case DataType.DT_IVec4:
                    {
                        string[] nums = str.Split(' ');
                        int length = GetColumns();
                        if (length != nums.Length)
                            throw new FormatException(String.Format("A vector of length {0} was expected, got {1}", length, nums.Length));

                        int[] vec = new int[length];
                        for (int i = 0; i < length; i++)
                            vec[i] = int.Parse(nums[i]);

                        value = vec;
                        break;
                    }

                case DataType.DT_Vec2:
                case DataType.DT_Vec3:
                case DataType.DT_Vec4:
                    {
                        string[] nums = str.Split(' ');
                        int length = GetColumns();
                        if (length != nums.Length)
                            throw new FormatException(String.Format("A vector of length {0} was expected, got {1}", length, nums.Length));

                        float[] vec = new float[length];
                        for (int i = 0; i < length; i++)
                            vec[i] = float.Parse(nums[i]);

                        value = vec;
                        break;
                    }

                case DataType.DT_Mat2:
                case DataType.DT_Mat3:
                case DataType.DT_Mat3x4:
                case DataType.DT_Mat4x3:
                case DataType.DT_Mat4:
                    var mat = Matrix.Parse(str);
                    if (mat.cols != GetColumns() || mat.rows != GetRows())
                        throw new FormatException("Invalid column/row count for matrix");
                    value = mat;
                    break;

                case DataType.DT_Bool:
                    if (str == "0") value = false;
                    else if (str == "1") value = true;
                    else value = Convert.ToBoolean(str);
                    break;

                case DataType.DT_String:
                case DataType.DT_Path:
                case DataType.DT_FixedString:
                case DataType.DT_LSString:
                case DataType.DT_WString:
                case DataType.DT_LSWString:
                    value = str;
                    break;

                case DataType.DT_TranslatedString:
                    // We'll only set the value part of the translated string, not the TranslatedStringKey / Handle part
                    // That can be changed separately via attribute.Value.Handle
                    if (value == null)
                        value = new TranslatedString();

                    ((TranslatedString)value).Value = str;
                    break;

                case DataType.DT_TranslatedFSString:
                    // We'll only set the value part of the translated string, not the TranslatedStringKey / Handle part
                    // That can be changed separately via attribute.Value.Handle
                    if (value == null)
                        value = new TranslatedFSString();

                    ((TranslatedFSString)value).Value = str;
                    break;

                case DataType.DT_ULongLong:
                    value = Convert.ToUInt64(str);
                    break;

                case DataType.DT_ScratchBuffer:
                    value = Convert.FromBase64String(str);
                    break;

                case DataType.DT_Long:
                case DataType.DT_Int64:
                    value = Convert.ToInt64(str);
                    break;

                case DataType.DT_Int8:
                    value = Convert.ToSByte(str);
                    break;

                case DataType.DT_UUID:
                    value = new Guid(str);
                    break;

                default:
                    // This should not happen!
                    throw new NotImplementedException(String.Format("FromString() not implemented for type {0}", this.type));
            }
        }
    }
}
