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
        public const string InitialVersion = "1";
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

        public Resource Read()
        {
            using (this.reader = XmlReader.Create(stream))
            {
                Resource rsrc = new Resource();
                Region currentRegion = null;
                List<Node> stack = new List<Node>();

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
                                if (version != InitialVersion && version != CurrentVersion)
                                    throw new InvalidFormatException(String.Format("Unsupported LSX version; expected {0}, found {1}", CurrentVersion, version));
                                
                                rsrc.Metadata.timestamp = Convert.ToUInt64(reader["time"]);
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
                                    stack.Add(node);
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
                                    var ts = ((TranslatedString)attr.Value);
                                    ts.Handle = reader["handle"];
                                    Debug.Assert(ts.Handle != null);
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
                                stack.RemoveAt(stack.Count - 1);
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
