﻿// #define DEBUG_LSF_SERIALIZATION
// #define DUMP_LSF_SERIALIZATION

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LSLib.LS.Enums;

namespace LSLib.LS
{
    public class LSFReader : IDisposable
    {
        /// <summary>
        /// Input stream
        /// </summary>
        private Stream Stream;

        /// <summary>
        /// Static string hash map
        /// </summary>
        private List<List<String>> Names;
        /// <summary>
        /// Preprocessed list of nodes (structures)
        /// </summary>
        private List<LSFNodeInfo> Nodes;
        /// <summary>
        /// Preprocessed list of node attributes
        /// </summary>
        private List<LSFAttributeInfo> Attributes;
        /// <summary>
        /// Node instances
        /// </summary>
        private List<Node> NodeInstances;
        /// <summary>
        /// Raw value data stream
        /// </summary>
        private Stream Values;
        /// <summary>
        /// Version of the file we're serializing
        /// </summary>
        private LSFVersion Version;
        /// <summary>
        /// Game version that generated the LSF file
        /// </summary>
        private PackedVersion GameVersion;

        public LSFReader(Stream stream)
        {
            this.Stream = stream;
        }

        public void Dispose()
        {
            Stream.Dispose();
        }

        /// <summary>
        /// Reads the static string hash table from the specified stream.
        /// </summary>
        /// <param name="s">Stream to read the hash table from</param>
        private void ReadNames(Stream s)
        {
#if DEBUG_LSF_SERIALIZATION
            Console.WriteLine(" ----- DUMP OF NAME TABLE -----");
#endif

            // Format:
            // 32-bit hash entry count (N)
            //     N x 16-bit chain length (L)
            //         L x 16-bit string length (S)
            //             [S bytes of UTF-8 string data]

            using (var reader = new BinaryReader(s))
            {
                var numHashEntries = reader.ReadUInt32();
                while (numHashEntries-- > 0)
                {
                    var hash = new List<String>();
                    Names.Add(hash);

                    var numStrings = reader.ReadUInt16();
                    while (numStrings-- > 0)
                    {
                        var nameLen = reader.ReadUInt16();
                        byte[] bytes = reader.ReadBytes(nameLen);
                        var name = System.Text.Encoding.UTF8.GetString(bytes);
                        hash.Add(name);
#if DEBUG_LSF_SERIALIZATION
                        Console.WriteLine(String.Format("{0,3:X}/{1}: {2}", Names.Count - 1, hash.Count - 1, name));
#endif
                    }
                }
            }
        }

        /// <summary>
        /// Reads the structure headers for the LSOF resource
        /// </summary>
        /// <param name="s">Stream to read the node headers from</param>
        /// <param name="longNodes">Use the long (V3) on-disk node format</param>
        private void ReadNodes(Stream s, bool longNodes)
        {
#if DEBUG_LSF_SERIALIZATION
            Console.WriteLine(" ----- DUMP OF NODE TABLE -----");
#endif

            using (var reader = new BinaryReader(s))
            {
                Int32 index = 0;
                while (s.Position < s.Length)
                {
                    var resolved = new LSFNodeInfo();
#if DEBUG_LSF_SERIALIZATION
                    var pos = s.Position;
#endif

                    if (longNodes)
                    {
                        var item = BinUtils.ReadStruct<LSFNodeEntryV3>(reader);
                        resolved.ParentIndex = item.ParentIndex;
                        resolved.NameIndex = item.NameIndex;
                        resolved.NameOffset = item.NameOffset;
                        resolved.FirstAttributeIndex = item.FirstAttributeIndex;
                    }
                    else
                    {
                        var item = BinUtils.ReadStruct<LSFNodeEntryV2>(reader);
                        resolved.ParentIndex = item.ParentIndex;
                        resolved.NameIndex = item.NameIndex;
                        resolved.NameOffset = item.NameOffset;
                        resolved.FirstAttributeIndex = item.FirstAttributeIndex;
                    }

#if DEBUG_LSF_SERIALIZATION
                    Console.WriteLine(String.Format(
                        "{0}: {1} @ {2:X} (parent {3}, firstAttribute {4})",
                        index, Names[resolved.NameIndex][resolved.NameOffset], pos, resolved.ParentIndex,
                        resolved.FirstAttributeIndex
                    ));
#endif

                    Nodes.Add(resolved);
                    index++;
                }
            }
        }

