using OpenTK.Mathematics;
using LSLib.Granny.Model.CurveData;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model;

public static class QuatHelpers
{
    public static Quaternion Product(Quaternion r, Quaternion q)
    {
        return new Quaternion(
            r.W * q.W - r.X * q.X - r.Y * q.Y - r.Z * q.Z,
            r.W * q.X + r.X * q.W - r.Y * q.Z + r.Z * q.Y,
            r.W * q.Y + r.X * q.Z + r.Y * q.W - r.Z * q.X,
            r.W * q.Z - r.X * q.Y + r.Y * q.X + r.Z * q.W
        );
    }

    public static float Dot(Quaternion r, Quaternion q)
    {
        return Vector3.Dot(r.Xyz, q.Xyz) + r.W * q.W;
    }
}

public class AnimationCurve
{
    [Serialization(Section = SectionType.Main, TypeSelector = typeof(AnimationCurveDataTypeSelector), Type = MemberType.VariantReference, MinVersion = 0x80000011)]
    public AnimationCurveData CurveData;
    [Serialization(MaxVersion = 0x80000010)]
    public Int32 Degree;
    [Serialization(Prototype = typeof(ControlReal32), Kind = SerializationKind.UserMember, Serializer = typeof(SingleListSerializer), MaxVersion = 0x80000010)]
    public List<Single> Knots;
    [Serialization(Prototype = typeof(ControlReal32), Kind = SerializationKind.UserMember, Serializer = typeof(SingleListSerializer), MaxVersion = 0x80000010)]
    public List<Single> Controls;

    /// <summary>
    /// Upgrades old animations (GR2 files with header version v6) to the new CurveData format
    /// </summary>
    public void UpgradeToGr7()
    {
        // Skip if we've already upgraded
        if (this.CurveData != null) return;

        if (this.Degree == 0)
        {
            // Degree 0 curves are identities in all cases
            CurveData = new DaIdentity
            {
                CurveDataHeader_DaIdentity = new CurveDataHeader
                {
                    Format = (byte)CurveFormat.DaIdentity,
                    Degree = 0
                }
            };
        }
        else if (this.Degree == 2)
        {
            if (this.Knots == null || this.Controls == null)
            {
                throw new InvalidOperationException("Could not upgrade animation curve: knots/controls unavailable");
            }

            // Degree 2 curves are stored in K32fC32f (v6 didn't support multiple curve formats)
            CurveData = new DaK32fC32f
            {
                CurveDataHeader_DaK32fC32f = new CurveDataHeader
                {
                    Format = (byte)CurveFormat.DaK32fC32f,
                    Degree = 2
                },
                Controls = Controls,
                Knots = Knots
            };
        }
        else
        {
            throw new InvalidOperationException("Could not upgrade animation curve: Unsupported curve degree");
        }
    }
}

public class Keyframe
{
    public float Time;
    public bool HasTranslation;
    public bool HasRotation;
    public bool HasScaleShear;
    public Vector3 Translation;
    public Quaternion Rotation;
    public Matrix3 ScaleShear;

    public Transform ToTransform()
    {
        var transform = new Transform();
        if (HasTranslation) transform.SetTranslation(Translation);
        if (HasRotation) transform.SetRotation(Rotation);
        if (HasScaleShear) transform.SetScaleShear(ScaleShear);
        return transform;
    }

    public void FromTransform(Transform transform)
    {
        Translation = transform.Translation;
        Rotation = transform.Rotation;
        ScaleShear = transform.ScaleShear;
    }
};

public class KeyframeTrack
{
    public SortedList<Single, Keyframe> Keyframes = [];

    private static Int32 FindFrame<T>(IList<T> list, T value, IComparer<T> comparer = null)
    {
        ArgumentNullException.ThrowIfNull(list);

        comparer ??= Comparer<T>.Default;

        Int32 lower = 0;
        Int32 upper = list.Count - 1;

        while (lower <= upper)
        {
            Int32 middle = lower + (upper - lower) / 2;
            Int32 comparisonResult = comparer.Compare(value, list[middle]);
            if (comparisonResult == 0)
                return middle;
            else if (comparisonResult < 0)
                upper = middle - 1;
            else
                lower = middle + 1;
        }

        return ~lower;
    }

