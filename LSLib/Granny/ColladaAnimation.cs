using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Collada141;
using OpenTK;
using LSLib.Granny.GR2;
using LSLib.Granny.Model.CurveData;

namespace LSLib.Granny.Model
{
    public class ColladaAnimation
    {
        private animation Animation;
        private Dictionary<String, Source> Sources;
        private InputLocalOffset[] Inputs;
        private List<Matrix4> Transforms;
        private List<Single> Times;
        private Bone Bone;

        private void ImportSources()
        {
            Sources = new Dictionary<String, Source>();
            foreach (var item in Animation.Items)
            {
                if (item is source)
                {
                    var src = Source.FromCollada(item as source);
                    Sources.Add(src.id, src);
                }
            }
        }

        private void ImportSampler()
        {
            sampler sampler = null;
            foreach (var item in Animation.Items)
            {
                if (item is sampler)
                {
                    sampler = item as sampler;
                    break;
                }
            }

            if (sampler == null)
                throw new ParsingException("Animation " + Animation.id + " has no sampler!");

            Source inputSource = null, outputSource = null, interpolationSource = null;
            foreach (var input in sampler.input)
            {
                if (input.source[0] != '#')
                    throw new ParsingException("Only ID references are supported for animation input sources");

                Source source;
                if (!Sources.TryGetValue(input.source.Substring(1), out source))
                    throw new ParsingException("Animation sampler " + input.semantic + " references nonexistent source: " + input.source);

                switch (input.semantic)
                {
                    case "INPUT":
                        inputSource = source;
                        break;

                    case "OUTPUT":
                        outputSource = source;
                        break;

                    case "INTERPOLATION":
                        interpolationSource = source;
                        break;

                    default:
                        break;
                }
            }

            if (inputSource == null || outputSource == null || interpolationSource == null)
                throw new ParsingException("Animation " + Animation.id + " must have an INPUT, OUTPUT and INTERPOLATION sampler input!");

            if (!inputSource.FloatParams.TryGetValue("TIME", out Times))
                Times = inputSource.FloatParams.Values.SingleOrDefault();

            if (Times == null)
                throw new ParsingException("Animation " + Animation.id + " INPUT must have a TIME parameter!");

            if (!outputSource.MatrixParams.TryGetValue("TRANSFORM", out Transforms))
                Transforms = outputSource.MatrixParams.Values.SingleOrDefault();

            if (Transforms == null)
                throw new ParsingException("Animation " + Animation.id + " OUTPUT must have a TRANSFORM parameter!");

            if (Transforms.Count != Times.Count)
                throw new ParsingException("Animation " + Animation.id + " has different time and transform counts!");

            for (var i = 0; i < Transforms.Count; i++ )
            {
                var m = Transforms[i];
                m.Transpose();
                Transforms[i] = m;
            }
        }

        private void ImportChannel(Skeleton skeleton)
        {
            channel channel = null;
            foreach (var item in Animation.Items)
            {
                if (item is channel)
                {
                    channel = item as channel;
                    break;
                }
            }

            if (channel == null)
                throw new ParsingException("Animation " + Animation.id + " has no channel!");

            var parts = channel.target.Split(new char[] { '/' });
            if (parts.Length != 2)
                throw new ParsingException("Unsupported channel target format: " + channel.target);

            Bone bone = null;
            if (!skeleton.BonesByID.TryGetValue(parts[0], out bone))
                throw new ParsingException("Animation channel references nonexistent bone: " + parts[0]);

            if (bone.TransformSID != parts[1])
                throw new ParsingException("Animation channel references nonexistent transform or transform is not float4x4: " + channel.target);

            Bone = bone;
        }

        public bool ImportFromCollada(animation colladaAnim, Skeleton skeleton)
        {
            Animation = colladaAnim;
            ImportSources();
            ImportSampler();

            // Avoid importing empty animations
            if (Transforms.Count == 0)
                return false;

            ImportChannel(skeleton);
            return true;
        }

        private Matrix3 ScaleToScaleShear(Vector3 scale)
        {
            var m = Matrix3.Identity;
            m[0, 0] *= scale.X;
            m[1, 1] *= scale.Y;
            m[2, 2] *= scale.Z;
            return m;
        }

        public TransformTrack MakeTrack()
        {
            var track = new TransformTrack();
            track.Flags = 0;
            track.Name = Bone.Name;

            var positions = Transforms.Select(m => m.ExtractTranslation()).ToList();
            var rotations = Transforms.Select(m => m.ExtractRotation()).ToList();
            var scales = Transforms.Select(m => ScaleToScaleShear(m.ExtractScale())).ToList();

            // Quaternion sign fixup
            // Since GR2 interpolation operates on the raw XYZ values of the quaternion, two subsequent quaternions
            // that express the same rotation (eg. [1, 0.5, 0.5, -0.5] and [1, -0.5, -0.5, 0.5]) will result in a 360 deg
            // rotation during the animation. Shuffle XYZ signs around to make this less likely to happen
            for (var i = 1; i < rotations.Count; i++)
            {
                var r0 = rotations[i - 1];
                var r1 = rotations[i];
                float mx = Math.Max(Math.Abs(r1.X), Math.Abs(r0.X)),
                    my = Math.Max(Math.Abs(r1.Y), Math.Abs(r0.Y)),
                    mz = Math.Max(Math.Abs(r1.Z), Math.Abs(r0.Z));
                bool dx = Math.Abs(r1.X - r0.X) > Math.Abs(r1.X + r0.X) && mx > 0.01,
                    dy = Math.Abs(r1.Y - r0.Y) > Math.Abs(r1.Y + r0.Y) && my > 0.01,
                    dz = Math.Abs(r1.Z - r0.Z) > Math.Abs(r1.Z + r0.Z) && mz > 0.01;
                int d = (dx ? 1 : 0) + (dy ? 1 : 0) + (dz ? 1 : 0);
                if (d >= 2 && (mx > 0.5 || my > 0.5 || mz > 0.5))
                {
                    rotations[i] = new Quaternion(-r1.X, -r1.Y, -r1.Z, r1.W);
                }
            }

            var posCurve = new DaK32fC32f();
            posCurve.CurveDataHeader_DaK32fC32f = new CurveDataHeader { Format = (int)CurveFormat.DaK32fC32f, Degree = 2 };
            posCurve.SetKnots(Times);
            posCurve.SetPoints(positions);
            track.PositionCurve = new AnimationCurve { CurveData = posCurve };

            var rotCurve = new DaK32fC32f();
            rotCurve.CurveDataHeader_DaK32fC32f = new CurveDataHeader { Format = (int)CurveFormat.DaK32fC32f, Degree = 2 };
            rotCurve.SetKnots(Times);
            rotCurve.SetQuaternions(rotations);
            track.OrientationCurve = new AnimationCurve { CurveData = rotCurve };

            var scaleCurve = new DaK32fC32f();
            scaleCurve.CurveDataHeader_DaK32fC32f = new CurveDataHeader { Format = (int)CurveFormat.DaK32fC32f, Degree = 2 };
            scaleCurve.SetKnots(Times);
            scaleCurve.SetMatrices(scales);
            track.ScaleShearCurve = new AnimationCurve { CurveData = scaleCurve };

            return track;
        }
    }
}
