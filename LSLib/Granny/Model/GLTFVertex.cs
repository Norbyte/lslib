using OpenTK.Mathematics;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using System.Numerics;
using System.Reflection;
using TKQuat = OpenTK.Mathematics.Quaternion;
using TKVec2 = OpenTK.Mathematics.Vector2;
using TKVec3 = OpenTK.Mathematics.Vector3;
using TKVec4 = OpenTK.Mathematics.Vector4;

namespace LSLib.Granny.Model;

static class GLTFConversionHelpers
{
    public static TKVec2 ToOpenTK(this System.Numerics.Vector2 v)
    {
        return new TKVec2(v.X, v.Y);
    }
    
    public static TKVec3 ToOpenTK(this System.Numerics.Vector3 v)
    {
        return new TKVec3(v.X, v.Y, v.Z);
    }
    
    public static TKVec4 ToOpenTK(this System.Numerics.Vector4 v)
    {
        return new TKVec4(v.X, v.Y, v.Z, v.W);
    }
    
    public static TKQuat ToOpenTK(this System.Numerics.Quaternion v)
    {
        return new TKQuat(v.X, v.Y, v.Z, v.W);
    }

    public static System.Numerics.Vector2 ToNumerics(this TKVec2 v)
    {
        return new System.Numerics.Vector2(v.X, v.Y);
    }
    
    public static System.Numerics.Vector3 ToNumerics(this TKVec3 v)
    {
        return new System.Numerics.Vector3(v.X, v.Y, v.Z);
    }
    
    public static System.Numerics.Vector4 ToNumerics(this TKVec4 v)
    {
        return new System.Numerics.Vector4(v.X, v.Y, v.Z, v.W);
    }
    
    public static System.Numerics.Quaternion ToNumerics(this TKQuat v)
    {
        return new System.Numerics.Quaternion(v.X, v.Y, v.Z, v.W);
    }
}

public interface GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert);
    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert);
}

public interface GLTFVertexSkinBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert, int[] remaps);
    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert, int[] remaps);
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
            pos.ToNumerics(),
            n.ToNumerics(),
            new System.Numerics.Vector4(t.X, t.Y, t.Z, w)
        );
        gltfVert.SetGeometry(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexPositionNormalTangent)gltfVert.GetGeometry();
        var t = geom.Tangent;
        gr2Vert.Position = geom.Position.ToOpenTK();
        gr2Vert.Normal = geom.Normal.ToOpenTK();
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
        var v = new VertexTexture1(
            gr2Vert.TextureCoordinates0.ToNumerics()
        );
        gltfVert.SetMaterial(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexTexture1)gltfVert.GetMaterial();
        gr2Vert.TextureCoordinates0 = geom.TexCoord.ToOpenTK();
    }
}

public class GLTFVertexMaterialBuilderTexture2 : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var v = new VertexTexture2(
            gr2Vert.TextureCoordinates0.ToNumerics(),
            gr2Vert.TextureCoordinates1.ToNumerics()
        );
        gltfVert.SetMaterial(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexTexture2)gltfVert.GetMaterial();
        gr2Vert.TextureCoordinates0 = geom.TexCoord0.ToOpenTK();
        gr2Vert.TextureCoordinates1 = geom.TexCoord1.ToOpenTK();
    }
}

public class GLTFVertexMaterialBuilderTexture3 : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var v = new VertexTexture3(
            gr2Vert.TextureCoordinates0.ToNumerics(),
            gr2Vert.TextureCoordinates1.ToNumerics(),
            gr2Vert.TextureCoordinates2.ToNumerics()
        );
        gltfVert.SetMaterial(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexTexture3)gltfVert.GetMaterial();
        gr2Vert.TextureCoordinates0 = geom.TexCoord0.ToOpenTK();
        gr2Vert.TextureCoordinates1 = geom.TexCoord1.ToOpenTK();
        gr2Vert.TextureCoordinates2 = geom.TexCoord2.ToOpenTK();
    }
}

public class GLTFVertexMaterialBuilderTexture4 : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var v = new VertexTexture4(
            gr2Vert.TextureCoordinates0.ToNumerics(),
            gr2Vert.TextureCoordinates1.ToNumerics(),
            gr2Vert.TextureCoordinates2.ToNumerics(),
            gr2Vert.TextureCoordinates3.ToNumerics()
        );
        gltfVert.SetMaterial(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexTexture4)gltfVert.GetMaterial();
        gr2Vert.TextureCoordinates0 = geom.TexCoord0.ToOpenTK();
        gr2Vert.TextureCoordinates1 = geom.TexCoord1.ToOpenTK();
        gr2Vert.TextureCoordinates2 = geom.TexCoord2.ToOpenTK();
        gr2Vert.TextureCoordinates3 = geom.TexCoord3.ToOpenTK();
    }
}

public class GLTFVertexMaterialBuilderColor1Texture1 : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var v = new VertexColor1Texture1(
            gr2Vert.Color0.ToNumerics(),
            gr2Vert.TextureCoordinates0.ToNumerics()
        );
        gltfVert.SetMaterial(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexColor1Texture1)gltfVert.GetMaterial();
        gr2Vert.TextureCoordinates0 = geom.TexCoord.ToOpenTK();
        gr2Vert.Color0 = geom.Color.ToOpenTK();
    }
}

