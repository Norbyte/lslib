namespace LSLib.LS.Story;

public abstract class Node : OsirisSerializable
{
    public enum Type : byte
    {
        Database = 1,
        Proc = 2,
        DivQuery = 3,
        And = 4,
        NotAnd = 5,
        RelOp = 6,
        Rule = 7,
        InternalQuery = 8,
        UserQuery = 9
    };

    public UInt32 Index;
    public DatabaseReference DatabaseRef;
    public string Name;
    public byte NumParams;

    public virtual void Read(OsiReader reader)
    {
        DatabaseRef = reader.ReadDatabaseRef();
        Name = reader.ReadString();
        if (Name.Length > 0)
        {
            NumParams = reader.ReadByte();
        }
    }

    public virtual void Write(OsiWriter writer)
    {
        DatabaseRef.Write(writer);
        writer.Write(Name);
        if (Name.Length > 0)
            writer.Write(NumParams);
    }

    public abstract Type NodeType();

    public abstract string TypeName();

    public abstract void MakeScript(TextWriter writer, Story story, Tuple tuple, bool printTypes = false);

    public virtual void PostLoad(Story story)
    {
        if (DatabaseRef.IsValid)
        {
            var database = DatabaseRef.Resolve();
            if (database.OwnerNode != null)
            {
                throw new InvalidDataException("A database cannot be assigned to multiple database nodes!");
            }

            database.OwnerNode = this;
        }
    }

    public virtual void PreSave(Story story)
    {
    }

    public virtual void PostSave(Story story)
    {
    }

    public virtual void DebugDump(TextWriter writer, Story story)
    {
        if (Name.Length > 0)
        {
            writer.Write("{0}({1}): ", Name, NumParams);
        }

        writer.Write("<{0}>", TypeName());
        if (DatabaseRef.IsValid)
        {
            writer.Write(", Database ");
            DatabaseRef.DebugDump(writer, story);
        }

        writer.WriteLine();
    }
}


public abstract class TreeNode : Node
{
    public NodeEntryItem NextNode;

    public override void Read(OsiReader reader)
    {
        base.Read(reader);
        NextNode = new NodeEntryItem();
        NextNode.Read(reader);
    }

    public override void Write(OsiWriter writer)
    {
        base.Write(writer);
        NextNode.Write(writer);
    }

    public override void PostLoad(Story story)
    {
        base.PostLoad(story);

        if (NextNode.NodeRef.IsValid)
        {
            var nextNode = NextNode.NodeRef.Resolve();
            if (nextNode is RuleNode)
            {
                (nextNode as RuleNode).DerivedGoalRef = new GoalReference(story, NextNode.GoalRef.Index);
            }
        }
    }

    public override void DebugDump(TextWriter writer, Story story)
    {
        base.DebugDump(writer, story);

        writer.Write("    Next: ");
        NextNode.DebugDump(writer, story);
        writer.WriteLine("");
    }
}
