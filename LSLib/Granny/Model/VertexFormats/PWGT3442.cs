using System;
using System.Collections.Generic;
using LSLib.Granny.GR2;

#pragma warning disable 0649

namespace LSLib.Granny.Model.VertexFormats
{
    [StructSerialization(MixedMarshal = true)]
    internal class PWHGT3442_Prototype
    {
        [Serialization(ArraySize = 3)]
        public float[] Position;
        [Serialization(ArraySize = 4, Type = MemberType.NormalUInt8)]
        public byte[] BoneWeights;
        [Serialization(ArraySize = 4)]
        public byte[] BoneIndices;
        [Serialization(ArraySize = 4)]
        public ushort[] QTangent;
        [Serialization(ArraySize = 2)]
        public ushort[] TextureCoordinates0;
    }

    [VertexPrototype(Prototype = typeof(PWHGT3442_Prototype)),
    VertexDescription(Position = true, BoneWeights = true, BoneIndices = true, Tangent = true, TextureCoordinates = 1)]
    public class PWHGT3442 : Vertex
    {
        public override List<String> ComponentNames()
        {
            return new List<String> { "Position", "BoneWeights", "BoneIndices", "Tangent", "MaxChannel_1" };
        }

        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
            WriteInfluences(section, BoneWeights);
            WriteInfluences(section, BoneIndices);
            WriteHalfVector3As4(section, Tangent);
            WriteHalfVector2(section, TextureCoordinates0);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
            BoneWeights = ReadInfluences(reader);
            BoneIndices = ReadInfluences(reader);
            Tangent = ReadHalfVector4As3(reader);
            TextureCoordinates0 = ReadHalfVector2(reader);
        }
    }
}
