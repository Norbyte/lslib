using LSLib.Granny.GR2;
using OpenTK.Mathematics;

namespace LSLib.Granny.Model;

public struct BoneWeight : IEquatable<BoneWeight>
{
    public byte A, B, C, D;

    /// <summary>
    /// Gets or sets the value at the index of the weight vector.
    /// </summary>
    public byte this[int index]
    {
        get
        {
            if (index == 0) return A;
            else if (index == 1) return B;
            else if (index == 2) return C;
            else if (index == 3) return D;
            throw new IndexOutOfRangeException("Illegal bone influence index: " + index);
        }
        set
        {
            if (index == 0) A = value;
            else if (index == 1) B = value;
            else if (index == 2) C = value;
            else if (index == 3) D = value;
            else throw new IndexOutOfRangeException("Illegal bone influence index: " + index);
        }
    }

    public bool Equals(BoneWeight w)
    {
        return A == w.A
            && B == w.B
            && C == w.C
            && D == w.D;
    }

    public override int GetHashCode()
    {
        return (int)A ^ (int)(B << 8) ^ (int)(C << 16) ^ (int)(D << 24);
    }
}

public enum PositionType
{
    None,
    Float3,
    Word4
};

public enum NormalType
{
    None,
    Float3,
    Half4,
    Byte4,
    QTangent
};

public enum ColorMapType
{
    None,
    Float4,
    Byte4
};

public enum TextureCoordinateType
{
    None,
    Float2,
    Half2
};

/// <summary>
/// Describes the properties (Position, Normal, Tangent, ...) of the vertex format
/// </summary>
public class VertexDescriptor
{
    public bool HasBoneWeights = false;
    public int NumBoneInfluences = Vertex.MaxBoneInfluences;
    public PositionType PositionType = PositionType.None;
    public NormalType NormalType = NormalType.None;
    public NormalType TangentType = NormalType.None;
    public NormalType BinormalType = NormalType.None;
    public ColorMapType ColorMapType = ColorMapType.None;
    public int ColorMaps = 0;
    public TextureCoordinateType TextureCoordinateType = TextureCoordinateType.None;
    public int TextureCoordinates = 0;
    private Type VertexType;

    public List<String> ComponentNames()
    {
        var names = new List<String>();
        if (PositionType != PositionType.None)
        {
            names.Add("Position");
        }

        if (HasBoneWeights)
        {
            names.Add("BoneWeights");
            names.Add("BoneIndices");
        }

        if (NormalType != NormalType.None)
        {
            if (NormalType == NormalType.QTangent)
            {
                names.Add("QTangent");
            }
            else
            {
                names.Add("Normal");
            }
        }

        if (TangentType != NormalType.None
            && TangentType != NormalType.QTangent)
        {
            names.Add("Tangent");
        }

        if (BinormalType != NormalType.None
            && BinormalType != NormalType.QTangent)
        {
            names.Add("Binormal");
        }

        if (ColorMapType != ColorMapType.None)
        {
            for (int i = 0; i < ColorMaps; i++)
            {
                names.Add("DiffuseColor_" + i.ToString());
            }
        }

        if (TextureCoordinateType != TextureCoordinateType.None)
        {
            for (int i = 0; i < TextureCoordinates; i++)
            {
                names.Add("TextureCoordinate_" + i.ToString());
            }
        }

        return names;
    }