        /// <summary>
        /// Reads the V2 attribute headers for the LSOF resource
        /// </summary>
        /// <param name="s">Stream to read the attribute headers from</param>
        private void ReadAttributesV2(Stream s)
        {
            using (var reader = new BinaryReader(s))
            {
#if DEBUG_LSF_SERIALIZATION
                var rawAttributes = new List<AttributeEntryV2>();
#endif

                var prevAttributeRefs = new List<Int32>();
                UInt32 dataOffset = 0;
                Int32 index = 0;
                while (s.Position < s.Length)
                {
                    var attribute = BinUtils.ReadStruct<LSFAttributeEntryV2>(reader);

                    var resolved = new LSFAttributeInfo();
                    resolved.NameIndex = attribute.NameIndex;
                    resolved.NameOffset = attribute.NameOffset;
                    resolved.TypeId = attribute.TypeId;
                    resolved.Length = attribute.Length;
                    resolved.DataOffset = dataOffset;
                    resolved.NextAttributeIndex = -1;

                    var nodeIndex = attribute.NodeIndex + 1;
                    if (prevAttributeRefs.Count > nodeIndex)
                    {
                        if (prevAttributeRefs[nodeIndex] != -1)
                        {
                            Attributes[prevAttributeRefs[nodeIndex]].NextAttributeIndex = index;
                        }

                        prevAttributeRefs[nodeIndex] = index;
                    }
                    else
                    {
                        while (prevAttributeRefs.Count < nodeIndex)
                        {
                            prevAttributeRefs.Add(-1);
                        }

                        prevAttributeRefs.Add(index);
                    }

#if DEBUG_LSF_SERIALIZATION
                    rawAttributes.Add(attribute);
#endif

                    dataOffset += resolved.Length;
                    Attributes.Add(resolved);
                    index++;
                }

#if DEBUG_LSF_SERIALIZATION
                Console.WriteLine(" ----- DUMP OF ATTRIBUTE REFERENCES -----");
                for (int i = 0; i < prevAttributeRefs.Count; i++)
                {
                    Console.WriteLine(String.Format("Node {0}: last attribute {1}", i, prevAttributeRefs[i]));
                }


                Console.WriteLine(" ----- DUMP OF V2 ATTRIBUTE TABLE -----");
                for (int i = 0; i < Attributes.Count; i++)
                {
                    var resolved = Attributes[i];
                    var attribute = rawAttributes[i];

                    var debug = String.Format(
                        "{0}: {1} (offset {2:X}, typeId {3}, nextAttribute {4}, node {5})",
                        i, Names[resolved.NameIndex][resolved.NameOffset], resolved.DataOffset,
                        resolved.TypeId, resolved.NextAttributeIndex, attribute.NodeIndex
                    );
                    Console.WriteLine(debug);
                }
#endif
            }
        }

        /// <summary>
        /// Reads the V3 attribute headers for the LSOF resource
        /// </summary>
        /// <param name="s">Stream to read the attribute headers from</param>
        private void ReadAttributesV3(Stream s)
        {
            using (var reader = new BinaryReader(s))
            {
                while (s.Position < s.Length)
                {
                    var attribute = BinUtils.ReadStruct<LSFAttributeEntryV3>(reader);

                    var resolved = new LSFAttributeInfo();
                    resolved.NameIndex = attribute.NameIndex;
                    resolved.NameOffset = attribute.NameOffset;
                    resolved.TypeId = attribute.TypeId;
                    resolved.Length = attribute.Length;
                    resolved.DataOffset = attribute.Offset;
                    resolved.NextAttributeIndex = attribute.NextAttributeIndex;

                    Attributes.Add(resolved);
                }

#if DEBUG_LSF_SERIALIZATION
                Console.WriteLine(" ----- DUMP OF V3 ATTRIBUTE TABLE -----");
                for (int i = 0; i < Attributes.Count; i++)
                {
                    var resolved = Attributes[i];

                    var debug = String.Format(
                        "{0}: {1} (offset {2:X}, typeId {3}, nextAttribute {4})",
                        i, Names[resolved.NameIndex][resolved.NameOffset], resolved.DataOffset,
                        resolved.TypeId, resolved.NextAttributeIndex
                    );
                    Console.WriteLine(debug);
                }
#endif
            }
        }

        private byte[] Decompress(BinaryReader reader, uint compressedSize, uint uncompressedSize, LSFMetadata metadata)
        {
            bool chunked = Version >= LSFVersion.VerChunkedCompress;
            byte[] compressed = reader.ReadBytes((int)compressedSize);
            return BinUtils.Decompress(compressed, (int)uncompressedSize, metadata.CompressionFlags, chunked);
        }

