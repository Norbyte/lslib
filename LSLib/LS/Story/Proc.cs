namespace LSLib.LS.Story;

public class ProcNode : DataNode
{
    public override Type NodeType()
    {
        return Type.Proc;
    }

    public override string TypeName()
    {
        return "Proc";
    }

    public override void MakeScript(TextWriter writer, Story story, Tuple tuple, bool printTypes)
    {
        writer.Write("{0}(", Name);
        tuple.MakeScript(writer, story, true);
        writer.WriteLine(")");
    }
}
