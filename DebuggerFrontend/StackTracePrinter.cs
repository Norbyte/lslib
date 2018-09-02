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

        private void DumpFrame(MsgFrame frame)
        {
            var node = DebugInfo.Nodes[frame.NodeId];

            string codeLocation = "";
            if (node.RuleId != 0)
            {
                var rule = DebugInfo.Rules[node.RuleId];
                var goal = DebugInfo.Goals[rule.GoalId];
                codeLocation = "@ " + goal.Name + ":" + node.Line.ToString() + " ";
            }

            string dbName = "";
            if (node.DatabaseId != 0)
            {
                var db = DebugInfo.Databases[node.DatabaseId];
                dbName = db.Name;
            }
            else
            {
                dbName = node.Name;
            }

            string tupleStr = TupleToString(frame);

            switch (frame.Type)
            {
                case MsgFrame.Types.FrameType.FrameIsValid:
                    if (node.Type == Node.Type.DivQuery || node.Type == Node.Type.InternalQuery || node.Type == Node.Type.UserQuery)
                    {
                        Console.WriteLine("    Query {0} ({1})", dbName, tupleStr);
                    }
                    else if (node.Type == Node.Type.Database)
                    {
                        Console.WriteLine("    Database {0} ({1})", dbName, tupleStr);
                    }
                    else
                    {
                        Console.WriteLine("    IsValid {0} {1} ({2})", node.Type, dbName, tupleStr);
                    }
                    break;

                case MsgFrame.Types.FrameType.FramePushdown:
                    if (node.Type == Node.Type.And || node.Type == Node.Type.NotAnd || node.Type == Node.Type.RelOp)
                    {
                        Console.WriteLine("    PushDown {0}({1})", codeLocation, tupleStr);
                    }
                    else if (node.Type == Node.Type.Rule)
                    {
                        Console.WriteLine("    Rule THEN part {0}({1})", codeLocation, tupleStr);
                    }
                    else
                    {
                        throw new InvalidOperationException("Pushdown operation not supported on this node");
                    }
                    break;

                case MsgFrame.Types.FrameType.FramePushdownDelete:
                    Console.WriteLine("    PushDownDelete {0} {1}({2})", node.Type, codeLocation, tupleStr);
                    break;

                case MsgFrame.Types.FrameType.FrameInsert:
                    if (node.Type == Node.Type.UserQuery)
                    {
                        Console.WriteLine("    User Query {0} ({1})", dbName, tupleStr);
                    }
                    else if (node.Type == Node.Type.Proc)
                    {
                        Console.WriteLine("    Call Proc {0} ({1})", dbName, tupleStr);
                    }
                    else if (node.Type == Node.Type.Database)
                    {
                        Console.WriteLine("    Insert Into {0} ({1})", dbName, tupleStr);
                    }
                    else
                    {
                        throw new InvalidOperationException("Insert operation not supported on this node");
                    }
                    break;

                case MsgFrame.Types.FrameType.FrameDelete:
                    if (node.Type == Node.Type.Database)
                    {
                        Console.WriteLine("    Delete from {0} ({1})", dbName, tupleStr);
                    }
                    else
                    {
                        throw new InvalidOperationException("Delete operation not supported on this node");
                    }
                    break;

                default:
                    throw new InvalidOperationException("Unsupported frame type");
            }
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

        private List<DebugVariable> TupleToVariables(MsgFrame frame)
        {
            var variables = new List<DebugVariable>();
            var node = DebugInfo.Nodes[frame.NodeId];
            RuleDebugInfo rule = null;
            if (node.RuleId != 0)
            {
                rule = DebugInfo.Rules[node.RuleId];
            }

            for (var i = 0; i < frame.Tuple.Column.Count; i++)
            {
                var value = frame.Tuple.Column[i];
                var variable = new DebugVariable();

                if (rule == null)
                {
                    variable.Name = "#" + i.ToString();
                }
                else if (i < node.ColumnToVariableMaps.Count)
                {
                    var mappedColumnIdx = node.ColumnToVariableMaps[i];
                    if (mappedColumnIdx < rule.Variables.Count)
                    {
                        var ruleVar = rule.Variables[mappedColumnIdx];
                        variable.Name = ruleVar.Name;
                    }
                    else
                    {
                        variable.Name = String.Format("(Bad Variable Idx #{0})", i);
                    }
                }
                else
                {
                    variable.Name = String.Format("(Unknown #{0})", i);
                }

                // TODO type!
                variable.Type = value.TypeId.ToString();
                variable.Value = ValueToString(value);

                variables.Add(variable);
            }

            return variables;
        }

        private List<MsgFrame> CoalesceCallStack(BkBreakpointTriggered message)
        {
            var frames = new List<MsgFrame>();
            var index = 0;
            foreach (var frame in message.CallStack)
            {
                if (frame.Type == MsgFrame.Types.FrameType.FrameInsert
                    || frame.Type == MsgFrame.Types.FrameType.FrameDelete
                    || (frame.Type == MsgFrame.Types.FrameType.FramePushdown
                        && DebugInfo.Nodes[frame.NodeId].Type == Node.Type.Rule)
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
            var node = DebugInfo.Nodes[frame.NodeId];

            string dbName = "";
            if (node.DatabaseId != 0)
            {
                var db = DebugInfo.Databases[node.DatabaseId];
                dbName = db.Name;
            }
            else
            {
                dbName = node.Name;
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

                default:
                    throw new InvalidOperationException($"Unsupported frame type: {frame.Type}");
            }
        }

        private CoalescedFrame MsgFrameToLocal(MsgFrame frame)
        {
            var outFrame = new CoalescedFrame();
            outFrame.Name = GetFrameName(frame);

            var node = DebugInfo.Nodes[frame.NodeId];
            if (node.RuleId != 0)
            {
                var rule = DebugInfo.Rules[node.RuleId];
                var goal = DebugInfo.Goals[rule.GoalId];
                outFrame.File = goal.Path;
                outFrame.Line = node.Line;
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
                // DumpFrame(frame);
            }

            return stack;
        }
    }
}
