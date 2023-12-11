using LSLib.LS.Story.Compiler;
using QUT.Gppg;
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace LSLib.DebuggerFrontend.ExpressionParser;

internal class ParserConstants
{
    public static CultureInfo ParserCulture = new CultureInfo("en-US");
}

public abstract class ExpressionScanBase : AbstractScanner<ExpressionNode, LexLocation>
{
    protected virtual bool yywrap() { return true; }

    protected Literal MakeLiteral(string lit) => new Literal()
    {
        Lit = lit
    };

    protected Literal MakeString(string lit)
    {
        return MakeLiteral(Regex.Unescape(lit.Substring(1, lit.Length - 2)));
    }
}

public sealed partial class ExpressionScanner : ExpressionScanBase
{
}

public partial class ExpressionParser
{
    public ExpressionParser(ExpressionScanner scnr) : base(scnr)
    {
    }

    public Statement GetStatement()
    {
        return CurrentSemanticValue as Statement;
    }

    private Statement MakeStatement(ExpressionNode name, ExpressionNode paramList, bool not) => new Statement
    {
        Name = (name as Literal).Lit,
        Not = not,
        Params = (paramList as StatementParamList).Params
    };

    private Statement MakeStatement(ExpressionNode name, bool not) => new Statement
    {
        Name = (name as Literal).Lit,
        Not = not
    };

    private StatementParamList MakeParamList() => new StatementParamList();

    private StatementParamList MakeParamList(ExpressionNode param)
    {
        var list = new StatementParamList();
        list.Params.Add(param as RValue);
        return list;
    }

    private StatementParamList MakeParamList(ExpressionNode list, ExpressionNode param)
    {
        var actionParamList = list as StatementParamList;
        actionParamList.Params.Add(param as RValue);
        return actionParamList;
    }

    private LocalVar MakeLocalVar(ExpressionNode varName) => new LocalVar()
    {
        Name = (varName as Literal).Lit
    };

    private LocalVar MakeLocalVar(ExpressionNode typeName, ExpressionNode varName) => new LocalVar()
    {
        Type = (typeName as Literal).Lit,
        Name = (varName as Literal).Lit
    };

    private ConstantValue MakeTypedConstant(ExpressionNode typeName, ExpressionNode constant)
    {
        var c = constant as ConstantValue;
        return new ConstantValue()
        {
            TypeName = (typeName as Literal).Lit,
            Type = c.Type,
            StringValue = c.StringValue,
            FloatValue = c.FloatValue,
            IntegerValue = c.IntegerValue,
        };
    }

    private ConstantValue MakeConstGuidString(ExpressionNode val) => new ConstantValue()
    {
        Type = IRConstantType.Name,
        StringValue = (val as Literal).Lit
    };

    private ConstantValue MakeConstString(ExpressionNode val) => new ConstantValue()
    {
        Type = IRConstantType.String,
        StringValue = (val as Literal).Lit
    };

    private ConstantValue MakeConstInteger(ExpressionNode val) => new ConstantValue()
    {
        Type = IRConstantType.Integer,
        IntegerValue = Int64.Parse((val as Literal).Lit, ParserConstants.ParserCulture.NumberFormat)
    };

    private ConstantValue MakeConstFloat(ExpressionNode val) => new ConstantValue()
    {
        Type = IRConstantType.Float,
        FloatValue = Single.Parse((val as Literal).Lit, ParserConstants.ParserCulture.NumberFormat)
    };
}