using Newtonsoft.Json;

namespace LSLib.LS.Story;

public class StoryDebugExportVisitor
{
    private Stream stream;
    private JsonTextWriter writer;

    public StoryDebugExportVisitor(Stream outputStream)
    {
        stream = outputStream;
    }

    public void Visit(Story story)
    {
        using (var streamWriter = new StreamWriter(stream))
        using (this.writer = new JsonTextWriter(streamWriter))
        {
            writer.IndentChar = '\t';
            writer.Indentation = 1;
            writer.Formatting = Newtonsoft.Json.Formatting.Indented;

            writer.WriteStartObject();

            writer.WritePropertyName("types");
            writer.WriteStartObject();
            foreach (var type in story.Types)
            {
                writer.WritePropertyName(type.Key.ToString());
                Visit(type.Value);
            }
            writer.WriteEndObject();

            writer.WritePropertyName("objects");
            writer.WriteStartObject();
            foreach (var obj in story.DivObjects)
            {
                writer.WritePropertyName(obj.Name);
                Visit(obj);
            }
            writer.WriteEndObject();

            writer.WritePropertyName("functions");
            writer.WriteStartObject();
            Int32 funcId = 1;
            foreach (var fun in story.Functions)
            {
                writer.WritePropertyName(funcId.ToString());
                funcId++;
                Visit(fun);
            }
            writer.WriteEndObject();

            writer.WritePropertyName("nodes");
            writer.WriteStartObject();
            foreach (var node in story.Nodes)
            {
                writer.WritePropertyName(node.Key.ToString());
                writer.WriteStartObject();
                VisitNode(node.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();

            writer.WritePropertyName("adapters");
            writer.WriteStartObject();
            foreach (var adapter in story.Adapters)
            {
                writer.WritePropertyName(adapter.Key.ToString());
                Visit(adapter.Value);
            }
            writer.WriteEndObject();

            writer.WritePropertyName("databases");
            writer.WriteStartObject();
            foreach (var database in story.Databases)
            {
                writer.WritePropertyName(database.Key.ToString());
                Visit(database.Value);
            }
            writer.WriteEndObject();

            writer.WritePropertyName("goals");
            writer.WriteStartObject();
            foreach (var goal in story.Goals)
            {
                writer.WritePropertyName(goal.Key.ToString());
                Visit(goal.Value);
            }
            writer.WriteEndObject();

            writer.WriteEndObject();
        }
    }

    public void Visit(OsirisType type)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("name");
        writer.WriteValue(type.Name);
        writer.WriteEndObject();
    }

    public void Visit(OsirisDivObject obj)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("name");
        writer.WriteValue(obj.Name);
        writer.WritePropertyName("type");
        writer.WriteValue(obj.Type);
        writer.WriteEndObject();
    }

    public void Visit(NodeReference r)
    {
        if (r.IsNull)
            writer.WriteNull();
        else
            writer.WriteValue(r.Index);
    }

