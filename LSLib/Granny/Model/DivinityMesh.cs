using LSLib.Granny.GR2;

namespace LSLib.Granny.Model;

[Flags]
public enum DivinityModelFlag
{
    MeshProxy = 0x01,
    Cloth = 0x02,
    HasProxyGeometry = 0x04,
    HasColor = 0x08,
    Skinned = 0x10,
    Rigid = 0x20,
    Spring = 0x40,
    Occluder = 0x80
};


[Flags]
public enum DivinityClothFlag
{
    // Unknown flags, possibly related to nudity filters
    Cloth01 = 0x01,
    Cloth02 = 0x02,
    Cloth04 = 0x04,
    ClothPhysics = 0x100
};

public static class DivinityModelFlagMethods
{
    public static bool IsMeshProxy(this DivinityModelFlag flag)
    {
        return (flag & DivinityModelFlag.MeshProxy) == DivinityModelFlag.MeshProxy;
    }

    public static bool IsCloth(this DivinityModelFlag flag)
    {
        return (flag & DivinityModelFlag.Cloth) == DivinityModelFlag.Cloth;
    }

    public static bool HasProxyGeometry(this DivinityModelFlag flag)
    {
        return (flag & DivinityModelFlag.HasProxyGeometry) == DivinityModelFlag.HasProxyGeometry;
    }

    public static bool IsRigid(this DivinityModelFlag flag)
    {
        return (flag & DivinityModelFlag.Rigid) == DivinityModelFlag.Rigid;
    }

    public static bool IsSpring(this DivinityModelFlag flag)
    {
        return (flag & DivinityModelFlag.Spring) == DivinityModelFlag.Spring;
    }

    public static bool IsOccluder(this DivinityModelFlag flag)
    {
        return (flag & DivinityModelFlag.Occluder) == DivinityModelFlag.Occluder;
    }

    public static bool HasClothFlag01(this DivinityClothFlag flag)
    {
        return (flag & DivinityClothFlag.Cloth01) == DivinityClothFlag.Cloth01;
    }

    public static bool HasClothFlag02(this DivinityClothFlag flag)
    {
        return (flag & DivinityClothFlag.Cloth02) == DivinityClothFlag.Cloth02;
    }

    public static bool HasClothFlag04(this DivinityClothFlag flag)
    {
        return (flag & DivinityClothFlag.Cloth04) == DivinityClothFlag.Cloth04;
    }

    public static bool HasClothPhysics(this DivinityClothFlag flag)
    {
        return (flag & DivinityClothFlag.ClothPhysics) == DivinityClothFlag.ClothPhysics;
    }
}

public enum DivinityVertexUsage
{
    None = 0,
    Position = 1,
    TexCoord = 2,
    QTangent = 3,
    Normal = 3, // The same value is reused for QTangents
    Tangent = 4,
    Binormal = 5,
    BoneWeights = 6,
    BoneIndices = 7,
    Color = 8
};

public enum DivinityVertexAttributeFormat
{
    Real32 = 0,
    UInt32 = 1,
    Int32 = 2,
    Real16 = 3,
    NormalUInt16 = 4,
    UInt16 = 5,
    BinormalInt16 = 6,
    Int16 = 7,
    NormalUInt8 = 8,
    UInt8 = 9,
    BinormalInt8 = 10,
    Int8 = 11
};

public class DivinityFormatDesc
{
    [Serialization(ArraySize = 1)]
    public SByte[] Stream;
    [Serialization(ArraySize = 1)]
    public Byte[] Usage;
    [Serialization(ArraySize = 1)]
    public Byte[] UsageIndex;
    [Serialization(ArraySize = 1)]
    public Byte[] RefType;
    [Serialization(ArraySize = 1)]
    public Byte[] Format;
    [Serialization(ArraySize = 1)]
    public Byte[] Size;

    private static DivinityFormatDesc Make(DivinityVertexUsage usage, DivinityVertexAttributeFormat format, Byte size, Byte usageIndex = 0)
    {
        return new DivinityFormatDesc
        {
            Stream = [0],
            Usage = [(byte)usage],
            UsageIndex = [usageIndex],
            RefType = [0],
            Format = [(byte)format],
            Size = [size]
        };
    }

