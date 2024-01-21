namespace LSLib.LS.Story;

public abstract class QueryNode : Node
{
    public override void MakeScript(TextWriter writer, Story story, Tuple tuple, bool printTypes)
    {
        writer.Write("{0}(", Name);
        tuple.MakeScript(writer, story, printTypes);
        writer.WriteLine(")");
    }
}

public class DivQueryNode : QueryNode
{
    public override Type NodeType()
    {
        return Type.DivQuery;
    }

    public override string TypeName()
    {
        return "Div Query";
    }
}

public class InternalQueryNode : QueryNode
{
    public override Type NodeType()
    {
        return Type.InternalQuery;
    }

    public override string TypeName()
    {
        return "Internal Query";
    }
}

public class UserQueryNode : QueryNode
{
    public override Type NodeType()
    {
        return Type.UserQuery;
    }

    public override string TypeName()
    {
        return "User Query";
    }
}
