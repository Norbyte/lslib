using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSTools.DebuggerFrontend
{
    public enum LineType
    {
        // Line is a single node (i.e. AND, rule, etc.)
        NodeLine,
        // Line is an action in the rule THEN part
        RuleActionLine,
        // Line is an action in the goal INIT section
        GoalInitActionLine,
        // Line is an action in the goal EXIT section
        GoalExitActionLine
    }

    // Node information associated to a line
    public class LineDebugInfo
    {
        // Type of line
        public LineType Type;
        // Node associated to this line
        public NodeDebugInfo Node;
        // Goal associated to this line
        public GoalDebugInfo Goal;
        // Index of action in INIT/EXIT/THEN part
        public UInt32 ActionIndex;
        // Line number
        public UInt32 Line;
    }

    public class GoalLineMap
    {
        public GoalDebugInfo Goal;
        // Line number => Node info mappings
        public Dictionary<UInt32, LineDebugInfo> LineMap;
    }

    public class Breakpoint
    {
        public UInt32 Id;
        public String GoalName;
        public UInt32 Line;
        public LineDebugInfo LineInfo;
        public bool Verified;
        public String ErrorReason;
    }

    public class BreakpointManager
    {
        private DebuggerClient DbgCli;
        private StoryDebugInfo DebugInfo;
        private Dictionary<UInt32, Breakpoint> Breakpoints;
        private UInt32 NextBreakpointId = 1;
        // Goal name => Goal mappings
        private Dictionary<String, GoalLineMap> GoalMap;

        public BreakpointManager(DebuggerClient client, StoryDebugInfo debugInfo)
        {
            DebugInfo = debugInfo;
            DbgCli = client;
            Breakpoints = new Dictionary<uint, Breakpoint>();
            GoalMap = new Dictionary<string, GoalLineMap>();
            BuildLineMap();
        }

        public void ClearGoalBreakpoints(String goalName)
        {
            Breakpoints = Breakpoints
                .Where(kv => kv.Value.GoalName != goalName)
                .Select(kv => kv.Value)
                .ToDictionary(kv => kv.Id);
        }

        public Breakpoint SetGoalBreakpoint(String goalName, DAPSourceBreakpoint breakpoint)
        {
            var bp = new Breakpoint
            {
                Id = NextBreakpointId++,
                GoalName = goalName,
                Line = (UInt32)breakpoint.line,
                LineInfo = FindLocation(goalName, (UInt32)breakpoint.line)
            };

            if (bp.LineInfo == null)
            {
                bp.Verified = false;
                bp.ErrorReason = $"Could not map {goalName}:{breakpoint.line} to a story node";
            }
            else if (breakpoint.condition != null || breakpoint.hitCondition != null)
            {
                bp.Verified = false;
                bp.ErrorReason = "Conditional breakpoints are not supported";
            }
            else
            {
                bp.Verified = true;
            }

            Breakpoints.Add(bp.Id, bp);
            return bp;
        }

        public void UpdateBreakpoints()
        {
            var breakpoints = Breakpoints.Values.Where(bp => bp.Verified).ToList();
            DbgCli.SendSetBreakpoints(breakpoints);
        }

        private LineDebugInfo FindLocation(String goalName, UInt32 line)
        {
            GoalLineMap goalMap;
            if (!GoalMap.TryGetValue(goalName, out goalMap))
            {
                return null;
            }

            LineDebugInfo lineInfo;
            if (!goalMap.LineMap.TryGetValue(line, out lineInfo))
            {
                return null;
            }

            return lineInfo;
        }

        private void AddLineMapping(LineType type, GoalDebugInfo goal, NodeDebugInfo node, UInt32 index, UInt32 line)
        {
            GoalLineMap goalMap;
            if (!GoalMap.TryGetValue(goal.Name, out goalMap))
            {
                goalMap = new GoalLineMap
                {
                    Goal = goal,
                    LineMap = new Dictionary<uint, LineDebugInfo>()
                };
                GoalMap.Add(goal.Name, goalMap);
            }

            var mapping = new LineDebugInfo
            {
                Type = type,
                Goal = goal,
                Node = node,
                ActionIndex = index,
                Line = line
            };
            goalMap.LineMap[line] = mapping;
        }

        private void BuildLineMap(GoalDebugInfo goal)
        {
            for (var index = 0; index < goal.InitActions.Count; index++)
            {
                AddLineMapping(LineType.GoalInitActionLine, goal, null, (UInt32)index, goal.InitActions[index].Line);
            }

            for (var index = 0; index < goal.ExitActions.Count; index++)
            {
                AddLineMapping(LineType.GoalExitActionLine, goal, null, (UInt32)index, goal.ExitActions[index].Line);
            }
        }

        private void BuildLineMap(NodeDebugInfo node)
        {
            if (node.RuleId != 0)
            {
                var rule = DebugInfo.Rules[node.RuleId];
                var goal = DebugInfo.Goals[rule.GoalId];

                if (node.Line != 0
                    && node.Type != LSLib.LS.Story.Node.Type.Rule)
                {
                    AddLineMapping(LineType.NodeLine, goal, node, 0, (UInt32)node.Line);
                }

                if (node.Type == LSLib.LS.Story.Node.Type.Rule)
                {
                    for (var index = 0; index < rule.Actions.Count; index++)
                    {
                        AddLineMapping(LineType.RuleActionLine, goal, node, (UInt32)index, rule.Actions[index].Line);
                    }
                }
            }
        }

        private void BuildLineMap()
        {
            foreach (var goal in DebugInfo.Goals)
            {
                BuildLineMap(goal.Value);
            }

            foreach (var node in DebugInfo.Nodes)
            {
                BuildLineMap(node.Value);
            }
        }
    }
}
