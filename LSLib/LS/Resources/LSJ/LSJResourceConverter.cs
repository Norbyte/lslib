using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Numerics;

namespace LSLib.LS;

public class LSJResourceConverter(NodeSerializationSettings settings) : JsonConverter
{
    private LSMetadata Metadata;
    private readonly NodeSerializationSettings SerializationSettings = settings;

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Node)
            || objectType == typeof(Resource);
    }

    private TranslatedFSStringArgument ReadFSStringArgument(JsonReader reader)
    {
        var fs = new TranslatedFSStringArgument();
        string key = null;
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
            else if (reader.TokenType == JsonToken.String)
            {
                if (key == "key")
                {
                    fs.Key = reader.Value.ToString();
                }
                else if (key == "value")
                {
                    fs.Value = reader.Value.ToString();
                }
                else
                {
                    throw new InvalidDataException("Unknown property encountered during TranslatedFSString argument parsing: " + key);
                }
            }
            else if (reader.TokenType == JsonToken.StartObject && key == "string")
            {
                fs.String = ReadTranslatedFSString(reader);
            }
            else
            {
                throw new InvalidDataException("Unexpected JSON token during parsing of TranslatedFSString argument: " + reader.TokenType);
            }
        }

        return fs;
    }

    private TranslatedFSString ReadTranslatedFSString(JsonReader reader)
    {
        var fs = new TranslatedFSString();
        string key = "";

        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                key = reader.Value.ToString();
            }
            else if (reader.TokenType == JsonToken.String)
            {
                if (key == "value")
                {
                    if (reader.Value != null)
                    {
                        fs.Value = reader.Value.ToString();
                    }
                    else
                    {
                        fs.Value = null;
                    }
                }
                else if (key == "handle")
                {
                    fs.Handle = reader.Value.ToString();
                }
                else
                {
                    throw new InvalidDataException("Unknown TranslatedFSString property: " + key);
                }
            }
            else if (reader.TokenType == JsonToken.StartArray && key == "arguments")
            {
                fs.Arguments = ReadFSStringArguments(reader);
            }
            else if (reader.TokenType == JsonToken.EndObject)
            {
                break;
            }
            else
            {
                throw new InvalidDataException("Unexpected JSON token during parsing of TranslatedFSString: " + reader.TokenType);
            }
        }

        return fs;
    }

    private List<TranslatedFSStringArgument> ReadFSStringArguments(JsonReader reader)
    {
        var args = new List<TranslatedFSStringArgument>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                args.Add(ReadFSStringArgument(reader));
            }
            else if (reader.TokenType == JsonToken.EndArray)
            {
                break;
            }
            else
            {
                throw new InvalidDataException("Unexpected JSON token during parsing of TranslatedFSString argument list: " + reader.TokenType);
            }
        }

        return args;
    }

    private NodeAttribute ReadAttribute(JsonReader reader)
    {
        string key = "", handle = null;
        List<TranslatedFSStringArgument> fsStringArguments = null;
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
                || reader.TokenType == JsonToken.Boolean
                || reader.TokenType == JsonToken.Null)
            {
                if (key == "type")
                {
                    if (!UInt32.TryParse((string)reader.Value, out uint type))
                    {
                        type = (uint)AttributeTypeMaps.TypeToId[(string)reader.Value];
                    }

                    attribute = new NodeAttribute((AttributeType)type);
                    if (type == (uint)AttributeType.TranslatedString)
                    {
                        attribute.Value = new TranslatedString
                        {
                            Handle = handle
                        };
                    }
                    else if (type == (uint)AttributeType.TranslatedFSString)
                    {
                        attribute.Value = new TranslatedFSString
                        {
                            Handle = handle,
                            Arguments = fsStringArguments
                        };
                    }
                }
                else if (key == "value")
                {
                    switch (attribute.Type)
                    {
                        case AttributeType.Byte:
                            attribute.Value = Convert.ToByte(reader.Value);
                            break;

                        case AttributeType.Short:
                            attribute.Value = Convert.ToInt16(reader.Value);
                            break;

                        case AttributeType.UShort:
                            attribute.Value = Convert.ToUInt16(reader.Value);
                            break;

                        case AttributeType.Int:
                            attribute.Value = Convert.ToInt32(reader.Value);
                            break;

                        case AttributeType.UInt:
                            attribute.Value = Convert.ToUInt32(reader.Value);
                            break;

                        case AttributeType.Float:
                            attribute.Value = Convert.ToSingle(reader.Value);
                            break;

                        case AttributeType.Double:
                            attribute.Value = Convert.ToDouble(reader.Value);
                            break;

                        case AttributeType.Bool:
                            attribute.Value = Convert.ToBoolean(reader.Value);
                            break;

                        case AttributeType.String:
                        case AttributeType.Path:
                        case AttributeType.FixedString:
                        case AttributeType.LSString:
                        case AttributeType.WString:
                        case AttributeType.LSWString:
                            attribute.Value = reader.Value.ToString();
                            break;

                        case AttributeType.ULongLong:
                            if (reader.Value.GetType() == typeof(System.Int64))
                                attribute.Value = Convert.ToUInt64((long)reader.Value);
                            else if (reader.Value.GetType() == typeof(BigInteger))
                                attribute.Value = (ulong)((BigInteger)reader.Value);
                            else
                                attribute.Value = (ulong)reader.Value;
                            break;

                        // TODO: Not sure if this is the correct format
                        case AttributeType.ScratchBuffer:
                            attribute.Value = Convert.FromBase64String(reader.Value.ToString());
                            break;

                        case AttributeType.Long:
                        case AttributeType.Int64:
                            attribute.Value = Convert.ToInt64(reader.Value);
                            break;

                        case AttributeType.Int8:
                            attribute.Value = Convert.ToSByte(reader.Value);
                            break;

                        case AttributeType.TranslatedString:
                            {
                                attribute.Value ??= new TranslatedString();

                                var ts = (TranslatedString)attribute.Value;
                                ts.Value = reader.Value.ToString();
                                ts.Handle = handle;
                                break;
                            }

                        case AttributeType.TranslatedFSString:
                            {
                                attribute.Value ??= new TranslatedFSString();

                                var fsString = (TranslatedFSString)attribute.Value;
                                fsString.Value = reader.Value?.ToString();
                                fsString.Handle = handle;
                                fsString.Arguments = fsStringArguments;
                                attribute.Value = fsString;
                                break;
                            }

                        case AttributeType.UUID:
                            if (SerializationSettings.ByteSwapGuids)
                            {
                                attribute.Value = NodeAttribute.ByteSwapGuid(new Guid(reader.Value.ToString()));
                            }
                            else
                            {
                                attribute.Value = new Guid(reader.Value.ToString());
                            }
                            break;

                        case AttributeType.IVec2:
                        case AttributeType.IVec3:
                        case AttributeType.IVec4:
                            {
                                string[] nums = reader.Value.ToString().Split(' ');
                                int length = attribute.Type.GetColumns();
                                if (length != nums.Length)
                                    throw new FormatException(String.Format("A vector of length {0} was expected, got {1}", length, nums.Length));

                                int[] vec = new int[length];
                                for (int i = 0; i < length; i++)
                                    vec[i] = int.Parse(nums[i]);

                                attribute.Value = vec;
                                break;
                            }

                        case AttributeType.Vec2:
                        case AttributeType.Vec3:
                        case AttributeType.Vec4:
                            {
                                string[] nums = reader.Value.ToString().Split(' ');
                                int length = attribute.Type.GetColumns();
                                if (length != nums.Length)
                                    throw new FormatException(String.Format("A vector of length {0} was expected, got {1}", length, nums.Length));

                                float[] vec = new float[length];
                                for (int i = 0; i < length; i++)
                                    vec[i] = float.Parse(nums[i]);

                                attribute.Value = vec;
                                break;
                            }

                        case AttributeType.Mat2:
                        case AttributeType.Mat3:
                        case AttributeType.Mat3x4:
                        case AttributeType.Mat4x3:
                        case AttributeType.Mat4:
                            var mat = Matrix.Parse(reader.Value.ToString());
                            if (mat.cols != attribute.Type.GetColumns() || mat.rows != attribute.Type.GetRows())
                                throw new FormatException("Invalid column/row count for matrix");
                            attribute.Value = mat;
                            break;

                        case AttributeType.None:
                        default:
                            throw new NotImplementedException("Don't know how to unserialize type " + attribute.Type.ToString());
                    }
                }
                else if (key == "handle")
                {
                    if (attribute != null)
                    {
                        if (attribute.Type == AttributeType.TranslatedString)
                        {
                            attribute.Value ??= new TranslatedString();

                            ((TranslatedString)attribute.Value).Handle = reader.Value.ToString();
                        }
                        else if (attribute.Type == AttributeType.TranslatedFSString)
                        {
                            attribute.Value ??= new TranslatedFSString();

                            ((TranslatedFSString)attribute.Value).Handle = reader.Value.ToString();
                        }
                    }
                    else
                    {
                        handle = reader.Value.ToString();
                    }
                }
                else if (key == "version")
                {
                    attribute.Value ??= new TranslatedString();

                    var ts = (TranslatedString)attribute.Value;
                    ts.Version = UInt16.Parse(reader.Value.ToString());
                }
                else
                {
                    throw new InvalidDataException("Unknown property encountered during attribute parsing: " + key);
                }
            }
            else if (reader.TokenType == JsonToken.StartArray && key == "arguments")
            {
                var args = ReadFSStringArguments(reader);

                if (attribute.Value != null)
                {
                    var fs = ((TranslatedFSString)attribute.Value);
                    fs.Arguments = args;
                }
                else
                {
                    fsStringArguments = args;
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
                        var childNode = new Node
                        {
                            Name = key
                        };
                        ReadNode(reader, childNode);
                        node.AppendChild(childNode);
                        childNode.Parent = node;
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
        resource ??= new Resource();

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
                    resource.Metadata.Timestamp = Convert.ToUInt32(reader.Value);
                }
                else if (key == "version")
                {
                    var pattern = @"^([0-9]+)\.([0-9]+)\.([0-9]+)\.([0-9]+)$";
                    var re = new Regex(pattern);
                    var match = re.Match(reader.Value.ToString());
                    if (match.Success)
                    {
                        resource.Metadata.MajorVersion = Convert.ToUInt32(match.Groups[1].Value);
                        resource.Metadata.MinorVersion = Convert.ToUInt32(match.Groups[2].Value);
                        resource.Metadata.Revision = Convert.ToUInt32(match.Groups[3].Value);
                        resource.Metadata.BuildNumber = Convert.ToUInt32(match.Groups[4].Value);
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
                region.Name = key;
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
        Metadata = resource.Metadata;
        writer.WriteStartObject();

        writer.WritePropertyName("save");
        writer.WriteStartObject();

        writer.WritePropertyName("header");
        writer.WriteStartObject();
        writer.WritePropertyName("time");
        writer.WriteValue(resource.Metadata.Timestamp);
        writer.WritePropertyName("version");
        var versionString = resource.Metadata.MajorVersion.ToString() + "."
            + resource.Metadata.MinorVersion.ToString() + "."
            + resource.Metadata.Revision.ToString() + "."
            + resource.Metadata.BuildNumber.ToString();
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

    private void WriteTranslatedFSString(JsonWriter writer, TranslatedFSString fs)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("value");
        WriteTranslatedFSStringInner(writer, fs);
        writer.WriteEndObject();
    }

    private void WriteTranslatedFSStringInner(JsonWriter writer, TranslatedFSString fs)
    {
        writer.WriteValue(fs.Value);
        writer.WritePropertyName("handle");
        writer.WriteValue(fs.Handle);
        writer.WritePropertyName("arguments");
        writer.WriteStartArray();
        for (int i = 0; i < fs.Arguments.Count; i++)
        {
            var arg = fs.Arguments[i];
            writer.WriteStartObject();
            writer.WritePropertyName("key");
            writer.WriteValue(arg.Key);
            writer.WritePropertyName("string");
            WriteTranslatedFSString(writer, arg.String);
            writer.WritePropertyName("value");
            writer.WriteValue(arg.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private void WriteNode(JsonWriter writer, Node node, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        foreach (var attribute in node.Attributes)
        {
            writer.WritePropertyName(attribute.Key);
            writer.WriteStartObject();
            writer.WritePropertyName("type");
            if (Metadata.MajorVersion >= 4)
            {
                writer.WriteValue(AttributeTypeMaps.IdToType[attribute.Value.Type]);
            }
            else
            {
                writer.WriteValue((int)attribute.Value.Type);
            }

            if (attribute.Value.Type != AttributeType.TranslatedString)
            {
                writer.WritePropertyName("value");
            }

            switch (attribute.Value.Type)
            {
                case AttributeType.Byte:
                    writer.WriteValue(Convert.ToByte(attribute.Value.Value));
                    break;

                case AttributeType.Short:
                    writer.WriteValue(Convert.ToInt16(attribute.Value.Value));
                    break;

                case AttributeType.UShort:
                    writer.WriteValue(Convert.ToUInt16(attribute.Value.Value));
                    break;

                case AttributeType.Int:
                    writer.WriteValue(Convert.ToInt32(attribute.Value.Value));
                    break;

                case AttributeType.UInt:
                    writer.WriteValue(Convert.ToUInt32(attribute.Value.Value));
                    break;

                case AttributeType.Float:
                    writer.WriteValue(Convert.ToSingle(attribute.Value.Value));
                    break;

                case AttributeType.Double:
                    writer.WriteValue(Convert.ToDouble(attribute.Value.Value));
                    break;

                case AttributeType.Bool:
                    writer.WriteValue(Convert.ToBoolean(attribute.Value.Value));
                    break;

                case AttributeType.String:
                case AttributeType.Path:
                case AttributeType.FixedString:
                case AttributeType.LSString:
                case AttributeType.WString:
                case AttributeType.LSWString:
                    writer.WriteValue(attribute.Value.AsString(SerializationSettings));
                    break;

                case AttributeType.ULongLong:
                    writer.WriteValue(Convert.ToUInt64(attribute.Value.Value));
                    break;

                // TODO: Not sure if this is the correct format
                case AttributeType.ScratchBuffer:
                    writer.WriteValue(Convert.ToBase64String((byte[])attribute.Value.Value));
                    break;

                case AttributeType.Long:
                case AttributeType.Int64:
                    writer.WriteValue(Convert.ToInt64(attribute.Value.Value));
                    break;

                case AttributeType.Int8:
                    writer.WriteValue(Convert.ToSByte(attribute.Value.Value));
                    break;

                case AttributeType.TranslatedString:
                    {
                        var ts = (TranslatedString)attribute.Value.Value;

                        if (ts.Value != null)
                        {
                            writer.WritePropertyName("value");
                            writer.WriteValue(ts.Value);
                        }

                        if (ts.Version > 0)
                        {
                            writer.WritePropertyName("version");
                            writer.WriteValue(ts.Version);
                        }

                        writer.WritePropertyName("handle");
                        writer.WriteValue(ts.Handle);
                        break;
                    }

                case AttributeType.TranslatedFSString:
                    {
                        var fs = (TranslatedFSString)attribute.Value.Value;
                        WriteTranslatedFSStringInner(writer, fs);
                        break;
                    }

                case AttributeType.UUID:
                    if (SerializationSettings.ByteSwapGuids)
                    {
                        writer.WriteValue((NodeAttribute.ByteSwapGuid((Guid)attribute.Value.Value)).ToString());
                    }
                    else
                    {
                        writer.WriteValue(((Guid)attribute.Value.Value).ToString());
                    }
                    break;

                // TODO: haven't seen any vectors/matrices in D:OS JSON files so far
                case AttributeType.Vec2:
                case AttributeType.Vec3:
                case AttributeType.Vec4:
                    {
                        var vec = (float[])attribute.Value.Value;
                        writer.WriteValue(String.Join(" ", vec));
                        break;
                    }

                case AttributeType.IVec2:
                case AttributeType.IVec3:
                case AttributeType.IVec4:
                    {
                        var ivec = (int[])attribute.Value.Value;
                        writer.WriteValue(String.Join(" ", ivec));
                        break;
                    }

                case AttributeType.Mat2:
                case AttributeType.Mat3:
                case AttributeType.Mat3x4:
                case AttributeType.Mat4x3:
                case AttributeType.Mat4:
                    {
                        var mat = (Matrix)attribute.Value.Value;
                        var str = "";
                        for (var r = 0; r < mat.rows; r++)
                        {
                            for (var c = 0; c < mat.cols; c++)
                                str += mat[r, c].ToString() + " ";
                            str += Environment.NewLine;
                        }

                        writer.WriteValue(str);
                        break;
                    }

                case AttributeType.None:
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