        public Resource Read()
        {
            using (var reader = new BinaryReader(Stream))
            {
                var magic = BinUtils.ReadStruct<LSFMagic>(reader);
                if (magic.Magic != BitConverter.ToUInt32(LSFMagic.Signature, 0))
                {
                    var msg = String.Format(
                        "Invalid LSF signature; expected {0,8:X}, got {1,8:X}",
                        BitConverter.ToUInt32(LSFMagic.Signature, 0), magic.Magic
                    );
                    throw new InvalidDataException(msg);
                }

                if (magic.Version < (ulong) LSFVersion.VerInitial || magic.Version > (ulong) LSFVersion.MaxVersion)
                {
                    var msg = String.Format("LSF version {0} is not supported", magic.Version);
                    throw new InvalidDataException(msg);
                }

                this.Version = (LSFVersion)magic.Version;

                if (this.Version >= LSFVersion.VerBG3ExtendedHeader)
                {
                    var hdr = BinUtils.ReadStruct<LSFHeaderV5>(reader);
                    GameVersion = PackedVersion.FromInt64(hdr.EngineVersion);
                }
                else
                {
                    var hdr = BinUtils.ReadStruct<LSFHeader>(reader);
                    GameVersion = PackedVersion.FromInt32(hdr.EngineVersion);
                }

                var meta = BinUtils.ReadStruct<LSFMetadata>(reader);

                Names = new List<List<String>>();
                bool isCompressed = BinUtils.CompressionFlagsToMethod(meta.CompressionFlags) != CompressionMethod.None;
                if (meta.StringsSizeOnDisk > 0 || meta.StringsUncompressedSize > 0)
                {
                    uint onDiskSize = isCompressed ? meta.StringsSizeOnDisk : meta.StringsUncompressedSize;
                    byte[] compressed = reader.ReadBytes((int)onDiskSize);
                    byte[] uncompressed;
                    if (isCompressed)
                    {
                        uncompressed = BinUtils.Decompress(compressed, (int)meta.StringsUncompressedSize, meta.CompressionFlags);
                    }
                    else
                    {
                        uncompressed = compressed;
                    }

#if DUMP_LSF_SERIALIZATION
                    using (var nodesFile = new FileStream("names.bin", FileMode.Create, FileAccess.Write))
                    {
                        nodesFile.Write(uncompressed, 0, uncompressed.Length);
                    }
#endif

                    using (var namesStream = new MemoryStream(uncompressed))
                    {
                        ReadNames(namesStream);
                    }
                }
                
                Nodes = new List<LSFNodeInfo>();
                if (meta.NodesSizeOnDisk > 0 || meta.NodesUncompressedSize > 0)
                {
                    uint onDiskSize = isCompressed ? meta.NodesSizeOnDisk : meta.NodesUncompressedSize;
                    var uncompressed = Decompress(reader, onDiskSize, meta.NodesUncompressedSize, meta);

#if DUMP_LSF_SERIALIZATION
                    using (var nodesFile = new FileStream("nodes.bin", FileMode.Create, FileAccess.Write))
                    {
                        nodesFile.Write(uncompressed, 0, uncompressed.Length);
                    }
#endif

                    using (var nodesStream = new MemoryStream(uncompressed))
                    {
                        var longNodes = Version >= LSFVersion.VerExtendedNodes
                            && meta.HasSiblingData == 1;
                        ReadNodes(nodesStream, longNodes);
                    }
                }

                Attributes = new List<LSFAttributeInfo>();
                if (meta.AttributesSizeOnDisk > 0 || meta.AttributesUncompressedSize > 0)
                {
                    uint onDiskSize = isCompressed ? meta.AttributesSizeOnDisk : meta.AttributesUncompressedSize;
                    var uncompressed = Decompress(reader, onDiskSize, meta.AttributesUncompressedSize, meta);

#if DUMP_LSF_SERIALIZATION
                    using (var attributesFile = new FileStream("attributes.bin", FileMode.Create, FileAccess.Write))
                    {
                        attributesFile.Write(uncompressed, 0, uncompressed.Length);
                    }
#endif

                    using (var attributesStream = new MemoryStream(uncompressed))
                    {
                        var hasSiblingData = Version >= LSFVersion.VerExtendedNodes
                            && meta.HasSiblingData == 1;
                        if (hasSiblingData)
                        {
                            ReadAttributesV3(attributesStream);
                        }
                        else
                        {
                            ReadAttributesV2(attributesStream);
                        }
                    }
                }

                if (meta.ValuesSizeOnDisk > 0 || meta.ValuesUncompressedSize > 0)
                {
                    uint onDiskSize = isCompressed ? meta.ValuesSizeOnDisk : meta.ValuesUncompressedSize;
                    var uncompressed = Decompress(reader, onDiskSize, meta.ValuesUncompressedSize, meta);
                    var valueStream = new MemoryStream(uncompressed);
                    this.Values = valueStream;

#if DUMP_LSF_SERIALIZATION
                    using (var valuesFile = new FileStream("values.bin", FileMode.Create, FileAccess.Write))
                    {
                        valuesFile.Write(uncompressed, 0, uncompressed.Length);
                    }
#endif
                }
                else
                {
                    this.Values = new MemoryStream();
                }

                Resource resource = new Resource();
                ReadRegions(resource);

                resource.Metadata.MajorVersion = GameVersion.Major;
                resource.Metadata.MinorVersion = GameVersion.Minor;
                resource.Metadata.Revision = GameVersion.Revision;
                resource.Metadata.BuildNumber = GameVersion.Build;

                return resource;
            }
        }

