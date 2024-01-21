namespace LSLib.LS.Story;

public enum RelOpType : byte
{
    Less = 0,
    LessOrEqual = 1,
    Greater = 2,
    GreaterOrEqual = 3,
    Equal = 4,
    NotEqual = 5
};

public class RelOpNode : RelNode
{
    public sbyte LeftValueIndex;
    public sbyte RightValueIndex;
    public Value LeftValue;
    public Value RightValue;
    public RelOpType RelOp;

    public override void Read(OsiReader reader)
    {
        base.Read(reader);
        LeftValueIndex = reader.ReadSByte();
        RightValueIndex = reader.ReadSByte();

        LeftValue = new Value();
        LeftValue.Read(reader);

        RightValue = new Value();
        RightValue.Read(reader);

        RelOp = (RelOpType)reader.ReadInt32();
    }

    public override void Write(OsiWriter writer)
    {
        base.Write(writer);
        writer.Write(LeftValueIndex);
        writer.Write(RightValueIndex);

        LeftValue.Write(writer);
        RightValue.Write(writer);
        writer.Write((UInt32)RelOp);
    }

    public override Type NodeType()
    {
        return Type.RelOp;
    }

    public override string TypeName()
    {
        return String.Format("RelOp {0}", RelOp);
    }

    public override void DebugDump(TextWriter writer, Story story)
    {
        base.DebugDump(writer, story);

        writer.Write("    Left Value: ");
        if (LeftValueIndex != -1)
            writer.Write("[Source Column {0}]", LeftValueIndex);
        else
            LeftValue.DebugDump(writer, story);
        writer.WriteLine();

        writer.Write("    Right Value: ");
        if (RightValueIndex != -1)
            writer.Write("[Source Column {0}]", RightValueIndex);
        else
            RightValue.DebugDump(writer, story);
        writer.WriteLine();
    }

    public override void MakeScript(TextWriter writer, Story story, Tuple tuple, bool printTypes)
    {
        var adaptedTuple = AdapterRef.Resolve().Adapt(tuple);
        ParentRef.Resolve().MakeScript(writer, story, adaptedTuple, printTypes);
        writer.WriteLine("AND");

        if (LeftValueIndex != -1)
            adaptedTuple.Logical[LeftValueIndex].MakeScript(writer, story, tuple);
        else
            LeftValue.MakeScript(writer, story, tuple);

        switch (RelOp)
        {
            case RelOpType.Less: writer.Write(" < "); break;
            case RelOpType.LessOrEqual: writer.Write(" <= "); break;
            case RelOpType.Greater: writer.Write(" > "); break;
            case RelOpType.GreaterOrEqual: writer.Write(" >= "); break;
            case RelOpType.Equal: writer.Write(" == "); break;
            case RelOpType.NotEqual: writer.Write(" != "); break;
        }

        if (RightValueIndex != -1)
            adaptedTuple.Logical[RightValueIndex].MakeScript(writer, story, tuple);
        else
            RightValue.MakeScript(writer, story, tuple);
        writer.WriteLine();
    }
}
