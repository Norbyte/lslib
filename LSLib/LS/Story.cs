using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS
{
    public interface OsirisSerializable
    {
        void Read(OsiReader reader);
    }

    public class OsiReader : BinaryReader
    {
        public byte Scramble = 0x00;
        public UInt32 MinorVersion;
        public UInt32 MajorVersion;

        public OsiReader(Stream stream) : base(stream)
        {
        }

        public override string ReadString()
        {
            List<byte> bytes = new List<byte>();
            while (true)
            {
                var b = (byte)(ReadByte() ^ Scramble);
                if (b != 0)
                {
                    bytes.Add(b);
                }
                else
                {
                    break;
                }
            }

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public Guid ReadGuid()
        {
            var guid = ReadBytes(16);
            return new Guid(guid);
        }

        public List<T> ReadList<T>() where T : OsirisSerializable, new()
        {
            var count = ReadUInt32();
            var items = new List<T>();
            while (count-- > 0)
            {
                var item = new T();
                item.Read(this);
                items.Add(item);
            }

            return items;
        }

        public ReteNodeRef ReadNodeRef()
        {
            var nodeRef = new ReteNodeRef();
            nodeRef.Read(this);
            return nodeRef;
        }

        public ReteAdapterRef ReadAdapterRef()
        {
            var adapterRef = new ReteAdapterRef();
            adapterRef.Read(this);
            return adapterRef;
        }

        public ReteDatabaseRef ReadDatabaseRef()
        {
            var databaseRef = new ReteDatabaseRef();
            databaseRef.Read(this);
            return databaseRef;
        }
    }

    public class OsirisHeader : OsirisSerializable
    {
        public string Version;
        public byte MajorVersion;
        public byte MinorVersion;
        public bool BigEndian;
        public byte Unused;
        public string StoryFileVersion;
        public UInt32 DebugFlags;

        public void Read(OsiReader reader)
        {
            reader.ReadByte();
            Version = reader.ReadString();
            MajorVersion = reader.ReadByte();
            MinorVersion = reader.ReadByte();
            BigEndian = reader.ReadByte() == 1;
            Unused = reader.ReadByte();

            if (MajorVersion >= 1 && MinorVersion >= 2)
                reader.ReadBytes(0x80); // Version string buffer

            if (MajorVersion >= 1 && MinorVersion >= 3)
                DebugFlags = reader.ReadUInt32();
            else
                DebugFlags = 0;
        }
    }

    public class OsirisType : OsirisSerializable
    {
        public byte Index;
        public string Name;

        public void Read(OsiReader reader)
        {
            Name = reader.ReadString();
            Index = reader.ReadByte();
        }

        public void DebugDump(TextWriter writer)
        {
            writer.WriteLine("{0}: {1}", Index, Name);
        }
    }

    public class OsirisDivObject : OsirisSerializable
    {
        public string Name;
        public byte Type;
        public UInt32 Key1;
        public UInt32 Key2; // Some ref?
        public UInt32 Key3; // Type again?
        public UInt32 Key4;

        public void Read(OsiReader reader)
        {
            Name = reader.ReadString();
            Type = reader.ReadByte();
            Key1 = reader.ReadUInt32();
            Key2 = reader.ReadUInt32();
            Key3 = reader.ReadUInt32();
            Key4 = reader.ReadUInt32();
        }

        public void DebugDump(TextWriter writer)
        {
            writer.WriteLine("{0} {1} ({2}, {3}, {4}, {5})", Type, Name, Key1, Key2, Key3, Key4);
        }
    }

    public class OsirisFunctionName : OsirisSerializable
    {
        public string Name;
        public List<byte> OutParamMask;
        public OsirisParameterList Parameters;

        public void Read(OsiReader reader)
        {
            Name = reader.ReadString();
            OutParamMask = new List<byte>();
            var outParamBytes = reader.ReadUInt32();
            while (outParamBytes-- > 0)
            {
                OutParamMask.Add(reader.ReadByte());
            }

            Parameters = new OsirisParameterList();
            Parameters.Read(reader);
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            writer.Write(Name);
            writer.Write("(");
            for (var i = 0; i < Parameters.Types.Count; i++ )
            {
                var type = story.Types[Parameters.Types[i]];
                var isOutParam = ((OutParamMask[i >> 3] << (i & 7)) & 0x80) == 0x80;
                if (isOutParam) writer.Write("out ");
                writer.Write(type.Name);
                if (i < Parameters.Types.Count - 1) writer.Write(", ");
            }
            writer.Write(")");
        }
    }

    public class OsirisParameterList : OsirisSerializable
    {
        public List<UInt32> Types;

        public void Read(OsiReader reader)
        {
            Types = new List<UInt32>();
            var count = reader.ReadByte();
            while (count-- > 0)
            {
                Types.Add(reader.ReadUInt32());
            }
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            for (var i = 0; i < Types.Count; i++)
            {
                writer.Write(story.Types[Types[i]].Name);
                if (i < Types.Count - 1) writer.Write(", ");
            }
        }
    }

    public enum OsirisFunctionType
    {
        Event = 1,
        Query = 2,
        Call = 3,
        Database = 4,
        Proc = 5,
        SysQuery = 6,
        SysCall = 7
    }

    public class OsirisFunction : OsirisSerializable
    {
        public UInt32 Line;
        public UInt32 Unknown1;
        public UInt32 Unknown2;
        public ReteNodeRef NodeRef;
        public OsirisFunctionType Type;
        public Guid GUID;
        public OsirisFunctionName Name;

        public void Read(OsiReader reader)
        {
            Line = reader.ReadUInt32();
            Unknown1 = reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
            NodeRef = reader.ReadNodeRef();
            Type = (OsirisFunctionType)reader.ReadByte();
            GUID = reader.ReadGuid();
            Name = new OsirisFunctionName();
            Name.Read(reader);
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            writer.Write("{0} ", Type.ToString());
            Name.DebugDump(writer, story);
            if (NodeRef.IsValid())
            {
                var node = story.Nodes[NodeRef.NodeIndex];
                writer.Write(" @ {0}/{1}", node.Name, node.NameIndex);
            }

            writer.WriteLine(" [{0}, {1}]", Unknown1, Unknown2);
        }
    }

    enum ReteNodeType : byte
    {
        Database = 1,
        Proc = 2,
        DivQuery = 3,
        And = 4,
        NotAnd = 5,
        RelOp = 6,
        Rule = 7,
        InternalQuery = 8,
        UserQuery = 9
    };

    public class ReteNodeEntryItem : OsirisSerializable
    {
        public ReteNodeRef NodeRef;
        public UInt32 EntryPoint;
        public UInt32 GoalId;

        public void Read(OsiReader reader)
        {
            NodeRef = reader.ReadNodeRef();
            EntryPoint = reader.ReadUInt32();
            GoalId = reader.ReadUInt32();
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            if (NodeRef.IsValid())
            {
                writer.Write("(");
                NodeRef.DebugDump(writer, story);
                writer.Write(", Entry Point {1}, Goal ", NodeRef, EntryPoint);
                writer.Write(story.Goals[GoalId].Name);
                writer.Write(")");
            }
            else
            {
                writer.Write("(none)");
            }
        }
    }

    public class ReteNodeRef : OsirisSerializable
    {
        public UInt32 NodeIndex;

        public void Read(OsiReader reader)
        {
            NodeIndex = reader.ReadUInt32();
        }

        public bool IsValid()
        {
            return NodeIndex != 0;
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            if (!IsValid())
            {
                writer.Write("(None)");
            }
            else
            {
                var node = story.Nodes[NodeIndex];
                if (node.Name.Length > 0)
                {
                    writer.Write("#{0} <{1}/{2} {3}>", NodeIndex, node.Name, node.NameIndex, node.TypeName());
                }
                else
                {
                    writer.Write("#{0} <{1}>", NodeIndex, node.TypeName());
                }
            }
        }
    }

    public class ReteAdapterRef : OsirisSerializable
    {
        public UInt32 AdapterIndex;

        public void Read(OsiReader reader)
        {
            AdapterIndex = reader.ReadUInt32();
        }

        public bool IsValid()
        {
            return AdapterIndex != 0;
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            if (!IsValid())
            {
                writer.Write("(None)");
            }
            else
            {
                writer.Write("#{0}", AdapterIndex);
            }
        }
    }

    public class ReteDatabaseRef : OsirisSerializable
    {
        public UInt32 DatabaseIndex;

        public void Read(OsiReader reader)
        {
            DatabaseIndex = reader.ReadUInt32();
        }

        public bool IsValid()
        {
            return DatabaseIndex != 0;
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            if (!IsValid())
            {
                writer.Write("(None)");
            }
            else
            {
                writer.Write("#{0}", DatabaseIndex);
            }
        }
    }

    abstract public class ReteNode : OsirisSerializable
    {
        public ReteDatabaseRef DatabaseRef;
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

        abstract public void MakeScript(TextWriter writer, Story story, OsirisTuple tuple);

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

    abstract public class ReteDataNode : ReteNode
    {
        public List<ReteNodeEntryItem> ReferencedBy;

        public override void Read(OsiReader reader)
        {
            base.Read(reader);
            ReferencedBy = reader.ReadList<ReteNodeEntryItem>();
        }

        public override void DebugDump(TextWriter writer, Story story)
        {
            base.DebugDump(writer, story);

            if (ReferencedBy.Count > 0)
            {
                writer.WriteLine("    Referenced By:");
                foreach (var entry in ReferencedBy)
                {
                    writer.Write("        ");
                    entry.DebugDump(writer, story);
                    writer.WriteLine("");
                }
            }
        }
    }

    public class ReteDatabaseNode : ReteDataNode
    {
        public override string TypeName()
        {
            return "Database";
        }

        public override void MakeScript(TextWriter writer, Story story, OsirisTuple tuple)
        {
            writer.Write("{0}(", Name);
            tuple.MakeScript(writer, story);
            writer.WriteLine(")");
        }
    }

    public class ReteProcNode : ReteDataNode
    {
        public override string TypeName()
        {
            return "Proc";
        }

        public override void MakeScript(TextWriter writer, Story story, OsirisTuple tuple)
        {
            writer.Write("{0}(", Name);
            tuple.MakeScript(writer, story, true);
            writer.WriteLine(")");
        }
    }

    abstract public class ReteQueryNode : ReteNode
    {
        public override void MakeScript(TextWriter writer, Story story, OsirisTuple tuple)
        {
            writer.Write("{0}(", Name);
            tuple.MakeScript(writer, story);
            writer.WriteLine(")");
        }
    }

    public class ReteDivQueryNode : ReteQueryNode
    {
        public override string TypeName()
        {
            return "Div Query";
        }
    }

    public class ReteInternalQueryNode : ReteQueryNode
    {
        public override string TypeName()
        {
            return "Internal Query";
        }
    }

    public class ReteUserQueryNode : ReteQueryNode
    {
        public override string TypeName()
        {
            return "User Query";
        }
    }

    abstract public class ReteTreeNode : ReteNode
    {
        public ReteNodeEntryItem NextNode;

        public override void Read(OsiReader reader)
        {
            base.Read(reader);
            NextNode = new ReteNodeEntryItem();
            NextNode.Read(reader);
        }

        public override void DebugDump(TextWriter writer, Story story)
        {
            base.DebugDump(writer, story);

            writer.Write("    Next: ");
            NextNode.DebugDump(writer, story);
            writer.WriteLine("");
        }
    }

    abstract public class ReteJoinNode : ReteTreeNode
    {
        public ReteNodeRef LeftParentRef;
        public ReteNodeRef RightParentRef;
        public ReteAdapterRef Adapter1Ref;
        public ReteAdapterRef Adapter2Ref;
        public ReteDatabaseRef Database1Ref;
        public byte Database1Flag;
        public ReteNodeEntryItem Database1;
        public ReteDatabaseRef Database2Ref;
        public byte Database2Flag;
        public ReteNodeEntryItem Database2;

        public override void Read(OsiReader reader)
        {
            base.Read(reader);
            LeftParentRef = reader.ReadNodeRef();
            RightParentRef = reader.ReadNodeRef();
            Adapter1Ref = reader.ReadAdapterRef();
            Adapter2Ref = reader.ReadAdapterRef();

            Database1Ref = reader.ReadDatabaseRef();
            Database1 = new ReteNodeEntryItem();
            Database1.Read(reader);
            Database1Flag = reader.ReadByte();

            Database2Ref = reader.ReadDatabaseRef();
            Database2 = new ReteNodeEntryItem();
            Database2.Read(reader);
            Database2Flag = reader.ReadByte();
        }

        public override void DebugDump(TextWriter writer, Story story)
        {
            base.DebugDump(writer, story);

            writer.Write("    Left:");
            if (LeftParentRef.IsValid())
            {
                writer.Write(" Parent ");
                LeftParentRef.DebugDump(writer, story);
            }

            if (Adapter1Ref.IsValid())
            {
                writer.Write(" Adapter ");
                Adapter1Ref.DebugDump(writer, story);
            }

            if (Database1Ref.IsValid())
            {
                writer.Write(" Database ");
                Database1Ref.DebugDump(writer, story);
                writer.Write(" Flag {0}", Database1Flag);
                writer.Write(" Entry ");
                Database1.DebugDump(writer, story);
            }

            writer.WriteLine("");

            writer.Write("    Right:");
            if (RightParentRef.IsValid())
            {
                writer.Write(" Parent ");
                RightParentRef.DebugDump(writer, story);
            }

            if (Adapter2Ref.IsValid())
            {
                writer.Write(" Adapter ");
                Adapter2Ref.DebugDump(writer, story);
            }

            if (Database2Ref.IsValid())
            {
                writer.Write(" Database ");
                Database2Ref.DebugDump(writer, story);
                writer.Write(" Flag {0}", Database2Flag);
                writer.Write(" Entry ");
                Database2.DebugDump(writer, story);
            }

            writer.WriteLine("");
        }
    }

    public class ReteAndNode : ReteJoinNode
    {
        public override string TypeName()
        {
            return "And";
        }

        public override void MakeScript(TextWriter writer, Story story, OsirisTuple tuple)
        {
            var leftTuple = story.Adapters[Adapter1Ref.AdapterIndex].Adapt(tuple);
            story.Nodes[LeftParentRef.NodeIndex].MakeScript(writer, story, leftTuple);
            writer.WriteLine("AND");
            var rightTuple = story.Adapters[Adapter2Ref.AdapterIndex].Adapt(tuple);
            story.Nodes[RightParentRef.NodeIndex].MakeScript(writer, story, rightTuple);
        }
    }

    public class ReteNotAndNode : ReteJoinNode
    {
        public override string TypeName()
        {
            return "Not And";
        }

        public override void MakeScript(TextWriter writer, Story story, OsirisTuple tuple)
        {
            var leftTuple = story.Adapters[Adapter1Ref.AdapterIndex].Adapt(tuple);
            story.Nodes[LeftParentRef.NodeIndex].MakeScript(writer, story, leftTuple);
            writer.WriteLine("AND NOT");
            var rightTuple = story.Adapters[Adapter2Ref.AdapterIndex].Adapt(tuple);
            story.Nodes[RightParentRef.NodeIndex].MakeScript(writer, story, rightTuple);
        }
    }

    abstract public class ReteRelNode : ReteTreeNode
    {
        public ReteNodeRef ParentRef;
        public ReteAdapterRef AdapterRef;
        public ReteDatabaseRef RelDatabaseRef;
        public ReteNodeEntryItem RelDatabase;
        public byte RelDatabaseFlag;

        public override void Read(OsiReader reader)
        {
            base.Read(reader);
            ParentRef = reader.ReadNodeRef();
            AdapterRef = reader.ReadAdapterRef();

            RelDatabaseRef = reader.ReadDatabaseRef();
            RelDatabase = new ReteNodeEntryItem();
            RelDatabase.Read(reader);
            RelDatabaseFlag = reader.ReadByte();
        }

        public override void DebugDump(TextWriter writer, Story story)
        {
            base.DebugDump(writer, story);

            writer.Write("   ");
            if (ParentRef.IsValid())
            {
                writer.Write(" Parent ");
                ParentRef.DebugDump(writer, story);
            }

            if (AdapterRef.IsValid())
            {
                writer.Write(" Adapter ");
                AdapterRef.DebugDump(writer, story);
            }

            if (RelDatabaseRef.IsValid())
            {
                writer.Write(" Database ");
                RelDatabaseRef.DebugDump(writer, story);
                writer.Write(" Flag {0}", RelDatabaseFlag);
                writer.Write(" Entry ");
                RelDatabase.DebugDump(writer, story);
            }

            writer.WriteLine("");
        }
    }

    public enum ReteRelOpType : byte
    {
        Less = 0,
        LessOrEqual = 1,
        Greater = 2,
        GreaterOrEqual = 3,
        Equal = 4,
        NotEqual = 5
    };

    public class ReteRelOpNode : ReteRelNode
    {
        public sbyte Value1Index;
        public sbyte Value2Index;
        public OsirisValue Value1;
        public OsirisValue Value2;
        public ReteRelOpType RelOp;

        public override void Read(OsiReader reader)
        {
            base.Read(reader);
            Value1Index = reader.ReadSByte();
            Value2Index = reader.ReadSByte();

            Value1 = new OsirisValue();
            Value1.Read(reader);

            Value2 = new OsirisValue();
            Value2.Read(reader);

            RelOp = (ReteRelOpType)reader.ReadInt32();
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

        public override void MakeScript(TextWriter writer, Story story, OsirisTuple tuple)
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
                case ReteRelOpType.Less: writer.Write(" < "); break;
                case ReteRelOpType.LessOrEqual: writer.Write(" <= "); break;
                case ReteRelOpType.Greater: writer.Write(" > "); break;
                case ReteRelOpType.GreaterOrEqual: writer.Write(" >= "); break;
                case ReteRelOpType.Equal: writer.Write(" == "); break;
                case ReteRelOpType.NotEqual: writer.Write(" != "); break;
            }

            if (Value2Index != -1)
                adaptedTuple.Logical[Value2Index].MakeScript(writer, story, tuple);
            else
                Value2.MakeScript(writer, story, tuple);
            writer.WriteLine();
        }
    }

    public class ReteCall : OsirisSerializable
    {
        public string Name;
        public List<OsirisTypedValue> Parameters;
        public bool Negate;
        public Int32 GoalIdOrDebugHook;

        public void Read(OsiReader reader)
        {
            Name = reader.ReadString();
            if (Name.Length > 0)
            {
                var hasParams = reader.ReadByte();
                if (hasParams > 0)
                {
                    Parameters = new List<OsirisTypedValue>();
                    var numParams = reader.ReadByte();
                    while (numParams-- > 0)
                    {
                        OsirisTypedValue param;
                        var type = reader.ReadByte();
                        if (type == 1)
                            param = new OsirisVariable();
                        else
                            param = new OsirisTypedValue();
                        param.Read(reader);
                        Parameters.Add(param);
                    }
                }

                Negate = reader.ReadByte() == 1;
            }

            GoalIdOrDebugHook = reader.ReadInt32();
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            if (Name.Length > 0)
            {
                if (Negate) writer.Write("!");
                writer.Write("{0}(", Name);
                if (Parameters != null)
                {
                    for (var i = 0; i < Parameters.Count; i++)
                    {
                        Parameters[i].DebugDump(writer, story);
                        if (i < Parameters.Count - 1) writer.Write(", ");
                    }
                }

                writer.Write(") ");
            }

            if (GoalIdOrDebugHook != 0)
            {
                if (GoalIdOrDebugHook < 0)
                {
                    writer.Write("<Debug hook #{0}>", -GoalIdOrDebugHook);
                }
                else
                {
                    var goal = story.Goals[(uint)GoalIdOrDebugHook];
                    writer.Write("<Complete goal #{0} {1}>", GoalIdOrDebugHook, goal.Name);
                }
            }
        }

        public void MakeScript(TextWriter writer, Story story, OsirisTuple tuple)
        {
            if (Name.Length > 0)
            {
                if (Negate) writer.Write("NOT ");
                writer.Write("{0}(", Name);
                if (Parameters != null)
                {
                    for (var i = 0; i < Parameters.Count; i++)
                    {
                        var param = Parameters[i];
                        param.MakeScript(writer, story, tuple);
                        if (i < Parameters.Count - 1)
                            writer.Write(", ");
                    }
                }

                writer.Write(")");
            }

            if (GoalIdOrDebugHook > 0)
            {
                writer.Write("GoalCompleted;");
            }
        }
    }

    public enum ReteRuleType
    {
        Rule,
        Proc,
        Query
    };

    public class ReteRuleNode : ReteRelNode
    {
        public List<ReteCall> Calls;
        public List<OsirisVariable> Variables;
        public UInt32 Line;
        public UInt32 DerivedGoalId;
        public bool IsQuery;

        public override void Read(OsiReader reader)
        {
            base.Read(reader);
            Calls = reader.ReadList<ReteCall>();

            Variables = new List<OsirisVariable>();
            var variables = reader.ReadByte();
            while (variables-- > 0)
            {
                var type = reader.ReadByte();
                if (type != 1) throw new InvalidDataException("Illegal value type in rule variable list");
                var variable = new OsirisVariable();
                variable.Read(reader);
                if (variable.Adapted)
                {
                    variable.VariableName = String.Format("_Var{0}", Variables.Count + 1);
                }

                Variables.Add(variable);
            }

            Line = reader.ReadUInt32();

            if (reader.MajorVersion >= 1 && reader.MinorVersion >= 6)
                IsQuery = reader.ReadByte() == 1;
            else
                IsQuery = false;
        }

        public override string TypeName()
        {
            if (IsQuery)
                return "Query Rule";
            else
                return "Rule";
        }

        public override void DebugDump(TextWriter writer, Story story)
        {
            base.DebugDump(writer, story);

            writer.WriteLine("    Variables: ");
            foreach (var v in Variables)
            {
                writer.Write("        ");
                v.DebugDump(writer, story);
                writer.WriteLine("");
            }

            writer.WriteLine("    Calls: ");
            foreach (var call in Calls)
            {
                writer.Write("        ");
                call.DebugDump(writer, story);
                writer.WriteLine("");
            }
        }

        public ReteNode GetRoot(Story story)
        {
            ReteNode parent = this;
            for (;;)
            {
                if (parent is ReteRelNode)
                {
                    var rel = parent as ReteRelNode;
                    parent = story.Nodes[rel.ParentRef.NodeIndex];
                }
                else if (parent is ReteJoinNode)
                {
                    var join = parent as ReteJoinNode;
                    parent = story.Nodes[join.LeftParentRef.NodeIndex];
                }
                else
                {
                    return parent;
                }
            }
        }

        public ReteRuleType GetType(Story story)
        {
            var root = GetRoot(story);
            if (root is ReteProcNode)
            {
                if (IsQuery)
                    return ReteRuleType.Query;
                else
                    return ReteRuleType.Proc;
            }
            else
                return ReteRuleType.Rule;
        }

        public OsirisTuple MakeInitialTuple()
        {
            var tuple = new OsirisTuple();
            for (int i = 0; i < Variables.Count; i++)
            {
                tuple.Physical.Add(Variables[i]);
                tuple.Logical.Add(i, Variables[i]);
            }

            return tuple;
        }

        public override void MakeScript(TextWriter writer, Story story, OsirisTuple tuple)
        {
            switch (GetType(story))
            {
                case ReteRuleType.Proc: writer.WriteLine("PROC"); break;
                case ReteRuleType.Query: writer.WriteLine("QRY"); break;
                case ReteRuleType.Rule: writer.WriteLine("IF"); break;
            }

            var initialTuple = MakeInitialTuple();
            story.Nodes[ParentRef.NodeIndex].MakeScript(writer, story, initialTuple);
            writer.WriteLine("THEN");
            foreach (var call in Calls)
            {
                call.MakeScript(writer, story, initialTuple);
                writer.WriteLine();
            }
        }
    }

    public enum OsirisValueType : uint
    {
        Unknown = 0,
        Integer = 1,
        Float = 2,
        String = 3
    }

    public class OsirisValue : OsirisSerializable
    {
        public UInt32 TypeId;
        public Int32 IntValue;
        public Single FloatValue;
        public String StringValue;

        public virtual void Read(OsiReader reader)
        {
            var wtf = reader.ReadByte();
            if (wtf == 49)
            {
                TypeId = reader.ReadUInt32();
                IntValue = reader.ReadInt32();
            }
            else if (wtf == 48)
            {
                TypeId = reader.ReadUInt32();
                switch ((OsirisValueType)TypeId)
                {
                    case OsirisValueType.Unknown:
                        break;

                    case OsirisValueType.Integer:
                        IntValue = reader.ReadInt32();
                        break;

                    case OsirisValueType.Float:
                        FloatValue = reader.ReadSingle();
                        break;

                    case OsirisValueType.String:
                        if (reader.ReadByte() > 0)
                        {
                            StringValue = reader.ReadString();
                        }
                        break;

                    default:
                        StringValue = reader.ReadString();
                        break;
                }
            }
            else
            {
                throw new InvalidDataException("Unrecognized value type");
            }
        }

        public virtual void DebugDump(TextWriter writer, Story story)
        {
            switch ((OsirisValueType)TypeId)
            {
                case OsirisValueType.Unknown:
                    writer.Write("<unknown>");
                    break;

                case OsirisValueType.Integer:
                    writer.Write(IntValue);
                    break;

                case OsirisValueType.Float:
                    writer.Write(FloatValue);
                    break;

                case OsirisValueType.String:
                    writer.Write("'{0}'", StringValue);
                    break;

                default:
                    writer.Write(StringValue);
                    break;
            }
        }

        public virtual void MakeScript(TextWriter writer, Story story, OsirisTuple tuple, bool printTypes = false)
        {
            switch ((OsirisValueType)TypeId)
            {
                case OsirisValueType.Unknown:
                    throw new InvalidDataException("Script cannot contain unknown values");

                case OsirisValueType.Integer:
                    writer.Write(IntValue);
                    break;

                case OsirisValueType.Float:
                    writer.Write(FloatValue);
                    break;

                case OsirisValueType.String:
                    writer.Write("\"{0}\"", StringValue);
                    break;

                default:
                    writer.Write(StringValue);
                    break;
            }
        }
    }

    public class OsirisTypedValue : OsirisValue
    {
        public bool IsValid;
        public bool OutParam;
        public bool IsAType;

        public override void Read(OsiReader reader)
        {
            base.Read(reader);
            IsValid = reader.ReadByte() == 1;
            OutParam = reader.ReadByte() == 1;
            IsAType = reader.ReadByte() == 1;
        }

        public override void DebugDump(TextWriter writer, Story story)
        {
            if (IsValid) writer.Write("valid ");
            if (OutParam) writer.Write("out ");
            if (IsAType) writer.Write("type ");

            if (IsValid)
            {
                base.DebugDump(writer, story);
            }
            else
            {
                writer.Write("<{0}>", story.Types[TypeId].Name);
            }
        }
    }

    public class OsirisVariable : OsirisTypedValue
    {
        public sbyte Index;
        public bool Unused;
        public bool Adapted;
        public string VariableName;

        public override void Read(OsiReader reader)
        {
            base.Read(reader);
            Index = reader.ReadSByte();
            Unused = reader.ReadByte() == 1;
            Adapted = reader.ReadByte() == 1;
        }

        public override void DebugDump(TextWriter writer, Story story)
        {
            writer.Write("#{0} ", Index);
            if (VariableName != null && VariableName.Length > 0) writer.Write("'{0}' ", VariableName);
            if (Unused) writer.Write("unused ");
            if (Adapted) writer.Write("adapted ");
            base.DebugDump(writer, story);
        }

        public override void MakeScript(TextWriter writer, Story story, OsirisTuple tuple, bool printTypes = false)
        {
            if (Unused)
            {
                writer.Write("_");
            }
            else if (Adapted)
            {
                if (VariableName != null && VariableName.Length > 0)
                {
                    if (printTypes)
                    {
                        writer.Write("({0})", story.Types[TypeId].Name);
                    }

                    writer.Write(VariableName);
                }
                else
                {
                    tuple.Logical[Index].MakeScript(writer, story, null);
                }
            }
            else
            {
                base.MakeScript(writer, story, tuple);
            }
        }
    }

    public class OsirisTuple : OsirisSerializable
    {
        public List<OsirisValue> Physical = new List<OsirisValue>();
        public Dictionary<int, OsirisValue> Logical = new Dictionary<int, OsirisValue>();

        public void Read(OsiReader reader)
        {
            Physical.Clear();
            Logical.Clear();

            var count = reader.ReadByte();
            while (count-- > 0)
            {
                var index = reader.ReadByte();
                var value = new OsirisValue();
                value.Read(reader);

                Physical.Add(value);
                Logical.Add(index, value);
            }
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            writer.Write("(");
            var keys = Logical.Keys.ToArray();
            for (var i = 0; i < Logical.Count; i++)
            {
                writer.Write("{0}: ", keys[i]);
                Logical[keys[i]].DebugDump(writer, story);
                if (i < Logical.Count - 1) writer.Write(", ");
            }
            writer.Write(")");
        }

        public void MakeScript(TextWriter writer, Story story, bool printTypes = false)
        {
            for (var i = 0; i < Physical.Count; i++)
            {
                var value = Physical[i];
                value.MakeScript(writer, story, null, printTypes);
                if (i < Physical.Count - 1)
                    writer.Write(", ");
            }
        }
    }

    public class OsirisAdapter : OsirisSerializable
    {
        public OsirisTuple Constants;
        public List<sbyte> LogicalIndices;
        public Dictionary<byte, byte> LogicalToPhysicalMap;
        public ReteNode OwnerNode;

        public void Read(OsiReader reader)
        {
            Constants = new OsirisTuple();
            Constants.Read(reader);

            LogicalIndices = new List<sbyte>();
            var count = reader.ReadByte();
            while (count-- > 0)
            {
                LogicalIndices.Add(reader.ReadSByte());
            }

            LogicalToPhysicalMap = new Dictionary<byte, byte>();
            count = reader.ReadByte();
            while (count-- > 0)
            {
                var key = reader.ReadByte();
                var value = reader.ReadByte();
                LogicalToPhysicalMap.Add(key, value);
            }
        }

        public OsirisTuple Adapt(OsirisTuple columns)
        {
            var result = new OsirisTuple();
            for (var i = 0; i < LogicalIndices.Count; i++)
            {
                var index = LogicalIndices[i];
                if (index != -1)
                {
                    var value = columns.Logical[index];
                    result.Physical.Add(value);
                }
                else if (Constants.Logical.ContainsKey(i))
                {
                    var value = Constants.Logical[i];
                    result.Physical.Add(value);
                }
                else
                {
                    var nullValue = new OsirisVariable();
                    nullValue.TypeId = (uint)OsirisValueType.Unknown;
                    nullValue.Unused = true;
                    result.Physical.Add(nullValue);
                }
            }

            foreach (var map in LogicalToPhysicalMap)
            {
                result.Logical.Add(map.Key, result.Physical[map.Value]);
            }

            return result;
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            writer.Write("Adapter - ");
            if (OwnerNode != null && OwnerNode.Name.Length > 0)
            {
                writer.WriteLine("Node {0}/{1}", OwnerNode.Name, OwnerNode.NameIndex);
            }
            else if (OwnerNode != null)
            {
                writer.WriteLine("Node <{0}>", OwnerNode.TypeName());
            }
            else
            {
                writer.WriteLine("(Not owned)");
            }

            if (Constants.Logical.Count > 0)
            {
                writer.Write("    Constants: ");
                Constants.DebugDump(writer, story);
                writer.WriteLine("");
            }

            if (LogicalIndices.Count > 0)
            {
                writer.Write("    Logical indices: ");
                foreach (var index in LogicalIndices)
                {
                    writer.Write("{0}, ", index);
                }
                writer.WriteLine("");
            }

            if (LogicalToPhysicalMap.Count > 0)
            {
                writer.Write("    Logical to physical mappings: ");
                foreach (var pair in LogicalToPhysicalMap)
                {
                    writer.Write("{0} -> {1}, ", pair.Key, pair.Value);
                }
                writer.WriteLine("");
            }
        }
    }

    public class OsirisFact : OsirisSerializable
    {
        public List<OsirisValue> Columns;

        public void Read(OsiReader reader)
        {
            Columns = new List<OsirisValue>();
            var count = reader.ReadByte();
            while (count-- > 0)
            {
                var value = new OsirisValue();
                value.Read(reader);
                Columns.Add(value);
            }
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            writer.Write("(");
            for (var i = 0; i < Columns.Count; i++)
            {
                Columns[i].DebugDump(writer, story);
                if (i < Columns.Count - 1) writer.Write(", ");
            }
            writer.Write(")");
        }
    }

    public class OsirisDatabase : OsirisSerializable
    {
        public OsirisParameterList Parameters;
        public List<OsirisFact> Facts;
        public ReteNode OwnerNode;
        public long FactsPosition;

        public void Read(OsiReader reader)
        {
            Parameters = new OsirisParameterList();
            Parameters.Read(reader);

            FactsPosition = reader.BaseStream.Position;
            Facts = reader.ReadList<OsirisFact>();
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            if (OwnerNode != null && OwnerNode.Name.Length > 0)
            {
                writer.Write("{0}/{1}", OwnerNode.Name, OwnerNode.NameIndex);
            }
            else if (OwnerNode != null)
            {
                writer.Write("<{0}>", OwnerNode.TypeName());
            }
            else
            {
                writer.Write("(Not owned)");
            }

            writer.Write(" @ {0:X}: ", FactsPosition);
            Parameters.DebugDump(writer, story);

            writer.WriteLine("");
            writer.WriteLine("    Facts: ");
            foreach (var fact in Facts)
            {
                writer.Write("        ");
                fact.DebugDump(writer, story);
                writer.WriteLine();
            }
        }
    }

    public class OsirisGoal : OsirisSerializable
    {
        public UInt32 Index;
        public string Name;
        public byte SubGoalCombination;
        public List<UInt32> ParentGoals;
        public List<UInt32> SubGoals;
        public byte Unknown; // 0x02 = Child goal
        public List<ReteCall> InitCalls;
        public List<ReteCall> ExitCalls;

        public void Read(OsiReader reader)
        {
            Index = reader.ReadUInt32();
            Name = reader.ReadString();
            SubGoalCombination = reader.ReadByte();

            ParentGoals = new List<uint>();
            var numItems = reader.ReadUInt32();
            while (numItems-- > 0)
            {
                ParentGoals.Add(reader.ReadUInt32());
            }

            SubGoals = new List<uint>();
            numItems = reader.ReadUInt32();
            while (numItems-- > 0)
            {
                SubGoals.Add(reader.ReadUInt32());
            }

            Unknown = reader.ReadByte();

            if (reader.MajorVersion >= 1 && reader.MinorVersion >= 1)
            {
                InitCalls = reader.ReadList<ReteCall>();
                ExitCalls = reader.ReadList<ReteCall>();
            }
            else
            {
                InitCalls = new List<ReteCall>();
                ExitCalls = new List<ReteCall>();
            }
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            writer.WriteLine("{0}: SGC {1}, Unknown {2}", Name, SubGoalCombination, Unknown);

            if (ParentGoals.Count > 0)
            {
                writer.Write("    Parent goals: ");
                foreach (var goalId in ParentGoals)
                {
                    var goal = story.Goals[goalId];
                    writer.Write("#{0} {1}, ", goalId, goal.Name);
                }
                writer.WriteLine();
            }

            if (SubGoals.Count > 0)
            {
                writer.Write("    Subgoals: ");
                foreach (var goalId in SubGoals)
                {
                    var goal = story.Goals[goalId];
                    writer.Write("#{0} {1}, ", goalId, goal.Name);
                }
                writer.WriteLine();
            }

            if (InitCalls.Count > 0)
            {
                writer.WriteLine("    Init Calls: ");
                foreach (var call in InitCalls)
                {
                    writer.Write("        ");
                    call.DebugDump(writer, story);
                    writer.WriteLine();
                }
            }

            if (ExitCalls.Count > 0)
            {
                writer.WriteLine("    Exit Calls: ");
                foreach (var call in ExitCalls)
                {
                    writer.Write("        ");
                    call.DebugDump(writer, story);
                    writer.WriteLine();
                }
            }
        }

        public void MakeScript(TextWriter writer, Story story)
        {
            writer.WriteLine("Version 1");
            writer.WriteLine("SubGoalCombiner SGC_AND");
            writer.WriteLine();
            writer.WriteLine("INITSECTION");

            var nullTuple = new OsirisTuple();
            foreach (var call in InitCalls)
            {
                call.MakeScript(writer, story, nullTuple);
                writer.WriteLine();
            }

            writer.WriteLine();
            writer.WriteLine("KBSECTION");

            foreach (var node in story.Nodes)
            {
                if (node.Value is ReteRuleNode)
                {
                    var rule = node.Value as ReteRuleNode;
                    if (rule.DerivedGoalId == Index)
                    {
                        node.Value.MakeScript(writer, story, nullTuple);
                        writer.WriteLine();
                    }
                }
            }

            writer.WriteLine();
            writer.WriteLine("EXITSECTION");

            foreach (var call in ExitCalls)
            {
                call.MakeScript(writer, story, nullTuple);
                writer.WriteLine();
            }

            writer.WriteLine("ENDEXITSECTION");
            writer.WriteLine();

            foreach (var goalId in SubGoals)
            {
                var goal = story.Goals[goalId];
                writer.WriteLine("ParentTargetEdge \"{0}\"", goal.Name);
            }

            foreach (var goalId in ParentGoals)
            {
                var goal = story.Goals[goalId];
                writer.WriteLine("TargetEdge \"{0}\"", goal.Name);
            }
        }
    }

    public class Story
    {
        public OsirisHeader Header;
        public Dictionary<uint, OsirisType> Types;
        public List<OsirisDivObject> DivObjects;
        public List<OsirisFunction> Functions;
        public Dictionary<uint, ReteNode> Nodes;
        public Dictionary<uint, OsirisAdapter> Adapters;
        public Dictionary<uint, OsirisDatabase> Databases;
        public Dictionary<uint, OsirisGoal> Goals;
        public List<ReteCall> GlobalActions;

        public void DebugDump(TextWriter writer)
        {
            writer.WriteLine(" --- TYPES ---");
            foreach (var type in Types)
            {
                type.Value.DebugDump(writer);
            }

            writer.WriteLine();
            writer.WriteLine(" --- DIV OBJECTS ---");
            foreach (var obj in DivObjects)
            {
                obj.DebugDump(writer);
            }

            writer.WriteLine();
            writer.WriteLine(" --- FUNCTIONS ---");
            foreach (var function in Functions)
            {
                function.DebugDump(writer, this);
            }

            writer.WriteLine();
            writer.WriteLine(" --- NODES ---");
            foreach (var node in Nodes)
            {
                writer.Write("#{0} ", node.Key);
                node.Value.DebugDump(writer, this);
                writer.WriteLine();
            }

            writer.WriteLine();
            writer.WriteLine(" --- ADAPTERS ---");
            foreach (var adapter in Adapters)
            {
                writer.Write("#{0} ", adapter.Key);
                adapter.Value.DebugDump(writer, this);
            }

            writer.WriteLine();
            writer.WriteLine(" --- DATABASES ---");
            foreach (var database in Databases)
            {
                writer.Write("#{0} ", database.Key);
                database.Value.DebugDump(writer, this);
            }

            writer.WriteLine();
            writer.WriteLine(" --- GOALS ---");
            foreach (var goal in Goals)
            {
                writer.Write("#{0} ", goal.Key);
                goal.Value.DebugDump(writer, this);
                writer.WriteLine();
            }

            writer.WriteLine();
            writer.WriteLine(" --- GLOBAL ACTIONS ---");
            foreach (var call in GlobalActions)
            {
                call.DebugDump(writer, this);
                writer.WriteLine();
            }
        }
    }

    public class StoryReader
    {
        public StoryReader()
        {

        }

        private Dictionary<uint, OsirisType> ReadTypes(OsiReader reader)
        {
            var types = new Dictionary<uint, OsirisType>();
            var count = reader.ReadUInt32();
            while (count-- > 0)
            {
                var type = new OsirisType();
                type.Read(reader);
                types.Add(type.Index, type);
            }

            return types;
        }

        private Dictionary<uint, ReteNode> ReadNodes(OsiReader reader)
        {
            var nodes = new Dictionary<uint, ReteNode>();
            var count = reader.ReadUInt32();
            while (count-- > 0)
            {
                ReteNode node = null;
                var type = reader.ReadByte();
                var nodeId = reader.ReadUInt32();
                switch ((ReteNodeType)type)
                {
                    case ReteNodeType.Database:
                        node = new ReteDatabaseNode();
                        break;

                    case ReteNodeType.Proc:
                        node = new ReteProcNode();
                        break;

                    case ReteNodeType.DivQuery:
                        node = new ReteDivQueryNode();
                        break;

                    case ReteNodeType.InternalQuery:
                        node = new ReteInternalQueryNode();
                        break;

                    case ReteNodeType.And:
                        node = new ReteAndNode();
                        break;

                    case ReteNodeType.NotAnd:
                        node = new ReteNotAndNode();
                        break;

                    case ReteNodeType.RelOp:
                        node = new ReteRelOpNode();
                        break;

                    case ReteNodeType.Rule:
                        node = new ReteRuleNode();
                        break;

                    case ReteNodeType.UserQuery:
                        node = new ReteUserQueryNode();
                        break;

                    default:
                        throw new NotImplementedException("No serializer found for this node type");
                }

                node.Read(reader);
                nodes.Add(nodeId, node);
            }

            return nodes;
        }

        private Dictionary<uint, OsirisAdapter> ReadAdapters(OsiReader reader)
        {
            var adapters = new Dictionary<uint, OsirisAdapter>();
            var count = reader.ReadUInt32();
            while (count-- > 0)
            {
                var adapterId = reader.ReadUInt32();
                var adapter = new OsirisAdapter();
                adapter.Read(reader);
                adapters.Add(adapterId, adapter);
            }

            return adapters;
        }

        private Dictionary<uint, OsirisDatabase> ReadDatabases(OsiReader reader)
        {
            var databases = new Dictionary<uint, OsirisDatabase>();
            var count = reader.ReadUInt32();
            while (count-- > 0)
            {
                var databaseId = reader.ReadUInt32();
                var database = new OsirisDatabase();
                database.Read(reader);
                databases.Add(databaseId, database);
            }

            return databases;
        }

        private Dictionary<uint, OsirisGoal> ReadGoals(OsiReader reader)
        {
            var goals = new Dictionary<uint, OsirisGoal>();
            var count = reader.ReadUInt32();
            while (count-- > 0)
            {
                var goal = new OsirisGoal();
                goal.Read(reader);
                goals.Add(goal.Index, goal);
            }

            return goals;
        }

        public Story Read(Stream stream)
        {
            using (var reader = new OsiReader(stream))
            {
                var story = new Story();
                var header = new OsirisHeader();
                header.Read(reader);
                reader.MinorVersion = header.MinorVersion;
                reader.MajorVersion = header.MajorVersion;

                if (reader.MajorVersion > 1 || (reader.MajorVersion == 1 && reader.MinorVersion > 7))
                {
                    var msg = String.Format(
                        "Osiris version v{0}.{1} unsupported; this tool supports versions up to v1.7.",
                        reader.MajorVersion, reader.MinorVersion
                    );
                    throw new InvalidDataException(msg);
                }

                if (reader.MajorVersion >= 1 && reader.MinorVersion >= 4)
                    reader.Scramble = 0xAD;


                if (reader.MajorVersion >= 1 && reader.MinorVersion >= 5)
                    story.Types = ReadTypes(reader);
                else
                    story.Types = new Dictionary<uint, OsirisType>();

                story.Types[0] = new OsirisType();
                story.Types[0].Index = 0;
                story.Types[0].Name = "UNKNOWN";
                story.Types[1] = new OsirisType();
                story.Types[1].Index = 1;
                story.Types[1].Name = "INTEGER";
                story.Types[2] = new OsirisType();
                story.Types[2].Index = 2;
                story.Types[2].Name = "FLOAT";
                story.Types[3] = new OsirisType();
                story.Types[3].Index = 3;
                story.Types[3].Name = "STRING";

                story.DivObjects = reader.ReadList<OsirisDivObject>();
                story.Functions = reader.ReadList<OsirisFunction>();
                story.Nodes = ReadNodes(reader);
                story.Adapters = ReadAdapters(reader);
                story.Databases = ReadDatabases(reader);
                story.Goals = ReadGoals(reader);
                story.GlobalActions = reader.ReadList<ReteCall>();

                foreach (var node in story.Nodes)
                {
                    if (node.Value.DatabaseRef.IsValid())
                    {
                        var database = story.Databases[node.Value.DatabaseRef.DatabaseIndex];
                        if (database.OwnerNode != null)
                        {
                            throw new InvalidDataException("A database cannot be assigned to multiple database nodes!");
                        }

                        database.OwnerNode = node.Value;
                    }

                    if (node.Value is ReteRuleNode)
                    {
                        // Remove the __DEF__ postfix that is added to the end of Query nodes
                        var rule = node.Value as ReteRuleNode;
                        if (rule.IsQuery)
                        {
                            var ruleRoot = rule.GetRoot(story);
                            if (ruleRoot.Name != null && 
                                ruleRoot.Name.Length > 7 &&
                                ruleRoot.Name.Substring(ruleRoot.Name.Length - 7) == "__DEF__")
                            {
                                ruleRoot.Name = ruleRoot.Name.Substring(0, ruleRoot.Name.Length - 7);
                            }
                        }
                    }

                    if (node.Value is ReteDataNode)
                    {
                        var data = node.Value as ReteDataNode;
                        foreach (var reference in data.ReferencedBy)
                        {
                            if (reference.NodeRef.IsValid())
                            {
                                var ruleNode = story.Nodes[reference.NodeRef.NodeIndex];
                                if (reference.GoalId > 0 &&
                                    ruleNode is ReteRuleNode)
                                {
                                    (ruleNode as ReteRuleNode).DerivedGoalId = reference.GoalId;
                                }
                            }
                        }
                    }

                    if (node.Value is ReteTreeNode)
                    {
                        var tree = node.Value as ReteTreeNode;
                        if (tree.NextNode.NodeRef.IsValid())
                        {
                            var nextNode = story.Nodes[tree.NextNode.NodeRef.NodeIndex];
                            if (nextNode is ReteRuleNode)
                            {
                                (nextNode as ReteRuleNode).DerivedGoalId = tree.NextNode.GoalId;
                            }
                        }
                    }
                    
                    if (node.Value is ReteRelNode)
                    {
                        var rel = node.Value as ReteRelNode;
                        if (rel.AdapterRef.IsValid())
                        {
                            var adapter = story.Adapters[rel.AdapterRef.AdapterIndex];
                            if (adapter.OwnerNode != null)
                            {
                                throw new InvalidDataException("An adapter cannot be assigned to multiple join/rel nodes!");
                            }

                            adapter.OwnerNode = node.Value;
                        }
                    }
                    else if (node.Value is ReteJoinNode)
                    {
                        var join = node.Value as ReteJoinNode;
                        if (join.Adapter1Ref.IsValid())
                        {
                            var adapter = story.Adapters[join.Adapter1Ref.AdapterIndex];
                            if (adapter.OwnerNode != null)
                            {
                                throw new InvalidDataException("An adapter cannot be assigned to multiple join/rel nodes!");
                            }

                            adapter.OwnerNode = node.Value;
                        }

                        if (join.Adapter2Ref.IsValid())
                        {
                            var adapter = story.Adapters[join.Adapter2Ref.AdapterIndex];
                            if (adapter.OwnerNode != null)
                            {
                                throw new InvalidDataException("An adapter cannot be assigned to multiple join/rel nodes!");
                            }

                            adapter.OwnerNode = node.Value;
                        }
                    }
                }

                return story;
            }
        }
    }
}