public class GLTFVertexMaterialBuilderColor1Texture2 : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var v = new VertexColor1Texture2(
            gr2Vert.Color0.ToNumerics(),
            gr2Vert.TextureCoordinates0.ToNumerics(),
            gr2Vert.TextureCoordinates1.ToNumerics()
        );
        gltfVert.SetMaterial(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexColor1Texture2)gltfVert.GetMaterial();
        gr2Vert.TextureCoordinates0 = geom.TexCoord0.ToOpenTK();
        gr2Vert.TextureCoordinates1 = geom.TexCoord1.ToOpenTK();
        gr2Vert.Color0 = geom.Color.ToOpenTK();
    }
}

public class GLTFVertexMaterialBuilderColor2Texture1 : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var v = new VertexColor2Texture1(
            gr2Vert.Color0.ToNumerics(),
            gr2Vert.Color1.ToNumerics(),
            gr2Vert.TextureCoordinates0.ToNumerics()
        );
        gltfVert.SetMaterial(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexColor2Texture1)gltfVert.GetMaterial();
        gr2Vert.TextureCoordinates0 = geom.TexCoord.ToOpenTK();
        gr2Vert.Color0 = geom.Color0.ToOpenTK();
        gr2Vert.Color1 = geom.Color1.ToOpenTK();
    }
}

public class GLTFVertexMaterialBuilderColor2Texture2 : GLTFVertexBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var v = new VertexColor2Texture2(
            gr2Vert.Color0.ToNumerics(),
            gr2Vert.Color1.ToNumerics(),
            gr2Vert.TextureCoordinates0.ToNumerics(),
            gr2Vert.TextureCoordinates1.ToNumerics()
        );
        gltfVert.SetMaterial(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert)
    {
        var geom = (VertexColor2Texture2)gltfVert.GetMaterial();
        gr2Vert.TextureCoordinates0 = geom.TexCoord0.ToOpenTK();
        gr2Vert.TextureCoordinates1 = geom.TexCoord1.ToOpenTK();
        gr2Vert.Color0 = geom.Color0.ToOpenTK();
        gr2Vert.Color1 = geom.Color1.ToOpenTK();
    }
}

public class GLTFVertexNoneSkinBuilder : GLTFVertexSkinBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert, int[] remaps)
    {
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert, int[] remaps)
    {
    }
}

public class GLTFVertexSkinningBuilder : GLTFVertexSkinBuilder
{
    public void ToGLTF(IVertexBuilder gltfVert, Vertex gr2Vert, int[] remaps)
    {
        var v = new VertexJoints4(
            (remaps[gr2Vert.BoneIndices.A], gr2Vert.BoneWeights.A / 255.0f),
            (remaps[gr2Vert.BoneIndices.B], gr2Vert.BoneWeights.B / 255.0f),
            (remaps[gr2Vert.BoneIndices.C], gr2Vert.BoneWeights.C / 255.0f),
            (remaps[gr2Vert.BoneIndices.D], gr2Vert.BoneWeights.D / 255.0f)
        );
        gltfVert.SetSkinning(v);
    }

    public void FromGLTF(IVertexBuilder gltfVert, Vertex gr2Vert, int[] remaps)
    {
        var skin = (VertexJoints4)gltfVert.GetSkinning();
        Span<byte> weights = stackalloc byte[4];
        VertexHelpers.CompressBoneWeights([skin.Weights.X, skin.Weights.Y, skin.Weights.Z, skin.Weights.W], weights);

        gr2Vert.BoneIndices.A = (byte)remaps[(byte)skin.Joints[0]];
        gr2Vert.BoneIndices.B = (byte)remaps[(byte)skin.Joints[1]];
        gr2Vert.BoneIndices.C = (byte)remaps[(byte)skin.Joints[2]];
        gr2Vert.BoneIndices.D = (byte)remaps[(byte)skin.Joints[3]];

        gr2Vert.BoneWeights.A = weights[0];
        gr2Vert.BoneWeights.B = weights[1];
        gr2Vert.BoneWeights.C = weights[2];
        gr2Vert.BoneWeights.D = weights[3];

        gr2Vert.FinalizeInfluences();
    }
}

public interface IMorphedMeshBuilder<MaterialBuilder> : IMeshBuilder<MaterialBuilder>
{
    float[] GetMorphWeights();
}

public class MorphedMeshBuilder<MaterialBuilder, TvG, TvM, TvS> : MeshBuilder<MaterialBuilder, TvG, TvM, TvS>, 
    IMorphedMeshBuilder<MaterialBuilder>
    where TvG : struct, IVertexGeometry
    where TvM : struct, IVertexMaterial
    where TvS : struct, IVertexSkinning
{
    internal float[] MorphWeights;

    public float[] GetMorphWeights()
    {
        return MorphWeights;
    }

    public MorphedMeshBuilder(string name)
        : base(name)
    {
    }
}

public interface IGLTFMeshBuildWrapper
{
    public IMorphedMeshBuilder<MaterialBuilder> Build(Mesh m);
}

