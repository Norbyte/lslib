using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;

namespace LSLib.DebuggerFrontend.ExpressionParser;

/// <summary>
/// Base class for all nodes.
/// (This doesn't do anything meaningful, it is needed only to 
/// provide the GPPG parser a semantic value base class.)
/// </summary>
public class ExpressionNode
{
}

/// <summary>
/// Parameter list of an expression.
/// This is discarded during parsing and does not appear in the final tree.
/// </summary>
public class StatementParamList : ExpressionNode
{
    public List<RValue> Params = new List<RValue>();
}

/// <summary>
/// An expression.
/// This is either a PROC call, QRY, or a database insert/delete operation.
/// </summary>
public class Statement : ExpressionNode
{
    // Function name
    public String Name;
    // Statement negation ("DB_Something(1)" vs. "NOT DB_Something(1)").
    public bool Not;
    // List of parameters
    public List<RValue> Params;
}

public class RValue : ExpressionNode
{
}

/// <summary>
/// Constant scalar value.
/// </summary>
public class ConstantValue : RValue
{
    // Type of value, if specified in the code.
    // (e.g. "(INT64)123")
    public String TypeName;
    // Internal type of the constant
    public IRConstantType Type;
    // Value of this constant if the type is Integer.
    public Int64 IntegerValue;
    // Value of this constant if the type is Float.
    public Single FloatValue;
    // Value of this constant if the type is String or Name.
    public String StringValue;
}

/// <summary>
/// Rule-local variable name.
/// (Any variable that begins with an underscore)
/// </summary>
public class LocalVar : RValue
{
    // Type of variable, if specified in the code.
    // (e.g. "(ITEMGUID)_Var")
    public String Type;
    // Name of variable.
    public String Name;
}

/// <summary>
/// String literal from lexing stage (yytext).
/// This is discarded during parsing and does not appear in the final tree.
/// </summary>
public class Literal : ExpressionNode
{
    public String Lit;
}
