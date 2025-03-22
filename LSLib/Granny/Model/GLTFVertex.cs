using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Geometry.VertexTypes;
using System.Numerics;
using TKVec2 = OpenTK.Mathematics.Vector2;
using TKVec3 = OpenTK.Mathematics.Vector3;
using TKVec4 = OpenTK.Mathematics.Vector4;
using System.Reflection;

namespace LSLib.Granny.Model;

public interface GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert);
    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert);
}

public class GLTFVertexNoneBuilder : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
    }
}

public class GLTFVertexGeometryBuilderPosition : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var pos = gr2Vert.Position;
        var v = new VertexPosition(pos.X, pos.Y, pos.Z);
        gltfVert.SetGeometry(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexPositionNormal)gltfVert.GetGeometry();
        var pos = geom.Position;
        gr2Vert.Position = new TKVec3(pos.X, pos.Y, pos.Z);
    }
}

public class GLTFVertexGeometryBuilderPositionNormal : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var pos = gr2Vert.Position;
        var n = gr2Vert.Normal;
        var v = new VertexPositionNormal(
            pos.X, pos.Y, pos.Z,
            n.X, n.Y, n.Z
        );
        gltfVert.SetGeometry(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexPositionNormal)gltfVert.GetGeometry();
        var pos = geom.Position;
        var n = geom.Normal;
        gr2Vert.Position = new TKVec3(pos.X, pos.Y, pos.Z);
        gr2Vert.Normal = new TKVec3(n.X, n.Y, n.Z);
    }
}

public class GLTFVertexGeometryBuilderPositionNormalTangent : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var pos = gr2Vert.Position;
        var n = gr2Vert.Normal;
        var t = gr2Vert.Tangent;
        var b = gr2Vert.Binormal;
        var w = (TKVec3.Dot(TKVec3.Cross(n, t), b) < 0.0F) ? -1.0F : 1.0F;

        var v = new VertexPositionNormalTangent(
            new Vector3(pos.X, pos.Y, pos.Z),
            new Vector3(n.X, n.Y, n.Z),
            new Vector4(t.X, t.Y, t.Z, w)
        );
        gltfVert.SetGeometry(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexPositionNormalTangent)gltfVert.GetGeometry();
        var pos = geom.Position;
        var n = geom.Normal;
        var t = geom.Tangent;
        gr2Vert.Position = new TKVec3(pos.X, pos.Y, pos.Z);
        gr2Vert.Normal = new TKVec3(n.X, n.Y, n.Z);
        gr2Vert.Tangent = new TKVec3(t.X, t.Y, t.Z);
        gr2Vert.Binormal = (TKVec3.Cross(gr2Vert.Normal, gr2Vert.Tangent) * t.W).Normalized();
    }
}

public class GLTFVertexBuilderNOTIMPL : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        throw new Exception("Not implemented yet");
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        throw new Exception("Not implemented yet");
    }
}

public class GLTFVertexMaterialBuilderTexture1 : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var uv0 = gr2Vert.TextureCoordinates0;
        var v = new VertexTexture1(
            new Vector2(uv0.X, uv0.Y)
        );
        gltfVert.SetMaterial(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexTexture1)gltfVert.GetMaterial();
        var uv0 = geom.TexCoord;
        gr2Vert.TextureCoordinates0 = new TKVec2(uv0.X, uv0.Y);
    }
}

public class GLTFVertexMaterialBuilderTexture2 : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var uv0 = gr2Vert.TextureCoordinates0;
        var uv1 = gr2Vert.TextureCoordinates1;
        var v = new VertexTexture2(
            new Vector2(uv0.X, uv0.Y),
            new Vector2(uv1.X, uv1.Y)
        );
        gltfVert.SetMaterial(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexTexture2)gltfVert.GetMaterial();
        var uv0 = geom.TexCoord0;
        var uv1 = geom.TexCoord1;
        gr2Vert.TextureCoordinates0 = new TKVec2(uv0.X, uv0.Y);
        gr2Vert.TextureCoordinates1 = new TKVec2(uv1.X, uv1.Y);
    }
}

