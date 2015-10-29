using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LSLib.LS
{
    public class LSBReader : IDisposable
    {
        private Stream stream;
        private BinaryReader reader;
        private Dictionary<UInt32, string> staticStrings = new Dictionary<UInt32, string>();

        public LSBReader(Stream stream)
        {
            this.stream = stream;
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        public Resource Read()
        {
            using (this.reader = new BinaryReader(stream))
            {
                LSBHeader header;
                header.signature = reader.ReadUInt32();
                if (header.signature != LSBHeader.Signature)
                    throw new InvalidFormatException(String.Format("Illegal signature in header; expected {0}, got {1}", LSBHeader.Signature, header.signature));

                header.totalSize = reader.ReadUInt32();
                if (stream.Length != header.totalSize)
                    throw new InvalidFormatException(String.Format("Invalid LSB file size; expected {0}, got {1}", header.totalSize, stream.Length));

                header.bigEndian = reader.ReadUInt32();
                // The game only uses little-endian files on all platforms currently and big-endian support isn't worth the hassle
                if (header.bigEndian != 0)
                    throw new InvalidFormatException("Big-endian LSB files are not supported");

                header.unknown = reader.ReadUInt32();
                header.metadata.timestamp = reader.ReadUInt64();
                header.metadata.majorVersion = reader.ReadUInt32();
                header.metadata.minorVersion = reader.ReadUInt32();
                header.metadata.revision = reader.ReadUInt32();
                header.metadata.buildNumber = reader.ReadUInt32();

                ReadStaticStrings();

                Resource rsrc = new Resource();
                rsrc.Metadata = header.metadata;
                ReadRegions(rsrc);
                return rsrc;
            }
        }

        private void ReadRegions(Resource rsrc)
        {
            UInt32 regions = reader.ReadUInt32();
            for (UInt32 i = 0; i < regions; i++)
            {
                UInt32 regionNameId = reader.ReadUInt32();
                UInt32 regionOffset = reader.ReadUInt32();

                Region rgn = new Region();
                rgn.RegionName = staticStrings[regionNameId];
                var lastRegionPos = stream.Position;

                stream.Seek(regionOffset, SeekOrigin.Begin);
                ReadNode(rgn);
                rsrc.Regions[rgn.RegionName] = rgn;
                stream.Seek(lastRegionPos, SeekOrigin.Begin);
            }
        }

        private void ReadNode(Node node)
        {
            UInt32 nodeNameId = reader.ReadUInt32();
            UInt32 attributeCount = reader.ReadUInt32();
            UInt32 childCount = reader.ReadUInt32();
            node.Name = staticStrings[nodeNameId];

            for (UInt32 i = 0; i < attributeCount; i++)
            {
                UInt32 attrNameId = reader.ReadUInt32();
                UInt32 attrTypeId = reader.ReadUInt32();
                if (attrTypeId > (int)NodeAttribute.DataType.DT_Max)
                    throw new InvalidFormatException(String.Format("Unsupported attribute data type: {0}", attrTypeId));

                node.Attributes[staticStrings[attrNameId]] = ReadAttribute((NodeAttribute.DataType)attrTypeId);
            }

            for (UInt32 i = 0; i < childCount; i++)
            {
                Node child = new Node();
                child.Parent = node;
                ReadNode(child);
                node.AppendChild(child);
            }
        }

        private NodeAttribute ReadAttribute(NodeAttribute.DataType type)
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
                    attr.Value = reader.ReadInt32(); ;
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
                    attr.Value = ReadString(true);
                    break;

                case NodeAttribute.DataType.DT_WString:
                case NodeAttribute.DataType.DT_LSWString:
                    attr.Value = ReadWideString(true);
                    break;

                case NodeAttribute.DataType.DT_TranslatedString:
                    var str = new TranslatedString();
                    str.Value = ReadString(true);
                    str.Handle = ReadString(true);
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

        private void ReadStaticStrings()
        {
            UInt32 strings = reader.ReadUInt32();
            for (UInt32 i = 0; i < strings; i++)
            {
                string s = ReadString(false);
                UInt32 index = reader.ReadUInt32();
                if (staticStrings.ContainsKey(index))
                    throw new InvalidFormatException(String.Format("String ID {0} duplicated in static string map", index));
                staticStrings.Add(index, s);
            }
        }

        private string ReadString(bool nullTerminated)
        {
            int length = reader.ReadInt32() - (nullTerminated ? 1 : 0);
            byte[] bytes = reader.ReadBytes(length);
            string str = System.Text.Encoding.UTF8.GetString(bytes);
            if (nullTerminated)
            {
                if (reader.ReadByte() != 0)
                    throw new InvalidFormatException("Illegal null terminated string");
            }

            return str;
        }

        private string ReadWideString(bool nullTerminated)
        {
            int length = reader.ReadInt32() - (nullTerminated ? 1 : 0);
            byte[] bytes = reader.ReadBytes(length * 2);
            string str = System.Text.Encoding.Unicode.GetString(bytes);
            if (nullTerminated)
            {
                if (reader.ReadUInt16() != 0)
                    throw new InvalidFormatException("Illegal null terminated widestring");
            }

            return str;
        }
    }
}