    public String Name()
    {
        string vertexFormat;
        vertexFormat = "";
        string attributeCounts = "";

        switch (PositionType)
        {
            case PositionType.None:
                break;

            case PositionType.Float3:
                vertexFormat += "P";
                attributeCounts += "3";
                break;

            case PositionType.Word4:
                vertexFormat += "PW";
                attributeCounts += "4";
                break;
        }

        if (HasBoneWeights)
        {
            vertexFormat += "W";
            attributeCounts += NumBoneInfluences.ToString();
        }
        
        switch (NormalType)
        {
            case NormalType.None:
                break;

            case NormalType.Float3:
                vertexFormat += "N";
                attributeCounts += "3";
                break;

            case NormalType.Half4:
                vertexFormat += "HN";
                attributeCounts += "4";
                break;

            case NormalType.QTangent:
                vertexFormat += "QN";
                attributeCounts += "4";
                break;
        }

        switch (TangentType)
        {
            case NormalType.None:
                break;

            case NormalType.Float3:
                vertexFormat += "G";
                attributeCounts += "3";
                break;

            case NormalType.Half4:
                vertexFormat += "HG";
                attributeCounts += "4";
                break;
        }

        switch (BinormalType)
        {
            case NormalType.None:
                break;

            case NormalType.Float3:
                vertexFormat += "B";
                attributeCounts += "3";
                break;

            case NormalType.Half4:
                vertexFormat += "HB";
                attributeCounts += "4";
                break;
        }

        for (var i = 0; i < ColorMaps; i++)
        {
            switch (ColorMapType)
            {
                case ColorMapType.None:
                    break;

                case ColorMapType.Float4:
                    vertexFormat += "D";
                    attributeCounts += "4";
                    break;

                case ColorMapType.Byte4:
                    vertexFormat += "CD";
                    attributeCounts += "4";
                    break;
            }
        }

        for (var i = 0; i < TextureCoordinates; i++)
        {
            switch (TextureCoordinateType)
            {
                case TextureCoordinateType.None:
                    break;

                case TextureCoordinateType.Float2:
                    vertexFormat += "T";
                    attributeCounts += "2";
                    break;

                case TextureCoordinateType.Half2:
                    vertexFormat += "HT";
                    attributeCounts += "2";
                    break;
            }
        }

        return vertexFormat + attributeCounts;
    }

    public Vertex CreateInstance()
    {
        if (VertexType == null)
        {
            var typeName = "Vertex_" + Name();
            VertexType = VertexTypeBuilder.CreateVertexSubtype(typeName);
        }

        var vert = Activator.CreateInstance(VertexType) as Vertex;
        vert.Format = this;
        return vert;
    }
}

[StructSerialization(TypeSelector = typeof(VertexDefinitionSelector), MixedMarshal = true)]
public class Vertex
{
    public const int MaxBoneInfluences = 4;
    public const int MaxUVs = 4;
    public const int MaxColors = 2;

    public VertexDescriptor Format;
    public Vector3 Position;
    public BoneWeight BoneWeights;
    public BoneWeight BoneIndices;
    public Vector3 Normal;
    public Vector3 Tangent;
    public Vector3 Binormal;
    public Vector4 Color0;
    public Vector4 Color1;
    public Vector2 TextureCoordinates0;
    public Vector2 TextureCoordinates1;
    public Vector2 TextureCoordinates2;
    public Vector2 TextureCoordinates3;
    public Vector2 TextureCoordinates4;
    public Vector2 TextureCoordinates5;

    protected Vertex() { }

    public Vector2 GetUV(int index)
    {
        return index switch
        {
            0 => TextureCoordinates0,
            1 => TextureCoordinates1,
            2 => TextureCoordinates2,
            3 => TextureCoordinates3,
            4 => TextureCoordinates4,
            5 => TextureCoordinates5,
            _ => throw new ArgumentException($"At most {MaxUVs} UVs are supported."),
        };
    }

    public void SetUV(int index, Vector2 uv)
    {
        switch (index)
        {
            case 0: TextureCoordinates0 = uv; break;
            case 1: TextureCoordinates1 = uv; break;
            case 2: TextureCoordinates2 = uv; break;
            case 3: TextureCoordinates3 = uv; break;
            case 4: TextureCoordinates4 = uv; break;
            case 5: TextureCoordinates5 = uv; break;
            default: throw new ArgumentException($"At most {MaxUVs} UVs are supported.");
        }
    }

    public Vector4 GetColor(int index)
    {
        return index switch
        {
            0 => Color0,
            1 => Color1,
            _ => throw new ArgumentException($"At most {MaxColors} color maps are supported."),
        };
    }

    public void SetColor(int index, Vector4 color)
    {
        switch (index)
        {
            case 0: Color0 = color; break;
            case 1: Color1 = color; break;
            default: throw new ArgumentException($"At most {MaxColors} color maps are supported.");
        }
    }

    public Vertex Clone()
    {
        return MemberwiseClone() as Vertex;
    }

