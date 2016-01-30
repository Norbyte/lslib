using LSLib.Granny.GR2;
using System;

#pragma warning disable 0649

namespace LSLib.Granny.Model.VertexFormat
{
    [StructSerialization(MixedMarshal = true)]
    internal class PT32_Prototype
    {
        [Serialization(ArraySize = 3)]
        public float[] Position;
        [Serialization(ArraySize = 2)]
        public float[] TextureCoordinates0;
    }

    [VertexPrototype(Prototype = typeof(PT32_Prototype)),
    VertexDescription(Position = true, TextureCoordinates = true)]
    public class PT32 : Vertex
    {
        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
            WriteVector2(section, TextureCoordinates0);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
            TextureCoordinates0 = ReadVector2(reader);
        }
    }
}
