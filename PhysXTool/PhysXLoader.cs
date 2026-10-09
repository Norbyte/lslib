using System.Collections.Frozen;
using System.Collections.Immutable;
using TurboXml;

namespace PhysXTool;

public sealed class PhysXLoader
{
    private readonly Lock _syncGate = new();
    private readonly Dictionary<string, string> _stringPool = new(StringComparer.Ordinal);

    public ImmutableArray<IPxBase> Collection { get; private set; } = ImmutableArray<IPxBase>.Empty;

    public FrozenDictionary<uint, PxMaterial> Materials { get; private set; } =
        FrozenDictionary<uint, PxMaterial>.Empty;

    public FrozenDictionary<string, PxRigidActor> Actors { get; private set; } =
        FrozenDictionary<string, PxRigidActor>.Empty;

    public void Load(ReadOnlySpan<char> xmlContent)
    {
        if (xmlContent.IsEmpty)
        {
            return;
        }

        lock (_syncGate)
        {
            PhysXHandler handler = new(_stringPool);
            try
            {
                XmlParser.Parse(xmlContent.ToString(), ref handler);
                handler.FinalizeCollections();

                Collection = handler.Collection;
                Materials = handler.Materials;
                Actors = handler.Actors;
            }
            finally
            {
                handler.FreeRentedBuffers();
            }
        }
    }

    public void Load(string xmlContent)
    {
        if (string.IsNullOrEmpty(xmlContent))
        {
            return;
        }

        lock (_syncGate)
        {
            PhysXHandler handler = new(_stringPool);
            try
            {
                XmlParser.Parse(xmlContent, ref handler);
                handler.FinalizeCollections();

                Collection = handler.Collection;
                Materials = handler.Materials;
                Actors = handler.Actors;
            }
            finally
            {
                handler.FreeRentedBuffers();
            }
        }
    }


    public void Load(Stream xmlContent)
    {
        ArgumentNullException.ThrowIfNull(xmlContent);
        if (!xmlContent.CanRead || xmlContent is { CanSeek: true, Length: 0 })
        {
            return;
        }

        lock (_syncGate)
        {
            PhysXHandler handler = new(_stringPool);
            try
            {
                XmlParser.Parse(xmlContent, ref handler);
                handler.FinalizeCollections();

                Collection = handler.Collection;
                Materials = handler.Materials;
                Actors = handler.Actors;
            }
            finally
            {
                handler.FreeRentedBuffers();
            }
        }
    }
}

