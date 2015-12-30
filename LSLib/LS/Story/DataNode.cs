using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS.Story
{
    abstract public class DataNode : Node
    {
        public List<NodeEntryItem> ReferencedBy;

        public override void Read(OsiReader reader)
        {
            base.Read(reader);
            ReferencedBy = reader.ReadList<NodeEntryItem>();
        }

        public override void Write(OsiWriter writer)
        {
            base.Write(writer);
            writer.WriteList<NodeEntryItem>(ReferencedBy);
        }

        public override void DebugDump(TextWriter writer, Story story)
        {
            base.DebugDump(writer, story);

            if (ReferencedBy.Count > 0)
            {
                writer.WriteLine("    Referenced By:");
                foreach (var entry in ReferencedBy)
                {
                    writer.Write("        ");
                    entry.DebugDump(writer, story);
                    writer.WriteLine("");
                }
            }
        }
    }
}
