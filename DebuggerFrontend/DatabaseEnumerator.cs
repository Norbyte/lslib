using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSTools.DebuggerFrontend
{
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
        // Databases that we'll have to send to the debugger after receipt
        private Dictionary<UInt32, List<DAPRequest>> PendingDatabaseRequests = new Dictionary<UInt32, List<DAPRequest>>();
        // Database contents that we're receiving from the backend
        private Dictionary<UInt32, List<MsgTuple>> DatabaseContents = new Dictionary<UInt32, List<MsgTuple>>();

        public DatabaseEnumerator(DebuggerClient dbgClient, DAPStream dap, StoryDebugInfo debugInfo, ValueFormatter formatter)
        {
            DebugInfo = debugInfo;
            DAP = dap;
            DbgClient = dbgClient;
            Formatter = formatter;

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
                DatabaseContents[databaseId] = new List<MsgTuple>();
            }

            requests.Add(request);

            DbgClient.SendGetDatabaseContents(databaseId);
        }

        public List<DAPVariable> GetVariables(DAPVariablesRequest msg, long variablesReference)
        {
            long variableType = (variablesReference >> 48);
            if (variableType == 1)
            {
                int databaseIndex = (int)(variablesReference & 0xffffff);
                return GetDatabaseRows( msg, databaseIndex);
            }
            else if (variableType == 2)
            {
                int databaseIndex = (int)(msg.variablesReference & 0xffffff);
                int rowIndex = (int)((msg.variablesReference >> 24) & 0xffffff);
                return GetDatabaseRow(msg, databaseIndex, rowIndex);
            }
            else
            {
                throw new InvalidOperationException($"DatabaseEnumerator does not support this variable type: {variableType}");
            }
        }

        private List<DAPVariable> GetDatabaseRows(DAPVariablesRequest msg, int databaseId)
        {
            if (databaseId <= 0 || databaseId > DebugInfo.Databases.Count)
            {
                throw new RequestFailedException($"Requested variables for unknown database {databaseId}");
            }

            var database = DebugInfo.Databases[(uint)databaseId];
            var rows = DatabaseContents[(uint)databaseId];

            int startIndex = msg.start == null ? 0 : (int)msg.start;
            int numVars = (msg.count == null || msg.count == 0) ? rows.Count : (int)msg.count;
            int lastIndex = Math.Min(startIndex + numVars, rows.Count);
            // TODO req.filter, format

            var variables = new List<DAPVariable>();
            for (var i = startIndex; i < startIndex + numVars; i++)
            {
                var row = rows[i];
                var dapVar = new DAPVariable
                {
                    name = i.ToString(),
                    value = "(" + Formatter.TupleToString(row) + ")",
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
                    variablesReference = ((long)2 << 48) | (i << 24) | databaseId,
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
                    indexedVariables = database.ParamTypes.Count
                };
                variables.Add(dapVar);
            }

            return variables;
        }

        private List<DAPVariable> GetDatabaseRow(DAPVariablesRequest msg, int databaseId, int rowIndex)
        {
            if (databaseId <= 0 || databaseId > DebugInfo.Databases.Count)
            {
                throw new RequestFailedException($"Requested variables for unknown database {databaseId}");
            }

            var database = DebugInfo.Databases[(uint)databaseId];
            var rows = DatabaseContents[(uint)databaseId];

            if (rowIndex < 0 || rowIndex >= rows.Count)
            {
                throw new RequestFailedException($"Requested nonexistent row {rowIndex} in database {databaseId}");
            }

            int startIndex = msg.start == null ? 0 : (int)msg.start;
            int numVars = (msg.count == null || msg.count == 0) ? rows.Count : (int)msg.count;
            int lastIndex = Math.Min(startIndex + numVars, rows.Count);
            // TODO req.filter, format

            var row = rows[rowIndex];
            var variables = new List<DAPVariable>();
            for (var i = startIndex; i < startIndex + numVars; i++)
            {
                var dapVar = new DAPVariable
                {
                    name = i.ToString(),
                    value = Formatter.ValueToString(row.Column[i])
                };
                variables.Add(dapVar);
            }

            return variables;
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
            evalResponse.variablesReference = ((long)1 << 48) | (long)msg.DatabaseId;

            var requests = PendingDatabaseRequests[msg.DatabaseId];
            foreach (var request in requests)
            {
                DAP.SendReply(request, evalResponse);
            }

            requests.Clear();
        }
    }
}
