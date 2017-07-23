using System;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;

namespace LSLib.LS
{
    public class LSJResourceConverter : JsonConverter
    {
        public LSJResourceConverter()
        {
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Node)
                || objectType == typeof(Resource);
        }

        private NodeAttribute ReadAttribute(JsonReader reader)
        {
            string key = "", handle = "";
            NodeAttribute attribute = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                {
                    break;
                }
                else if (reader.TokenType == JsonToken.PropertyName)
                {
                    key = reader.Value.ToString();
                }
                else if (reader.TokenType == JsonToken.String
                    || reader.TokenType == JsonToken.Integer
                    || reader.TokenType == JsonToken.Float
                    || reader.TokenType == JsonToken.Boolean)
                {
                    if (key == "type")
                    {
                        var type = (NodeAttribute.DataType)Convert.ToUInt32(reader.Value);
                        attribute = new NodeAttribute(type);
                    }
                    else if (key == "value")
                    {
                        switch (attribute.Type)
                        {
                            case NodeAttribute.DataType.DT_Byte:
                                attribute.Value = Convert.ToByte(reader.Value);
                                break;

                            case NodeAttribute.DataType.DT_Short:
                                attribute.Value = Convert.ToInt16(reader.Value);
                                break;

                            case NodeAttribute.DataType.DT_UShort:
                                attribute.Value = Convert.ToUInt16(reader.Value);
                                break;

                            case NodeAttribute.DataType.DT_Int:
                                attribute.Value = Convert.ToInt32(reader.Value);
                                break;

                            case NodeAttribute.DataType.DT_UInt:
                                attribute.Value = Convert.ToUInt32(reader.Value);
                                break;

                            case NodeAttribute.DataType.DT_Float:
                                attribute.Value = Convert.ToSingle(reader.Value);
                                break;

                            case NodeAttribute.DataType.DT_Double:
                                attribute.Value = Convert.ToDouble(reader.Value);
                                break;

                            case NodeAttribute.DataType.DT_Bool:
                                attribute.Value = Convert.ToBoolean(reader.Value);
                                break;

                            case NodeAttribute.DataType.DT_String:
                            case NodeAttribute.DataType.DT_Path:
                            case NodeAttribute.DataType.DT_FixedString:
                            case NodeAttribute.DataType.DT_LSString:
                            case NodeAttribute.DataType.DT_WString:
                            case NodeAttribute.DataType.DT_LSWString:
                                attribute.Value = reader.Value.ToString();
                                break;

                            case NodeAttribute.DataType.DT_ULongLong:
                                attribute.Value = Convert.ToUInt64(reader.Value);
                                break;

                            // TODO: Not sure if this is the correct format
                            case NodeAttribute.DataType.DT_ScratchBuffer:
                                attribute.Value = Convert.FromBase64String(reader.Value.ToString());
                                break;

                            case NodeAttribute.DataType.DT_Long:
                                attribute.Value = Convert.ToInt64(reader.Value);
                                break;

                            case NodeAttribute.DataType.DT_Int8:
                                attribute.Value = Convert.ToSByte(reader.Value);
                                break;

                            case NodeAttribute.DataType.DT_TranslatedString:
                                var translatedString = new TranslatedString();
                                translatedString.Value = reader.Value.ToString();
                                if (handle == null)
                                {
                                    throw new InvalidDataException("Missing handle for translated string");
                                }

                                translatedString.Handle = handle;
                                handle = null;
                                attribute.Value = translatedString;
                                break;

                            case NodeAttribute.DataType.DT_UUID:
                                attribute.Value = new Guid(reader.Value.ToString());
                                break;

                            // TODO: haven't seen any vectors/matrices in D:OS JSON files so far
                            case NodeAttribute.DataType.DT_None:
                            case NodeAttribute.DataType.DT_IVec2:
                            case NodeAttribute.DataType.DT_IVec3:
                            case NodeAttribute.DataType.DT_IVec4:
                            case NodeAttribute.DataType.DT_Vec2:
                            case NodeAttribute.DataType.DT_Vec3:
                            case NodeAttribute.DataType.DT_Vec4:
                            case NodeAttribute.DataType.DT_Mat2:
                            case NodeAttribute.DataType.DT_Mat3:
                            case NodeAttribute.DataType.DT_Mat3x4:
                            case NodeAttribute.DataType.DT_Mat4x3:
                            case NodeAttribute.DataType.DT_Mat4:
                            default:
                                throw new NotImplementedException("Don't know how to unserialize type " + attribute.Type.ToString());
                        }
                    }
                    else if (key == "handle")
                    {
                        handle = reader.Value.ToString();
                    }
                    else
                    {
                        throw new InvalidDataException("Unknown property encountered during attribute parsing: " + key);
                    }
                }
                else
                {
                    throw new InvalidDataException("Unexpected JSON token during parsing of attribute: " + reader.TokenType);
                }
            }

