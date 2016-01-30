using LSLib.Granny.GR2;
using System;

#pragma warning disable 0649

namespace LSLib.Granny.Model.VertexFormat
{
    [StructSerialization(MixedMarshal = true)]
    internal class PWNG3433_Prototype
    {
        [Serialization(ArraySize = 3)]
        public float[] Position;
        [Serialization(ArraySize = 4, Type = MemberType.NormalUInt8)]
        public byte[] BoneWeights;
        [Serialization(ArraySize = 4)]
        public byte[] BoneIndices;
        [Serialization(ArraySize = 3)]
        public float[] Normal;
        [Serialization(ArraySize = 3)]
        public float[] Tangent;
    }

    [VertexPrototype(Prototype = typeof(PWNG3433_Prototype)),
    VertexDescription(Position = true, BoneWeights = true, BoneIndices = true, Normal = true, Tangent = true)]
    public class PWNG3433 : Vertex
    {
        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
            WriteInfluences(section, BoneWeights);
            WriteInfluences(section, BoneIndices);
            WriteVector3(section, Normal);
            WriteVector3(section, Tangent);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
            BoneWeights = ReadInfluences(reader);
            BoneIndices = ReadInfluences(reader);
            Normal = ReadVector3(reader);
            Tangent = ReadVector3(reader);
        }
    }
}