public class GLTFVertexMaterialBuilderTexture3 : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var uv0 = gr2Vert.TextureCoordinates0;
        var uv1 = gr2Vert.TextureCoordinates1;
        var uv2 = gr2Vert.TextureCoordinates2;
        var v = new VertexTexture3(
            new Vector2(uv0.X, uv0.Y),
            new Vector2(uv1.X, uv1.Y),
            new Vector2(uv2.X, uv2.Y)
        );
        gltfVert.SetMaterial(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexTexture3)gltfVert.GetMaterial();
        var uv0 = geom.TexCoord0;
        var uv1 = geom.TexCoord1;
        var uv2 = geom.TexCoord2;
        gr2Vert.TextureCoordinates0 = new TKVec2(uv0.X, uv0.Y);
        gr2Vert.TextureCoordinates1 = new TKVec2(uv1.X, uv1.Y);
        gr2Vert.TextureCoordinates2 = new TKVec2(uv2.X, uv2.Y);
    }
}

public class GLTFVertexMaterialBuilderTexture4 : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var uv0 = gr2Vert.TextureCoordinates0;
        var uv1 = gr2Vert.TextureCoordinates1;
        var uv2 = gr2Vert.TextureCoordinates2;
        var uv3 = gr2Vert.TextureCoordinates3;
        var v = new VertexTexture4(
            new Vector2(uv0.X, uv0.Y),
            new Vector2(uv1.X, uv1.Y),
            new Vector2(uv2.X, uv2.Y),
            new Vector2(uv3.X, uv3.Y)
        );
        gltfVert.SetMaterial(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexTexture4)gltfVert.GetMaterial();
        var uv0 = geom.TexCoord0;
        var uv1 = geom.TexCoord1;
        var uv2 = geom.TexCoord2;
        var uv3 = geom.TexCoord3;
        gr2Vert.TextureCoordinates0 = new TKVec2(uv0.X, uv0.Y);
        gr2Vert.TextureCoordinates1 = new TKVec2(uv1.X, uv1.Y);
        gr2Vert.TextureCoordinates2 = new TKVec2(uv2.X, uv2.Y);
        gr2Vert.TextureCoordinates3 = new TKVec2(uv3.X, uv3.Y);
    }
}

public class GLTFVertexMaterialBuilderColor1Texture1 : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var c0 = gr2Vert.Color0;
        var uv0 = gr2Vert.TextureCoordinates0;
        var v = new VertexColor1Texture1(
            new Vector4(c0.X, c0.Y, c0.Z, c0.W),
            new Vector2(uv0.X, uv0.Y)
        );
        gltfVert.SetMaterial(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexColor1Texture1)gltfVert.GetMaterial();
        var uv0 = geom.TexCoord;
        var c0 = geom.Color;
        gr2Vert.TextureCoordinates0 = new TKVec2(uv0.X, uv0.Y);
        gr2Vert.Color0 = new TKVec4(c0.X, c0.Y, c0.Z, c0.W);
    }
}

public class GLTFVertexMaterialBuilderColor1Texture2 : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var c0 = gr2Vert.Color0;
        var uv0 = gr2Vert.TextureCoordinates0;
        var uv1 = gr2Vert.TextureCoordinates1;
        var v = new VertexColor1Texture2(
            new Vector4(c0.X, c0.Y, c0.Z, c0.W),
            new Vector2(uv0.X, uv0.Y),
            new Vector2(uv1.X, uv1.Y)
        );
        gltfVert.SetMaterial(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexColor1Texture2)gltfVert.GetMaterial();
        var uv0 = geom.TexCoord0;
        var uv1 = geom.TexCoord1;
        var c0 = geom.Color;
        gr2Vert.TextureCoordinates0 = new TKVec2(uv0.X, uv0.Y);
        gr2Vert.TextureCoordinates1 = new TKVec2(uv1.X, uv1.Y);
        gr2Vert.Color0 = new TKVec4(c0.X, c0.Y, c0.Z, c0.W);
    }
}

