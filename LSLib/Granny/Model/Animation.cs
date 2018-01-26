using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using LSLib.Granny.Model.CurveData;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model
{
    public class AnimationCurve
    {
        [Serialization(TypeSelector = typeof(AnimationCurveDataTypeSelector), Type = MemberType.VariantReference)]
        public AnimationCurveData CurveData;
    }

    public class Keyframe
    {
        public float time;
        public Vector3 translation;
        public Quaternion rotation;
        public bool hasTranslation;
        public bool hasRotation;
        public Matrix3 scaleShear;
        public bool hasScaleShear;
    };

    public class TransformTrack
    {
        public string Name;
        public int Flags;
        [Serialization(Type = MemberType.Inline)]
        public AnimationCurve OrientationCurve;
        [Serialization(Type = MemberType.Inline)]
        public AnimationCurve PositionCurve;
        [Serialization(Type = MemberType.Inline)]
        public AnimationCurve ScaleShearCurve;
        [Serialization(Kind = SerializationKind.None)]
        public Animation ParentAnimation;


        private void MergeAdjacentFrames(List<Keyframe> keyframes)
        {
            int i = 1;
            while (i < keyframes.Count)
            {
                Keyframe k0 = keyframes[i - 1],
                    k1 = keyframes[i];

                if (k1.time - k0.time < 0.004f)
                {
                    if (k1.hasTranslation && !k0.hasTranslation)
                    {
                        k0.hasTranslation = true;
                        k0.translation = k1.translation;
                    }

                    if (k1.hasRotation && !k0.hasRotation)
                    {
                        k0.hasRotation = true;
                        k0.rotation = k1.rotation;
                    }

                    if (k1.hasScaleShear && !k0.hasScaleShear)
                    {
                        k0.hasScaleShear = true;
                        k0.scaleShear = k1.scaleShear;
                    }

                    keyframes.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        private void InterpolateFrames(List<Keyframe> keyframes)
        {
            for (int i = 1; i < keyframes.Count; i++)
            {
                Keyframe k0 = keyframes[i - 1],
                    k1 = keyframes[i];

                if (k0.hasTranslation && !k1.hasTranslation)
                {
                    Keyframe k2 = null;
                    for (var j = i + 1; j < keyframes.Count; j++)
                    {
                        if (keyframes[j].hasTranslation)
                        {
                            k2 = keyframes[j];
                            break;
                        }
                    }

                    k1.hasTranslation = true;
                    if (k2 != null)
                    {
                        float alpha = (k1.time - k0.time) / (k2.time - k0.time);
                        k1.translation = Vector3.Lerp(k0.translation, k2.translation, alpha);
                    }
                    else
                    {
                        k1.translation = k0.translation;
                    }
                }

                if (k0.hasRotation && !k1.hasRotation)
                {
                    Keyframe k2 = null;
                    for (var j = i + 1; j < keyframes.Count; j++)
                    {
                        if (keyframes[j].hasRotation)
                        {
                            k2 = keyframes[j];
                            break;
                        }
                    }

                    k1.hasRotation = true;
                    if (k2 != null)
                    {
                        float alpha = (k1.time - k0.time) / (k2.time - k0.time);
                        k1.rotation = Quaternion.Slerp(k0.rotation, k2.rotation, alpha);
                    }
                    else
                    {
                        k1.rotation = k0.rotation;
                    }
                }

                if (k0.hasScaleShear && !k1.hasScaleShear)
                {
                    Keyframe k2 = null;
                    for (var j = i + 1; j < keyframes.Count; j++)
                    {
                        if (keyframes[j].hasScaleShear)
                        {
                            k2 = keyframes[j];
                            break;
                        }
                    }

                    k1.hasScaleShear = true;
                    if (k2 != null)
                    {
                        float alpha = (k1.time - k0.time) / (k2.time - k0.time);
                        k1.scaleShear[0, 0] = k0.scaleShear[0, 0] * (1.0f - alpha) + k2.scaleShear[0, 0] * alpha;
                        k1.scaleShear[1, 1] = k0.scaleShear[1, 1] * (1.0f - alpha) + k2.scaleShear[1, 1] * alpha;
                        k1.scaleShear[2, 2] = k0.scaleShear[2, 2] * (1.0f - alpha) + k2.scaleShear[2, 2] * alpha;
                    }
                    else
                    {
                        k1.scaleShear = k0.scaleShear;
                    }
                }
            }
        }


        public List<Keyframe> mergeKeyframes()
        {
            var keyframes = new SortedList<float, Keyframe>();
            OrientationCurve.CurveData.ExportKeyframes(keyframes, AnimationCurveData.ExportType.Rotation);
            PositionCurve.CurveData.ExportKeyframes(keyframes, AnimationCurveData.ExportType.Position);
            ScaleShearCurve.CurveData.ExportKeyframes(keyframes, AnimationCurveData.ExportType.ScaleShear);

            var outFrames = keyframes.Values.ToList();
            InterpolateFrames(outFrames);
            MergeAdjacentFrames(outFrames);
            return outFrames;
        }


        public List<animation> ExportTransform(IList<Keyframe> keyframes, string name, string target)
        {
            var anims = new List<animation>();
            var inputs = new List<InputLocal>();

            var outputs = new List<float>(keyframes.Count * 16);
            foreach (var keyframe in keyframes)
            {
                var transform = Matrix4.Identity;

                if (keyframe.hasTranslation)
                {
                    transform = Matrix4.CreateTranslation(keyframe.translation);
                }

                if (keyframe.hasRotation)
                    transform = Matrix4.CreateFromQuaternion(keyframe.rotation) * transform;

                if (keyframe.hasScaleShear)
                {
                    var scaleShear = Matrix4.Identity;
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                            scaleShear[j, i] = keyframe.scaleShear[i, j];
                    }

                    transform = scaleShear * transform;
                }

                transform.Transpose();
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                        outputs.Add(transform[i, j]);
                }
            }

            var interpolations = new List<string>(keyframes.Count);
            for (int i = 0; i < keyframes.Count; i++)
            {
                // TODO: Add control point estimation code and add in/out tangents for Bezier
                //interpolations.Add("BEZIER");
                interpolations.Add("LINEAR");
            }

            var knots = new List<float>(keyframes.Count);
            foreach (var keyframe in keyframes)
            {
                knots.Add(keyframe.time);
            }

            /*
             * Fix up animations that have only one keyframe by adding another keyframe at
             * the end of the animation.
             * (This mainly applies to DaIdentity and DnConstant32f)
             */
            if (keyframes.Count == 1)
            {
                knots.Add(ParentAnimation.Duration);
                for (int i = 0; i < 16; i++)
                    outputs.Add(outputs[i]);
                interpolations.Add(interpolations[0]);
            }

            var knotsSource = ColladaUtils.MakeFloatSource(name, "inputs", new string[] { "TIME" }, knots.ToArray());
            var knotsInput = new InputLocal();
            knotsInput.semantic = "INPUT";
            knotsInput.source = "#" + knotsSource.id;
            inputs.Add(knotsInput);

            var outSource = ColladaUtils.MakeFloatSource(name, "outputs", new string[] { "TRANSFORM" }, outputs.ToArray(), 16, "float4x4");
            var outInput = new InputLocal();
            outInput.semantic = "OUTPUT";
            outInput.source = "#" + outSource.id;
            inputs.Add(outInput);

            var interpSource = ColladaUtils.MakeNameSource(name, "interpolations", new string[] { "INTERPOLATION" }, interpolations.ToArray());

            var interpInput = new InputLocal();
            interpInput.semantic = "INTERPOLATION";
            interpInput.source = "#" + interpSource.id;
            inputs.Add(interpInput);

            var sampler = new sampler();
            sampler.id = name + "_sampler";
            sampler.input = inputs.ToArray();

            var channel = new channel();
            channel.source = "#" + sampler.id;
            channel.target = target;

            var animation = new animation();
            animation.id = name;
            animation.name = name;
            var animItems = new List<object>();
            animItems.Add(knotsSource);
            animItems.Add(outSource);
            animItems.Add(interpSource);
            animItems.Add(sampler);
            animItems.Add(channel);
            animation.Items = animItems.ToArray();
            anims.Add(animation);
            return anims;
        }

        public List<animation> ExportAnimations()
        {
            var anims = new List<animation>();
            var name = "Bone_" + Name.Replace(' ', '_');

            // Export all tracks in a single transform
            anims.AddRange(ExportTransform(mergeKeyframes(), name + "_Transform", name + "/Transform"));

            return anims;
        }
    }

    public class VectorTrack
    {
        public string Name;
        public UInt32 TrackKey;
        public Int32 Dimension;
        [Serialization(Type = MemberType.Inline)]
        public AnimationCurve ValueCurve;
    }

    public class TransformLODError
    {
        public Single Real32;
    }

    public class TextTrackEntry
    {
        public Single TimeStamp;
        public string Text;
    }

    public class TextTrack
    {
        public string Name;
        public List<TextTrackEntry> Entries;
    }

    public class PeriodicLoop
    {
        public Single Radius;
        public Single dAngle;
        public Single dZ;
        [Serialization(ArraySize = 3)]
        public Single[] BasisX;
        [Serialization(ArraySize = 3)]
        public Single[] BasisY;
        [Serialization(ArraySize = 3)]
        public Single[] Axis;
    }

    public class TrackGroup
    {
        public string Name;
        public List<VectorTrack> VectorTracks;
        public List<TransformTrack> TransformTracks;
        public List<TransformLODError> TransformLODErrors;
        public List<TextTrack> TextTracks;
        public Transform InitialPlacement;
        public int AccumulationFlags;
        [Serialization(ArraySize = 3)]
        public float[] LoopTranslation;
        public PeriodicLoop PeriodicLoop;
        [Serialization(Type = MemberType.VariantReference)]
        public object ExtendedData;

        public List<animation> ExportAnimations()
        {
            var anims = new List<animation>();
            foreach (var track in TransformTracks)
            {
                anims.AddRange(track.ExportAnimations());
            }

            return anims;
        }
    }

    public class Animation
    {
        public string Name;
        public float Duration;
        public float TimeStep;
        public float Oversampling;
        [Serialization(Type = MemberType.ArrayOfReferences)]
        public List<TrackGroup> TrackGroups;
        public Int32 DefaultLoopCount;
        public Int32 Flags;
        [Serialization(Type = MemberType.VariantReference)]
        public object ExtendedData;

        public List<animation> ExportAnimations()
        {
            var animations = new List<animation>();
            foreach (var trackGroup in TrackGroups)
            {
                /*
                 * We need to propagate animation data as the track exporter may need information from it
                 * (Duration and TimeStep usually)
                 */
                foreach (var track in trackGroup.TransformTracks)
                {
                    track.ParentAnimation = this;
                    track.OrientationCurve.CurveData.ParentAnimation = this;
                    track.PositionCurve.CurveData.ParentAnimation = this;
                    track.ScaleShearCurve.CurveData.ParentAnimation = this;
                }

                animations.AddRange(trackGroup.ExportAnimations());
            }

            return animations;
        }
    }
}
