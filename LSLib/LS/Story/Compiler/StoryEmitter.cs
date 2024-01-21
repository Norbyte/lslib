using LSLib.LS.Story.GoalParser;
using System.Diagnostics;

namespace LSLib.LS.Story.Compiler;

/// <summary>
/// Type of reference being made to a named function.
/// </summary>
public enum NameRefType
{
    // Function is not referenced, only emitted
    None,
    // Function is referenced in the IF part of a rule
    Condition,
    // Function referenced in the THEN part of a rule, or init/exit section of a goal
    Action
}

public class StoryEmitter
{
    private CompilationContext Context;
    private Story Story;
    private Dictionary<IRGoal, Goal> Goals = new Dictionary<IRGoal, Goal>();
    private Dictionary<FunctionNameAndArity, Database> Databases = new Dictionary<FunctionNameAndArity, Database>();
    private Dictionary<FunctionNameAndArity, Node> Funcs = new Dictionary<FunctionNameAndArity, Node>();
    private Dictionary<FunctionNameAndArity, Function> FuncEntries = new Dictionary<FunctionNameAndArity, Function>();
    private Dictionary<IRRule, RuleNode> Rules = new Dictionary<IRRule, RuleNode>();
    public StoryDebugInfo DebugInfo;

    public StoryEmitter(CompilationContext context)
    {
        Context = context;
    }

    public void EnableDebugInfo()
    {
        DebugInfo = new StoryDebugInfo();
        DebugInfo.Version = StoryDebugInfo.CurrentVersion;
    }

    private void AddStoryTypes()
    {
        foreach (var type in Context.TypesById)
        {
            var osiType = new OsirisType();
            osiType.Index = (byte)type.Value.TypeId;
            if (type.Value.TypeId == (uint)type.Value.IntrinsicTypeId)
            {
                osiType.Alias = (byte)0;
                osiType.IsBuiltin = true;
            }
            else
            {
                osiType.Alias = (byte)type.Value.IntrinsicTypeId;
                osiType.IsBuiltin = false;
            }

            osiType.Name = type.Value.Name;
            Story.Types.Add(osiType.Index, osiType);
        }
    }

    private TypedValue EmitTypedValue(IRConstant constant)
    {
        var osiValue = new TypedValue
        {
            TypeId = constant.Type.TypeId,
            IntValue = (int)constant.IntegerValue,
            Int64Value = constant.IntegerValue,
            FloatValue = constant.FloatValue,
            StringValue = constant.StringValue,

            IsValid = true,
            OutParam = false,
            IsAType = false
        };

        return osiValue;
    }

    private TypedValue EmitTypedValue(IRValue val)
    {
        if (val is IRVariable)
        {
            var variable = val as IRVariable;
            return new Variable
            {
                TypeId = val.Type.TypeId,
                IsValid = false,
                OutParam = false,
                IsAType = true,
                Index = (sbyte)variable.Index,
                Unused = false,
                Adapted = true
            };
        }
        else
        {
            return EmitTypedValue(val as IRConstant);
        }
    }

    private Value EmitValue(IRConstant constant)
    {
        var osiValue = new Value
        {
            TypeId = constant.Type.TypeId,
            IntValue = (int)constant.IntegerValue,
            Int64Value = constant.IntegerValue,
            FloatValue = constant.FloatValue,
            StringValue = constant.StringValue
        };

        return osiValue;
    }

    private LS.Story.FunctionSignature EmitFunctionSignature(FunctionSignature signature)
    {
        var osiSignature = new LS.Story.FunctionSignature
        {
            Name = signature.Name,
            OutParamMask = new List<byte>(signature.Params.Count / 8 + 1),
            Parameters = new ParameterList
            {
                Types = new List<uint>(signature.Params.Count)
            }
        };

        var outParamBytes = ((signature.Params.Count + 7) & ~7) >> 3;
        for (var outByte = 0; outByte < outParamBytes; outByte++)
        {
            byte outParamByte = 0;
            for (var i = outByte * 8; i < Math.Min((outByte + 1) * 8, signature.Params.Count); i++)
            {
                if (signature.Params[i].Direction == ParamDirection.Out)
                {
                    outParamByte |= (byte)(0x80 >> (i & 7));
                }
            }

            osiSignature.OutParamMask.Add(outParamByte);
        }

        foreach (var param in signature.Params)
        {
            osiSignature.Parameters.Types.Add(param.Type.TypeId);
        }

        return osiSignature;
    }

    private void AddNodeDebugInfo(Node node, CodeLocation location, Int32 numColumns, IRRule rule)
    {
        if (DebugInfo != null)
        {
            var nodeDebug = new NodeDebugInfo
            {
                Id = node.Index,
                RuleId = 0,
                Line = location != null ? location.StartLine : 0,
                ColumnToVariableMaps = new Dictionary<Int32, Int32>(),
                DatabaseId = node.DatabaseRef.Index,
                Name = node.Name,
                Type = node.NodeType(),
                ParentNodeId = 0
            };

            if (node is JoinNode)
            {
                nodeDebug.ParentNodeId = (node as JoinNode).LeftParentRef.Index;
            }
            else if (node is RelNode)
            {
                nodeDebug.ParentNodeId = (node as RelNode).ParentRef.Index;
            }

            if (node.Name != "")
            {
                nodeDebug.FunctionName = new FunctionNameAndArity(node.Name, node.NumParams);
            }

            if (location != null)
            {
                var columnIndex = 0;
                var variableIndex = 0;
                while (columnIndex < numColumns)
                {
                    if (!rule.Variables[variableIndex].IsUnused())
                    {
                        nodeDebug.ColumnToVariableMaps.Add(columnIndex, variableIndex);
                        columnIndex++;
                    }

                    variableIndex++;
                }
            }

            DebugInfo.Nodes.Add(nodeDebug.Id, nodeDebug);
        }
    }

