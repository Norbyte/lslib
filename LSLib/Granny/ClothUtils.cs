using LSLib.Granny.Model;
using LSLib.LS;
using OpenTK.Mathematics;
using Supercluster.KDTree;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace LSLib.Granny
{
    public class ClothUtils
    {
        // Tested this by incrementally moving an overlapping vertex until the cloth physics broke in-game. Repeated on all 3 dimensions
        // to make sure it wasn't connected to precision; all 3 stopped working immediately above this value. On X and Z axes, the actual
        // increments could be close but not over 0.0000001 (e.g. 0.00000008940697), while on the Y axis it could only be 0 (not broken) or 0.00000011920929 (broken).
        private const double OverlappingVertexSearchRadius = 0.0000001f;

        public static string Serialize(Triplet[] triplets)
        {
            using var target = new MemoryStream();

            using (var zlibStream = new ZLibStream(target, CompressionLevel.SmallestSize))
            using (var writer = new BinaryWriter(zlibStream))
            {
                BinUtils.WriteStructs(writer, triplets);
            }

            return Convert.ToBase64String(target.ToArray());
        }

        public static Triplet[] Deserialize(string str)
        {
            var compressedData = Convert.FromBase64String(str);
            using var target = new MemoryStream();

            using (var source = new MemoryStream(compressedData))
            using (var zlibStream = new ZLibStream(source, CompressionMode.Decompress))
            {
                zlibStream.CopyTo(target);
            }

            Triplet[] triplets = new Triplet[target.Length / 6];
            target.Position = 0;

            using var reader = new BinaryReader(target);
            BinUtils.ReadStructs(reader, triplets);
            
            return triplets;
        }

        public static Triplet[] Generate(Mesh physicsMesh, Mesh targetMesh)
        {
            Debug.WriteLine($"Generate Start");
            Stopwatch stopwatch = Stopwatch.StartNew();

            var physicsClothMesh = ClothMesh.Build(physicsMesh);
            Debug.WriteLine($"Build Physics Mesh {stopwatch.Elapsed}");

            var targetClothMesh = ClothMesh.Build(targetMesh);
            Debug.WriteLine($"Build Target Mesh {stopwatch.Elapsed}");

            var physicsClothVertices = GetPhysicsClothVertices(physicsClothMesh);
            Debug.WriteLine($"GetPhysicsClothVertices {stopwatch.Elapsed}");

            var targetClothVertices = GetTargetClothVertices(targetClothMesh);
            Debug.WriteLine($"GetTargetClothVertices {stopwatch.Elapsed}");

            if (physicsClothVertices.Length == 0 || targetClothVertices.Length == 0)
            {
                return [];
            }

            var kdTree = BuildKdTree(physicsClothVertices);
            Debug.WriteLine($"BuildKdTree {stopwatch.Elapsed}");

            var triplets = new Triplet[targetClothVertices.Length];

            Parallel.For(0, targetClothVertices.Length, (index) =>
            {
                var vertex = targetClothVertices[index];
                Span<(short PhysicsIndex, float Distance)> triplet = stackalloc (short, float)[3];
                var tripletIndex = 0;

                // TODO: Searching the whole tree every time is slow. Maybe do RadialSearch? Hard to tell if there was a limit used by the game devs.
                // Looking at a flame graph, most of the time seems to be spent internally allocating small arrays.
                foreach (var physicsVertex in kdTree.NearestNeighbors(vertex.PositionAsArray, kdTree.Count).Select(t => t.Item2))
                {
                    if (vertex.Mask != 0 && (physicsVertex.Mask & vertex.Mask) == 0)
                    {
                        continue;
                    }

                    var distance = (vertex.Position - physicsVertex.Position).Length;

                    // TODO: this doesn't make a whole lot of sense but gets us very close to what the game does
                    if (tripletIndex == 1 && distance > triplet[0].Distance * 2.875)
                    {
                        break;
                    }
                    else if (tripletIndex == 2 && (distance > (triplet[0].Distance + triplet[1].Distance) || distance > triplet[0].Distance * 2.8))
                    {
                        break;
                    }

                    triplet[tripletIndex++] = (physicsVertex.PhysicsIndex, distance);

                    if (tripletIndex == 3)
                    {
                        break;
                    }
                }

                while (tripletIndex < 3)
                {
                    triplet[tripletIndex++] = (-1, -1);
                }

                triplets[index] = new Triplet(triplet[0].PhysicsIndex, triplet[1].PhysicsIndex, triplet[2].PhysicsIndex);
            });

            Debug.WriteLine($"Generate End {stopwatch.Elapsed}");

            return triplets;
        }

        private static KDTree<float, T> BuildKdTree<T>(T[] vertices)
            where T : BaseVertex
        {
            // TODO: This kinda sucks. It'd be nice if the KD tree could use the position property directly. Might be worth modifying the package?
            var points = new float[vertices.Length][];

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = vertices[i].PositionAsArray;
            }

            // don't use Math.Sqrt and Math.Pow for the metric since they're relatively slow and this'll be called thousands of times (square distance is fine here)
            return new KDTree<float, T>(3, points, vertices, (a, b) => (b[0] - a[0]) * (b[0] - a[0]) + (b[1] - a[1]) * (b[1] - a[1]) + (b[2] - a[2]) * (b[2] - a[2]));
        }

        private static PhysicsVertex[] GetPhysicsClothVertices(ClothMesh mesh)
        {
            var markedVertices = GetClothVertices(mesh);
            var encountered = new HashSet<Vector3>();
            var physicsVertices = new List<PhysicsVertex>(markedVertices.Count);

            for (int i = 0; i < mesh.Indices.Count / 3; i++)
            {
                ClothVertex v1 = mesh.Vertices[mesh.Indices[i * 3]];
                ClothVertex v2 = mesh.Vertices[mesh.Indices[i * 3 + 1]];
                ClothVertex v3 = mesh.Vertices[mesh.Indices[i * 3 + 2]];

                // skip the triangle if it has a non-cloth vertex
                if (!markedVertices.Contains(v1) || !markedVertices.Contains(v2) || !markedVertices.Contains(v3))
                {
                    continue;
                }

                for (int j = 0; j < 3; j++)
                {
                    ClothVertex vertex = mesh.Vertices[mesh.Indices[i * 3 + j]];

                    // overlapping vertices should all map to the first encountered
                    // TODO: investigate whether this also requires looking around with OverlappingVertexSearchRadius (or some other radius)
                    if (encountered.Contains(vertex.Position))
                    {
                        continue;
                    }

                    physicsVertices.Add(new PhysicsVertex((short)physicsVertices.Count, vertex));
                    encountered.Add(vertex.Position);
                }
            }

            return [.. physicsVertices];
        }

        private static ClothVertex[] GetTargetClothVertices(ClothMesh mesh)
        {
            ClothVertex[] clothVertices = [.. GetClothVertices(mesh).OrderBy(v => v.Index)];
            var packedVertices = new ClothVertex[clothVertices.Length];

            var i = 0;

            for (; i < clothVertices.Length && clothVertices[i].Index < clothVertices.Length; i++)
            {
                var vertex = clothVertices[i];
                packedVertices[vertex.Index] = vertex;
            }

            var current = 0;

            for (; i < clothVertices.Length; i++)
            {
                while (packedVertices[current] != null)
                {
                    current++;
                }

                packedVertices[current++] = clothVertices[i];
            }

            return packedVertices;
        }

        private static HashSet<ClothVertex> GetClothVertices(ClothMesh mesh)
        {
            var clothVertices = new HashSet<ClothVertex>(mesh.Vertices.Where(v => v.Weight > 0));

            AddOverlappingVertices(clothVertices);
            AddNeighboringVertices(clothVertices);
            AddOverlappingVertices(clothVertices);
            AddNeighboringVertices(clothVertices);
            AddOverlappingVertices(clothVertices);

            return clothVertices;
        }

        private static void AddOverlappingVertices(HashSet<ClothVertex> clothVertices)
        {
            foreach (var vertex in clothVertices.SelectMany(v => v.Overlapping).ToList())
            {
                clothVertices.Add(vertex);
            }
        }

        private static void AddNeighboringVertices(HashSet<ClothVertex> clothVertices)
        {
            foreach (var vertex in clothVertices.SelectMany(v => v.Neighbors).ToList())
            {
                clothVertices.Add(vertex);
            }
        }

        public readonly struct Triplet(short a, short b, short c) : IEquatable<Triplet>
        {
            public short A => a;

            public short B => b;

            public short C => c;

            public static bool operator ==(Triplet x, Triplet y) => x.Equals(y);

            public static bool operator !=(Triplet x, Triplet y) => !x.Equals(y);

            public override bool Equals([NotNullWhen(true)] object obj) => obj is Triplet triplet && Equals(triplet);

            public override int GetHashCode() => HashCode.Combine(A, B, C);

            public override string ToString() => $"({A}, {B}, {C})";

            public bool Equals(Triplet other) => A == other.A && B == other.B && C == other.C;
        }

        private record ClothMesh
        {
            private ClothMesh(IReadOnlyList<ClothVertex> vertices, IReadOnlyList<int> indices)
            {
                Vertices = vertices;
                Indices = indices;
            }

            internal IReadOnlyList<ClothVertex> Vertices { get; }

            internal IReadOnlyList<int> Indices { get; }

            internal static ClothMesh Build(Mesh mesh)
            {
                var vertices = new ClothVertex[mesh.PrimaryVertexData.Vertices.Count];

                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = new ClothVertex(i, mesh.PrimaryVertexData.Vertices[i]);
                }

                var kdTree = BuildKdTree(vertices);

                Parallel.ForEach(vertices, (vertex) =>
                {
                    // searchRadius is squared because our tree uses the square distance as its metric
                    var overlapping = kdTree.RadialSearch(vertex.PositionAsArray, OverlappingVertexSearchRadius * OverlappingVertexSearchRadius);

                    // nothing to do if there are no overlapping vertices (we just found the vertex itself)
                    if (overlapping.Length == 1)
                    {
                        return;
                    }

                    // overlapping vertices take the weight of the first vertex by index
                    vertex.Weight = overlapping.MinBy(v => v.Item2.Index).Item2.Weight;
                    vertex.Overlapping.AddRange(overlapping.Select(v => v.Item2).Where(v => v != vertex));
                });

                var indices = mesh.PrimaryTopology.Indices;

                for (var i = 0; i < indices.Count / 3; i++)
                {
                    for (var j = 0; j < 3; j++)
                    {
                        var vertex = vertices[indices[i * 3 + j]];
                        vertex.Neighbors.Add(vertices[indices[i * 3 + ((j + 1) % 3)]]);
                        vertex.Neighbors.Add(vertices[indices[i * 3 + ((j + 2) % 3)]]);
                    }
                }

                return new ClothMesh(vertices, indices);
            }
        }

        private record BaseVertex
        {
            protected BaseVertex(Vector3 position)
            {
                Position = position;
                PositionAsArray = [position.X, position.Y, position.Z];
            }

            internal Vector3 Position { get; }

            internal float[] PositionAsArray { get; }
        }

        private record ClothVertex : BaseVertex
        {
            internal int Index { get; }

            internal byte Weight { get; set; }

            internal byte Mask { get; }

            internal List<ClothVertex> Neighbors { get; } = [];

            internal List<ClothVertex> Overlapping { get; } = [];

            // byte conversion must be the same as VertexSerialization.WriteNormalByteVector4 or things will break!
            internal ClothVertex(int index, Vertex vertex)
                : this(index, vertex.Position, (byte)(vertex.Color0.X * 255), (byte)(vertex.Color0.Z * 255))
            {
            }

            internal ClothVertex(int index, Vector3 position, byte weight, byte mask)
                : base(position)
            {
                Index = index;
                Weight = weight;
                Mask = mask;
            }
        }

        private record PhysicsVertex : BaseVertex
        {
            internal short PhysicsIndex { get; }

            internal byte Mask { get; }

            internal PhysicsVertex(short physicsIndex, ClothVertex clothVertex)
                : this(physicsIndex, clothVertex.Position, clothVertex.Mask)
            {
            }

            internal PhysicsVertex(short physicsIndex, Vector3 position, byte mask)
                : base(position)
            {
                PhysicsIndex = physicsIndex;
                Mask = mask;
            }
        }
    }
}
