using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS.Story
{
    public class DatabaseNode : DataNode
    {
        public override string TypeName()
        {
            return "Database";
        }

        public override void MakeScript(TextWriter writer, Story story, Tuple tuple)
        {
            writer.Write("{0}(", Name);
            tuple.MakeScript(writer, story);
            writer.WriteLine(")");
        }
    }
}
