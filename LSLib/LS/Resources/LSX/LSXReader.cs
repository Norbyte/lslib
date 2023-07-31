using LSLib.LS.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace LSLib.LS
{
    public class LSXReader : IDisposable
    {
        private static Dictionary<string, NodeAttribute.DataType> TypeNames = new Dictionary<string, NodeAttribute.DataType>
        {
            { "None", NodeAttribute.DataType.DT_None },
            { "uint8", NodeAttribute.DataType.DT_Byte },
            { "int16", NodeAttribute.DataType.DT_Short },
            { "uint16", NodeAttribute.DataType.DT_UShort },
            { "int32", NodeAttribute.DataType.DT_Int },
            { "uint32", NodeAttribute.DataType.DT_UInt },
            { "float", NodeAttribute.DataType.DT_Float },
            { "double", NodeAttribute.DataType.DT_Double },
            { "ivec2", NodeAttribute.DataType.DT_IVec2 },
            { "ivec3", NodeAttribute.DataType.DT_IVec3 },
            { "ivec4", NodeAttribute.DataType.DT_IVec4 },
            { "fvec2", NodeAttribute.DataType.DT_Vec2 },
            { "fvec3", NodeAttribute.DataType.DT_Vec3 },
            { "fvec4", NodeAttribute.DataType.DT_Vec4 },
            { "mat2x2", NodeAttribute.DataType.DT_Mat2 },
            { "mat3x3", NodeAttribute.DataType.DT_Mat3 },
            { "mat3x4", NodeAttribute.DataType.DT_Mat3x4 },
            { "mat4x3", NodeAttribute.DataType.DT_Mat4x3 },
            { "mat4x4", NodeAttribute.DataType.DT_Mat4 },
            { "bool", NodeAttribute.DataType.DT_Bool },
            { "string", NodeAttribute.DataType.DT_String },
            { "path", NodeAttribute.DataType.DT_Path },
            { "FixedString", NodeAttribute.DataType.DT_FixedString },
            { "LSString", NodeAttribute.DataType.DT_LSString },
            { "uint64", NodeAttribute.DataType.DT_ULongLong },
            { "ScratchBuffer", NodeAttribute.DataType.DT_ScratchBuffer },
            { "old_int64", NodeAttribute.DataType.DT_Long },
            { "int8", NodeAttribute.DataType.DT_Int8 },
            { "TranslatedString", NodeAttribute.DataType.DT_TranslatedString },
            { "WString", NodeAttribute.DataType.DT_WString },
            { "LSWString", NodeAttribute.DataType.DT_LSWString },
            { "guid", NodeAttribute.DataType.DT_UUID },
            { "int64", NodeAttribute.DataType.DT_Int64 },
            { "TranslatedFSString", NodeAttribute.DataType.DT_TranslatedFSString },
        };

        private Stream stream;
        private XmlReader reader;
        private Resource resource;
        private Region currentRegion;
        private List<Node> stack;
        private int lastLine, lastColumn;
        private LSXVersion Version = LSXVersion.V3;

        public LSXReader(Stream stream)
        {
            this.stream = stream;
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        private void ReadTranslatedFSString(TranslatedFSString fs)
        {
            fs.Value = reader["value"];
            fs.Handle = reader["handle"];
            Debug.Assert(fs.Handle != null);

            var arguments = Convert.ToInt32(reader["arguments"]);
            fs.Arguments = new List<TranslatedFSStringArgument>(arguments);
            if (arguments > 0)
            {
                while (reader.Read() && reader.NodeType != XmlNodeType.Element);
                if (reader.Name != "arguments")
                {
                    throw new InvalidFormatException(String.Format("Expected <arguments>: {0}", reader.Name));
                }

                int processedArgs = 0;
                while (processedArgs < arguments && reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name != "argument")
                        {
                            throw new InvalidFormatException(String.Format("Expected <argument>: {0}", reader.Name));
                        }

                        var arg = new TranslatedFSStringArgument();
                        arg.Key = reader["key"];
                        arg.Value = reader["value"];

                        while (reader.Read() && reader.NodeType != XmlNodeType.Element);
                        if (reader.Name != "string")
                        {
                            throw new InvalidFormatException(String.Format("Expected <string>: {0}", reader.Name));
                        }

                        arg.String = new TranslatedFSString();
                        ReadTranslatedFSString(arg.String);

                        fs.Arguments.Add(arg);
                        processedArgs++;

                        while (reader.Read() && reader.NodeType != XmlNodeType.EndElement);
                    }
                }

                while (reader.Read() && reader.NodeType != XmlNodeType.EndElement);
                // Close outer element
                while (reader.Read() && reader.NodeType != XmlNodeType.EndElement);
                Debug.Assert(processedArgs == arguments);
            }
        }

        private void ReadElement()
        {
            switch (reader.Name)
            {
                case "save":
                    // Root element
                    if (stack.Count() > 0)
                        throw new InvalidFormatException("Node <save> was unexpected.");
                    break;

                case "header":
                    // LSX metadata part 1
                    resource.Metadata.Timestamp = Convert.ToUInt64(reader["time"]);
                    break;

                case "version":
                    // LSX metadata part 2
                    resource.Metadata.MajorVersion = Convert.ToUInt32(reader["major"]);
                    resource.Metadata.MinorVersion = Convert.ToUInt32(reader["minor"]);
                    resource.Metadata.Revision = Convert.ToUInt32(reader["revision"]);
                    resource.Metadata.BuildNumber = Convert.ToUInt32(reader["build"]);
                    Version = (resource.Metadata.MajorVersion >= 4) ? LSXVersion.V4 : LSXVersion.V3;
                    break;

                case "region":
                    if (currentRegion != null)
                        throw new InvalidFormatException("A <region> can only start at the root level of a resource.");

                    Debug.Assert(!reader.IsEmptyElement);
                    var region = new Region();
                    region.RegionName = reader["id"];
                    Debug.Assert(region.RegionName != null);
                    resource.Regions.Add(region.RegionName, region);
                    currentRegion = region;
                    break;

                case "node":
                    if (currentRegion == null)
                        throw new InvalidFormatException("A <node> must be located inside a region.");

                    Node node;
                    if (stack.Count() == 0)
                    {
                        // The node is the root node of the region
                        node = currentRegion;
                    }
                    else
                    {
                        // New node under the current parent
                        node = new Node();
                        node.Parent = stack.Last();
                    }

                    node.Name = reader["id"];
                    Debug.Assert(node.Name != null);
                    if (node.Parent != null)
                        node.Parent.AppendChild(node);

                    if (!reader.IsEmptyElement)
                        stack.Add(node);
                    break;

                case "attribute":
                    UInt32 attrTypeId;
                    if (Version >= LSXVersion.V4)
                    {
                        attrTypeId = (uint)TypeNames[reader["type"]];
                    }
                    else
                    {
                        if (!UInt32.TryParse(reader["type"], out attrTypeId))
                        {
                            attrTypeId = (uint)TypeNames[reader["type"]];
                        }
                    }

                    var attrName = reader["id"];
                    if (attrTypeId > (int)NodeAttribute.DataType.DT_Max)
                        throw new InvalidFormatException(String.Format("Unsupported attribute data type: {0}", attrTypeId));

                    Debug.Assert(attrName != null);
                    var attr = new NodeAttribute((NodeAttribute.DataType)attrTypeId);

                    var attrValue = reader["value"];
                    if (attrValue != null)
                    {
                        attr.FromString(attrValue);
                    }

                    if (attr.Type == NodeAttribute.DataType.DT_TranslatedString)
                    {
                        if (attr.Value == null)
                        {
                            attr.Value = new TranslatedString();
                        }

                        var ts = ((TranslatedString)attr.Value);
                        ts.Handle = reader["handle"];
                        Debug.Assert(ts.Handle != null);

                        if (attrValue == null)
                        {
                            ts.Version = UInt16.Parse(reader["version"]);
                        }
                    }
                    else if (attr.Type == NodeAttribute.DataType.DT_TranslatedFSString)
                    {
                        var fs = ((TranslatedFSString)attr.Value);
                        ReadTranslatedFSString(fs);
                    }

                    stack.Last().Attributes.Add(attrName, attr);
                    break;

                case "children":
                    // Child nodes are handled in the "node" case
                    break;

                default:
                    throw new InvalidFormatException(String.Format("Unknown element encountered: {0}", reader.Name));
            }
        }

        private void ReadEndElement()
        {
            switch (reader.Name)
            {
                case "save":
                case "header":
                case "version":
                case "attribute":
                case "children":
                    // These elements don't change the stack, just discard them
                    break;

                case "region":
                    Debug.Assert(stack.Count == 0);
                    Debug.Assert(currentRegion != null);
                    Debug.Assert(currentRegion.Name != null);
                    currentRegion = null;
                    break;

                case "node":
                    stack.RemoveAt(stack.Count - 1);
                    break;

                default:
                    throw new InvalidFormatException(String.Format("Unknown element encountered: {0}", reader.Name));
            }
        }

        private void ReadInternal()
        {
            using (this.reader = XmlReader.Create(stream))
            {
                try
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            ReadElement();
                        }
                        else if (reader.NodeType == XmlNodeType.EndElement)
                        {
                            ReadEndElement();
                        }
                    }
                } catch (Exception)
                {
                    lastLine = ((IXmlLineInfo)reader).LineNumber;
                    lastColumn = ((IXmlLineInfo)reader).LinePosition;
                    throw;
                }
            }
        }

        public Resource Read()
        {
            resource = new Resource();
            currentRegion = null;
            stack = new List<Node>();
            lastLine = lastColumn = 0;
            var resultResource = resource;

            try
            {
                ReadInternal();
            }
            catch (Exception e)
            {
                if (lastLine > 0)
                {
                    throw new Exception($"Parsing error at or near line {lastLine}, column {lastColumn}:{Environment.NewLine}{e.Message}", e);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                resource = null;
                currentRegion = null;
                stack = null;
            }

            return resultResource;
        }
    }
}
