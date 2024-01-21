using LSLib.LS.Story.Compiler;

namespace LSLib.LS.Story.HeaderParser;

/// <summary>
/// Base class for all AST nodes.
/// (This doesn't do anything meaningful, it is needed only to 
/// provide the GPPG parser a semantic value base class.)
/// </summary>
public class ASTNode
{
}

/// <summary>
/// Declarations node - contains every declaration from the story header file.
/// </summary>
public class ASTDeclarations : ASTNode
{
    // Debug options
    public List<String> Options = new List<String>();
    // Declared type aliases
    public List<ASTAlias> Aliases = new List<ASTAlias>();
    // Declared functions
    public List<ASTFunction> Functions = new List<ASTFunction>();
}

/// <summary>
/// Function type wrapper node
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
public class ASTFunctionTypeNode : ASTNode
{
    // Type of function (SysQuery, SysCall, Event, etc.)
    public Compiler.FunctionType Type;
}

/// <summary>
/// Function meta-information
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
public class ASTFunctionMetadata : ASTNode
{
    public UInt32 Meta1;
    public UInt32 Meta2;
    public UInt32 Meta3;
    public UInt32 Meta4;
}

/// <summary>
/// Describes a built-in function with its name, number and parameters.
/// </summary>
public class ASTFunction : ASTNode
{
    // Type of function (SysQuery, SysCall, Event, etc.)
    public Compiler.FunctionType Type;
    // Name of the function
    public String Name;
    // Function parameters
    public List<ASTFunctionParam> Params;
    // Function metadata for Osiris internal use - mostly unknown.
    public UInt32 Meta1;
    public UInt32 Meta2;
    public UInt32 Meta3;
    public UInt32 Meta4;
}

/// <summary>
/// List of function parameters
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
public class ASTFunctionParamList : ASTNode
{
    // Function parameters
    public List<ASTFunctionParam> Params = new List<ASTFunctionParam>();
}

/// <summary>
/// Typed (and optionally direction marked) parameter of a function
/// </summary>
public class ASTFunctionParam : ASTNode
{
    // Parameter name
    public String Name;
    // Parameter type
    public String Type;
    // Parameter direction (IN/OUT)
    // This is only meaningful for Query and SysQuery, for all other types direction is always "IN".
    public ParamDirection Direction;
}

/// <summary>
/// Type alias - defines a new type name and type ID, and maps it to an existing base type.
/// </summary>
public class ASTAlias : ASTNode
{
    // Name of the new type
    public String TypeName;
    // ID of the new type (must be a new type ID)
    public uint TypeId;
    // ID of the type this type is mapped to (must be an existing type ID)
    public uint AliasId;
}

/// <summary>
/// Debug/compiler option
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
public class ASTOption : ASTNode
{
    // Name of debug option
    public String Name;
}

/// <summary>
/// String literal from lexing stage (yytext).
/// This is discarded during parsing and does not appear in the final AST.
/// </summary>
public class ASTLiteral : ASTNode
{
    public String Literal;
}
