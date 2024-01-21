using OpenTK.Mathematics;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model;

public class ColladaMesh
{
    private mesh Mesh;
    private Dictionary<String, ColladaSource> Sources;
    private InputLocalOffset[] Inputs;
    private List<Vertex> Vertices;
    private List<Vector3> Normals;
    private List<Vector3> Tangents;
    private List<Vector3> Binormals;
    private List<List<Vector2>> UVs;
    private List<List<Vector4>> Colors;
    private List<int> Indices;

    private int InputOffsetCount = 0;
    private int VertexInputIndex = -1;
    private int NormalsInputIndex = -1;
    private int TangentsInputIndex = -1;
    private int BinormalsInputIndex = -1;
    private List<int> UVInputIndices = [];
    private List<int> ColorInputIndices = [];
    private VertexDescriptor InputVertexType;
    private VertexDescriptor OutputVertexType;
    private bool HasNormals = false;
    private bool HasTangents = false;

    public int TriangleCount;
    public List<Vertex> ConsolidatedVertices;
    public List<int> ConsolidatedIndices;
    public Dictionary<int, List<int>> OriginalToConsolidatedVertexIndexMap;
    private ExporterOptions Options;

    public VertexDescriptor InternalVertexType
    {
        get { return OutputVertexType; }
    }

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
        if (ConsolidatedVertices.Count > 0)
        {
            var v = ConsolidatedVertices[0];
            if (v.Format.TextureCoordinates == 0)
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

            if ((Single.IsNaN(r) || Single.IsInfinity(r)) && !Options.IgnoreUVNaN)
            {
                throw new Exception($"Couldn't calculate tangents; the mesh most likely contains non-manifold geometry.{Environment.NewLine}"
                    + $"UV1: {w1}{Environment.NewLine}UV2: {w2}{Environment.NewLine}UV3: {w3}");
            }

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
        return Indices.Count / InputOffsetCount;
    }

    private int VertexIndex(int index)
    {
        return Indices[index * InputOffsetCount + VertexInputIndex];
    }

