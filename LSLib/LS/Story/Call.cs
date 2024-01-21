namespace LSLib.LS.Story;

public class Call : OsirisSerializable
{
    public string Name;
    public List<TypedValue> Parameters;
    public bool Negate;
    public Int32 GoalIdOrDebugHook;

    public void Read(OsiReader reader)
    {
        Name = reader.ReadString();
        if (Name.Length > 0)
        {
            var hasParams = reader.ReadByte();
            if (hasParams > 0)
            {
                Parameters = new List<TypedValue>();
                var numParams = reader.ReadByte();
                while (numParams-- > 0)
                {
                    TypedValue param;
                    var type = reader.ReadByte();
                    if (type == 1)
                        param = new Variable();
                    else
                        param = new TypedValue();
                    param.Read(reader);
                    Parameters.Add(param);
                }
            }

            Negate = reader.ReadBoolean();
        }

        GoalIdOrDebugHook = reader.ReadInt32();
    }

    public void Write(OsiWriter writer)
    {
        writer.Write(Name);
        if (Name.Length > 0)
        {
            writer.Write(Parameters != null);
            if (Parameters != null)
            {
                writer.Write((byte)Parameters.Count);
                foreach (var param in Parameters)
                {
                    writer.Write(param is Variable);
                    param.Write(writer);
                }
            }

            writer.Write(Negate);
        }

        writer.Write(GoalIdOrDebugHook);
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        if (Name.Length > 0)
        {
            if (Negate) writer.Write("!");
            writer.Write("{0}(", Name);
            if (Parameters != null)
            {
                for (var i = 0; i < Parameters.Count; i++)
                {
                    Parameters[i].DebugDump(writer, story);
                    if (i < Parameters.Count - 1) writer.Write(", ");
                }
            }

            writer.Write(") ");
        }

        if (GoalIdOrDebugHook != 0)
        {
            if (GoalIdOrDebugHook < 0)
            {
                writer.Write("<Debug hook #{0}>", -GoalIdOrDebugHook);
            }
            else
            {
                var goal = story.Goals[(uint)GoalIdOrDebugHook];
                writer.Write("<Complete goal #{0} {1}>", GoalIdOrDebugHook, goal.Name);
            }
        }
    }

    public void MakeScript(TextWriter writer, Story story, Tuple tuple, bool printTypes)
    {
        if (Name.Length > 0)
        {
            if (Negate) writer.Write("NOT ");
            writer.Write("{0}(", Name);
            if (Parameters != null)
            {
                for (var i = 0; i < Parameters.Count; i++)
                {
                    var param = Parameters[i];
                    param.MakeScript(writer, story, tuple, printTypes);
                    if (i < Parameters.Count - 1)
                        writer.Write(", ");
                }
            }

            writer.Write(")");
        }

        if (GoalIdOrDebugHook > 0)
        {
            writer.Write("GoalCompleted");
        }
    }
}