    private void AddNodeWithoutDebugInfo(Node node)
    {
        node.Index = (uint)Story.Nodes.Count + 1;
        Story.Nodes.Add(node.Index, node);
    }

    private void AddNode(Node node)
    {
        AddNodeWithoutDebugInfo(node);
        AddNodeDebugInfo(node, null, 0, null);
    }

    private Function EmitFunction(LS.Story.FunctionType type, FunctionSignature signature, NodeReference nodeRef)
    {
        var osiFunc = new Function
        {
            Line = 0,
            ConditionReferences = 0,
            ActionReferences = 0,
            NodeRef = nodeRef,
            Type = type,
            Meta1 = 0,
            Meta2 = 0,
            Meta3 = 0,
            Meta4 = 0,
            Name = EmitFunctionSignature(signature)
        };

        var sig = signature.GetNameAndArity();
        FuncEntries.Add(sig, osiFunc);
        Story.Functions.Add(osiFunc);
        Story.FunctionSignatureMap.Add(sig.Name + "/" + sig.Arity.ToString(), osiFunc);

        if (DebugInfo != null)
        {
            var funcDebug = new FunctionDebugInfo
            {
                Name = osiFunc.Name.Name,
                Params = new List<FunctionParamDebugInfo>(),
                TypeId = (UInt32)osiFunc.Type
            };

            foreach (var param in signature.Params)
            {
                funcDebug.Params.Add(new FunctionParamDebugInfo
                {
                    TypeId = (UInt32)param.Type.IntrinsicTypeId,
                    Name = param.Name,
                    Out = param.Direction == ParamDirection.Out
                });
            }

            DebugInfo.Functions.Add(signature.GetNameAndArity(), funcDebug);
        }

        return osiFunc;
    }

    private Function EmitFunction(LS.Story.FunctionType type, FunctionSignature signature, NodeReference nodeRef, BuiltinFunction builtin)
    {
        var osiFunc = EmitFunction(type, signature, nodeRef);
        osiFunc.Meta1 = builtin.Meta1;
        osiFunc.Meta2 = builtin.Meta2;
        osiFunc.Meta3 = builtin.Meta3;
        osiFunc.Meta4 = builtin.Meta4;
        return osiFunc;
    }

    private InternalQueryNode EmitSysQuery(FunctionSignature signature, NameRefType refType)
    {
        var builtin = Context.LookupName(signature.GetNameAndArity()) as BuiltinFunction;
        InternalQueryNode osiQuery = null;
        if (refType == NameRefType.Condition)
        {
            osiQuery = new InternalQueryNode
            {
                DatabaseRef = new DatabaseReference(),
                Name = signature.Name,
                NumParams = (byte)signature.Params.Count
            };
            AddNode(osiQuery);
        }

        EmitFunction(LS.Story.FunctionType.SysQuery, signature, new NodeReference(Story, osiQuery), builtin);
        return osiQuery;
    }

    private void EmitSysCall(FunctionSignature signature)
    {
        var builtin = Context.LookupName(signature.GetNameAndArity()) as BuiltinFunction;
        EmitFunction(LS.Story.FunctionType.SysCall, signature, new NodeReference(), builtin);
    }

    private ProcNode EmitEvent(FunctionSignature signature, NameRefType refType)
    {
        var builtin = Context.LookupName(signature.GetNameAndArity()) as BuiltinFunction;
        ProcNode osiProc = null;
        if (refType == NameRefType.Condition)
        {
            osiProc = new ProcNode
            {
                DatabaseRef = new DatabaseReference(),
                Name = signature.Name,
                NumParams = (byte)signature.Params.Count,
                ReferencedBy = new List<NodeEntryItem>()
            };
            AddNode(osiProc);
        }

        EmitFunction(LS.Story.FunctionType.Event, signature, new NodeReference(Story, osiProc), builtin);
        return osiProc;
    }

    private ProcNode EmitCall(FunctionSignature signature, NameRefType refType)
    {
        var builtin = Context.LookupName(signature.GetNameAndArity()) as BuiltinFunction;
        ProcNode osiProc = null;
        if (refType == NameRefType.Condition)
        {
            osiProc = new ProcNode
            {
                DatabaseRef = new DatabaseReference(),
                Name = signature.Name,
                NumParams = (byte)signature.Params.Count,
                ReferencedBy = new List<NodeEntryItem>()
            };
            AddNode(osiProc);
        }

        EmitFunction(LS.Story.FunctionType.Call, signature, new NodeReference(Story, osiProc), builtin);
        return osiProc;
    }

    private DivQueryNode EmitQuery(FunctionSignature signature, NameRefType refType)
    {
        var builtin = Context.LookupName(signature.GetNameAndArity()) as BuiltinFunction;
        DivQueryNode osiQuery = null;
        if (refType == NameRefType.Condition)
        {
            osiQuery = new DivQueryNode
            {
                DatabaseRef = new DatabaseReference(),
                Name = signature.Name,
                NumParams = (byte)signature.Params.Count
            };
            AddNode(osiQuery);
        }

        EmitFunction(LS.Story.FunctionType.Query, signature, new NodeReference(Story, osiQuery), builtin);
        return osiQuery;
    }

    private ProcNode EmitProc(FunctionSignature signature)
    {
        var osiProc = new ProcNode
        {
            DatabaseRef = new DatabaseReference(),
            Name = signature.Name,
            NumParams = (byte)signature.Params.Count,
            ReferencedBy = new List<NodeEntryItem>()
        };
        AddNode(osiProc);

        EmitFunction(LS.Story.FunctionType.Proc, signature, new NodeReference(Story, osiProc));
        return osiProc;
    }

