using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LSLib.LS
{
    public class TranslatedString
    {
        public string Value;
        public string Handle;

        public override string ToString()
        {
            return Value;
        }
    }

    public class NodeAttribute
    {
        public enum DataType
        {
            DT_None = 0,
            DT_Byte,
            DT_Short,
            DT_UShort,
            DT_Int,
            DT_UInt,
            DT_Float,
            DT_Double,
            DT_IVec2,
            DT_IVec3,
            DT_IVec4,
            DT_Vec2,
            DT_Vec3,
            DT_Vec4,
            DT_Mat2,
            DT_Mat3,
            DT_Mat3x4,
            DT_Mat4x3,
            DT_Mat4,
            DT_Bool,
            DT_String,
            DT_Path,
            DT_FixedString,
            DT_LSString,
            DT_ULongLong,
            DT_ScratchBuffer,
            DT_Long,
            DT_Int8,
            DT_TranslatedString,
            DT_WString,
            DT_LSWString,
            // Last supported datatype, always keep this one at the end
            DT_Max = DT_LSWString
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

        public void FromString(string str)
        {
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
                    value = Convert.ToBoolean(str);
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

                case DataType.DT_ULongLong:
                    value = Convert.ToUInt64(str);
                    break;

                case DataType.DT_ScratchBuffer:
                    value = Convert.FromBase64String(str);
                    break;

                case DataType.DT_Long:
                    value = Convert.ToInt64(str);
                    break;

                case DataType.DT_Int8:
                    value = Convert.ToSByte(str);
                    break;

                default:
                    // This should not happen!
                    throw new NotImplementedException(String.Format("FromString() not implemented for type {0}", this.type));
            }
        }
    }
}