    public void AddInfluence(byte boneIndex, byte weight)
    {
        // Get the first zero vertex influence and update it with the new one
        for (var influence = 0; influence < MaxBoneInfluences; influence++)
        {
            if (BoneWeights[influence] == 0)
            {
                // BoneIndices refers to Mesh.BoneBindings[index], not Skeleton.Bones[index] !
                BoneIndices[influence] = boneIndex;
                BoneWeights[influence] = weight;
                break;
            }
        }
    }

    public void FinalizeInfluences()
    {
        for (var influence = 1; influence < MaxBoneInfluences; influence++)
        {
            if (BoneWeights[influence] == 0)
            {
                BoneIndices[influence] = BoneIndices[0];
            }
        }
    }

    public void Transform(Matrix4 transformation, Matrix4 inverse)
    {
        Position = Vector3.TransformPosition(Position, transformation);
        Normal = Vector3.Normalize(Vector3.TransformNormalInverse(Normal, inverse));
        Tangent = Vector3.Normalize(Vector3.TransformNormalInverse(Tangent, inverse));
        Binormal = Vector3.Normalize(Vector3.TransformNormalInverse(Binormal, inverse));
    }

    public void Serialize(WritableSection section)
    {
        VertexSerializationHelpers.Serialize(section, this);
    }

    public void Unserialize(GR2Reader reader)
    {
        VertexSerializationHelpers.Unserialize(reader, this);
    }
}


public class VertexSerializer : NodeSerializer
{
    private Dictionary<object, VertexDescriptor> VertexTypeCache = new Dictionary<object, VertexDescriptor>();