    private UserQueryNode EmitUserQuery(FunctionSignature signature)
    {
        var osiQuery = new UserQueryNode
        {
            DatabaseRef = new DatabaseReference(),
            Name = signature.Name,
            NumParams = (byte)signature.Params.Count
        };
        AddNode(osiQuery);

        EmitFunction(LS.Story.FunctionType.Database, signature, new NodeReference(Story, osiQuery));
        return osiQuery;
    }

    private DatabaseNode EmitDatabase(FunctionSignature signature)
    {
        var osiDb = new Database
        {
            Index = (uint)Story.Databases.Count + 1,
            Parameters = new ParameterList
            {
                Types = new List<uint>(signature.Params.Count)
            },
            OwnerNode = null,
            FactsPosition = 0
        };

        foreach (var param in signature.Params)
        {
            osiDb.Parameters.Types.Add(param.Type.TypeId);
        }

        osiDb.Facts = new FactCollection(osiDb, Story);
        Story.Databases.Add(osiDb.Index, osiDb);

        var osiDbNode = new DatabaseNode
        {
            DatabaseRef = new DatabaseReference(Story, osiDb),
            Name = signature.Name,
            NumParams = (byte)signature.Params.Count,
            ReferencedBy = new List<NodeEntryItem>()
        };
        AddNode(osiDbNode);

        osiDb.OwnerNode = osiDbNode;

        EmitFunction(LS.Story.FunctionType.Database, signature, new NodeReference(Story, osiDbNode));

        if (DebugInfo != null)
        {
            var dbDebug = new DatabaseDebugInfo
            {
                Id = osiDb.Index,
                Name = signature.Name,
                ParamTypes = new List<uint>()
            };
            foreach (var param in signature.Params)
            {
                dbDebug.ParamTypes.Add(param.Type.TypeId);
            }

            DebugInfo.Databases.Add(dbDebug.Id, dbDebug);
        }

        return osiDbNode;
    }

    private Database EmitIntermediateDatabase(IRRule rule, int tupleSize, Node ownerNode)
    {
        var paramTypes = new List<uint>(tupleSize);
        for (var i = 0; i < tupleSize; i++)
        {
            var param = rule.Variables[i];
            if (!param.IsUnused())
            {
                paramTypes.Add(param.Type.TypeId);
            }
        }

        if (paramTypes.Count == 0)
        {
            return null;
        }

        var osiDb = new Database
        {
            Index = (uint)Story.Databases.Count + 1,
            Parameters = new ParameterList
            {
                Types = paramTypes
            },
            OwnerNode = ownerNode,
            FactsPosition = 0
        };

        osiDb.Facts = new FactCollection(osiDb, Story);
        Story.Databases.Add(osiDb.Index, osiDb);

        if (DebugInfo != null)
        {
            var dbDebug = new DatabaseDebugInfo
            {
                Id = osiDb.Index,
                Name = "",
                ParamTypes = new List<uint>()
            };
            foreach (var paramType in paramTypes)
            {
                dbDebug.ParamTypes.Add(paramType);
            }

            DebugInfo.Databases.Add(dbDebug.Id, dbDebug);
        }

        return osiDb;
    }

    private Node EmitName(FunctionNameAndArity name, NameRefType refType)
    {
        Node node = null;
        if (!Funcs.TryGetValue(name, out node))
        {
            var signature = Context.LookupSignature(name);
            switch (signature.Type)
            {
                case FunctionType.SysQuery: node = EmitSysQuery(signature, refType); break;
                case FunctionType.SysCall: EmitSysCall(signature); break;
                case FunctionType.Event: node = EmitEvent(signature, refType); break;
                case FunctionType.Query: node = EmitQuery(signature, refType); break;
                case FunctionType.Call: node = EmitCall(signature, refType); break;
                case FunctionType.Database: node = EmitDatabase(signature); break;
                case FunctionType.Proc: node = EmitProc(signature); break;
                case FunctionType.UserQuery: node = EmitUserQuery(signature); break;
                default: throw new ArgumentException("Invalid function type");
            }

            Funcs.Add(name, node);
        }

        var func = FuncEntries[name];
        switch (refType)
        {
            case NameRefType.None:
                break;
            case NameRefType.Condition:
                func.ConditionReferences++;
                if (node == null)
                {
                    throw new InvalidOperationException("Tried to emit a condition reference after a node was already generated");
                }
                break;
            case NameRefType.Action:
                func.ActionReferences++;
                break;
        }

        if (node is UserQueryNode)
        {
            // We need to add a reference to the user query definition entry as well
            var defnName = new FunctionNameAndArity(name.Name + "__DEF__", name.Arity);
            if (FuncEntries.TryGetValue(defnName, out Function defn))
            {
                switch (refType)
                {
                    case NameRefType.Condition:
                        defn.ConditionReferences++;
                        break;
                    case NameRefType.Action:
                        defn.ActionReferences++;
                        break;
                }
            }
        }
        
        return node;
    }

    private Call EmitCall(IRFact fact)
    {
        if (fact.Database != null)
        {
            EmitName(fact.Database.Name, NameRefType.Action);

            var osiCall = new Call
            {
                Name = fact.Database.Name.Name,
                Parameters = new List<TypedValue>(fact.Elements.Count),
                Negate = fact.Not,
                // TODO const - InvalidGoalId?
                GoalIdOrDebugHook = 0
            };

            foreach (var param in fact.Elements)
            {
                var osiParam = EmitTypedValue(param);
                osiCall.Parameters.Add(osiParam);
            }

            return osiCall;
        }
        else
        {
            return new Call
            {
                Name = "",
                Parameters = new List<TypedValue>(),
                Negate = false,
                GoalIdOrDebugHook = (int)Goals[fact.Goal].Index
            };
        }
    }

