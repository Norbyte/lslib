using OpenTK.Mathematics;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model;

public class Deduplicator<T>(IEqualityComparer<T> comparer)
{
    private readonly IEqualityComparer<T> Comparer = comparer;
    public Dictionary<int, int> DeduplicationMap = [];
    public List<T> Uniques = [];

    public void MakeIdentityMapping(IEnumerable<T> items)
    {
        var i = 0;
        foreach (var item in items)
        {
            Uniques.Add(item);
            DeduplicationMap.Add(i, i);
            i++;
        }
    }

    public void Deduplicate(IEnumerable<T> items)
    {
        var uniqueItems = new Dictionary<T, int>(Comparer);
        var i = 0;
        foreach (var item in items)
        {
            if (!uniqueItems.TryGetValue(item, out int mappedIndex))
            {
                mappedIndex = uniqueItems.Count;
                uniqueItems.Add(item, mappedIndex);
                Uniques.Add(item);
            }

            DeduplicationMap.Add(i, mappedIndex);
            i++;
        }
    }
}

class GenericEqualityComparer<T> : IEqualityComparer<T> where T : IEquatable<T>
{
    public bool Equals(T a, T b)
    {
        return a.Equals(b);
    }

    public int GetHashCode(T v)
    {
        return v.GetHashCode();
    }
}

public struct SkinnedVertex : IEquatable<SkinnedVertex>
{
    public Vector3 Position;
    public BoneWeight Indices;
    public BoneWeight Weights;

    public bool Equals(SkinnedVertex w)
    {
        return Position.Equals(w.Position)
            && Indices.Equals(w.Indices)
            && Weights.Equals(w.Weights);
    }

    public override int GetHashCode()
    {
        return Position.GetHashCode() ^ Indices.GetHashCode() ^ Weights.GetHashCode();
    }
}

public class VertexDeduplicator
{
    public Deduplicator<SkinnedVertex> Vertices = new(new GenericEqualityComparer<SkinnedVertex>());
    public Deduplicator<Matrix3> Normals = new(new GenericEqualityComparer<Matrix3>());
    public List<Deduplicator<Vector2>> UVs = [];
    public List<Deduplicator<Vector4>> Colors = [];

    public void MakeIdentityMapping(List<Vertex> vertices)
    {
        if (vertices.Count == 0) return;

        var format = vertices[0].Format;

        Vertices.MakeIdentityMapping(vertices.Select(v => new SkinnedVertex {
            Position = v.Position,
            Indices = v.BoneIndices,
            Weights = v.BoneWeights
        }));

        if (format.NormalType != NormalType.None
            || format.TangentType != NormalType.None
            || format.BinormalType != NormalType.None)
        {
            Normals.MakeIdentityMapping(vertices.Select(v => new Matrix3(v.Normal, v.Tangent, v.Binormal)));
        }

        var numUvs = format.TextureCoordinates;
        for (var uv = 0; uv < numUvs; uv++)
        {
            var uvDedup = new Deduplicator<Vector2>(new GenericEqualityComparer<Vector2>());
            uvDedup.MakeIdentityMapping(vertices.Select(v => v.GetUV(uv)));
            UVs.Add(uvDedup);
        }

        var numColors = format.ColorMaps;
        for (var color = 0; color < numColors; color++)
        {
            var colorDedup = new Deduplicator<Vector4>(new GenericEqualityComparer<Vector4>());
            colorDedup.MakeIdentityMapping(vertices.Select(v => v.GetColor(color)));
            Colors.Add(colorDedup);
        }
    }

    public void Deduplicate(List<Vertex> vertices)
    {
        if (vertices.Count == 0) return;

        var format = vertices[0].Format;

        Vertices.Deduplicate(vertices.Select(v => new SkinnedVertex
        {
            Position = v.Position,
            Indices = v.BoneIndices,
            Weights = v.BoneWeights
        }));

        if (format.NormalType != NormalType.None
            || format.TangentType != NormalType.None
            || format.BinormalType != NormalType.None)
        {
            Normals.Deduplicate(vertices.Select(v => new Matrix3(v.Normal, v.Tangent, v.Binormal)));
        }

        var numUvs = format.TextureCoordinates;
        for (var uv = 0; uv < numUvs; uv++)
        {
            var uvDedup = new Deduplicator<Vector2>(new GenericEqualityComparer<Vector2>());
            uvDedup.Deduplicate(vertices.Select(v => v.GetUV(uv)));
            UVs.Add(uvDedup);
        }

        var numColors = format.ColorMaps;
        for (var color = 0; color < numColors; color++)
        {
            var colorDedup = new Deduplicator<Vector4>(new GenericEqualityComparer<Vector4>());
            colorDedup.Deduplicate(vertices.Select(v => v.GetColor(color)));
            Colors.Add(colorDedup);
        }
    }
}

