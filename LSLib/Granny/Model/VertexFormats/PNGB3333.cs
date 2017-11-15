using System;
using System.Collections.Generic;
using LSLib.Granny.GR2;

#pragma warning disable 0649

namespace LSLib.Granny.Model.VertexFormats
{
    [StructSerialization(MixedMarshal = true)]
    internal class PNGB3333_Prototype
    {
        [Serialization(ArraySize = 3)]
        public float[] Position;
        [Serialization(ArraySize = 3)]
        public float[] Normal;
        [Serialization(ArraySize = 3)]
        public float[] Tangent;
        [Serialization(ArraySize = 3)]
        public float[] Binormal;
    }

    [VertexPrototype(Prototype = typeof(PNGB3333_Prototype)),
    VertexDescription(Position = true, Normal = true, Tangent = true, Binormal = true)]
    public class PNGB3333 : Vertex
    {
        public override List<String> ComponentNames()
        {
            return new List<String> { "Position", "Normal", "Tangent", "Binormal" };
        }

        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
            WriteVector3(section, Normal);
            WriteVector3(section, Tangent);
            WriteVector3(section, Binormal);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
            Normal = ReadVector3(reader);
            Tangent = ReadVector3(reader);
            Binormal = ReadVector3(reader);
        }
    }
}