    private Call EmitCall(IRStatement statement)
    {
        if (statement.Goal != null)
        {
            return new Call
            {
                Name = "",
                Parameters = new List<TypedValue>(statement.Params.Count),
                Negate = false,
                GoalIdOrDebugHook = (int)Goals[statement.Goal].Index
            };
        }
        else
        {
            var name = Context.LookupSignature(statement.Func.Name);
            EmitName(statement.Func.Name, NameRefType.Action);
            
            var osiCall = new Call
            {
                Name = statement.Func.Name.Name,
                Parameters = new List<TypedValue>(statement.Params.Count),
                Negate = statement.Not,
                // TODO const - InvalidGoalId?
                // TODO - use statement goal id if available?
                GoalIdOrDebugHook = 0
            };

            foreach (var param in statement.Params)
            {
                var osiParam = EmitTypedValue(param);
                osiCall.Parameters.Add(osiParam);
            }

            return osiCall;
        }
    }

    private void AddJoinTarget(Node node, Node target, EntryPoint entryPoint, Goal goal)
    {
        var targetRef = new NodeEntryItem
        {
            NodeRef = new NodeReference(Story, target),
            EntryPoint = entryPoint,
            GoalRef = new GoalReference(Story, goal)
        };

        if (node is TreeNode)
        {
            var treeNode = node as TreeNode;
            Debug.Assert(treeNode.NextNode == null);
            treeNode.NextNode = targetRef;
        }
        else if (node is DataNode)
        {
            var dataNode = node as DataNode;
            dataNode.ReferencedBy.Add(targetRef);
        }

        if (target is RelNode)
        {
            Debug.Assert(entryPoint == EntryPoint.None);
            var relNode = target as RelNode;
            relNode.ParentRef = new NodeReference(Story, node);
        }
        else
        {
            var joinNode = target as JoinNode;
            if (entryPoint == EntryPoint.Left)
            {
                joinNode.LeftParentRef = new NodeReference(Story, node);
            }
            else
            {
                Debug.Assert(entryPoint == EntryPoint.Right);
                joinNode.RightParentRef = new NodeReference(Story, node);
            }
        }
    }

    private Adapter EmitAdapter()
    {
        var adapter = new Adapter
        {
            Index = (uint)Story.Adapters.Count + 1,
            Constants = new Tuple(),
            LogicalIndices = new List<sbyte>(),
            LogicalToPhysicalMap = new Dictionary<byte, byte>()
        };
        Story.Adapters.Add(adapter.Index, adapter);
        return adapter;
    }

    private Adapter EmitIdentityMappingAdapter(IRRule rule, int tupleSize, bool allowPartialPhysicalRow)
    {
        var adapter = EmitAdapter();

        if (tupleSize > rule.Variables.Count)
        {
            tupleSize = rule.Variables.Count;
        }

        for (var i = 0; i < tupleSize; i++)
        {
            if (rule.Variables[i].IsUnused())
            {
                if (!allowPartialPhysicalRow)
                {
                    adapter.LogicalIndices.Add((sbyte)-1);
                }
            }
            else
            {
                adapter.LogicalIndices.Add((sbyte)i);
                adapter.LogicalToPhysicalMap.Add((byte)i, (byte)(adapter.LogicalIndices.Count - 1));
            }
        }

        return adapter;
    }

    private Adapter EmitJoinAdapter(IRFuncCondition condition, IRRule rule)
    {
        var adapter = EmitAdapter();

        for (var i = 0; i < condition.Params.Count; i++)
        {
            var param = condition.Params[i];
            if (param is IRConstant)
            {
                var osiConst = EmitValue(param as IRConstant);
                adapter.Constants.Physical.Add(osiConst);
                adapter.Constants.Logical.Add(i, osiConst);
                adapter.LogicalIndices.Add((sbyte)-1);
            }
            else
            {
                var variable = param as IRVariable;
                if (rule.Variables[variable.Index].IsUnused())
                {
                    adapter.LogicalIndices.Add((sbyte)-1);
                }
                else
                {
                    adapter.LogicalIndices.Add((sbyte)variable.Index);
                    if (!adapter.LogicalToPhysicalMap.ContainsKey((byte)variable.Index))
                    {
                        adapter.LogicalToPhysicalMap.Add((byte)variable.Index, (byte)(adapter.LogicalIndices.Count - 1));
                    }
                }
            }
        }

        var sortedMap = new Dictionary<byte, byte>(adapter.LogicalToPhysicalMap.Count);
        foreach (var mapping in adapter.LogicalToPhysicalMap.OrderBy(v => v.Key))
        {
            sortedMap.Add(mapping.Key, mapping.Value);
        }
        adapter.LogicalToPhysicalMap = sortedMap;

        return adapter;
    }

    private Adapter EmitNodeAdapter(IRRule rule, IRCondition condition, Node node)
    {
        if (node is DataNode || node is QueryNode)
        {
            return EmitJoinAdapter(condition as IRFuncCondition, rule);
        }
        else if (node is RelOpNode)
        {
            // (node as RelOpNode).AdapterRef.Resolve().LogicalIndices.Count
            return EmitIdentityMappingAdapter(rule, (int)condition.TupleSize, true);
        }
        else if (node is JoinNode)
        {
            return EmitIdentityMappingAdapter(rule, (int)condition.TupleSize, true);
        }
        else
        {
            throw new ArgumentException("Unable to emit an adapter for this node type.");
        }
    }

