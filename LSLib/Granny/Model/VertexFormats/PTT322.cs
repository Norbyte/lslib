using System;
using System.Collections.Generic;
using LSLib.Granny.GR2;

#pragma warning disable 0649

namespace LSLib.Granny.Model.VertexFormats
{
    [StructSerialization(MixedMarshal = true)]
    internal class PTT322_Prototype
    {
        [Serialization(ArraySize = 3)]
        public float[] Position;
        [Serialization(ArraySize = 2)]
        public float[] TextureCoordinates0;
        [Serialization(ArraySize = 2)]
        public float[] TextureCoordinates1;
    }

    [VertexPrototype(Prototype = typeof(PTT322_Prototype)),
    VertexDescription(Position = true, TextureCoordinates = 2)]
    public class PTT322 : Vertex
    {
        public override List<String> ComponentNames()
        {
            return new List<String> { "Position", "MaxChannel_1", "MaxChannel_2" };
        }

        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
            WriteVector2(section, TextureCoordinates1);
            WriteVector2(section, TextureCoordinates0);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
            TextureCoordinates0 = ReadVector2(reader);
            TextureCoordinates1 = ReadVector2(reader);
        }
    }
}
