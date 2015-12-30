using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS.Story
{
    public class ProcNode : DataNode
    {
        public override Type NodeType()
        {
            return Type.Proc;
        }

        public override string TypeName()
        {
            return "Proc";
        }

        public override void MakeScript(TextWriter writer, Story story, Tuple tuple)
        {
            writer.Write("{0}(", Name);
            tuple.MakeScript(writer, story, true);
            writer.WriteLine(")");
        }
    }
}
