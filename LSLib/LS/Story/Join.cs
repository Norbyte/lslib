namespace LSLib.LS.Story;

public abstract class JoinNode : TreeNode
{
    public NodeReference LeftParentRef;
    public NodeReference RightParentRef;
    public AdapterReference LeftAdapterRef;
    public AdapterReference RightAdapterRef;
    public NodeReference LeftDatabaseNodeRef;
    public byte LeftDatabaseIndirection;
    public NodeEntryItem LeftDatabaseJoin;
    public NodeReference RightDatabaseNodeRef;
    public byte RightDatabaseIndirection;
    public NodeEntryItem RightDatabaseJoin;

    public override void Read(OsiReader reader)
    {
        base.Read(reader);
        LeftParentRef = reader.ReadNodeRef();
        RightParentRef = reader.ReadNodeRef();
        LeftAdapterRef = reader.ReadAdapterRef();
        RightAdapterRef = reader.ReadAdapterRef();

        LeftDatabaseNodeRef = reader.ReadNodeRef();
        LeftDatabaseJoin = new NodeEntryItem();
        LeftDatabaseJoin.Read(reader);
        LeftDatabaseIndirection = reader.ReadByte();

        RightDatabaseNodeRef = reader.ReadNodeRef();
        RightDatabaseJoin = new NodeEntryItem();
        RightDatabaseJoin.Read(reader);
        RightDatabaseIndirection = reader.ReadByte();
    }

    public override void Write(OsiWriter writer)
    {
        base.Write(writer);
        LeftParentRef.Write(writer);
        RightParentRef.Write(writer);
        LeftAdapterRef.Write(writer);
        RightAdapterRef.Write(writer);

        LeftDatabaseNodeRef.Write(writer);
        LeftDatabaseJoin.Write(writer);
        writer.Write(LeftDatabaseIndirection);

        RightDatabaseNodeRef.Write(writer);
        RightDatabaseJoin.Write(writer);
        writer.Write(RightDatabaseIndirection);
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

        if (LeftDatabaseNodeRef.IsValid)
        {
            writer.Write(" DbNode ");
            LeftDatabaseNodeRef.DebugDump(writer, story);
            writer.Write(" Indirection {0}", LeftDatabaseIndirection);
            writer.Write(" Join ");
            LeftDatabaseJoin.DebugDump(writer, story);
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

        if (RightDatabaseNodeRef.IsValid)
        {
            writer.Write(" DbNode ");
            RightDatabaseNodeRef.DebugDump(writer, story);
            writer.Write(" Indirection {0}", RightDatabaseIndirection);
            writer.Write(" Join ");
            RightDatabaseJoin.DebugDump(writer, story);
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

    public override void MakeScript(TextWriter writer, Story story, Tuple tuple, bool printTypes)
    {
        var leftTuple = LeftAdapterRef.Resolve().Adapt(tuple);
        LeftParentRef.Resolve().MakeScript(writer, story, leftTuple, printTypes);
        writer.WriteLine("AND");
        var rightTuple = RightAdapterRef.Resolve().Adapt(tuple);
        RightParentRef.Resolve().MakeScript(writer, story, rightTuple, false);
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

    public override void MakeScript(TextWriter writer, Story story, Tuple tuple, bool printTypes)
    {
        var leftTuple = LeftAdapterRef.Resolve().Adapt(tuple);
        LeftParentRef.Resolve().MakeScript(writer, story, leftTuple, printTypes);
        writer.WriteLine("AND NOT");
        var rightTuple = RightAdapterRef.Resolve().Adapt(tuple);
        RightParentRef.Resolve().MakeScript(writer, story, rightTuple, false);
    }
}