    private void computeNormals()
    {
        for (var vertexIdx = 0; vertexIdx < Vertices.Count; vertexIdx++)
        {
            Vector3 N = new(0, 0, 0);
            var numIndices = VertexIndexCount();
            for (int triVertIdx = 0; triVertIdx < numIndices; triVertIdx++)
            {
                if (VertexIndex(triVertIdx) == vertexIdx)
                {
                    int baseIdx = ((int)(triVertIdx / 3)) * 3;
                    var indices = new int[] {
                        VertexIndex(baseIdx + 0),
                        VertexIndex(baseIdx + 1),
                        VertexIndex(baseIdx + 2)
                    };
                    N += triangleNormalFromVertex(indices, triVertIdx - baseIdx);
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
            else if (item is lines)
            {
                throw new ParsingException("Lines found in input geometry. Make sure that all geometries are triangulated.");
            }
        }

        if (Indices == null || Inputs == null)
            throw new ParsingException("No valid triangle source found, expected <triangles> or <polylist>");

        InputOffsetCount = 0;
        foreach (var input in Inputs)
        {
            if ((int)input.offset >= InputOffsetCount)
            {
                InputOffsetCount = (int)input.offset + 1;
            }
        }

        if (Indices.Count % (InputOffsetCount * 3) != 0 || Indices.Count / InputOffsetCount / 3 != TriangleCount)
            throw new ParsingException("Triangle input stride / vertex count mismatch.");
    }

    private ColladaSource FindSource(string id)
    {
        if (id.Length == 0 || id[0] != '#')
            throw new ParsingException("Only ID references are supported for input sources: " + id);

        if (!Sources.TryGetValue(id.Substring(1), out ColladaSource inputSource))
            throw new ParsingException("Input source does not exist: " + id);

        return inputSource;
    }

    private void ImportVertices()
    {
        var vertexSemantics = new Dictionary<String, List<Vector3>>();
        foreach (var input in Mesh.vertices.input)
        {
            ColladaSource inputSource = FindSource(input.source);
            var vertices = ColladaHelpers.SourceToPositions(inputSource);
            vertexSemantics.Add(input.semantic, vertices);
        }

        List<Vector3> vertexPositions = null;
        List<Vector3> perVertexNormals = null;
        List<Vector3> perVertexTangents = null;
        List<Vector3> perVertexBinormals = null;

        vertexSemantics.TryGetValue("POSITION", out vertexPositions);
        vertexSemantics.TryGetValue("NORMAL", out perVertexNormals);
        if (!vertexSemantics.TryGetValue("TANGENT", out perVertexTangents))
        {
            vertexSemantics.TryGetValue("TEXTANGENT", out perVertexTangents);
        }

        if (!vertexSemantics.TryGetValue("BINORMAL", out perVertexBinormals))
        {
            vertexSemantics.TryGetValue("TEXBINORMAL", out perVertexBinormals);
        }

        foreach (var input in Inputs)
        {
            if (input.semantic == "VERTEX")
            {
                VertexInputIndex = (int)input.offset;
            }
            else if (input.semantic == "NORMAL")
            {
                var normalsSource = FindSource(input.source);
                Normals = ColladaHelpers.SourceToPositions(normalsSource);
                NormalsInputIndex = (int)input.offset;
            }
            else if (input.semantic == "TANGENT" || input.semantic == "TEXTANGENT")
            {
                var tangentsSource = FindSource(input.source);
                Tangents = ColladaHelpers.SourceToPositions(tangentsSource);
                TangentsInputIndex = (int)input.offset;
            }
            else if (input.semantic == "BINORMAL" || input.semantic == "TEXBINORMAL")
            {
                var binormalsSource = FindSource(input.source);
                Binormals = ColladaHelpers.SourceToPositions(binormalsSource);
                BinormalsInputIndex = (int)input.offset;
            }
        }

        if (VertexInputIndex == -1)
            throw new ParsingException("Required triangle input semantic missing: VERTEX");

        Vertices = new List<Vertex>(vertexPositions.Count);
        for (var vert = 0; vert < vertexPositions.Count; vert++)
        {
            var vertex = OutputVertexType.CreateInstance();
            vertex.Position = vertexPositions[vert];

            if (perVertexNormals != null)
            {
                vertex.Normal = perVertexNormals[vert];
            }

            if (perVertexTangents != null)
            {
                vertex.Tangent = perVertexTangents[vert];
            }

            if (perVertexBinormals != null)
            {
                vertex.Binormal = perVertexBinormals[vert];
            }

            Vertices.Add(vertex);
        }

        HasNormals = perVertexNormals != null || NormalsInputIndex != -1;
        HasTangents = (perVertexTangents != null || TangentsInputIndex != -1)
            && (perVertexBinormals != null || BinormalsInputIndex != -1);
    }

    private void ImportColors()
    {
        ColorInputIndices.Clear();
        Colors = [];
        foreach (var input in Inputs)
        {
            if (input.semantic == "COLOR")
            {
                ColorInputIndices.Add((int)input.offset);

                if (input.source[0] != '#')
                    throw new ParsingException("Only ID references are supported for color input sources");

                ColladaSource inputSource = null;
                if (!Sources.TryGetValue(input.source.Substring(1), out inputSource))
                    throw new ParsingException("Color input source does not exist: " + input.source);

                List<Single> r = null, g = null, b = null;
                if (!inputSource.FloatParams.TryGetValue("R", out r) ||
                    !inputSource.FloatParams.TryGetValue("G", out g) ||
                    !inputSource.FloatParams.TryGetValue("B", out b))
                {
                    if (!inputSource.FloatParams.TryGetValue("X", out r) ||
                        !inputSource.FloatParams.TryGetValue("Y", out g) ||
                        !inputSource.FloatParams.TryGetValue("Z", out b))
                    {
                        throw new ParsingException("Color input source " + input.source + " must have R, G, B float attributes");
                    }
                }

                var colors = new List<Vector4>();
                Colors.Add(colors);
                for (var i = 0; i < r.Count; i++)
                {
                    colors.Add(new Vector4(r[i], g[i], b[i], 1.0f));
                }
            }
        }
    }

    private void ImportUVs()
    {
        bool flip = Options.FlipUVs;
        UVInputIndices.Clear();
        UVs = [];
        foreach (var input in Inputs)
        {
            if (input.semantic == "TEXCOORD")
            {
                UVInputIndices.Add((int)input.offset);

                if (input.source[0] != '#')
                    throw new ParsingException("Only ID references are supported for UV input sources");

                ColladaSource inputSource = null;
                if (!Sources.TryGetValue(input.source[1..], out inputSource))
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
        Sources = [];
        foreach (var source in Mesh.source)
        {
            var src = ColladaSource.FromCollada(source);
            Sources.Add(src.id, src);
        }
    }

    private VertexDescriptor FindVertexFormat(bool isSkinned)
    {
        var desc = new VertexDescriptor
        {
            PositionType = PositionType.Float3
        };
        if (isSkinned)
        {
            desc.HasBoneWeights = true;
        }

        foreach (var input in Mesh.vertices.input)
        {
            switch (input.semantic)
            {
                case "NORMAL": desc.NormalType = NormalType.Float3; break;
                case "TANGENT":
                case "TEXTANGENT": 
                    desc.TangentType = NormalType.Float3; break;
                case "BINORMAL":
                case "TEXBINORMAL":
                    desc.BinormalType = NormalType.Float3; break;
            }
        }

        foreach (var input in Inputs)
        {
            switch (input.semantic)
            {
                case "NORMAL": desc.NormalType = NormalType.Float3; break;
                case "TANGENT": 
                case "TEXTANGENT": 
                    desc.TangentType = NormalType.Float3; break;
                case "BINORMAL": 
                case "TEXBINORMAL": 
                    desc.BinormalType = NormalType.Float3; break;
                case "TEXCOORD":
                    desc.TextureCoordinateType = TextureCoordinateType.Float2;
                    desc.TextureCoordinates++;
                    break;
                case "COLOR":
                    desc.ColorMapType = ColorMapType.Float4;
                    desc.ColorMaps++;
                    break;
            }
        }

        return desc;
    }

    public void ImportFromCollada(mesh mesh, VertexDescriptor vertexFormat, bool isSkinned, ExporterOptions options)
    {
        Options = options;
        Mesh = mesh;
        ImportSources();
        ImportFaces();

        vertexFormat ??= FindVertexFormat(isSkinned);

        InputVertexType = vertexFormat;
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

        ImportVertices();

        // TODO: This should be done before deduplication!
        // TODO: Move this to somewhere else ... ?
        if (!HasNormals || Options.RecalculateNormals)
        {
            if (!HasNormals)
                Utils.Info(String.Format("Channel 'NORMAL' not found, will rebuild vertex normals after import."));

            HasNormals = true;
            OutputVertexType.NormalType = NormalType.Float3;
            computeNormals();
        }

        ImportColors();
        ImportUVs();

        if (UVInputIndices.Count > 0 || ColorInputIndices.Count > 0
            || NormalsInputIndex != -1 || TangentsInputIndex != -1 || BinormalsInputIndex != -1)
        {
            var outVertexIndices = new Dictionary<int[], int>(new VertexIndexComparer());
            ConsolidatedIndices = new List<int>(TriangleCount * 3);
            ConsolidatedVertices = new List<Vertex>(Vertices.Count);
            OriginalToConsolidatedVertexIndexMap = [];
            for (var vert = 0; vert < TriangleCount * 3; vert++)
            {
                var index = new int[InputOffsetCount];
                for (var i = 0; i < InputOffsetCount; i++)
                {
                    index[i] = Indices[vert * InputOffsetCount + i];
                }

                if (!outVertexIndices.TryGetValue(index, out int consolidatedIndex))
                {
                    var vertexIndex = index[VertexInputIndex];
                    consolidatedIndex = ConsolidatedVertices.Count;
                    Vertex vertex = Vertices[vertexIndex].Clone();
                    if (NormalsInputIndex != -1)
                    {
                        vertex.Normal = Normals[index[NormalsInputIndex]];
                    }
                    if (TangentsInputIndex != -1)
                    {
                        vertex.Tangent = Tangents[index[TangentsInputIndex]];
                    }
                    if (BinormalsInputIndex != -1)
                    {
                        vertex.Binormal = Binormals[index[BinormalsInputIndex]];
                    }
                    for (int uv = 0; uv < UVInputIndices.Count; uv++)
                    {
                        vertex.SetUV(uv, UVs[uv][index[UVInputIndices[uv]]]);
                    }
                    for (int color = 0; color < ColorInputIndices.Count; color++)
                    {
                        vertex.SetColor(color, Colors[color][index[ColorInputIndices[color]]]);
                    }
                    outVertexIndices.Add(index, consolidatedIndex);
                    ConsolidatedVertices.Add(vertex);

                    if (!OriginalToConsolidatedVertexIndexMap.TryGetValue(vertexIndex, out List<int> mappedIndices))
                    {
                        mappedIndices = [];
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
            Utils.Info(String.Format("Mesh has no separate normals, colors or UV map, vertex consolidation step skipped."));

            ConsolidatedVertices = Vertices;

            ConsolidatedIndices = new List<int>(TriangleCount * 3);
            for (var vert = 0; vert < TriangleCount * 3; vert++)
                ConsolidatedIndices.Add(VertexIndex(vert));

            OriginalToConsolidatedVertexIndexMap = [];
            for (var i = 0; i < Vertices.Count; i++)
                OriginalToConsolidatedVertexIndexMap.Add(i, [i]);
        }

        if ((InputVertexType.TangentType == NormalType.None 
            || InputVertexType.BinormalType == NormalType.None)
            && ((!HasTangents && UVs.Count > 0) || Options.RecalculateTangents))
        {
            if (!HasTangents)
                Utils.Info(String.Format("Channel 'TANGENT'/'BINROMAL' not found, will rebuild vertex tangents after import."));

            OutputVertexType.TangentType = NormalType.Float3;
            OutputVertexType.BinormalType = NormalType.Float3;
            HasTangents = true;
            computeTangents();
        }

        // Use optimized tangent, texture map and color map format when exporting for D:OS 2
        if ((Options.ModelInfoFormat == DivinityModelInfoFormat.LSMv0
            || Options.ModelInfoFormat == DivinityModelInfoFormat.LSMv1
            || Options.ModelInfoFormat == DivinityModelInfoFormat.LSMv3)
            && Options.EnableQTangents
            && HasNormals 
            && HasTangents)
        {
            OutputVertexType.NormalType = NormalType.QTangent;
            OutputVertexType.TangentType = NormalType.QTangent;
            OutputVertexType.BinormalType = NormalType.QTangent;

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
