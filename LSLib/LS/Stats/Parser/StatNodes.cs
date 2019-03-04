using System;
using System.Collections.Generic;

namespace LSLib.LS.Stats.StatParser
{
    /// <summary>
    /// Base class for all stat nodes.
    /// (This doesn't do anything meaningful, it is needed only to 
    /// provide the GPPG parser a semantic value base class.)
    /// </summary>
    public class StatNode
    {
    }

    /// <summary>
    /// Declarations node - contains every declaration from the story header file.
    /// </summary>
    public class StatDeclarations : StatNode
    {
        public List<StatDeclaration> Declarations = new List<StatDeclaration>();
    }

    /// <summary>
    /// List of stat properties
    /// </summary>
    public class StatDeclaration : StatNode
    {
        public Dictionary<String, object> Properties = new Dictionary<String, object>();
    }

    /// <summary>
    /// Wrapped declaration that won't be merged into the parent declaration.
    /// </summary>
    public class StatWrappedDeclaration : StatNode
    {
        public StatDeclaration Declaration;
    }

    /// <summary>
    /// A string property of a stat entry (Key/value pair)
    /// </summary>
    public class StatProperty : StatNode
    {
        public String Key;
        public object Value;
    }

    /// <summary>
    /// An element of collection of a stat entry (Key/value pair)
    /// </summary>
    public class StatElement : StatNode
    {
        public String Collection;
        public object Value;
    }

    /// <summary>
    /// A collection of sub-stats.
    /// </summary>
    public class StatCollection : StatNode
    {
        public List<object> Collection;
    }

    /// <summary>
    /// String literal from lexing stage (yytext).
    /// This is discarded during parsing and does not appear in the final AST.
    /// </summary>
    public class StatLiteral : StatNode
    {
        public String Literal;
    }
}
