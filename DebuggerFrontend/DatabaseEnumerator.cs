using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSTools.DebuggerFrontend;

public class RequestFailedException : Exception
{
    public RequestFailedException(string message)
        : base(message)
    {
    }
}

class DatabaseEnumerator
{
    private StoryDebugInfo DebugInfo;
    DAPStream DAP;
    private DebuggerClient DbgClient;
    private ValueFormatter Formatter;
    private EvaluationResultManager ResultManager;
    // Databases that we'll have to send to the debugger after receipt
    private Dictionary<UInt32, List<DAPRequest>> PendingDatabaseRequests = new Dictionary<UInt32, List<DAPRequest>>();
    // Database contents that we're receiving from the backend
    private Dictionary<UInt32, EvaluationResults> DatabaseContents = new Dictionary<UInt32, EvaluationResults>();

    public DatabaseEnumerator(DebuggerClient dbgClient, DAPStream dap, StoryDebugInfo debugInfo, ValueFormatter formatter,
        EvaluationResultManager resultManager)
    {
        DebugInfo = debugInfo;
        DAP = dap;
        DbgClient = dbgClient;
        Formatter = formatter;
        ResultManager = resultManager;

        DbgClient.OnBeginDatabaseContents = this.OnBeginDatabaseContents;
        DbgClient.OnDatabaseRow = this.OnDatabaseRow;
        DbgClient.OnEndDatabaseContents = this.OnEndDatabaseContents;
    }

    public void RequestDatabaseEvaluation(DAPRequest request, UInt32 databaseId)
    {
        List<DAPRequest> requests;
        if (!PendingDatabaseRequests.TryGetValue(databaseId, out requests))
        {
            requests = new List<DAPRequest>();
            PendingDatabaseRequests[databaseId] = requests;
        }

        if (requests.Count == 0)
        {
            var databaseDebugInfo = DebugInfo.Databases[databaseId];
            DatabaseContents[databaseId] = ResultManager.MakeResults(databaseDebugInfo.ParamTypes.Count);
        }

        requests.Add(request);

        DbgClient.SendGetDatabaseContents(databaseId);
    }

    private void OnBeginDatabaseContents(BkBeginDatabaseContents msg)
    {
    }

    private void OnDatabaseRow(BkDatabaseRow msg)
    {
        var db = DatabaseContents[msg.DatabaseId];
        foreach (var row in msg.Row)
        {
            db.Add(row);
        }
    }

    private void OnEndDatabaseContents(BkEndDatabaseContents msg)
    {
        var rows = DatabaseContents[msg.DatabaseId];
        var db = DebugInfo.Databases[msg.DatabaseId];

        var evalResponse = new DAPEvaluateResponse();
        evalResponse.result = $"Database {db.Name} ({rows.Count} rows)";
        evalResponse.namedVariables = 0;
        evalResponse.indexedVariables = rows.Count;
        evalResponse.variablesReference = rows.VariablesReference;

        var requests = PendingDatabaseRequests[msg.DatabaseId];
        foreach (var request in requests)
        {
            DAP.SendReply(request, evalResponse);
        }

        requests.Clear();
    }
}
