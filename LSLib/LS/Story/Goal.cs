namespace LSLib.LS.Story;

public class Goal : OsirisSerializable
{
    public UInt32 Index;
    public string Name;
    public byte SubGoalCombination;
    public List<GoalReference> ParentGoals;
    public List<GoalReference> SubGoals;
    public byte Flags; // 0x02 = Child goal
    public List<Call> InitCalls;
    public List<Call> ExitCalls;
    public Story Story;

    public Goal(Story story)
    {
        Story = story;
    }

    public void Read(OsiReader reader)
    {
        Index = reader.ReadUInt32();
        Name = reader.ReadString();
        SubGoalCombination = reader.ReadByte();

        ParentGoals = reader.ReadRefList<GoalReference, Goal>();
        SubGoals = reader.ReadRefList<GoalReference, Goal>();

        Flags = reader.ReadByte();

        if (reader.Ver >= OsiVersion.VerAddInitExitCalls)
        {
            InitCalls = reader.ReadList<Call>();
            ExitCalls = reader.ReadList<Call>();
        }
        else
        {
            InitCalls = new List<Call>();
            ExitCalls = new List<Call>();
        }
    }

    public void Write(OsiWriter writer)
    {
        writer.Write(Index);
        writer.Write(Name);
        writer.Write(SubGoalCombination);

        writer.WriteList<GoalReference>(ParentGoals);
        writer.WriteList<GoalReference>(SubGoals);

        writer.Write(Flags);

        if (writer.Ver >= OsiVersion.VerAddInitExitCalls)
        {
            writer.WriteList<Call>(InitCalls);
            writer.WriteList<Call>(ExitCalls);
        }
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        writer.WriteLine("{0}: SGC {1}, Flags {2}", Name, SubGoalCombination, Flags);

        if (ParentGoals.Count > 0)
        {
            writer.Write("    Parent goals: ");
            foreach (var goalRef in ParentGoals)
            {
                var goal = goalRef.Resolve();
                writer.Write("#{0} {1}, ", goal.Index, goal.Name);
            }
            writer.WriteLine();
        }

        if (SubGoals.Count > 0)
        {
            writer.Write("    Subgoals: ");
            foreach (var goalRef in SubGoals)
            {
                var goal = goalRef.Resolve();
                writer.Write("#{0} {1}, ", goal.Index, goal.Name);
            }
            writer.WriteLine();
        }

        if (InitCalls.Count > 0)
        {
            writer.WriteLine("    Init Calls: ");
            foreach (var call in InitCalls)
            {
                writer.Write("        ");
                call.DebugDump(writer, story);
                writer.WriteLine();
            }
        }

        if (ExitCalls.Count > 0)
        {
            writer.WriteLine("    Exit Calls: ");
            foreach (var call in ExitCalls)
            {
                writer.Write("        ");
                call.DebugDump(writer, story);
                writer.WriteLine();
            }
        }
    }

    public void MakeScript(TextWriter writer, Story story)
    {
        writer.WriteLine("Version 1");
        writer.WriteLine("SubGoalCombiner SGC_AND");
        writer.WriteLine();
        writer.WriteLine("INITSECTION");

        var nullTuple = new Tuple();
        foreach (var call in InitCalls)
        {
            call.MakeScript(writer, story, nullTuple, false);
            writer.WriteLine(";");
        }

        writer.WriteLine();
        writer.WriteLine("KBSECTION");

        foreach (var node in story.Nodes)
        {
            if (node.Value is RuleNode)
            {
                var rule = node.Value as RuleNode;
                if (rule.DerivedGoalRef != null && rule.DerivedGoalRef.Index == Index)
                {
                    node.Value.MakeScript(writer, story, nullTuple, false);
                    writer.WriteLine();
                }
            }
        }

        writer.WriteLine();
        writer.WriteLine("EXITSECTION");

        foreach (var call in ExitCalls)
        {
            call.MakeScript(writer, story, nullTuple, false);
            writer.WriteLine(";");
        }

        writer.WriteLine("ENDEXITSECTION");
        writer.WriteLine();

        foreach (var goalRef in ParentGoals)
        {
            var goal = goalRef.Resolve();
            writer.WriteLine("ParentTargetEdge \"{0}\"", goal.Name);
        }
    }
}
