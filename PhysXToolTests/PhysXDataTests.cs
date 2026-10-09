using System.Numerics;
using PhysXTool;

namespace Tests;

public static class PhysXDataTests
{
    [Theory]
    [InlineData(-10.5f, 0.0f)]
    [InlineData(0.0f, 0.0f)]
    [InlineData(5.75f, 5.75f)]
    public static void PxMaterial_StaticFriction_UsesFieldKeywordAndClampsNegativeToZero(float input, float expected)
    {
        var material = new PxMaterial { StaticFriction = input };
        Assert.Equal(expected, material.StaticFriction);
    }

    [Theory]
    [InlineData(-1.0f, 0.0f)]
    [InlineData(0.0f, 0.0f)]
    [InlineData(123.4f, 123.4f)]
    public static void PxMaterial_DynamicFriction_UsesFieldKeywordAndClampsNegativeToZero(float input, float expected)
    {
        var material = new PxMaterial { DynamicFriction = input };
        Assert.Equal(expected, material.DynamicFriction);
    }

    [Theory]
    [InlineData(-0.5f, 0.0f)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(1.5f, 1.0f)]
    public static void PxMaterial_Restitution_UsesFieldKeywordAndClampsToUnitRange(float input, float expected)
    {
        var material = new PxMaterial { Restitution = input };
        Assert.Equal(expected, material.Restitution);
    }

    [Fact]
    public static void PxMaterial_LoadState_ReturnsCorrectValue()
    {
        var material = new PxMaterial();
        Assert.Equal(LoadingState.Material, material.LoadState);
    }

    [Fact]
    public static void PxSphereGeometry_Initialization_AssignsCorrectProperties()
    {
        const float expectedRadius = 5.25f;
        var geometry = new PxSphereGeometry(expectedRadius);

        Assert.Equal(PxGeometryType.Sphere, geometry.GeometryType);
        Assert.Equal(expectedRadius, geometry.Radius);
    }

    [Fact]
    public static void PxCapsuleGeometry_Initialization_AssignsCorrectProperties()
    {
        const float expectedRadius = 2.0f;
        const float expectedHalfHeight = 4.5f;
        var geometry = new PxCapsuleGeometry(expectedRadius, expectedHalfHeight);

        Assert.Equal(PxGeometryType.Capsule, geometry.GeometryType);
        Assert.Equal(expectedRadius, geometry.Radius);
        Assert.Equal(expectedHalfHeight, geometry.HalfHeight);
    }

    [Fact]
    public static void PxBoxGeometry_Initialization_AssignsCorrectProperties()
    {
        var extents = new Vector3(1.0f, 2.0f, 3.0f);
        var geometry = new PxBoxGeometry(extents);

        Assert.Equal(PxGeometryType.Box, geometry.GeometryType);
        Assert.Equal(extents.X, geometry.HalfExtents.X);
        Assert.Equal(extents.Y, geometry.HalfExtents.Y);
        Assert.Equal(extents.Z, geometry.HalfExtents.Z);
    }

    [Fact]
    public static void PxConvexMeshGeometry_Initialization_AssignsCorrectProperties()
    {
        var mesh = new PxConvexMesh();
        var scale = new PxMeshScale { Scale = Vector3.One, Rotation = Quaternion.Identity };
        var geometry = new PxConvexMeshGeometry(mesh, scale);

        Assert.Equal(PxGeometryType.ConvexMesh, geometry.GeometryType);
        Assert.Same(mesh, geometry.ConvexMesh);
        Assert.Equal(scale.Scale, geometry.Scale.Scale);
    }

    [Fact]
    public static void PxTriangleMeshGeometry_Initialization_AssignsCorrectProperties()
    {
        var mesh = new PxTriangleMesh();
        var scale = new PxMeshScale { Scale = Vector3.One, Rotation = Quaternion.Identity };
        var geometry = new PxTriangleMeshGeometry(mesh, scale);

        Assert.Equal(PxGeometryType.TriangleMesh, geometry.GeometryType);
        Assert.Same(mesh, geometry.TriangleMesh);
        Assert.Equal(scale.Scale, geometry.Scale.Scale);
    }

