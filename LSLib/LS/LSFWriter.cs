using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LSLib.LS.LSF
{
    public class LSFWriter : IDisposable
    {
        private static int StringHashMapSize = 0x200;

        private Stream Stream;
        private BinaryWriter Writer;

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

        public LSFWriter(Stream stream)
        {
            this.Stream = stream;
        }

        public void Dispose()
        {
            Stream.Dispose();
        }

        public void Write(Resource resource)
        {
            using (this.Writer = new BinaryWriter(Stream))
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

                var compressionMethod = CompressionMethod.LZ4;
                var compressionLevel = CompressionLevel.MaxCompression;

                var nodeBuffer = NodeStream.ToArray();
                var attributeBuffer = AttributeStream.ToArray();
                var valueBuffer = ValueStream.ToArray();

                byte[] stringsCompressed = BinUtils.Compress(stringBuffer, compressionMethod, compressionLevel);
                byte[] nodesCompressed = BinUtils.Compress(nodeBuffer, compressionMethod, compressionLevel);
                byte[] attributesCompressed = BinUtils.Compress(attributeBuffer, compressionMethod, compressionLevel);
                byte[] valuesCompressed = BinUtils.Compress(valueBuffer, compressionMethod, compressionLevel);

                var header = new Header();
                header.Magic = BitConverter.ToUInt32(Header.Signature, 0);
                header.Version = Header.CurrentVersion;
                header.Unknown = 0x20000000;
                header.StringsUncompressedSize = (UInt32)stringBuffer.Length;
                header.StringsSizeOnDisk = (UInt32)stringsCompressed.Length;
                header.NodesUncompressedSize = (UInt32)nodeBuffer.Length;
                header.NodesSizeOnDisk = (UInt32)nodesCompressed.Length;
                header.AttributesUncompressedSize = (UInt32)attributeBuffer.Length;
                header.AttributesSizeOnDisk = (UInt32)attributesCompressed.Length;
                header.ValuesUncompressedSize = (UInt32)valueBuffer.Length;
                header.ValuesSizeOnDisk = (UInt32)valuesCompressed.Length;
                header.CompressionFlags = BinUtils.MakeCompressionFlags(compressionMethod, compressionLevel);
                header.Unknown2 = 0;
                header.Unknown3 = 0;
                header.Unknown4 = 0;
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
                WriteNode(region.Value);
            }
        }

        private void WriteNode(Node node)
        {
            var nodeInfo = new NodeEntry();
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
                UInt32 lastOffset = (UInt32)ValueStream.Position;
                foreach (KeyValuePair<string, NodeAttribute> entry in node.Attributes)
                {
                    WriteAttribute(ValueWriter, entry.Value);

                    var attributeInfo = new AttributeEntry();
                    var length = (UInt32)ValueStream.Position - lastOffset;
                    attributeInfo.TypeAndLength = (UInt32)entry.Value.Type | (length << 6);
                    attributeInfo.NameHashTableIndex = AddStaticString(entry.Key);
                    attributeInfo.NodeIndex = NextNodeIndex;
                    BinUtils.WriteStruct<AttributeEntry>(AttributeWriter, ref attributeInfo);
                    NextAttributeIndex++;

                    lastOffset = (UInt32)ValueStream.Position;
                }
            }
            else
            {
                nodeInfo.FirstAttributeIndex = -1;
            }

            BinUtils.WriteStruct<NodeEntry>(NodeWriter, ref nodeInfo);
            NodeIndices[node] = NextNodeIndex;
            NextNodeIndex++;

            foreach (var children in node.Children)
            {
                foreach (var child in children.Value)
                {
                    WriteNode(child);
                }
            }
        }

        private void WriteAttribute(BinaryWriter writer, NodeAttribute attr)
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
                    var str = (TranslatedString)attr.Value;
                    WriteString(writer, str.Value);
                    WriteString(writer, str.Handle);
                    break;

                case NodeAttribute.DataType.DT_ScratchBuffer:
                    var buffer = (byte[])attr.Value;
                    writer.Write(buffer);
                    break;

                default:
                    BinUtils.WriteAttribute(writer, attr);
                    break;
            }
        }

        private uint AddStaticString(string s)
        {
            var bucket = (int)((uint)s.GetHashCode() % StringHashMapSize);
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

        private void WriteString(BinaryWriter writer, string s)
        {
            byte[] utf = System.Text.Encoding.UTF8.GetBytes(s);
            writer.Write(utf);
            writer.Write((Byte)0);
        }
    }
}
