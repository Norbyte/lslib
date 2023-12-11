using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSTools.DebuggerFrontend;

class EvaluationResults
{
    private ValueFormatter Formatter;
    private Int64 VariableIndexPrefix;
    private Int64 VariableReference;
    private Int32 NumColumns;
    private List<MsgTuple> Tuples;
    public List<String> ColumnNames;

    public int Count
    {
        get { return Tuples.Count; }
    }

    public Int64 VariablesReference
    {
        get { return VariableReference; }
    }

    public EvaluationResults(ValueFormatter formatter, Int64 variableReference, 
        Int64 variableIndexPrefix, Int32 numColumns)
    {
        Formatter = formatter;
        VariableReference = variableReference;
        VariableIndexPrefix = variableIndexPrefix;
        NumColumns = numColumns;
        Tuples = new List<MsgTuple>();
    }

    public void Add(MsgTuple tuple)
    {
        Tuples.Add(tuple);
    }

    public List<DAPVariable> GetRows(DAPVariablesRequest msg)
    {
        int startIndex = msg.start == null ? 0 : (int)msg.start;
        int numVars = (msg.count == null || msg.count == 0) ? Tuples.Count : (int)msg.count;
        int lastIndex = Math.Min(startIndex + numVars, Tuples.Count);
        // TODO req.filter, format

        var variables = new List<DAPVariable>();
        for (var i = startIndex; i < startIndex + numVars; i++)
        {
            var row = Tuples[i];
            var dapVar = new DAPVariable
            {
                name = i.ToString(),
                value = "(" + Formatter.TupleToString(row) + ")",
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
                variablesReference = VariableIndexPrefix | i,
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
                indexedVariables = ColumnNames == null ? NumColumns : 0,
                namedVariables = ColumnNames == null ? 0 : NumColumns
            };
            variables.Add(dapVar);
        }

        return variables;
    }

    public List<DAPVariable> GetRow(DAPVariablesRequest msg, int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= Tuples.Count)
        {
            throw new RequestFailedException($"Requested nonexistent row {rowIndex}");
        }

        int startIndex = msg.start == null ? 0 : (int)msg.start;
        int numVars = (msg.count == null || msg.count == 0) ? NumColumns : (int)msg.count;
        int lastIndex = Math.Min(startIndex + numVars, NumColumns);
        // TODO req.filter, format

        var row = Tuples[rowIndex];
        var variables = new List<DAPVariable>();
        for (var i = startIndex; i < startIndex + numVars; i++)
        {
            var dapVar = new DAPVariable
            {
                name = ColumnNames == null ? i.ToString() : ColumnNames[i],
                value = Formatter.ValueToString(row.Column[i])
            };
            variables.Add(dapVar);
        }

        return variables;
    }
}

class EvaluationResultManager
{
    private ValueFormatter Formatter;
    private List<EvaluationResults> Results;

    public EvaluationResultManager(ValueFormatter formatter)
    {
        Formatter = formatter;
        Results = new List<EvaluationResults>();
    }

    public EvaluationResults MakeResults(int numColumns)
    {
        return MakeResults(numColumns, null);
    }

    public EvaluationResults MakeResults(int numColumns, List<String> columnNames)
    {
        var variableRef = ((UInt64)1 << 48) | ((UInt64)Results.Count << 24);
        var variableIndexPrefix = ((UInt64)2 << 48) | ((UInt64)Results.Count << 24);
        var result = new EvaluationResults(Formatter, (Int64)variableRef, (Int64)variableIndexPrefix, numColumns);
        result.ColumnNames = columnNames;
        Results.Add(result);
        return result;
    }

    public List<DAPVariable> GetVariables(DAPVariablesRequest msg, long variablesReference)
    {
        long variableType = (variablesReference >> 48);
        if (variableType == 1)
        {
            int resultSetIdx = (int)((variablesReference >> 24) & 0xffffff);
            if (resultSetIdx < 0 || resultSetIdx >= Results.Count)
            {
                throw new InvalidOperationException($"Evaluation result set ID does not exist {resultSetIdx}");
            }

            return Results[resultSetIdx].GetRows(msg);
        }
        else if (variableType == 2)
        {
            int resultSetIdx = (int)((variablesReference >> 24) & 0xffffff);
            if (resultSetIdx < 0 || resultSetIdx >= Results.Count)
            {
                throw new InvalidOperationException($"Evaluation result set ID does not exist {resultSetIdx}");
            }

            int rowIndex = (int)(msg.variablesReference & 0xffffff);
            return Results[resultSetIdx].GetRow(msg, rowIndex);
        }
        else
        {
            throw new InvalidOperationException($"EvaluationResultManager does not support this variable type: {variableType}");
        }
    }
}
