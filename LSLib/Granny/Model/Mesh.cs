using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model
{
    public class VertexDeduplicator
    {
        public Dictionary<int, int> VertexDeduplicationMap = new Dictionary<int, int>();
        public List<Dictionary<int, int>> UVDeduplicationMaps = new List<Dictionary<int, int>>();
        public List<Dictionary<int, int>> ColorDeduplicationMaps = new List<Dictionary<int, int>>();
        public List<Vertex> DeduplicatedPositions = new List<Vertex>();
        public List<List<Vector2>> DeduplicatedUVs = new List<List<Vector2>>();
        public List<List<Vector4>> DeduplicatedColors = new List<List<Vector4>>();

        private class VertexPositionComparer : IEqualityComparer<Vector3>
        {
            public bool Equals(Vector3 a, Vector3 b)
            {
                return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
            }

            public int GetHashCode(Vector3 v)
            {
                int hash = 17;
                hash = hash * 23 + v.X.GetHashCode();
                hash = hash * 23 + v.Y.GetHashCode();
                hash = hash * 23 + v.Z.GetHashCode();
                return hash;
            }
        }

        private class VertexUVComparer : IEqualityComparer<Vector2>
        {
            public bool Equals(Vector2 a, Vector2 b)
            {
                return a.X == b.X && a.Y == b.Y;
            }

            public int GetHashCode(Vector2 v)
            {
                int hash = 17;
                hash = hash * 23 + v.X.GetHashCode();
                hash = hash * 23 + v.Y.GetHashCode();
                return hash;
            }
        }

        private class VertexColorComparer : IEqualityComparer<Vector4>
        {
            public bool Equals(Vector4 a, Vector4 b)
            {
                return a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W;
            }

            public int GetHashCode(Vector4 v)
            {
                int hash = 17;
                hash = hash * 23 + v.X.GetHashCode();
                hash = hash * 23 + v.Y.GetHashCode();
                hash = hash * 23 + v.Z.GetHashCode();
                hash = hash * 23 + v.W.GetHashCode();
                return hash;
            }
        }

        public void MakeIdentityMapping(List<Vertex> vertices)
        {
            for (var i = 0; i < vertices.Count; i++)
            {
                DeduplicatedPositions.Add(vertices[i]);
                VertexDeduplicationMap.Add(i, i);
            }

            var numUvs = vertices[0].Format.TextureCoordinates;
            for (var uv = 0; uv < numUvs; uv++)
            {
                var uvMap = new Dictionary<int, int>();
                var deduplicatedUvs = new List<Vector2>();
                UVDeduplicationMaps.Add(uvMap);
                DeduplicatedUVs.Add(deduplicatedUvs);

                for (var i = 0; i < vertices.Count; i++)
                {
                    deduplicatedUvs.Add(vertices[i].GetUV(uv));
                    uvMap.Add(i, i);
                }
            }

            var numColors = vertices[0].Format.DiffuseColors;
            for (var color = 0; color < numColors; color++)
            {
                var colorMap = new Dictionary<int, int>();
                var deduplicatedColors = new List<Vector4>();
                ColorDeduplicationMaps.Add(colorMap);
                DeduplicatedColors.Add(deduplicatedColors);

                for (var i = 0; i < vertices.Count; i++)
                {
                    deduplicatedColors.Add(vertices[i].GetColor(color));
                    colorMap.Add(i, i);
                }
            }
        }

        public void Deduplicate(List<Vertex> vertices)
        {
            var positions = new Dictionary<Vector3, int>(new VertexPositionComparer());
            for (var i = 0; i < vertices.Count; i++)
            {
                int mappedIndex;
                if (!positions.TryGetValue(vertices[i].Position, out mappedIndex))
                {
                    mappedIndex = positions.Count;
                    positions.Add(vertices[i].Position, mappedIndex);
                    DeduplicatedPositions.Add(vertices[i]);
                }

                VertexDeduplicationMap.Add(i, mappedIndex);
            }

            var numUvs = vertices[0].Format.TextureCoordinates;
            for (var uv = 0; uv < numUvs; uv++)
            {
                var uvMap = new Dictionary<int, int>();
                var deduplicatedUvs = new List<Vector2>();
                UVDeduplicationMaps.Add(uvMap);
                DeduplicatedUVs.Add(deduplicatedUvs);

                var uvs = new Dictionary<Vector2, int>(new VertexUVComparer());
                for (var i = 0; i < vertices.Count; i++)
                {
                    int mappedIndex;
                    if (!uvs.TryGetValue(vertices[i].GetUV(uv), out mappedIndex))
                    {
                        mappedIndex = uvs.Count;
                        uvs.Add(vertices[i].GetUV(uv), mappedIndex);
                        deduplicatedUvs.Add(vertices[i].GetUV(uv));
                    }

                    uvMap.Add(i, mappedIndex);
                }
            }

            var numColors = vertices[0].Format.DiffuseColors;
            for (var color = 0; color < numColors; color++)
            {
                var colorMap = new Dictionary<int, int>();
                var deduplicatedColors = new List<Vector4>();
                ColorDeduplicationMaps.Add(colorMap);
                DeduplicatedColors.Add(deduplicatedColors);

                var colors = new Dictionary<Vector4, int>(new VertexColorComparer());
                for (var i = 0; i < vertices.Count; i++)
                {
                    int mappedIndex;
                    if (!colors.TryGetValue(vertices[i].GetColor(color), out mappedIndex))
                    {
                        mappedIndex = colors.Count;
                        colors.Add(vertices[i].GetColor(color), mappedIndex);
                        deduplicatedColors.Add(vertices[i].GetColor(color));
                    }

                    colorMap.Add(i, mappedIndex);
                }
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

    public class VertexData
    {
        [Serialization(Type = MemberType.ReferenceToVariantArray, SectionSelector = typeof(VertexSerializer),
            TypeSelector = typeof(VertexSerializer), Serializer = typeof(VertexSerializer),
            Kind = SerializationKind.UserElement)]
        public List<Vertex> Vertices;
        public List<GrannyString> VertexComponentNames;
        public List<VertexAnnotationSet> VertexAnnotationSets;
        [Serialization(Kind = SerializationKind.None)]
        public VertexDeduplicator Deduplicator;

        public void PostLoad()
        {
            // Fix missing vertex component names
            if (VertexComponentNames == null)
            {
                VertexComponentNames = new List<GrannyString>();
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
            var positions = new float[Deduplicator.DeduplicatedPositions.Count * 3];
            foreach (var vertex in Deduplicator.DeduplicatedPositions)
            {
                var pos = vertex.Position;
                positions[index++] = pos[0];
                positions[index++] = pos[1];
                positions[index++] = pos[2];
            }

            return ColladaUtils.MakeFloatSource(name, "positions", new string[] { "X", "Y", "Z" }, positions);
        }

        public source MakeColladaNormals(string name)
        {
            EnsureDeduplicationMap();

            int index = 0;
            var normals = new float[Deduplicator.DeduplicatedPositions.Count * 3];
            foreach (var vertex in Deduplicator.DeduplicatedPositions)
            {
                var normal = vertex.Normal;
                normals[index++] = normal[0];
                normals[index++] = normal[1];
                normals[index++] = normal[2];
            }

            return ColladaUtils.MakeFloatSource(name, "normals", new string[] { "X", "Y", "Z" }, normals);
        }

        public source MakeColladaTangents(string name)
        {
            EnsureDeduplicationMap();

            int index = 0;
            var tangents = new float[Deduplicator.DeduplicatedPositions.Count * 3];
            foreach (var vertex in Deduplicator.DeduplicatedPositions)
            {
                var tangent = vertex.Tangent;
                tangents[index++] = tangent[0];
                tangents[index++] = tangent[1];
                tangents[index++] = tangent[2];
            }

            return ColladaUtils.MakeFloatSource(name, "tangents", new string[] { "X", "Y", "Z" }, tangents);
        }

        public source MakeColladaBinormals(string name)
        {
            EnsureDeduplicationMap();

            int index = 0;
            var binormals = new float[Deduplicator.DeduplicatedPositions.Count * 3];
            foreach (var vertex in Deduplicator.DeduplicatedPositions)
            {
                var binormal = vertex.Binormal;
                binormals[index++] = binormal[0];
                binormals[index++] = binormal[1];
                binormals[index++] = binormal[2];
            }

            return ColladaUtils.MakeFloatSource(name, "binormals", new string[] { "X", "Y", "Z" }, binormals);
        }

        public source MakeColladaUVs(string name, int uvIndex, bool flip)
        {
            EnsureDeduplicationMap();

            int index = 0;
            var uvs = new float[Deduplicator.DeduplicatedUVs[uvIndex].Count * 2];
            foreach (var uv in Deduplicator.DeduplicatedUVs[uvIndex])
            {
                uvs[index++] = uv[0];
                if (flip)
                    uvs[index++] = 1.0f - uv[1];
                else
                    uvs[index++] = uv[1];
            }

            return ColladaUtils.MakeFloatSource(name, "uvs" + uvIndex.ToString(), new string[] { "S", "T" }, uvs);
        }

        public source MakeColladaColors(string name, int setIndex)
        {
            EnsureDeduplicationMap();

            int index = 0;
            var colors = new float[Deduplicator.DeduplicatedColors[setIndex].Count * 3];
            foreach (var color in Deduplicator.DeduplicatedColors[setIndex])
            {
                colors[index++] = color[0];
                colors[index++] = color[1];
                colors[index++] = color[2];
            }

            return ColladaUtils.MakeFloatSource(name, "colors" + setIndex.ToString(), new string[] { "R", "G", "B" }, colors);
        }

        public source MakeBoneWeights(string name)
        {
            EnsureDeduplicationMap();

            var weights = new List<float>(Deduplicator.DeduplicatedPositions.Count);
            foreach (var vertex in Deduplicator.DeduplicatedPositions)
            {
                var boneWeights = vertex.BoneWeights;
                for (int i = 0; i < 4; i++)
                {
                    if (boneWeights[i] > 0)
                        weights.Add(boneWeights[i] / 255.0f);
                }
            }

            return ColladaUtils.MakeFloatSource(name, "weights", new string[] { "WEIGHT" }, weights.ToArray());
        }

        public void Transform(Matrix4 transformation)
        {
            foreach (var vertex in Vertices)
            {
                vertex.Transform(transformation);
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
        [Serialization(Section = SectionType.RigidIndex, Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer))]
        public List<Int32> TriAnnotationIndices;
    }

    public class TriTopology
    {
        public List<TriTopologyGroup> Groups;
        [Serialization(Section = SectionType.DeformableIndex, Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer))]
        public List<Int32> Indices;
        [Serialization(Section = SectionType.DeformableIndex, Prototype = typeof(TriIndex16), Kind = SerializationKind.UserMember, Serializer = typeof(Int16ListSerializer))]
        public List<Int16> Indices16;
        [Serialization(Section = SectionType.DeformableIndex, Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer))]
        public List<Int32> VertexToVertexMap;
        [Serialization(Section = SectionType.DeformableIndex, Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer))]
        public List<Int32> VertexToTriangleMap;
        [Serialization(Section = SectionType.DeformableIndex, Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer))]
        public List<Int32> SideToNeighborMap;
        [Serialization(Section = SectionType.DeformableIndex, Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer), MinVersion = 0x80000038)]
        public List<Int32> PolygonIndexStarts;
        [Serialization(Section = SectionType.DeformableIndex, Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer), MinVersion = 0x80000038)]
        public List<Int32> PolygonIndices;
        [Serialization(Section = SectionType.DeformableIndex, Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer))]
        public List<Int32> BonesForTriangle;
        [Serialization(Section = SectionType.DeformableIndex, Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer))]
        public List<Int32> TriangleToBoneIndices;
        public List<TriAnnotationSet> TriAnnotationSets;

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

        public triangles MakeColladaTriangles(InputLocalOffset[] inputs, Dictionary<int, int> vertexMaps,
            List<Dictionary<int, int>> uvMaps, List<Dictionary<int, int>> colorMaps)
        {
            int numTris = (from grp in Groups
                           select grp.TriCount).Sum();

            var tris = new triangles();
            tris.count = (ulong)numTris;
            tris.input = inputs;

            List<Dictionary<int, int>> inputMaps = new List<Dictionary<int, int>>();
            int uvIndex = 0, colorIndex = 0;
            for (int i = 0; i < inputs.Length; i++)
            {
                var input = inputs[i];
                switch (input.semantic)
                {
                    case "VERTEX": inputMaps.Add(vertexMaps); break;
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
        [Serialization(Section = SectionType.DeformableIndex, Prototype = typeof(TriIndex), Kind = SerializationKind.UserMember, Serializer = typeof(Int32ListSerializer))]
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
        public VertexData VertexData;
        public Int32 DataIsDeltas;
    }

    public class Mesh
    {
        public string Name;
        public VertexData PrimaryVertexData;
        public List<MorphTarget> MorphTargets;
        public TriTopology PrimaryTopology;
        [Serialization(DataArea = true)]
        public List<MaterialBinding> MaterialBindings;
        public List<BoneBinding> BoneBindings;
        [Serialization(Type = MemberType.VariantReference)]
        public DivinityExtendedData ExtendedData;

        [Serialization(Kind = SerializationKind.None)]
        public Dictionary<int, List<int>> OriginalToConsolidatedVertexIndexMap;

        [Serialization(Kind = SerializationKind.None)]
        public VertexDescriptor VertexFormat;

        public void PostLoad()
        {
            if (PrimaryVertexData.Vertices.Count > 0)
            {
                VertexFormat = PrimaryVertexData.Vertices[0].Format;
            }
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
}