    private JoinNode EmitJoin(Node left, IRCondition leftCondition, IRFuncCondition rightCondition, IRRule rule, Goal goal, ReferencedDatabaseInfo referencedDb)
    {
        if (referencedDb.DbNodeRef.IsValid)
        {
            referencedDb.Indirection++;
        }

        var right = EmitName(rightCondition.Func.Name, NameRefType.Condition);
        JoinNode osiCall;
        if (rightCondition.Not)
        {
            osiCall = new NotAndNode();
        }
        else
        {
            osiCall = new AndNode();
        }

        var leftAdapter = EmitNodeAdapter(rule, leftCondition, left);
        var rightAdapter = EmitNodeAdapter(rule, rightCondition, right);

        DatabaseReference database;
        Database db = null;
        if (left.DatabaseRef.IsValid && right.DatabaseRef.IsValid)
        {
            db = EmitIntermediateDatabase(rule, (int)rightCondition.TupleSize, null);
            if (db != null)
            {
                database = new DatabaseReference(Story, db);
            }
            else
            {
                database = new DatabaseReference();
            }
        }
        else
        {
            database = new DatabaseReference();
        }

        // VERY TODO
        osiCall.DatabaseRef = database;
        osiCall.Name = "";
        osiCall.NumParams = 0;
        osiCall.LeftParentRef = new NodeReference();
        osiCall.RightParentRef = new NodeReference();
        osiCall.LeftAdapterRef = new AdapterReference(Story, leftAdapter);
        osiCall.RightAdapterRef = new AdapterReference(Story, rightAdapter);
        if (db == null)
        {
            osiCall.LeftDatabaseNodeRef = referencedDb.DbNodeRef;
            osiCall.LeftDatabaseIndirection = referencedDb.Indirection;
            osiCall.LeftDatabaseJoin = referencedDb.JoinRef;
        }
        else
        {
            osiCall.LeftDatabaseNodeRef = new NodeReference();
            osiCall.LeftDatabaseIndirection = 0;
            osiCall.LeftDatabaseJoin = new NodeEntryItem
            {
                NodeRef = new NodeReference(),
                EntryPoint = EntryPoint.None,
                GoalRef = new GoalReference()
            };
        }

        SortedSet<byte> uniqueLogicalIndices = new SortedSet<byte>();
        foreach (var columnIndex in leftAdapter.LogicalToPhysicalMap.Keys)
        {
            uniqueLogicalIndices.Add(columnIndex);
        }

        foreach (var columnIndex in rightAdapter.LogicalToPhysicalMap.Keys)
        {
            uniqueLogicalIndices.Add(columnIndex);
        }
        
        AddNodeWithoutDebugInfo(osiCall);

        if (db != null)
        {
            referencedDb.DbNodeRef = new NodeReference(Story, osiCall);
            referencedDb.Indirection = 0;
            referencedDb.JoinRef = new NodeEntryItem
            {
                NodeRef = new NodeReference(Story, osiCall),
                GoalRef = new GoalReference(Story, goal),
                EntryPoint = EntryPoint.None
            };
        }
        else if (referencedDb.DbNodeRef.IsValid
            && left.DatabaseRef.IsValid)
        {
            referencedDb.JoinRef = new NodeEntryItem
            {
                NodeRef = new NodeReference(Story, osiCall),
                GoalRef = new GoalReference(Story, goal),
                EntryPoint = EntryPoint.Left
            };
            osiCall.LeftDatabaseJoin = referencedDb.JoinRef;
        }

        if (right is DatabaseNode && db == null)
        {
            osiCall.RightDatabaseNodeRef = new NodeReference(Story, right);
            osiCall.RightDatabaseIndirection = 1;
            osiCall.RightDatabaseJoin = new NodeEntryItem
            {
                NodeRef = new NodeReference(Story, osiCall),
                EntryPoint = EntryPoint.Right,
                GoalRef = new GoalReference(Story, goal)
            };
        }
        else
        {
            osiCall.RightDatabaseNodeRef = new NodeReference();
            osiCall.RightDatabaseIndirection = 0;
            osiCall.RightDatabaseJoin = new NodeEntryItem
            {
                NodeRef = new NodeReference(),
                EntryPoint = EntryPoint.None,
                GoalRef = new GoalReference()
            };
        }

        AddJoinTarget(left, osiCall, EntryPoint.Left, goal);
        AddJoinTarget(right, osiCall, EntryPoint.Right, goal);
        
        AddNodeDebugInfo(osiCall, rightCondition.Location, uniqueLogicalIndices.Count, rule);

        if (osiCall.RightDatabaseIndirection != 0
            && osiCall.LeftDatabaseIndirection != 0
            && osiCall.RightDatabaseIndirection < osiCall.LeftDatabaseIndirection)
        {
            referencedDb.DbNodeRef = osiCall.RightDatabaseNodeRef;
            referencedDb.Indirection = osiCall.RightDatabaseIndirection;
            referencedDb.JoinRef = osiCall.RightDatabaseJoin;
        }

        return osiCall;
    }

