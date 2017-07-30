using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Collada141;
using OpenTK;
using LSLib.Granny.GR2;
using LSLib.Granny.Model.VertexFormat;

namespace LSLib.Granny.Model
{
    public class ColladaMesh
    {
        private mesh Mesh;
        private Dictionary<String, ColladaSource> Sources;
        private InputLocalOffset[] Inputs;
        private List<Vertex> Vertices;
        private List<List<Vector2>> UVs;
        private List<int> Indices;

        private int VertexInputIndex = -1;
        private List<int> UVInputIndices = new List<int>();
        private Type VertexType;
        private bool HasNormals = false;
        private bool HasTangents = false;

        public int TriangleCount;
        public List<Vertex> ConsolidatedVertices;
        public List<int> ConsolidatedIndices;
        public Dictionary<int, List<int>> OriginalToConsolidatedVertexIndexMap;
        private ExporterOptions Options;


        private class VertexIndexComparer : IEqualityComparer<int[]>
        {
            public bool Equals(int[] x, int[] y)
            {
                if (x.Length != y.Length)
                {
                    return false;
                }
                for (int i = 0; i < x.Length; i++)
                {
                    if (x[i] != y[i])
                    {
                        return false;
                    }
                }
                return true;
            }

            public int GetHashCode(int[] obj)
            {
                int result = 17;
                for (int i = 0; i < obj.Length; i++)
                {
                    unchecked
                    {
                        result = result * 23 + obj[i];
                    }
                }
                return result;
            }
        }

        void computeTangents()
        {
            // Check if the vertex format has at least one UV set
            if (ConsolidatedVertices.Count() > 0)
            {
                var v = ConsolidatedVertices[0];
                var descriptor = Vertex.Description(v.GetType());
                if (descriptor.TextureCoordinates == 0)
                {
                    throw new InvalidOperationException("At least one UV set is required to recompute tangents");
                }
            }

            foreach (var v in ConsolidatedVertices)
            {
                v.Tangent = Vector3.Zero;
                v.Binormal = Vector3.Zero;
            }

            for (int i = 0; i < TriangleCount; i++)
            {
                var i1 = ConsolidatedIndices[i * 3 + 0];
                var i2 = ConsolidatedIndices[i * 3 + 1];
                var i3 = ConsolidatedIndices[i * 3 + 2];

                var vert1 = ConsolidatedVertices[i1];
                var vert2 = ConsolidatedVertices[i2];
                var vert3 = ConsolidatedVertices[i3];

                var v1 = vert1.Position;
                var v2 = vert2.Position;
                var v3 = vert3.Position;

                var w1 = vert1.TextureCoordinates0;
                var w2 = vert2.TextureCoordinates0;
                var w3 = vert3.TextureCoordinates0;

                float x1 = v2.X - v1.X;
                float x2 = v3.X - v1.X;
                float y1 = v2.Y - v1.Y;
                float y2 = v3.Y - v1.Y;
                float z1 = v2.Z - v1.Z;
                float z2 = v3.Z - v1.Z;

                float s1 = w2.X - w1.X;
                float s2 = w3.X - w1.X;
                float t1 = w2.Y - w1.Y;
                float t2 = w3.Y - w1.Y;

                float r = 1.0F / (s1 * t2 - s2 * t1);
                var sdir = new Vector3(
                    (t2 * x1 - t1 * x2) * r,
                    (t2 * y1 - t1 * y2) * r,
                    (t2 * z1 - t1 * z2) * r
                );
                var tdir = new Vector3(
                    (s1 * x2 - s2 * x1) * r,
                    (s1 * y2 - s2 * y1) * r,
                    (s1 * z2 - s2 * z1) * r
                );

                vert1.Tangent += sdir;
                vert2.Tangent += sdir;
                vert3.Tangent += sdir;

                vert1.Binormal += tdir;
                vert2.Binormal += tdir;
                vert3.Binormal += tdir;
            }

            foreach (var v in ConsolidatedVertices)
            {
                var n = v.Normal;
                var t = v.Tangent;
                var b = v.Binormal;

                // Gram-Schmidt orthogonalize
                var tangent = (t - n * Vector3.Dot(n, t)).Normalized();

                // Calculate handedness
                var w = (Vector3.Dot(Vector3.Cross(n, t), b) < 0.0F) ? 1.0F : -1.0F;
                var binormal = (Vector3.Cross(n, t) * w).Normalized();

                v.Tangent = tangent;
                v.Binormal = binormal;
            }
        }

