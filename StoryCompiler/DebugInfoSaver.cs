using System;
using Google.Protobuf;
using System.IO;
using LSLib.LS;
using LSLib.LS.Story.Compiler;
using System.Text;

namespace LSTools.StoryCompiler;

class DebugInfoSaver
{
    private DatabaseDebugInfoMsg ToProtobuf(DatabaseDebugInfo debugInfo)
    {
        var msg = new DatabaseDebugInfoMsg
        {
            Id = debugInfo.Id,
            Name = debugInfo.Name
        };
        foreach (var paramType in debugInfo.ParamTypes)
        {
            msg.ParamTypes.Add(paramType);
        }

        return msg;
    }

    private GoalDebugInfoMsg ToProtobuf(GoalDebugInfo debugInfo)
    {
        var msg = new GoalDebugInfoMsg
        {
            Id = debugInfo.Id,
            Name = debugInfo.Name,
            Path = debugInfo.Path
        };

        foreach (var action in debugInfo.InitActions)
        {
            var varAct = ToProtobuf(action);
            msg.InitActions.Add(varAct);
        }

        foreach (var action in debugInfo.ExitActions)
        {
            var varAct = ToProtobuf(action);
            msg.ExitActions.Add(varAct);
        }

        return msg;
    }

    private RuleVariableDebugInfoMsg ToProtobuf(RuleVariableDebugInfo debugInfo)
    {
        return new RuleVariableDebugInfoMsg
        {
            Index = debugInfo.Index,
            Name = debugInfo.Name,
            Type = debugInfo.Type,
            Unused = debugInfo.Unused
        };
    }

    private ActionDebugInfoMsg ToProtobuf(ActionDebugInfo debugInfo)
    {
        return new ActionDebugInfoMsg
        {
            Line = debugInfo.Line
        };
    }

    private RuleDebugInfoMsg ToProtobuf(RuleDebugInfo debugInfo)
    {
        var msg = new RuleDebugInfoMsg
        {
            Id = debugInfo.Id,
            GoalId = debugInfo.GoalId,
            Name = debugInfo.Name,
            ConditionsStartLine = debugInfo.ConditionsStartLine,
            ConditionsEndLine = debugInfo.ConditionsEndLine,
            ActionsStartLine = debugInfo.ActionsStartLine,
            ActionsEndLine = debugInfo.ActionsEndLine
        };

        foreach (var variable in debugInfo.Variables)
        {
            var varMsg = ToProtobuf(variable);
            msg.Variables.Add(varMsg);
        }

        foreach (var action in debugInfo.Actions)
        {
            var varAct = ToProtobuf(action);
            msg.Actions.Add(varAct);
        }

        return msg;
    }

    private NodeDebugInfoMsg ToProtobuf(NodeDebugInfo debugInfo)
    {
        var msg = new NodeDebugInfoMsg
        {
            Id = debugInfo.Id,
            RuleId = debugInfo.RuleId,
            Line = (UInt32)debugInfo.Line,
            DatabaseId = debugInfo.DatabaseId,
            Name = debugInfo.Name,
            Type = (NodeDebugInfoMsg.Types.NodeType)debugInfo.Type,
            ParentNodeId = debugInfo.ParentNodeId,
            FunctionName = debugInfo.FunctionName != null ? debugInfo.FunctionName.Name : "",
            FunctionArity = debugInfo.FunctionName != null ? (uint)debugInfo.FunctionName.Arity : 0
        };

        foreach (var map in debugInfo.ColumnToVariableMaps)
        {
            msg.ColumnMaps.Add((UInt32)map.Key, (UInt32)map.Value);
        }

        return msg;
    }

    private FunctionParamDebugInfoMsg ToProtobuf(FunctionParamDebugInfo debugInfo)
    {
        return new FunctionParamDebugInfoMsg
        {
            TypeId = debugInfo.TypeId,
            Name = debugInfo.Name ?? "",
            Out = debugInfo.Out
        };
    }

    private FunctionDebugInfoMsg ToProtobuf(FunctionDebugInfo debugInfo)
    {
        var msg = new FunctionDebugInfoMsg
        {
            Name = debugInfo.Name,
            TypeId = debugInfo.TypeId
        };

        foreach (var param in debugInfo.Params)
        {
            msg.Params.Add(ToProtobuf(param));
        }

        return msg;
    }

    private StoryDebugInfoMsg ToProtobuf(StoryDebugInfo debugInfo)
    {
        var msg = new StoryDebugInfoMsg();
        msg.Version = debugInfo.Version;

        foreach (var db in debugInfo.Databases)
        {
            var dbMsg = ToProtobuf(db.Value);
            msg.Databases.Add(dbMsg);
        }

        foreach (var goal in debugInfo.Goals)
        {
            var goalMsg = ToProtobuf(goal.Value);
            msg.Goals.Add(goalMsg);
        }

        foreach (var rule in debugInfo.Rules)
        {
            var ruleMsg = ToProtobuf(rule.Value);
            msg.Rules.Add(ruleMsg);
        }

        foreach (var node in debugInfo.Nodes)
        {
            var nodeMsg = ToProtobuf(node.Value);
            msg.Nodes.Add(nodeMsg);
        }

        foreach (var func in debugInfo.Functions)
        {
            var funcMsg = ToProtobuf(func.Value);
            msg.Functions.Add(funcMsg);
        }

        return msg;
    }

    public void Save(Stream stream, StoryDebugInfo debugInfo)
    {
        var msg = ToProtobuf(debugInfo);
        using (var ms = new MemoryStream())
        using (var codedStream = new CodedOutputStream(ms))
        {
            msg.WriteTo(codedStream);
            codedStream.Flush();

            byte[] proto = ms.ToArray();
            var flags = BinUtils.MakeCompressionFlags(CompressionMethod.LZ4, LSCompressionLevel.Fast);
            byte[] compressed = BinUtils.Compress(proto, flags);
            stream.Write(compressed, 0, compressed.Length);

            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write((UInt32)proto.Length);
            }
        }
    }
}