    private RelOpNode EmitRelOp(IRRule rule, IRBinaryCondition condition, ReferencedDatabaseInfo referencedDb, 
        IRCondition previousCondition, Node previousNode)
    {
        if (referencedDb.DbNodeRef.IsValid)
        {
            referencedDb.Indirection++;
        }

        DatabaseReference database;
        Database db = null;
        if (previousNode.DatabaseRef.IsValid)
        {
            db = EmitIntermediateDatabase(rule, (int)condition.TupleSize, null);
            database = new DatabaseReference(Story, db);
        }
        else
        {
            database = new DatabaseReference();
        }

        var adapter = EmitNodeAdapter(rule, previousCondition, previousNode);
        var osiRelOp = new RelOpNode
        {
            DatabaseRef = database,
            Name = "",
            NumParams = 0,
            
            ParentRef = null,
            AdapterRef = new AdapterReference(Story, adapter),

            RelOp = condition.Op
        };

        if (condition.LValue is IRConstant)
        {
            osiRelOp.LeftValue = EmitValue(condition.LValue as IRConstant);
            osiRelOp.LeftValueIndex = -1;
        }
        else
        {
            osiRelOp.LeftValue = new Value
            {
                TypeId = (uint)Value.Type.None
            };
            osiRelOp.LeftValueIndex = (sbyte)(condition.LValue as IRVariable).Index;
        }

        if (condition.RValue is IRConstant)
        {
            osiRelOp.RightValue = EmitValue(condition.RValue as IRConstant);
            osiRelOp.RightValueIndex = -1;
        }
        else
        {
            osiRelOp.RightValue = new Value
            {
                TypeId = (uint)Value.Type.None
            };
            osiRelOp.RightValueIndex = (sbyte)(condition.RValue as IRVariable).Index;
        }

        if (db != null)
        {
            db.OwnerNode = osiRelOp;

            osiRelOp.RelDatabaseNodeRef = new NodeReference();
            osiRelOp.RelJoin = new NodeEntryItem
            {
                NodeRef = new NodeReference(),
                GoalRef = new GoalReference(),
                EntryPoint = EntryPoint.None
            };
            osiRelOp.RelDatabaseIndirection = 0;
        }
        else
        {
            osiRelOp.RelDatabaseNodeRef = referencedDb.DbNodeRef;
            osiRelOp.RelJoin = referencedDb.JoinRef;
            osiRelOp.RelDatabaseIndirection = referencedDb.Indirection;
        }

        AddNodeWithoutDebugInfo(osiRelOp);

        if (db != null)
        {
            referencedDb.DbNodeRef = new NodeReference(Story, osiRelOp);
            referencedDb.Indirection = 0;
            referencedDb.JoinRef = new NodeEntryItem
            {
                NodeRef = new NodeReference(),
                EntryPoint = EntryPoint.None,
                GoalRef = new GoalReference()
            };
        }

        return osiRelOp;
    }

    private Variable EmitVariable(IRRuleVariable variable)
    {
        return new Variable
        {
            TypeId = variable.Type.TypeId,
            IsValid = false,
            OutParam = false,
            IsAType = true,
            Index = (sbyte)variable.Index,
            Unused = variable.IsUnused(),
            Adapted = !variable.IsUnused(),
            VariableName = variable.Name
        };
    }

    private RuleNode EmitRuleNode(IRRule rule, Goal goal, ReferencedDatabaseInfo referencedDb, IRCondition lastCondition, Node previousNode)
    {
        if (referencedDb.DbNodeRef.IsValid)
        {
            referencedDb.Indirection++;
        }

        DatabaseReference database;
        Database db = null;
        if (previousNode.DatabaseRef.IsValid)
        {
            db = EmitIntermediateDatabase(rule, (int)rule.Variables.Count, null);
            if (db != null)
            {
                database = new DatabaseReference(Story, db);

                // TODO - set Dummy referencedDb
                referencedDb = new ReferencedDatabaseInfo
                {
                    DbNodeRef = new NodeReference(),
                    Indirection = 0,
                    JoinRef = new NodeEntryItem
                    {
                        NodeRef = new NodeReference(),
                        GoalRef = new GoalReference(),
                        EntryPoint = EntryPoint.None
                    }
                };
            }
            else
            {
                database = new DatabaseReference();
            }
        }
        else
        {
            database = new DatabaseReference();
        }

        Adapter adapter = EmitNodeAdapter(rule, lastCondition, previousNode);
        var osiRule = new RuleNode
        {
            DatabaseRef = database,
            Name = "",
            NumParams = 0,

            NextNode = new NodeEntryItem
            {
                NodeRef = new NodeReference(),
                EntryPoint = EntryPoint.None,
                GoalRef = new GoalReference()
            },
            ParentRef = null,
            AdapterRef = new AdapterReference(Story, adapter),
            RelDatabaseNodeRef = referencedDb.DbNodeRef,
            RelJoin = referencedDb.JoinRef,
            RelDatabaseIndirection = referencedDb.Indirection,

            Calls = new List<Call>(rule.Actions.Count),
            Variables = new List<Variable>(rule.Variables.Count),
            Line = 0,
            DerivedGoalRef = new GoalReference(Story, goal),
            IsQuery = (rule.Type == RuleType.Query)
        };

        foreach (var variable in rule.Variables)
        {
            osiRule.Variables.Add(EmitVariable(variable));
        }

        if (db != null)
        {
            db.OwnerNode = osiRule;
        }

        AddNodeWithoutDebugInfo(osiRule);

        if (referencedDb.DbNodeRef.IsValid && referencedDb.Indirection == 1)
        {
            osiRule.RelJoin = new NodeEntryItem
            {
                NodeRef = new NodeReference(Story, osiRule),
                GoalRef = new GoalReference(Story, goal),
                EntryPoint = EntryPoint.None
            };
        }

        return osiRule;
    }

    private void EmitRuleActions(IRRule rule, RuleNode osiRule)
    {
        foreach (var action in rule.Actions)
        {
            osiRule.Calls.Add(EmitCall(action));
        }
    }