    public void Visit(FunctionSignature fun)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("name");
        writer.WriteValue(fun.Name);
        writer.WritePropertyName("out");
        writer.WriteValue(fun.OutParamMask[0]);
        writer.WritePropertyName("params");
        Visit(fun.Parameters);
        writer.WriteEndObject();
    }

    public void Visit(Function fun)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("signature");
        Visit(fun.Name);
        writer.WritePropertyName("type");
        writer.WriteValue(fun.Type.ToString());
        writer.WritePropertyName("ref");
        Visit(fun.NodeRef);
        writer.WriteEndObject();
    }

    public void VisitNode(Node node)
    {
        if (node is RelOpNode)
            Visit(node as RelOpNode);
        else if (node is RuleNode)
            Visit(node as RuleNode);
        //else if (node is RelNode)
        //    Visit(node as RelNode);
        else if (node is UserQueryNode)
            Visit(node as QueryNode);
        else if (node is InternalQueryNode)
            Visit(node as QueryNode);
        else if (node is DivQueryNode)
            Visit(node as QueryNode);
        //else if (node is QueryNode)
        //    Visit(node as QueryNode);
        else if (node is AndNode)
            Visit(node as JoinNode);
        else if (node is NotAndNode)
            Visit(node as JoinNode);
        //else if (node is JoinNode)
        //    Visit(node as JoinNode);
        //else if (node is TreeNode)
        //    Visit(node as TreeNode);
        else if (node is ProcNode)
            Visit(node as DataNode);
        else if (node is DatabaseNode)
            Visit(node as DataNode);
        // else if (node is DataNode)
        //     Visit(node as DataNode);
        else
            throw new Exception("Unsupported node type");
    }

    public void Visit(Value val)
    {
        writer.WritePropertyName("type");
        writer.WriteValue(val.TypeId);
        writer.WritePropertyName("value");
        writer.WriteValue(val.ToString());
    }

    public void Visit(TypedValue val)
    {
        Visit(val as Value);
        writer.WritePropertyName("valid");
        writer.WriteValue(val.IsValid);
        writer.WritePropertyName("out");
        writer.WriteValue(val.OutParam);
        writer.WritePropertyName("isType");
        writer.WriteValue(val.IsAType);
    }

    public void Visit(Variable var)
    {
        Visit(var as TypedValue);
        writer.WritePropertyName("index");
        writer.WriteValue(var.Index);
        writer.WritePropertyName("unused");
        writer.WriteValue(var.Unused);
        writer.WritePropertyName("adapted");
        writer.WriteValue(var.Adapted);
        writer.WritePropertyName("name");
        writer.WriteValue(var.VariableName);
    }

    public void VisitVar(Value val)
    {
        writer.WriteStartObject();
        if (val is Variable)
            Visit(val as Variable);
        else if (val is TypedValue)
            Visit(val as TypedValue);
        else
            Visit(val);
        writer.WriteEndObject();
    }

    public void Visit(AdapterReference r)
    {
        if (r.IsNull)
            writer.WriteNull();
        else
            writer.WriteValue(r.Index);
    }

    public void Visit(DatabaseReference r)
    {
        if (r.IsNull)
            writer.WriteNull();
        else
            writer.WriteValue(r.Index);
    }

    public void Visit(GoalReference r)
    {
        if (r.IsNull)
            writer.WriteNull();
        else
            writer.WriteValue(r.Index);
    }

    public void Visit(NodeEntryItem entry)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("node");
        Visit(entry.NodeRef);
        writer.WritePropertyName("entry");
        writer.WriteValue(entry.EntryPoint);
        writer.WritePropertyName("goal");
        Visit(entry.GoalRef);
        writer.WriteEndObject();
    }

    public void Visit(RelOpNode node)
    {
        Visit(node as RelNode);
        writer.WritePropertyName("op");
        writer.WriteValue(node.RelOp.ToString());
        writer.WritePropertyName("left");
        VisitVar(node.LeftValue);
        writer.WritePropertyName("leftIndex");
        writer.WriteValue(node.LeftValueIndex);
        writer.WritePropertyName("right");
        VisitVar(node.RightValue);
        writer.WritePropertyName("rightIndex");
        writer.WriteValue(node.RightValueIndex);
    }

    public void Visit(RuleNode node)
    {
        Visit(node as RelNode);
        writer.WritePropertyName("calls");
        writer.WriteStartArray();
        foreach (var call in node.Calls)
        {
            Visit(call);
        }
        writer.WriteEndArray();

        writer.WritePropertyName("variables");
        writer.WriteStartArray();
        foreach (var v in node.Variables)
        {
            VisitVar(v);
        }
        writer.WriteEndArray();

        writer.WritePropertyName("line");
        writer.WriteValue(node.Line);
        writer.WritePropertyName("query");
        writer.WriteValue(node.IsQuery);
    }

    public void Visit(RelNode node)
    {
        Visit(node as TreeNode);
        writer.WritePropertyName("parent");
        Visit(node.ParentRef);
        writer.WritePropertyName("adapter");
        Visit(node.AdapterRef);
        writer.WritePropertyName("databaseNode");
        Visit(node.RelDatabaseNodeRef);
        writer.WritePropertyName("databaseJoin");
        Visit(node.RelJoin);
        writer.WritePropertyName("databaseIndirection");
        writer.WriteValue(node.RelDatabaseIndirection);
    }

    public void Visit(TreeNode node)
    {
        Visit(node as Node);
        writer.WritePropertyName("next");
        Visit(node.NextNode);
    }

    public void Visit(Node node)
    {
        writer.WritePropertyName("type");
        writer.WriteValue(node.TypeName());
        writer.WritePropertyName("name");
        writer.WriteValue(node.Name);
        writer.WritePropertyName("numParams");
        writer.WriteValue(node.NumParams);
        writer.WritePropertyName("nodeDb");
        Visit(node.DatabaseRef);
    }

    public void Visit(QueryNode node)
    {
        Visit(node as Node);
    }

    public void Visit(JoinNode node)
    {
        Visit(node as Node);

        writer.WritePropertyName("left");
        writer.WriteStartObject();
        writer.WritePropertyName("parent");
        Visit(node.LeftParentRef);
        writer.WritePropertyName("adapter");
        Visit(node.LeftAdapterRef);
        writer.WritePropertyName("databaseNode");
        Visit(node.LeftDatabaseNodeRef);
        writer.WritePropertyName("databaseJoin");
        Visit(node.LeftDatabaseJoin);
        writer.WritePropertyName("databaseIndirection");
        writer.WriteValue(node.LeftDatabaseIndirection);
        writer.WriteEndObject();

        writer.WritePropertyName("right");
        writer.WriteStartObject();
        writer.WritePropertyName("parent");
        Visit(node.RightParentRef);
        writer.WritePropertyName("adapter");
        Visit(node.RightAdapterRef);
        writer.WritePropertyName("databaseNode");
        Visit(node.RightDatabaseNodeRef);
        writer.WritePropertyName("databaseJoin");
        Visit(node.RightDatabaseJoin);
        writer.WritePropertyName("databaseIndirection");
        writer.WriteValue(node.RightDatabaseIndirection);
        writer.WriteEndObject();
    }

    public void Visit(DataNode node)
    {
        Visit(node as Node);

        writer.WritePropertyName("references");
        writer.WriteStartArray();
        foreach (var r in node.ReferencedBy)
        {
            Visit(r);
        }
        writer.WriteEndArray();
    }

    public void Visit(Tuple tuple)
    {
        writer.WriteStartObject();
        var keys = tuple.Logical.Keys.ToArray();
        for (var i = 0; i < tuple.Logical.Count; i++)
        {
            writer.WritePropertyName(keys[i].ToString());
            VisitVar(tuple.Logical[keys[i]]);
        }
        writer.WriteEndObject();
    }

    public void Visit(Adapter adapter)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("constants");
        Visit(adapter.Constants);

        writer.WritePropertyName("logical");
        writer.WriteStartArray();
        foreach (var index in adapter.LogicalIndices)
        {
            writer.WriteValue(index);
        }
        writer.WriteEndArray();

        writer.WritePropertyName("mappings");
        writer.WriteStartObject();
        foreach (var index in adapter.LogicalToPhysicalMap)
        {
            writer.WritePropertyName(index.Key.ToString());
            writer.WriteValue(index.Value);
        }
        writer.WriteEndObject();

        writer.WritePropertyName("output");
        writer.WriteStartArray();
        for (var i = 0; i < adapter.LogicalIndices.Count; i++)
        {
            var index = adapter.LogicalIndices[i];
            // If a logical index is present, emit a column from the input tuple
            if (index != -1)
            {
                writer.WriteValue(String.Format("input[{0}]", index));
            }
            // Otherwise check if a constant is mapped to the specified logical index
            else if (adapter.Constants.Logical.ContainsKey(i))
            {
                var value = adapter.Constants.Logical[i];
                VisitVar(value);
            }
            // If we haven't found a constant, emit a null variable
            else
            {
                writer.WriteNull();
            }
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    public void Visit(ParameterList args)
    {
        writer.WriteStartArray();
        foreach (var arg in args.Types)
        {
            writer.WriteValue(arg);
        }
        writer.WriteEndArray();
    }

    public void Visit(Fact fact)
    {
        writer.WriteStartArray();
        foreach (var val in fact.Columns)
        {
            VisitVar(val);
        }
        writer.WriteEndArray();
    }

    public void Visit(FactCollection facts)
    {
        writer.WriteStartArray();
        foreach (var fact in facts)
        {
            Visit(fact);
        }
        writer.WriteEndArray();
    }

    public void Visit(Database db)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("columns");
        Visit(db.Parameters);
        writer.WritePropertyName("facts");
        Visit(db.Facts);
        writer.WriteEndObject();
    }

    public void Visit(Call call)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("negate");
        writer.WriteValue(call.Negate);
        writer.WritePropertyName("name");
        writer.WriteValue(call.Name);
        if (call.Parameters != null)
        {
            writer.WritePropertyName("params");
            writer.WriteStartArray();
            foreach (var arg in call.Parameters)
            {
                VisitVar(arg);
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }

    public void Visit(List<Call> calls)
    {
        writer.WriteStartArray();
        foreach (var call in calls)
        {
            Visit(call);
        }
        writer.WriteEndArray();
    }

    public void Visit(Goal goal)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("name");
        writer.WriteValue(goal.Name);
        writer.WritePropertyName("sgc");
        writer.WriteValue(goal.SubGoalCombination);
        writer.WritePropertyName("init");
        Visit(goal.InitCalls);
        writer.WritePropertyName("exit");
        Visit(goal.ExitCalls);
        writer.WriteEndObject();
    }
}
