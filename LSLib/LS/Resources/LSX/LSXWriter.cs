using LSLib.LS.Enums;
using System.Xml;

namespace LSLib.LS;

public class LSXWriter(Stream stream)
{
    private readonly Stream stream = stream;
    private XmlWriter writer;

    public bool PrettyPrint = false;
    public LSXVersion Version = LSXVersion.V3;
    public NodeSerializationSettings SerializationSettings = new();

    private XmlWriter PrepareWrite(uint? majorVersion)
    {
        if (Version == LSXVersion.V3 && majorVersion != null && majorVersion == 4)
        {
            throw new InvalidDataException("Cannot resave a BG3 (v4.x) resource in D:OS2 (v3.x) file format, maybe you have the wrong game selected?");
        }

        var settings = new XmlWriterSettings
        {
            Indent = PrettyPrint,
            IndentChars = "\t"
        };

        return XmlWriter.Create(stream, settings);
    }

    public void Write(Resource rsrc)
    {
        using (this.writer = PrepareWrite(rsrc.Metadata.MajorVersion))
        {
            writer.WriteStartElement("save");

            writer.WriteStartElement("version");

            writer.WriteAttributeString("major", rsrc.Metadata.MajorVersion.ToString());
            writer.WriteAttributeString("minor", rsrc.Metadata.MinorVersion.ToString());
            writer.WriteAttributeString("revision", rsrc.Metadata.Revision.ToString());
            writer.WriteAttributeString("build", rsrc.Metadata.BuildNumber.ToString());
            writer.WriteAttributeString("lslib_meta", SerializationSettings.BuildMeta());
            writer.WriteEndElement();

            WriteRegions(rsrc);

            writer.WriteEndElement();
        }
    }

    public void Write(Node node)
    {
        using (this.writer = PrepareWrite(null))
        {
            WriteNode(node);
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
            if (Version >= LSXVersion.V4)
            {
                writer.WriteAttributeString("type", AttributeTypeMaps.IdToType[attribute.Value.Type]);
            }
            else
            {
                writer.WriteAttributeString("type", ((int)attribute.Value.Type).ToString());
            }

            if (attribute.Value.Type == AttributeType.TranslatedString)
            {
                var ts = ((TranslatedString)attribute.Value.Value);
                writer.WriteAttributeString("handle", ts.Handle);
                if (ts.Value != null)
                {
                    writer.WriteAttributeString("value", ts.ToString());
                }
                else
                {
                    writer.WriteAttributeString("version", ts.Version.ToString());
                }
            }
            else if (attribute.Value.Type == AttributeType.TranslatedFSString)
            {
                var fs = ((TranslatedFSString)attribute.Value.Value);
                writer.WriteAttributeString("value", fs.Value);
                WriteTranslatedFSStringInner(fs);
            }
            else
            {
                // Replace bogus 001F characters found in certain LSF nodes
                writer.WriteAttributeString("value", attribute.Value.AsString(SerializationSettings).Replace("\x1f", ""));
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
