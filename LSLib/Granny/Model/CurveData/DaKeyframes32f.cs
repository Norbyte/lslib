using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model.CurveData
{
    public class DaKeyframes32f : AnimationCurveData
    {
        [Serialization(Type = MemberType.Inline)]
        public CurveDataHeader CurveDataHeader_DaKeyframes32f;
        public Int16 Dimension;
        [Serialization(Prototype = typeof(ControlReal32), Kind = SerializationKind.UserMember, Serializer = typeof(SingleListSerializer))]
        public List<Single> Controls;

        public ExportType CurveType()
        {
            if (Dimension == 3)
                return ExportType.Position;
            else if (Dimension == 4)
                return ExportType.Rotation;
            else if (Dimension == 9)
                return ExportType.ScaleShear;
            else
                throw new NotSupportedException("Unsupported DaKeyframes32f dimension number");
        }

        public override int NumKnots()
        {
            return Controls.Count / Dimension;
        }

        public override List<float> GetKnots()
        {
            var knots = new List<float>(NumKnots());
            for (var i = 0; i < NumKnots(); i++)
                knots.Add((float)i);

            return knots;
        }

        public void SetKnots(List<float> knots)
        {
            throw new NotSupportedException("Knots are fixed for DaKeyframes32f curves");
        }

        public override List<Vector3> GetPoints()
        {
            if (CurveType() != ExportType.Position)
                throw new InvalidOperationException("DaKeyframes32f: This curve is not a position curve!");

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
                throw new InvalidOperationException("DaKeyframes32f: This curve is not a scale/shear curve!");

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
                throw new InvalidOperationException("DaKeyframes32f: This curve is not a rotation curve!");

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
