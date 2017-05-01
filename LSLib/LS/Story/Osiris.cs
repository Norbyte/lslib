using QUT.Gppg;

namespace LSLib.LS.Osiris
{
    public abstract class GoalScanBase : AbstractScanner<int, LexLocation>
    {
        protected virtual bool yywrap() { return true; }
    }
    
    public partial class GoalParser
    {
        public GoalParser(GoalScanner scnr) : base(scnr)
        {
        }
    }
}