using LSLib.Granny.GR2;
using System;

#pragma warning disable 0649

namespace LSLib.Granny.Model.VertexFormat
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

    [VertexPrototype(Prototype = typeof(PWN323_Prototype))]
    public class PWN323 : Vertex
    {
        public override bool HasBoneInfluences()
        {
            return true;
        }

        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
            WriteInfluences2(section, BoneWeights);
            WriteInfluences2(section, BoneIndices);
            WriteVector2(section, TextureCoordinates0);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
            BoneWeights = ReadInfluences2(reader);
            BoneIndices = ReadInfluences2(reader);
            TextureCoordinates0 = ReadVector2(reader);
        }
    }
}