            return attribute;
        }

        private Node ReadNode(JsonReader reader, Node node)
        {
            string key = "";
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                {
                    break;
                }
                else if (reader.TokenType == JsonToken.PropertyName)
                {
                    key = reader.Value.ToString();
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    var attribute = ReadAttribute(reader);
                    node.Attributes.Add(key, attribute);
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.EndArray)
                        {
                            break;
                        }
                        else if (reader.TokenType == JsonToken.StartObject)
                        {
                            var childNode = new Node();
                            childNode.Name = key;
                            ReadNode(reader, childNode);
                            node.AppendChild(childNode);
                        }
                        else
                        {
                            throw new InvalidDataException("Unexpected JSON token during parsing of child node list: " + reader.TokenType);
                        }
                    }
                }
                else
                {
                    throw new InvalidDataException("Unexpected JSON token during parsing of node: " + reader.TokenType);
                }
            }
            
            return node;
        }

        private Resource ReadResource(JsonReader reader, Resource resource)
        {
            if (resource == null) resource = new Resource();

            if (!reader.Read() || reader.TokenType != JsonToken.PropertyName || !reader.Value.Equals("save"))
            {
                throw new InvalidDataException("Expected JSON property 'save'");
            }

            if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
            {
                throw new InvalidDataException("Expected JSON object start token for 'save': " + reader.TokenType);
            }

            if (!reader.Read() || reader.TokenType != JsonToken.PropertyName || !reader.Value.Equals("header"))
            {
                throw new InvalidDataException("Expected JSON property 'header'");
            }

            if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
            {
                throw new InvalidDataException("Expected JSON object start token for 'header': " + reader.TokenType);
            }

            string key = "";
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                {
                    break;
                }
                else if (reader.TokenType == JsonToken.PropertyName)
                {
                    key = reader.Value.ToString();
                }
                else if (reader.TokenType == JsonToken.String || reader.TokenType == JsonToken.Integer)
                {
                    if (key == "time")
                    {
                        resource.Metadata.timestamp = Convert.ToUInt32(reader.Value);
                    }
                    else if (key == "version")
                    {
                        var pattern = @"^([0-9]+)\.([0-9]+)\.([0-9]+)\.([0-9]+)$";
                        var re = new Regex(pattern);
                        var match = re.Match(reader.Value.ToString());
                        if (match.Success)
                        {
                            resource.Metadata.majorVersion = Convert.ToUInt32(match.Groups[1].Value);
                            resource.Metadata.minorVersion = Convert.ToUInt32(match.Groups[2].Value);
                            resource.Metadata.revision = Convert.ToUInt32(match.Groups[3].Value);
                            resource.Metadata.buildNumber = Convert.ToUInt32(match.Groups[4].Value);
                        }
                        else
                        {
                            throw new InvalidDataException("Malformed version string: " + reader.Value.ToString());
                        }
                    }
                    else
                    {
                        throw new InvalidDataException("Unknown property encountered during header parsing: " + key);
                    }
                }
                else
                {
                    throw new InvalidDataException("Unexpected JSON token during parsing of header: " + reader.TokenType);
                }
            }

            if (!reader.Read() || reader.TokenType != JsonToken.PropertyName || !reader.Value.Equals("regions"))
            {
                throw new InvalidDataException("Expected JSON property 'regions'");
            }

            if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
            {
                throw new InvalidDataException("Expected JSON object start token for 'regions': " + reader.TokenType);
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                {
                    break;
                }
                else if (reader.TokenType == JsonToken.PropertyName)
                {
                    key = reader.Value.ToString();
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    var region = new Region();
                    ReadNode(reader, region);
                    region.Name = "root";
                    region.RegionName = key;
                    resource.Regions.Add(key, region);
                }
                else
                {
                    throw new InvalidDataException("Unexpected JSON token during parsing of region list: " + reader.TokenType);
                }
            }

            return resource;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(Node))
            {
                return ReadNode(reader, existingValue as Node);
            }
            else if (objectType == typeof(Resource))
            {
                return ReadResource(reader, existingValue as Resource);
            }
            else
            {
                throw new InvalidOperationException("Cannot unserialize unknown type");
            }
        }

        private void WriteResource(JsonWriter writer, Resource resource, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("save");
            writer.WriteStartObject();

            writer.WritePropertyName("header");
            writer.WriteStartObject();
            writer.WritePropertyName("time");
            writer.WriteValue(resource.Metadata.timestamp);
            writer.WritePropertyName("version");
            var versionString = resource.Metadata.majorVersion.ToString() + "."
                + resource.Metadata.minorVersion.ToString() + "."
                + resource.Metadata.revision.ToString() + "."
                + resource.Metadata.buildNumber.ToString();
            writer.WriteValue(versionString);
            writer.WriteEndObject();

            writer.WritePropertyName("regions");
            writer.WriteStartObject();
            foreach (var region in resource.Regions)
            {
                writer.WritePropertyName(region.Key);
                WriteNode(writer, region.Value, serializer);
            }
            writer.WriteEndObject();

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        private void WriteNode(JsonWriter writer, Node node, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            foreach (var attribute in node.Attributes)
            {
                writer.WritePropertyName(attribute.Key);
                writer.WriteStartObject();
                writer.WritePropertyName("type");
                writer.WriteValue((int)attribute.Value.Type);
                writer.WritePropertyName("value");
                switch (attribute.Value.Type)
                {
                    case NodeAttribute.DataType.DT_Byte:
                        writer.WriteValue(Convert.ToByte(attribute.Value.Value));
                        break;

                    case NodeAttribute.DataType.DT_Short:
                        writer.WriteValue(Convert.ToInt16(attribute.Value.Value));
                        break;

                    case NodeAttribute.DataType.DT_UShort:
                        writer.WriteValue(Convert.ToUInt16(attribute.Value.Value));
                        break;

                    case NodeAttribute.DataType.DT_Int:
                        writer.WriteValue(Convert.ToInt32(attribute.Value.Value));
                        break;

                    case NodeAttribute.DataType.DT_UInt:
                        writer.WriteValue(Convert.ToUInt32(attribute.Value.Value));
                        break;

                    case NodeAttribute.DataType.DT_Float:
                        writer.WriteValue(Convert.ToSingle(attribute.Value.Value));
                        break;

                    case NodeAttribute.DataType.DT_Double:
                        writer.WriteValue(Convert.ToDouble(attribute.Value.Value));
                        break;

                    case NodeAttribute.DataType.DT_Bool:
                        writer.WriteValue(Convert.ToBoolean(attribute.Value.Value));
                        break;

                    case NodeAttribute.DataType.DT_String:
                    case NodeAttribute.DataType.DT_Path:
                    case NodeAttribute.DataType.DT_FixedString:
                    case NodeAttribute.DataType.DT_LSString:
                    case NodeAttribute.DataType.DT_WString:
                    case NodeAttribute.DataType.DT_LSWString:
                        writer.WriteValue(attribute.Value.ToString());
                        break;

                    case NodeAttribute.DataType.DT_ULongLong:
                        writer.WriteValue(Convert.ToUInt64(attribute.Value.Value));
                        break;

                    // TODO: Not sure if this is the correct format
                    case NodeAttribute.DataType.DT_ScratchBuffer:
                        writer.WriteValue(Convert.ToBase64String((byte[])attribute.Value.Value));
                        break;

                    case NodeAttribute.DataType.DT_Long:
                        writer.WriteValue(Convert.ToInt64(attribute.Value.Value));
                        break;

                    case NodeAttribute.DataType.DT_Int8:
                        writer.WriteValue(Convert.ToSByte(attribute.Value.Value));
                        break;

                    case NodeAttribute.DataType.DT_TranslatedString:
                        writer.WriteValue(attribute.Value.ToString());
                        writer.WritePropertyName("handle");
                        writer.WriteValue(((TranslatedString)attribute.Value.Value).Handle);
                        break;

                    case NodeAttribute.DataType.DT_UUID:
                        writer.WriteValue(((Guid)attribute.Value.Value).ToString());
                        break;
                        
                    // TODO: haven't seen any vectors/matrices in D:OS JSON files so far
                    case NodeAttribute.DataType.DT_Vec2:
                    case NodeAttribute.DataType.DT_Vec3:
                    case NodeAttribute.DataType.DT_Vec4:
                        {
                            var vec = (float[])attribute.Value.Value;
                            writer.WriteValue(String.Join(" ", vec));
                            break;
                        }

                    case NodeAttribute.DataType.DT_IVec2:
                    case NodeAttribute.DataType.DT_IVec3:
                    case NodeAttribute.DataType.DT_IVec4:
                        {
                            var ivec = (int[])attribute.Value.Value;
                            writer.WriteValue(String.Join(" ", ivec));
                            break;
                        }

                    case NodeAttribute.DataType.DT_Mat2:
                    case NodeAttribute.DataType.DT_Mat3:
                    case NodeAttribute.DataType.DT_Mat3x4:
                    case NodeAttribute.DataType.DT_Mat4x3:
                    case NodeAttribute.DataType.DT_Mat4:
                        {
                            var mat = (Matrix)attribute.Value.Value;
                            var str = "";
                            for (var r = 0; r < mat.rows; r++)
                            {
                                for (var c = 0; c < mat.cols; c++)
                                    str += mat[r, c].ToString() + ",";
                            }

                            writer.WriteValue(str);
                            break;
                        }

                    case NodeAttribute.DataType.DT_None:
                    default:
                        throw new NotImplementedException("Don't know how to serialize type " + attribute.Value.Type.ToString());
                }

                writer.WriteEndObject();
            }

            foreach (var children in node.Children)
            {
                writer.WritePropertyName(children.Key);
                writer.WriteStartArray();
                foreach (var child in children.Value)
                    WriteNode(writer, child, serializer);
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is Node)
            {
                WriteNode(writer, value as Node, serializer);
            }
            else if (value is Resource)
            {
                WriteResource(writer, value as Resource, serializer);
            }
            else
            {
                throw new InvalidOperationException("Cannot serialize unknown type");
            }
        }
    }
}
