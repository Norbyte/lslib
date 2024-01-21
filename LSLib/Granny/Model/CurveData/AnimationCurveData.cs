using OpenTK.Mathematics;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model.CurveData;
public class CurveRegistry
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

        TypeToFormatMap = [];
        NameToTypeMap = [];

        Register(typeof(DaKeyframes32f), CurveFormat.DaKeyframes32f);
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

    public static Dictionary<String, Type> GetAllTypes()
    {
        Init();

        return NameToTypeMap;
    }

    public static Type Resolve(String name)
    {
        Init();

        if (!NameToTypeMap.TryGetValue(name, out Type type))
            throw new ParsingException("Unsupported curve type: " + name);

        return type;
    }
}

public enum CurveFormat
{
    // Types:
    // Da: (animation) 3x3 matrix
    // D[1-4]: 1 - 4 component vector
    // I[1/3]: 1/3 values for the main diagonal, others are zero
    // n: Normalized quaternion
    // Constant: Constant vector/matrix
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
    public Byte UInt8 = 0;
}

class ControlUInt16
{
    public UInt16 UInt16 = 0;
}

class ControlReal32
{
    public Single Real32 = 0;
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
        if (fieldName[..16] != "CurveDataHeader_")
            throw new ParsingException("Unrecognized curve data header type: " + fieldName);

        var curveType = fieldName[16..];
        return CurveRegistry.Resolve(curveType);
    }
}

[StructSerialization(MixedMarshal = true)]
public abstract class AnimationCurveData
{
    public enum ExportType
    {
        Position,
        Rotation,
        ScaleShear
    };

    [Serialization(Kind = SerializationKind.None)]
    public Animation ParentAnimation;

    protected float ConvertOneOverKnotScaleTrunc(UInt16 oneOverKnotScaleTrunc)
    {
        UInt32[] i = [(UInt32)oneOverKnotScaleTrunc << 16];
        float[] f = new float[1];
        Buffer.BlockCopy(i, 0, f, 0, i.Length * 4);
        return f[0];
    }

    public float Duration()
    {
        return GetKnots()[NumKnots() - 1];
    }

    public abstract int NumKnots();
    public abstract List<float> GetKnots();

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
        List<Quaternion> quats = new(matrices.Count);
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
        

    public void ExportKeyframes(KeyframeTrack track, ExportType type)
    {
        var numKnots = NumKnots();
        var knots = GetKnots();
        if (type == ExportType.Position)
        {
            var positions = GetPoints();
            for (var i = 0; i < numKnots; i++)
            {
                track.AddTranslation(knots[i], positions[i]);
            }
        }
        else if (type == ExportType.Rotation)
        {
            var quats = GetQuaternions();
            for (var i = 0; i < numKnots; i++)
            {
                track.AddRotation(knots[i], quats[i]);
            }
        }
        else if (type == ExportType.ScaleShear)
        {
            var mats = GetMatrices();
            for (var i = 0; i < numKnots; i++)
            {
                track.AddScaleShear(knots[i], mats[i]);
            }
        }
    }
}
