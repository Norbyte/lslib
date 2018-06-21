using System;
using System.Collections.Generic;
using LSLib.Granny.GR2;

#pragma warning disable 0649

namespace LSLib.Granny.Model.VertexFormats
{
    [StructSerialization(MixedMarshal = true)]
    internal class PWN323_Prototype
    {
        [Serialization(ArraySize = 3)]
        public float[] Position;
        [Serialization(ArraySize = 2, Type = MemberType.NormalUInt8)]
        public byte[] BoneWeights;
        [Serialization(ArraySize = 2)]
        public byte[] BoneIndices;
        [Serialization(ArraySize = 3)]
        public float[] Normal;
    }

    [VertexPrototype(Prototype = typeof(PWN323_Prototype)),
    VertexDescription(Position = true, BoneWeights = true, BoneIndices = true, Normal = true)]
    public class PWN323 : Vertex
    {
        public override List<String> ComponentNames()
        {
            return new List<String> { "Position", "BoneWeights", "BoneIndices", "Normal" };
        }

        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
            WriteInfluences2(section, BoneWeights);
            WriteInfluences2(section, BoneIndices);
            WriteVector3(section, Normal);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
            BoneWeights = ReadInfluences2(reader);
            BoneIndices = ReadInfluences2(reader);
            Normal = ReadVector3(reader);
        }
    }
}
