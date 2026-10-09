using System.Numerics;
using PhysXTool;

namespace Tests;

public static class PhysXColliderTests
{
    [Fact]
    public static void GetBoxSupport_AlignedPositiveDirection_ReturnsCorrectExtents()
    {
        var extents = new Vector3(2.0f, 4.0f, 6.0f);
        var box = new PxBoxGeometry(extents);
        var transform = new PxTransform(new Vector3(10.0f, 10.0f, 10.0f), Quaternion.Identity);
        var direction = new Vector3(1.0f, 0.0f, 0.0f);
        Vector3 support = PhysXCollider.GetBoxSupport(box, transform, direction);
        Assert.Equal(12.0f, support.X);
        Assert.Equal(14.0f, support.Y);
        Assert.Equal(16.0f, support.Z);
    }

    [Fact]
    public static void GetBoxSupport_AlignedNegativeDirection_ReturnsFlippedExtents()
    {
        var extents = new Vector3(3.0f, 3.0f, 3.0f);
        var box = new PxBoxGeometry(extents);
        var transform = new PxTransform(Vector3.Zero, Quaternion.Identity);
        var direction = new Vector3(-1.0f, -1.0f, -1.0f);
        Vector3 support = PhysXCollider.GetBoxSupport(box, transform, direction);
        Assert.Equal(-3.0f, support.X);
        Assert.Equal(-3.0f, support.Y);
        Assert.Equal(-3.0f, support.Z);
    }

    [Fact]
    public static void GetSphereSupport_ValidDirection_ReturnsPointOnRadius()
    {
        var sphere = new PxSphereGeometry(5.0f);
        var transform = new PxTransform(new Vector3(1.0f, 2.0f, 3.0f), Quaternion.Identity);
        var direction = new Vector3(0.0f, 2.0f, 0.0f); 
        Vector3 support = PhysXCollider.GetSphereSupport(sphere, transform, direction);
        Assert.Equal(1.0f, support.X);
        Assert.Equal(7.0f, support.Y);
        Assert.Equal(3.0f, support.Z);
    }

    [Fact]
    public static void GetSphereSupport_ZeroDirection_FallsBackToUnitX()
    {
        var sphere = new PxSphereGeometry(2.5f);
        var transform = new PxTransform(Vector3.Zero, Quaternion.Identity);
        Vector3 direction = Vector3.Zero;
        Vector3 support = PhysXCollider.GetSphereSupport(sphere, transform, direction);
        Assert.Equal(2.5f, support.X);
        Assert.Equal(0.0f, support.Y);
        Assert.Equal(0.0f, support.Z);
    }

    [Fact]
    public static void EvaluateGjk_IdenticalCoincidentCenters_ResolvesWithoutDeadlock()
    {
        var boxA = new PxBoxGeometry(Vector3.One);
        var transformA = new PxTransform(Vector3.Zero, Quaternion.Identity);
        var boxB = new PxBoxGeometry(Vector3.One);
        var transformB = new PxTransform(Vector3.Zero, Quaternion.Identity);

        bool result = PhysXCollider.EvaluateGjk(
            boxA, transformA,
            boxB, transformB,
            out _, out _);

        Assert.True(result);
    }
}
