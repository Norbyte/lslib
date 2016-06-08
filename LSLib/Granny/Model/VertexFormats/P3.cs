using LSLib.Granny.GR2;
using System;
using System.Collections.Generic;

#pragma warning disable 0649

namespace LSLib.Granny.Model.VertexFormat
{
    [StructSerialization(MixedMarshal = true)]
    internal class P3_Prototype
    {
        [Serialization(ArraySize = 3)]
        public float[] Position;
    }

    [VertexPrototype(Prototype = typeof(P3_Prototype)), 
    VertexDescription(Position = true)]
    public class P3 : Vertex
    {
        public override List<String> ComponentNames()
        {
            return new List<String> { "Position" };
        }

        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
        }
    }
}
