using LSLib.LS.Story;
using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSTools.DebuggerFrontend
{
    public class DebugVariable
    {
        public String Name;
        public String Type;
        public String Value;
    }

    public class CoalescedFrame
    {
        public String Name;
        public String File;
        public int Line;
        public MsgFrame Frame;
        public List<DebugVariable> Variables;
    }

    public class StackTracePrinter
    {
        private StoryDebugInfo DebugInfo;
        public bool MergeFrames = true;

        public StackTracePrinter(StoryDebugInfo debugInfo)
        {
            DebugInfo = debugInfo;
        }

        private string TupleToString(MsgFrame frame)
        {
            string tuple = "";
            var node = DebugInfo.Nodes[frame.NodeId];
            RuleDebugInfo rule = null;
            if (node.RuleId != 0)
            {
                rule = DebugInfo.Rules[node.RuleId];
            }

            for (var i = 0; i < frame.Tuple.Column.Count; i++)
            {
                var value = frame.Tuple.Column[i];
                string columnName;
                if (rule == null)
                {
                    columnName = "";
                }
                else if (i < node.ColumnToVariableMaps.Count)
                {
                    var mappedColumnIdx = node.ColumnToVariableMaps[i];
                    if (mappedColumnIdx < rule.Variables.Count)
                    {
                        var variable = rule.Variables[mappedColumnIdx];
                        columnName = variable.Name;
                    }
                    else
                    {
                        columnName = "(Bad Variable Idx)";
                    }
                }
                else
                {
                    columnName = "(Unknown)";
                }

                string valueStr;
                switch ((Value.Type)value.TypeId)
                {
                    case Value.Type.Unknown:
                        valueStr = "(None)";
                        break;

                    case Value.Type.Integer:
                    case Value.Type.Integer64:
                        valueStr = value.Intval.ToString();
                        break;

                    case Value.Type.Float:
                        valueStr = value.Floatval.ToString();
                        break;

                    case Value.Type.String:
                    case Value.Type.GuidString:
                    default:
                        valueStr = value.Stringval;
                        break;

                }

                if (columnName.Length > 0)
                {
                    tuple += String.Format("{0}={1}, ", columnName, valueStr);
                }
                else
                {
                    tuple += String.Format("{0}, ", valueStr);
                }
            }

            return tuple;
        }

        private string ValueToString(MsgTypedValue value)
        {
            string valueStr;
            switch ((Value.Type)value.TypeId)
            {
                case Value.Type.Unknown:
                    valueStr = "(None)";
                    break;

                case Value.Type.Integer:
                case Value.Type.Integer64:
                    valueStr = value.Intval.ToString();
                    break;

                case Value.Type.Float:
                    valueStr = value.Floatval.ToString();
                    break;

                case Value.Type.String:
                case Value.Type.GuidString:
                default:
                    valueStr = value.Stringval;
                    break;
            }

            return valueStr;
        }

        private String TupleVariableIndexToName(RuleDebugInfo rule, NodeDebugInfo node, int index)
        {
            if (rule == null)
            {
                return "#" + index.ToString();
            }
            else if (node != null)
            {
                if (index < node.ColumnToVariableMaps.Count)
                {
                    var mappedColumnIdx = node.ColumnToVariableMaps[index];
                    if (mappedColumnIdx < rule.Variables.Count)
                    {
                        return rule.Variables[mappedColumnIdx].Name;
                    }
                    else
                    {
                        return String.Format("(Bad Variable Idx #{0})", index);
                    }
                }
                else
                {
                    return String.Format("(Unknown #{0})", index);
                }
            }
            else
            {
                if (index < rule.Variables.Count)
                {
                    return rule.Variables[index].Name;
                }
                else
                {
                    return String.Format("(Bad Variable Idx #{0})", index);
                }
            }
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
                    Name = TupleVariableIndexToName(rule, node, i),
                    // TODO type name!
                    Type = value.TypeId.ToString(),
                    Value = ValueToString(value)
                };

                variables.Add(variable);
            }

            return variables;
        }

        private string GetFrameDebugName(MsgFrame frame)
        {
            string frameType;
            switch (frame.Type)
            {
                case MsgFrame.Types.FrameType.IsValid: frameType = "IsValid"; break;
                case MsgFrame.Types.FrameType.Pushdown: frameType = "Pushdown"; break;
                case MsgFrame.Types.FrameType.PushdownDelete: frameType = "PushdownDelete"; break;
                case MsgFrame.Types.FrameType.Insert: frameType = "Insert"; break;
                case MsgFrame.Types.FrameType.Delete: frameType = "Delete"; break;
                case MsgFrame.Types.FrameType.RuleAction: frameType = "RuleAction"; break;
                case MsgFrame.Types.FrameType.GoalInitAction: frameType = "GoalInitAction"; break;
                case MsgFrame.Types.FrameType.GoalExitAction: frameType = "GoalExitAction"; break;

                default:
                    throw new InvalidOperationException($"Unsupported frame type: {frame.Type}");
            }

            if (frame.NodeId != 0)
            {
                string dbName = "";
                var node = DebugInfo.Nodes[frame.NodeId];
                if (node.DatabaseId != 0)
                {
                    var db = DebugInfo.Databases[node.DatabaseId];
                    dbName = db.Name;
                }
                else if (node.Name != null && node.Name.Length > 0)
                {
                    dbName = node.Name;
                }

                if (dbName != "")
                {
                    return $"{frameType} @ {node.Type} (DB {dbName})";
                }
                else
                {
                    return $"{frameType} @ {node.Type}";
                }
            }
            else
            {
                var goal = DebugInfo.Goals[frame.GoalId];
                return $"{frameType} @ {goal.Name}";
            }
        }

        private string GetFrameName(MsgFrame frame)
        {
            switch (frame.Type)
            {
                case MsgFrame.Types.FrameType.GoalInitAction:
                    {
                        var goal = DebugInfo.Goals[frame.GoalId].Name;
                        return goal + " (INIT)";
                    }

                case MsgFrame.Types.FrameType.GoalExitAction:
                    {
                        var goal = DebugInfo.Goals[frame.GoalId].Name;
                        return goal + " (EXIT)";
                    }

                case MsgFrame.Types.FrameType.Insert:
                    {
                        var node = DebugInfo.Nodes[frame.NodeId];
                        if (node.Type == Node.Type.Database)
                        {
                            var db = DebugInfo.Databases[node.DatabaseId];
                            return db.Name + " (INSERT)";
                        }
                        else
                        {
                            return node.Name;
                        }
                    }

                case MsgFrame.Types.FrameType.Delete:
                    {
                        var node = DebugInfo.Nodes[frame.NodeId];
                        var db = DebugInfo.Databases[node.DatabaseId];
                        return db.Name + " (DELETE)";
                    }

                default:
                    throw new InvalidOperationException($"Unsupported root frame type: {frame.Type}");
            }
        }

        private CoalescedFrame MsgFrameToLocal(MsgFrame frame)
        {
            var outFrame = new CoalescedFrame();
            outFrame.Name = GetFrameDebugName(frame);
            
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
            }

            if (frame.Variables == null)
            {
                frame.Variables = new List<DebugVariable>();
            }

            frame.Name = GetFrameName(frame.Frame);

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
}
