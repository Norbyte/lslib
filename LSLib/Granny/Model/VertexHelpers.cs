using OpenTK.Mathematics;

namespace LSLib.Granny.Model;

public class VertexHelpers
{
    public static void CompressBoneWeights(Span<float> weights, Span<byte> compressedWeights)
    {
        Span<float> errors = stackalloc float[weights.Length];

        var influenceCount = weights.Length;
        float influenceSum = 0.0f;
        foreach (var w in weights)
        {
            influenceSum += w;
        }

        ushort totalEncoded = 0;
        for (var i = 0; i < influenceCount; i++)
        {
            var weight = weights[i] / influenceSum * 255.0f;
            var encodedWeight = (byte)Math.Round(weight);
            totalEncoded += encodedWeight;
            errors[i] = encodedWeight - weight;
            compressedWeights[i] = encodedWeight;
        }

        while (totalEncoded != 0 && totalEncoded != 255)
        {
            int errorIndex = 0;
            if (totalEncoded < 255)
            {
                for (var i = 1; i < influenceCount; i++)
                {
                    if (errors[i] < errors[errorIndex])
                    {
                        errorIndex = i;
                    }
                }

                compressedWeights[errorIndex]++;
                errors[errorIndex]++;
                totalEncoded++;
            }
            else
            {
                for (var i = 1; i < influenceCount; i++)
                {
                    if (errors[i] > errors[errorIndex])
                    {
                        errorIndex = i;
                    }
                }

                compressedWeights[errorIndex]--;
                errors[errorIndex]--;
                totalEncoded--;
            }
        }
    }
    
    public static void ComputeTangents(IList<Vertex> vertices, IList<int> indices, bool ignoreNaNUV)
    {
        // Check if the vertex format has at least one UV set
        if (vertices.Count > 0)
        {
            var v = vertices[0];
            if (v.Format.TextureCoordinates == 0)
            {
                throw new InvalidOperationException("At least one UV set is required to recompute tangents");
            }
        }

        foreach (var v in vertices)
        {
            v.Tangent = Vector3.Zero;
            v.Binormal = Vector3.Zero;
        }

        for (int i = 0; i < indices.Count/3; i++)
        {
            var i1 = indices[i * 3 + 0];
            var i2 = indices[i * 3 + 1];
            var i3 = indices[i * 3 + 2];

            var vert1 = vertices[i1];
            var vert2 = vertices[i2];
            var vert3 = vertices[i3];

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

            if ((Single.IsNaN(r) || Single.IsInfinity(r)) && !ignoreNaNUV)
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

        foreach (var v in vertices)
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

    public static Vector3 TriangleNormalFromVertex(IList<Vertex> vertices, IList<int> indices, int vertexIndex)
    {
        // This assumes that A->B->C is a counter-clockwise ordering
        var a = vertices[indices[vertexIndex]].Position;
        var b = vertices[indices[(vertexIndex + 1) % 3]].Position;
        var c = vertices[indices[(vertexIndex + 2) % 3]].Position;

        var N = Vector3.Cross(b - a, c - a);
        float sin_alpha = N.Length / ((b - a).Length * (c - a).Length);
        return N.Normalized() * (float)Math.Asin(sin_alpha);
    }

    public static void ComputeNormals(IList<Vertex> vertices, IList<int> indices)
    {
        for (var vertexIdx = 0; vertexIdx < vertices.Count; vertexIdx++)
        {
            Vector3 N = new(0, 0, 0);
            var numIndices = indices.Count;
            for (int triVertIdx = 0; triVertIdx < numIndices; triVertIdx++)
            {
                if (indices[triVertIdx] == vertexIdx)
                {
                    int baseIdx = ((int)(triVertIdx / 3)) * 3;
                    N += TriangleNormalFromVertex(vertices, indices, baseIdx);
                }
            }

            N.Normalize();
            vertices[vertexIdx].Normal = N;
        }
    }

    class OBB
    {
        public Vector3 Min, Max;
        public int NumVerts;
    }

    public static void UpdateOBBs(Skeleton skeleton, Mesh mesh)
    {
        if (mesh.BoneBindings == null || mesh.BoneBindings.Count == 0) return;

        var obbs = new List<OBB>(mesh.BoneBindings.Count);
        for (var i = 0; i < mesh.BoneBindings.Count; i++)
        {
            obbs.Add(new OBB
            {
                Min = new Vector3(1000.0f, 1000.0f, 1000.0f),
                Max = new Vector3(-1000.0f, -1000.0f, -1000.0f),
                NumVerts = 0
            });
        }

        foreach (var vert in mesh.PrimaryVertexData.Vertices)
        {
            for (var i = 0; i < Vertex.MaxBoneInfluences; i++)
            {
                if (vert.BoneWeights[i] > 0)
                {
                    var bi = vert.BoneIndices[i];
                    var obb = obbs[bi];
                    obb.NumVerts++;

                    var bone = skeleton.GetBoneByName(mesh.BoneBindings[bi].BoneName);
                    var invWorldTransform = ColladaHelpers.FloatsToMatrix(bone.InverseWorldTransform);
                    var transformed = Vector3.TransformPosition(vert.Position, invWorldTransform);

                    obb.Min.X = Math.Min(obb.Min.X, transformed.X);
                    obb.Min.Y = Math.Min(obb.Min.Y, transformed.Y);
                    obb.Min.Z = Math.Min(obb.Min.Z, transformed.Z);

                    obb.Max.X = Math.Max(obb.Max.X, transformed.X);
                    obb.Max.Y = Math.Max(obb.Max.Y, transformed.Y);
                    obb.Max.Z = Math.Max(obb.Max.Z, transformed.Z);
                }
            }
        }

        for (var i = 0; i < obbs.Count; i++)
        {
            var obb = obbs[i];
            if (obb.NumVerts > 0)
            {
                mesh.BoneBindings[i].OBBMin = [obb.Min.X, obb.Min.Y, obb.Min.Z];
                mesh.BoneBindings[i].OBBMax = [obb.Max.X, obb.Max.Y, obb.Max.Z];
            }
            else
            {
                mesh.BoneBindings[i].OBBMin = [0.0f, 0.0f, 0.0f];
                mesh.BoneBindings[i].OBBMax = [0.0f, 0.0f, 0.0f];
            }
        }
    }
}
