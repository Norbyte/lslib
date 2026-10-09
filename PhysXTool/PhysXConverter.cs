using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PhysXTool;

public sealed class PhysXConverter
{
    private readonly PhysXLoader _xmlLoader = new();
    private readonly PhysXExporter _xmlExporter = new();
    private bool _isInitialized;

    public ImmutableArray<IPxBase> Collection => _xmlLoader.Collection;

    public bool InitPhysX()
    {
        _isInitialized = true;
        return true;
    }

    public void ShutdownPhysX()
    {
        _isInitialized = false;
    }

    public ImmutableArray<IPxBase> LoadCollectionFromXml(ReadOnlySpan<char> xmlSpan)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                "PhysX Converter context must be initialized via InitPhysX before parsing.");
        }

        _xmlLoader.Load(xmlSpan);
        return _xmlLoader.Collection;
    }

    public void SaveCollectionToXml(ImmutableArray<IPxBase> collection, out byte[] rentedBuffer, out int length)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                "PhysX Converter context must be initialized via InitPhysX before exporting.");
        }

        if (collection.IsEmpty)
        {
            rentedBuffer = Array.Empty<byte>();
            length = 0;
            return;
        }

        rentedBuffer = ArrayPool<byte>.Shared.Rent(65536);

        using var memoryStream = new MemoryStream(rentedBuffer);
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true);
        _xmlExporter.Export(collection.AsSpan(), writer);
        writer.Flush();
        length = (int)memoryStream.Position;
    }

    public ImmutableArray<IPxBase> LoadCollectionFromBinary(ReadOnlySpan<byte> binarySpan)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                "PhysX Converter context must be initialized via InitPhysX before parsing.");
        }

        if (binarySpan.IsEmpty || binarySpan.Length < Unsafe.SizeOf<PxBinaryHeader>())
        {
            return ImmutableArray<IPxBase>.Empty;
        }

        ReadOnlySpan<PxBinaryHeader> headerSlice =
            MemoryMarshal.Cast<byte, PxBinaryHeader>(binarySpan[..Unsafe.SizeOf<PxBinaryHeader>()]);
        PxBinaryHeader header = headerSlice[0];

        if (header.ActorCount is <= 0 or > 65536)
        {
            return ImmutableArray<IPxBase>.Empty;
        }

        IPxBase[] tempStorage = ArrayPool<IPxBase>.Shared.Rent(header.ActorCount);
        var parsedCount = 0;

        try
        {
            ReadOnlySpan<byte> stringPoolWindow = binarySpan.Slice(header.StringPoolOffset, header.StringPoolLength);
            int cursor = Unsafe.SizeOf<PxBinaryHeader>();

            for (var i = 0; i < header.ActorCount; i++)
            {
                int nextActorBlock = cursor + Unsafe.SizeOf<PxBinaryActorHeader>();
                ReadOnlySpan<PxBinaryActorHeader> actorHeaderSlice =
                    MemoryMarshal.Cast<byte, PxBinaryActorHeader>(binarySpan[cursor..nextActorBlock]);
                PxBinaryActorHeader actorMeta = actorHeaderSlice[0];
                cursor = nextActorBlock;
                ReadOnlySpan<byte> nameSlice = stringPoolWindow[actorMeta.NameOffset..];
                int nullTerminatorIndex = nameSlice.IndexOf((byte)0);
                if (nullTerminatorIndex > 0)
                {
                    nameSlice = nameSlice[..nullTerminatorIndex];
                }

                string resolvedActorName = Encoding.UTF8.GetString(nameSlice);
                if (actorMeta.ActorType == LoadingState.RigidStatic)
                {
                    var staticActor = new PxRigidStatic(resolvedActorName)
                    {
                        DominanceGroup = actorMeta.DominanceGroup,
                        GlobalPose = actorMeta.GlobalPose
                    };
                    tempStorage[parsedCount++] = staticActor;
                }
                else if (actorMeta.ActorType == LoadingState.RigidDynamic)
                {
                    const int dataSize = sizeof(float) * 3;
                    ReadOnlySpan<float> dynamicProperties =
                        MemoryMarshal.Cast<byte, float>(binarySpan[cursor..(cursor + dataSize)]);
                    cursor += dataSize;

                    var dynamicActor = new PxRigidDynamic(resolvedActorName)
                    {
                        DominanceGroup = actorMeta.DominanceGroup,
                        GlobalPose = actorMeta.GlobalPose,
                        Mass = dynamicProperties[0],
                        LinearDamping = dynamicProperties[1],
                        AngularDamping = dynamicProperties[2]
                    };
                    tempStorage[parsedCount++] = dynamicActor;
                }
            }

            return ImmutableArray.Create(tempStorage, 0, parsedCount);
        }
        finally
        {
            ArrayPool<IPxBase>.Shared.Return(tempStorage, clearArray: true);
        }
    }

    public void SaveCollectionToBinary(ImmutableArray<IPxBase> collection, out byte[] rentedBuffer, out int length)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                "PhysX Converter context must be initialized via InitPhysX before exporting.");
        }

        if (collection.IsEmpty)
        {
            rentedBuffer = Array.Empty<byte>();
            length = 0;
            return;
        }

        rentedBuffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);

        using var memoryStream = new MemoryStream(rentedBuffer);
        byte[] stringPoolScratch = ArrayPool<byte>.Shared.Rent(65536);
        var stringPoolLength = 0;

        try
        {
            memoryStream.Position = Unsafe.SizeOf<PxBinaryHeader>();
            Span<float> properties = stackalloc float[3];
            for (var i = 0; i < collection.Length; i++)
            {
                if (collection[i] is PxRigidActor actor)
                {
                    int nameOffset = stringPoolLength;
                    int encodedBytes = Encoding.UTF8.GetBytes(actor.Name, stringPoolScratch.AsSpan(stringPoolLength));
                    stringPoolLength += encodedBytes;
                    stringPoolScratch[stringPoolLength++] = 0;
                    PxBinaryActorHeader metaHeader = new()
                    {
                        ActorType = actor is PxRigidStatic ? LoadingState.RigidStatic : LoadingState.RigidDynamic,
                        NameOffset = nameOffset,
                        DominanceGroup = actor.DominanceGroup,
                        GlobalPose = actor is PxRigidStatic rs ? rs.GlobalPose : ((PxRigidDynamic)actor).GlobalPose
                    };
                    ReadOnlySpan<PxBinaryActorHeader> structView = MemoryMarshal.CreateReadOnlySpan(ref metaHeader, 1);
                    ReadOnlySpan<byte> byteView = MemoryMarshal.AsBytes(structView);
                    memoryStream.Write(byteView);
                    if (actor is PxRigidDynamic dynamicActor)
                    {
                        properties[0] = dynamicActor.Mass;
                        properties[1] = dynamicActor.LinearDamping;
                        properties[2] = dynamicActor.AngularDamping;
                        ReadOnlySpan<byte> propertiesBytes = MemoryMarshal.AsBytes((ReadOnlySpan<float>)properties);
                        memoryStream.Write(propertiesBytes);
                    }
                }
            }

            var stringPoolFileOffset = (int)memoryStream.Position;
            memoryStream.Write(stringPoolScratch.AsSpan(0, stringPoolLength));
            memoryStream.Position = 0;

            Span<byte> headerBuffer = stackalloc byte[Unsafe.SizeOf<PxBinaryHeader>()];
            ref PxBinaryHeader header = ref MemoryMarshal.AsRef<PxBinaryHeader>(headerBuffer);
            header.Magic = 0x42473350; // 'BG3P' constant marker
            header.Version = 1;
            header.StringPoolOffset = stringPoolFileOffset;
            header.StringPoolLength = stringPoolLength;
            header.ActorCount = collection.Length;

            ReadOnlySpan<byte> headerBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1));
            memoryStream.Write(headerBytes);
            length = (int)memoryStream.Length;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(stringPoolScratch, clearArray: true);
        }
    }
}