public class VertexAnnotationSet
{
    public string Name;
    [Serialization(Type = MemberType.ReferenceToVariantArray)]
    public List<object> VertexAnnotations;
    public Int32 IndicesMapFromVertexToAnnotation;
    public List<TriIndex> VertexAnnotationIndices;
}

public class VertexDataSectionSelector : SectionSelector
{
    public SectionType SelectSection(MemberDefinition member, Type type, object obj)
    {
        if (obj is VertexData data)
        {
            return data.SerializationSection;
        }
        else
        {
            return SectionType.Invalid;
        }
    }
}

public class VertexData
{
    [Serialization(Type = MemberType.ReferenceToVariantArray, MixedMarshal = true,
        TypeSelector = typeof(VertexSerializer), Serializer = typeof(VertexSerializer),
        Kind = SerializationKind.UserMember)]
    public List<Vertex> Vertices;
    public List<GrannyString> VertexComponentNames;
    public List<VertexAnnotationSet> VertexAnnotationSets;
    [Serialization(Kind = SerializationKind.None)]
    public VertexDeduplicator Deduplicator;

    [Serialization(Kind = SerializationKind.None)]
    public SectionType SerializationSection = SectionType.Invalid;

    public void PostLoad()
    {
        // Fix missing vertex component names
        if (VertexComponentNames == null)
        {
            VertexComponentNames = [];
            if (Vertices.Count > 0)
            {
                var components = Vertices[0].Format.ComponentNames();
                foreach (var name in components)
                {
                    VertexComponentNames.Add(new GrannyString(name));
                }
            }
        }
    }

    public void Deduplicate()
    {
        Deduplicator = new VertexDeduplicator();
        Deduplicator.Deduplicate(Vertices);
    }

    private void EnsureDeduplicationMap()
    {
        // Makes sure that we have an original -> duplicate vertex index map to work with.
        // If we don't, it creates an identity mapping between the original and the Collada vertices.
        // To deduplicate GR2 vertex data, Deduplicate() should be called before any Collada export call.
        if (Deduplicator == null)
        {
            Deduplicator = new VertexDeduplicator();
            Deduplicator.MakeIdentityMapping(Vertices);
        }
    }

    public source MakeColladaPositions(string name)
    {
        EnsureDeduplicationMap();

        int index = 0;
        var positions = new float[Deduplicator.Vertices.Uniques.Count * 3];
        foreach (var vertex in Deduplicator.Vertices.Uniques)
        {
            var pos = vertex.Position;
            positions[index++] = pos[0];
            positions[index++] = pos[1];
            positions[index++] = pos[2];
        }

        return ColladaUtils.MakeFloatSource(name, "positions", ["X", "Y", "Z"], positions);
    }

    public source MakeColladaNormals(string name)
    {
        EnsureDeduplicationMap();

        int index = 0;
        var normals = new float[Deduplicator.Normals.Uniques.Count * 3];
        foreach (var ntb in Deduplicator.Normals.Uniques)
        {
            var normal = ntb.Row0;
            normals[index++] = normal[0];
            normals[index++] = normal[1];
            normals[index++] = normal[2];
        }

        return ColladaUtils.MakeFloatSource(name, "normals", ["X", "Y", "Z"], normals);
    }

    public source MakeColladaTangents(string name)
    {
        EnsureDeduplicationMap();

        int index = 0;
        var tangents = new float[Deduplicator.Normals.Uniques.Count * 3];
        foreach (var ntb in Deduplicator.Normals.Uniques)
        {
            var tangent = ntb.Row1;
            tangents[index++] = tangent[0];
            tangents[index++] = tangent[1];
            tangents[index++] = tangent[2];
        }

        return ColladaUtils.MakeFloatSource(name, "tangents", ["X", "Y", "Z"], tangents);
    }

    public source MakeColladaBinormals(string name)
    {
        EnsureDeduplicationMap();

        int index = 0;
        var binormals = new float[Deduplicator.Normals.Uniques.Count * 3];
        foreach (var ntb in Deduplicator.Normals.Uniques)
        {
            var binormal = ntb.Row2;
            binormals[index++] = binormal[0];
            binormals[index++] = binormal[1];
            binormals[index++] = binormal[2];
        }

        return ColladaUtils.MakeFloatSource(name, "binormals", ["X", "Y", "Z"], binormals);
    }

