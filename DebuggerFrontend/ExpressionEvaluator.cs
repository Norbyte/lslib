using LSLib.DebuggerFrontend.ExpressionParser;
using LSLib.LS.Story;
using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LSTools.DebuggerFrontend;

class PendingExpressionEvaluation
{
    public DAPRequest Request;
    public EvaluationResults Results;
    public NodeDebugInfo Node;
    public FunctionDebugInfo Function;
}

class ExpressionEvaluator
{
    private StoryDebugInfo DebugInfo;
    private DebuggerClient DbgClient;
    private DAPStream DAP;
    private Dictionary<FunctionNameAndArity, NodeDebugInfo> NameToNodeMap;
    public DatabaseEnumerator DatabaseDumper;
    private EvaluationResultManager EvalResults;
    private Dictionary<UInt32, PendingExpressionEvaluation> PendingEvaluations = new Dictionary<UInt32, PendingExpressionEvaluation>();

    public ExpressionEvaluator(StoryDebugInfo debugInfo, DAPStream dap, DebuggerClient dbgClient, ValueFormatter formatter,
        EvaluationResultManager results)
    {
        DebugInfo = debugInfo;
        DbgClient = dbgClient;
        DAP = dap;
        DatabaseDumper = new DatabaseEnumerator(dbgClient, dap, debugInfo, formatter, results);
        EvalResults = results;

        DbgClient.OnEvaluateRow = this.OnEvaluateRow;
        DbgClient.OnEvaluateFinished = this.OnEvaluateFinished;

        MakeFunctionNameMap();
    }


    private void MakeFunctionNameMap()
    {
        NameToNodeMap = new Dictionary<FunctionNameAndArity, NodeDebugInfo>();
        foreach (var node in DebugInfo.Nodes)
        {
            if (node.Value.FunctionName != null)
            {
                NodeDebugInfo existingNode;
                // Make sure that we don't overwrite user queries with their PROC equivalents
                if (NameToNodeMap.TryGetValue(node.Value.FunctionName, out existingNode))
                {
                    if (existingNode.Type != Node.Type.UserQuery)
                    {
                        NameToNodeMap[node.Value.FunctionName] = node.Value;
                    }
                }
                else
                {
                    NameToNodeMap.Add(node.Value.FunctionName, node.Value);
                }
            }
        }
    }


    private MsgTypedValue ConstantToTypedValue(ConstantValue c)
    {
        var tv = new MsgTypedValue();
        // TODO - c.TypeName?
        switch (c.Type)
        {
            case IRConstantType.Integer:
                tv.TypeId = (UInt32)Value.Type.Integer;
                tv.Intval = c.IntegerValue;
                break;

            case IRConstantType.Float:
                tv.TypeId = (UInt32)Value.Type.Float;
                tv.Floatval = c.FloatValue;
                break;

            case IRConstantType.String:
                tv.TypeId = (UInt32)Value.Type.String;
                tv.Stringval = c.StringValue;
                break;

            case IRConstantType.Name:
                tv.TypeId = (UInt32)Value.Type.GuidString;
                tv.Stringval = c.StringValue;
                break;

            default:
                throw new ArgumentException("Constant has unknown type");
        }

        return tv;
    }


    private MsgTypedValue VariableToTypedValue(LocalVar lvar, CoalescedFrame frame)
    {
        // TODO - lvar.Type?
        if (lvar.Name == "_")
        {
            var tv = new MsgTypedValue();
            tv.TypeId = (UInt32)Value.Type.None;
            return tv;
        }
        else
        {
            var frameVar = frame.Variables.FirstOrDefault(v => v.Name == lvar.Name);
            if (frameVar == null)
            {
                throw new RequestFailedException($"Variable does not exist: \"{lvar.Name}\"");
            }

            return frameVar.TypedValue;
        }
    }


