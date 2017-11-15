using System;
using System.Collections.Generic;
using OpenTK;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model.CurveData
{
    public class D3Constant32f : AnimationCurveData
    {
        [Serialization(Type = MemberType.Inline)]
        public CurveDataHeader CurveDataHeader_D3Constant32f;
        public Int16 Padding;
        [Serialization(ArraySize = 3)]
        public float[] Controls;

        public override int NumKnots()
        {
            return 1;
        }

        public override List<float> GetKnots()
        {
            return new List<float>() { 0.0f };
        }

        public override List<Vector3> GetPoints()
        {
            return new List<Vector3>() { new Vector3(Controls[0], Controls[1], Controls[2]) };
        }
    }
}
