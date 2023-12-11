using LSLib.LS.Story;
using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LSTools.DebuggerFrontend;

public class ValueFormatter
{
    private StoryDebugInfo DebugInfo;

    public ValueFormatter(StoryDebugInfo debugInfo)
    {
        DebugInfo = debugInfo;
    }

    public string TupleToString(MsgFrame frame)
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
            string columnName = TupleVariableIndexToName(rule, node, i);

            string valueStr;
            switch ((Value.Type)value.TypeId)
            {
                case Value.Type.None:
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

    public string TupleToString(MsgTuple tuple)
    {
        return String.Join(", ", tuple.Column.Select(val => ValueToString(val)));
    }

    public string ValueToString(MsgTypedValue value)
    {
        string valueStr;
        switch ((Value.Type)value.TypeId)
        {
            case Value.Type.None:
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

    public String TupleVariableIndexToName(RuleDebugInfo rule, NodeDebugInfo node, int index)
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

    public string GetFrameDebugName(MsgFrame frame)
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

    public string GetFrameName(MsgFrame frame, MsgTuple arguments)
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
            case MsgFrame.Types.FrameType.Delete:
                {
                    string argumentsFmt = "";
                    if (arguments != null)
                    {
                        argumentsFmt = "(" + TupleToString(arguments) + ")";
                    }

                    var node = DebugInfo.Nodes[frame.NodeId];
                    if (node.Type == Node.Type.Database)
                    {
                        var db = DebugInfo.Databases[node.DatabaseId];
                        if (frame.Type == MsgFrame.Types.FrameType.Insert)
                        {
                            return db.Name + argumentsFmt + " (INSERT)";
                        }
                        else
                        {
                            return db.Name + argumentsFmt + " (DELETE)";
                        }
                    }
                    else
                    {
                        return node.Name + argumentsFmt;
                    }
                }

            default:
                throw new InvalidOperationException($"Unsupported root frame type: {frame.Type}");
        }
    }
}