    private MsgTuple ParamsToTuple(IEnumerable<RValue> args, CoalescedFrame frame)
    {
        var tuple = new MsgTuple();
        foreach (var arg in args)
        {
            if (arg is ConstantValue)
            {
                tuple.Column.Add(ConstantToTypedValue(arg as ConstantValue));
            }
            else
            {
                if (frame != null)
                {
                    tuple.Column.Add(VariableToTypedValue(arg as LocalVar, frame));
                }
                else
                {
                    throw new RequestFailedException("Local variables cannot be referenced without a stack frame");
                }
            }
        }

        return tuple;
    }

    public void EvaluateCall(DAPRequest request, Statement stmt, CoalescedFrame frame, bool allowMutation)
    {
        NodeDebugInfo node;
        var func = new FunctionNameAndArity(stmt.Name, stmt.Params.Count);
        if (!NameToNodeMap.TryGetValue(func, out node))
        {
            DAP.SendReply(request, "Name not found: " + func);
            return;
        }

        var function = DebugInfo.Functions[node.FunctionName];
        var args = ParamsToTuple(stmt.Params, frame);

        DbgEvaluate.Types.EvalType evalType;
        switch (node.Type)
        {
            case Node.Type.Database:
                if (stmt.Not)
                {
                    evalType = DbgEvaluate.Types.EvalType.Insert;
                }
                else
                {
                    evalType = DbgEvaluate.Types.EvalType.Delete;
                }
                break;

            case Node.Type.Proc:
                if (stmt.Not)
                {
                    throw new RequestFailedException("\"NOT\" statements not supported for PROCs");
                }

                evalType = DbgEvaluate.Types.EvalType.Insert;
                break;

            case Node.Type.DivQuery:
            case Node.Type.InternalQuery:
            case Node.Type.UserQuery:
                if (stmt.Not)
                {
                    throw new RequestFailedException("\"NOT\" statements not supported for QRYs");
                }

                evalType = DbgEvaluate.Types.EvalType.IsValid;
                break;

            default:
                throw new RequestFailedException($"Eval node type not supported: {node.Type}");
        }

        if ((evalType != DbgEvaluate.Types.EvalType.IsValid
            || node.Type == Node.Type.UserQuery)
            && !allowMutation)
        {
            throw new RequestFailedException($"Evaluation could cause game state change");
        }
        
        UInt32 seq = DbgClient.SendEvaluate(evalType, node.Id, args);

        var argNames = function.Params.Select(arg => arg.Name).ToList();
        var eval = new PendingExpressionEvaluation
        {
            Request = request,
            Results = EvalResults.MakeResults(function.Params.Count, argNames),
            Node = node,
            Function = function
        };
        PendingEvaluations.Add(seq, eval);
    }

    public void EvaluateName(DAPRequest request, string name, bool allowMutation)
    {
        if (name == "help")
        {
            SendUsage();
            return;
        }

        // TODO - this is bad for performance!
        var db = DebugInfo.Databases.Values.FirstOrDefault(r => r.Name == name);
        if (db == null)
        {
            throw new RequestFailedException($"Database does not exist: \"{name}\"");
        }

        DatabaseDumper.RequestDatabaseEvaluation(request, db.Id);
    }

    private Statement Parse(string expression)
    {
        var exprBytes = Encoding.UTF8.GetBytes(expression);
        using (var exprStream = new MemoryStream(exprBytes))
        {
            var scanner = new ExpressionScanner();
            scanner.SetSource(exprStream);
            var parser = new ExpressionParser(scanner);
            bool parsed = parser.Parse();

            if (parsed)
            {
                return parser.GetStatement();
            }
            else
            {
                return null;
            }
        }
    }

    private void SendUsage()
    {
        string usageText = $@"Basic Usage:
    Dump the contents of a database: DB_Database
    Insert a row into a database (EXPERIMENTAL!): DB_Database(1, 2, 3)
    Delete a row from a database (EXPERIMENTAL!): NOT DB_Database(4, 5, 6)
    Evaluate a query: QRY_Query(""test"")
    Evaluate a built-in query: IntegerSum(100, 200, _)
    Call a PROC: PROC_Proc(111.0, TEST_12345678-1234-1234-1234-123456789abc)
    Call a built-in call (NOT YET COMPLETE!): SetStoryEvent(...)
    Trigger an event: GameStarted(""FTJ_FortJoy"", 1)

Notes:
    - Built-in queries will return their output if they succeed.
    - You can use local variables from the active rule (_Char, etc.) in the expressions.
";

        var outputMsg = new DAPOutputMessage
        {
            category = "console",
            output = usageText
        };
        DAP.SendEvent("output", outputMsg);
    }

