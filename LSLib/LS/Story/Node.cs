using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS.Story
{
    abstract public class Node : OsirisSerializable
    {
        public DatabaseRef DatabaseRef;
        public string Name;
        public byte NameIndex;

        public virtual void Read(OsiReader reader)
        {
            DatabaseRef = reader.ReadDatabaseRef();
            Name = reader.ReadString();
            if (Name.Length > 0)
            {
                NameIndex = reader.ReadByte();
            }
        }

        abstract public string TypeName();

        abstract public void MakeScript(TextWriter writer, Story story, Tuple tuple);

        public virtual void DebugDump(TextWriter writer, Story story)
        {
            if (Name.Length > 0)
            {
                writer.Write("{0}/{1}: ", Name, NameIndex);
            }

            writer.Write("<{0}>", TypeName());
            if (DatabaseRef.IsValid())
            {
                writer.Write(", Database ");
                DatabaseRef.DebugDump(writer, story);
            }

            writer.WriteLine();
        }
    }
}
