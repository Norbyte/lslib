using System.IO;

namespace LSLib.LS.Story
{
    public abstract class JoinNode : TreeNode
    {
        public NodeReference LeftParentRef;
        public NodeReference RightParentRef;
        public AdapterReference LeftAdapterRef;
        public AdapterReference RightAdapterRef;
        public DatabaseReference LeftDatabaseRef;
        public byte LeftDatabaseFlag;
        public NodeEntryItem LeftDatabase;
        public DatabaseReference RightDatabaseRef;
        public byte RightDatabaseFlag;
        public NodeEntryItem RightDatabase;

        public override void Read(OsiReader reader)
        {
            base.Read(reader);
            LeftParentRef = reader.ReadNodeRef();
            RightParentRef = reader.ReadNodeRef();
            LeftAdapterRef = reader.ReadAdapterRef();
            RightAdapterRef = reader.ReadAdapterRef();

            LeftDatabaseRef = reader.ReadDatabaseRef();
            LeftDatabase = new NodeEntryItem();
            LeftDatabase.Read(reader);
            LeftDatabaseFlag = reader.ReadByte();

            RightDatabaseRef = reader.ReadDatabaseRef();
            RightDatabase = new NodeEntryItem();
            RightDatabase.Read(reader);
            RightDatabaseFlag = reader.ReadByte();
        }

        public override void Write(OsiWriter writer)
        {
            base.Write(writer);
            LeftParentRef.Write(writer);
            RightParentRef.Write(writer);
            LeftAdapterRef.Write(writer);
            RightAdapterRef.Write(writer);

            LeftDatabaseRef.Write(writer);
            LeftDatabase.Write(writer);
            writer.Write(LeftDatabaseFlag);

            RightDatabaseRef.Write(writer);
            RightDatabase.Write(writer);
            writer.Write(RightDatabaseFlag);
        }

        public override void PostLoad(Story story)
        {
            base.PostLoad(story);

            if (LeftAdapterRef.IsValid)
            {
                var adapter = LeftAdapterRef.Resolve();
                if (adapter.OwnerNode != null)
                {
                    throw new InvalidDataException("An adapter cannot be assigned to multiple join/rel nodes!");
                }

                adapter.OwnerNode = this;
            }

            if (RightAdapterRef.IsValid)
            {
                var adapter = RightAdapterRef.Resolve();
                if (adapter.OwnerNode != null)
                {
                    throw new InvalidDataException("An adapter cannot be assigned to multiple join/rel nodes!");
                }

                adapter.OwnerNode = this;
            }
        }

        public override void DebugDump(TextWriter writer, Story story)
        {
            base.DebugDump(writer, story);

            writer.Write("    Left:");
            if (LeftParentRef.IsValid)
            {
                writer.Write(" Parent ");
                LeftParentRef.DebugDump(writer, story);
            }

            if (LeftAdapterRef.IsValid)
            {
                writer.Write(" Adapter ");
                LeftAdapterRef.DebugDump(writer, story);
            }

            if (LeftDatabaseRef.IsValid)
            {
                writer.Write(" Database ");
                LeftDatabaseRef.DebugDump(writer, story);
                writer.Write(" Flag {0}", LeftDatabaseFlag);
                writer.Write(" Entry ");
                LeftDatabase.DebugDump(writer, story);
            }

            writer.WriteLine("");

            writer.Write("    Right:");
            if (RightParentRef.IsValid)
            {
                writer.Write(" Parent ");
                RightParentRef.DebugDump(writer, story);
            }

            if (RightAdapterRef.IsValid)
            {
                writer.Write(" Adapter ");
                RightAdapterRef.DebugDump(writer, story);
            }

            if (RightDatabaseRef.IsValid)
            {
                writer.Write(" Database ");
                RightDatabaseRef.DebugDump(writer, story);
                writer.Write(" Flag {0}", RightDatabaseFlag);
                writer.Write(" Entry ");
                RightDatabase.DebugDump(writer, story);
            }

            writer.WriteLine("");
        }
    }

    public class AndNode : JoinNode
    {
        public override Type NodeType()
        {
            return Type.And;
        }

        public override string TypeName()
        {
            return "And";
        }

        public override void MakeScript(TextWriter writer, Story story, Tuple tuple)
        {
            var leftTuple = LeftAdapterRef.Resolve().Adapt(tuple);
            LeftParentRef.Resolve().MakeScript(writer, story, leftTuple);
            writer.WriteLine("AND");
            var rightTuple = RightAdapterRef.Resolve().Adapt(tuple);
            RightParentRef.Resolve().MakeScript(writer, story, rightTuple);
        }
    }

    public class NotAndNode : JoinNode
    {
        public override Type NodeType()
        {
            return Type.NotAnd;
        }

        public override string TypeName()
        {
            return "Not And";
        }

        public override void MakeScript(TextWriter writer, Story story, Tuple tuple)
        {
            var leftTuple = LeftAdapterRef.Resolve().Adapt(tuple);
            LeftParentRef.Resolve().MakeScript(writer, story, leftTuple);
            writer.WriteLine("AND NOT");
            var rightTuple = RightAdapterRef.Resolve().Adapt(tuple);
            RightParentRef.Resolve().MakeScript(writer, story, rightTuple);
        }
    }
}