public class GLTFVertexMaterialBuilderColor2Texture1 : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var c0 = gr2Vert.Color0;
        var c1 = gr2Vert.Color1;
        var uv0 = gr2Vert.TextureCoordinates0;
        var v = new VertexColor2Texture1(
            new Vector4(c0.X, c0.Y, c0.Z, c0.W),
            new Vector4(c1.X, c1.Y, c1.Z, c1.W),
            new Vector2(uv0.X, uv0.Y)
        );
        gltfVert.SetMaterial(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexColor2Texture1)gltfVert.GetMaterial();
        var uv0 = geom.TexCoord;
        var c0 = geom.Color0;
        var c1 = geom.Color1;
        gr2Vert.TextureCoordinates0 = new TKVec2(uv0.X, uv0.Y);
        gr2Vert.Color0 = new TKVec4(c0.X, c0.Y, c0.Z, c0.W);
        gr2Vert.Color1 = new TKVec4(c1.X, c1.Y, c1.Z, c1.W);
    }
}

public class GLTFVertexMaterialBuilderColor2Texture2 : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var c0 = gr2Vert.Color0;
        var c1 = gr2Vert.Color1;
        var uv0 = gr2Vert.TextureCoordinates0;
        var uv1 = gr2Vert.TextureCoordinates1;
        var v = new VertexColor2Texture2(
            new Vector4(c0.X, c0.Y, c0.Z, c0.W),
            new Vector4(c1.X, c1.Y, c1.Z, c1.W),
            new Vector2(uv0.X, uv0.Y),
            new Vector2(uv1.X, uv1.Y)
        );
        gltfVert.SetMaterial(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexColor2Texture2)gltfVert.GetMaterial();
        var uv0 = geom.TexCoord0;
        var uv1 = geom.TexCoord1;
        var c0 = geom.Color0;
        var c1 = geom.Color1;
        gr2Vert.TextureCoordinates0 = new TKVec2(uv0.X, uv0.Y);
        gr2Vert.TextureCoordinates1 = new TKVec2(uv1.X, uv1.Y);
        gr2Vert.Color0 = new TKVec4(c0.X, c0.Y, c0.Z, c0.W);
        gr2Vert.Color1 = new TKVec4(c1.X, c1.Y, c1.Z, c1.W);
    }
}

public class GLTFVertexSkinningBuilder : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var v = new VertexJoints4(
            (gr2Vert.BoneIndices.A, gr2Vert.BoneWeights.A / 255.0f),
            (gr2Vert.BoneIndices.B, gr2Vert.BoneWeights.B / 255.0f),
            (gr2Vert.BoneIndices.C, gr2Vert.BoneWeights.C / 255.0f),
            (gr2Vert.BoneIndices.D, gr2Vert.BoneWeights.D / 255.0f)
        );
        gltfVert.SetSkinning(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var skin = (VertexJoints4)gltfVert.GetSkinning();
        Span<byte> weights = stackalloc byte[4];
        VertexHelpers.CompressBoneWeights([skin.Weights.X, skin.Weights.Y, skin.Weights.Z, skin.Weights.W], weights);

        gr2Vert.BoneIndices.A = (byte)skin.Joints[0];
        gr2Vert.BoneIndices.B = (byte)skin.Joints[1];
        gr2Vert.BoneIndices.C = (byte)skin.Joints[2];
        gr2Vert.BoneIndices.D = (byte)skin.Joints[3];

        gr2Vert.BoneWeights.A = weights[0];
        gr2Vert.BoneWeights.B = weights[1];
        gr2Vert.BoneWeights.C = weights[2];
        gr2Vert.BoneWeights.D = weights[3];

        gr2Vert.FinalizeInfluences();
    }
}

public interface IGLTFMeshBuildWrapper
{
    public IMeshBuilder<MaterialBuilder> Build(Mesh m);
}