    public static List<DivinityFormatDesc> FromVertexFormat(VertexDescriptor format)
    {
        var formats = new List<DivinityFormatDesc>();
        if (format.PositionType != PositionType.None)
        {
            formats.Add(Make(DivinityVertexUsage.Position, DivinityVertexAttributeFormat.Real32, 3));
        }

        if (format.HasBoneWeights)
        {
            formats.Add(Make(DivinityVertexUsage.BoneWeights, DivinityVertexAttributeFormat.NormalUInt8, (byte)format.NumBoneInfluences));
            formats.Add(Make(DivinityVertexUsage.BoneIndices, DivinityVertexAttributeFormat.UInt8, (byte)format.NumBoneInfluences));
        }

        if (format.NormalType != NormalType.None)
        {
            if (format.NormalType == NormalType.QTangent)
            {
                formats.Add(Make(DivinityVertexUsage.QTangent, DivinityVertexAttributeFormat.BinormalInt16, 4));
            }
            else if (format.NormalType == NormalType.Float3)
            {
                formats.Add(Make(DivinityVertexUsage.Normal, DivinityVertexAttributeFormat.Real32, 3));
                if (format.TangentType == NormalType.Float3)
                {
                    formats.Add(Make(DivinityVertexUsage.Tangent, DivinityVertexAttributeFormat.Real32, 3));
                }
                if (format.BinormalType == NormalType.Float3)
                {
                    formats.Add(Make(DivinityVertexUsage.Binormal, DivinityVertexAttributeFormat.Real32, 3));
                }
            }
            else
            {
                throw new InvalidOperationException($"Normal format not supported in LSM: {format.NormalType}");
            }
        }

        if (format.ColorMapType != ColorMapType.None)
        {
            if (format.ColorMapType == ColorMapType.Byte4)
            {
                for (int i = 0; i < format.ColorMaps; i++)
                {
                    formats.Add(Make(DivinityVertexUsage.Color, DivinityVertexAttributeFormat.NormalUInt8, 4, (byte)i));
                }
            }
            else if (format.ColorMapType == ColorMapType.Float4)
            {
                for (int i = 0; i < format.ColorMaps; i++)
                {
                    formats.Add(Make(DivinityVertexUsage.Color, DivinityVertexAttributeFormat.Real32, 4, (byte)i));
                }
            }
            else
            {
                throw new InvalidOperationException($"Color format not supported in LSM: {format.ColorMapType}");
            }
        }

        if (format.TextureCoordinateType != TextureCoordinateType.None)
        {
            if (format.TextureCoordinateType == TextureCoordinateType.Half2)
            {
                for (int i = 0; i < format.TextureCoordinates; i++)
                {
                    formats.Add(Make(DivinityVertexUsage.TexCoord, DivinityVertexAttributeFormat.Real16, 2, (byte)i));
                }
            }
            else if (format.TextureCoordinateType == TextureCoordinateType.Float2)
            {
                for (int i = 0; i < format.TextureCoordinates; i++)
                {
                    formats.Add(Make(DivinityVertexUsage.TexCoord, DivinityVertexAttributeFormat.Real32, 2, (byte)i));
                }
            }
            else
            {
                throw new InvalidOperationException($"UV format not supported in LSM: {format.TextureCoordinateType}");
            }
        }

        return formats;
    }
}

public class DivinityMeshProperties
{
    [Serialization(ArraySize = 4)]
    public UInt32[] Flags;
    [Serialization(ArraySize = 1)]
    public Int32[] Lod;
    public List<DivinityFormatDesc> FormatDescs;
    [Serialization(Type = MemberType.VariantReference)]
    public object ExtendedData;
    [Serialization(ArraySize = 1)]
    public float[] LodDistance;
    [Serialization(ArraySize = 1)]
    public Int32[] IsImpostor;
    [Serialization(Kind = SerializationKind.None)]
    public bool NewlyAdded = false;

    public DivinityModelFlag MeshFlags
    {
        get { return (DivinityModelFlag)Flags[0]; }
        set { Flags[0] = (UInt32)value; }
    }

    public DivinityClothFlag ClothFlags
    {
        get { return (DivinityClothFlag)Flags[2]; }
        set { Flags[2] = (UInt32)value; }
    }
}

public class DivinityMeshExtendedData
{
    const Int32 CurrentLSMVersion = 3;