    public Keyframe FindFrame(Single time, Single threshold = 0.01f)
    {
        Int32 lower = FindFrame(Keyframes.Keys, time);
        if (lower >= 0)
        {
            return Keyframes.Values[lower];
        }

        if (-lower <= Keyframes.Count)
        {
            float frameTime = Keyframes.Keys[-lower - 1];
            if (Math.Abs(frameTime - time) < threshold)
            {
                return Keyframes.Values[-lower - 1];
            }
        }

        return null;
    }

    public Keyframe RequireFrame(Single time, Single threshold = 0.01f)
    {
        Keyframe frame = FindFrame(time, threshold);
        if (frame == null)
        {
            frame = new Keyframe();
            frame.Time = time;
            Keyframes.Add(time, frame);
        }

        return frame;
    }

    public void AddTranslation(Single time, Vector3 translation)
    {
        Keyframe frame = RequireFrame(time);
        frame.Translation = translation;
        frame.HasTranslation = true;
    }

    public void AddRotation(Single time, Quaternion rotation)
    {
        Keyframe frame = RequireFrame(time);
        frame.Rotation = rotation;
        frame.HasRotation = true;
    }

    public void AddScaleShear(Single time, Matrix3 scaleShear)
    {
        Keyframe frame = RequireFrame(time);
        frame.ScaleShear = scaleShear;
        frame.HasScaleShear = true;
    }

