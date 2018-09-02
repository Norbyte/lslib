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

        private List<MsgFrame> CoalesceCallStack(BkBreakpointTriggered message)
        {
            var frames = new List<MsgFrame>();
            var index = 0;
            for (var i = 0; i < message.CallStack.Count; i++)
            {
                var frame = message.CallStack[i];

                // Copy rule-local variables from the join frame to the action frame
                if (frame.Type == MsgFrame.Types.FrameType.FrameRuleAction
                    && i > 0)
                {
                    var prevFrame = message.CallStack[i - 1];
                    frame.Tuple = prevFrame.Tuple;
                }

                if (frame.Type == MsgFrame.Types.FrameType.FrameInsert
                    || frame.Type == MsgFrame.Types.FrameType.FrameDelete
                    || frame.Type == MsgFrame.Types.FrameType.FrameRuleAction
                    || frame.Type == MsgFrame.Types.FrameType.FrameGoalInitAction
                    || frame.Type == MsgFrame.Types.FrameType.FrameGoalExitAction
                    || index == message.CallStack.Count - 1)
                {
                    frames.Add(frame);
                }

                index++;
            }

            return frames;
        }

        private string GetFrameName(MsgFrame frame)
        {
            if (frame.Type == MsgFrame.Types.FrameType.FrameIsValid
                || frame.Type == MsgFrame.Types.FrameType.FramePushdown
                || frame.Type == MsgFrame.Types.FrameType.FramePushdownDelete
                || frame.Type == MsgFrame.Types.FrameType.FrameInsert
                || frame.Type == MsgFrame.Types.FrameType.FrameDelete)
            {
                return GetNodeFrameName(frame);
            }
            else
            {
                return GetActionFrameName(frame);
            }
        }

        private string GetNodeFrameName(MsgFrame frame)
        {
            var node = DebugInfo.Nodes[frame.NodeId];

            string dbName = "";
            if (node.DatabaseId != 0)
            {
                var db = DebugInfo.Databases[node.DatabaseId];
                dbName = db.Name;
            }
            else if (node.Name != null && node.Name.Length > 0)
            {
                dbName = node.Name;
            }
            else if (node.RuleId != 0)
            {
                var rule = DebugInfo.Rules[node.RuleId];
                // TODOrule.
                dbName = "(rule)";
            }
            else
            {
                dbName = "(unknown)";
            }

            switch (frame.Type)
            {
                case MsgFrame.Types.FrameType.FrameIsValid:
                    if (node.Type == Node.Type.DivQuery || node.Type == Node.Type.InternalQuery || node.Type == Node.Type.UserQuery)
                    {
                        return dbName;
                    }
                    else if (node.Type == Node.Type.Database)
                    {
                        return dbName + " (Query)";
                    }
                    else
                    {
                        return String.Format("IsValid({0}, {1})", node.Type, dbName);
                    }

                case MsgFrame.Types.FrameType.FramePushdown:
                    if (node.Type == Node.Type.And || node.Type == Node.Type.NotAnd || node.Type == Node.Type.RelOp)
                    {
                        return "PushDown";
                    }
                    else if (node.Type == Node.Type.Rule)
                    {
                        return "Rule THEN part";
                    }
                    else
                    {
                        throw new InvalidOperationException($"Pushdown operation not supported on node {node.Type}");
                    }

                case MsgFrame.Types.FrameType.FramePushdownDelete:
                    return String.Format("    PushDownDelete {0}", node.Type);

                case MsgFrame.Types.FrameType.FrameInsert:
                    if (node.Type == Node.Type.UserQuery || node.Type == Node.Type.Proc)
                    {
                        return dbName;
                    }
                    else if (node.Type == Node.Type.Database)
                    {
                        return dbName + " (Insert)";
                    }
                    else
                    {
                        throw new InvalidOperationException($"Insert operation not supported on node {node.Type}");
                    }

                case MsgFrame.Types.FrameType.FrameDelete:
                    if (node.Type == Node.Type.Database)
                    {
                        return dbName + " (Delete)";
                    }
                    else
                    {
                        throw new InvalidOperationException($"Delete operation not supported on node {node.Type}");
                    }

                case MsgFrame.Types.FrameType.FrameRuleAction:
                    if (node.Type == Node.Type.Rule)
                    {
                        return dbName + " (THEN part)";
                    }
                    else
                    {
                        throw new InvalidOperationException($"Delete operation not supported on node {node.Type}");
                    }

                default:
                    throw new InvalidOperationException($"Unsupported frame type: {frame.Type}");
            }
        }

        private string GetActionFrameName(MsgFrame frame)
        {
            switch (frame.Type)
            {
                case MsgFrame.Types.FrameType.FrameGoalInitAction:
                    {
                        var goal = DebugInfo.Goals[frame.GoalId].Name;
                        return goal + " (INIT)";
                    }

                case MsgFrame.Types.FrameType.FrameGoalExitAction:
                    {
                        var goal = DebugInfo.Goals[frame.GoalId].Name;
                        return goal + " (EXIT)";
                    }

                case MsgFrame.Types.FrameType.FrameRuleAction:
                    {
                        var node = DebugInfo.Nodes[frame.NodeId];
                        var rule = DebugInfo.Rules[node.RuleId];
                        return rule.Name + " (THEN part)";
                    }

                default:
                    throw new InvalidOperationException($"Unsupported action type: {frame.Type}");
            }
        }

        private CoalescedFrame MsgFrameToLocal(MsgFrame frame)
        {
            var outFrame = new CoalescedFrame();
            outFrame.Name = GetFrameName(frame);


            if (frame.Type == MsgFrame.Types.FrameType.FrameGoalInitAction
                || frame.Type == MsgFrame.Types.FrameType.FrameGoalExitAction)
            {
                var goal = DebugInfo.Goals[frame.GoalId];
                outFrame.File = goal.Path;
                if (frame.Type == MsgFrame.Types.FrameType.FrameGoalInitAction)
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

                    if (frame.Type == MsgFrame.Types.FrameType.FramePushdown
                        && node.Type == Node.Type.Rule)
                    {
                        outFrame.Line = (int)rule.ActionsStartLine;
                    }
                    else if (frame.Type == MsgFrame.Types.FrameType.FrameRuleAction)
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

        public List<CoalescedFrame> BreakpointToStack(BkBreakpointTriggered message)
        {
            var coalesced = CoalesceCallStack(message);
            var stack = new List<CoalescedFrame>();
            foreach (var frame in coalesced)
            {
                stack.Add(MsgFrameToLocal(frame));
            }

            stack.Reverse();
            return stack;
        }
    }
}
