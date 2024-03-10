using QUT.Gppg;

namespace LSLib.Parser;
public class CodeLocation : IMerge<CodeLocation>
{
    private string fileName;
    private int startLine;   // start line
    private int startColumn; // start column
    private int endLine;     // end line
    private int endColumn;   // end column

    /// <summary>
    /// The line at which the text span starts.
    /// </summary>
    public string FileName { get { return fileName; } }

    /// <summary>
    /// The line at which the text span starts.
    /// </summary>
    public int StartLine { get { return startLine; } }

    /// <summary>
    /// The column at which the text span starts.
    /// </summary>
    public int StartColumn { get { return startColumn; } }

    /// <summary>
    /// The line on which the text span ends.
    /// </summary>
    public int EndLine { get { return endLine; } }

    /// <summary>
    /// The column of the first character
    /// beyond the end of the text span.
    /// </summary>
    public int EndColumn { get { return endColumn; } }

    /// <summary>
    /// Default no-arg constructor.
    /// </summary>
    public CodeLocation() { }

    /// <summary>
    /// Constructor for text-span with given start and end.
    /// </summary>
    /// <param name="sl">start line</param>
    /// <param name="sc">start column</param>
    /// <param name="el">end line </param>
    /// <param name="ec">end column</param>
    public CodeLocation(string fl, int sl, int sc, int el, int ec)
    {
        fileName = fl;
        startLine = sl;
        startColumn = sc;
        endLine = el;
        endColumn = ec;
    }

    /// <summary>
    /// Create a text location which spans from the
    /// start of "this" to the end of the argument "last"
    /// </summary>
    /// <param name="last">The last location in the result span</param>
    /// <returns>The merged span</returns>
    public CodeLocation Merge(CodeLocation last)
    {
        return new CodeLocation(this.fileName, this.startLine, this.startColumn, last.endLine, last.endColumn);
    }
}
