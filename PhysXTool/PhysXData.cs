using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.InteropServices;

namespace PhysXTool;

[Flags]
public enum PxActors : uint
{
    None = 0,
    DisableGravity = 1 << 0
}

[Flags]
public enum PxRigidBodies : uint
{
    None = 0,
    Kinematic = 1 << 0,
    EnableCcd = 1 << 1,
    EnableCcdFriction = 1 << 2,
    EnableSpeculativeCcd = 1 << 3,
    EnableCcdMaxContactImpulse = 1 << 4,
    RetainAccelerations = 1 << 5
}

[Flags]
public enum PxRigidDynamicLocks : uint
{
    None = 0,
    LockLinearX = 1 << 0,
    LockLinearY = 1 << 1,
    LockLinearZ = 1 << 2,
    LockAngularX = 1 << 3,
    LockAngularY = 1 << 4,
    LockAngularZ = 1 << 5
}

[Flags]
public enum PxConstraints : uint
{
    None = 0,
    ProjectToActor0 = 1 << 0,
    ProjectToActor1 = 1 << 1,
    CollisionEnabled = 1 << 2,
    DriveLimitsAreForces = 1 << 3
}

public enum D6Motion
{
    Locked = 0,
    Limited = 1,
    Free = 2
}

public enum D6Axis
{
    X = 0,
    Y = 1,
    Z = 2,
    Twist = 3,
    Swing1 = 4,
    Swing2 = 5
}

public enum D6Drive
{
    X = 0,
    Y = 1,
    Z = 2,
    Swing = 3,
    Twist = 4,
    Slerp = 5
}

public enum PxGeometryType
{
    Sphere = 0,
    Capsule = 1,
    Box = 2,
    ConvexMesh = 3,
    TriangleMesh = 4
}

public enum LoadingState
{
    None = 0,
    Material = 1,
    RigidStatic = 2,
    RigidDynamic = 3,
    Shape = 4,
    Geometry = 5,
    Transform = 6,
    Vector3 = 7,
    Quaternion = 8,
    LinearLimit = 9,
    LinearLimitPair = 10,
    AngularLimitPair = 11,
    LimitCone = 12,
    LimitPyramid = 13,
    JointDrive = 14,
    D6Joint = 15,
    Articulation = 16,
    ArticulationLink = 17,
    ArticulationJoint = 18,
}

public interface IPxBase
{
    public LoadingState LoadState { get; }
}