    public Int32 MeshProxy;
    public Int32 Rigid;
    public Int32 Cloth;
    public Int32 Spring;
    public Int32 Occluder;
    public Int32 LOD;
    public string UserDefinedProperties;
    public DivinityMeshProperties UserMeshProperties;
    public Int32 LSMVersion;

    public static DivinityMeshExtendedData Make()
    {
        return new DivinityMeshExtendedData
        {
            Rigid = 0,
            Cloth = 0,
            Spring = 0,
            Occluder = 0,
            LOD = 0,
            UserDefinedProperties = "",
            UserMeshProperties = new DivinityMeshProperties
            {
                Flags = [0, 0, 0, 0],
                Lod = [-1],
                FormatDescs = null,
                ExtendedData = null,
                LodDistance = [3.40282347E+38f],
                IsImpostor = [0],
                NewlyAdded = true
            },
            LSMVersion = CurrentLSMVersion
        };
    }


    public void UpdateFromModelInfo(Mesh mesh, DivinityModelInfoFormat format)
    {
        DivinityModelFlag meshFlags = 0;
        if (UserMeshProperties != null)
        {
            meshFlags = UserMeshProperties.MeshFlags;
        }

        if (mesh.VertexFormat.HasBoneWeights)
        {
            meshFlags |= DivinityModelFlag.Skinned;
        }

        if (mesh.VertexFormat.ColorMaps > 0)
        {
            meshFlags |= DivinityModelFlag.HasColor;
        }
        else
        {
            meshFlags &= ~DivinityModelFlag.Cloth;
        }

        if (format == DivinityModelInfoFormat.UserDefinedProperties)
        {
            LSMVersion = 0;
            UserMeshProperties = null;
            UserDefinedProperties =
               UserDefinedPropertiesHelpers.MeshFlagsToUserDefinedProperties(meshFlags);
        }
        else
        {
            UserMeshProperties.MeshFlags = meshFlags;

            if (format == DivinityModelInfoFormat.LSMv3)
            {
                LSMVersion = 3;
                UserMeshProperties.FormatDescs = DivinityFormatDesc.FromVertexFormat(mesh.VertexFormat);
            }
            else if (format == DivinityModelInfoFormat.LSMv1)
            {
                LSMVersion = 1;
                UserMeshProperties.FormatDescs = DivinityFormatDesc.FromVertexFormat(mesh.VertexFormat);
            }
            else
            {
                LSMVersion = 0;
                UserMeshProperties.FormatDescs = [];
            }
        }
    }
}

public class BG3TrackGroupExtendedData
{
    public string SkeletonResourceID;
}

public static class UserDefinedPropertiesHelpers
{
    // The GR2 loader checks for this exact string, including spaces.
    public const string UserDefinedProperties_Rigid = "Rigid = true";
    // The GR2 loader checks for this exact string.
    public const string UserDefinedProperties_Cloth = "Cloth=true";
    public const string UserDefinedProperties_MeshProxy = "MeshProxy=true";

    public static string MeshFlagsToUserDefinedProperties(DivinityModelFlag meshFlags)
    {
        List<string> properties = new();
        if (meshFlags.IsRigid())
        {
            properties.Add(UserDefinedProperties_Rigid);
        }

        if (meshFlags.IsCloth())
        {
            properties.Add(UserDefinedProperties_Cloth);
        }

        if (meshFlags.IsMeshProxy())
        {
            properties.Add(UserDefinedProperties_MeshProxy);
        }

        return String.Join("\n", properties);
    }

    public static DivinityModelFlag UserDefinedPropertiesToMeshType(string userDefinedProperties)
    {
        // The D:OS 2 editor uses the ExtendedData attribute to determine whether a model can be 
        // bound to a character.
        // The "Rigid = true" user defined property is checked for rigid bodies (e.g. weapons), the "Cloth=true"
        // user defined property is checked for clothes.
        DivinityModelFlag flags = 0;
        if (userDefinedProperties.Contains("Rigid"))
        {
            flags |= DivinityModelFlag.Rigid;
        }

        if (userDefinedProperties.Contains("Cloth"))
        {
            flags |= DivinityModelFlag.Cloth;
        }

        if (userDefinedProperties.Contains("MeshProxy"))
        {
            flags |= DivinityModelFlag.MeshProxy | DivinityModelFlag.HasProxyGeometry;
        }

        return flags;
    }
}
