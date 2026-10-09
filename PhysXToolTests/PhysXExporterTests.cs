using System.Collections.Immutable;
using PhysXTool;

namespace Tests;

public sealed class PhysXExporterTests
{
    [Fact]
    public static void Export_NullWriter_ThrowsArgumentNullException_WithoutClosureAllocation()
    {
        Assert.Throws<ArgumentNullException>("writer", static () =>
        {
            var exporter = new PhysXExporter();
            ReadOnlySpan<IPxBase> emptyCollection = ReadOnlySpan<IPxBase>.Empty;
            exporter.Export(emptyCollection, null!);
        });
    }

    [Fact]
    public static void Export_EmptyCollection_GeneratesValidRootXmlStructure()
    {
        var exporter = new PhysXExporter();
        using var writer = new StringWriter();
        ReadOnlySpan<IPxBase> emptyCollection = ReadOnlySpan<IPxBase>.Empty;
        exporter.Export(emptyCollection, writer);
        var output = writer.ToString();
        Assert.Contains("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n", output, StringComparison.Ordinal);
        Assert.Contains("<BG3Physics>\n", output, StringComparison.Ordinal);
        Assert.Contains("</BG3Physics>\n", output, StringComparison.Ordinal);
    }

    [Fact]
    public static void Export_RigidStaticAndMaterials_DeDuplicatesAndFormatsCorrectly()
    {
        var exporter = new PhysXExporter();
        using var writer = new StringWriter();

        var sharedMaterial = new PxMaterial { Index = 0, StaticFriction = 0.5f };
        
        var shape1 = new PxShape { Material = sharedMaterial };
        var shape2 = new PxShape { Material = sharedMaterial };

        var actor1 = new PxRigidStatic("StaticActor_A")
        {
            Shapes = ImmutableArray.Create(shape1)
        };
        var actor2 = new PxRigidStatic("StaticActor_B")
        {
            Shapes = ImmutableArray.Create(shape2)
        };

        ReadOnlySpan<IPxBase> collection = [actor1, actor2];

        exporter.Export(collection, writer);
        var output = writer.ToString();

        Assert.Contains("<Material>\n", output, StringComparison.Ordinal);
        Assert.Contains("<Index>0</Index>\n", output, StringComparison.Ordinal);
        Assert.Contains("<RigidStatic>\n", output, StringComparison.Ordinal);
        Assert.Contains("<Name>StaticActor_A</Name>\n", output, StringComparison.Ordinal);
        Assert.Contains("<Name>StaticActor_B</Name>\n", output, StringComparison.Ordinal);
        int materialCount = CountOccurrences(output, "<Material>");
        
        #pragma warning disable xUnit2013
        Assert.Equal(1, materialCount);
        #pragma warning restore xUnit2013
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public static void Export_RigidDynamic_RespectsExportAllPropertiesFlag(bool exportAll)
    {
        var exporter = new PhysXExporter(exportAllProperties: exportAll);
        using var writer = new StringWriter();

        var dynamicActor = new PxRigidDynamic("DynamicActor_A")
        {
            Mass = 75.25f,
            LinearDamping = 0.123f
        };

        IPxBase[] collection = [dynamicActor];
        exporter.Export(collection, writer);
        var output = writer.ToString();
        Assert.Contains("<RigidDynamic>\n", output, StringComparison.Ordinal);
        Assert.Contains("<Name>DynamicActor_A</Name>\n", output, StringComparison.Ordinal);

        if (exportAll)
        {
            Assert.Contains("<Mass>75.25000000</Mass>\n", output, StringComparison.Ordinal);
            Assert.Contains("<LinearDamping>0.12300000</LinearDamping>\n", output, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("<Mass>", output, StringComparison.Ordinal);
            Assert.DoesNotContain("<LinearDamping>", output, StringComparison.Ordinal);
        }
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += value.Length;
        }
        return count;
    }
}