public class GLTFMeshBuildWrapper<TvG, TvM, TvS> : IGLTFMeshBuildWrapper
    where TvG : struct, IVertexGeometry
    where TvM : struct, IVertexMaterial
    where TvS : struct, IVertexSkinning
{
    private GLTFVertexBuildHelper BuildHelper;
    private MeshBuilder<MaterialBuilder, TvG, TvM, TvS> Builder;
    private PrimitiveBuilder<MaterialBuilder, TvG, TvM, TvS> Primitives;
    private List<IVertexBuilder> Vertices;
    private List<(int A, int B, int C)> TriIndices;

    public GLTFMeshBuildWrapper(GLTFVertexBuildHelper helper, string exportedId)
    {
        BuildHelper = helper;
        Builder = new MeshBuilder<MaterialBuilder, TvG, TvM, TvS>(exportedId);
        var material = new MaterialBuilder("Dummy");
        Primitives = Builder.UsePrimitive(material);

        var triInds = Primitives.GetType().GetField("_TriIndices", BindingFlags.Instance | BindingFlags.NonPublic);
        TriIndices = (List<(int A, int B, int C)>)triInds.GetValue(Primitives);
    }

    private void BuildVertices(Mesh mesh)
    {
        Vertices = new List<IVertexBuilder>(mesh.PrimaryVertexData.Vertices.Count);

        foreach (var v in mesh.PrimaryVertexData.Vertices)
        {
            var vert = Primitives.VertexFactory();
            BuildHelper.ToGLTF(vert, v);
            Vertices.Add(vert);
        }
    }

    public IMeshBuilder<MaterialBuilder> Build(Mesh m)
    {
        BuildVertices(m);

        var useVert = Primitives.GetType().BaseType.GetMethod("UseVertex", BindingFlags.Instance | BindingFlags.NonPublic);
        // var fun = useVert.CreateDelegate<Func<VertexBuilder<TvG, TvM, TvS>, Int32>>(Primitives);
        foreach (var vertex in Vertices)
        {
            //fun((VertexBuilder<TvG, TvM, TvS>)vertex);
            useVert.Invoke(Primitives, [vertex]);
        }

        var inds = m.PrimaryTopology.Indices;
        for (var i = 0; i < inds.Count; i += 3)
        {
            // Primitives.AddTriangle(Vertices[inds[i]], Vertices[inds[i + 1]], Vertices[inds[i + 2]]);
            TriIndices.Add((inds[i], inds[i+1], inds[i+2]));
        }

        return Builder;
    }
}

public class GLTFVertexBuildHelper
{
    private readonly string ExportedId;
    private readonly VertexDescriptor VertexFormat;

    private GLTFVertexBuilder GeometryBuilder;
    private GLTFVertexBuilder MaterialBuilder;
    private GLTFVertexBuilder SkinningBuilder;

    private Type GeometryDataType;
    private Type MaterialDataType;
    private Type SkinningDataType;

    private int UVs;
    private int ColorMaps;
    private bool HasNormals;
    private bool HasTangents;

    public GLTFVertexBuildHelper(string exportedId, VertexDescriptor vertexFormat)
    {
        ExportedId = exportedId;
        VertexFormat = vertexFormat;

        HasNormals = VertexFormat.NormalType != NormalType.None;
        HasTangents = VertexFormat.TangentType != NormalType.None;
        UVs = VertexFormat.TextureCoordinates;
        ColorMaps = VertexFormat.ColorMaps;

        SelectGeometryBuilder();
        SelectMaterialBuilder();
        SelectSkinningBuilder();
    }

    private void SelectGeometryBuilder()
    {
        if (HasNormals)
        {
            if (HasTangents)
            {
                GeometryDataType = typeof(VertexPositionNormalTangent);
                GeometryBuilder = new GLTFVertexGeometryBuilderPositionNormalTangent();
            }
            else
            {
                GeometryDataType = typeof(VertexPositionNormal);
                GeometryBuilder = new GLTFVertexGeometryBuilderPositionNormal();
            }
        }
        else
        {
            GeometryDataType = typeof(VertexPosition);
            GeometryBuilder = new GLTFVertexGeometryBuilderPosition();
        }
    }

