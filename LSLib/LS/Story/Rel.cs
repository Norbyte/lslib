using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS.Story
{
    abstract public class RelNode : TreeNode
    {
        public NodeRef ParentRef;
        public AdapterRef AdapterRef;
        public DatabaseRef RelDatabaseRef;
        public NodeEntryItem RelDatabase;
        public byte RelDatabaseFlag;

        public override void Read(OsiReader reader)
        {
            base.Read(reader);
            ParentRef = reader.ReadNodeRef();
            AdapterRef = reader.ReadAdapterRef();

            RelDatabaseRef = reader.ReadDatabaseRef();
            RelDatabase = new NodeEntryItem();
            RelDatabase.Read(reader);
            RelDatabaseFlag = reader.ReadByte();
        }

        public override void Write(OsiWriter writer)
        {
            base.Write(writer);
            ParentRef.Write(writer);
            AdapterRef.Write(writer);

            RelDatabaseRef.Write(writer);
            RelDatabase.Write(writer);
            writer.Write(RelDatabaseFlag);
        }

        public override void DebugDump(TextWriter writer, Story story)
        {
            base.DebugDump(writer, story);

            writer.Write("   ");
            if (ParentRef.IsValid())
            {
                writer.Write(" Parent ");
                ParentRef.DebugDump(writer, story);
            }

            if (AdapterRef.IsValid())
            {
                writer.Write(" Adapter ");
                AdapterRef.DebugDump(writer, story);
            }

            if (RelDatabaseRef.IsValid())
            {
                writer.Write(" Database ");
                RelDatabaseRef.DebugDump(writer, story);
                writer.Write(" Flag {0}", RelDatabaseFlag);
                writer.Write(" Entry ");
                RelDatabase.DebugDump(writer, story);
            }

            writer.WriteLine("");
        }
    }
}
