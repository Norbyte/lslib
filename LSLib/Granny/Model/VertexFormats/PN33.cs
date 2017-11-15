using System;
using System.Collections.Generic;
using LSLib.Granny.GR2;

#pragma warning disable 0649

namespace LSLib.Granny.Model.VertexFormats
{
    [StructSerialization(MixedMarshal = true)]
    internal class PN33_Prototype
    {
        [Serialization(ArraySize = 3)]
        public float[] Position;
        [Serialization(ArraySize = 3)]
        public float[] Normal;
    }

    [VertexPrototype(Prototype = typeof(PN33_Prototype)),
    VertexDescription(Position = true, Normal = true)]
    public class PN33 : Vertex
    {
        public override List<String> ComponentNames()
        {
            return new List<String> { "Position", "Normal" };
        }

        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
            WriteVector3(section, Normal);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
            Normal = ReadVector3(reader);
        }
    }
}
