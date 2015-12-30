using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS.Story
{
    public class Fact : OsirisSerializable
    {
        public List<Value> Columns;

        public void Read(OsiReader reader)
        {
            Columns = new List<Value>();
            var count = reader.ReadByte();
            while (count-- > 0)
            {
                var value = new Value();
                value.Read(reader);
                Columns.Add(value);
            }
        }

        public void Write(OsiWriter writer)
        {
            writer.Write((byte)Columns.Count);
            foreach (var column in Columns)
            {
                column.Write(writer);
            }
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            writer.Write("(");
            for (var i = 0; i < Columns.Count; i++)
            {
                Columns[i].DebugDump(writer, story);
                if (i < Columns.Count - 1) writer.Write(", ");
            }
            writer.Write(")");
        }
    }

    public class Database : OsirisSerializable
    {
        public ParameterList Parameters;
        public List<Fact> Facts;
        public Node OwnerNode;
        public long FactsPosition;

        public void Read(OsiReader reader)
        {
            Parameters = new ParameterList();
            Parameters.Read(reader);

            FactsPosition = reader.BaseStream.Position;
            Facts = reader.ReadList<Fact>();
        }

        public void Write(OsiWriter writer)
        {
            Parameters.Write(writer);
            writer.WriteList<Fact>(Facts);
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            if (OwnerNode != null && OwnerNode.Name.Length > 0)
            {
                writer.Write("{0}/{1}", OwnerNode.Name, OwnerNode.NameIndex);
            }
            else if (OwnerNode != null)
            {
                writer.Write("<{0}>", OwnerNode.TypeName());
            }
            else
            {
                writer.Write("(Not owned)");
            }

            writer.Write(" @ {0:X}: ", FactsPosition);
            Parameters.DebugDump(writer, story);

            writer.WriteLine("");
            writer.WriteLine("    Facts: ");
            foreach (var fact in Facts)
            {
                writer.Write("        ");
                fact.DebugDump(writer, story);
                writer.WriteLine();
            }
        }
    }
}
