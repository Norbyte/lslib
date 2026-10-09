using System.Buffers;
using System.Collections.Immutable;
using System.Numerics;
using PhysXTool;

namespace Tests;


public sealed class PhysXConverterTests
{
    [Fact]
    public void UninitializedContext_ThrowsInvalidOperationException_WithoutClosureAllocation()
    {
        Assert.Throws<InvalidOperationException>(static () =>
        {
            var converter = new PhysXConverter();
            converter.LoadCollectionFromXml("<Xml></Xml>".AsSpan());
        });

        Assert.Throws<InvalidOperationException>(static () =>
        {
            var converter = new PhysXConverter();
            converter.SaveCollectionToXml(ImmutableArray<IPxBase>.Empty, out _, out _);
        });

        Assert.Throws<InvalidOperationException>(static () =>
        {
            var converter = new PhysXConverter();
            ReadOnlySpan<byte> emptyBytes = ReadOnlySpan<byte>.Empty;
            converter.LoadCollectionFromBinary(emptyBytes);
        });

        Assert.Throws<InvalidOperationException>(static () =>
        {
            var converter = new PhysXConverter();
            converter.SaveCollectionToBinary(ImmutableArray<IPxBase>.Empty, out _, out _);
        });
    }

    [Fact]
    public static void ContextLifecycle_TransitionsCorrectly()
    {
        var converter = new PhysXConverter();
        Assert.True(converter.Collection.IsEmpty);
        Assert.True(converter.InitPhysX());
        
        converter.ShutdownPhysX();
        
        Assert.Throws<InvalidOperationException>(static () =>
        {
            var conv = new PhysXConverter();
            conv.InitPhysX();
            conv.ShutdownPhysX();
            conv.LoadCollectionFromXml("".AsSpan());
        });
    }

    [Fact]
    public static void EmptyCollection_Serialization_YieldsSafeEmptyOutputs()
    {
        var converter = new PhysXConverter();
        converter.InitPhysX();
        
        converter.SaveCollectionToXml(ImmutableArray<IPxBase>.Empty, out byte[] rentedXmlBuffer, out int xmlLength);
        converter.SaveCollectionToBinary(ImmutableArray<IPxBase>.Empty, out byte[] rentedBinBuffer, out int binLength);

        Assert.Empty(rentedXmlBuffer);
        #pragma warning disable xUnit2013
        Assert.Equal(0, xmlLength);
        Assert.Empty(rentedBinBuffer);
        Assert.Equal(0, binLength);
        #pragma warning restore xUnit2013
    }

    [Fact]
    public static void BinaryParser_CorruptedSizingOrMissingHeader_ReturnsEmptyCollection()
    {
        var converter = new PhysXConverter();
        converter.InitPhysX();
        
        ReadOnlySpan<byte> corruptedShortSpan = [0x01, 0x02, 0x03];
        ImmutableArray<IPxBase> parsed = converter.LoadCollectionFromBinary(corruptedShortSpan);
        Assert.True(parsed.IsEmpty);
    }

    [Fact]
    public static void BinaryRoundTrip_StaticAndDynamicActors_MaintainsStructuralIntegrity()
    {
        var converter = new PhysXConverter();
        converter.InitPhysX();

        var staticActor = new PxRigidStatic("Static_A")
        {
            DominanceGroup = 5,
            GlobalPose = new PxTransform(new Vector3(10f, 20f, 30f), Quaternion.Identity)
        };

        var dynamicActor = new PxRigidDynamic("Dynamic_B")
        {
            DominanceGroup = 2,
            GlobalPose = new PxTransform(Vector3.Zero, Quaternion.Identity),
            Mass = 50.5f,
            LinearDamping = 0.1f,
            AngularDamping = 0.05f
        };

        ImmutableArray<IPxBase> elements = ImmutableArray.Create<IPxBase>(staticActor, dynamicActor);
        converter.SaveCollectionToBinary(elements, out byte[] binaryBuffer, out int writtenLength);

        ImmutableArray<IPxBase> resultCollection;
        try
        {
            ReadOnlySpan<byte> parsedSpan = binaryBuffer.AsSpan(0, writtenLength);
            resultCollection = converter.LoadCollectionFromBinary(parsedSpan);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(binaryBuffer);
        }

        Assert.False(resultCollection.IsEmpty);
    #pragma warning disable xUnit2013
        Assert.Equal(2, resultCollection.Length);
    #pragma warning restore xUnit2013
        PxRigidStatic parsedStatic = Assert.IsType<PxRigidStatic>(resultCollection[0]);
        Assert.Equal("Static_A", parsedStatic.Name);
        Assert.Equal(5u, parsedStatic.DominanceGroup);
        Assert.Equal(10f, parsedStatic.GlobalPose.Position.X);
        PxRigidDynamic parsedDynamic = Assert.IsType<PxRigidDynamic>(resultCollection[1]);
        Assert.Equal("Dynamic_B", parsedDynamic.Name);
        Assert.Equal(2u, parsedDynamic.DominanceGroup);
        Assert.Equal(50.5f, parsedDynamic.Mass);
        Assert.Equal(0.1f, parsedDynamic.LinearDamping);
    }

}