    public void MergeAdjacentFrames()
    {
        int i = 1;
        while (i < Keyframes.Count)
        {
            Keyframe k0 = Keyframes.Values[i - 1],
                k1 = Keyframes.Values[i];

            if (k1.Time - k0.Time < 0.004f)
            {
                if (k1.HasTranslation && !k0.HasTranslation)
                {
                    k0.HasTranslation = true;
                    k0.Translation = k1.Translation;
                }

                if (k1.HasRotation && !k0.HasRotation)
                {
                    k0.HasRotation = true;
                    k0.Rotation = k1.Rotation;
                }

                if (k1.HasScaleShear && !k0.HasScaleShear)
                {
                    k0.HasScaleShear = true;
                    k0.ScaleShear = k1.ScaleShear;
                }

                Keyframes.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }
    }

    public void InterpolateFrames()
    {
        for (int i = 1; i < Keyframes.Count; i++)
        {
            Keyframe k0 = Keyframes.Values[i - 1],
                k1 = Keyframes.Values[i];

            if (k0.HasTranslation && !k1.HasTranslation)
            {
                Keyframe k2 = null;
                for (var j = i + 1; j < Keyframes.Count; j++)
                {
                    if (Keyframes.Values[j].HasTranslation)
                    {
                        k2 = Keyframes.Values[j];
                        break;
                    }
                }

                k1.HasTranslation = true;
                if (k2 != null)
                {
                    float alpha = (k1.Time - k0.Time) / (k2.Time - k0.Time);
                    k1.Translation = Vector3.Lerp(k0.Translation, k2.Translation, alpha);
                }
                else
                {
                    k1.Translation = k0.Translation;
                }
            }

            if (k0.HasRotation && !k1.HasRotation)
            {
                Keyframe k2 = null;
                for (var j = i + 1; j < Keyframes.Count; j++)
                {
                    if (Keyframes.Values[j].HasRotation)
                    {
                        k2 = Keyframes.Values[j];
                        break;
                    }
                }

                k1.HasRotation = true;
                if (k2 != null)
                {
                    float alpha = (k1.Time - k0.Time) / (k2.Time - k0.Time);
                    k1.Rotation = Quaternion.Slerp(k0.Rotation, k2.Rotation, alpha);
                }
                else
                {
                    k1.Rotation = k0.Rotation;
                }
            }

            if (k0.HasScaleShear && !k1.HasScaleShear)
            {
                Keyframe k2 = null;
                for (var j = i + 1; j < Keyframes.Count; j++)
                {
                    if (Keyframes.Values[j].HasScaleShear)
                    {
                        k2 = Keyframes.Values[j];
                        break;
                    }
                }

                k1.HasScaleShear = true;
                if (k2 != null)
                {
                    float alpha = (k1.Time - k0.Time) / (k2.Time - k0.Time);
                    k1.ScaleShear[0, 0] = k0.ScaleShear[0, 0] * (1.0f - alpha) + k2.ScaleShear[0, 0] * alpha;
                    k1.ScaleShear[1, 1] = k0.ScaleShear[1, 1] * (1.0f - alpha) + k2.ScaleShear[1, 1] * alpha;
                    k1.ScaleShear[2, 2] = k0.ScaleShear[2, 2] * (1.0f - alpha) + k2.ScaleShear[2, 2] * alpha;
                }
                else
                {
                    k1.ScaleShear = k0.ScaleShear;
                }
            }
        }
    }

    public void RemoveTrivialTranslations()
    {
        var times = Keyframes.Where(f => f.Value.HasTranslation).Select(f => f.Key).ToList();
        var transforms = Keyframes.Where(f => f.Value.HasTranslation).Select(f => f.Value.Translation).ToList();

        var i = 1;
        while (i < transforms.Count - 1)
        {
            Vector3 v0 = transforms[i - 1],
                v1 = transforms[i],
                v2 = transforms[i + 1];

            Single t0 = times[i - 1],
                t1 = times[i],
                t2 = times[i + 1];

            Single alpha = (t1 - t0) / (t2 - t0);
            Vector3 v1l = Vector3.Lerp(v0, v2, alpha);

            if ((v1 - v1l).Length < 0.001f)
            {
                Keyframes[times[i]].HasTranslation = false;
                Keyframes[times[i]].Translation = Vector3.Zero;
                times.RemoveAt(i);
                transforms.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }

        if (transforms.Count == 2 && (transforms[0] - transforms[1]).Length < 0.0001f)
        {
            Keyframes[times[1]].HasTranslation = false;
            Keyframes[times[1]].Translation = Vector3.Zero;
            times.RemoveAt(1);
            transforms.RemoveAt(1);
        }
    }

    public void RemoveTrivialRotations()
    {
        var times = Keyframes.Where(f => f.Value.HasRotation).Select(f => f.Key).ToList();
        var transforms = Keyframes.Where(f => f.Value.HasRotation).Select(f => f.Value.Rotation).ToList();

        var keyframesToRemove = 0;
        for (int i = 1; i < transforms.Count - 1; i++)
        {
            Quaternion v0 = transforms[i - 1],
                v1 = transforms[i],
                v2 = transforms[i + 1];

            Single t0 = times[i - 1],
                t1 = times[i],
                t2 = times[i + 1];

            Single alpha = (t1 - t0) / (t2 - t0);
            Quaternion v1l = Quaternion.Slerp(v0, v2, alpha);

            if ((v1 - v1l).Length < 0.001f)
            {
                keyframesToRemove++;
            }
        }

        if (keyframesToRemove == transforms.Count - 2 && (transforms[0] - transforms[^1]).Length < 0.0001f)
        {
            for (int i = 1; i < times.Count; i++)
            {
                Keyframes[times[i]].HasRotation = false;
                Keyframes[times[i]].Rotation = Quaternion.Identity;
            }

            times.RemoveRange(1, times.Count - 1);
            transforms.RemoveRange(1, transforms.Count - 1);
        }
    }

    public void RemoveTrivialScales()
    {
        var times = Keyframes.Where(f => f.Value.HasScaleShear).Select(f => f.Key).ToList();
        var transforms = Keyframes.Where(f => f.Value.HasScaleShear).Select(f => f.Value.ScaleShear).ToList();

        var i = 2;
        while (i < transforms.Count - 1)
        {
            Matrix3 t0 = transforms[i - 2],
                t1 = transforms[i - 1],
                t2 = transforms[i];

            float diff1 = 0.0f, diff2 = 0.0f;
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    diff1 += Math.Abs(t1[x, y] - t0[x, y]);
                    diff2 += Math.Abs(t2[x, y] - t1[x, y]);
                }
            }

            if (diff1 < 0.001f && diff2 < 0.001f)
            {
                Keyframes[times[i]].HasScaleShear = false;
                Keyframes[times[i]].ScaleShear = Matrix3.Identity;
                times.RemoveAt(i);
                transforms.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }

        if (transforms.Count == 3)
        {
            Matrix3 t0 = transforms[0],
                t1 = transforms[1],
                t2 = transforms[2];
            float diff = 0.0f;
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    diff += Math.Abs(t0[x, y] - t1[x, y]) + Math.Abs(t0[x, y] - t2[x, y]);
                }
            }

            if (diff < 0.001f)
            {
                Keyframes[times[2]].HasScaleShear = false;
                Keyframes[times[2]].ScaleShear = Matrix3.Identity;
                times.RemoveAt(2);
                transforms.RemoveAt(2);

                Keyframes[times[1]].HasScaleShear = false;
                Keyframes[times[1]].ScaleShear = Matrix3.Identity;
                times.RemoveAt(1);
                transforms.RemoveAt(1);
            }
        }
    }

    public void RemoveTrivialFrames()
    {
        var newFrames = new SortedList<Single, Keyframe>();
        foreach (var kv in Keyframes)
        {
            if (kv.Value.HasTranslation
                || kv.Value.HasRotation
                || kv.Value.HasScaleShear)
            {
                newFrames.Add(kv.Key, kv.Value);
            }
        }

        Keyframes = newFrames;
    }

    public void SwapBindPose(Matrix4 oldBindPose, Matrix4 newBindPose)
    {
        var oldToNewTransform = newBindPose * oldBindPose.Inverted();
        foreach (var keyframe in Keyframes)
        {
            var newTransform = oldToNewTransform * keyframe.Value.ToTransform().ToMatrix4();
            keyframe.Value.FromTransform(Transform.FromMatrix4(newTransform));
        }
    }

    public static KeyframeTrack FromMatrices(IList<Single> times, IEnumerable<Matrix4> transforms)
    {
        var track = new KeyframeTrack();

        var translations = transforms.Select(m => m.ExtractTranslation()).ToList();
        var rotations = transforms.Select(m => m.ExtractRotation()).ToList();
        var scales = transforms.Select(m => m.ExtractScale()).ToList();

        // Quaternion sign fixup
        // The same rotation can be represented by both q and -q. However the Slerp path
        // will be different; one will go the long away around, the other the short away around.
        // Replace quaterions to ensure that Slerp will take the short path.
        float flip = 1.0f;
        for (var i = 0; i < rotations.Count - 1; i++)
        {
            var r0 = rotations[i];
            var r1 = rotations[i + 1];
            var dot = QuatHelpers.Dot(r0, r1 * flip);
            
            if (dot < 0.0f)
            {
                flip = -flip;
            }

            rotations[i + 1] *= flip;
        }
        
        for (var i = 0; i < times.Count; i++)
        {
            track.AddTranslation(times[i], translations[i]);
            track.AddRotation(times[i], rotations[i]);
            var scaleShear = new Matrix3(
                scales[i][0], 0.0f, 0.0f,
                0.0f, scales[i][1], 0.0f,
                0.0f, 0.0f, scales[i][2]
            );
            track.AddScaleShear(times[i], scaleShear);
        }

        return track;
    }
}