        private Vector3 triangleNormalFromVertex(int[] indices, int vertexIndex)
        {
            // This assumes that A->B->C is a counter-clockwise ordering
            var a = Vertices[indices[vertexIndex]].Position;
            var b = Vertices[indices[(vertexIndex + 1) % 3]].Position;
            var c = Vertices[indices[(vertexIndex + 2) % 3]].Position;

            var N = Vector3.Cross(b - a, c - a);
            float sin_alpha = N.Length / ((b - a).Length * (c - a).Length);
            return N.Normalized() * (float)Math.Asin(sin_alpha);
        }

        private int VertexIndexCount()
        {
            return Indices.Count / Inputs.Length;
        }

        private int VertexIndex(int index)
        {
            return Indices[index * Inputs.Length + VertexInputIndex];
        }

        private void computeNormals()
        {
            for (var vertexIdx = 0; vertexIdx < Vertices.Count; vertexIdx++)
            {
                Vector3 N = new Vector3(0, 0, 0);
                for (int triVertIdx = 0; triVertIdx < VertexIndexCount(); triVertIdx++)
                {
                    if (VertexIndex(triVertIdx) == vertexIdx)
                    {
                        int baseIdx = ((int)(triVertIdx / 3)) * 3;
                        var indices = new int[] {
                            VertexIndex(baseIdx + 0),
                            VertexIndex(baseIdx + 1),
                            VertexIndex(baseIdx + 2)
                        };
                        N = N + triangleNormalFromVertex(indices, triVertIdx - baseIdx);
                    }
                }

                N.Normalize();
                Vertices[vertexIdx].Normal = N;
            }
        }

        private void ImportFaces()
        {
            foreach (var item in Mesh.Items)
            {
                if (item is triangles)
                {
                    var tris = item as triangles;
                    TriangleCount = (int)tris.count;
                    Inputs = tris.input;
                    Indices = ColladaHelpers.StringsToIntegers(tris.p);
                }
                else if (item is polylist)
                {
                    var plist = item as polylist;
                    TriangleCount = (int)plist.count;
                    Inputs = plist.input;
                    Indices = ColladaHelpers.StringsToIntegers(plist.p);
                    var vertexCounts = ColladaHelpers.StringsToIntegers(plist.vcount);
                    foreach (var count in vertexCounts)
                    {
                        if (count != 3)
                            throw new ParsingException("Non-triangle found in COLLADA polylist. Make sure that all geometries are triangulated.");
                    }
                }
            }

            if (Indices == null || Inputs == null)
                throw new ParsingException("No valid triangle source found, expected <triangles> or <polylist>");

            if (Indices.Count % (Inputs.Length * 3) != 0 || Indices.Count / Inputs.Length / 3 != TriangleCount)
                throw new ParsingException("Triangle input stride / vertex count mismatch.");
        }