        private void ReadRegions(Resource resource)
        {
            var attrReader = new BinaryReader(Values);
            NodeInstances = new List<Node>();
            for (int i = 0; i < Nodes.Count; i++)
            {
                var defn = Nodes[i];
                if (defn.ParentIndex == -1)
                {
                    var region = new Region();
                    ReadNode(defn, region, attrReader);
                    NodeInstances.Add(region);
                    region.RegionName = region.Name;
                    resource.Regions[region.Name] = region;
                }
                else
                {
                    var node = new Node();
                    ReadNode(defn, node, attrReader);
                    node.Parent = NodeInstances[defn.ParentIndex];
                    NodeInstances.Add(node);
                    NodeInstances[defn.ParentIndex].AppendChild(node);
                }
            }
        }

        private void ReadNode(LSFNodeInfo defn, Node node, BinaryReader attributeReader)
        {
            node.Name = Names[defn.NameIndex][defn.NameOffset];

#if DEBUG_LSF_SERIALIZATION
            Console.WriteLine(String.Format("Begin node {0}", node.Name));
#endif

            if (defn.FirstAttributeIndex != -1)
            {
                var attribute = Attributes[defn.FirstAttributeIndex];
                while (true)
                {
                    Values.Position = attribute.DataOffset;
                    var value = ReadAttribute((NodeAttribute.DataType)attribute.TypeId, attributeReader, attribute.Length);
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

        private NodeAttribute ReadAttribute(NodeAttribute.DataType type, BinaryReader reader, uint length)
        {
            // LSF and LSB serialize the buffer types differently, so specialized
            // code is added to the LSB and LSf serializers, and the common code is
            // available in BinUtils.ReadAttribute()
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
                        attr.Value = ReadString(reader, (int)length);
                        return attr;
                    }

                case NodeAttribute.DataType.DT_TranslatedString:
                    {
                        var attr = new NodeAttribute(type);
                        var str = new TranslatedString();

                        if (Version >= LSFVersion.VerBG3)
                        {
                            str.Version = reader.ReadUInt16();
                        }
                        else
                        {
                            str.Version = 0;
                            var valueLength = reader.ReadInt32();
                            str.Value = ReadString(reader, valueLength);
                        }

                        var handleLength = reader.ReadInt32();
                        str.Handle = ReadString(reader, handleLength);

                        attr.Value = str;
                        return attr;
                    }

                case NodeAttribute.DataType.DT_TranslatedFSString:
                    {
                        var attr = new NodeAttribute(type);
                        attr.Value = ReadTranslatedFSString(reader);
                        return attr;
                    }

                case NodeAttribute.DataType.DT_ScratchBuffer:
                    {
                        var attr = new NodeAttribute(type);
                        attr.Value = reader.ReadBytes((int)length);
                        return attr;
                    }

                default:
                    return BinUtils.ReadAttribute(type, reader);
            }
        }

        private TranslatedFSString ReadTranslatedFSString(BinaryReader reader)
        {
            var str = new TranslatedFSString();

            if (Version >= LSFVersion.VerBG3)
            {
                str.Version = reader.ReadUInt16();
            }
            else
            {
                str.Version = 0;
                var valueLength = reader.ReadInt32();
                str.Value = ReadString(reader, valueLength);
            }

            var handleLength = reader.ReadInt32();
            str.Handle = ReadString(reader, handleLength);

            var arguments = reader.ReadInt32();
            str.Arguments = new List<TranslatedFSStringArgument>(arguments);
            for (int i = 0; i < arguments; i++)
            {
                var arg = new TranslatedFSStringArgument();
                var argKeyLength = reader.ReadInt32();
                arg.Key = ReadString(reader, argKeyLength);

                arg.String = ReadTranslatedFSString(reader);

                var argValueLength = reader.ReadInt32();
                arg.Value = ReadString(reader, argValueLength);

                str.Arguments.Add(arg);
            }

            return str;
        }

        private string ReadString(BinaryReader reader, int length)
        {
            var bytes = reader.ReadBytes(length - 1);

            // Remove null bytes at the end of the string
            int lastNull = bytes.Length;
            while (lastNull > 0 && bytes[lastNull - 1] == 0)
                lastNull--;

            var nullTerminator = reader.ReadByte();
            if (nullTerminator != 0)
            {
                throw new InvalidDataException("String is not null-terminated");
            }

            return Encoding.UTF8.GetString(bytes, 0, lastNull);
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