    public void Evaluate(DAPRequest request, string expression, CoalescedFrame frame, bool allowMutation)
    {
        var stmt = Parse(expression);
        if (stmt == null)
        {
            DAP.SendReply(request, "Syntax error. Type \"help\" for usage.");
            return;
        }

        if (stmt.Params == null)
        {
            EvaluateName(request, stmt.Name, allowMutation);
        }
        else
        {
            EvaluateCall(request, stmt, frame, allowMutation);
        }
    }

    private void OnEvaluateRow(UInt32 seq, BkEvaluateRow msg)
    {
        var results = PendingEvaluations[seq].Results;
        foreach (var row in msg.Row)
        {
            results.Add(row);
        }
    }

    private void OnEvaluateFinished(UInt32 seq, BkEvaluateFinished msg)
    {
        var eval = PendingEvaluations[seq];

        if (msg.ResultCode != StatusCode.Success)
        {
            DAP.SendReply(eval.Request, $"Evaluation failed: DBG server sent error code: {msg.ResultCode}");
            return;
        }

        var funcType = (LSLib.LS.Story.FunctionType)eval.Function.TypeId;
        if (eval.Node.Type == Node.Type.UserQuery)
        {
            funcType = LSLib.LS.Story.FunctionType.UserQuery;
        }

        string resultText = "";
        string consoleText = "";
        bool returnResults;
        switch (funcType)
        {
            case LSLib.LS.Story.FunctionType.Event:
                consoleText = $"Event {eval.Node.FunctionName} triggered";
                returnResults = false;
                break;

            case LSLib.LS.Story.FunctionType.Query:
            case LSLib.LS.Story.FunctionType.SysQuery:
            case LSLib.LS.Story.FunctionType.UserQuery:
                if (msg.QuerySucceeded)
                {
                    consoleText = $"Query {eval.Node.FunctionName} SUCCEEDED";
                }
                else
                {
                    consoleText = $"Query {eval.Node.FunctionName} FAILED";
                }

                resultText = "Query results";
                returnResults = (funcType != LSLib.LS.Story.FunctionType.UserQuery);
                break;

            case LSLib.LS.Story.FunctionType.Proc:
                consoleText = $"PROC {eval.Node.FunctionName} called";
                returnResults = false;
                break;

            case LSLib.LS.Story.FunctionType.SysCall:
            case LSLib.LS.Story.FunctionType.Call:
                consoleText = $"Built-in function {eval.Node.FunctionName} called";
                returnResults = false;
                break;

            case LSLib.LS.Story.FunctionType.Database:
                consoleText = $"Inserted row into {eval.Node.FunctionName}";
                returnResults = false;
                break;

            default:
                throw new InvalidOperationException($"Unknown function type: {eval.Function.TypeId}");
        }

        if (consoleText.Length > 0)
        {
            var outputMsg = new DAPOutputMessage
            {
                category = "console",
                output = consoleText + "\r\n"
            };
            DAP.SendEvent("output", outputMsg);
        }

        if (funcType == LSLib.LS.Story.FunctionType.Database)
        {
            // For database inserts we'll return the whole database in the response.
            DatabaseDumper.RequestDatabaseEvaluation(eval.Request, eval.Node.DatabaseId);
            return;
        }

        var evalResponse = new DAPEvaluateResponse
        {
            result = resultText,
            namedVariables = 0,
            indexedVariables = returnResults ? eval.Results.Count : 0,
            variablesReference = returnResults ? eval.Results.VariablesReference : 0
        };

        DAP.SendReply(eval.Request, evalResponse);

        PendingEvaluations.Remove(seq);
    }
}