    public source MakeColladaUVs(string name, int uvIndex, bool flip)
    {
        EnsureDeduplicationMap();

        int index = 0;
        var uvs = new float[Deduplicator.UVs[uvIndex].Uniques.Count * 2];
        foreach (var uv in Deduplicator.UVs[uvIndex].Uniques)
        {
            uvs[index++] = uv[0];
            if (flip)
                uvs[index++] = 1.0f - uv[1];
            else
                uvs[index++] = uv[1];
        }

        return ColladaUtils.MakeFloatSource(name, "uvs" + uvIndex.ToString(), ["S", "T"], uvs);
    }

    public source MakeColladaColors(string name, int setIndex)
    {
        EnsureDeduplicationMap();

        int index = 0;
        var colors = new float[Deduplicator.Colors[setIndex].Uniques.Count * 3];
        foreach (var color in Deduplicator.Colors[setIndex].Uniques)
        {
            colors[index++] = color[0];
            colors[index++] = color[1];
            colors[index++] = color[2];
        }

        return ColladaUtils.MakeFloatSource(name, "colors" + setIndex.ToString(), ["R", "G", "B"], colors);
    }

    public source MakeBoneWeights(string name)
    {
        EnsureDeduplicationMap();

        var weights = new List<float>(Deduplicator.Vertices.Uniques.Count);
        foreach (var vertex in Deduplicator.Vertices.Uniques)
        {
            var boneWeights = vertex.Weights;
            for (int i = 0; i < 4; i++)
            {
                if (boneWeights[i] > 0)
                    weights.Add(boneWeights[i] / 255.0f);
            }
        }

        return ColladaUtils.MakeFloatSource(name, "weights", ["WEIGHT"], weights.ToArray());
    }

    public void Transform(Matrix4 transformation)
    {
        var inverse = transformation.Inverted();

        foreach (var vertex in Vertices)
        {
            vertex.Transform(transformation, inverse);
        }
    }

    public void Flip()
    {
        foreach (var vertex in Vertices)
        {
            vertex.Position.X *= -1;
            vertex.Normal.X *= -1;
            vertex.Tangent.X *= -1;
            vertex.Binormal.X *= -1;
        }

        if (Deduplicator == null) return;

        for (var i = 0; i < Deduplicator.Vertices.Uniques.Count; i++)
        {
            var vert = Deduplicator.Vertices.Uniques[i];
            vert.Position.X *= -1;
            Deduplicator.Vertices.Uniques[i] = vert;
        }

        for (var i = 0; i < Deduplicator.Normals.Uniques.Count; i++)
        {
            var normal = Deduplicator.Normals.Uniques[i];
            normal.Row0.X *= -1; // vertex.Normal.X
            normal.Row1.X *= -1; // vertex.Tangent.X
            normal.Row2.X *= -1; // vertex.Binormal.X
            Deduplicator.Normals.Uniques[i] = normal;
        }
    }
}

public class TriTopologyGroup
{
    public int MaterialIndex;
    public int TriFirst;
    public int TriCount;
}

public class TriIndex
{
    public Int32 Int32;
}

public class TriIndex16
{
    public Int16 Int16;
}

public class TriAnnotationSet
{
    public string Name;
    [Serialization(Type = MemberType.ReferenceToVariantArray)]
    public object TriAnnotations;
    public Int32 IndicesMapFromTriToAnnotation;
    [Serialization(Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer))]
    public List<Int32> TriAnnotationIndices;
}

public class TriTopologySectionSelector : SectionSelector
{
    public SectionType SelectSection(MemberDefinition member, Type type, object obj)
    {
        if (obj is TriTopology)
        {
            return ((TriTopology)obj).SerializationSection;
        }
        else
        {
            return SectionType.Invalid;
        }
}
}