public class TransformTrack
{
    public string Name;
    [Serialization(MinVersion = 0x80000011)]
    public int Flags;
    [Serialization(Type = MemberType.Inline)]
    public AnimationCurve OrientationCurve;
    [Serialization(Type = MemberType.Inline)]
    public AnimationCurve PositionCurve;
    [Serialization(Type = MemberType.Inline)]
    public AnimationCurve ScaleShearCurve;
    [Serialization(Kind = SerializationKind.None)]
    public Animation ParentAnimation;

    public static TransformTrack FromKeyframes(KeyframeTrack keyframes)
    {
        var track = new TransformTrack
        {
            Flags = 0
        };

        var translateTimes = keyframes.Keyframes.Where(f => f.Value.HasTranslation).Select(f => f.Key).ToList();
        var translations = keyframes.Keyframes.Where(f => f.Value.HasTranslation).Select(f => f.Value.Translation).ToList();
        if (translateTimes.Count == 1)
        {
            var posCurve = new D3Constant32f
            {
                CurveDataHeader_D3Constant32f = new CurveDataHeader { Format = (int)CurveFormat.D3Constant32f, Degree = 2 },
                Controls = new float[3] { translations[0].X, translations[0].Y, translations[0].Z }
            };
            track.PositionCurve = new AnimationCurve { CurveData = posCurve };
        }
        else
        {
            var posCurve = new DaK32fC32f();
            posCurve.CurveDataHeader_DaK32fC32f = new CurveDataHeader { Format = (int)CurveFormat.DaK32fC32f, Degree = 2 };
            posCurve.SetKnots(translateTimes);
            posCurve.SetPoints(translations);
            track.PositionCurve = new AnimationCurve { CurveData = posCurve };
        }

        var rotationTimes = keyframes.Keyframes.Where(f => f.Value.HasRotation).Select(f => f.Key).ToList();
        var rotations = keyframes.Keyframes.Where(f => f.Value.HasRotation).Select(f => f.Value.Rotation).ToList();
        if (rotationTimes.Count == 1)
        {
            var rotCurve = new D4Constant32f
            {
                CurveDataHeader_D4Constant32f = new CurveDataHeader { Format = (int)CurveFormat.D4Constant32f, Degree = 2 },
                Controls = new float[4] { rotations[0].X, rotations[0].Y, rotations[0].Z, rotations[0].W }
            };
            track.OrientationCurve = new AnimationCurve { CurveData = rotCurve };
        }
        else
        {
            var rotCurve = new DaK32fC32f();
            rotCurve.CurveDataHeader_DaK32fC32f = new CurveDataHeader { Format = (int)CurveFormat.DaK32fC32f, Degree = 2 };
            rotCurve.SetKnots(rotationTimes);
            rotCurve.SetQuaternions(rotations);
            track.OrientationCurve = new AnimationCurve { CurveData = rotCurve };
        }

        var scaleTimes = keyframes.Keyframes.Where(f => f.Value.HasScaleShear).Select(f => f.Key).ToList();
        var scales = keyframes.Keyframes.Where(f => f.Value.HasScaleShear).Select(f => f.Value.ScaleShear).ToList();
        if (scaleTimes.Count == 1)
        {
            var scaleCurve = new DaConstant32f();
            scaleCurve.CurveDataHeader_DaConstant32f = new CurveDataHeader { Format = (int)CurveFormat.DaConstant32f, Degree = 2 };
            var m = scales[0];
            scaleCurve.Controls =
            [
                m[0, 0], m[0, 1], m[0, 2],
                m[1, 0], m[1, 1], m[1, 2],
                m[2, 0], m[2, 1], m[2, 2]
            ];
            track.ScaleShearCurve = new AnimationCurve { CurveData = scaleCurve };
        }
        else
        {
            var scaleCurve = new DaK32fC32f();
            scaleCurve.CurveDataHeader_DaK32fC32f = new CurveDataHeader { Format = (int)CurveFormat.DaK32fC32f, Degree = 2 };
            scaleCurve.SetKnots(scaleTimes);
            scaleCurve.SetMatrices(scales);
            track.ScaleShearCurve = new AnimationCurve { CurveData = scaleCurve };
        }

        return track;
    }

    public KeyframeTrack ToKeyframes()
    {
        var track = new KeyframeTrack();

        OrientationCurve.CurveData.ExportKeyframes(track, AnimationCurveData.ExportType.Rotation);
        PositionCurve.CurveData.ExportKeyframes(track, AnimationCurveData.ExportType.Position);
        ScaleShearCurve.CurveData.ExportKeyframes(track, AnimationCurveData.ExportType.ScaleShear);
        
        return track;
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
    public BG3TrackGroupExtendedData ExtendedData;
}

public class Animation
{
    public string Name;
    public float Duration;
    public float TimeStep;
    [Serialization(MinVersion = 0x80000011)]
    public float Oversampling;
    [Serialization(Type = MemberType.ArrayOfReferences)]
    public List<TrackGroup> TrackGroups;
    [Serialization(MinVersion = 0x80000011)]
    public Int32 DefaultLoopCount;
    [Serialization(MinVersion = 0x80000011)]
    public Int32 Flags;
    [Serialization(Type = MemberType.VariantReference, MinVersion = 0x80000011)]
    public object ExtendedData;
}
