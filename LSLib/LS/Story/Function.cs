using System;
using System.Collections.Generic;
using System.IO;

namespace LSLib.LS.Story
{
    public class FunctionSignature : OsirisSerializable
    {
        public string Name;
        public List<byte> OutParamMask;
        public ParameterList Parameters;

        public void Read(OsiReader reader)
        {
            Name = reader.ReadString();
            OutParamMask = new List<byte>();
            var outParamBytes = reader.ReadUInt32();
            while (outParamBytes-- > 0)
            {
                OutParamMask.Add(reader.ReadByte());
            }

            Parameters = new ParameterList();
            Parameters.Read(reader);
        }

        public void Write(OsiWriter writer)
        {
            writer.Write(Name);

            writer.Write((UInt32)OutParamMask.Count);
            foreach (var b in OutParamMask)
            {
                writer.Write(b);
            }

            Parameters.Write(writer);
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            writer.Write(Name);
            writer.Write("(");
            for (var i = 0; i < Parameters.Types.Count; i++)
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

    public class ParameterList : OsirisSerializable
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

        public void Write(OsiWriter writer)
        {
            writer.Write((byte)Types.Count);
            foreach (var type in Types)
            {
                writer.Write(type);
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

    public enum FunctionType
    {
        Event = 1,
        Query = 2,
        Call = 3,
        Database = 4,
        Proc = 5,
        SysQuery = 6,
        SysCall = 7
    }

    public class Function : OsirisSerializable
    {
        public UInt32 Line;
        public UInt32 Unknown1;
        public UInt32 Unknown2;
        public NodeReference NodeRef;
        public FunctionType Type;
        public Guid GUID;
        public FunctionSignature Name;

        public void Read(OsiReader reader)
        {
            Line = reader.ReadUInt32();
            Unknown1 = reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
            NodeRef = reader.ReadNodeRef();
            Type = (FunctionType)reader.ReadByte();
            GUID = reader.ReadGuid();
            Name = new FunctionSignature();
            Name.Read(reader);
        }

        public void Write(OsiWriter writer)
        {
            writer.Write(Line);
            writer.Write(Unknown1);
            writer.Write(Unknown2);
            NodeRef.Write(writer);
            writer.Write((byte)Type);
            writer.Write(GUID);
            Name.Write(writer);
        }

        public void DebugDump(TextWriter writer, Story story)
        {
            writer.Write("{0} ", Type.ToString());
            Name.DebugDump(writer, story);
            if (NodeRef.IsValid)
            {
                var node = NodeRef.Resolve();
                writer.Write(" @ {0}({1})", node.Name, node.NumParams);
            }

            writer.WriteLine(" [{0}, {1}]", Unknown1, Unknown2);
        }
    }
}
