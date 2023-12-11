using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSTools.DebuggerFrontend;

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

public class CodeLocationTranslator
{
    private StoryDebugInfo DebugInfo;
    // Goal name => Goal mappings
    private Dictionary<String, GoalLineMap> GoalMap;

    public CodeLocationTranslator(StoryDebugInfo debugInfo)
    {
        DebugInfo = debugInfo;
        GoalMap = new Dictionary<string, GoalLineMap>();
        BuildLineMap();
    }

    public LineDebugInfo LocationToNode(String goalName, UInt32 line)
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

public class Breakpoint
{
    // Unique breakpoint ID on frontend
    public UInt32 Id;
    // Source code location reference
    public DAPSource Source;
    // Story goal name
    public String GoalName;
    // 1-based line number on goal file
    public UInt32 Line;
    // Line to node mapping (if the line could be mapped to a valid location)
    public LineDebugInfo LineInfo;
    // Is the node permanently invalidated?
    // (ie. an unsupported feature was requested when adding the breakpoint, like conditional breaks)
    public bool PermanentlyInvalid;
    // Was the breakpoint correct and could it be mapped to a node?
    // This is updated each time the debug info is reloaded.
    public bool Verified;
    // Reason for verification error
    public String ErrorReason;

    public DAPBreakpoint ToDAP()
    {
        return new DAPBreakpoint
        {
            id = (int)Id,
            verified = Verified,
            message = ErrorReason,
            source = Source,
            line = (int)Line
        };
    }
}

public class BreakpointManager
{
    private DebuggerClient DbgCli;
    private CodeLocationTranslator LocationTranslator;
    private Dictionary<UInt32, Breakpoint> Breakpoints;
    private UInt32 NextBreakpointId = 1;

    public BreakpointManager(DebuggerClient client)
    {
        DbgCli = client;
        Breakpoints = new Dictionary<uint, Breakpoint>();
    }

    public List<Breakpoint> DebugInfoLoaded(StoryDebugInfo debugInfo)
    {
        LocationTranslator = new CodeLocationTranslator(debugInfo);
        var changes = RevalidateBreakpoints();
        // Sync breakpoint list to backend as the current debugger instance doesn't have
        // any of our breakpoints yet
        UpdateBreakpointsOnBackend();
        return changes;
    }

    public List<Breakpoint> DebugInfoUnloaded()
    {
        LocationTranslator = null;
        var changes = RevalidateBreakpoints();
        return changes;
    }

    public void ClearGoalBreakpoints(String goalName)
    {
        Breakpoints = Breakpoints
            .Where(kv => kv.Value.GoalName != goalName)
            .Select(kv => kv.Value)
            .ToDictionary(kv => kv.Id);
    }

    public Breakpoint AddBreakpoint(DAPSource source, DAPSourceBreakpoint breakpoint)
    {
        var bp = new Breakpoint
        {
            Id = NextBreakpointId++,
            Source = source,
            GoalName = Path.GetFileNameWithoutExtension(source.name),
            Line = (UInt32)breakpoint.line,
            PermanentlyInvalid = false
        };
        Breakpoints.Add(bp.Id, bp);

        if (breakpoint.condition != null || breakpoint.hitCondition != null)
        {
            bp.PermanentlyInvalid = true;
            bp.ErrorReason = "Conditional breakpoints are not supported";
        }

        ValidateBreakpoint(bp);

        return bp;
    }

    /// <summary>
    /// Transmits the list of active breakpoints to the debugger backend.
    /// </summary>
    public void UpdateBreakpointsOnBackend()
    {
        var breakpoints = Breakpoints.Values.Where(bp => bp.Verified).ToList();
        DbgCli.SendSetBreakpoints(breakpoints);
    }

    /// <summary>
    /// Rechecks the code -> node mapping of each breakpoint.
    /// This is required after each story reload/recompilation to make sure
    /// that we don't use stale node ID-s from the previous compilation.
    /// </summary>
    List<Breakpoint> RevalidateBreakpoints()
    {
        var changes = new List<Breakpoint>();
        foreach (var bp in Breakpoints)
        {
            bool changed = ValidateBreakpoint(bp.Value);
            if (changed)
            {
                changes.Add(bp.Value);
            }
        }

        return changes;
    }

    private bool ValidateBreakpoint(Breakpoint bp)
    {
        if (bp.PermanentlyInvalid)
        {
            bp.Verified = false;
            // Don't touch the error message here, as it was already updated when the 
            // PermanentlyInvalid flag was set.
            return false;
        }

        var oldVerified = bp.Verified;
        var oldReason = bp.ErrorReason;

        bp.LineInfo = LocationToNode(bp.GoalName, bp.Line);

        if (bp.LineInfo == null)
        {
            bp.Verified = false;
            bp.ErrorReason = $"Could not map {bp.GoalName}:{bp.Line} to a story node";
        }
        else
        {
            bp.Verified = true;
            bp.ErrorReason = null;
        }

        var changed = (bp.Verified != oldVerified || bp.ErrorReason != oldReason);
        return changed;
    }

    private LineDebugInfo LocationToNode(String goalName, UInt32 line)
    {
        if (LocationTranslator == null)
        {
            return null;
        }
        else
        {
            return LocationTranslator.LocationToNode(goalName, line);
        }
    }
}
