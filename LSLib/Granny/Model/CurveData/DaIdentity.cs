using System;
using System.Collections.Generic;
using OpenTK;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model.CurveData
{
    public class DaIdentity : AnimationCurveData
    {
        [Serialization(Type = MemberType.Inline)]
        public CurveDataHeader CurveDataHeader_DaIdentity;
        public Int16 Dimension;

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
            return new List<Vector3>() { new Vector3(0.0f, 0.0f, 0.0f) };
        }

        public override List<Matrix3> GetMatrices()
        {
            return new List<Matrix3>() { Matrix3.Identity };
        }
    }
}
