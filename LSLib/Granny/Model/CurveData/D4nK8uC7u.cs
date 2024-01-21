using OpenTK.Mathematics;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model.CurveData;

public class D4nK8uC7u : AnimationCurveData
{
    private static readonly float[] ScaleTable = [
        1.4142135f, 0.70710677f, 0.35355338f, 0.35355338f,
        0.35355338f, 0.17677669f, 0.17677669f, 0.17677669f,
        -1.4142135f, -0.70710677f, -0.35355338f, -0.35355338f,
        -0.35355338f, -0.17677669f, -0.17677669f, -0.17677669f
    ];

    private static readonly float[] OffsetTable = [
        -0.70710677f, -0.35355338f, -0.53033006f, -0.17677669f,
        0.17677669f, -0.17677669f, -0.088388346f, 0.0f,
        0.70710677f, 0.35355338f, 0.53033006f, 0.17677669f,
        -0.17677669f, 0.17677669f, 0.088388346f, -0.0f
    ];

    [Serialization(Type = MemberType.Inline)]
    public CurveDataHeader CurveDataHeader_D4nK8uC7u;
    public UInt16 ScaleOffsetTableEntries;
    public Single OneOverKnotScale;
    [Serialization(Prototype = typeof(ControlUInt8), Kind = SerializationKind.UserMember, Serializer = typeof(UInt8ListSerializer))]
    public List<Byte> KnotsControls;

    public override int NumKnots()
    {
        return KnotsControls.Count / 4;
    }

    public override List<float> GetKnots()
    {
        var numKnots = NumKnots();
        var knots = new List<float>(numKnots);
        for (var i = 0; i < numKnots; i++)
            knots.Add((float)KnotsControls[i] / OneOverKnotScale);

        return knots;
    }

    private Quaternion QuatFromControl(Byte a, Byte b, Byte c, float[] scales, float[] offsets)
    {
        // Control data format:
        //   ----- A ----- ----- B ----- ----- C -----
        // |  1  2 ... 7  |  1  2 ... 7 |  1  2 ... 7 |
        //    G     DA      S1     DB     S2     DC
        //
        // G: Sign flag; the last word is negative if G == 1
        // S1, S2: Swizzle value (S1 << 1) | S2, determines the order of X, Y, Z, W components.
        // DA, DB, DC: Data values for 3 of 4 components

        // The swizzle value for each component is calculated using an addition over 4,
        // using the formula: S(n+1) = (S(n) + 1) & 3
        var swizzle1 = ((b & 0x80) >> 6) | ((c & 0x80) >> 7);
        var swizzle2 = (swizzle1 + 1) & 3;
        var swizzle3 = (swizzle2 + 1) & 3;
        var swizzle4 = (swizzle3 + 1) & 3;

        var dataA = (a & 0x7f) * scales[swizzle2] + offsets[swizzle2];
        var dataB = (b & 0x7f) * scales[swizzle3] + offsets[swizzle3];
        var dataC = (c & 0x7f) * scales[swizzle4] + offsets[swizzle4];

        var dataD = (float)Math.Sqrt(1 - (dataA * dataA + dataB * dataB + dataC * dataC));
        if ((a & 0x80) != 0)
            dataD = -dataD;

        var f = new float[4];
        f[swizzle2] = dataA;
        f[swizzle3] = dataB;
        f[swizzle4] = dataC;
        f[swizzle1] = dataD;

        return new Quaternion(f[0], f[1], f[2], f[3]);
    }

    public override List<Quaternion> GetQuaternions()
    {
        // ScaleOffsetTableEntries is a bitmask containing the indexes of 4 scale table and offset table entries.
        // Format:
        // | 1 ... 4  5 ... 8 | 1 ... 4  5 ... 8 |
        //   Entry 4  Entry 3 | Entry 2  Entry 1
        var selector = ScaleOffsetTableEntries;
        var scaleTable = new float[] {
            ScaleTable[(selector >> 0) & 0x0F] * 0.0078740157f,
            ScaleTable[(selector >> 4) & 0x0F] * 0.0078740157f,
            ScaleTable[(selector >> 8) & 0x0F] * 0.0078740157f,
            ScaleTable[(selector >> 12) & 0x0F] * 0.0078740157f
        };

        var offsetTable = new float[] {
            OffsetTable[(selector >> 0) & 0x0F],
            OffsetTable[(selector >> 4) & 0x0F],
            OffsetTable[(selector >> 8) & 0x0F],
            OffsetTable[(selector >> 12) & 0x0F]
        };

        var numKnots = NumKnots();
        var quats = new List<Quaternion>(numKnots);
        for (var i = 0; i < numKnots; i++)
        {
            var quat = QuatFromControl(
                KnotsControls[numKnots + i * 3 + 0],
                KnotsControls[numKnots + i * 3 + 1],
                KnotsControls[numKnots + i * 3 + 2],
                scaleTable, offsetTable
            );
            quats.Add(quat);
        }

        return quats;
    }
}
