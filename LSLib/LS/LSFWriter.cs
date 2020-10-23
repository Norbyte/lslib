using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LSLib.LS.Enums;

namespace LSLib.LS
{
    public class LSFWriter
    {
        private static int StringHashMapSize = 0x200;

        private Stream Stream;
        private BinaryWriter Writer;
        private UInt32 Version;
        private CompressionMethod Compression;
        private CompressionLevel CompressionLevel;

        private MemoryStream NodeStream;
        private BinaryWriter NodeWriter;
        private int NextNodeIndex = 0;
        private Dictionary<Node, int> NodeIndices;

        private MemoryStream AttributeStream;
        private BinaryWriter AttributeWriter;
        private int NextAttributeIndex = 0;

        private MemoryStream ValueStream;
        private BinaryWriter ValueWriter;

        private List<List<string>> StringHashMap;
        private bool ExtendedNodes = false;

        public LSFWriter(Stream stream, FileVersion version)
        {
            this.Stream = stream;
            this.Version = (uint) version;
        }

        public void Write(Resource resource)
        {
            if (Version <= (uint)FileVersion.VerExtendedNodes)
            {
                Compression = CompressionMethod.LZ4;
                CompressionLevel = CompressionLevel.MaxCompression;
            }
            else
            {
                // BG3 doesn't seem to compress LSFs anymore
                Compression = CompressionMethod.None;
                CompressionLevel = CompressionLevel.DefaultCompression;
            }

            using (this.Writer = new BinaryWriter(Stream, Encoding.Default, true))
            using (this.NodeStream = new MemoryStream())
            using (this.NodeWriter = new BinaryWriter(NodeStream))
            using (this.AttributeStream = new MemoryStream())
            using (this.AttributeWriter = new BinaryWriter(AttributeStream))
            using (this.ValueStream = new MemoryStream())
            using (this.ValueWriter = new BinaryWriter(ValueStream))
            {
                NextNodeIndex = 0;
                NextAttributeIndex = 0;
                NodeIndices = new Dictionary<Node, int>();
                StringHashMap = new List<List<string>>(StringHashMapSize);
                while (StringHashMap.Count < StringHashMapSize)
                {
                    StringHashMap.Add(new List<string>());
                }

                WriteRegions(resource);

                byte[] stringBuffer = null;
                using (var stringStream = new MemoryStream())
                using (var stringWriter = new BinaryWriter(stringStream))
                {
                    WriteStaticStrings(stringWriter);
                    stringBuffer = stringStream.ToArray();
                }

                var nodeBuffer = NodeStream.ToArray();
                var attributeBuffer = AttributeStream.ToArray();
                var valueBuffer = ValueStream.ToArray();

                var header = new Header();
                header.Magic = BitConverter.ToUInt32(Header.Signature, 0);
                header.Version = Version;
                header.EngineVersion = (resource.Metadata.MajorVersion << 28) |
                    (resource.Metadata.MinorVersion << 24) |
                    (resource.Metadata.Revision << 16) |
                    resource.Metadata.BuildNumber;

                bool chunked = header.Version >= (ulong) FileVersion.VerChunkedCompress;
                byte[] stringsCompressed = BinUtils.Compress(stringBuffer, Compression, CompressionLevel);
                byte[] nodesCompressed = BinUtils.Compress(nodeBuffer, Compression, CompressionLevel, chunked);
                byte[] attributesCompressed = BinUtils.Compress(attributeBuffer, Compression, CompressionLevel, chunked);
                byte[] valuesCompressed = BinUtils.Compress(valueBuffer, Compression, CompressionLevel, chunked);

                header.StringsUncompressedSize = (UInt32)stringBuffer.Length;
                header.NodesUncompressedSize = (UInt32)nodeBuffer.Length;
                header.AttributesUncompressedSize = (UInt32)attributeBuffer.Length;
                header.ValuesUncompressedSize = (UInt32)valueBuffer.Length;

                if (Compression == CompressionMethod.None)
                {
                    header.StringsSizeOnDisk = 0;
                    header.NodesSizeOnDisk = 0;
                    header.AttributesSizeOnDisk = 0;
                    header.ValuesSizeOnDisk = 0;
                }
                else
                {
                    header.StringsSizeOnDisk = (UInt32)stringsCompressed.Length;
                    header.NodesSizeOnDisk = (UInt32)nodesCompressed.Length;
                    header.AttributesSizeOnDisk = (UInt32)attributesCompressed.Length;
                    header.ValuesSizeOnDisk = (UInt32)valuesCompressed.Length;
                }

                header.CompressionFlags = BinUtils.MakeCompressionFlags(Compression, CompressionLevel);
                header.Unknown2 = 0;
                header.Unknown3 = 0;
                header.Extended = ExtendedNodes ? 1u : 0u;
                BinUtils.WriteStruct<Header>(Writer, ref header);

                Writer.Write(stringsCompressed, 0, stringsCompressed.Length);
                Writer.Write(nodesCompressed, 0, nodesCompressed.Length);
                Writer.Write(attributesCompressed, 0, attributesCompressed.Length);
                Writer.Write(valuesCompressed, 0, valuesCompressed.Length);
            }
        }