    private ProcNode EmitUserQueryDefinition(FunctionSignature signature, Function queryFunc)
    {
        var osiProc = new ProcNode
        {
            DatabaseRef = new DatabaseReference(),
            Name = signature.Name,
            NumParams = (byte)signature.Params.Count,
            ReferencedBy = new List<NodeEntryItem>()
        };
        AddNode(osiProc);

        var aliasedSignature = new FunctionSignature
        {
            FullyTyped = signature.FullyTyped,
            Name = signature.Name + "__DEF__",
            Params = signature.Params,
            Type = signature.Type,
            Inserted = signature.Inserted,
            Deleted = signature.Deleted,
            Read = signature.Read
        };

        var osiFunc = EmitFunction(LS.Story.FunctionType.UserQuery, aliasedSignature, new NodeReference(Story, osiProc));
        if (queryFunc != null)
        {
            osiFunc.ConditionReferences = queryFunc.ConditionReferences;
            osiFunc.ActionReferences = queryFunc.ActionReferences;
        }
        return osiProc;
    }

    private Node EmitUserQueryInitialFunc(IRFuncCondition condition)
    {
        var signature = Context.LookupSignature(condition.Func.Name);
        var name = new FunctionNameAndArity(signature.Name + "__DEF__", signature.Params.Count);
        if (!Funcs.TryGetValue(name, out Node initialFunc))
        {
            Function osiUserQuery = null;
            FuncEntries.TryGetValue(signature.GetNameAndArity(), out osiUserQuery);
            initialFunc = EmitUserQueryDefinition(signature, osiUserQuery);
            Funcs.Add(name, initialFunc);
        }

        return initialFunc;
    }

    private class ReferencedDatabaseInfo
    {
        public NodeReference DbNodeRef = new NodeReference();
        public byte Indirection = 0;
        public NodeEntryItem JoinRef = new NodeEntryItem
        {
            NodeRef = new NodeReference(),
            EntryPoint = EntryPoint.None,
            GoalRef = new GoalReference()
        };
    }

    private RuleNode EmitRule(IRRule rule, Goal goal)
    {
        var referencedDb = new ReferencedDatabaseInfo();
        var initialCall = rule.Conditions[0] as IRFuncCondition;
        Node initialFunc;
        if (rule.Type == RuleType.Query)
        {
            initialFunc = EmitUserQueryInitialFunc(initialCall);
        }
        else
        {
            initialFunc = EmitName(initialCall.Func.Name, NameRefType.Condition);
            if (initialFunc is DatabaseNode)
            {
                referencedDb.Indirection = 0;
                referencedDb.DbNodeRef = new NodeReference(Story, initialFunc);
            }
        }

        var lastConditionNode = initialFunc;
        IRCondition lastCondition = initialCall;
        for (var i = 1; i < rule.Conditions.Count; i++)
        {
            var condition = rule.Conditions[i];
            if (condition is IRBinaryCondition)
            {
                var relOp = EmitRelOp(rule, condition as IRBinaryCondition, referencedDb, lastCondition, lastConditionNode);
                AddJoinTarget(lastConditionNode, relOp, EntryPoint.None, goal);
                AddNodeDebugInfo(relOp, condition.Location, relOp.AdapterRef.Resolve().LogicalToPhysicalMap.Count, rule);
                lastConditionNode = relOp;
            }
            else
            {
                var func = condition as IRFuncCondition;
                var leftFunc = (i == 1) ? initialCall : null;
                var join = EmitJoin(lastConditionNode, lastCondition, func, rule, goal, referencedDb);
                lastConditionNode = join;
            }
            lastCondition = condition;
        }

        var osiRule = EmitRuleNode(rule, goal, referencedDb, lastCondition, lastConditionNode);
        AddJoinTarget(lastConditionNode, osiRule, EntryPoint.None, goal);
        Rules.Add(rule, osiRule);

        var validVariables = rule.Variables.Where(v => !v.IsUnused()).Count();
        AddNodeDebugInfo(osiRule, rule.Location, validVariables, rule);

        if (DebugInfo != null)
        {
            var ruleDebug = new RuleDebugInfo
            {
                Id = osiRule.Index,
                GoalId = (UInt32)Story.Goals.Count,
                Name = (rule.Conditions.First() as IRFuncCondition).Func.Name.ToString(),
                Variables = new List<RuleVariableDebugInfo>(),
                Actions = new List<ActionDebugInfo>(),
                ConditionsStartLine = (uint)rule.Location.StartLine,
                ConditionsEndLine = (uint)rule.Conditions.Last().Location.EndLine,
                ActionsStartLine = (uint)rule.Actions.First().Location.StartLine,
                ActionsEndLine = (uint)rule.Location.EndLine
            };
            
            foreach (var variable in rule.Variables)
            {
                var varDebug = new RuleVariableDebugInfo
                {
                    Index = (UInt32)variable.Index,
                    Name = variable.Name,
                    Type = (UInt32)variable.Type.IntrinsicTypeId,
                    Unused = variable.IsUnused()
                };
                ruleDebug.Variables.Add(varDebug);
            }
            
            foreach (var action in rule.Actions)
            {
                ruleDebug.Actions.Add(new ActionDebugInfo
                {
                    Line = (uint)action.Location.StartLine
                });
            }

            DebugInfo.Rules.Add(ruleDebug.Id, ruleDebug);
        }

        return osiRule;
    }

    private void EmitGoalActions(IRGoal goal, Goal osiGoal)
    {
        foreach (var fact in goal.InitSection)
        {
            var call = EmitCall(fact);
            osiGoal.InitCalls.Add(call);
        }

        foreach (var fact in goal.ExitSection)
        {
            var call = EmitCall(fact);
            osiGoal.ExitCalls.Add(call);
        }
    }