    private void SelectMaterialBuilder()
    {
        if (UVs == 0 && ColorMaps == 0)
        {
            MaterialDataType = typeof(VertexEmpty);
            MaterialBuilder = new GLTFVertexNoneBuilder();
        }
        else if (UVs == 1 && ColorMaps == 0)
        {
            MaterialDataType = typeof(VertexTexture1);
            MaterialBuilder = new GLTFVertexMaterialBuilderTexture1();
        }
        else if (UVs == 2 && ColorMaps == 0)
        {
            MaterialDataType = typeof(VertexTexture2);
            MaterialBuilder = new GLTFVertexMaterialBuilderTexture2();
        }
        else if (UVs == 3 && ColorMaps == 0)
        {
            MaterialDataType = typeof(VertexTexture3);
            MaterialBuilder = new GLTFVertexMaterialBuilderTexture3();
        }
        else if (UVs == 4 && ColorMaps == 0)
        {
            MaterialDataType = typeof(VertexTexture4);
            MaterialBuilder = new GLTFVertexMaterialBuilderTexture4();
        }
        else if (UVs == 1 && ColorMaps == 1)
        {
            MaterialDataType = typeof(VertexColor1Texture1);
            MaterialBuilder = new GLTFVertexMaterialBuilderColor1Texture1();
        }
        else if (UVs == 2 && ColorMaps == 1)
        {
            MaterialDataType = typeof(VertexColor1Texture2);
            MaterialBuilder = new GLTFVertexMaterialBuilderColor1Texture2();
        }
        else if (UVs == 1 && ColorMaps == 2)
        {
            MaterialDataType = typeof(VertexColor2Texture1);
            MaterialBuilder = new GLTFVertexMaterialBuilderColor2Texture1();
        }
        else if (UVs == 2 && ColorMaps == 2)
        {
            MaterialDataType = typeof(VertexColor2Texture2);
            MaterialBuilder = new GLTFVertexMaterialBuilderColor2Texture2();
        }
        else
        {
            throw new InvalidDataException($"Unsupported vertex format for glTF export: UVs {UVs}, Color maps {ColorMaps}");
        }
    }

    private void SelectSkinningBuilder()
    {
        if (VertexFormat.HasBoneWeights)
        {
            SkinningDataType = typeof(VertexJoints4);
            SkinningBuilder = new GLTFVertexSkinningBuilder();
        }
        else
        {
            SkinningDataType = typeof(VertexEmpty);
            SkinningBuilder = new GLTFVertexNoneBuilder();
        }
    }

    public IGLTFMeshBuildWrapper InternalCreateBuilder<TvG, TvM, TvS>()
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        return new GLTFMeshBuildWrapper<TvG, TvM, TvS>(this, ExportedId);
    }

    public IGLTFMeshBuildWrapper CreateBuilder()
    {
        return (IGLTFMeshBuildWrapper)GetType()
            .GetMethod("InternalCreateBuilder")
            .MakeGenericMethod([GeometryDataType, MaterialDataType, SkinningDataType])
            .Invoke(this, []);
    }

    public void ToGLTF(IVertexBuilder gltf, Vertex gr)
    {
        GeometryBuilder.ToGLTF(gltf, gr);
        MaterialBuilder.ToGLTF(gltf, gr);
        SkinningBuilder.ToGLTF(gltf, gr);
    }

    public Vertex FromGLTF(IVertexBuilder gltf)
    {
        var gr = VertexFormat.CreateInstance();
        GeometryBuilder.FromGLTF(gltf, gr);
        MaterialBuilder.FromGLTF(gltf, gr);
        SkinningBuilder.FromGLTF(gltf, gr);
        return gr;
    }
}

public class GLTFMeshExporter(Mesh mesh, string exportedId)
{
    private readonly Mesh ExportedMesh = mesh;
    private readonly GLTFVertexBuildHelper BuildHelper = new(exportedId, mesh.VertexFormat);

    public IMeshBuilder<MaterialBuilder> Export()
    {
        var builder = BuildHelper.CreateBuilder();
        return builder.Build(ExportedMesh);
    }
}