    [Fact]
    public static void PxShape_LocalPose_ByRefModification_DoesNotAllocate()
    {
        var shape = new PxShape
        {
            Name = "ByRefShape",
            ContactOffset = 0.05f
        };
        var targetPosition = new Vector3(10f, 20f, 30f);
        ref PxTransform pose = ref shape.LocalPose;
        pose.Position = targetPosition;
        pose.Rotation = Quaternion.Identity;
        Assert.Equal("ByRefShape", shape.Name);
        Assert.Equal(LoadingState.Shape, shape.LoadState);
        Assert.Equal(0.05f, shape.ContactOffset);
        Assert.Equal(targetPosition, shape.LocalPose.Position);
    }

    [Fact]
    public static void PxD6Joint_ByRefLimitsAndDrives_ModifyStateCorrectly()
    {
        var joint = new PxD6Joint("D6Joint");
        ref PxJointLinearLimit distanceLimit = ref joint.DistanceLimit;
        distanceLimit.Value = 42.0f;
        distanceLimit.Stiffness = 500.0f;

        ref PxD6JointDrive driveX = ref joint.DriveX;
        driveX.ForceLimit = 100.0f;
        driveX.IsAcceleration = true;

        Assert.Equal("D6Joint", joint.Name);
        Assert.Equal(LoadingState.D6Joint, joint.LoadState);
        Assert.Equal(42.0f, joint.DistanceLimit.Value);
        Assert.Equal(500.0f, joint.DistanceLimit.Stiffness);
        Assert.Equal(100.0f, joint.DriveX.ForceLimit);
        Assert.True(joint.DriveX.IsAcceleration);
    }

    [Fact]
    public static void PxRigidStatic_DefaultValues_InitializedCorrectlyWithoutBoxing()
    {
        var rigidStatic = new PxRigidStatic("StaticMesh")
        {
            ActorFlags = PxActors.DisableGravity,
            DominanceGroup = 2
        };

        Assert.Equal("StaticMesh", rigidStatic.Name);
        Assert.Equal(LoadingState.RigidStatic, rigidStatic.LoadState);
        Assert.Equal(PxActors.DisableGravity, rigidStatic.ActorFlags);
        Assert.Equal(2u, rigidStatic.DominanceGroup);
        Assert.True(rigidStatic.Shapes.IsEmpty); 
    }

    [Fact]
    public static void PxRigidDynamic_DefaultValues_InitializedCorrectly()
    {
        var dynamicActor = new PxRigidDynamic("DynamicBox");

        Assert.Equal("DynamicBox", dynamicActor.Name);
        Assert.Equal(LoadingState.RigidDynamic, dynamicActor.LoadState);
        Assert.Equal(1.0f, dynamicActor.Mass);
        Assert.Equal(Vector3.One, dynamicActor.MassSpaceInertiaTensor);
        Assert.Equal(4u, dynamicActor.MinPositionIterations);
        Assert.Equal(1u, dynamicActor.MinVelocityIterations);
        Assert.Equal(0.005f, dynamicActor.SleepThreshold);
        Assert.True(dynamicActor.Shapes.IsEmpty);
    }

    [Fact]
    public static void PxArticulationJoint_DefaultValues_AreCorrectlyAssigned()
    {
        var joint = new PxArticulationJoint
        {
            Stiffness = 15.0f,
            Damping = 3.5f
        };

        Assert.Equal(LoadingState.ArticulationJoint, joint.LoadState);
        Assert.Equal(15.0f, joint.Stiffness);
        Assert.Equal(3.5f, joint.Damping);
        Assert.Equal(MathF.PI / 4.0f, joint.SwingLimitZ);
        Assert.Equal(-MathF.PI / 4.0f, joint.TwistLimitLower);
        Assert.False(joint.SwingLimitEnabled);
        Assert.False(joint.TwistLimitEnabled);
    }

    [Fact]
    public static void PxArticulationLink_And_Articulation_TreeInitializations()
    {
        var articulation = new PxArticulation();
        var link = new PxArticulationLink("BaseLink")
        {
            InboundJointDof = 6
        };

        Assert.Equal(LoadingState.Articulation, articulation.LoadState);
        Assert.Equal(0.005f, articulation.SleepThreshold);
        Assert.True(articulation.RootLinks.IsEmpty);

        Assert.Equal("BaseLink", link.Name);
        Assert.Equal(LoadingState.ArticulationLink, link.LoadState);
        Assert.Equal(6u, link.InboundJointDof);
        Assert.Null(link.InboundJoint);
        Assert.True(link.Children.IsEmpty);
    }
}
