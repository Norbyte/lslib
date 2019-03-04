using QUT.Gppg;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LSLib.LS.Stats.StatPropertyParser
{
    public class StatPropertyNode
    {
    }

    public partial class StatPropertyScanner
    {
        public LexLocation LastLocation()
        {
            return new LexLocation(tokLin, tokCol, tokELin, tokECol);
        }
    }

    public abstract class StatPropertyScanBase : AbstractScanner<StatPropertyNode, LexLocation>
    {
        protected virtual bool yywrap() { return true; }
    }

    public partial class StatPropertyParser
    {
        public StatPropertyParser(StatPropertyScanner scnr) : base(scnr)
        {
        }
    }
}