    private Goal EmitGoal(IRGoal goal)
    {
        var osiGoal = new Goal(Story)
        {
            Index = (uint)(Story.Goals.Count + 1),
            Name = goal.Name,
            InitCalls = new List<Call>(goal.InitSection.Count),
            ExitCalls = new List<Call>(goal.ExitSection.Count),
            ParentGoals = new List<GoalReference>(),
            SubGoals = new List<GoalReference>()
        };

        if (goal.ParentTargetEdges.Count > 0)
        {
            // TODO const
            osiGoal.SubGoalCombination = 1; // SGC_AND ?
            osiGoal.Flags = 2; // HasParentGoal flag ?
        }
        else
        {
            osiGoal.SubGoalCombination = 0;
            osiGoal.Flags = 0;
        }

        if (DebugInfo != null)
        {
            string canonicalizedPath;
            if (File.Exists(goal.Location.FileName))
            {
                canonicalizedPath = Path.GetFullPath(goal.Location.FileName);
            }
            else
            {
                canonicalizedPath = goal.Location.FileName;
            }

            var goalDebug = new GoalDebugInfo
            {
                Id = osiGoal.Index,
                Name = goal.Name,
                Path = canonicalizedPath,
                InitActions = new List<ActionDebugInfo>(),
                ExitActions = new List<ActionDebugInfo>()
            };

            foreach (var action in goal.InitSection)
            {
                goalDebug.InitActions.Add(new ActionDebugInfo
                {
                    Line = (uint)action.Location.StartLine
                });
            }

            foreach (var action in goal.ExitSection)
            {
                goalDebug.ExitActions.Add(new ActionDebugInfo
                {
                    Line = (uint)action.Location.StartLine
                });
            }

            DebugInfo.Goals.Add(goalDebug.Id, goalDebug);
        }

        return osiGoal;
    }
    
    private void EmitGoals()
    {
        foreach (var goal in Context.GoalsByName)
        {
            var osiGoal = EmitGoal(goal.Value);
            osiGoal.Index = (uint)Story.Goals.Count + 1;
            Goals.Add(goal.Value, osiGoal);
            Story.Goals.Add(osiGoal.Index, osiGoal);

            foreach (var rule in goal.Value.KBSection)
            {
                var firstNodeIndex = (uint)Story.Nodes.Count + 1;
                var osiRule = EmitRule(rule, osiGoal);

                if (DebugInfo != null)
                {
                    var lastNodeIndex = (uint)Story.Nodes.Count;
                    for (var i = firstNodeIndex; i <= lastNodeIndex; i++)
                    {
                        var osiNode = Story.Nodes[i];
                        if (osiNode is TreeNode 
                            || osiNode is RelNode
                            || i == lastNodeIndex)
                        {
                            DebugInfo.Nodes[i].RuleId = osiRule.Index;
                        }
                    }
                }
            }
        }

        foreach (var goal in Goals)
        {
            EmitGoalActions(goal.Key, goal.Value);
        }
        
        foreach (var rule in Rules)
        {
            EmitRuleActions(rule.Key, rule.Value);
        }
    }

    /// <summary>
    /// Add parent goal/subgoal mapping to the story.
    /// This needs to be done after all goals were generated, as we need the Osiris goal
    /// object ID-s to make goal references.
    /// </summary>
    private void EmitParentGoals()
    {
        foreach (var goal in Context.GoalsByName)
        {
            var osiGoal = Goals[goal.Value];
            foreach (var parent in goal.Value.ParentTargetEdges)
            {
                var parentGoal = Context.LookupGoal(parent.Goal.Name);
                var osiParentGoal = Goals[parentGoal];
                osiGoal.ParentGoals.Add(new GoalReference(Story, osiParentGoal));
                osiParentGoal.SubGoals.Add(new GoalReference(Story, osiGoal));
            }
        }
    }

    /// <summary>
    /// Generates a function entry for each function in the story header that was not referenced
    /// from the story scripts. The Osiris runtime crashes if some functions from the story
    /// header are not included in the final story file.
    /// </summary>
    private void EmitHeaderFunctions()
    {
        foreach (var signature in Context.Signatures)
        {
            if (signature.Value.Type == FunctionType.SysCall
                || signature.Value.Type == FunctionType.SysQuery
                || signature.Value.Type == FunctionType.Call
                || signature.Value.Type == FunctionType.Query
                || signature.Value.Type == FunctionType.Event)
            {
                if (!Funcs.TryGetValue(signature.Key, out Node funcNode))
                {
                    EmitName(signature.Value.GetNameAndArity(), NameRefType.None);
                }
            }
        }
    }

    public Story EmitStory()
    {
        Story = new Story
        {
            MajorVersion = (byte)(OsiVersion.VerLastSupported >> 8),
            MinorVersion = (byte)(OsiVersion.VerLastSupported & 0xff),
            Header = new SaveFileHeader
            {
                Version = "Osiris save file dd. 03/30/17 07:28:20. Version 1.8.",
                BigEndian = false,
                DebugFlags = 0x000C10A0,
                MajorVersion = (byte)(OsiVersion.VerLastSupported >> 8),
                MinorVersion = (byte)(OsiVersion.VerLastSupported & 0xff),
                Unused = 0
            },
            Types = new Dictionary<uint, OsirisType>(),
            DivObjects = new List<OsirisDivObject>(),
            Functions = new List<Function>(),
            Nodes = new Dictionary<uint, Node>(),
            Adapters = new Dictionary<uint, Adapter>(),
            Databases = new Dictionary<uint, Database>(),
            Goals = new Dictionary<uint, Goal>(),
            GlobalActions = new List<Call>(),
            ExternalStringTable = new List<string>(),
            FunctionSignatureMap = new Dictionary<string, Function>()
        };

        // TODO HEADER

        AddStoryTypes();
        EmitGoals();
        EmitHeaderFunctions();
        EmitParentGoals();

        return Story;
    }
}
