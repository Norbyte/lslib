using LSLib.LS.Story;
using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSTools.DebuggerFrontend;

public class DebugInfoSync
{
    private StoryDebugInfo DebugInfo;
    private Dictionary<UInt32, MsgGoalInfo> Goals = new Dictionary<UInt32, MsgGoalInfo>();
    private Dictionary<UInt32, MsgDatabaseInfo> Databases = new Dictionary<UInt32, MsgDatabaseInfo>();
    private Dictionary<UInt32, MsgNodeInfo> Nodes = new Dictionary<UInt32, MsgNodeInfo>();
    private Dictionary<UInt32, MsgRuleInfo> Rules = new Dictionary<UInt32, MsgRuleInfo>();

    public Boolean Matches;
    public List<String> Reasons = new List<string>();

    public DebugInfoSync(StoryDebugInfo debugInfo)
    {
        DebugInfo = debugInfo;
    }

    public void AddData(BkSyncStoryData data)
    {
        foreach (var goal in data.Goal)
        {
            Goals.Add(goal.Id, goal);
        }

        foreach (var db in data.Database)
        {
            Databases.Add(db.Id, db);
        }

        foreach (var node in data.Node)
        {
            Nodes.Add(node.Id, node);
        }

        foreach (var rule in data.Rule)
        {
            Rules.Add(rule.NodeId, rule);
        }
    }

    public void Finish()
    {
        if (Goals.Count != DebugInfo.Goals.Count)
        {
            Reasons.Add($"Goal count mismatch; local {DebugInfo.Goals.Count}, remote {Goals.Count}");
        }

        if (Databases.Count != DebugInfo.Databases.Count)
        {
            Reasons.Add($"Database count mismatch; local {DebugInfo.Databases.Count}, remote {Databases.Count}");
        }

        if (Nodes.Count != DebugInfo.Nodes.Count)
        {
            Reasons.Add($"Node count mismatch; local {DebugInfo.Nodes.Count}, remote {Nodes.Count}");
        }

        if (Rules.Count != DebugInfo.Rules.Count)
        {
            Reasons.Add($"Rule count mismatch; local {DebugInfo.Rules.Count}, remote {Rules.Count}");
        }

        if (Reasons.Count > 0)
        {
            Matches = false;
            return;
        }

        foreach (var goal in DebugInfo.Goals)
        {
            var remoteGoal = Goals[goal.Key];
            if (remoteGoal.Name != goal.Value.Name)
            {
                Reasons.Add($"Goal {goal.Key} name mismatch; local {goal.Value.Name}, remote {remoteGoal.Name}");
            }

            if (remoteGoal.InitActions.Count != goal.Value.InitActions.Count)
            {
                Reasons.Add($"Goal {goal.Key} INIT action count mismatch; local {goal.Value.InitActions.Count}, remote {remoteGoal.InitActions.Count}");
            }

            if (remoteGoal.ExitActions.Count != goal.Value.ExitActions.Count)
            {
                Reasons.Add($"Goal {goal.Key} EXIT action count mismatch; local {goal.Value.ExitActions.Count}, remote {remoteGoal.ExitActions.Count}");
            }

            // TODO - check INIT/EXIT actions func, arity, goal id
        }

        foreach (var db in DebugInfo.Databases)
        {
            var remoteDb = Databases[db.Key];
            if (remoteDb.ArgumentType.Count != db.Value.ParamTypes.Count)
            {
                Reasons.Add($"DB {db.Key} arity mismatch; local {db.Value.ParamTypes.Count}, remote {remoteDb.ArgumentType.Count}");
            }
            else
            {
                for (var i = 0; i < db.Value.ParamTypes.Count; i++)
                {
                    var localType = db.Value.ParamTypes[i];
                    var remoteType = remoteDb.ArgumentType[i];
                    if (localType != remoteType)
                    {
                        Reasons.Add($"DB {db.Key} arg {i} mismatch; local {localType}, remote {remoteType}");
                    }
                }
            }
        }

        Dictionary<UInt32, UInt32> ruleIdToIndexMap = new Dictionary<uint, UInt32>();

        foreach (var node in DebugInfo.Nodes)
        {
            var remoteNode = Nodes[node.Key];
            if ((Node.Type)remoteNode.Type != node.Value.Type)
            {
                Reasons.Add($"Node {node.Key} type mismatch; local {node.Value.Type}, remote {remoteNode.Type}");
            }

            if (remoteNode.Name != node.Value.Name
                && remoteNode.Name != node.Value.Name + "__DEF__")
            {
                Reasons.Add($"Node {node.Key} name mismatch; local {node.Value.Name}, remote {remoteNode.Name}");
            }

            if (node.Value.RuleId != 0)
            {
                ruleIdToIndexMap[node.Value.RuleId] = node.Key;
            }
        }

        foreach (var ruleMapping in ruleIdToIndexMap)
        {
            var localRule = DebugInfo.Rules[ruleMapping.Key];
            var remoteRule = Rules[ruleMapping.Value];

            if (remoteRule.Actions.Count != localRule.Actions.Count)
            {
                Reasons.Add($"Rule {ruleMapping.Value} action count mismatch; local {localRule.Actions.Count}, remote {remoteRule.Actions.Count}");
            }

            // TODO - check actions func, arity, goal id
        }

        Matches = (Reasons.Count == 0);
    }
}
