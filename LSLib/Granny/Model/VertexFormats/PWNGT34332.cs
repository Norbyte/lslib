using System;
using System.Collections.Generic;
using LSLib.Granny.GR2;

#pragma warning disable 0649

namespace LSLib.Granny.Model.VertexFormats
{
    [StructSerialization(MixedMarshal = true)]
    internal class PWNGT34332_Prototype
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
        [Serialization(ArraySize = 2)]
        public float[] TextureCoordinates0;
    }

    [VertexPrototype(Prototype = typeof(PWNGT34332_Prototype)),
    VertexDescription(Position = true, BoneWeights = true, BoneIndices = true, Normal = true, Tangent = true, TextureCoordinates = 1)]
    public class PWNGT34332 : Vertex
    {
        public override List<String> ComponentNames()
        {
            return new List<String> { "Position", "BoneWeights", "BoneIndices", "Normal", "Tangent", "MaxChannel_1" };
        }

        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
            WriteInfluences(section, BoneWeights);
            WriteInfluences(section, BoneIndices);
            WriteVector3(section, Normal);
            WriteVector3(section, Tangent);
            WriteVector2(section, TextureCoordinates0);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
            BoneWeights = ReadInfluences(reader);
            BoneIndices = ReadInfluences(reader);
            Normal = ReadVector3(reader);
            Tangent = ReadVector3(reader);
            TextureCoordinates0 = ReadVector2(reader);
        }
    }
}
