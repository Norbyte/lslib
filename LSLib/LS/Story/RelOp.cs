using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS.Story
{
    public class RelOpNode : RelNode
    {
        public enum RelOpType : byte
        {
            Less = 0,
            LessOrEqual = 1,
            Greater = 2,
            GreaterOrEqual = 3,
            Equal = 4,
            NotEqual = 5
        };

        public sbyte Value1Index;
        public sbyte Value2Index;
        public Value Value1;
        public Value Value2;
        public RelOpType RelOp;

        public override void Read(OsiReader reader)
        {
            base.Read(reader);
            Value1Index = reader.ReadSByte();
            Value2Index = reader.ReadSByte();

            Value1 = new Value();
            Value1.Read(reader);

            Value2 = new Value();
            Value2.Read(reader);

            RelOp = (RelOpType)reader.ReadInt32();
        }

        public override void Write(OsiWriter writer)
        {
            base.Write(writer);
            writer.Write(Value1Index);
            writer.Write(Value2Index);

            Value1.Write(writer);
            Value2.Write(writer);
            writer.Write((UInt32)RelOp);
        }

        public override Type NodeType()
        {
            return Type.RelOp;
        }

        public override string TypeName()
        {
            return String.Format("RelOp {0}", RelOp);
        }

        public override void DebugDump(TextWriter writer, Story story)
        {
            base.DebugDump(writer, story);

            writer.Write("    Left Value: ");
            if (Value1Index != -1)
                writer.Write("[Source Column {0}]", Value1Index);
            else
                Value1.DebugDump(writer, story);
            writer.WriteLine();

            writer.Write("    Right Value: ");
            if (Value2Index != -1)
                writer.Write("[Source Column {0}]", Value2Index);
            else
                Value2.DebugDump(writer, story);
            writer.WriteLine();
        }

        public override void MakeScript(TextWriter writer, Story story, Tuple tuple)
        {
            var adaptedTuple = story.Adapters[AdapterRef.AdapterIndex].Adapt(tuple);
            story.Nodes[ParentRef.NodeIndex].MakeScript(writer, story, adaptedTuple);
            writer.WriteLine("AND");

            if (Value1Index != -1)
                adaptedTuple.Logical[Value1Index].MakeScript(writer, story, tuple);
            else
                Value1.MakeScript(writer, story, tuple);

            switch (RelOp)
            {
                case RelOpType.Less: writer.Write(" < "); break;
                case RelOpType.LessOrEqual: writer.Write(" <= "); break;
                case RelOpType.Greater: writer.Write(" > "); break;
                case RelOpType.GreaterOrEqual: writer.Write(" >= "); break;
                case RelOpType.Equal: writer.Write(" == "); break;
                case RelOpType.NotEqual: writer.Write(" != "); break;
            }

            if (Value2Index != -1)
                adaptedTuple.Logical[Value2Index].MakeScript(writer, story, tuple);
            else
                Value2.MakeScript(writer, story, tuple);
            writer.WriteLine();
        }
    }
}