        private void WriteRegions(Resource resource)
        {
            foreach (var region in resource.Regions)
            {
                if (Version >= (ulong) FileVersion.VerExtendedNodes
                    && ExtendedNodes)
                {
                    WriteNodeV3(region.Value);
                }
                else
                {
                    WriteNodeV2(region.Value);
                }
            }
        }

        private void WriteNodeAttributesV2(Node node)
        {
            UInt32 lastOffset = (UInt32)ValueStream.Position;
            foreach (KeyValuePair<string, NodeAttribute> entry in node.Attributes)
            {
                WriteAttributeValue(ValueWriter, entry.Value);

                var attributeInfo = new AttributeEntryV2();
                var length = (UInt32)ValueStream.Position - lastOffset;
                attributeInfo.TypeAndLength = (UInt32)entry.Value.Type | (length << 6);
                attributeInfo.NameHashTableIndex = AddStaticString(entry.Key);
                attributeInfo.NodeIndex = NextNodeIndex;
                BinUtils.WriteStruct<AttributeEntryV2>(AttributeWriter, ref attributeInfo);
                NextAttributeIndex++;

                lastOffset = (UInt32)ValueStream.Position;
            }
        }

        private void WriteNodeAttributesV3(Node node)
        {
            UInt32 lastOffset = (UInt32)ValueStream.Position;
            int numWritten = 0;
            foreach (KeyValuePair<string, NodeAttribute> entry in node.Attributes)
            {
                WriteAttributeValue(ValueWriter, entry.Value);
                numWritten++;

                var attributeInfo = new AttributeEntryV3();
                var length = (UInt32)ValueStream.Position - lastOffset;
                attributeInfo.TypeAndLength = (UInt32)entry.Value.Type | (length << 6);
                attributeInfo.NameHashTableIndex = AddStaticString(entry.Key);
                if (numWritten == node.Attributes.Count)
                {
                    attributeInfo.NextAttributeIndex = -1;
                }
                else
                {
                    attributeInfo.NextAttributeIndex = NextAttributeIndex + 1;
                }
                attributeInfo.Offset = (UInt32)ValueStream.Position;
                BinUtils.WriteStruct<AttributeEntryV3>(AttributeWriter, ref attributeInfo);

                NextAttributeIndex++;

                lastOffset = (UInt32)ValueStream.Position;
            }
        }

        private void WriteNodeChildren(Node node)
        {
            foreach (var children in node.Children)
            {
                foreach (var child in children.Value)
                {
                    if (Version >= (ulong) FileVersion.VerExtendedNodes && ExtendedNodes)
                    {
                        WriteNodeV3(child);
                    }
                    else
                    {
                        WriteNodeV2(child);
                    }
                }
            }
        }

        private void WriteNodeV2(Node node)
        {
            var nodeInfo = new NodeEntryV2();
            if (node.Parent == null)
            {
                nodeInfo.ParentIndex = -1;
            }
            else
            {
                nodeInfo.ParentIndex = NodeIndices[node.Parent];
            }

            nodeInfo.NameHashTableIndex = AddStaticString(node.Name);

            if (node.Attributes.Count > 0)
            {
                nodeInfo.FirstAttributeIndex = NextAttributeIndex;
                WriteNodeAttributesV2(node);
            }
            else
            {
                nodeInfo.FirstAttributeIndex = -1;
            }

            BinUtils.WriteStruct<NodeEntryV2>(NodeWriter, ref nodeInfo);
            NodeIndices[node] = NextNodeIndex;
            NextNodeIndex++;

            WriteNodeChildren(node);
        }

