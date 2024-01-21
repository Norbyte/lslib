using OpenTK.Mathematics;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model.CurveData;

public class D4Constant32f : AnimationCurveData
{
    [Serialization(Type = MemberType.Inline)]
    public CurveDataHeader CurveDataHeader_D4Constant32f;
    public Int16 Padding;
    [Serialization(ArraySize = 4)]
    public float[] Controls;

    public override int NumKnots()
    {
        return 1;
    }

    public override List<float> GetKnots()
    {
        return [0.0f];
    }

    // TODO: GetMatrices
    public override List<Quaternion> GetQuaternions()
    {
        return [new Quaternion(Controls[0], Controls[1], Controls[2], Controls[3])];
    }
}
