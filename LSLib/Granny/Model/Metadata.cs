using LSLib.Granny.GR2;
using LSLib.LS;

namespace LSLib.Granny.Model;

public class ArtToolInfo
{
    public String FromArtToolName;
    public Int32 ArtToolMajorRevision;
    public Int32 ArtToolMinorRevision;
    [Serialization(MinVersion = 0x80000011)]
    public Int32 ArtToolPointerSize;
    public Single UnitsPerMeter;
    [Serialization(ArraySize = 3)]
    public Single[] Origin;
    [Serialization(ArraySize = 3)]
    public Single[] RightVector;
    [Serialization(ArraySize = 3)]
    public Single[] UpVector;
    [Serialization(ArraySize = 3)]
    public Single[] BackVector;
    [Serialization(Type = MemberType.VariantReference, MinVersion = 0x80000011)]
    public object ExtendedData;

    public static ArtToolInfo CreateDefault()
    {
        return new ArtToolInfo
        {
            FromArtToolName = "",
            ArtToolMajorRevision = 1,
            ArtToolMinorRevision = 0,
            ArtToolPointerSize = 64,
            UnitsPerMeter = 1,
            Origin = [0, 0, 0]
        };
    }

    public void SetYUp()
    {
        RightVector = [1, 0, 0];
        UpVector = [0, 1, 0];
        BackVector = [0, 0, -1];
    }

    public void SetZUp()
    {
        RightVector = [1, 0, 0];
        UpVector = [0, 0, 1];
        BackVector = [0, 1, 0];
    }
}

public class ExporterInfo
{
    public String ExporterName;
    public Int32 ExporterMajorRevision;
    public Int32 ExporterMinorRevision;
    public Int32 ExporterCustomization;
    public Int32 ExporterBuildNumber;
    [Serialization(Type = MemberType.VariantReference, MinVersion = 0x80000011)]
    public object ExtendedData;

    public static ExporterInfo MakeCurrent()
    {
        return new ExporterInfo
        {
            ExporterName = $"LSLib GR2 Exporter v{Common.LibraryVersion()}",
            ExporterMajorRevision = Common.MajorVersion,
            ExporterMinorRevision = Common.MinorVersion,
            ExporterBuildNumber = 0,
            ExporterCustomization = Common.PatchVersion
        };
    }
}
