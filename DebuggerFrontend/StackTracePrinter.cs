using LSLib.LS.Story;
using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LSTools.DebuggerFrontend;

public class DebugVariable
{
    public String Name;
    public String Type;
    public String Value;
    public MsgTypedValue TypedValue;
}

public class CoalescedFrame
{
    public String Name;
    public String File;
    public int Line;
    public MsgFrame Frame;
    // List of named variables available in this frame (if any)
    public List<DebugVariable> Variables;
    // Arguments that the PROC/QRY was called with
    // If the frame is not a call, this will be null.
    public MsgTuple CallArguments;
    // Rule that this frame belongs to.
    // We use this info to restrict the scope of a frame (to source lines) in VS
    public RuleDebugInfo Rule;
}

public class StackTracePrinter
{
    private StoryDebugInfo DebugInfo;
    private ValueFormatter Formatter;
    public bool MergeFrames = true;
    // Mod/project UUID we'll send to the debugger instead of the packaged path
    public string ModUuid;

    public StackTracePrinter(StoryDebugInfo debugInfo, ValueFormatter formatter)
    {
        DebugInfo = debugInfo;
        Formatter = formatter;
    }

    private List<DebugVariable> TupleToVariables(MsgFrame frame)
    {
        var variables = new List<DebugVariable>();
        NodeDebugInfo node = null;
        RuleDebugInfo rule = null;
        if (frame.NodeId != 0)
        {
            node = DebugInfo.Nodes[frame.NodeId];
            if (node.RuleId != 0)
            {
                rule = DebugInfo.Rules[node.RuleId];
            }
        }

        for (var i = 0; i < frame.Tuple.Column.Count; i++)
        {
            var value = frame.Tuple.Column[i];
            var variable = new DebugVariable
            {
                Name = Formatter.TupleVariableIndexToName(rule, node, i),
                // TODO type name!
                Type = value.TypeId.ToString(),
                Value = Formatter.ValueToString(value),
                TypedValue = value
            };

            variables.Add(variable);
        }

        return variables;
    }

    private CoalescedFrame MsgFrameToLocal(MsgFrame frame)
    {
        var outFrame = new CoalescedFrame();
        outFrame.Name = Formatter.GetFrameDebugName(frame);
        
        if (frame.Type == MsgFrame.Types.FrameType.GoalInitAction
            || frame.Type == MsgFrame.Types.FrameType.GoalExitAction)
        {
            var goal = DebugInfo.Goals[frame.GoalId];
            outFrame.File = goal.Path;
            if (frame.Type == MsgFrame.Types.FrameType.GoalInitAction)
            {
                outFrame.Line = (int)goal.InitActions[(int)frame.ActionIndex].Line;
            }
            else
            {
                outFrame.Line = (int)goal.ExitActions[(int)frame.ActionIndex].Line;
            }
        }
        else if (frame.NodeId != 0)
        {
            var node = DebugInfo.Nodes[frame.NodeId];
            if (node.RuleId != 0)
            {
                var rule = DebugInfo.Rules[node.RuleId];
                var goal = DebugInfo.Goals[rule.GoalId];
                outFrame.File = goal.Path;

                if (frame.Type == MsgFrame.Types.FrameType.Pushdown
                    && node.Type == Node.Type.Rule)
                {
                    outFrame.Line = (int)rule.ActionsStartLine;
                }
                else if (frame.Type == MsgFrame.Types.FrameType.RuleAction)
                {
                    outFrame.Line = (int)rule.Actions[(int)frame.ActionIndex].Line;
                }
                else
                {
                    outFrame.Line = node.Line;
                }
            }
        }

        outFrame.Variables = TupleToVariables(frame);
        outFrame.Frame = frame;

        if (outFrame.File != null
            && ModUuid != null)
        {
            var modRe = new Regex(".*\\.pak:/Mods/.*/Story/RawFiles/Goals/(.*)\\.txt");
            var match = modRe.Match(outFrame.File);
            if (match.Success)
            {
                outFrame.File = "divinity:/" + ModUuid + "/" + match.Groups[1].Value + ".divgoal";
            }
        }

        return outFrame;
    }

