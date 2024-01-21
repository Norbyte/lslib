using LSLib.LS.Story.HeaderParser;

namespace LSLib.LS.Story.Compiler;

/// <summary>
/// Responsible for parsing story header files (story_header.div),
/// and loading header definitions to the compilation context.
/// </summary>
public class StoryHeaderLoader
{
    private CompilationContext Context;

    public StoryHeaderLoader(CompilationContext context)
    {
        Context = context;
    }

    /// <summary>
    /// Creates and loads a type alias (e.g. CHARACTERGUID, ITEMGUID, etc.) from an AST node.
    /// </summary>
    private bool LoadAliasFromAST(ASTAlias astAlias)
    {
        var type = new ValueType
        {
            Name = astAlias.TypeName,
            TypeId = astAlias.TypeId,
            IntrinsicTypeId = (Value.Type)astAlias.AliasId
        };
        return Context.RegisterType(type);
    }

    /// <summary>
    /// Creates and loads a function declaration from an AST node.
    /// </summary>
    private bool LoadFunctionFromAST(ASTFunction astFunction)
    {
        var args = new List<FunctionParam>(astFunction.Params.Count);
        foreach (var astParam in astFunction.Params)
        {
            var type = Context.LookupType(astParam.Type);
            // Since types and alias types are declared at the beginning of the
            // story header, we shold have full type information here, so any
            // unresolved types will be flagged as an error.
            if (type == null)
            {
                Context.Log.Error(null, DiagnosticCode.UnresolvedTypeInSignature,
                    String.Format("Function \"{0}({1})\" argument \"{2}\" has unresolved type \"{3}\"",
                        astFunction.Name, astFunction.Params.Count, astParam.Name, astParam.Type));
                continue;
            }

            var param = new FunctionParam
            {
                Name = astParam.Name,
                Type = type,
                Direction = astParam.Direction
            };
            args.Add(param);
        }

        var signature = new FunctionSignature
        {
            Name = astFunction.Name,
            Type = astFunction.Type,
            Params = args,
            FullyTyped = true,
            Inserted = false,
            Deleted = false,
            Read = false
        };

        var func = new BuiltinFunction
        {
            Signature = signature,
            Meta1 = astFunction.Meta1,
            Meta2 = astFunction.Meta2,
            Meta3 = astFunction.Meta3,
            Meta4 = astFunction.Meta4
        };

        return Context.RegisterFunction(signature, func);
    }

    /// <summary>
    /// Parses a story header file into an AST.
    /// </summary>
    public ASTDeclarations ParseHeader(Stream stream)
    {
        var scanner = new HeaderScanner();
        scanner.SetSource(stream);
        var parser = new HeaderParser.HeaderParser(scanner);
        bool parsed = parser.Parse();

        if (parsed)
        {
            return parser.GetDeclarations();
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Loads all declarations from a story header file.
    /// </summary>
    public void LoadHeader(ASTDeclarations declarations)
    {
        foreach (var alias in declarations.Aliases)
        {
            LoadAliasFromAST(alias);
        }

        foreach (var func in declarations.Functions)
        {
            LoadFunctionFromAST(func);
        }
    }
}
