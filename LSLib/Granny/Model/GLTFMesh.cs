using LSLib.Granny.GR2;
using SharpGLTF.Scenes;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;

namespace LSLib.Granny.Model;

public class GLTFMesh
{
    private VertexDescriptor InputVertexType;
    private VertexDescriptor OutputVertexType;
    private GLTFVertexBuildHelper BuildHelper;
    private bool HasNormals = false;
    private bool HasTangents = false;

    public InfluencingJoints InfluencingJoints;
    public int TriangleCount;
    public List<Vertex> Vertices;
    public List<int> Indices;
    private ExporterOptions Options;

    public VertexDescriptor InternalVertexType
    {
        get { return OutputVertexType; }
    }

    private void ImportTriangles(IPrimitiveReader<MaterialBuilder> primitives)
    {
        if (primitives.Points.Count > 0 ||
            primitives.Lines.Count > 0 ||
            primitives.VerticesPerPrimitive != 3)
        {
            throw new ParsingException($"glTF mesh needs to be triangulated; "
                + $"got {primitives.Points.Count} points, {primitives.Lines.Count} lines, {primitives.VerticesPerPrimitive} verts per primitive");
        }

        TriangleCount = primitives.Triangles.Count;
        Indices = new List<int>(TriangleCount * 3);
        foreach (var (A, B, C) in primitives.Triangles)
        {
            Indices.Add(A);
            Indices.Add(B);
            Indices.Add(C);
        }
    }

    private void ImportVertices(IPrimitiveReader<MaterialBuilder> primitives, int[] jointRemaps)
    {
        BuildHelper = new GLTFVertexBuildHelper("", OutputVertexType, jointRemaps);

        Vertices = new List<Vertex>(primitives.Vertices.Count);
        foreach (var vert in primitives.Vertices)
        {
            var vertex = BuildHelper.FromGLTF(vert);
            Vertices.Add(vertex);
        }

        HasNormals = (InputVertexType.NormalType != NormalType.None);
        HasTangents = (InputVertexType.TangentType != NormalType.None);
    }

    private VertexDescriptor FindVertexFormat(Type type)
    {
        var desc = new VertexDescriptor
        {
            PositionType = PositionType.Float3
        };

        foreach (var field in type.GetFields())
        {
            if (field.Name == "Geometry")
            {
                if (field.FieldType == typeof(VertexPosition))
                {
                    // No normals available
                }
                else if (field.FieldType == typeof(VertexPositionNormal))
                {
                    desc.NormalType = NormalType.Float3;
                }
                else if (field.FieldType == typeof(VertexPositionNormalTangent))
                {
                    desc.NormalType = NormalType.Float3;
                    desc.TangentType = NormalType.Float3;
                    desc.BinormalType = NormalType.Float3;
                }
                else
                {
                    throw new InvalidDataException($"Unsupported geometry data format: {field.FieldType}");
                }
            }
            else if (field.Name == "Material")
            {
                if (field.FieldType == typeof(VertexEmpty))
                {
                    // No texture data available
                }
                else if (field.FieldType == typeof(VertexTexture1))
                {
                    desc.TextureCoordinateType = TextureCoordinateType.Float2;
                    desc.TextureCoordinates = 1;
                }
                else if (field.FieldType == typeof(VertexTexture2))
                {
                    desc.TextureCoordinateType = TextureCoordinateType.Float2;
                    desc.TextureCoordinates = 2;
                }
                else if (field.FieldType == typeof(VertexTexture3))
                {
                    desc.TextureCoordinateType = TextureCoordinateType.Float2;
                    desc.TextureCoordinates = 3;
                }
                else if (field.FieldType == typeof(VertexTexture4))
                {
                    desc.TextureCoordinateType = TextureCoordinateType.Float2;
                    desc.TextureCoordinates = 4;
                }
                else if (field.FieldType == typeof(VertexColor1Texture1))
                {
                    desc.TextureCoordinateType = TextureCoordinateType.Float2;
                    desc.TextureCoordinates = 1;
                    desc.ColorMapType = ColorMapType.Float4;
                    desc.ColorMaps = 1;
                }
                else if (field.FieldType == typeof(VertexColor1Texture2))
                {
                    desc.TextureCoordinateType = TextureCoordinateType.Float2;
                    desc.TextureCoordinates = 2;
                    desc.ColorMapType = ColorMapType.Float4;
                    desc.ColorMaps = 1;
                }
                else if (field.FieldType == typeof(VertexColor2Texture1))
                {
                    desc.TextureCoordinateType = TextureCoordinateType.Float2;
                    desc.TextureCoordinates = 1;
                    desc.ColorMapType = ColorMapType.Float4;
                    desc.ColorMaps = 2;
                }
                else if (field.FieldType == typeof(VertexColor2Texture2))
                {
                    desc.TextureCoordinateType = TextureCoordinateType.Float2;
                    desc.TextureCoordinates = 2;
                    desc.ColorMapType = ColorMapType.Float4;
                    desc.ColorMaps = 2;
                }
                else
                {
                    throw new InvalidDataException($"Unsupported material data format: {field.FieldType}");
                }
            }
            else if (field.Name == "Skinning")
            {
                if (field.FieldType == typeof(VertexEmpty))
                {
                    // No skinning data available
                }
                else if (field.FieldType == typeof(VertexJoints4))
                {
                    desc.HasBoneWeights = true;
                }
                else
                {
                    throw new InvalidDataException($"Unsupported skinning data format: {field.FieldType}");
                }
            }
        }

        return desc;
    }

