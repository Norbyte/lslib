using System.Numerics;
using System.Runtime.CompilerServices;

namespace PhysXTool;

public static class PhysXCollider
{
    private const float GjkEpsilon = 1e-6f;
    private const int GjkMaxIterations = 32;
    private const int EpaMaxVertices = 64;
    private const int EpaMaxFaces = 128;
    private const int EpaMaxIterations = 32;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetBoxSupport(in PxBoxGeometry box, in PxTransform transform, Vector3 direction)
    {
        var rotationInverse = Quaternion.Inverse(transform.Rotation);
        var localDir = Vector3.Transform(direction, rotationInverse);
        
        var localSupport = new Vector3(
            localDir.X >= 0.0f ? box.HalfExtents.X : -box.HalfExtents.X,
            localDir.Y >= 0.0f ? box.HalfExtents.Y : -box.HalfExtents.Y,
            localDir.Z >= 0.0f ? box.HalfExtents.Z : -box.HalfExtents.Z
        );

        return Vector3.Transform(localSupport, transform.Rotation) + transform.Position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetSphereSupport(in PxSphereGeometry sphere, in PxTransform transform, Vector3 direction)
    {
        float dirLength = direction.Length();
        Vector3 normDir = dirLength > GjkEpsilon ? direction / dirLength : Vector3.UnitX;
        
        return transform.Position + (normDir * sphere.Radius);
    }

    public static bool EvaluateGjk(
        in PxBoxGeometry shapeA, in PxTransform transformA,
        in PxBoxGeometry shapeB, in PxTransform transformB,
        out Vector3 penetrationNormal, out float penetrationDepth)
    {
        penetrationNormal = Vector3.Zero;
        penetrationDepth = 0.0f;
        Span<Vector3> simplex = stackalloc Vector3[4];
        var simplexCount = 0;

        Vector3 direction = transformB.Position - transformA.Position;
        if (direction.LengthSquared() < GjkEpsilon)
        {
            direction = Vector3.UnitX;
        }

        for (var i = 0; i < GjkMaxIterations; i++)
        {
            Vector3 supportA = GetBoxSupport(shapeA, transformA, direction);
            Vector3 supportB = GetBoxSupport(shapeB, transformB, -direction);
            Vector3 newVertex = supportA - supportB;

            if (Vector3.Dot(newVertex, direction) < 0.0f)
            {
                return false; 
            }

            if (simplexCount >= 4)
            {
                return false; 
            }

            simplex[simplexCount++] = newVertex;

            if (UpdateSimplexAndDirection(simplex, ref simplexCount, ref direction))
            {
                return EvaluateEpa(simplex, shapeA, transformA, shapeB, transformB, out penetrationNormal, out penetrationDepth);
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool UpdateSimplexAndDirection(Span<Vector3> simplex, ref int count, ref Vector3 direction)
    {
        if (count == 1)
        {
            direction = -simplex[0];
            return false;
        }

        if (count == 2)
        {
            Vector3 ab = simplex[1] - simplex[0];
            Vector3 ao = -simplex[0];
            direction = Vector3.Cross(Vector3.Cross(ab, ao), ab);
            return false;
        }

        if (count == 3)
        {
            Vector3 a = simplex[2];
            Vector3 b = simplex[1];
            Vector3 c = simplex[0];
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            var abc = Vector3.Cross(ab, ac);
            Vector3 ao = -a;

            if (Vector3.Dot(Vector3.Cross(abc, ac), ao) > 0.0f)
            {
                simplex[1] = c;
                count = 2;
                direction = Vector3.Cross(Vector3.Cross(ac, ao), ac);
                return false;
            }

            if (Vector3.Dot(Vector3.Cross(ab, abc), ao) > 0.0f)
            {
                count = 2;
                direction = Vector3.Cross(Vector3.Cross(ab, ao), ab);
                return false;
            }

            direction = Vector3.Dot(abc, ao) > 0.0f ? abc : -abc;
            return false;
        }

        if (count == 4)
        {
            Vector3 a = simplex[3];
            Vector3 b = simplex[2];
            Vector3 c = simplex[1];
            Vector3 d = simplex[0];

            var abc = Vector3.Cross(b - a, c - a);
            var acd = Vector3.Cross(c - a, d - a);
            var adb = Vector3.Cross(d - a, b - a);
            Vector3 ao = -a;

            if (Vector3.Dot(abc, ao) > 0.0f)
            {
                simplex[0] = c;
                simplex[1] = b;
                simplex[2] = a;
                count = 3;
                direction = abc;
                return false;
            }

            if (Vector3.Dot(acd, ao) > 0.0f)
            {
                simplex[0] = d;
                simplex[1] = c;
                simplex[2] = a;
                count = 3;
                direction = acd;
                return false;
            }

            if (Vector3.Dot(adb, ao) > 0.0f)
            {
                simplex[0] = b;
                simplex[1] = d;
                simplex[2] = a;
                count = 3;
                direction = adb;
                return false;
            }

            return true;
        }

        return false;
    }

    private static bool EvaluateEpa(
        ReadOnlySpan<Vector3> gjkSimplex,
        in PxBoxGeometry shapeA, in PxTransform transformA,
        in PxBoxGeometry shapeB, in PxTransform transformB,
        out Vector3 penetrationNormal, out float penetrationDepth)
    {
        penetrationNormal = Vector3.UnitX;
        penetrationDepth = 0.0f;

        Span<Vector3> polytopeVertices = stackalloc Vector3[EpaMaxVertices];
        Span<int> polytopeFaces = stackalloc int[EpaMaxFaces * 3];
        
        for (var i = 0; i < 4; i++)
        {
            polytopeVertices[i] = gjkSimplex[i];
        }

        polytopeFaces[0] = 0;
        polytopeFaces[1] = 1;
        polytopeFaces[2] = 2;

        polytopeFaces[3] = 0;
        polytopeFaces[4] = 2;
        polytopeFaces[5] = 3;

        polytopeFaces[6] = 0;
        polytopeFaces[7] = 3;
        polytopeFaces[8] = 1;

        polytopeFaces[9] = 1;
        polytopeFaces[10] = 3;
        polytopeFaces[11] = 2;

        var vertexCount = 4;
        const int faceCount = 4;

        for (var iter = 0; iter < EpaMaxIterations; iter++)
        {
            var closestFaceIndex = 0;
            float minDistance = float.MaxValue;
            Vector3 minNormal = Vector3.Zero;

            for (var f = 0; f < faceCount; f++)
            {
                Vector3 a = polytopeVertices[polytopeFaces[f * 3]];
                Vector3 b = polytopeVertices[polytopeFaces[f * 3 + 1]];
                Vector3 c = polytopeVertices[polytopeFaces[f * 3 + 2]];

                var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
                float distance = Vector3.Dot(normal, a);

                if (distance < 0.0f)
                {
                    distance = -distance;
                    normal = -normal;
                }

                if (distance < minDistance)
                {
                    minDistance = distance;
                    minNormal = normal;
                    closestFaceIndex = f;
                }
            }

            Vector3 supportA = GetBoxSupport(shapeA, transformA, minNormal);
            Vector3 supportB = GetBoxSupport(shapeB, transformB, -minNormal);
            Vector3 newVertex = supportA - supportB;

            float supportingDistance = Vector3.Dot(minNormal, newVertex);
            if (supportingDistance - minDistance < GjkEpsilon)
            {
                penetrationNormal = minNormal;
                penetrationDepth = minDistance;
                return true;
            }

            if (vertexCount >= EpaMaxVertices)
            {
                break;
            }

            polytopeVertices[vertexCount++] = newVertex;
            polytopeFaces[closestFaceIndex * 3] = closestFaceIndex; 
        }

        return false;
    }
}
