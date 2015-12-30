using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS.Story
{
    abstract public class JoinNode : TreeNode
    {
        public NodeRef LeftParentRef;
        public NodeRef RightParentRef;
        public AdapterRef Adapter1Ref;
        public AdapterRef Adapter2Ref;
        public DatabaseRef Database1Ref;
        public byte Database1Flag;
        public NodeEntryItem Database1;
        public DatabaseRef Database2Ref;
        public byte Database2Flag;
        public NodeEntryItem Database2;

        public override void Read(OsiReader reader)
        {
            base.Read(reader);
            LeftParentRef = reader.ReadNodeRef();
            RightParentRef = reader.ReadNodeRef();
            Adapter1Ref = reader.ReadAdapterRef();
            Adapter2Ref = reader.ReadAdapterRef();

            Database1Ref = reader.ReadDatabaseRef();
            Database1 = new NodeEntryItem();
            Database1.Read(reader);
            Database1Flag = reader.ReadByte();

            Database2Ref = reader.ReadDatabaseRef();
            Database2 = new NodeEntryItem();
            Database2.Read(reader);
            Database2Flag = reader.ReadByte();
        }

        public override void DebugDump(TextWriter writer, Story story)
        {
            base.DebugDump(writer, story);

            writer.Write("    Left:");
            if (LeftParentRef.IsValid())
            {
                writer.Write(" Parent ");
                LeftParentRef.DebugDump(writer, story);
            }

            if (Adapter1Ref.IsValid())
            {
                writer.Write(" Adapter ");
                Adapter1Ref.DebugDump(writer, story);
            }

            if (Database1Ref.IsValid())
            {
                writer.Write(" Database ");
                Database1Ref.DebugDump(writer, story);
                writer.Write(" Flag {0}", Database1Flag);
                writer.Write(" Entry ");
                Database1.DebugDump(writer, story);
            }

            writer.WriteLine("");

            writer.Write("    Right:");
            if (RightParentRef.IsValid())
            {
                writer.Write(" Parent ");
                RightParentRef.DebugDump(writer, story);
            }

            if (Adapter2Ref.IsValid())
            {
                writer.Write(" Adapter ");
                Adapter2Ref.DebugDump(writer, story);
            }

            if (Database2Ref.IsValid())
            {
                writer.Write(" Database ");
                Database2Ref.DebugDump(writer, story);
                writer.Write(" Flag {0}", Database2Flag);
                writer.Write(" Entry ");
                Database2.DebugDump(writer, story);
            }

            writer.WriteLine("");
        }
    }

    public class AndNode : JoinNode
    {
        public override string TypeName()
        {
            return "And";
        }

        public override void MakeScript(TextWriter writer, Story story, Tuple tuple)
        {
            var leftTuple = story.Adapters[Adapter1Ref.AdapterIndex].Adapt(tuple);
            story.Nodes[LeftParentRef.NodeIndex].MakeScript(writer, story, leftTuple);
            writer.WriteLine("AND");
            var rightTuple = story.Adapters[Adapter2Ref.AdapterIndex].Adapt(tuple);
            story.Nodes[RightParentRef.NodeIndex].MakeScript(writer, story, rightTuple);
        }
    }

    public class NotAndNode : JoinNode
    {
        public override string TypeName()
        {
            return "Not And";
        }

        public override void MakeScript(TextWriter writer, Story story, Tuple tuple)
        {
            var leftTuple = story.Adapters[Adapter1Ref.AdapterIndex].Adapt(tuple);
            story.Nodes[LeftParentRef.NodeIndex].MakeScript(writer, story, leftTuple);
            writer.WriteLine("AND NOT");
            var rightTuple = story.Adapters[Adapter2Ref.AdapterIndex].Adapt(tuple);
            story.Nodes[RightParentRef.NodeIndex].MakeScript(writer, story, rightTuple);
        }
    }
}
