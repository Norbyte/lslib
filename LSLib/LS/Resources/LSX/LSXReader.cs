using LSLib.LS.Enums;
using System.Diagnostics;
using System.Xml;

namespace LSLib.LS;

public class LSXReader(Stream stream) : IDisposable
{
    private Stream stream = stream;
    private XmlReader reader;
    private Resource resource;
    private Region currentRegion;
    private List<Node> stack;
    public int lastLine, lastColumn;
    private LSXVersion Version = LSXVersion.V3;
    public NodeSerializationSettings SerializationSettings = new();
    private NodeAttribute LastAttribute = null;
    private int ValueOffset = 0;

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

                    var arg = new TranslatedFSStringArgument
                    {
                        Key = reader["key"],
                        Value = reader["value"]
                    };

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
                if (stack.Count > 0)
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
                var lslibMeta = reader["lslib_meta"];
                SerializationSettings.InitFromMeta(lslibMeta ?? "");
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
                if (stack.Count == 0)
                {
                    // The node is the root node of the region
                    node = currentRegion;
                }
                else
                {
                    // New node under the current parent
                    node = new Node
                    {
                        Parent = stack.Last(),
                        Line = ((IXmlLineInfo)reader).LineNumber
                    };
                }

                node.Name = reader["id"];
                Debug.Assert(node.Name != null);
                node.Parent?.AppendChild(node);

                if (!reader.IsEmptyElement)
                    stack.Add(node);
                break;

            case "attribute":
                UInt32 attrTypeId;
                if (!UInt32.TryParse(reader["type"], out attrTypeId))
                {
                    attrTypeId = (uint)AttributeTypeMaps.TypeToId[reader["type"]];
                }

                var attrName = reader["id"];
                if (attrTypeId > (int)AttributeType.Max)
                    throw new InvalidFormatException(String.Format("Unsupported attribute data type: {0}", attrTypeId));

                Debug.Assert(attrName != null);
                var attr = new NodeAttribute((AttributeType)attrTypeId)
                {
                    Line = ((IXmlLineInfo)reader).LineNumber
                };

                var attrValue = reader["value"];
                if (attrValue != null)
                {
                    attr.FromString(attrValue, SerializationSettings);
                }
                else
                {
                    // Preallocate value for vector/matrix types
                    switch (attr.Type)
                    {
                        case AttributeType.Vec2: attr.Value = new float[2]; break;
                        case AttributeType.Vec3: attr.Value = new float[3]; break;
                        case AttributeType.Vec4: attr.Value = new float[4]; break;
                        case AttributeType.Mat2: attr.Value = new float[2*2]; break;
                        case AttributeType.Mat3: attr.Value = new float[3*3]; break;
                        case AttributeType.Mat3x4: attr.Value = new float[3*4]; break;
                        case AttributeType.Mat4: attr.Value = new float[4*4]; break;
                        case AttributeType.Mat4x3: attr.Value = new float[4*3]; break;
                        case AttributeType.TranslatedString: break;
                        case AttributeType.TranslatedFSString: break;
                        default: throw new Exception($"Attribute of type {attr.Type} should have an inline value!");
                    }

                    ValueOffset = 0;
                    LastAttribute = attr;
                }

                if (attr.Type == AttributeType.TranslatedString)
                {
                    attr.Value ??= new TranslatedString();

                    var ts = ((TranslatedString)attr.Value);
                    ts.Handle = reader["handle"];
                    Debug.Assert(ts.Handle != null);

                    if (attrValue == null)
                    {
                        ts.Version = UInt16.Parse(reader["version"]);
                    }
                }
                else if (attr.Type == AttributeType.TranslatedFSString)
                {
                    var fs = ((TranslatedFSString)attr.Value);
                    ReadTranslatedFSString(fs);
                }

                stack.Last().Attributes.Add(attrName, attr);
                break;

            case "float2":
                {
                    var val = (float[])LastAttribute.Value;
                    val[ValueOffset++] = Single.Parse(reader["x"]);
                    val[ValueOffset++] = Single.Parse(reader["y"]);
                    break;
                }

            case "float3":
                {
                    var val = (float[])LastAttribute.Value;
                    val[ValueOffset++] = Single.Parse(reader["x"]);
                    val[ValueOffset++] = Single.Parse(reader["y"]);
                    val[ValueOffset++] = Single.Parse(reader["z"]);
                    break;
                }

            case "float4":
                {
                    var val = (float[])LastAttribute.Value;
                    val[ValueOffset++] = Single.Parse(reader["x"]);
                    val[ValueOffset++] = Single.Parse(reader["y"]);
                    val[ValueOffset++] = Single.Parse(reader["z"]);
                    val[ValueOffset++] = Single.Parse(reader["w"]);
                    break;
                }

            case "mat2":
            case "mat3":
            case "mat4":
                // These are read in the float2/3/4 nodes
                break;

            case "children":
                // Child nodes are handled in the "node" case
                break;

            default:
                throw new InvalidFormatException($"Unknown element encountered: {reader.Name}");
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

            // Value nodes, processed in ReadElement()
            case "float2":
            case "float3":
            case "float4":
            case "mat2":
            case "mat3":
            case "mat4":
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
        stack = [];
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
