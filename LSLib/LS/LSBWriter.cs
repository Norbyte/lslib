using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LSLib.LS
{
    public class LSBWriter : IDisposable
    {
        private Stream stream;
        private BinaryWriter writer;
        private Dictionary<string, UInt32> staticStrings = new Dictionary<string, UInt32>();
        private UInt32 nextStaticStringId = 0;

        public LSBWriter(Stream stream)
        {
            this.stream = stream;
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        public void Write(Resource rsrc)
        {
            using (this.writer = new BinaryWriter(stream))
            {
                writer.Write(LSBHeader.Signature);
                var sizeOffset = stream.Position;
                writer.Write((UInt32)0); // Total size of file, will be updater after we finished serializing
                writer.Write((UInt32)0); // Little-endian format
                writer.Write((UInt32)0); // Unknown
                writer.Write(rsrc.Metadata.timestamp);
                writer.Write(rsrc.Metadata.majorVersion);
                writer.Write(rsrc.Metadata.minorVersion);
                writer.Write(rsrc.Metadata.revision);
                writer.Write(rsrc.Metadata.buildNumber);

                CollectStaticStrings(rsrc);
                WriteStaticStrings();

                WriteRegions(rsrc);

                UInt32 fileSize = (UInt32)stream.Position;
                stream.Seek(sizeOffset, SeekOrigin.Begin);
                writer.Write(fileSize);
            }
        }

        private void WriteRegions(Resource rsrc)
        {
            writer.Write((UInt32)rsrc.Regions.Count);
            var regionMapOffset = stream.Position;
            foreach (var rgn in rsrc.Regions)
            {
                writer.Write(staticStrings[rgn.Key]);
                writer.Write((UInt32)0); // Offset of region, will be updater after we finished serializing
            }

            List<UInt32> regionPositions = new List<UInt32>();
            foreach (var rgn in rsrc.Regions)
            {
                regionPositions.Add((UInt32)stream.Position);
                WriteNode(rgn.Value);
            }

            var endOffset = stream.Position;
            stream.Seek(regionMapOffset, SeekOrigin.Begin);
            foreach (var position in regionPositions)
            {
                stream.Seek(4, SeekOrigin.Current);
                writer.Write(position);
            }

            stream.Seek(endOffset, SeekOrigin.Begin);
        }

        private void WriteNode(Node node)
        {
            writer.Write(staticStrings[node.Name]);
            writer.Write((UInt32)node.Attributes.Count);
            writer.Write((UInt32)node.ChildCount);

            foreach (var attribute in node.Attributes)
            {
                writer.Write(staticStrings[attribute.Key]);
                writer.Write((UInt32)attribute.Value.Type);
                WriteAttribute(attribute.Value);
            }

            foreach (var children in node.Children)
            {
                foreach (var child in children.Value)
                    WriteNode(child);
            }
        }

        private void WriteAttribute(NodeAttribute attr)
        {
            switch (attr.Type)
            {
                case NodeAttribute.DataType.DT_None:
                    break;

                case NodeAttribute.DataType.DT_Byte:
                    writer.Write((Byte)attr.Value);
                    break;

                case NodeAttribute.DataType.DT_Short:
                    writer.Write((Int16)attr.Value);
                    break;

                case NodeAttribute.DataType.DT_UShort:
                    writer.Write((UInt16)attr.Value);
                    break;

                case NodeAttribute.DataType.DT_Int:
                    writer.Write((Int32)attr.Value);
                    break;

                case NodeAttribute.DataType.DT_UInt:
                    writer.Write((UInt32)attr.Value);
                    break;

                case NodeAttribute.DataType.DT_Float:
                    writer.Write((float)attr.Value);
                    break;

                case NodeAttribute.DataType.DT_Double:
                    writer.Write((Double)attr.Value);
                    break;

                case NodeAttribute.DataType.DT_IVec2:
                case NodeAttribute.DataType.DT_IVec3:
                case NodeAttribute.DataType.DT_IVec4:
                    foreach (var item in (int[])attr.Value)
                    {
                        writer.Write(item);
                    }
                    break;

                case NodeAttribute.DataType.DT_Vec2:
                case NodeAttribute.DataType.DT_Vec3:
                case NodeAttribute.DataType.DT_Vec4:
                    foreach (var item in (float[])attr.Value)
                    {
                        writer.Write(item);
                    }
                    break;

                case NodeAttribute.DataType.DT_Mat2:
                case NodeAttribute.DataType.DT_Mat3:
                case NodeAttribute.DataType.DT_Mat3x4:
                case NodeAttribute.DataType.DT_Mat4x3:
                case NodeAttribute.DataType.DT_Mat4:
                    {
                        var mat = (Matrix)attr.Value;
                        for (int col = 0; col < mat.cols; col++)
                        {
                            for (int row = 0; row < mat.rows; row++)
                            {
                                writer.Write((float)mat[row, col]);
                            }
                        }
                        break;
                    }

                case NodeAttribute.DataType.DT_Bool:
                    writer.Write((Byte)((Boolean)attr.Value ? 1 : 0));
                    break;

                case NodeAttribute.DataType.DT_String:
                case NodeAttribute.DataType.DT_Path:
                case NodeAttribute.DataType.DT_FixedString:
                case NodeAttribute.DataType.DT_LSString:
                    WriteString((string)attr.Value, true);
                    break;

                case NodeAttribute.DataType.DT_WString:
                case NodeAttribute.DataType.DT_LSWString:
                    WriteWideString((string)attr.Value, true);
                    break;

                case NodeAttribute.DataType.DT_TranslatedString:
                    var str = (TranslatedString)attr.Value;
                    WriteString(str.Value, true);
                    WriteString(str.Handle, true);
                    break;

                case NodeAttribute.DataType.DT_ULongLong:
                    writer.Write((UInt64)attr.Value);
                    break;

                case NodeAttribute.DataType.DT_ScratchBuffer:
                    var buffer = (byte[])attr.Value;
                    writer.Write((UInt32)buffer.Length);
                    writer.Write(buffer);
                    break;

                case NodeAttribute.DataType.DT_Long:
                    writer.Write((Int64)attr.Value);
                    break;

                case NodeAttribute.DataType.DT_Int8:
                    writer.Write((SByte)attr.Value);
                    break;

                default:
                    throw new InvalidFormatException(String.Format("WriteAttribute() not implemented for type {0}", attr.Type));
            }
        }

        private void CollectStaticStrings(Resource rsrc)
        {
            staticStrings.Clear();
            foreach (var rgn in rsrc.Regions)
            {
                AddStaticString(rgn.Key);
                CollectStaticStrings(rgn.Value);
            }
        }

        private void CollectStaticStrings(Node node)
        {
            AddStaticString(node.Name);

            foreach (var attr in node.Attributes)
            {
                AddStaticString(attr.Key);
            }

            foreach (var children in node.Children)
            {
                foreach (var child in children.Value)
                    CollectStaticStrings(child);
            }
        }

        private void AddStaticString(string s)
        {
            if (!staticStrings.ContainsKey(s))
            {
                staticStrings.Add(s, nextStaticStringId++);
            }
        }

        private void WriteStaticStrings()
        {
            writer.Write((UInt32)staticStrings.Count);
            foreach (var s in staticStrings)
            {
                WriteString(s.Key, false);
                writer.Write(s.Value);
            }
        }

        private void WriteString(string s, bool nullTerminated)
        {
            byte[] utf = System.Text.Encoding.UTF8.GetBytes(s);
            int length = utf.Length + (nullTerminated ? 1 : 0);
            writer.Write(length);
            writer.Write(utf);
            if (nullTerminated)
                writer.Write((Byte)0);
        }

        private void WriteWideString(string s, bool nullTerminated)
        {
            byte[] unicode = System.Text.Encoding.Unicode.GetBytes(s);
            int length = (unicode.Length / 2) + (nullTerminated ? 1 : 0);
            writer.Write(length);
            writer.Write(unicode);
            if (nullTerminated)
                writer.Write((UInt16)0);
        }
    }
}
