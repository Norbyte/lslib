namespace LSLib.LS.Story;

public class DatabaseNode : DataNode
{
    public override Type NodeType()
    {
        return Type.Database;
    }

    public override string TypeName()
    {
        return "Database";
    }

    public override void MakeScript(TextWriter writer, Story story, Tuple tuple, bool printTypes)
    {
        writer.Write("{0}(", Name);
        tuple.MakeScript(writer, story, printTypes);
        writer.WriteLine(")");
    }
}
