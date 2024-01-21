namespace LSLib.LS.Story;

public abstract class OsiReference<T> : OsirisSerializable
{
    public const UInt32 NullReference = 0;
    // TODO: hide!
    public UInt32 Index = NullReference;
    protected Story Story;

    public bool IsNull
    {
        get { return Index == NullReference; }
    }

    public bool IsValid
    {
        get { return Index != NullReference; }
    }

    public OsiReference()
    {
    }

    public OsiReference(Story story, UInt32 reference)
    {
        Story = story;
        Index = reference;
    }

    public void BindStory(Story story)
    {
        if (Story == null)
            Story = story;
        else
            throw new InvalidOperationException("Reference already bound to a story!");
    }

    public void Read(OsiReader reader)
    {
        Index = reader.ReadUInt32();
    }

    public void Write(OsiWriter writer)
    {
        writer.Write(Index);
    }

    abstract public T Resolve();

    abstract public void DebugDump(TextWriter writer, Story story);
}

public class NodeReference : OsiReference<Node>
{
    public NodeReference()
        : base()
    {
    }

    public NodeReference(Story story, UInt32 reference)
        : base(story, reference)
    {
    }

    public NodeReference(Story story, Node reference)
        : base(story, reference == null ? NullReference : reference.Index)
    {
    }

    public override Node Resolve()
    {
        if (Index == NullReference)
            return null;
        else
            return Story.Nodes[Index];
    }

    public override void DebugDump(TextWriter writer, Story story)
    {
        if (!IsValid)
        {
            writer.Write("(None)");
        }
        else
        {
            var node = Resolve();
            if (node.Name.Length > 0)
            {
                writer.Write("#{0} <{1}({2}) {3}>", Index, node.Name, node.NumParams, node.TypeName());
            }
            else
            {
                writer.Write("#{0} <{1}>", Index, node.TypeName());
            }
        }
    }
}

public class AdapterReference : OsiReference<Adapter>
{
    public AdapterReference()
        : base()
    {
    }

    public AdapterReference(Story story, UInt32 reference)
        : base(story, reference)
    {
    }

    public AdapterReference(Story story, Adapter reference)
        : base(story, reference.Index)
    {
    }

    public override Adapter Resolve()
    {
        if (Index == NullReference)
            return null;
        else
            return Story.Adapters[Index];
    }

    public override void DebugDump(TextWriter writer, Story story)
    {
        if (!IsValid)
        {
            writer.Write("(None)");
        }
        else
        {
            writer.Write("#{0}", Index);
        }
    }
}

public class DatabaseReference : OsiReference<Database>
{
    public DatabaseReference()
        : base()
    {
    }

    public DatabaseReference(Story story, UInt32 reference)
        : base(story, reference)
    {
    }

    public DatabaseReference(Story story, Database reference)
        : base(story, reference.Index)
    {
    }

    public override Database Resolve()
    {
        if (Index == NullReference)
            return null;
        else
            return Story.Databases[Index];
    }

    public override void DebugDump(TextWriter writer, Story story)
    {
        if (!IsValid)
        {
            writer.Write("(None)");
        }
        else
        {
            writer.Write("#{0}", Index);
        }
    }
}

public class GoalReference : OsiReference<Goal>
{
    public GoalReference()
        : base()
    {
    }

    public GoalReference(Story story, UInt32 reference)
        : base(story, reference)
    {
    }

    public GoalReference(Story story, Goal reference)
        : base(story, reference.Index)
    {
    }

    public override Goal Resolve()
    {
        if (Index == NullReference)
            return null;
        else
            return Story.Goals[Index];
    }

    public override void DebugDump(TextWriter writer, Story story)
    {
        if (!IsValid)
        {
            writer.Write("(None)");
        }
        else
        {
            var goal = Resolve();
            writer.Write("#{0} <{1}>", Index, goal.Name);
        }
    }
}