    public void ImportFromGLTF(ContentTransformer content, InfluencingJoints influencingJoints, ExporterOptions options, GLTFMeshExtensions extensions)
    {
        var geometry = content.GetGeometryAsset();
        var primitives = geometry.Primitives.First();
        
        Options = options;
        InfluencingJoints = influencingJoints;

        var vertexFormat = FindVertexFormat(primitives.VertexType);
        InputVertexType = vertexFormat;

        if (extensions.Occluder || extensions.MeshProxy)
        {
            // Proxies only have a position attribute, and no other vertex data
            OutputVertexType = new VertexDescriptor
            {
                PositionType = PositionType.Float3
            };
        }
        else
        {
            OutputVertexType = new VertexDescriptor
            {
                HasBoneWeights = InputVertexType.HasBoneWeights,
                NumBoneInfluences = InputVertexType.NumBoneInfluences,
                PositionType = InputVertexType.PositionType,
                NormalType = InputVertexType.NormalType,
                TangentType = InputVertexType.TangentType,
                BinormalType = InputVertexType.BinormalType,
                ColorMapType = InputVertexType.ColorMapType,
                ColorMaps = InputVertexType.ColorMaps,
                TextureCoordinateType = InputVertexType.TextureCoordinateType,
                TextureCoordinates = InputVertexType.TextureCoordinates
            };
        }

        // Objects with a single binding are attached to the skeleton, but are not skinned
        if (InfluencingJoints.SkeletonJoints.Count == 1)
        {
            OutputVertexType.HasBoneWeights = false;
        }

        ImportTriangles(primitives);
        ImportVertices(primitives, influencingJoints?.BindRemaps);

        if (!HasNormals)
        {
            HasNormals = true;
            OutputVertexType.NormalType = NormalType.Float3;
            VertexHelpers.ComputeNormals(Vertices, Indices);
        }

        if ((InputVertexType.TangentType == NormalType.None
            || InputVertexType.BinormalType == NormalType.None)
            && !HasTangents 
            && InputVertexType.TextureCoordinates > 0)
        {
            OutputVertexType.TangentType = NormalType.Float3;
            OutputVertexType.BinormalType = NormalType.Float3;
            HasTangents = true;
            VertexHelpers.ComputeTangents(Vertices, Indices, Options.IgnoreUVNaN);
        }

        if (!HasNormals || !HasTangents)
        {
            throw new InvalidDataException($"Import needs geometry with normal and tangent data");
        }

        // Use optimized tangent, texture map and color map format when exporting for D:OS 2+
        if ((Options.ModelInfoFormat == DivinityModelInfoFormat.LSMv0
            || Options.ModelInfoFormat == DivinityModelInfoFormat.LSMv1
            || Options.ModelInfoFormat == DivinityModelInfoFormat.LSMv3))
        {
            if (Options.EnableQTangents
                && HasNormals
                && HasTangents)
            {
                OutputVertexType.NormalType = NormalType.QTangent;
                OutputVertexType.TangentType = NormalType.QTangent;
                OutputVertexType.BinormalType = NormalType.QTangent;
            }

            if (OutputVertexType.TextureCoordinateType == TextureCoordinateType.Float2)
            {
                OutputVertexType.TextureCoordinateType = TextureCoordinateType.Half2;
            }

            if (OutputVertexType.ColorMapType == ColorMapType.Float4)
            {
                OutputVertexType.ColorMapType = ColorMapType.Byte4;
            }
        }
    }
}
