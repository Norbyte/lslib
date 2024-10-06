using LSLib.Stats.Functors;
using QUT.Gppg;

namespace LSLib.Stats.Requirements;

public partial class RequirementScanner
{
    public LexLocation LastLocation()
    {
        return new LexLocation(tokLin, tokCol, tokELin, tokECol);
    }
}

public abstract class RequirementScanBase : AbstractScanner<object, LexLocation>
{
    protected virtual bool yywrap() { return true; }
}

public partial class RequirementParser
{
    public RequirementParser(RequirementScanner scnr) : base(scnr)
    {
    }

    private List<Requirement> MakeRequirements() => new List<Requirement>();

    private List<Requirement> AddRequirement(object requirements, object requirement)
    {
        var req = requirements as List<Requirement>;
        req.Add(requirement as Requirement);
        return req;
    }

    private Requirement MakeNotRequirement(object requirement)
    {
        var req = requirement as Requirement;
        req.Not = true;
        return req;
    }

    private Requirement MakeRequirement(object name)
    {
        return new Requirement
        {
            Not = false,
            RequirementName = name as string,
            IntParam = 0,
            TagParam = ""
        };
    }

    private Requirement MakeIntRequirement(object name, object intArg)
    {
        var reqmtName = name as string;

        /*if (!RequirementsWithArgument.ValueToIndexMap.ContainsKey(reqmtName))
        {
            OnError?.Invoke($"Requirement '{reqmtName}' doesn't need any arguments");
        }*/

        return new Requirement
        {
            Not = false,
            RequirementName = reqmtName,
            IntParam = Int32.Parse(intArg as string),
            TagParam = ""
        };
    }
}
