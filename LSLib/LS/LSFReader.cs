using LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LSLib.LS
{
    public struct LSFHeader
    {
        public UInt32 Magic;
        public UInt32 Version;
        public UInt32 Unknown;
        public UInt32 NamesUncompressedSize;
        public UInt32 NamesSizeOnDisk;
        public UInt32 StructsUncompressedSize;
        public UInt32 StructsSizeOnDisk;
        public UInt32 FieldsUncompressedSize;
        public UInt32 FieldsSizeOnDisk;
        public UInt32 ValuesUncompressedSize;
        public UInt32 ValuesSizeOnDisk;
        public Byte CompressionFlags;
        public Byte Unknown2;
        public UInt16 Unknown3;
        public UInt32 Unknown4;
    }

    internal struct StructElement
    {
        public Int32 NameIndex;
        public Int32 LastFieldIndex;
        public Int32 ParentIndex;
    };

    internal class ResolvedStruct
    {
        public int ParentIndex;
        public int NameIndex;
        public int NameOffset;
        public int LastFieldIndex;
        public int Reference;
    };

    internal struct FieldElement
    {
        public Int32 NameIndex;
        public UInt32 TypeAndOffset;
        public Int32 LastFieldIndex;
    };

    internal class ResolvedField
    {
        public int NameIndex;
        public int NameOffset;
        public uint Type;
        public uint DataOffset;
        public int Reference;
    };

    public class LSFReader : IDisposable
    {
        private Stream Stream;
        private BinaryReader Reader;
        private List<List<String>> Names;
        private List<ResolvedStruct> Structs;
        private List<ResolvedField> Fields;
        private List<Node> StructInstances;
        private Stream Values;

        public LSFReader(Stream stream)
        {
            this.Stream = stream;
        }

        public void Dispose()
        {
            Stream.Dispose();
        }

        private void ReadNames(Stream s)
        {
            Names = new List<List<String>>();
            using (var reader = new BinaryReader(s))
            {
                var numStructInfos = reader.ReadUInt32();
                while (numStructInfos-- > 0)
                {
                    var info = new List<String>();
                    Names.Add(info);

                    var numFields = reader.ReadUInt16();
                    while (numFields-- > 0)
                    {
                        var nameLen = reader.ReadUInt16();
                        byte[] bytes = reader.ReadBytes(nameLen);
                        var name = System.Text.Encoding.UTF8.GetString(bytes);
                        info.Add(name);
                    }
                }
            }
        }

        private void ReadStructs(Stream s)
        {
            Structs = new List<ResolvedStruct>();
            using (var reader = new BinaryReader(s))
            {
                var refList = new List<Int32>();
                Int32 index = 0;
                while (s.Position < s.Length)
                {
                    var item = BinUtils.ReadStruct<StructElement>(reader);

                    var resolved = new ResolvedStruct();
                    resolved.ParentIndex = item.ParentIndex;
                    resolved.NameIndex = item.NameIndex >> 16;
                    resolved.NameOffset = item.NameIndex & 0xffff;
                    resolved.LastFieldIndex = item.LastFieldIndex;
                    resolved.Reference = -1;

                    var lastFieldIndex = item.LastFieldIndex + 1;
                    if (refList.Count > lastFieldIndex)
                    {
                        if (refList[lastFieldIndex] != -1)
                        {
                            Structs[refList[lastFieldIndex]].Reference = index;
                        }

                        refList[lastFieldIndex] = index;
                    }
                    else
                    {
                        while (refList.Count < lastFieldIndex)
                        {
                            refList.Add(-1);
                        }

                        refList.Add(index);
                    }

                    Structs.Add(resolved);
                    index++;
                }
            }
        }

        private void ReadFields(Stream s)
        {
            Fields = new List<ResolvedField>();
            using (var reader = new BinaryReader(s))
            {
                var refList = new List<Int32>();
                UInt32 dataOffset = 0;
                Int32 index = 0;
                while (s.Position < s.Length)
                {
                    var item = BinUtils.ReadStruct<FieldElement>(reader);

                    var resolved = new ResolvedField();
                    resolved.NameIndex = item.NameIndex >> 16;
                    resolved.NameOffset = item.NameIndex & 0xffff;
                    resolved.Type = item.TypeAndOffset & 0x3f;
                    resolved.DataOffset = dataOffset;
                    resolved.Reference = -1;

                    var lastFieldIndex = item.LastFieldIndex + 1;
                    if (refList.Count > lastFieldIndex)
                    {
                        if (refList[lastFieldIndex] != -1)
                        {
                            Fields[refList[lastFieldIndex]].Reference = index;
                        }

                        refList[lastFieldIndex] = index;
                    }
                    else
                    {
                        while (refList.Count < lastFieldIndex)
                        {
                            refList.Add(-1);
                        }

                        refList.Add(index);
                    }

                    dataOffset += item.TypeAndOffset >> 6;
                    Fields.Add(resolved);
                    index++;
                }
            }
        }

        public Resource Read()
        {
            using (this.Reader = new BinaryReader(Stream))
            {
                var hdr = BinUtils.ReadStruct<LSFHeader>(this.Reader);

                if (hdr.NamesSizeOnDisk > 0)
                {
                    byte[] compressed = this.Reader.ReadBytes((int)hdr.NamesSizeOnDisk);
                    var uncompressed = BinUtils.Decompress(compressed, (int)hdr.NamesUncompressedSize, hdr.CompressionFlags);
                    using (var namesStream = new MemoryStream(uncompressed))
                    {
                        ReadNames(namesStream);
                    }
                }

                if (hdr.StructsSizeOnDisk > 0)
                {
                    byte[] compressed = this.Reader.ReadBytes((int)hdr.StructsSizeOnDisk);
                    var uncompressed = BinUtils.Decompress(compressed, (int)hdr.StructsUncompressedSize, hdr.CompressionFlags);
                    using (var structsStream = new MemoryStream(uncompressed))
                    {
                        ReadStructs(structsStream);
                    }
                }

                if (hdr.FieldsSizeOnDisk > 0)
                {
                    byte[] compressed = this.Reader.ReadBytes((int)hdr.FieldsSizeOnDisk);
                    var uncompressed = BinUtils.Decompress(compressed, (int)hdr.FieldsUncompressedSize, hdr.CompressionFlags);
                    using (var fieldsStream = new MemoryStream(uncompressed))
                    {
                        ReadFields(fieldsStream);
                    }
                }

                if (hdr.ValuesSizeOnDisk > 0)
                {
                    byte[] compressed = this.Reader.ReadBytes((int)hdr.ValuesSizeOnDisk);
                    var uncompressed = BinUtils.Decompress(compressed, (int)hdr.ValuesUncompressedSize, hdr.CompressionFlags);
                    var valueStream = new MemoryStream(uncompressed);
                    this.Values = valueStream;
                }

                Resource rsrc = new Resource();
                var region = new Region();
                ReadStructs(region);
                region.RegionName = region.Name;
                rsrc.Regions[region.Name] = region;
                return rsrc;
            }
        }

        private void ReadStructs(Region region)
        {
            var attrReader = new BinaryReader(Values);
            StructInstances = new List<Node>();
            for (int i = 0; i < Structs.Count; i++)
            {
                var defn = Structs[i];
                if (defn.ParentIndex == -1)
                {
                    ReadNode(defn, region, attrReader);
                    StructInstances.Add(region);
                }
                else
                {
                    var node = new Node();
                    ReadNode(defn, node, attrReader);
                    StructInstances.Add(node);
                    StructInstances[defn.ParentIndex].AppendChild(node);
                }
            }
        }

        private void ReadNode(ResolvedStruct defn, Node node, BinaryReader attributeReader)
        {
            node.Name = Names[defn.NameIndex][defn.NameOffset];

            if (defn.LastFieldIndex != -1)
            {
                var attribute = Fields[defn.LastFieldIndex];
                while (true)
                {
                    Values.Position = attribute.DataOffset;
                    var value = ReadAttribute((NodeAttribute.DataType)attribute.Type, attributeReader);
                    node.Attributes[Names[attribute.NameIndex][attribute.NameOffset]] = value;

                    if (attribute.Reference == -1)
                    {
                        break;
                    }
                    else
                    {
                        attribute = Fields[attribute.Reference];
                    }
                }
            }
        }

        private NodeAttribute ReadAttribute(NodeAttribute.DataType type, BinaryReader reader)
        {
            var attr = new NodeAttribute(type);
            switch (type)
            {
                case NodeAttribute.DataType.DT_None:
                    break;

                case NodeAttribute.DataType.DT_Byte:
                    attr.Value = reader.ReadByte();
                    break;

                case NodeAttribute.DataType.DT_Short:
                    attr.Value = reader.ReadInt16();
                    break;

                case NodeAttribute.DataType.DT_UShort:
                    attr.Value = reader.ReadUInt16();
                    break;

                case NodeAttribute.DataType.DT_Int:
                    attr.Value = reader.ReadInt32();
                    break;

                case NodeAttribute.DataType.DT_UInt:
                    attr.Value = reader.ReadUInt32();
                    break;

                case NodeAttribute.DataType.DT_Float:
                    attr.Value = reader.ReadSingle();
                    break;

                case NodeAttribute.DataType.DT_Double:
                    attr.Value = reader.ReadDouble();
                    break;

                case NodeAttribute.DataType.DT_IVec2:
                case NodeAttribute.DataType.DT_IVec3:
                case NodeAttribute.DataType.DT_IVec4:
                    {
                        int columns = attr.GetColumns();
                        var vec = new int[columns];
                        for (int i = 0; i < columns; i++)
                            vec[i] = reader.ReadInt32();
                        attr.Value = vec;
                        break;
                    }

                case NodeAttribute.DataType.DT_Vec2:
                case NodeAttribute.DataType.DT_Vec3:
                case NodeAttribute.DataType.DT_Vec4:
                    {
                        int columns = attr.GetColumns();
                        var vec = new float[columns];
                        for (int i = 0; i < columns; i++)
                            vec[i] = reader.ReadSingle();
                        attr.Value = vec;
                        break;
                    }

                case NodeAttribute.DataType.DT_Mat2:
                case NodeAttribute.DataType.DT_Mat3:
                case NodeAttribute.DataType.DT_Mat3x4:
                case NodeAttribute.DataType.DT_Mat4x3:
                case NodeAttribute.DataType.DT_Mat4:
                    {
                        int columns = attr.GetColumns();
                        int rows = attr.GetRows();
                        var mat = new Matrix(rows, columns);
                        attr.Value = mat;

                        for (int col = 0; col < columns; col++)
                        {
                            for (int row = 0; row < rows; row++)
                            {
                                mat[row, col] = reader.ReadSingle();
                            }
                        }
                        break;
                    }

                case NodeAttribute.DataType.DT_Bool:
                    attr.Value = reader.ReadByte() != 0;
                    break;

                case NodeAttribute.DataType.DT_String:
                case NodeAttribute.DataType.DT_Path:
                case NodeAttribute.DataType.DT_FixedString:
                case NodeAttribute.DataType.DT_LSString:
                case NodeAttribute.DataType.DT_WString:
                case NodeAttribute.DataType.DT_LSWString:
                    attr.Value = ReadString(reader);
                    break;

                case NodeAttribute.DataType.DT_TranslatedString:
                    var str = new TranslatedString();
                    str.Value = ReadString(reader);
                    str.Handle = ReadString(reader);
                    attr.Value = str;
                    break;

                case NodeAttribute.DataType.DT_ULongLong:
                    attr.Value = reader.ReadUInt64();
                    break;

                case NodeAttribute.DataType.DT_ScratchBuffer:
                    var bufferLength = reader.ReadInt32();
                    attr.Value = reader.ReadBytes(bufferLength);
                    break;

                case NodeAttribute.DataType.DT_Long:
                    attr.Value = reader.ReadInt64();
                    break;

                case NodeAttribute.DataType.DT_Int8:
                    attr.Value = reader.ReadSByte();
                    break;

                default:
                    throw new InvalidFormatException(String.Format("ReadAttribute() not implemented for type {0}", type));
            }

            return attr;
        }

        private string ReadString(BinaryReader reader)
        {
            List<byte> bytes = new List<byte>();
            while (true)
            {
                var b = reader.ReadByte();
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
    }
}
