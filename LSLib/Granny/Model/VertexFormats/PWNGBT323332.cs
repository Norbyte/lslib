using LSLib.Granny.GR2;
using System;

#pragma warning disable 0649

namespace LSLib.Granny.Model.VertexFormat
{
    [StructSerialization(MixedMarshal = true)]
    internal class PWNGBT323332_Prototype
    {
        [Serialization(ArraySize = 3)]
        public float[] Position;
        [Serialization(ArraySize = 2, Type = MemberType.NormalUInt8)]
        public byte[] BoneWeights;
        [Serialization(ArraySize = 2)]
        public byte[] BoneIndices;
        [Serialization(ArraySize = 3)]
        public float[] Normal;
        [Serialization(ArraySize = 3)]
        public float[] Tangent;
        [Serialization(ArraySize = 3)]
        public float[] Binormal;
        [Serialization(ArraySize = 2)]
        public float[] TextureCoordinates0;
    }

    [VertexPrototype(Prototype = typeof(PWNGBT323332_Prototype)),
    VertexDescription(Position = true, BoneWeights = true, BoneIndices = true, Normal = true, Tangent = true, Binormal = true, TextureCoordinates = true)]
    public class PWNGBT323332 : Vertex
    {
        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
            WriteInfluences2(section, BoneWeights);
            WriteInfluences2(section, BoneIndices);
            WriteVector3(section, Normal);
            WriteVector3(section, Tangent);
            WriteVector3(section, Binormal);
            WriteVector2(section, TextureCoordinates0);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
            BoneWeights = ReadInfluences2(reader);
            BoneIndices = ReadInfluences2(reader);
            Normal = ReadVector3(reader);
            Tangent = ReadVector3(reader);
            Binormal = ReadVector3(reader);
            TextureCoordinates0 = ReadVector2(reader);
        }
    }
}