public class GLTFMeshBuildWrapper<TvG, TvM, TvS> : IGLTFMeshBuildWrapper
    where TvG : struct, IVertexGeometry
    where TvM : struct, IVertexMaterial
    where TvS : struct, IVertexSkinning
{
    private GLTFVertexBuildHelper BuildHelper;
    private MorphedMeshBuilder<MaterialBuilder, TvG, TvM, TvS> Builder;
    private PrimitiveBuilder<MaterialBuilder, TvG, TvM, TvS> Primitives;
    private List<MorphTargetBuilder<MaterialBuilder, TvG, TvS, TvM>> MorphTargets = [];
    private List<IVertexBuilder> Vertices;
    private List<(int A, int B, int C)> TriIndices;

    public GLTFMeshBuildWrapper(GLTFVertexBuildHelper helper, string exportedId)
    {
        BuildHelper = helper;
        Builder = new MorphedMeshBuilder<MaterialBuilder, TvG, TvM, TvS>(exportedId);
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

    private float BuildMorphTarget(Mesh m, MorphTarget target, MorphTargetBuilder<MaterialBuilder, TvG, TvS, TvM> gltf)
    {
        if (target.DataIsDeltas != 1)
        {
            throw new InvalidDataException("Only delta morph targets are supported");
        }

        var annotations = target.VertexData.VertexAnnotationSets;
        if (annotations.Count != 2
            || annotations[0].Name != "MaxVertDisplacement"
            || annotations[1].Name != "BlendShapeIndexMapping")
        {
            throw new InvalidDataException("Unsupported morph annotation layout");
        }

        if (annotations[0].VertexAnnotations is not List<Single>
            || annotations[1].VertexAnnotations is not List<UInt16>)
        {
            throw new InvalidDataException("Unsupported morph target data format");
        }

        var maxDisplacement = (List<Single>)annotations[0].VertexAnnotations;
        var indexMapping = (List<UInt16>)annotations[1].VertexAnnotations;

        if (indexMapping.Count != m.PrimaryVertexData.Vertices.Count)
        {
            throw new InvalidDataException("Morph target vertex count mismatch");
        }

        for (var i = 0; i < indexMapping.Count; ++i)
        {
            var index = indexMapping[i];
            var vertex = m.PrimaryVertexData.Vertices[i];
            var displacement = target.VertexData.Vertices[index];

            gltf.SetVertexDelta(vertex.Position.ToNumerics(), new VertexGeometryDelta(
                displacement.Position.ToNumerics(),
                displacement.Normal.ToNumerics(),
                displacement.Tangent.ToNumerics()
            ));
        }

        return maxDisplacement[0];
    }

    public IMorphedMeshBuilder<MaterialBuilder> Build(Mesh m)
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

        if (m.MorphTargets != null && m.PrimaryVertexData.Vertices.Count > 0)
        {
            MorphTargets = [];
            var weights = new float[m.MorphTargets.Count];
            for (var i = 0; i < m.MorphTargets.Count; i++)
            {
                var target = Builder.UseMorphTarget(i);
                MorphTargets.Add(target);
                var maxDisplacement = BuildMorphTarget(m, m.MorphTargets[i], target);
                weights[i] = maxDisplacement;
            }

            Builder.MorphWeights = weights;
        }

        return Builder;
    }
}

public class GLTFVertexBuildHelper
{
    private readonly string ExportedId;
    private readonly VertexDescriptor VertexFormat;
    private readonly int[] JointRemaps;

    private GLTFVertexBuilder GeometryBuilder;
    private GLTFVertexBuilder MaterialBuilder;
    private GLTFVertexSkinBuilder SkinningBuilder;

    private Type GeometryDataType;
    private Type MaterialDataType;
    private Type SkinningDataType;

    private int UVs;
    private int ColorMaps;
    private bool HasNormals;
    private bool HasTangents;

    public GLTFVertexBuildHelper(string exportedId, VertexDescriptor vertexFormat, int[] jointRemaps)
    {
        ExportedId = exportedId;
        VertexFormat = vertexFormat;
        JointRemaps = jointRemaps;

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
            SkinningBuilder = new GLTFVertexNoneSkinBuilder();
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
        SkinningBuilder.ToGLTF(gltf, gr, JointRemaps);
    }

    public Vertex FromGLTF(IVertexBuilder gltf)
    {
        var gr = VertexFormat.CreateInstance();
        GeometryBuilder.FromGLTF(gltf, gr);
        MaterialBuilder.FromGLTF(gltf, gr);
        SkinningBuilder.FromGLTF(gltf, gr, JointRemaps);
        return gr;
    }
}

public class GLTFMeshExporter(Mesh mesh, string exportedId, int[] jointRemaps)
{
    private readonly Mesh ExportedMesh = mesh;
    private readonly GLTFVertexBuildHelper BuildHelper = new(exportedId, mesh.VertexFormat, jointRemaps);

    public IMorphedMeshBuilder<MaterialBuilder> Export()
    {
        var builder = BuildHelper.CreateBuilder();
        return builder.Build(ExportedMesh);
    }
}
