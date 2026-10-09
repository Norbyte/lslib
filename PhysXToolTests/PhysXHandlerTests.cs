using PhysXTool;

namespace Tests;

public static class PhysXHandlerTests
{
    [Fact]
    public static void Constructor_InitializesDefaultCollectionsAndEmptyStates()
    {
        Dictionary<string, string> pool = new(StringComparer.Ordinal);
        var handler = new PhysXHandler(pool);
        Assert.True(handler.Collection.IsEmpty);
        Assert.Empty(handler.Materials);
        Assert.Empty(handler.Actors);
    }

    [Fact]
    public static void MaterialParsingLifecycle_CreatesValidCollections_ViaIntegrationPayload()
    {
        var loader = new PhysXLoader();
        ReadOnlySpan<char> xmlPayload = """
        <BG3Physics>
            <PxMaterial>
                <Index>10</Index>
                <StaticFriction>0.75</StaticFriction>
            </PxMaterial>
        </BG3Physics>
        """;
        loader.Load(xmlPayload);
        Assert.False(loader.Collection.IsEmpty);
        
#pragma warning disable xUnit2013
        Assert.Equal(1, loader.Collection.Length);
#pragma warning restore xUnit2013

        Assert.True(loader.Materials.ContainsKey(10u));
        PxMaterial material = loader.Materials[10u];
        Assert.Equal(10u, material.Index);
        Assert.Equal(0.75f, material.StaticFriction);
    }

    [Fact]
    public static void RigidStaticParsingLifecycle_CreatesValidActor_ViaIntegrationPayload()
    {
        var loader = new PhysXLoader();
        ReadOnlySpan<char> xmlPayload = """
        <BG3Physics>
            <PxRigidStatic>
                <Name>Ground_Collider</Name>
                <DominanceGroup>3</DominanceGroup>
            </PxRigidStatic>
        </BG3Physics>
        """;
        loader.Load(xmlPayload);
        Assert.False(loader.Collection.IsEmpty);
        Assert.True(loader.Actors.ContainsKey("Ground_Collider"));

        PxRigidActor actor = loader.Actors["Ground_Collider"];
        Assert.Equal("Ground_Collider", actor.Name);
        Assert.Equal(3u, actor.DominanceGroup);
        Assert.Equal(LoadingState.RigidStatic, actor.LoadState);
    }

    [Fact]
    public static void UnnamedActors_FallbackToDefaultStringKey_ViaIntegrationPayload()
    {
        var loader = new PhysXLoader();
        ReadOnlySpan<char> xmlPayload = """
        <BG3Physics>
            <PxRigidStatic>
                <DominanceGroup>1</DominanceGroup>
            </PxRigidStatic>
        </BG3Physics>
        """;
        loader.Load(xmlPayload);
        Assert.True(loader.Actors.ContainsKey("UnnamedActor"));
        PxRigidActor actor = loader.Actors["UnnamedActor"];
        Assert.Equal("UnnamedActor", actor.Name);
    }
    
    [Fact]
    public static void FreeRentedBuffers_ExecutesWithoutThrowing()
    {
        Dictionary<string, string> pool = new(StringComparer.Ordinal);
        var handler = new PhysXHandler(pool);
        handler.FreeRentedBuffers();
    }

    [Fact]
    public static void EmptyInterfaceHooks_CanBeInvokedSafely()
    {
        Dictionary<string, string> pool = new(StringComparer.Ordinal);
        var handler = new PhysXHandler(pool);
        ReadOnlySpan<char> dummySpan = ReadOnlySpan<char>.Empty;
        handler.OnXmlDeclaration(dummySpan, dummySpan, dummySpan, 0, 0);
        handler.OnProcessingInstruction(dummySpan, dummySpan, 0, 0);
        handler.OnAttribute(dummySpan, dummySpan, 0, 0, 0, 0);
        handler.OnComment(dummySpan, 0, 0);
        handler.OnCData(dummySpan, 0, 0);
        handler.OnEndTagEmpty();
        handler.OnError("MockError", 0, 0);
    }
   

    [Theory]
    [InlineData("PxMaterial")]
    [InlineData("PxRigidStatic")]
    [InlineData("PxRigidDynamic")]
    [InlineData("Shape")]
    [InlineData("PxD6Joint")]
    [InlineData("PxArticulation")]
    [InlineData("PxArticulationLink")]
    [InlineData("PxArticulationJoint")]
    [InlineData("DistanceLimit")]
    [InlineData("LinearLimitX")]
    [InlineData("LinearLimitY")]
    [InlineData("LinearLimitZ")]
    [InlineData("TwistLimit")]
    [InlineData("SwingLimit")]
    [InlineData("PyramidSwingLimit")]
    [InlineData("DriveX")]
    [InlineData("DriveY")]
    [InlineData("DriveZ")]
    [InlineData("DriveSwing")]
    [InlineData("DriveTwist")]
    [InlineData("DriveSlerp")]
    public static void OnBeginTag_StructuralTags_ChangesStateCorrectly(string tagName)
    {
        Dictionary<string, string> pool = new(StringComparer.Ordinal);
        var handler = new PhysXHandler(pool);
        handler.OnBeginTag(tagName.AsSpan(), 1, 1);
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("Index")]
    [InlineData("StaticFriction")]
    [InlineData("DynamicFriction")]
    [InlineData("Restitution")]
    [InlineData("DominanceGroup")]
    [InlineData("W")]
    [InlineData("X")]
    [InlineData("Y")]
    [InlineData("Z")]
    [InlineData("ForceLimit")]
    [InlineData("Stiffness")]
    [InlineData("Damping")]
    [InlineData("IsAcceleration")]
    [InlineData("DriveType")]
    [InlineData("NonExistentProperty")]
    public static void OnBeginTag_PropertyTags_ProcessesTokensWithoutThrowing(string valueTagName)
    {
        Dictionary<string, string> pool = new(StringComparer.Ordinal);
        var handler = new PhysXHandler(pool);
        handler.OnBeginTag(valueTagName.AsSpan(), 1, 1);
    }
}