public interface IPxGeometry
{
    public PxGeometryType GeometryType { get; }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PxBinaryHeader
{
    public uint Magic;
    public uint Version;
    public int StringPoolOffset;
    public int StringPoolLength;
    public int ActorCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PxBinaryActorHeader
{
    public LoadingState ActorType;
    public int NameOffset;
    public uint DominanceGroup;
    public PxTransform GlobalPose;
}

[StructLayout(LayoutKind.Sequential, Pack = 16)]
public struct PxTransform
{
    public static LoadingState LoadState => LoadingState.Transform;

    public Vector3 Position;
    public Quaternion Rotation;
    public PxTransform(Vector3 position, Quaternion rotation)
    {
        Position = position;
        Rotation = rotation;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PxMeshScale
{
    public Vector3 Scale;
    public Quaternion Rotation;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PxJointLinearLimit
{
    public float Value;
    public float Restitution;
    public float BounceThreshold;
    public float Stiffness;
    public float Damping;
    public float ContactDistance;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PxJointLinearLimitPair
{
    public float Lower;
    public float Upper;
    public float Restitution;
    public float BounceThreshold;
    public float Stiffness;
    public float Damping;
    public float ContactDistance;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PxJointAngularLimitPair
{
    public float Lower;
    public float Upper;
    public float Restitution;
    public float BounceThreshold;
    public float Stiffness;
    public float Damping;
    public float ContactDistance;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PxJointLimitCone
{
    public float YAngle;
    public float ZAngle;
    public float Restitution;
    public float BounceThreshold;
    public float Stiffness;
    public float Damping;
    public float ContactDistance;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PxJointLimitPyramid
{
    public float YAngleMin;
    public float YAngleMax;
    public float ZAngleMin;
    public float ZAngleMax;
    public float Restitution;
    public float BounceThreshold;
    public float Stiffness;
    public float Damping;
    public float ContactDistance;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PxD6JointDrive
{
    public D6Axis TargetAxisType { get; set;  }
    public D6Drive TargetDriveType { get; set; }
    public float ForceLimit;
    public bool IsAcceleration;
    public float Stiffness;
    public float Damping;
}

public sealed class PxMaterial : IPxBase
{
    public LoadingState LoadState => LoadingState.Material;
    public uint Index { get; init; }
    public float StaticFriction { get; init => field = value < 0.0f ? 0.0f : value; }
    public float DynamicFriction { get; init => field = value < 0.0f ? 0.0f : value; }
    public float Restitution { get; init => field = Math.Clamp(value, 0.0f, 1.0f); }
}

public sealed class PxSphereGeometry(float radius) : IPxGeometry 
{ 
    public PxGeometryType GeometryType => PxGeometryType.Sphere;
    public float Radius { get; init; } = radius;
}

public sealed class PxCapsuleGeometry(float radius, float halfHeight) : IPxGeometry 
{ 
    public PxGeometryType GeometryType => PxGeometryType.Capsule;
    public float Radius { get; init; } = radius; 
    public float HalfHeight { get; init; } = halfHeight; 
}

public sealed class PxBoxGeometry(Vector3 halfExtents) : IPxGeometry 
{ 
    public PxGeometryType GeometryType => PxGeometryType.Box;
    public Vector3 HalfExtents { get; init; } = halfExtents; 
}

public sealed class PxConvexMeshGeometry(PxConvexMesh mesh, PxMeshScale scale) : IPxGeometry 
{ 
    public PxGeometryType GeometryType => PxGeometryType.ConvexMesh;
    public PxConvexMesh ConvexMesh { get; init; } = mesh; 
    public PxMeshScale Scale { get; init; } = scale; 
}

public sealed class PxTriangleMeshGeometry(PxTriangleMesh mesh, PxMeshScale scale) : IPxGeometry 
{ 
    public PxGeometryType GeometryType => PxGeometryType.TriangleMesh;
    public PxTriangleMesh TriangleMesh { get; init; } = mesh; 
    public PxMeshScale Scale { get; init; } = scale; 
}

public sealed class PxConvexMesh : IPxBase
{
    public LoadingState LoadState { get; init; } = LoadingState.Geometry;
}

public sealed class PxTriangleMesh : IPxBase
{
    public LoadingState LoadState { get; init; } = LoadingState.Geometry;
}

public sealed class PxShape : IPxBase
{
    public string Name { get; init; } = string.Empty;
    public IPxGeometry? Geometry { get; set; }
    
    public LoadingState LoadState { get; init; } = LoadingState.Shape;
    public PxMaterial? Material { get; set; }
    public float ContactOffset { get; set; } = 0.02f;
    public float RestOffset { get; set; }
    public float TorsionalPatchRadius { get; set; }
    public float MinTorsionalPatchRadius { get; set; }
    private PxTransform _localPose;
    public ref PxTransform LocalPose => ref _localPose;
}

public abstract class PxRigidActor(string name, LoadingState state = LoadingState.Shape) : IPxBase
{
    public string Name { get; init; } = name;
    public LoadingState LoadState { get; init; } = state;
    public PxActors ActorFlags { get; set; }
    public uint DominanceGroup { get; set; }
    public ImmutableArray<PxShape> Shapes { get; init; } = ImmutableArray<PxShape>.Empty;
}

public sealed class PxRigidStatic(string name) : PxRigidActor(name, LoadingState.RigidStatic) 
{ 
    public PxTransform GlobalPose { get; set; }
}

public abstract class PxRigidBody(string name, LoadingState state = LoadingState.RigidDynamic) : PxRigidActor(name, state)
{
    public PxTransform CMassLocalPose { get; set; }
    public float Mass { get; set; } = 1.0f;
    public Vector3 MassSpaceInertiaTensor { get; set; } = Vector3.One;
    public float LinearDamping { get; set; }
    public float AngularDamping { get; set; } = 0.05f;
    public float MaxLinearVelocity { get; set; } = 1e+15f;
    public float MaxAngularVelocity { get; set; } = 100.0f;
    public PxRigidBodies RigidBodyFlags { get; set; }
    public float MinCcdAdvanceCoefficient { get; set; } = 0.15f;
    public float MaxDepenetrationVelocity { get; set; } = 1e+31f;
    public float MaxContactImpulse { get; set; } = 1e+31f;
}

public sealed class PxRigidDynamic(string name) : PxRigidBody(name)
{
    public PxTransform GlobalPose { get; set; }
    public uint MinPositionIterations { get; set; } = 4;
    public uint MinVelocityIterations { get; set; } = 1;
    public float SleepThreshold { get; set; } = 0.005f;
    public float StabilizationThreshold { get; set; } = 0.0025f;
    public PxRigidDynamicLocks LockFlags { get; set; }
    public float WakeCounter { get; set; }
    public float ContactReportThreshold { get; set; } = float.MaxValue;
}

public abstract class PxJoint(string name, LoadingState state = LoadingState.JointDrive) : IPxBase
{
    public string Name { get; set; } = name;
    public string Actor0 { get; set; } = string.Empty;
    public string Actor1 { get; set; } = string.Empty;
    public PxTransform Actor0LocalPose { get; set; }
    public PxTransform Actor1LocalPose { get; set; }
    public float BreakForce { get; set; } = float.MaxValue;
    public float BreakTorque { get; set; } = float.MaxValue;
    public PxConstraints ConstraintFlags { get; set; }
    public float InvMassScale0 { get; set; } = 1.0f;
    public float InvInertiaScale0 { get; set; } = 1.0f;
    public float InvMassScale1 { get; set; } = 1.0f;
    public float InvInertiaScale1 { get; set; } = 1.0f;
    public LoadingState LoadState { get; init; } = state;
}

public sealed class PxD6Joint(string name) : PxJoint(name, LoadingState.D6Joint)
{
    public D6Motion MotionX { get; set; } = D6Motion.Free;
    public D6Motion MotionY { get; set; } = D6Motion.Free;
    public D6Motion MotionZ { get; set; } = D6Motion.Free;
    public D6Motion MotionTwist { get; set; } = D6Motion.Free;
    public D6Motion MotionSwing1 { get; set; } = D6Motion.Free;
    public D6Motion MotionSwing2 { get; set; } = D6Motion.Free;
    public float ProjectionLinearTolerance { get; set; } = 1e+10f;
    public float ProjectionAngularTolerance { get; set; } = 3.14159f;
    private PxJointLinearLimit _distanceLimit;
    private PxJointLinearLimitPair _linearLimitX;
    private PxJointLinearLimitPair _linearLimitY;
    private PxJointLinearLimitPair _linearLimitZ;
    private PxJointAngularLimitPair _twistLimit;
    private PxJointLimitCone _swingLimit;
    private PxJointLimitPyramid _pyramidSwingLimit;
    private PxD6JointDrive _driveX;
    private PxD6JointDrive _driveY;
    private PxD6JointDrive _driveZ;
    private PxD6JointDrive _driveSwing;
    private PxD6JointDrive _driveTwist;
    private PxD6JointDrive _driveSlerp;
    public ref PxJointLinearLimit DistanceLimit => ref _distanceLimit;
    public ref PxJointLinearLimitPair LinearLimitX => ref _linearLimitX;
    public ref PxJointLinearLimitPair LinearLimitY => ref _linearLimitY;
    public ref PxJointLinearLimitPair LinearLimitZ => ref _linearLimitZ;
    public ref PxJointAngularLimitPair TwistLimit => ref _twistLimit;
    public ref PxJointLimitCone SwingLimit => ref _swingLimit;
    public ref PxJointLimitPyramid PyramidSwingLimit => ref _pyramidSwingLimit;
    public ref PxD6JointDrive DriveX => ref _driveX;
    public ref PxD6JointDrive DriveY => ref _driveY;
    public ref PxD6JointDrive DriveZ => ref _driveZ;
    public ref PxD6JointDrive DriveSwing => ref _driveSwing;
    public ref PxD6JointDrive DriveTwist => ref _driveTwist;
    public ref PxD6JointDrive DriveSlerp => ref _driveSlerp;
}

public abstract class PxArticulationJointBase(LoadingState state = LoadingState.ArticulationJoint) : IPxBase
{
    public PxTransform ParentPose { get; set; }
    public PxTransform ChildPose { get; set; }
    public LoadingState LoadState { get; init; } = state;
}

public sealed class PxArticulationJoint : PxArticulationJointBase
{
    public float Stiffness { get; set; }
    public float Damping { get; set; }
    public float InternalCompliance { get; set; }
    public float ExternalCompliance { get; set; }
    public float SwingLimitZ { get; set; } = MathF.PI / 4.0f;
    public float SwingLimitY { get; set; } = MathF.PI / 4.0f;
    public float TangentialStiffness { get; set; }
    public float TangentialDamping { get; set; }
    public float SwingLimitContactDistance { get; set; } = 0.05f;
    public bool SwingLimitEnabled { get; set; }
    public float TwistLimitLower { get; set; } = -MathF.PI / 4.0f;
    public float TwistLimitUpper { get; set; } = MathF.PI / 4.0f;
    public float TwistLimitContactDistance { get; set; } = 0.05f;
    public bool TwistLimitEnabled { get; set; }
}

public sealed class PxArticulationLink(string name) : PxRigidBody(name, LoadingState.ArticulationLink)
{
    public uint InboundJointDof { get; set; }
    public PxArticulationJoint? InboundJoint { get; set; }
    public ImmutableArray<PxArticulationLink> Children { get; init; } = ImmutableArray<PxArticulationLink>.Empty;
}

public sealed class PxArticulation : IPxBase
{
    public LoadingState LoadState { get; init; } = LoadingState.Articulation;
    public float SleepThreshold { get; set; } = 0.005f;
    public float StabilizationThreshold { get; set; } = 0.0025f;
    public float WakeCounter { get; set; } = 0.4f;
    public uint MaxProjectionIterations { get; set; } = 4;
    public float SeparationTolerance { get; set; } = 0.01f;
    public uint InternalDriveIterations { get; set; } = 4;
    public uint ExternalDriveIterations { get; set; } = 4;
    public ImmutableArray<PxArticulationLink> RootLinks { get; init; } = ImmutableArray<PxArticulationLink>.Empty;
}
