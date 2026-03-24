using QUT.Gppg;

namespace LSLib.Stats.Expression;

public partial class ExpressionScanner
{
    public LexLocation LastLocation()
    {
        return new LexLocation(tokLin, tokCol, tokELin, tokECol);
    }
}

public abstract class ExpressionScanBase : AbstractScanner<object, LexLocation>
{
    protected virtual bool yywrap() { return true; }
}

public partial class ExpressionParser
{
    public ExpressionParser(ExpressionScanner scnr) : base(scnr)
    {
    }
}