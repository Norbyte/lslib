using LSLib.Granny.GR2;
using System;
using System.Collections.Generic;

#pragma warning disable 0649

namespace LSLib.Granny.Model.VertexFormat
{
    [StructSerialization(MixedMarshal = true)]
    internal class PNGBTT33322_Prototype
    {
        [Serialization(ArraySize = 3)]
        public float[] Position;
        [Serialization(ArraySize = 3)]
        public float[] Normal;
        [Serialization(ArraySize = 3)]
        public float[] Tangent;
        [Serialization(ArraySize = 3)]
        public float[] Binormal;
        [Serialization(ArraySize = 2)]
        public float[] TextureCoordinates0;
        [Serialization(ArraySize = 2)]
        public float[] TextureCoordinates1;
    }

    [VertexPrototype(Prototype = typeof(PNGBTT33322_Prototype)),
    VertexDescription(Position = true, Normal = true, Tangent = true, Binormal = true, TextureCoordinates = 2)]
    public class PNGBTT33322 : Vertex
    {
        public override List<String> ComponentNames()
        {
            return new List<String> { "Position", "Normal", "Tangent", "Binormal", "MaxChannel_1", "MaxChannel_2" };
        }

        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
            WriteVector3(section, Normal);
            WriteVector3(section, Tangent);
            WriteVector3(section, Binormal);
            WriteVector2(section, TextureCoordinates0);
            WriteVector2(section, TextureCoordinates1);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
            Normal = ReadVector3(reader);
            Tangent = ReadVector3(reader);
            Binormal = ReadVector3(reader);
            TextureCoordinates0 = ReadVector2(reader);
            TextureCoordinates1 = ReadVector2(reader);
        }
    }
}