        private void WriteNodeV3(Node node)
        {
            var nodeInfo = new NodeEntryV3();
            if (node.Parent == null)
            {
                nodeInfo.ParentIndex = -1;
            }
            else
            {
                nodeInfo.ParentIndex = NodeIndices[node.Parent];
            }

            nodeInfo.NameHashTableIndex = AddStaticString(node.Name);

            if (node.Attributes.Count > 0)
            {
                nodeInfo.FirstAttributeIndex = NextAttributeIndex;
                WriteNodeAttributesV3(node);
            }
            else
            {
                nodeInfo.FirstAttributeIndex = -1;
            }

            // FIXME!
            throw new Exception("Writing uncompressed LSFv3 is not supported yet");
            nodeInfo.NextSiblingIndex = -1;
            BinUtils.WriteStruct<NodeEntryV3>(NodeWriter, ref nodeInfo);
            NodeIndices[node] = NextNodeIndex;
            NextNodeIndex++;

            WriteNodeChildren(node);
        }

        private void WriteTranslatedFSString(BinaryWriter writer, TranslatedFSString fs)
        {
            if (Version >= (uint)FileVersion.VerBG3)
            {
                writer.Write(fs.Version);
            }
            else
            {
                WriteStringWithLength(writer, fs.Value ?? "");
            }

            WriteStringWithLength(writer, fs.Handle);

            writer.Write((UInt32)fs.Arguments.Count);
            foreach (var arg in fs.Arguments)
            {
                WriteStringWithLength(writer, arg.Key);
                WriteTranslatedFSString(writer, arg.String);
                WriteStringWithLength(writer, arg.Value);
            }
        }

        private void WriteAttributeValue(BinaryWriter writer, NodeAttribute attr)
        {
            switch (attr.Type)
            {
                case NodeAttribute.DataType.DT_String:
                case NodeAttribute.DataType.DT_Path:
                case NodeAttribute.DataType.DT_FixedString:
                case NodeAttribute.DataType.DT_LSString:
                case NodeAttribute.DataType.DT_WString:
                case NodeAttribute.DataType.DT_LSWString:
                    WriteString(writer, (string)attr.Value);
                    break;

                case NodeAttribute.DataType.DT_TranslatedString:
                    {
                        var ts = (TranslatedString)attr.Value;
                        if (Version >= (uint)FileVersion.VerBG3)
                        {
                            writer.Write(ts.Version);
                        }
                        else
                        {
                            WriteStringWithLength(writer, ts.Value ?? "");
                        }

                        WriteStringWithLength(writer, ts.Handle);
                        break;
                    }

                case NodeAttribute.DataType.DT_TranslatedFSString:
                    {
                        var fs = (TranslatedFSString)attr.Value;
                        WriteTranslatedFSString(writer, fs);
                        break;
                    }

                case NodeAttribute.DataType.DT_ScratchBuffer:
                    {
                        var buffer = (byte[])attr.Value;
                        writer.Write(buffer);
                        break;
                    }

                default:
                    BinUtils.WriteAttribute(writer, attr);
                    break;
            }
        }

        private uint AddStaticString(string s)
        {
            var hashCode = (uint)s.GetHashCode();
            var bucket = (int)((hashCode & 0x1ff) ^ ((hashCode >> 9) & 0x1ff) ^ ((hashCode >> 18) & 0x1ff) ^ ((hashCode >> 27) & 0x1ff));
            for (int i = 0; i < StringHashMap[bucket].Count; i++)
            {
                if (StringHashMap[bucket][i].Equals(s))
                {
                    return (uint)((bucket << 16) | i);
                }
            }

            StringHashMap[bucket].Add(s);
            return (uint)((bucket << 16) | (StringHashMap[bucket].Count - 1));
        }

        private void WriteStaticStrings(BinaryWriter writer)
        {
            writer.Write((UInt32)StringHashMap.Count);
            for (int i = 0; i < StringHashMap.Count; i++)
            {
                var entry = StringHashMap[i];
                writer.Write((UInt16)entry.Count);
                for (int j = 0; j < entry.Count; j++)
                {
                    WriteStaticString(writer, entry[j]);
                }
            }
        }

        private void WriteStaticString(BinaryWriter writer, string s)
        {
            byte[] utf = System.Text.Encoding.UTF8.GetBytes(s);
            writer.Write((UInt16)utf.Length);
            writer.Write(utf);
        }

        private void WriteStringWithLength(BinaryWriter writer, string s)
        {
            byte[] utf = System.Text.Encoding.UTF8.GetBytes(s);
            writer.Write((Int32)(utf.Length + 1));
            writer.Write(utf);
            writer.Write((Byte)0);
        }

        private void WriteString(BinaryWriter writer, string s)
        {
            byte[] utf = System.Text.Encoding.UTF8.GetBytes(s);
            writer.Write(utf);
            writer.Write((Byte)0);
        }
    }
}