    public VertexDescriptor ConstructDescriptor(MemberDefinition memberDefn, StructDefinition defn, object parent)
    {
        var desc = new VertexDescriptor();
        
        foreach (var member in defn.Members)
        {
            switch (member.Name)
            {
                case "Position":
                    if (member.Type == MemberType.Real32 && member.ArraySize == 3)
                    {
                        desc.PositionType = PositionType.Float3;
                    }
                    // Game incorrectly uses UInt16 instead of BinormalInt16 sometimes
                    else if ((member.Type == MemberType.BinormalInt16 || member.Type == MemberType.UInt16) && member.ArraySize == 4)
                    {
                        desc.PositionType = PositionType.Word4;
                    }
                    else
                    {
                        throw new Exception($"Unsupported position format: {member.Type}, {member.ArraySize}");
                    }
                    break;

                case "BoneWeights":
                    if (member.Type != MemberType.NormalUInt8)
                    {
                        throw new Exception("Bone weight must be a NormalUInt8");
                    }

                    if (member.ArraySize != 2 && member.ArraySize != 4)
                    {
                        throw new Exception($"Unsupported bone influence count: {member.ArraySize}");
                    }

                    desc.HasBoneWeights = true;
                    desc.NumBoneInfluences = (int)member.ArraySize;
                    break;

                case "BoneIndices":
                    if (member.Type != MemberType.UInt8)
                    {
                        throw new Exception("Bone index must be an UInt8");
                    }
                    break;

                case "Normal":
                    if (member.Type == MemberType.Real32 && member.ArraySize == 3)
                    {
                        desc.NormalType = NormalType.Float3;
                    }
                    else if (member.Type == MemberType.Real16 && member.ArraySize == 4)
                    {
                        desc.NormalType = NormalType.Half4;
                    }
                    else if (member.Type == MemberType.BinormalInt8 && member.ArraySize == 4)
                    {
                        desc.NormalType = NormalType.Byte4;
                    }
                    else
                    {
                        throw new Exception($"Unsupported normal format: {member.Type}, {member.ArraySize}");
                    }
                    break;

                case "QTangent":
                    // Game incorrectly uses UInt16 instead of BinormalInt16 sometimes
                    if ((member.Type == MemberType.BinormalInt16 || member.Type == MemberType.UInt16) && member.ArraySize == 4)
                    {
                        desc.NormalType = NormalType.QTangent;
                        desc.TangentType = NormalType.QTangent;
                        desc.BinormalType = NormalType.QTangent;
                    }
                    else
                    {
                        throw new Exception($"Unsupported QTangent format: {member.Type}, {member.ArraySize}");
                    }
                    break;

                case "Tangent":
                    if (member.Type == MemberType.Real32 && member.ArraySize == 3)
                    {
                        desc.TangentType = NormalType.Float3;
                    }
                    else if (member.Type == MemberType.Real16 && member.ArraySize == 4)
                    {
                        desc.TangentType = NormalType.Half4;
                    }
                    else if (member.Type == MemberType.BinormalInt8 && member.ArraySize == 4)
                    {
                        desc.TangentType = NormalType.Byte4;
                    }
                    else
                    {
                        throw new Exception($"Unsupported tangent format: {member.Type}, {member.ArraySize}");
                    }
                    break;

                case "Binormal":
                    if (member.Type == MemberType.Real32 && member.ArraySize == 3)
                    {
                        desc.BinormalType = NormalType.Float3;
                    }
                    else if (member.Type == MemberType.Real16 && member.ArraySize == 4)
                    {
                        desc.BinormalType = NormalType.Half4;
                    }
                    else if (member.Type == MemberType.BinormalInt8 && member.ArraySize == 4)
                    {
                        desc.BinormalType = NormalType.Byte4;
                    }
                    else
                    {
                        throw new Exception($"Unsupported binormal format: {member.Type}, {member.ArraySize}");
                    }
                    break;

                case "DiffuseColor0":
                case "DiffuseColor1":
                    desc.ColorMaps++;
                    if (member.Type == MemberType.Real32 && member.ArraySize == 4)
                    {
                        desc.ColorMapType = ColorMapType.Float4;
                    }
                    else if (member.Type == MemberType.NormalUInt8 && member.ArraySize == 4)
                    {
                        desc.ColorMapType = ColorMapType.Byte4;
                    }
                    //Some Granny2 model formats report their color maps as UInt8 type instead of NormalUInt8, causing it to fail checks.
                    else if (member.Type == MemberType.UInt8 && member.ArraySize == 4)
                    {
                        desc.ColorMapType = ColorMapType.Byte4;
                    }
                    else
                    {
                        throw new Exception($"Unsupported color map type: {member.Type}, {member.ArraySize}");
                    }
                    break;

                case "TextureCoordinates0":
                case "TextureCoordinates1":
                case "TextureCoordinates2":
                case "TextureCoordinates3":
                case "TextureCoordinates4":
                case "TextureCoordinates5":
                    desc.TextureCoordinates++;
                    if (member.Type == MemberType.Real32 && member.ArraySize == 2)
                    {
                        desc.TextureCoordinateType = TextureCoordinateType.Float2;
                    }
                    else if (member.Type == MemberType.Real16 && member.ArraySize == 2)
                    {
                        desc.TextureCoordinateType = TextureCoordinateType.Half2;
                    }
                    else
                    {
                        throw new Exception($"Unsupported texture coordinate format: {member.Type}, {member.ArraySize}");
                    }
                    break;

                default:
                    throw new Exception($"Unknown vertex property: {member.Name}");
            }
        }

        return desc;
    }

    public Vertex ReadVertex(GR2Reader reader, VertexDescriptor descriptor)
    {
        var vertex = descriptor.CreateInstance();
        vertex.Unserialize(reader);
        return vertex;
    }

    public object Read(GR2Reader gr2, StructDefinition definition, MemberDefinition member, uint arraySize, object parent)
    {
        if (!VertexTypeCache.TryGetValue(parent, out VertexDescriptor descriptor))
        {
            descriptor = ConstructDescriptor(member, definition, parent);
            VertexTypeCache.Add(parent, descriptor);
        }

        var vertices = new List<Vertex>((int)arraySize);
        for (int i = 0; i < arraySize; i++)
            vertices.Add(ReadVertex(gr2, descriptor));
        return vertices;
    }

    public void Write(GR2Writer writer, WritableSection section, MemberDefinition member, object obj)
    {
        var items = obj as List<Vertex>;

        if (items.Count > 0)
        {
            section.StoreObjectOffset(items[0]);
        }

        for (int i = 0; i < items.Count; i++)
        {
            items[i].Serialize(section);
        }
    }
}
