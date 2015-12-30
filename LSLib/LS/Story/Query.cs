using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS.Story
{
    abstract public class QueryNode : Node
    {
        public override void MakeScript(TextWriter writer, Story story, Tuple tuple)
        {
            writer.Write("{0}(", Name);
            tuple.MakeScript(writer, story);
            writer.WriteLine(")");
        }
    }

    public class DivQueryNode : QueryNode
    {
        public override string TypeName()
        {
            return "Div Query";
        }
    }

    public class InternalQueryNode : QueryNode
    {
        public override string TypeName()
        {
            return "Internal Query";
        }
    }

    public class UserQueryNode : QueryNode
    {
        public override string TypeName()
        {
            return "User Query";
        }
    }
}
