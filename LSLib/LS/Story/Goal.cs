using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS.Story
{
    public class Goal : OsirisSerializable
    {
        public UInt32 Index;
        public string Name;
        public byte SubGoalCombination;
        public List<UInt32> ParentGoals;
        public List<UInt32> SubGoals;
        public byte Unknown; // 0x02 = Child goal
        public List<Call> InitCalls;
        public List<Call> ExitCalls;

        public void Read(OsiReader reader)
        {
            Index = reader.ReadUInt32();
            Name = reader.ReadString();
            SubGoalCombination = reader.ReadByte();

            ParentGoals = new List<uint>();
            var numItems = reader.ReadUInt32();
            while (numItems-- > 0)
            {
                ParentGoals.Add(reader.ReadUInt32());
            }

            SubGoals = new List<uint>();
            numItems = reader.ReadUInt32();
            while (numItems-- > 0)
            {
                SubGoals.Add(reader.ReadUInt32());
            }

            Unknown = reader.ReadByte();

            if (reader.MajorVersion >= 1 && reader.MinorVersion >= 1)
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

        public void DebugDump(TextWriter writer, Story story)
        {
            writer.WriteLine("{0}: SGC {1}, Unknown {2}", Name, SubGoalCombination, Unknown);

            if (ParentGoals.Count > 0)
            {
                writer.Write("    Parent goals: ");
                foreach (var goalId in ParentGoals)
                {
                    var goal = story.Goals[goalId];
                    writer.Write("#{0} {1}, ", goalId, goal.Name);
                }
                writer.WriteLine();
            }

            if (SubGoals.Count > 0)
            {
                writer.Write("    Subgoals: ");
                foreach (var goalId in SubGoals)
                {
                    var goal = story.Goals[goalId];
                    writer.Write("#{0} {1}, ", goalId, goal.Name);
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
                call.MakeScript(writer, story, nullTuple);
                writer.WriteLine();
            }

            writer.WriteLine();
            writer.WriteLine("KBSECTION");

            foreach (var node in story.Nodes)
            {
                if (node.Value is RuleNode)
                {
                    var rule = node.Value as RuleNode;
                    if (rule.DerivedGoalId == Index)
                    {
                        node.Value.MakeScript(writer, story, nullTuple);
                        writer.WriteLine();
                    }
                }
            }

            writer.WriteLine();
            writer.WriteLine("EXITSECTION");

            foreach (var call in ExitCalls)
            {
                call.MakeScript(writer, story, nullTuple);
                writer.WriteLine();
            }

            writer.WriteLine("ENDEXITSECTION");
            writer.WriteLine();

            foreach (var goalId in SubGoals)
            {
                var goal = story.Goals[goalId];
                writer.WriteLine("ParentTargetEdge \"{0}\"", goal.Name);
            }

            foreach (var goalId in ParentGoals)
            {
                var goal = story.Goals[goalId];
                writer.WriteLine("TargetEdge \"{0}\"", goal.Name);
            }
        }
    }
}
