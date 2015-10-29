using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace LSLib.LS
{
    public class LSXWriter : IDisposable
    {
        private Stream stream;
        private XmlWriter writer;

        public LSXWriter(Stream stream)
        {
            this.stream = stream;
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        public void Write(Resource rsrc)
        {
            using (this.writer = XmlWriter.Create(stream))
            {
                writer.WriteStartElement("save");

                writer.WriteStartElement("header");
                writer.WriteAttributeString("version", LSXReader.CurrentVersion);
                writer.WriteAttributeString("time", rsrc.Metadata.timestamp.ToString());
                writer.WriteEndElement();

                writer.WriteStartElement("version");
                writer.WriteAttributeString("major", rsrc.Metadata.majorVersion.ToString());
                writer.WriteAttributeString("minor", rsrc.Metadata.minorVersion.ToString());
                writer.WriteAttributeString("revision", rsrc.Metadata.revision.ToString());
                writer.WriteAttributeString("build", rsrc.Metadata.buildNumber.ToString());
                writer.WriteEndElement();

                WriteRegions(rsrc);

                writer.WriteEndElement();
                writer.Flush();
            }
        }

        private void WriteRegions(Resource rsrc)
        {
            foreach (var region in rsrc.Regions)
            {
                writer.WriteStartElement("region");
                writer.WriteAttributeString("id", region.Key);
                WriteNode(region.Value);
                writer.WriteEndElement();
            }
        }

        private void WriteNode(Node node)
        {
            writer.WriteStartElement("node");
            writer.WriteAttributeString("id", node.Name);

            foreach (var attribute in node.Attributes)
            {
                writer.WriteStartElement("attribute");
                writer.WriteAttributeString("id", attribute.Key);
                writer.WriteAttributeString("value", attribute.Value.ToString());
                writer.WriteAttributeString("type", ((int)attribute.Value.Type).ToString());
                if (attribute.Value.Type == NodeAttribute.DataType.DT_TranslatedString)
                    writer.WriteAttributeString("handle", ((TranslatedString)attribute.Value.Value).Handle);
                writer.WriteEndElement();
            }

            if (node.ChildCount > 0)
            {
                writer.WriteStartElement("children");
                foreach (var children in node.Children)
                {
                    foreach (var child in children.Value)
                        WriteNode(child);
                }
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }
}
