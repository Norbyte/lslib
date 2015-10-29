using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace LSLib.LS
{
    public class LSXReader : IDisposable
    {
        public const string CurrentVersion = "2";

        private Stream stream;
        private XmlReader reader;

        public LSXReader(Stream stream)
        {
            this.stream = stream;
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        public Resource Read()
        {
            using (this.reader = XmlReader.Create(stream))
            {
                Resource rsrc = new Resource();
                Region currentRegion = null;
                Stack<Node> stack = new Stack<Node>();

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
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
                                string version = reader["version"];
                                if (version != CurrentVersion)
                                    throw new InvalidFormatException(String.Format("Unsupported LSX version; expected {0}, found {1}", CurrentVersion, version));

                                rsrc.Metadata.timestamp = Convert.ToUInt64(reader["timestamp"]);
                                break;

                            case "version":
                                // LSX metadata part 2
                                rsrc.Metadata.majorVersion = Convert.ToUInt32(reader["major"]);
                                rsrc.Metadata.minorVersion = Convert.ToUInt32(reader["minor"]);
                                rsrc.Metadata.revision = Convert.ToUInt32(reader["revision"]);
                                rsrc.Metadata.buildNumber = Convert.ToUInt32(reader["build"]);
                                break;

                            case "region":
                                if (currentRegion != null)
                                    throw new InvalidFormatException("A <region> can only start at the root level of a resource.");

                                Debug.Assert(!reader.IsEmptyElement);
                                var region = new Region();
                                region.RegionName = reader["id"];
                                Debug.Assert(region.RegionName != null);
                                rsrc.Regions.Add(region.RegionName, region);
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
                                    stack.Push(node);
                                break;

                            case "attribute":
                                var attrTypeId = Convert.ToUInt32(reader["type"]);
                                var attrName = reader["id"];
                                var attrValue = reader["value"];
                                if (attrTypeId > (int)NodeAttribute.DataType.DT_Max)
                                    throw new InvalidFormatException(String.Format("Unsupported attribute data type: {0}", attrTypeId));
                                
                                Debug.Assert(attrName != null);
                                Debug.Assert(attrValue != null);
                                var attr = new NodeAttribute((NodeAttribute.DataType)attrTypeId);
                                attr.FromString(attrValue);
                                if (attr.Type == NodeAttribute.DataType.DT_TranslatedString)
                                {
                                    ((TranslatedString)attr.Value).Handle = reader["handle"];
                                    Debug.Assert(((TranslatedString)attr.Value).Handle != null);
                                }

                                stack.Peek().Attributes.Add(attrName, attr);
                                break;

                            case "children":
                                // Child nodes are handled in the "node" case
                                break;

                            default:
                                throw new InvalidFormatException(String.Format("Unknown element encountered: {0}", reader.Name));
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement)
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
                                stack.Pop();
                                break;

                            default:
                                throw new InvalidFormatException(String.Format("Unknown element encountered: {0}", reader.Name));
                        }
                    }
                }

                return rsrc;
            }
        }
    }
}
