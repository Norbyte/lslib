using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model.CurveData
{
    public class D9I3K16uC16u : AnimationCurveData
    {
        [Serialization(Type = MemberType.Inline)]
        public CurveDataHeader CurveDataHeader_D9I3K16uC16u;
        public UInt16 OneOverKnotScaleTrunc;
        [Serialization(ArraySize = 3)]
        public float[] ControlScales;
        [Serialization(ArraySize = 3)]
        public float[] ControlOffsets;
        [Serialization(Prototype = typeof(ControlUInt16), Kind = SerializationKind.UserMember, Serializer = typeof(UInt16ListSerializer))]
        public List<UInt16> KnotsControls;

        public override int NumKnots()
        {
            return KnotsControls.Count / 4;
        }

        public override List<float> GetKnots()
        {
            var scale = ConvertOneOverKnotScaleTrunc(OneOverKnotScaleTrunc);
            var numKnots = NumKnots();
            var knots = new List<float>(numKnots);
            for (var i = 0; i < numKnots; i++)
                knots.Add((float)KnotsControls[i] / scale);

            return knots;
        }

        public override List<Quaternion> GetQuaternions()
        {
            throw new InvalidOperationException("D9I3K16uC16u is not a rotation curve!");
        }

        public override List<Matrix3> GetMatrices()
        {
            var numKnots = NumKnots();
            var knots = new List<Matrix3>(numKnots);
            for (var i = 0; i < numKnots; i++)
            {
                var mat = new Matrix3(
                    (float)KnotsControls[numKnots + i * 3 + 0] * ControlScales[0] + ControlOffsets[0], 0, 0,
                    0, (float)KnotsControls[numKnots + i * 3 + 1] * ControlScales[1] + ControlOffsets[1], 0,
                    0, 0, (float)KnotsControls[numKnots + i * 3 + 2] * ControlScales[2] + ControlOffsets[2]
                );
                knots.Add(mat);
            }

            return knots;
        }
    }
}
