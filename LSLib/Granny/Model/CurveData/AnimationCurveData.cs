using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using Collada141;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model.CurveData
{
    class CurveRegistry
    {
        private static Dictionary<Type, CurveFormat> TypeToFormatMap;
        private static Dictionary<String, Type> NameToTypeMap;

        private static void Register(Type type, CurveFormat format)
        {
            TypeToFormatMap.Add(type, format);
            NameToTypeMap.Add(type.Name, type);
        }

        private static void Init()
        {
            if (TypeToFormatMap != null)
            {
                return;
            }

            TypeToFormatMap = new Dictionary<Type, CurveFormat>();
            NameToTypeMap = new Dictionary<String, Type>();

            // Register(typeof(DaKeyframes32f), CurveFormat.DaKeyframes32f);
            Register(typeof(DaK32fC32f), CurveFormat.DaK32fC32f);
            Register(typeof(DaIdentity), CurveFormat.DaIdentity);
            Register(typeof(DaConstant32f), CurveFormat.DaConstant32f);
            Register(typeof(D3Constant32f), CurveFormat.D3Constant32f);
            Register(typeof(D4Constant32f), CurveFormat.D4Constant32f);
            Register(typeof(DaK16uC16u), CurveFormat.DaK16uC16u);
            Register(typeof(DaK8uC8u), CurveFormat.DaK8uC8u);
            Register(typeof(D4nK16uC15u), CurveFormat.D4nK16uC15u);
            Register(typeof(D4nK8uC7u), CurveFormat.D4nK8uC7u);
            Register(typeof(D3K16uC16u), CurveFormat.D3K16uC16u);
            Register(typeof(D3K8uC8u), CurveFormat.D3K8uC8u);
            Register(typeof(D9I1K16uC16u), CurveFormat.D9I1K16uC16u);
            Register(typeof(D9I3K16uC16u), CurveFormat.D9I3K16uC16u);
            Register(typeof(D9I1K8uC8u), CurveFormat.D9I1K8uC8u);
            Register(typeof(D9I3K8uC8u), CurveFormat.D9I3K8uC8u);
            Register(typeof(D3I1K32fC32f), CurveFormat.D3I1K32fC32f);
            Register(typeof(D3I1K16uC16u), CurveFormat.D3I1K16uC16u);
            Register(typeof(D3I1K8uC8u), CurveFormat.D3I1K8uC8u);
        }

        public static Type Resolve(String name)
        {
            Init();

            Type type = null;
            if (!NameToTypeMap.TryGetValue(name, out type))
                throw new ParsingException("Unsupported curve type: " + name);

            return type;
        }
    }

    public enum CurveFormat
    {
        // Types:
        // Da: (animation?) 3x3 matrix
        // D[1-4]: 1 - 4 component vector
        // I[1-3]: ???
        // n: ???
        // Constant: Constant vector/matrix
        // 32f: Float, 32-bit
        // K[n][nothing/u/f]: n-bit value for knots; u = unsigned; f = floating point
        // C[n][nothing/u/f]: n-bit value for controls; u = unsigned; f = floating point
        DaKeyframes32f = 0,
        DaK32fC32f = 1,
        DaIdentity = 2,
        DaConstant32f = 3,
        D3Constant32f = 4,
        D4Constant32f = 5,
        DaK16uC16u = 6,
        DaK8uC8u = 7,
        D4nK16uC15u = 8,
        D4nK8uC7u = 9,
        D3K16uC16u = 10,
        D3K8uC8u = 11,
        D9I1K16uC16u = 12,
        D9I3K16uC16u = 13,
        D9I1K8uC8u = 14,
        D9I3K8uC8u = 15,
        D3I1K32fC32f = 16,
        D3I1K16uC16u = 17,
        D3I1K8uC8u = 18
    }

    public class CurveDataHeader
    {
        public byte Format;
        public byte Degree;

        public bool IsFloat()
        {
            switch ((CurveFormat)Format)
            {
                case CurveFormat.DaKeyframes32f:
                case CurveFormat.DaK32fC32f:
                case CurveFormat.DaIdentity:
                case CurveFormat.DaConstant32f:
                case CurveFormat.D3Constant32f:
                case CurveFormat.D4Constant32f:
                    return true;

                default:
                    return false;
            }
        }

        public int BytesPerKnot()
        {
            switch ((CurveFormat)Format)
            {
                case CurveFormat.DaKeyframes32f:
                case CurveFormat.DaK32fC32f:
                case CurveFormat.D3I1K32fC32f:
                    return 4;

                case CurveFormat.DaIdentity:
                case CurveFormat.DaConstant32f:
                case CurveFormat.D3Constant32f:
                case CurveFormat.D4Constant32f:
                    throw new ParsingException("Should not serialize knots/controls here");

                case CurveFormat.DaK16uC16u:
                case CurveFormat.D4nK16uC15u:
                case CurveFormat.D3K16uC16u:
                case CurveFormat.D9I1K16uC16u:
                case CurveFormat.D9I3K16uC16u:
                case CurveFormat.D3I1K16uC16u:
                    return 2;

                case CurveFormat.DaK8uC8u:
                case CurveFormat.D4nK8uC7u:
                case CurveFormat.D3K8uC8u:
                case CurveFormat.D9I1K8uC8u:
                case CurveFormat.D9I3K8uC8u:
                case CurveFormat.D3I1K8uC8u:
                    return 1;

                default:
                    throw new ParsingException("Unsupported curve data format");
            }
        }
    }

    class ControlUInt8
    {
        public Byte UInt8;
    }

    class ControlUInt16
    {
        public UInt16 UInt16;
    }

    class ControlReal32
    {
        public Single Real32;
    }

    class AnimationCurveDataTypeSelector : VariantTypeSelector
    {
        public Type SelectType(MemberDefinition member, object node)
        {
            return null;
        }

        public Type SelectType(MemberDefinition member, StructDefinition defn, object parent)
        {
            var fieldName = defn.Members[0].Name;
            if (fieldName.Substring(0, 16) != "CurveDataHeader_")
                throw new ParsingException("Unrecognized curve data header type: " + fieldName);

            var curveType = fieldName.Substring(16);
            return CurveRegistry.Resolve(curveType);
        }
    }

    abstract public class AnimationCurveData
    {
        public enum ExportType
        {
            Position,
            Rotation,
            ScaleShear
        };

        [Serialization(Kind = SerializationKind.None)]
        public Animation ParentAnimation;

        protected float ConvertOneOverKnotScaleTrunc(float oneOverKnotScaleTrunc)
        {
            UInt32[] i = new UInt32[] { (UInt32)oneOverKnotScaleTrunc << 16 };
            float[] f = new float[1];
            Buffer.BlockCopy(i, 0, f, 0, i.Length * 4);
            return f[0];
        }

        public float Duration()
        {
            return GetKnots()[NumKnots() - 1];
        }

        abstract public int NumKnots();
        abstract public List<float> GetKnots();

        public virtual List<Vector3> GetPoints()
        {
            throw new ParsingException("Curve does not contain position data");
        }

        public virtual List<Matrix3> GetMatrices()
        {
            throw new ParsingException("Curve does not contain rotation data");
        }

        public virtual List<Quaternion> GetQuaternions()
        {
            var matrices = GetMatrices();
            List<Quaternion> quats = new List<Quaternion>(matrices.Count);
            foreach (var matrix in matrices)
            {
                // Check that the matrix is orthogonal
                for (var i = 0; i < 3; i++)
                {
                    for (var j = 0; j < i; j++)
                    {
                        if (matrix[i, j] != matrix[j, i])
                            throw new ParsingException("Cannot convert into quaternion: Transformation matrix is not orthogonal!");
                    }
                }

                // Check that the matrix is special orthogonal
                // det(matrix) = 1
                if (Math.Abs(matrix.Determinant - 1) > 0.001)
                    throw new ParsingException("Cannot convert into quaternion: Transformation matrix is not special orthogonal!");

                quats.Add(matrix.ExtractRotation());
            }

            return quats;
        }

        public List<float> ExportChannelControlData(int coordinate, bool isRotation)
        {
            var outputs = new List<float>();
            if (isRotation)
            {
                var quats = GetQuaternions();
                foreach (var rotation in quats)
                {
                    var axisAngle = rotation.ToAxisAngle();
                    switch (coordinate)
                    {
                        case 0:
                            outputs.Add(axisAngle.X * axisAngle.W * 180.0f / (float)Math.PI);
                            break;

                        case 1:
                            outputs.Add(axisAngle.Y * axisAngle.W * 180.0f / (float)Math.PI);
                            break;

                        case 2:
                            outputs.Add(axisAngle.Z * axisAngle.W * 180.0f / (float)Math.PI);
                            break;

                        case 3:
                            throw new ArgumentException("TODO?");
                            // Not sure if we'll ever need this ...
                            // outputs.Add(axisAngle.W);
                            // break;

                        default:
                            throw new ArgumentException("Invalid rotation coordinate index");
                    }
                }
            }
            else
            {
                foreach (var position in GetPoints())
                {
                    switch (coordinate)
                    {
                        case 0:
                            outputs.Add(position.X);
                            break;

                        case 1:
                            outputs.Add(position.Y);
                            break;

                        case 2:
                            outputs.Add(position.Z);
                            break;

                        default:
                            throw new ArgumentException("Invalid position coordinate index");
                    }
                }
            }

            return outputs;
        }

        public animation ExportChannel(string name, string target, string paramName, int coordinate, bool isRotation)
        {
            var inputs = new List<InputLocal>();
            var numKnots = NumKnots();
            var knots = GetKnots();
            var outputs = ExportChannelControlData(coordinate, isRotation);

            var interpolations = new List<string>(numKnots);
            for (int i = 0; i < numKnots; i++)
            {
                // TODO: Add control point estimation code and add in/out tangents for Bezier
                //interpolations.Add("BEZIER");
                interpolations.Add("LINEAR");
            }

            /*
             * Fix up animations that have only one keyframe by adding another keyframe at
             * the end of the animation.
             * (This mainly applies to DaIdentity and DnConstant32f)
             */
            if (numKnots == 1)
            {
                knots.Add(ParentAnimation.Duration);
                outputs.Add(outputs[0]);
                interpolations.Add(interpolations[0]);
            }

            var knotsSource = ColladaUtils.MakeFloatSource(name, "inputs", new string[] { "TIME" }, knots.ToArray());
            var knotsInput = new InputLocal();
            knotsInput.semantic = "INPUT";
            knotsInput.source = "#" + knotsSource.id;
            inputs.Add(knotsInput);

            var outSource = ColladaUtils.MakeFloatSource(name, "outputs", new string[] { paramName }, outputs.ToArray());
            var outInput = new InputLocal();
            outInput.semantic = "OUTPUT";
            outInput.source = "#" + outSource.id;
            inputs.Add(outInput);

            var interpSource = ColladaUtils.MakeNameSource(name, "interpolations", new string[] { "" }, interpolations.ToArray());

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
            return animation;
        }

        public List<animation> ExportPositions(string name, string target)
        {
            var anims = new List<animation>();
            if (NumKnots() > 0)
            {
                anims.Add(ExportChannel(name + "_X", target + ".X", "X", 0, false));
                anims.Add(ExportChannel(name + "_Y", target + ".Y", "Y", 1, false));
                anims.Add(ExportChannel(name + "_Z", target + ".Z", "Z", 2, false));
            }
            return anims;
        }

        public List<animation> ExportRotations(string name, string target)
        {
            var anims = new List<animation>();
            if (NumKnots() > 0)
            {
                anims.Add(ExportChannel(name + "_X", target + "X.ANGLE", "ANGLE", 0, true));
                anims.Add(ExportChannel(name + "_Y", target + "Y.ANGLE", "ANGLE", 1, true));
                anims.Add(ExportChannel(name + "_Z", target + "Z.ANGLE", "ANGLE", 2, true));
            }
            return anims;
        }

        public List<animation> ExportRotation(string name, string target)
        {
            var anims = new List<animation>();
            if (NumKnots() > 0)
            {
                var inputs = new List<InputLocal>();
                var numKnots = NumKnots();
                var knots = GetKnots();

                var outputs = new List<float>(knots.Count * 4);
                var quats = GetQuaternions();
                foreach (var rotation in quats)
                {
                    var axisAngle = rotation.ToAxisAngle();
                    outputs.Add(axisAngle.X);
                    outputs.Add(axisAngle.Y);
                    outputs.Add(axisAngle.Z);
                    outputs.Add(axisAngle.W);
                }

                var interpolations = new List<string>(numKnots);
                for (int i = 0; i < numKnots; i++)
                {
                    // TODO: Add control point estimation code and add in/out tangents for Bezier
                    //interpolations.Add("BEZIER");
                    interpolations.Add("LINEAR");
                }

                /*
                 * Fix up animations that have only one keyframe by adding another keyframe at
                 * the end of the animation.
                 * (This mainly applies to DaIdentity and DnConstant32f)
                 */
                if (numKnots == 1)
                {
                    knots.Add(ParentAnimation.Duration);
                    outputs.Add(outputs[0]);
                    outputs.Add(outputs[1]);
                    outputs.Add(outputs[2]);
                    outputs.Add(outputs[3]);
                    interpolations.Add(interpolations[0]);
                }

                var knotsSource = ColladaUtils.MakeFloatSource(name, "inputs", new string[] { "TIME" }, knots.ToArray());
                var knotsInput = new InputLocal();
                knotsInput.semantic = "INPUT";
                knotsInput.source = "#" + knotsSource.id;
                inputs.Add(knotsInput);

                var outSource = ColladaUtils.MakeFloatSource(name, "outputs", new string[] { "X", "Y", "Z", "ANGLE" }, outputs.ToArray());
                var outInput = new InputLocal();
                outInput.semantic = "OUTPUT";
                outInput.source = "#" + outSource.id;
                inputs.Add(outInput);

                var interpSource = ColladaUtils.MakeNameSource(name, "interpolations", new string[] { "" }, interpolations.ToArray());

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
            }
            return anims;
        }


        public void ExportKeyframes(SortedList<float, Keyframe> keyframes, ExportType type)
        {
            var numKnots = NumKnots();
            var knots = GetKnots();
            if (type == ExportType.Position)
            {
                var positions = GetPoints();
                for (var i = 0; i < numKnots; i++)
                {
                    Keyframe frame;
                    if (!keyframes.TryGetValue(knots[i], out frame))
                    {
                        frame = new Keyframe();
                        frame.time = knots[i];
                        keyframes[frame.time] = frame;
                    }

                    frame.translation = positions[i];
                    frame.hasTranslation = true;
                }
            }
            else if (type == ExportType.Rotation)
            {
                var quats = GetQuaternions();
                for (var i = 0; i < numKnots; i++)
                {
                    Keyframe frame;
                    if (!keyframes.TryGetValue(knots[i], out frame))
                    {
                        frame = new Keyframe();
                        frame.time = knots[i];
                        keyframes[frame.time] = frame;
                    }

                    frame.rotation = quats[i];
                    frame.hasRotation = true;
                }
            }
            else if (type == ExportType.ScaleShear)
            {
                var mats = GetMatrices();
                for (var i = 0; i < numKnots; i++)
                {
                    Keyframe frame;
                    if (!keyframes.TryGetValue(knots[i], out frame))
                    {
                        frame = new Keyframe();
                        frame.time = knots[i];
                        keyframes[frame.time] = frame;
                    }

                    frame.scaleShear = mats[i];
                    frame.hasScaleShear = true;
                }
            }
        }


        public List<animation> ExportTransform(string name, string target)
        {
            var anims = new List<animation>();
            if (NumKnots() > 0)
            {
                var inputs = new List<InputLocal>();
                var numKnots = NumKnots();
                var knots = GetKnots();

                var outputs = new List<float>(knots.Count * 16);
                var quats = GetQuaternions();
                foreach (var rotation in quats)
                {
                    var transform = Matrix4.CreateFromQuaternion(rotation);
                    for (int i = 0; i < 4; i++)
                        for (int j = 0; j < 4; j++)
                        outputs.Add(transform[i, j]);
                }

                var interpolations = new List<string>(numKnots);
                for (int i = 0; i < numKnots; i++)
                {
                    // TODO: Add control point estimation code and add in/out tangents for Bezier
                    //interpolations.Add("BEZIER");
                    interpolations.Add("LINEAR");
                }

                /*
                 * Fix up animations that have only one keyframe by adding another keyframe at
                 * the end of the animation.
                 * (This mainly applies to DaIdentity and DnConstant32f)
                 */
                if (numKnots == 1)
                {
                    knots.Add(ParentAnimation.Duration);
                    for (int i = 0; i < 16; i++ )
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

                var interpSource = ColladaUtils.MakeNameSource(name, "interpolations", new string[] { "" }, interpolations.ToArray());

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
            }
            return anims;
        }
    }
}