        private void ImportVertices()
        {
            var vertexSemantics = new Dictionary<String, List<Vector3>>();
            foreach (var input in Mesh.vertices.input)
            {
                if (input.source[0] != '#')
                    throw new ParsingException("Only ID references are supported for vertex input sources");

                ColladaSource inputSource = null;
                if (!Sources.TryGetValue(input.source.Substring(1), out inputSource))
                    throw new ParsingException("Vertex input source does not exist: " + input.source);

                List<Single> x = null, y = null, z = null;
                if (!inputSource.FloatParams.TryGetValue("X", out x) ||
                    !inputSource.FloatParams.TryGetValue("Y", out y) ||
                    !inputSource.FloatParams.TryGetValue("Z", out z))
                    throw new ParsingException("Vertex input source " + input.source + " must have X, Y, Z float attributes");

                var vertices = new List<Vector3>(x.Count);
                for (var i = 0; i < x.Count; i++)
                {
                    vertices.Add(new Vector3(x[i], y[i], z[i]));
                }

                vertexSemantics.Add(input.semantic, vertices);
            }

            List<Vector3> positions = null;
            List<Vector3> normals = null;
            List<Vector3> tangents = null;
            List<Vector3> binormals = null;

            vertexSemantics.TryGetValue("POSITION", out positions);
            vertexSemantics.TryGetValue("NORMAL", out normals);
            vertexSemantics.TryGetValue("TANGENT", out tangents);
            vertexSemantics.TryGetValue("BINORMAL", out binormals);

            int normalInputIndex = -1;
            foreach (var input in Inputs)
            {
                if (input.semantic == "VERTEX")
                {
                    VertexInputIndex = (int)input.offset;
                }
                else if (input.semantic == "NORMAL")
                {
                    normals = new List<Vector3>();
                    normalInputIndex = (int)input.offset;

                    if (input.source[0] != '#')
                        throw new ParsingException("Only ID references are supported for Normal input sources");

                    ColladaSource inputSource = null;
                    if (!Sources.TryGetValue(input.source.Substring(1), out inputSource))
                        throw new ParsingException("Normal input source does not exist: " + input.source);

                    List<Single> x = null, y = null, z = null;
                    if (!inputSource.FloatParams.TryGetValue("X", out x) ||
                        !inputSource.FloatParams.TryGetValue("Y", out y) ||
                        !inputSource.FloatParams.TryGetValue("Z", out z))
                        throw new ParsingException("Normal input source " + input.source + " must have X, Y, Z float attributes");

                    for (var i = 0; i < x.Count; i++)
                    {
                        normals.Add(new Vector3(x[i], y[i], z[i]));
                    }
                }
            }

            if (VertexInputIndex == -1)
                throw new ParsingException("Required triangle input semantic missing: VERTEX");

            Vertices = new List<Vertex>(positions.Count);
            var vertexCtor = GR2.Helpers.GetConstructor(VertexType);
            for (var vert = 0; vert < positions.Count; vert++)
            {
                Vertex vertex = vertexCtor() as Vertex;
                vertex.Position = positions[vert];

                if (tangents != null)
                {
                    vertex.Tangent = tangents[vert];
                }

                if (binormals != null)
                {
                    vertex.Binormal = binormals[vert];
                }

                if (normals != null && normalInputIndex == -1)
                {
                    vertex.Normal = normals[vert];
                }

                Vertices.Add(vertex);
            }

            if (normalInputIndex != -1)
            {
                for (var vert = 0; vert < TriangleCount * 3; vert++)
                {
                    var vertexIndex = Indices[vert * Inputs.Length + VertexInputIndex];
                    var normalIndex = Indices[vert * Inputs.Length + normalInputIndex];

                    Vertex vertex = Vertices[vertexIndex];
                    vertex.Normal = normals[normalIndex];
                }
            }

            HasNormals = normals != null;
            HasTangents = tangents != null && binormals != null;
        }

        private void ImportUVs()
        {
            bool flip = Options.FlipUVs;
            UVInputIndices.Clear();
            UVs = new List<List<Vector2>>();
            foreach (var input in Inputs)
            {
                if (input.semantic == "TEXCOORD")
                {
                    UVInputIndices.Add((int)input.offset);

                    if (input.source[0] != '#')
                        throw new ParsingException("Only ID references are supported for UV input sources");

                    ColladaSource inputSource = null;
                    if (!Sources.TryGetValue(input.source.Substring(1), out inputSource))
                        throw new ParsingException("UV input source does not exist: " + input.source);

                    List<Single> s = null, t = null;
                    if (!inputSource.FloatParams.TryGetValue("S", out s) ||
                        !inputSource.FloatParams.TryGetValue("T", out t))
                        throw new ParsingException("UV input source " + input.source + " must have S, T float attributes");

                    var uvs = new List<Vector2>();
                    UVs.Add(uvs);
                    for (var i = 0; i < s.Count; i++)
                    {
                        if (flip) t[i] = 1.0f - t[i];
                        uvs.Add(new Vector2(s[i], t[i]));
                    }
                }
            }
        }

