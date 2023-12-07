using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model.CurveData
{
    public class DaK32fC32f : AnimationCurveData
    {
        [Serialization(Type = MemberType.Inline)]
        public CurveDataHeader CurveDataHeader_DaK32fC32f;
        public Int16 Padding;
        [Serialization(Prototype = typeof(ControlReal32), Kind = SerializationKind.UserMember, Serializer = typeof(SingleListSerializer))]
        public List<Single> Knots;
        [Serialization(Prototype = typeof(ControlReal32), Kind = SerializationKind.UserMember, Serializer = typeof(SingleListSerializer))]
        public List<Single> Controls;

        public ExportType CurveType()
        {
            if (Knots.Count * 3 == Controls.Count)
                return ExportType.Position;
            else if (Knots.Count * 4 == Controls.Count)
                return ExportType.Rotation;
            else if (Knots.Count * 9 == Controls.Count)
                return ExportType.ScaleShear;
            else
                throw new NotSupportedException("Unsupported DaK32fC32f control data size");
        }

        public override int NumKnots()
        {
            return Knots.Count;
        }

        public override List<float> GetKnots()
        {
            var numKnots = NumKnots();
            var knots = new List<float>(numKnots);
            for (var i = 0; i < numKnots; i++)
                knots.Add(Knots[i]);

            return knots;
        }

        public void SetKnots(List<float> knots)
        {
            Knots = new List<float>(knots);
        }

        public override List<Vector3> GetPoints()
        {
            if (CurveType() != ExportType.Position)
                throw new InvalidOperationException("DaK32fC32f: This curve is not a position curve!");

            var numKnots = NumKnots();
            var positions = new List<Vector3>(numKnots);
            for (var i = 0; i < numKnots; i++)
            {
                var vec = new Vector3(
                    Controls[i * 3 + 0],
                    Controls[i * 3 + 1],
                    Controls[i * 3 + 2]
                );
                positions.Add(vec);
            }

            return positions;
        }

        public void SetPoints(List<Vector3> points)
        {
            Controls = points.SelectMany(p => new float[] { p.X, p.Y, p.Z }).ToList();
        }

        public override List<Matrix3> GetMatrices()
        {
            if (CurveType() != ExportType.ScaleShear)
                throw new InvalidOperationException("DaK32fC32f: This curve is not a scale/shear curve!");

            var numKnots = NumKnots();
            var scaleShear = new List<Matrix3>(numKnots);
            for (var i = 0; i < numKnots; i++)
            {
                var mat = new Matrix3(
                    Controls[i * 9 + 0],
                    Controls[i * 9 + 1],
                    Controls[i * 9 + 2],
                    Controls[i * 9 + 3],
                    Controls[i * 9 + 4],
                    Controls[i * 9 + 5],
                    Controls[i * 9 + 6],
                    Controls[i * 9 + 7],
                    Controls[i * 9 + 8]
                );
                scaleShear.Add(mat);
            }

            return scaleShear;
        }

        public void SetMatrices(List<Matrix3> matrices)
        {
            Controls = matrices.SelectMany(m => new float[] {
                m[0, 0],  m[0, 1], m[0, 2],
                m[1, 0],  m[1, 1], m[1, 2],
                m[2, 0],  m[2, 1], m[2, 2]
            }).ToList();
        }

        public override List<Quaternion> GetQuaternions()
        {
            if (CurveType() != ExportType.Rotation)
                throw new InvalidOperationException("DaK32fC32f: This curve is not a rotation curve!");

            var numKnots = NumKnots();
            var rotations = new List<Quaternion>(numKnots);
            for (var i = 0; i < numKnots; i++)
            {
                var quat = new Quaternion(
                    Controls[i * 4 + 0],
                    Controls[i * 4 + 1],
                    Controls[i * 4 + 2],
                    Controls[i * 4 + 3]
                );
                rotations.Add(quat);
            }

            return rotations;
        }

        public void SetQuaternions(List<Quaternion> quats)
        {
            Controls = quats.SelectMany(q => new float[] { q.X, q.Y, q.Z, q.W }).ToList();
        }
    }
}