public class TriTopology
{
    public List<TriTopologyGroup> Groups;
    [Serialization(Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer))]
    public List<Int32> Indices;
    [Serialization(Prototype = typeof(TriIndex16), Kind = SerializationKind.UserMember, Serializer = typeof(UInt16ListSerializer))]
    public List<UInt16> Indices16;
    [Serialization(Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer))]
    public List<Int32> VertexToVertexMap;
    [Serialization(Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer))]
    public List<Int32> VertexToTriangleMap;
    [Serialization(Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer))]
    public List<Int32> SideToNeighborMap;
    [Serialization(Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer), MinVersion = 0x80000038)]
    public List<Int32> PolygonIndexStarts;
    [Serialization(Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer), MinVersion = 0x80000038)]
    public List<Int32> PolygonIndices;
    [Serialization(Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer))]
    public List<Int32> BonesForTriangle;
    [Serialization(Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer))]
    public List<Int32> TriangleToBoneIndices;
    public List<TriAnnotationSet> TriAnnotationSets;

    [Serialization(Kind = SerializationKind.None)]
    public SectionType SerializationSection = SectionType.Invalid;

    public void ChangeWindingOrder()
    {
        if (Indices != null)
        {
            var tris = Indices.Count / 3;
            for (var i = 0; i < tris; i++)
            {
                var v1 = Indices[i * 3 + 1];
                Indices[i * 3 + 1] = Indices[i * 3 + 2];
                Indices[i * 3 + 2] = v1;
            }
        }

        if (Indices16 != null)
        {
            var tris = Indices16.Count / 3;
            for (var i = 0; i < tris; i++)
            {
                var v1 = Indices16[i * 3 + 1];
                Indices16[i * 3 + 1] = Indices16[i * 3 + 2];
                Indices16[i * 3 + 2] = v1;
            }
        }
    }

    public void PostLoad()
    {
        // Convert 16-bit vertex indices to 32-bit indices
        // (for convenience, so we won't have to handle both Indices and Indices16 in all code paths)
        if (Indices16 != null)
        {
            Indices = new List<Int32>(Indices16.Count);
            foreach (var index in Indices16)
            {
                Indices.Add(index);
            }

            Indices16 = null;
        }
    }

    public triangles MakeColladaTriangles(InputLocalOffset[] inputs, 
        Dictionary<int, int> positionMaps,
        Dictionary<int, int> normalMaps,
        List<Dictionary<int, int>> uvMaps, 
        List<Dictionary<int, int>> colorMaps)
    {
        int numTris = (from grp in Groups
                       select grp.TriCount).Sum();

        var tris = new triangles
        {
            count = (ulong)numTris,
            input = inputs
        };

        List<Dictionary<int, int>> inputMaps = [];
        int uvIndex = 0, colorIndex = 0;
        for (int i = 0; i < inputs.Length; i++)
        {
            var input = inputs[i];
            switch (input.semantic)
            {
                case "VERTEX": inputMaps.Add(positionMaps); break;
                case "NORMAL":
                case "TANGENT":
                case "BINORMAL": 
                case "TEXTANGENT":
                case "TEXBINORMAL": 
                    inputMaps.Add(normalMaps); break;
                case "TEXCOORD": inputMaps.Add(uvMaps[uvIndex]); uvIndex++; break;
                case "COLOR": inputMaps.Add(colorMaps[colorIndex]); colorIndex++; break;
                default: throw new InvalidOperationException("No input maps available for semantic " + input.semantic);
            }
        }

        var indicesBuilder = new StringBuilder();
        foreach (var group in Groups)
        {
            var indices = Indices;
            for (int index = group.TriFirst; index < group.TriFirst + group.TriCount; index++)
            {
                int firstIdx = index * 3;
                for (int vertIndex = 0; vertIndex < 3; vertIndex++)
                {
                    for (int i = 0; i < inputs.Length; i++)
                    {
                        indicesBuilder.Append(inputMaps[i][indices[firstIdx + vertIndex]]);
                        indicesBuilder.Append(" ");
                    }
                }
            }
        }

        tris.p = indicesBuilder.ToString();
        return tris;
    }
}

public class BoneBinding
{
    public string BoneName;
    [Serialization(ArraySize = 3)]
    public float[] OBBMin;
    [Serialization(ArraySize = 3)]
    public float[] OBBMax;
    [Serialization(Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer))]
    public List<Int32> TriangleIndices;
}

public class MaterialReference
{
    public string Usage;
    public Material Map;
}

public class TextureLayout
{
    public Int32 BytesPerPixel;
    [Serialization(ArraySize = 4)]
    public Int32[] ShiftForComponent;
    [Serialization(ArraySize = 4)]
    public Int32[] BitsForComponent;
}

public class PixelByte
{
    public Byte UInt8;
}

public class TextureMipLevel
{
    public Int32 Stride;
    public List<PixelByte> PixelBytes;
}

public class TextureImage
{
    public List<TextureMipLevel> MIPLevels;
}