        private void ImportSources()
        {
            Sources = new Dictionary<String, ColladaSource>();
            foreach (var source in Mesh.source)
            {
                var src = ColladaSource.FromCollada(source);
                Sources.Add(src.id, src);
            }
        }

        public void ImportFromCollada(mesh mesh, string vertexFormat, ExporterOptions options)
        {
            Options = options;
            Mesh = mesh;
            VertexType = VertexFormatRegistry.Resolve(vertexFormat);
            ImportSources();
            ImportFaces();
            ImportVertices();

            // TODO: This should be done before deduplication!
            // TODO: Move this to somewhere else ... ?
            if (!HasNormals || Options.RecalculateNormals)
            {
                if (!HasNormals)
                    Utils.Info(String.Format("Channel 'NORMAL' not found, will rebuild vertex normals after import."));
                computeNormals();
            }

            ImportUVs();
            if (UVInputIndices.Count() > 0)
            {
                var outVertexIndices = new Dictionary<int[], int>(new VertexIndexComparer());
                ConsolidatedIndices = new List<int>(TriangleCount * 3);
                ConsolidatedVertices = new List<Vertex>(Vertices.Count);
                OriginalToConsolidatedVertexIndexMap = new Dictionary<int, List<int>>();
                for (var vert = 0; vert < TriangleCount * 3; vert++)
                {
                    var index = new int[Inputs.Length];
                    for (var i = 0; i < Inputs.Length; i++)
                    {
                        index[i] = Indices[vert * Inputs.Length + i];
                    }

                    int consolidatedIndex;
                    if (!outVertexIndices.TryGetValue(index, out consolidatedIndex))
                    {
                        var vertexIndex = index[VertexInputIndex];
                        consolidatedIndex = ConsolidatedVertices.Count;
                        Vertex vertex = Vertices[vertexIndex].Clone();
                        for (int uv = 0; uv < UVInputIndices.Count(); uv++ )
                        {
                            vertex.SetTextureCoordinates(uv, UVs[uv][index[UVInputIndices[uv]]]);
                        }
                        outVertexIndices.Add(index, consolidatedIndex);
                        ConsolidatedVertices.Add(vertex);

                        List<int> mappedIndices = null;
                        if (!OriginalToConsolidatedVertexIndexMap.TryGetValue(vertexIndex, out mappedIndices))
                        {
                            mappedIndices = new List<int>();
                            OriginalToConsolidatedVertexIndexMap.Add(vertexIndex, mappedIndices);
                        }

                        mappedIndices.Add(consolidatedIndex);
                    }

                    ConsolidatedIndices.Add(consolidatedIndex);
                }

                Utils.Info(String.Format("Merged {0} vertices into {1} output vertices", Vertices.Count, ConsolidatedVertices.Count));
            }
            else
            {
                Utils.Info(String.Format("Mesh has no UV map, vertex consolidation step skipped."));

                ConsolidatedVertices = Vertices;

                ConsolidatedIndices = new List<int>(TriangleCount * 3);
                for (var vert = 0; vert < TriangleCount * 3; vert++)
                    ConsolidatedIndices.Add(VertexIndex(vert));

                OriginalToConsolidatedVertexIndexMap = new Dictionary<int, List<int>>();
                for (var i = 0; i < Vertices.Count; i++)
                    OriginalToConsolidatedVertexIndexMap.Add(i, new List<int> { i });
            }

            if (!HasTangents || Options.RecalculateTangents)
            {
                if (!HasTangents)
                    Utils.Info(String.Format("Channel 'TANGENT'/'BINROMAL' not found, will rebuild vertex tangents after import."));
                computeTangents();
            }
        }
    }
}
