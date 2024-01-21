namespace LSLib.LS.Story;

public abstract class RelNode : TreeNode
{
    public NodeReference ParentRef;
    public AdapterReference AdapterRef;
    public NodeReference RelDatabaseNodeRef;
    public NodeEntryItem RelJoin;
    public byte RelDatabaseIndirection;

    public override void Read(OsiReader reader)
    {
        base.Read(reader);
        ParentRef = reader.ReadNodeRef();
        AdapterRef = reader.ReadAdapterRef();

        RelDatabaseNodeRef = reader.ReadNodeRef();
        RelJoin = new NodeEntryItem();
        RelJoin.Read(reader);
        RelDatabaseIndirection = reader.ReadByte();
    }

    public override void Write(OsiWriter writer)
    {
        base.Write(writer);
        ParentRef.Write(writer);
        AdapterRef.Write(writer);

        RelDatabaseNodeRef.Write(writer);
        RelJoin.Write(writer);
        writer.Write(RelDatabaseIndirection);
    }

    public override void PostLoad(Story story)
    {
        base.PostLoad(story);

        if (AdapterRef.IsValid)
        {
            var adapter = AdapterRef.Resolve();
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

        writer.Write("   ");
        if (ParentRef.IsValid)
        {
            writer.Write(" Parent ");
            ParentRef.DebugDump(writer, story);
        }

        if (AdapterRef.IsValid)
        {
            writer.Write(" Adapter ");
            AdapterRef.DebugDump(writer, story);
        }

        if (RelDatabaseNodeRef.IsValid)
        {
            writer.Write(" DbNode ");
            RelDatabaseNodeRef.DebugDump(writer, story);
            writer.Write(" Indirection {0}", RelDatabaseIndirection);
            writer.Write(" Join ");
            RelJoin.DebugDump(writer, story);
        }

        writer.WriteLine("");
    }
}
