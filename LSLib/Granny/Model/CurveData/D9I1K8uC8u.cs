using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model.CurveData
{
    public class D9I1K8uC8u : AnimationCurveData
    {
        [Serialization(Type = MemberType.Inline)]
        public CurveDataHeader CurveDataHeader_D9I1K8uC8u;
        public UInt16 OneOverKnotScaleTrunc;
        public float ControlScale;
        public float ControlOffset;
        [Serialization(Prototype = typeof(ControlUInt8), Kind = SerializationKind.UserMember, Serializer = typeof(UInt8ListSerializer))]
        public List<Byte> KnotsControls;

        public override int NumKnots()
        {
            return KnotsControls.Count / 2;
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
            throw new InvalidOperationException("D9I1K8uC8u is not a rotation curve!");
        }

        public override List<Matrix3> GetMatrices()
        {
            var numKnots = NumKnots();
            var knots = new List<Matrix3>(numKnots);
            for (var i = 0; i < numKnots; i++)
            {
                // TODO: Not sure if correct?
                var scale = (float)KnotsControls[numKnots + i] * ControlScale + ControlOffset;
                var mat = new Matrix3(
                    scale, 0, 0,
                    0, scale, 0,
                    0, 0, scale
                );
                knots.Add(mat);
            }

            return knots;
        }
    }
}
