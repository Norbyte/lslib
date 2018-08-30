using System;
using Google.Protobuf;
using System.IO;
using LSLib.LS;
using LSLib.LS.Story.Compiler;

namespace LSTools.StoryCompiler
{
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
            return new GoalDebugInfoMsg
            {
                Id = debugInfo.Id,
                Name = debugInfo.Name,
                Path = debugInfo.Path
            };
        }

        private RuleVariableDebugInfoMsg ToProtobuf(RuleVariableDebugInfo debugInfo)
        {
            return new RuleVariableDebugInfoMsg
            {
                Index = debugInfo.Index,
                Name = debugInfo.Name,
                Type = debugInfo.Type
            };
        }

        private RuleDebugInfoMsg ToProtobuf(RuleDebugInfo debugInfo)
        {
            var msg = new RuleDebugInfoMsg
            {
                Id = debugInfo.Id,
                GoalId = debugInfo.GoalId
            };

            foreach (var variable in debugInfo.Variables)
            {
                var varMsg = ToProtobuf(variable);
                msg.Variables.Add(varMsg);
            }

            return msg;
        }

        private NodeDebugInfoMsg ToProtobuf(NodeDebugInfo debugInfo)
        {
            var msg = new NodeDebugInfoMsg
            {
                Id = debugInfo.Id,
                RuleId = debugInfo.RuleId,
                Line = (UInt32)debugInfo.Line
            };

            foreach (var map in debugInfo.ColumnToVariableMaps)
            {
                msg.ColumnMaps.Add((UInt32)map.Key, (UInt32)map.Value);
            }

            return msg;
        }

        private StoryDebugInfoMsg ToProtobuf(StoryDebugInfo debugInfo)
        {
            var msg = new StoryDebugInfoMsg();
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

            return msg;
        }

        public void Save(Stream stream, StoryDebugInfo debugInfo)
        {
            var msg = ToProtobuf(debugInfo);
            using (var ms = new MemoryStream())
            using (var codedStream = new CodedOutputStream(ms))
            {
                msg.WriteTo(codedStream);
                byte[] proto = ms.GetBuffer();
                byte flags = BinUtils.MakeCompressionFlags(LSLib.LS.Enums.CompressionMethod.LZ4, LSLib.LS.Enums.CompressionLevel.FastCompression);
                byte[] compressed = BinUtils.Compress(proto, flags);
                stream.Write(compressed, 0, compressed.Length);
            }
        }
    }
}
