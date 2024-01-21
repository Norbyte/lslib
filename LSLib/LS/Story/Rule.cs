namespace LSLib.LS.Story;

public enum RuleType
{
    Rule,
    Proc,
    Query
};

public class RuleNode : RelNode
{
    public List<Call> Calls;
    public List<Variable> Variables;
    public UInt32 Line;
    public GoalReference DerivedGoalRef;
    public bool IsQuery;

    public override void Read(OsiReader reader)
    {
        base.Read(reader);
        Calls = reader.ReadList<Call>();

        Variables = new List<Variable>();
        var variables = reader.ReadByte();
        while (variables-- > 0)
        {
            var type = reader.ReadByte();
            if (type != 1) throw new InvalidDataException("Illegal value type in rule variable list");
            var variable = new Variable();
            variable.Read(reader);
            if (variable.Adapted)
            {
                variable.VariableName = String.Format("_Var{0}", Variables.Count + 1);
            }

            Variables.Add(variable);
        }

        Line = reader.ReadUInt32();

        if (reader.Ver >= OsiVersion.VerAddQuery)
            IsQuery = reader.ReadBoolean();
        else
            IsQuery = false;
    }

    public override void Write(OsiWriter writer)
    {
        base.Write(writer);
        writer.WriteList<Call>(Calls);

        writer.Write((byte)Variables.Count);
        foreach (var variable in Variables)
        {
            writer.Write((byte)1);
            variable.Write(writer);
        }

        writer.Write(Line);
        if (writer.Ver >= OsiVersion.VerAddQuery)
            writer.Write(IsQuery);
    }

    public override Type NodeType()
    {
        return Type.Rule;
    }

    public override string TypeName()
    {
        if (IsQuery)
            return "Query Rule";
        else
            return "Rule";
    }

    public override void DebugDump(TextWriter writer, Story story)
    {
        base.DebugDump(writer, story);

        writer.WriteLine("    Variables: ");
        foreach (var v in Variables)
        {
            writer.Write("        ");
            v.DebugDump(writer, story);
            writer.WriteLine("");
        }

        writer.WriteLine("    Calls: ");
        foreach (var call in Calls)
        {
            writer.Write("        ");
            call.DebugDump(writer, story);
            writer.WriteLine("");
        }
    }

    public Node GetRoot(Story story)
    {
        Node parent = this;
        for (;;)
        {
            if (parent is RelNode)
            {
                var rel = parent as RelNode;
                parent = rel.ParentRef.Resolve();
            }
            else if (parent is JoinNode)
            {
                var join = parent as JoinNode;
                parent = join.LeftParentRef.Resolve();
            }
            else
            {
                return parent;
            }
        }
    }

    public RuleType? GetRuleType(Story story)
    {
        var root = GetRoot(story);
        if (root is DatabaseNode)
        {
            return RuleType.Rule;
        }
        else if (root is ProcNode)
        {
            var querySig = root.Name + "__DEF__/" + root.NumParams.ToString();
            var sig = root.Name + "/" + root.NumParams.ToString();

            if (!story.FunctionSignatureMap.TryGetValue(querySig, out Function func)
                && !story.FunctionSignatureMap.TryGetValue(sig, out func))
            {
                return null;
            }

            switch (func.Type)
            {
                case FunctionType.Event:
                    return RuleType.Rule;

                case FunctionType.Proc:
                    return RuleType.Proc;

                case FunctionType.UserQuery:
                    return RuleType.Query;

                default:
                    throw new InvalidDataException($"Unsupported root function type: {func.Type}");
            }
        }
        else
        {
            throw new InvalidDataException("Cannot export rules with this root node");
        }
    }

    public Tuple MakeInitialTuple()
    {
        var tuple = new Tuple();
        for (int i = 0; i < Variables.Count; i++)
        {
            tuple.Physical.Add(Variables[i]);
            tuple.Logical.Add(i, Variables[i]);
        }

        return tuple;
    }

    public override void MakeScript(TextWriter writer, Story story, Tuple tuple, bool printTypes)
    {
        var ruleType = GetRuleType(story);
        if (ruleType == null)
        {
            return;
        }

        switch (ruleType)
        {
            case RuleType.Proc: writer.WriteLine("PROC"); break;
            case RuleType.Query: writer.WriteLine("QRY"); break;
            case RuleType.Rule: writer.WriteLine("IF"); break;
        }

        var initialTuple = MakeInitialTuple();
        if (AdapterRef.IsValid)
        {
            var adapter = AdapterRef.Resolve();
            initialTuple = adapter.Adapt(initialTuple);
        }

        printTypes = printTypes || ruleType == RuleType.Proc || ruleType == RuleType.Query;
        ParentRef.Resolve().MakeScript(writer, story, initialTuple, printTypes);
        writer.WriteLine("THEN");
        foreach (var call in Calls)
        {
            call.MakeScript(writer, story, initialTuple, false);
            writer.WriteLine(";");
        }
    }

    private void RemoveQueryPostfix(Story story)
    {
        // Remove the __DEF__ postfix that is added to the end of Query nodes
        if (IsQuery)
        {
            var ruleRoot = GetRoot(story);
            if (ruleRoot.Name != null &&
                ruleRoot.Name.Length > 7 &&
                ruleRoot.Name.Substring(ruleRoot.Name.Length - 7) == "__DEF__")
            {
                ruleRoot.Name = ruleRoot.Name.Substring(0, ruleRoot.Name.Length - 7);
            }
        }
    }

    public override void PostLoad(Story story)
    {
        base.PostLoad(story);
        RemoveQueryPostfix(story);
    }

    public override void PreSave(Story story)
    {
        base.PreSave(story);

        // Re-add the __DEF__ postfix that is added to the end of Query nodes
        if (IsQuery)
        {
            var ruleRoot = GetRoot(story);
            if (ruleRoot.Name != null &&
                (ruleRoot.Name.Length < 7 ||
                ruleRoot.Name.Substring(ruleRoot.Name.Length - 7) != "__DEF__"))
            {
                ruleRoot.Name += "__DEF__";
            }
        }
    }

    public override void PostSave(Story story)
    {
        base.PostSave(story);
        RemoveQueryPostfix(story);
    }
}
