// #define DEBUG_LSF_SERIALIZATION

using LZ4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public Int32 LastAttributeIndex;
        public Int32 ParentIndex;
    };

    internal class ResolvedStruct
    {
        public int ParentIndex;
        public int NameIndex;
        public int NameOffset;
        public int LastAttributeIndex;
        public int Reference;
    };

    internal struct AttributeElement
    {
        public Int32 NameIndex;
        public UInt32 TypeAndOffset;
        public Int32 LastAttributeIndex;
    };

    internal class ResolvedAttribute
    {
        public int NameIndex;
        public int NameOffset;
        public uint TypeId;
        public uint DataOffset;
        public int NextAttributeIndex;
    };

    public class LSFReader : IDisposable
    {
        private Stream Stream;
        private BinaryReader Reader;
        private List<List<String>> Names;
        private List<ResolvedStruct> Structs;
        private List<ResolvedAttribute> Attributes;
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
#if DEBUG_LSF_SERIALIZATION
            Console.WriteLine(" ----- DUMP OF NAME TABLE -----");
#endif

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
#if DEBUG_LSF_SERIALIZATION
                        Console.WriteLine(String.Format("{0,3:X}/{1}: {2}", Names.Count - 1, info.Count - 1, name));
#endif
                    }
                }
            }
        }

        private void ReadStructs(Stream s)
        {
#if DEBUG_LSF_SERIALIZATION
            Console.WriteLine(" ----- DUMP OF STRUCT TABLE -----");
#endif

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
                    resolved.LastAttributeIndex = item.LastAttributeIndex;
                    resolved.Reference = -1;

                    var lastFieldIndex = item.LastAttributeIndex + 1;
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

#if DEBUG_LSF_SERIALIZATION
                    Console.WriteLine(String.Format(
                        "{0}: {1} (parent {2}, field {3})", 
                        Structs.Count, Names[resolved.NameIndex][resolved.NameOffset], resolved.ParentIndex, resolved.LastFieldIndex
                    ));
#endif

                    Structs.Add(resolved);
                    index++;
                }
            }
        }

        private void ReadAttributes(Stream s)
        {
#if DEBUG_LSF_SERIALIZATION
            Console.WriteLine(" ----- DUMP OF ATTRIBUTE TABLE -----");
#endif

            Attributes = new List<ResolvedAttribute>();
            using (var reader = new BinaryReader(s))
            {
                var refList = new List<Int32>();
                UInt32 dataOffset = 0;
                Int32 index = 0;
                while (s.Position < s.Length)
                {
                    var attribute = BinUtils.ReadStruct<AttributeElement>(reader);

                    var resolved = new ResolvedAttribute();
                    resolved.NameIndex = attribute.NameIndex >> 16;
                    resolved.NameOffset = attribute.NameIndex & 0xffff;
                    resolved.TypeId = attribute.TypeAndOffset & 0x3f;
                    resolved.DataOffset = dataOffset;
                    resolved.NextAttributeIndex = -1;

                    var lastAttributeIndex = attribute.LastAttributeIndex + 1;
                    if (refList.Count > lastAttributeIndex)
                    {
                        if (refList[lastAttributeIndex] != -1)
                        {
                            Attributes[refList[lastAttributeIndex]].NextAttributeIndex = index;
                        }

                        refList[lastAttributeIndex] = index;
                    }
                    else
                    {
                        while (refList.Count < lastAttributeIndex)
                        {
                            refList.Add(-1);
                        }

                        refList.Add(index);
                    }

#if DEBUG_LSF_SERIALIZATION
                    Console.WriteLine(String.Format("{0}: {1} (offset {2:X}, type {3}, next {4})", Fields.Count, Names[resolved.NameIndex][resolved.NameOffset], dataOffset, resolved.Type, resolved.Reference));
#endif

                    dataOffset += attribute.TypeAndOffset >> 6;
                    Attributes.Add(resolved);
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
                        ReadAttributes(fieldsStream);
                    }
                }

                if (hdr.ValuesSizeOnDisk > 0)
                {
                    byte[] compressed = this.Reader.ReadBytes((int)hdr.ValuesSizeOnDisk);
                    var uncompressed = BinUtils.Decompress(compressed, (int)hdr.ValuesUncompressedSize, hdr.CompressionFlags);
                    var valueStream = new MemoryStream(uncompressed);
                    this.Values = valueStream;

#if DEBUG_LSF_SERIALIZATION
                    using (var valuesFile = new FileStream("values.bin", FileMode.Create, FileAccess.Write))
                    {
                        valuesFile.Write(uncompressed, 0, uncompressed.Length);
                    }
#endif
                }

                Resource rsrc = new Resource();
                ReadStructs(rsrc);
                return rsrc;
            }
        }

        private void ReadStructs(Resource resource)
        {
            var attrReader = new BinaryReader(Values);
            StructInstances = new List<Node>();
            for (int i = 0; i < Structs.Count; i++)
            {
                var defn = Structs[i];
                if (defn.ParentIndex == -1)
                {
                    var region = new Region();
                    ReadNode(defn, region, attrReader);
                    StructInstances.Add(region);
                    region.RegionName = region.Name;
                    resource.Regions[region.Name] = region;
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
            
#if DEBUG_LSF_SERIALIZATION
            Console.WriteLine(String.Format("Begin node {0}", node.Name));
#endif

            if (defn.LastAttributeIndex != -1)
            {
                var attribute = Attributes[defn.LastAttributeIndex];
                while (true)
                {
                    Values.Position = attribute.DataOffset;
                    var value = ReadAttribute((NodeAttribute.DataType)attribute.TypeId, attributeReader);
                    node.Attributes[Names[attribute.NameIndex][attribute.NameOffset]] = value;

#if DEBUG_LSF_SERIALIZATION
                    Console.WriteLine(String.Format("    {0:X}: {1} ({2})", attribute.DataOffset, Names[attribute.NameIndex][attribute.NameOffset], value));
#endif

                    if (attribute.NextAttributeIndex == -1)
                    {
                        break;
                    }
                    else
                    {
                        attribute = Attributes[attribute.NextAttributeIndex];
                    }
                }
            }
        }

        private NodeAttribute ReadAttribute(NodeAttribute.DataType type, BinaryReader reader)
        {
            switch (type)
            {
                case NodeAttribute.DataType.DT_String:
                case NodeAttribute.DataType.DT_Path:
                case NodeAttribute.DataType.DT_FixedString:
                case NodeAttribute.DataType.DT_LSString:
                case NodeAttribute.DataType.DT_WString:
                case NodeAttribute.DataType.DT_LSWString:
                { 
                    var attr = new NodeAttribute(type);
                    attr.Value = ReadString(reader);
                    return attr;
                }

                case NodeAttribute.DataType.DT_TranslatedString:
                {
                    var attr = new NodeAttribute(type);
                    var str = new TranslatedString();
                    str.Value = ReadString(reader);
                    str.Handle = ReadString(reader);
                    attr.Value = str;
                    return attr;
                }

                case NodeAttribute.DataType.DT_ScratchBuffer:
                { 
                    var attr = new NodeAttribute(type);
                    // TODO: Not sure how to determine length
                    attr.Value = new byte[0];
                    return attr;
                }

                default:
                    return BinUtils.ReadAttribute(type, reader);
            }
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
