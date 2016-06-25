using LSLib.Granny.GR2;
using System;
using System.Collections.Generic;

#pragma warning disable 0649

namespace LSLib.Granny.Model.VertexFormat
{
    [StructSerialization(MixedMarshal = true)]
    internal class PWNT3432_Prototype
    {
        [Serialization(ArraySize = 3)]
        public float[] Position;
        [Serialization(ArraySize = 4, Type = MemberType.NormalUInt8)]
        public byte[] BoneWeights;
        [Serialization(ArraySize = 4)]
        public byte[] BoneIndices;
        [Serialization(ArraySize = 3)]
        public float[] Normal;
        [Serialization(ArraySize = 2)]
        public float[] TextureCoordinates0;
    }

    [VertexPrototype(Prototype = typeof(PWNT3432_Prototype)),
    VertexDescription(Position = true, BoneWeights = true, BoneIndices = true, Normal = true, TextureCoordinates = 1)]
    public class PWNT3432 : Vertex
    {
        public override List<String> ComponentNames()
        {
            return new List<String> { "Position", "BoneWeights", "BoneIndices", "Normal", "MaxChannel_1" };
        }

        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
            WriteInfluences(section, BoneWeights);
            WriteInfluences(section, BoneIndices);
            WriteVector3(section, Normal);
            WriteVector2(section, TextureCoordinates0);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
            BoneWeights = ReadInfluences(reader);
            BoneIndices = ReadInfluences(reader);
            Normal = ReadVector3(reader);
            TextureCoordinates0 = ReadVector2(reader);
        }
    }
}