public class Texture
{
    public string FromFileName;
    public Int32 TextureType;
    public Int32 Width;
    public Int32 Height;
    public Int32 Encoding;
    public Int32 SubFormat;
    [Serialization(Type = MemberType.Inline)]
    public TextureLayout Layout;
    public List<TextureImage> Images;
    public object ExtendedData;
}

public class Material
{
    public string Name;
    public List<MaterialReference> Maps;
    public Texture Texture;
    public object ExtendedData;
}

public class MaterialBinding
{
    public Material Material;
}

public class MorphTarget
{
    public string ScalarName;
    [Serialization(SectionSelector = typeof(VertexDataSectionSelector))]
    public VertexData VertexData;
    public Int32 DataIsDeltas;
}

public class Mesh
{
    public string Name;
    [Serialization(SectionSelector = typeof(VertexDataSectionSelector))]
    public VertexData PrimaryVertexData;
    public List<MorphTarget> MorphTargets;
    [Serialization(SectionSelector = typeof(TriTopologySectionSelector))]
    public TriTopology PrimaryTopology;
    [Serialization(DataArea = true)]
    public List<MaterialBinding> MaterialBindings;
    public List<BoneBinding> BoneBindings;
    [Serialization(Type = MemberType.VariantReference)]
    public DivinityMeshExtendedData ExtendedData;

    [Serialization(Kind = SerializationKind.None)]
    public Dictionary<int, List<int>> OriginalToConsolidatedVertexIndexMap;

    [Serialization(Kind = SerializationKind.None)]
    public VertexDescriptor VertexFormat;

    [Serialization(Kind = SerializationKind.None)]
    public int ExportOrder = -1;

    public void PostLoad()
    {
        if (PrimaryVertexData.Vertices.Count > 0)
        {
            VertexFormat = PrimaryVertexData.Vertices[0].Format;
        }

        if (ExtendedData != null && ExtendedData.UserMeshProperties.MeshFlags == 0)
        {
            ExtendedData.UserMeshProperties.MeshFlags = AutodetectMeshFlags();
        }
    }
    public DivinityModelFlag AutodetectMeshFlags()
    {
        DivinityModelFlag flags = 0;

        if (ExtendedData != null 
            && ExtendedData.UserMeshProperties != null
            && ExtendedData.UserMeshProperties.MeshFlags != 0)
        {
            return ExtendedData.UserMeshProperties.MeshFlags;
        }

        if (ExtendedData != null 
            && ExtendedData.UserDefinedProperties != null)
        {
            flags = UserDefinedPropertiesHelpers.UserDefinedPropertiesToMeshType(ExtendedData.UserDefinedProperties);
        }
        else
        {
            // Only mark model as cloth if it has colored vertices
            if (VertexFormat.ColorMaps > 0)
            {
                flags |= DivinityModelFlag.Cloth;
            }

            if (!VertexFormat.HasBoneWeights)
            {
                flags |= DivinityModelFlag.Rigid;
            }
        }

        return flags;
    }

    public List<string> VertexComponentNames()
    {
        if (PrimaryVertexData.VertexComponentNames != null
            && PrimaryVertexData.VertexComponentNames.Count > 0
            && PrimaryVertexData.VertexComponentNames[0].String != "")
        {
            return PrimaryVertexData.VertexComponentNames.Select(s => s.String).ToList();
        }
        else if (PrimaryVertexData.Vertices != null
            && PrimaryVertexData.Vertices.Count > 0)
        {
            return PrimaryVertexData.Vertices[0].Format.ComponentNames();
        }
        else
        {
            throw new ParsingException("Unable to determine mesh component list: No vertices and vertex component names available.");
        }
    }

    public bool IsSkinned()
    {
        // Check if we have both the BoneWeights and BoneIndices vertex components.
        bool hasWeights = false, hasIndices = false;

        // If we have vertices, check the vertex prototype, as VertexComponentNames is unreliable.
        if (PrimaryVertexData.Vertices.Count > 0)
        {
            var desc = PrimaryVertexData.Vertices[0].Format;
            hasWeights = hasIndices = desc.HasBoneWeights;
        }
        else
        {
            // Otherwise try to figure out the components from VertexComponentNames
            foreach (var component in PrimaryVertexData.VertexComponentNames)
            {
                if (component.String == "BoneWeights")
                    hasWeights = true;
                else if (component.String == "BoneIndices")
                    hasIndices = true;
            }
        }

        return hasWeights && hasIndices;
    }
}
