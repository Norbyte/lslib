using System;
using System.IO;
using LSLib.LS;
using LSLib.LS.Story.Compiler;
using LSTools.StoryCompiler;
using System.Text;
using System.Collections.Generic;

namespace LSTools.DebuggerFrontend;

class DebugInfoLoader
{
    private DatabaseDebugInfo FromProtobuf(DatabaseDebugInfoMsg msg)
    {
        var debugInfo = new DatabaseDebugInfo
        {
            Id = msg.Id,
            Name = msg.Name,
            ParamTypes = new List<uint>()
        };
        foreach (var paramType in msg.ParamTypes)
        {
            debugInfo.ParamTypes.Add(paramType);
        }

        return debugInfo;
    }

    private ActionDebugInfo FromProtobuf(ActionDebugInfoMsg msg)
    {
        return new ActionDebugInfo
        {
            Line = msg.Line
        };
    }

    private GoalDebugInfo FromProtobuf(GoalDebugInfoMsg msg)
    {
        var debugInfo = new GoalDebugInfo
        {
            Id = msg.Id,
            Name = msg.Name,
            Path = msg.Path,
            InitActions = new List<ActionDebugInfo>(),
            ExitActions = new List<ActionDebugInfo>()
        };

        foreach (var action in msg.InitActions)
        {
            debugInfo.InitActions.Add(FromProtobuf(action));
        }

        foreach (var action in msg.ExitActions)
        {
            debugInfo.ExitActions.Add(FromProtobuf(action));
        }

        return debugInfo;
    }

    private RuleVariableDebugInfo FromProtobuf(RuleVariableDebugInfoMsg msg)
    {
        return new RuleVariableDebugInfo
        {
            Index = msg.Index,
            Name = msg.Name,
            Type = msg.Type
        };
    }

    private RuleDebugInfo FromProtobuf(RuleDebugInfoMsg msg)
    {
        var debugInfo = new RuleDebugInfo
        {
            Id = msg.Id,
            GoalId = msg.GoalId,
            Name = msg.Name,
            Variables = new List<RuleVariableDebugInfo>(),
            Actions = new List<ActionDebugInfo>(),
            ConditionsStartLine = msg.ConditionsStartLine,
            ConditionsEndLine = msg.ConditionsEndLine,
            ActionsStartLine = msg.ActionsStartLine,
            ActionsEndLine = msg.ActionsEndLine
        };

        foreach (var variableMsg in msg.Variables)
        {
            var variable = FromProtobuf(variableMsg);
            debugInfo.Variables.Add(variable);
        }

        foreach (var action in msg.Actions)
        {
            debugInfo.Actions.Add(FromProtobuf(action));
        }

        return debugInfo;
    }

    private NodeDebugInfo FromProtobuf(NodeDebugInfoMsg msg)
    {
        var debugInfo = new NodeDebugInfo
        {
            Id = msg.Id,
            RuleId = msg.RuleId,
            Line = (Int32)msg.Line,
            ColumnToVariableMaps = new Dictionary<int, int>(),
            DatabaseId = msg.DatabaseId,
            Name = msg.Name,
            Type = (LSLib.LS.Story.Node.Type)msg.Type,
            ParentNodeId = msg.ParentNodeId
        };

        if (msg.FunctionName != "")
        {
            debugInfo.FunctionName = new FunctionNameAndArity(msg.FunctionName, (int)msg.FunctionArity);
        }

        foreach (var map in msg.ColumnMaps)
        {
            debugInfo.ColumnToVariableMaps.Add((Int32)map.Key, (Int32)map.Value);
        }

        return debugInfo;
    }

    private FunctionParamDebugInfo FromProtobuf(FunctionParamDebugInfoMsg msg)
    {
        return new FunctionParamDebugInfo
        {
            TypeId = msg.TypeId,
            Name = msg.Name,
            Out = msg.Out
        };
    }

    private FunctionDebugInfo FromProtobuf(FunctionDebugInfoMsg msg)
    {
        var debugInfo = new FunctionDebugInfo
        {
            Name = msg.Name,
            Params = new List<FunctionParamDebugInfo>(),
            TypeId = msg.TypeId
        };

        foreach (var param in msg.Params)
        {
            debugInfo.Params.Add(FromProtobuf(param));
        }

        return debugInfo;
    }

    private StoryDebugInfo FromProtobuf(StoryDebugInfoMsg msg)
    {
        var debugInfo = new StoryDebugInfo();
        debugInfo.Version = msg.Version;

        foreach (var dbMsg in msg.Databases)
        {
            var db = FromProtobuf(dbMsg);
            debugInfo.Databases.Add(db.Id, db);
        }

        foreach (var goalMsg in msg.Goals)
        {
            var goal = FromProtobuf(goalMsg);
            debugInfo.Goals.Add(goal.Id, goal);
        }

        foreach (var ruleMsg in msg.Rules)
        {
            var rule = FromProtobuf(ruleMsg);
            debugInfo.Rules.Add(rule.Id, rule);
        }

        foreach (var nodeMsg in msg.Nodes)
        {
            var node = FromProtobuf(nodeMsg);
            debugInfo.Nodes.Add(node.Id, node);
        }

        foreach (var funcMsg in msg.Functions)
        {
            var func = FromProtobuf(funcMsg);
            debugInfo.Functions.Add(new FunctionNameAndArity(func.Name, func.Params.Count), func);
        }

        return debugInfo;
    }

    public StoryDebugInfo Load(byte[] msgPayload)
    {
        UInt32 decompressedSize;
        byte[] lengthBuf = new byte[4];
        Array.Copy(msgPayload, msgPayload.Length - 4, lengthBuf, 0, 4);
        using (var ms = new MemoryStream(lengthBuf))
        using (var reader = new BinaryReader(ms, Encoding.UTF8, true))
        {
            decompressedSize = reader.ReadUInt32();
        }

        var compressed = new byte[msgPayload.Length - 4];
        Array.Copy(msgPayload, 0, compressed, 0, msgPayload.Length - 4);

        var flags = BinUtils.MakeCompressionFlags(CompressionMethod.LZ4, LSCompressionLevel.Fast);
        byte[] decompressed = BinUtils.Decompress(compressed, (int)decompressedSize, flags);
        var msg = StoryDebugInfoMsg.Parser.ParseFrom(decompressed);
        var debugInfo = FromProtobuf(msg);
        return debugInfo;
    }
}
