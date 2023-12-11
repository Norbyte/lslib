using QUT.Gppg;

namespace LSLib.LS.Stats.Lua;

public partial class StatLuaScanner
{
    public LexLocation LastLocation()
    {
        return new LexLocation(tokLin, tokCol, tokELin, tokECol);
    }
}

public abstract class StatLuaScanBase : AbstractScanner<object, LexLocation>
{
    protected virtual bool yywrap() { return true; }
}

public partial class StatLuaParser
{
    public StatLuaParser(StatLuaScanner scnr) : base(scnr)
    {
    }
}