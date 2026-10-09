using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace PhysXTool;

public sealed class PhysXExporter(bool exportAllProperties = true)
{
    public bool ExportAllProperties { get; set; } = exportAllProperties;

    [ThreadStatic] private static char[]? _tlsFormatBuffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Span<char> GetFormatBuffer()
    {
        _tlsFormatBuffer ??= new char[256];
        return _tlsFormatBuffer;
    }

    public void Export(ReadOnlySpan<IPxBase> collection, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        PxMaterial[] materialsTracker = ArrayPool<PxMaterial>.Shared.Rent(512);
        var uniqueMaterialsCount = 0;

        try
        {
            writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
            writer.Write("<BG3Physics>\n");

            for (var i = 0; i < collection.Length; i++)
            {
                if (collection[i] is PxRigidActor actor)
                {
                    ReadOnlySpan<PxShape> shapes = actor.Shapes.AsSpan();
                    for (var s = 0; s < shapes.Length; s++)
                    {
                        PxMaterial? mat = shapes[s].Material;
                        if (mat is not null && !ContainsMaterial(materialsTracker, uniqueMaterialsCount, mat))
                        {
                            materialsTracker[uniqueMaterialsCount++] = mat;
                        }
                    }
                }
            }

            ReadOnlySpan<PxMaterial> uniqueMaterials = materialsTracker.AsSpan()[..uniqueMaterialsCount];

            for (var i = 0; i < uniqueMaterials.Length; i++)
            {
                ExportMaterial(writer, uniqueMaterials[i], (uint)i, 1);
            }

            for (var i = 0; i < collection.Length; i++)
            {
                ExportTopLevel(writer, collection[i], uniqueMaterials, 1);
            }

            writer.Write("</BG3Physics>\n");
        }
        finally
        {
            ArrayPool<PxMaterial>.Shared.Return(materialsTracker, clearArray: true);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsMaterial(PxMaterial[] tracker, int count, PxMaterial mat)
    {
        for (var i = 0; i < count; i++)
        {
            if (ReferenceEquals(tracker[i], mat))
            {
                return true;
            }
        }

        return false;
    }

    private void ExportTopLevel(TextWriter writer, IPxBase obj, ReadOnlySpan<PxMaterial> uniqueMaterials, int indent)
    {
        switch (obj)
        {
            case PxRigidStatic rigidStatic:
                ExportRigidStatic(writer, rigidStatic, uniqueMaterials, indent);
                break;
            case PxRigidDynamic rigidDynamic:
                ExportRigidDynamic(writer, rigidDynamic, uniqueMaterials, indent);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteIndent(TextWriter writer, int indent)
    {
        for (var i = 0; i < indent; i++)
        {
            writer.Write("  ");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteRequiredProperty<T>(TextWriter writer, string name, T value, int indent)
        where T : ISpanFormattable
    {
        WriteIndent(writer, indent);
        writer.Write('<');
        writer.Write(name);
        writer.Write('>');

        Span<char> formatBuffer = GetFormatBuffer();
        if (value.TryFormat(formatBuffer, out int charsWritten, "G", CultureInfo.InvariantCulture))
        {
            writer.Write(formatBuffer[..charsWritten]);
        }
        else
        {
            writer.Write(value.ToString());
        }

        writer.Write("</");
        writer.Write(name);
        writer.Write(">\n");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteFloatProperty(TextWriter writer, string name, float value, int indent)
    {
        WriteIndent(writer, indent);
        writer.Write('<');
        writer.Write(name);
        writer.Write('>');

        Span<char> formatBuffer = GetFormatBuffer();
        if (value.TryFormat(formatBuffer, out int charsWritten, "F8", CultureInfo.InvariantCulture))
        {
            writer.Write(formatBuffer[..charsWritten]);
        }

        writer.Write("</");
        writer.Write(name);
        writer.Write(">\n");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteStringProperty(TextWriter writer, string name, string value, int indent)
    {
        WriteIndent(writer, indent);
        writer.Write('<');
        writer.Write(name);
        writer.Write('>');
        writer.Write(value);
        writer.Write("</");
        writer.Write(name);
        writer.Write(">\n");
    }

    public static void ExportMaterial(TextWriter writer, PxMaterial obj, uint index, int indent)
    {
        WriteIndent(writer, indent);
        writer.Write("<Material>\n");
        WriteRequiredProperty(writer, "Index", index, indent + 1);
        WriteIndent(writer, indent);
        writer.Write("</Material>\n");
    }

    public static void ExportRigidStatic(TextWriter writer, PxRigidStatic o, ReadOnlySpan<PxMaterial> uniqueMaterials,
    int indent)
    {
        WriteIndent(writer, indent);
        writer.Write("<RigidStatic>\n");
        WriteStringProperty(writer, "Name", o.Name, indent + 1);
        WriteIndent(writer, indent);
        writer.Write("</RigidStatic>\n");
    }

    public static void ExportDynamicProperties(TextWriter writer, PxRigidDynamic o, int indent)
    {
        WriteFloatProperty(writer, "Mass", o.Mass, indent);
        WriteFloatProperty(writer, "LinearDamping", o.LinearDamping, indent);
        WriteFloatProperty(writer, "AngularDamping", o.AngularDamping, indent);
        WriteFloatProperty(writer, "MaxLinearVelocity", o.MaxLinearVelocity, indent);
        WriteFloatProperty(writer, "MaxAngularVelocity", o.MaxAngularVelocity, indent);
    }

    public void ExportRigidDynamic(TextWriter writer, PxRigidDynamic o, ReadOnlySpan<PxMaterial> uniqueMaterials,
    int indent)
    {
        WriteIndent(writer, indent);
        writer.Write("<RigidDynamic>\n");
        WriteStringProperty(writer, "Name", o.Name, indent + 1);

        if (ExportAllProperties)
        {
            ExportDynamicProperties(writer, o, indent + 1);
        }

        WriteIndent(writer, indent);
        writer.Write("</RigidDynamic>\n");
    }
}