    /// <summary>
    /// Maps node calls to ranges. Each range represents one output frame in the final call stack.
    /// </summary>
    private List<List<CoalescedFrame>> DetermineFrameRanges(List<CoalescedFrame> frames)
    {
        var ranges = new List<List<CoalescedFrame>>();

        List<CoalescedFrame> currentFrames = new List<CoalescedFrame>();
        foreach (var frame in frames)
        {
            if (frame.Frame.Type == MsgFrame.Types.FrameType.GoalInitAction
                || frame.Frame.Type == MsgFrame.Types.FrameType.GoalExitAction)
            {
                // Goal INIT/EXIT frames don't have parent frames, so we'll add them as separate frames
                if (currentFrames.Count > 0)
                {
                    ranges.Add(currentFrames);
                    currentFrames = new List<CoalescedFrame>();
                }

                currentFrames.Add(frame);
                ranges.Add(currentFrames);
                currentFrames = new List<CoalescedFrame>();
            }
            else
            {
                // Embedded PROC/QRY frames start with Insert/Delete frames
                if (frame.Frame.Type == MsgFrame.Types.FrameType.Insert
                    || frame.Frame.Type == MsgFrame.Types.FrameType.Insert)
                {
                    if (currentFrames.Count > 0)
                    {
                        ranges.Add(currentFrames);
                        currentFrames = new List<CoalescedFrame>();
                    }
                }

                currentFrames.Add(frame);

                // Rule frames are terminated by RuleAction (THEN part) frames
                if (frame.Frame.Type == MsgFrame.Types.FrameType.RuleAction)
                {
                    ranges.Add(currentFrames);
                    currentFrames = new List<CoalescedFrame>();
                }
            }
        }


        if (currentFrames.Count > 0)
        {
            ranges.Add(currentFrames);
        }

        return ranges;
    }

    /// <summary>
    /// Merges a node call range into an output stack frame.
    /// </summary>
    private CoalescedFrame MergeFrame(List<CoalescedFrame> range)
    {
        var frame = new CoalescedFrame();
        frame.Frame = range[0].Frame;

        foreach (var node in range)
        {
            // Use last available location/variable data in the range
            if (node.Line != 0)
            {
                frame.File = node.File;
                frame.Line = node.Line;
            }

            if (frame.Rule == null && node.Frame.NodeId != 0)
            {
                var storyNode = DebugInfo.Nodes[node.Frame.NodeId];
                if (storyNode.RuleId != 0)
                {
                    var rule = DebugInfo.Rules[storyNode.RuleId];
                    frame.Rule = rule;
                }
            }

            if (node.Frame.Type == MsgFrame.Types.FrameType.Pushdown
                || node.Frame.Type == MsgFrame.Types.FrameType.Insert
                || node.Frame.Type == MsgFrame.Types.FrameType.Delete)
            {
                // Rule variable info is only propagated through Pushdown nodes.
                // All other nodes either have no variable info at all, or contain
                // local tuples used for DB insert/delete/query.

                // We'll keep the variables from Insert/Delete nodes if there are 
                // no better rule candidates, as they show the PROC/DB input tuple.
                frame.Variables = node.Variables;
            }

            if (node.Frame.Type == MsgFrame.Types.FrameType.Insert
                || node.Frame.Type == MsgFrame.Types.FrameType.Delete)
            {
                // We'll keep the initial argument list that was passed to the PROC/QRY/DB
                // (from the initial Insert/Delete frame) to display in the call frame name
                frame.CallArguments = node.Frame.Tuple;
            }
        }

        if (frame.Variables == null)
        {
            frame.Variables = new List<DebugVariable>();
        }

        frame.Name = Formatter.GetFrameName(frame.Frame, frame.CallArguments);

        // Special indicator for backward propagation of database inserts/deletes
        if (range.Count >= 2
            && (range[0].Frame.Type == MsgFrame.Types.FrameType.Insert
                || range[0].Frame.Type == MsgFrame.Types.FrameType.Delete)
            && (range[1].Frame.Type == MsgFrame.Types.FrameType.Pushdown
                || range[1].Frame.Type == MsgFrame.Types.FrameType.PushdownDelete))
        {
            var pushdownNode = DebugInfo.Nodes[range[1].Frame.NodeId];
            if (range[0].Frame.NodeId != pushdownNode.ParentNodeId)
            {
                frame.Name = "(Database Propagation) " + frame.Name;
            }
        }
        
        return frame;
    }

    private List<CoalescedFrame> MergeCallStack(List<CoalescedFrame> nodes)
    {
        var frameRanges = DetermineFrameRanges(nodes);
        var frames = frameRanges.Select(range => MergeFrame(range)).ToList();
        return frames;
    }

    public List<CoalescedFrame> BreakpointToStack(BkBreakpointTriggered message)
    {
        var rawFrames = message.CallStack.Select(frame => MsgFrameToLocal(frame)).ToList();
        List<CoalescedFrame> mergedFrames;
        if (MergeFrames)
        {
            mergedFrames = MergeCallStack(rawFrames);
        }
        else
        {
            mergedFrames = rawFrames;
        }

        mergedFrames.Reverse();
        return mergedFrames;
    }
}
