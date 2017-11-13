using System.IO;
using System.Xml;

namespace LSLib.LS
{
    public class LSXWriter
    {
        private Stream stream;
        private XmlWriter writer;
        public bool PrettyPrint = false;

        public LSXWriter(Stream stream)
        {
            this.stream = stream;
        }

        public void Write(Resource rsrc)
        {
            var settings = new XmlWriterSettings();
            settings.Indent = PrettyPrint;
            settings.IndentChars = "\t";

            using (this.writer = XmlWriter.Create(stream, settings))
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

        private void WriteTranslatedFSString(TranslatedFSString fs)
        {
            writer.WriteStartElement("string");
            writer.WriteAttributeString("value", fs.Value);
            WriteTranslatedFSStringInner(fs);
            writer.WriteEndElement();
        }

        private void WriteTranslatedFSStringInner(TranslatedFSString fs)
        {
            writer.WriteAttributeString("handle", fs.Handle);
            writer.WriteAttributeString("arguments", fs.Arguments.Count.ToString());

            if (fs.Arguments.Count > 0)
            {
                writer.WriteStartElement("arguments");
                for (int i = 0; i < fs.Arguments.Count; i++)
                {
                    var argument = fs.Arguments[i];
                    writer.WriteStartElement("argument");
                    writer.WriteAttributeString("key", argument.Key);
                    writer.WriteAttributeString("value", argument.Value);
                    WriteTranslatedFSString(argument.String);
                    writer.WriteEndElement();
                }
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
                {
                    var ts = ((TranslatedString)attribute.Value.Value);
                    writer.WriteAttributeString("handle", ts.Handle);
                }
                else if (attribute.Value.Type == NodeAttribute.DataType.DT_TranslatedFSString)
                {
                    var fs = ((TranslatedFSString)attribute.Value.Value);
                    WriteTranslatedFSStringInner(fs);
                }
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
