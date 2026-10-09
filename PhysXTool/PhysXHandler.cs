using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TurboXml;

namespace PhysXTool;

public enum PropertyToken
{
    None = 0,
    Name = 1,
    Index = 2,
    StaticFriction = 3,
    DynamicFriction = 4,
    Restitution = 5,
    DominanceGroup = 6,
    W = 7,
    X = 8,
    Y = 9,
    Z = 10,
    ForceLimit = 11,
    Stiffness = 12,
    Damping = 13,
    IsAcceleration = 14,
    DriveType = 15
}

public struct PhysXHandler : IXmlReadHandler
{
    private PropertyToken _currentPropertyToken;
    private string _activeActorName;
    private LoadingState _activeActorType;
    private readonly Dictionary<string, string> _sharedStringPool;
    private readonly IPxBase[] _collectionBuilder;
    private readonly KeyValuePair<uint, PxMaterial>[] _materialsBuilder;
    private readonly KeyValuePair<string, PxRigidActor>[] _actorsBuilder;
    private readonly byte[] _stateStack;
    private readonly PxShape[] _shapesBuilder;
    private int _collectionCount;
    private int _materialsCount;
    private int _actorsCount;
    private int _stateStackPointer;
    private int _shapesCount;
    private uint _currentMaterialIndex;
    private uint _activeDominanceGroup;
    private float _activeMass;
    private float _activeLinearDamping;
    private float _activeAngularDamping;
    private PxMaterial? _activeMaterial;
    private PxShape? _activeShape;
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private Vector3 _activeVector;
    private float _activeQuatW;
    private float _activeQuatX;
    private float _activeQuatY;
    private float _activeQuatZ; 
    private PxD6Joint? _activeD6Joint;
    private PxArticulation? _activeArticulation;
    private PxArticulationLink? _activeArticulationLink;
    private PxArticulationJoint? _activeArticulationJoint;
    private PxJointLinearLimit _transientLinearLimit;
    private PxJointLinearLimitPair _transientLinearLimitPair;
    private PxJointAngularLimitPair _transientAngularLimitPair;
    private PxJointLimitCone _transientLimitCone;
    private PxJointLimitPyramid _transientLimitPyramid;
    private PxD6JointDrive _transientJointDrive;
    public ImmutableArray<IPxBase> Collection { get; private set; }
    public FrozenDictionary<uint, PxMaterial> Materials { get; private set; }
    public FrozenDictionary<string, PxRigidActor> Actors { get; private set; }

    public PhysXHandler(Dictionary<string, string> sharedStringPool)
    {
        _currentPropertyToken = PropertyToken.None;
        _activeActorName = string.Empty;
        _activeActorType = LoadingState.None;
        _sharedStringPool = sharedStringPool;
        _collectionBuilder = ArrayPool<IPxBase>.Shared.Rent(4096);
        _materialsBuilder = ArrayPool<KeyValuePair<uint, PxMaterial>>.Shared.Rent(512);
        _actorsBuilder = ArrayPool<KeyValuePair<string, PxRigidActor>>.Shared.Rent(1024);
        _stateStack = ArrayPool<byte>.Shared.Rent(64);
        _shapesBuilder = ArrayPool<PxShape>.Shared.Rent(256);
        _collectionCount = 0;
        _materialsCount = 0;
        _actorsCount = 0;
        _stateStackPointer = 0;
        _shapesCount = 0;
        _currentMaterialIndex = 0;
        _activeDominanceGroup = 0;
        _activeMass = 1.0f;
        _activeLinearDamping = 0.0f;
        _activeAngularDamping = 0.05f;
        _activeMaterial = null;
        _activeShape = null;
        _activeVector = Vector3.Zero;
        _activeD6Joint = null;
        _activeArticulation = null;
        _activeArticulationLink = null;
        _activeArticulationJoint = null;
        _transientLinearLimit = default;
        _transientLinearLimitPair = default;
        _transientAngularLimitPair = default;
        _transientLimitCone = default;
        _transientLimitPyramid = default;
        _transientJointDrive = default;
        Collection = ImmutableArray<IPxBase>.Empty;
        Materials = FrozenDictionary<uint, PxMaterial>.Empty;
        Actors = FrozenDictionary<string, PxRigidActor>.Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string ResolveStringFromPool(ReadOnlySpan<char> span)
    {
        Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> alternateLookup =
            _sharedStringPool.GetAlternateLookup<ReadOnlySpan<char>>();
        ref string? stringRef = ref CollectionsMarshal.GetValueRefOrAddDefault(alternateLookup, span, out bool exists);
        if (!exists)
        {
            stringRef = span.ToString();
        }

        return stringRef!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
public void OnBeginTag(ReadOnlySpan<char> name, int line, int column)
{
    switch (name)
    {
        case "LocalPose":
        case "GlobalPose":
        case "BG3Physics":
            _stateStack[_stateStackPointer++] = (byte)LoadingState.None;
            _currentPropertyToken = PropertyToken.None;
            return;

        case "PxMaterial":
            _currentMaterialIndex = 0;
            _activeMaterial = null;
            _stateStack[_stateStackPointer++] = (byte)LoadingState.Material;
            _currentPropertyToken = PropertyToken.None;
            return;

        case "PxRigidStatic":
            _activeActorType = LoadingState.RigidStatic;
            _activeActorName = string.Empty;
            _activeDominanceGroup = 0;
            _shapesCount = 0;
            _stateStack[_stateStackPointer++] = (byte)LoadingState.RigidStatic;
            _currentPropertyToken = PropertyToken.None;
            return;

        case "PxRigidDynamic":
            _activeActorType = LoadingState.RigidDynamic;
            _activeActorName = string.Empty;
            _activeDominanceGroup = 0;
            _shapesCount = 0;
            _activeMass = 1.0f;
            _activeLinearDamping = 0.0f;
            _activeAngularDamping = 0.05f;
            _stateStack[_stateStackPointer++] = (byte)LoadingState.RigidDynamic;
            _currentPropertyToken = PropertyToken.None;
            return;

        case "Shape":
            _activeShape = new PxShape();
            _stateStack[_stateStackPointer++] = (byte)LoadingState.Shape;
            _currentPropertyToken = PropertyToken.None;
            return;

        case "Quaternion":
            _activeQuatW = 1.0f;
            _activeQuatX = 0.0f;
            _activeQuatY = 0.0f;
            _activeQuatZ = 0.0f;
            _stateStack[_stateStackPointer++] = (byte)LoadingState.Quaternion;
            _currentPropertyToken = PropertyToken.None;
            return;

        case "PxD6Joint":
            _activeActorName = string.Empty;
            _activeD6Joint = new PxD6Joint(_activeActorName);
            _stateStack[_stateStackPointer++] = (byte)LoadingState.D6Joint;
            _currentPropertyToken = PropertyToken.None;
            return;

        case "PxArticulation":
            _activeArticulation = new PxArticulation();
            _stateStack[_stateStackPointer++] = (byte)LoadingState.Articulation;
            _currentPropertyToken = PropertyToken.None;
            return;

        case "PxArticulationLink":
            _activeArticulationLink = new PxArticulationLink(string.Empty);
            _stateStack[_stateStackPointer++] = (byte)LoadingState.ArticulationLink;
            _currentPropertyToken = PropertyToken.None;
            return;

        case "PxArticulationJoint":
            _activeArticulationJoint = new PxArticulationJoint();
            _stateStack[_stateStackPointer++] = (byte)LoadingState.ArticulationJoint;
            _currentPropertyToken = PropertyToken.None;
            return;

        case "DistanceLimit":
            _transientLinearLimit = default;
            _stateStack[_stateStackPointer++] = (byte)LoadingState.LinearLimit;
            _currentPropertyToken = PropertyToken.None;
            return;

        case "LinearLimitX":
        case "LinearLimitY":
        case "LinearLimitZ":
            _transientLinearLimitPair = default;
            _stateStack[_stateStackPointer++] = (byte)LoadingState.LinearLimitPair;
            _currentPropertyToken = PropertyToken.None;
            return;

        case "TwistLimit":
            _transientAngularLimitPair = default;
            _stateStack[_stateStackPointer++] = (byte)LoadingState.AngularLimitPair;
            _currentPropertyToken = PropertyToken.None;
            return;

        case "SwingLimit":
            _transientLimitCone = default;
            _stateStack[_stateStackPointer++] = (byte)LoadingState.LimitCone;
            _currentPropertyToken = PropertyToken.None;
            return;

        case "PyramidSwingLimit":
            _transientLimitPyramid = default;
            _stateStack[_stateStackPointer++] = (byte)LoadingState.LimitPyramid;
            _currentPropertyToken = PropertyToken.None;
            return;

        case "DriveX":
        case "DriveY":
        case "DriveZ":
        case "DriveSwing":
        case "DriveTwist":
        case "DriveSlerp":
            _transientJointDrive = default;
            _stateStack[_stateStackPointer++] = (byte)LoadingState.JointDrive;
            _currentPropertyToken = PropertyToken.None;
            return;
    }

    _currentPropertyToken = name switch
    {
        "Name" => PropertyToken.Name,
        "Index" => PropertyToken.Index,
        "StaticFriction" => PropertyToken.StaticFriction,
        "DynamicFriction" => PropertyToken.DynamicFriction,
        "Restitution" => PropertyToken.Restitution,
        "DominanceGroup" => PropertyToken.DominanceGroup,
        "W" => PropertyToken.W,
        "X" => PropertyToken.X,
        "Y" => PropertyToken.Y,
        "Z" => PropertyToken.Z,
        "ForceLimit" => PropertyToken.ForceLimit,
        "Stiffness" => PropertyToken.Stiffness,
        "Damping" => PropertyToken.Damping,
        "IsAcceleration" => PropertyToken.IsAcceleration,
        "DriveType" => PropertyToken.DriveType,
        _ => PropertyToken.None
    };
}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnText(ReadOnlySpan<char> text, int line, int column)
    {
        if (_stateStackPointer == 0) 
        {
            return;
        }
    
        var state = (LoadingState)_stateStack[_stateStackPointer - 1];
        ReadOnlySpan<char> cleaned = text.Trim();
        if (cleaned.IsEmpty) 
        {
            return;
        }

        if (state is LoadingState.LinearLimit or LoadingState.LinearLimitPair or LoadingState.Quaternion or LoadingState.JointDrive)
        {
            ParseLimitStructFields(state, cleaned);
            return;
        }

        if (_currentPropertyToken == PropertyToken.None) 
        {
            return;
        }

        switch (state)
        {
            case LoadingState.Material:
                if (_currentPropertyToken == PropertyToken.Index) 
                {
                    _currentMaterialIndex = uint.Parse(cleaned);
                }
                else if (_currentPropertyToken == PropertyToken.StaticFriction) 
                {
                    _activeMaterial = new PxMaterial { Index = _currentMaterialIndex, StaticFriction = float.Parse(cleaned, CultureInfo.InvariantCulture) };
                }
                break;
            case LoadingState.RigidStatic:
            case LoadingState.RigidDynamic:
            case LoadingState.D6Joint:
                if (_currentPropertyToken == PropertyToken.Name) 
                {
                    _activeActorName = ResolveStringFromPool(cleaned);
                }
                else if (_currentPropertyToken == PropertyToken.DominanceGroup) 
                {
                    _activeDominanceGroup = uint.Parse(cleaned);
                }
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ParseLimitStructFields(LoadingState state, ReadOnlySpan<char> cleaned)
{
    if (_currentPropertyToken == PropertyToken.IsAcceleration)
    {
        _transientJointDrive.IsAcceleration = bool.Parse(cleaned);
        return;
    }
    if (_currentPropertyToken == PropertyToken.DriveType)
    {
        if (Enum.TryParse(cleaned, out D6Drive driveType))
        {
            _transientJointDrive.TargetDriveType = driveType;
        }
        return;
    }

    var value = float.Parse(cleaned, CultureInfo.InvariantCulture);
    switch (state)
    {
        case LoadingState.Quaternion:
            if (_currentPropertyToken == PropertyToken.W)
            {
                _activeQuatW = value;
            }
            else if (_currentPropertyToken == PropertyToken.X)
            {
                _activeQuatX = value;
            }
            else if (_currentPropertyToken == PropertyToken.Y)
            {
                _activeQuatY = value;
            }
            else if (_currentPropertyToken == PropertyToken.Z)
            {
                _activeQuatZ = value;
            }
            break;

        case LoadingState.LinearLimit:
            if (_currentPropertyToken == PropertyToken.X)
            {
                _transientLinearLimit.Value = value;
            }
            else if (_currentPropertyToken == PropertyToken.Y)
            {
                _transientLinearLimit.Restitution = value;
            }
            else if (_currentPropertyToken == PropertyToken.Z)
            {
                _transientLinearLimit.Stiffness = value;
            }
            break;
        case LoadingState.LinearLimitPair:
            if (_currentPropertyToken == PropertyToken.X)
            {
                _transientLinearLimitPair.Lower = value;
            }
            else if (_currentPropertyToken == PropertyToken.Y)
            {
                _transientLinearLimitPair.Upper = value;
            }
            else if (_currentPropertyToken == PropertyToken.Z)
            {
                _transientLinearLimitPair.Stiffness = value;
            }
            break;
        case LoadingState.JointDrive:
            if (_currentPropertyToken == PropertyToken.ForceLimit)
            {
                _transientJointDrive.ForceLimit = value;
            }
            else if (_currentPropertyToken == PropertyToken.Stiffness)
            {
                _transientJointDrive.Stiffness = value;
            }
            else if (_currentPropertyToken == PropertyToken.Damping)
            {
                _transientJointDrive.Damping = value;
            }
            break;
    }
}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnEndTag(ReadOnlySpan<char> name, int line, int column)
{
    _currentPropertyToken = PropertyToken.None;

    if (_stateStackPointer == 0)
    {
        return;
    }

    switch (name)
    {
        case "PxMaterial":
            if (_activeMaterial is not null)
            {
                _collectionBuilder[_collectionCount++] = _activeMaterial;
                _materialsBuilder[_materialsCount++] =
                    new KeyValuePair<uint, PxMaterial>(_currentMaterialIndex, _activeMaterial);
                _activeMaterial = null;
            }
            _stateStackPointer--;
            break;

        case "PxRigidStatic":
        case "PxRigidDynamic":
            string actorName = string.IsNullOrEmpty(_activeActorName) ? "UnnamedActor" : _activeActorName;
            ImmutableArray<PxShape> assignedShapes = _shapesCount > 0
                ? ImmutableArray.Create(_shapesBuilder, 0, _shapesCount)
                : ImmutableArray<PxShape>.Empty;

            PxRigidActor actor = _activeActorType == LoadingState.RigidStatic
                ? new PxRigidStatic(actorName) 
                { 
                    DominanceGroup = _activeDominanceGroup, 
                    Shapes = assignedShapes 
                }
                : new PxRigidDynamic(actorName)
                {
                    DominanceGroup = _activeDominanceGroup, 
                    Mass = _activeMass,
                    LinearDamping = _activeLinearDamping, 
                    AngularDamping = _activeAngularDamping,
                    Shapes = assignedShapes
                };

            _collectionBuilder[_collectionCount++] = actor;
            _actorsBuilder[_actorsCount++] = new KeyValuePair<string, PxRigidActor>(actorName, actor);

            _shapesCount = 0;
            _activeActorName = string.Empty;
            _activeDominanceGroup = 0;
            _stateStackPointer--;
            break;

        case "Shape":
            if (_activeShape is not null)
            {
                _shapesBuilder[_shapesCount++] = _activeShape;
                _collectionBuilder[_collectionCount++] = _activeShape;
                _activeShape = null;
            }
            _stateStackPointer--;
            break;

        case "DistanceLimit":
            _activeD6Joint?.DistanceLimit = _transientLinearLimit;
            _stateStackPointer--;
            break;

        case "LinearLimitX":
            _activeD6Joint?.LinearLimitX = _transientLinearLimitPair;
            _stateStackPointer--;
            break;

        case "LinearLimitY":
            _activeD6Joint?.LinearLimitY = _transientLinearLimitPair;
            _stateStackPointer--;
            break;

        case "LinearLimitZ":
            _activeD6Joint?.LinearLimitZ = _transientLinearLimitPair;
            _stateStackPointer--;
            break;

        case "TwistLimit":
            _activeD6Joint?.TwistLimit = _transientAngularLimitPair;
            _stateStackPointer--;
            break;

        case "SwingLimit":
            _activeD6Joint?.SwingLimit = _transientLimitCone;
            _stateStackPointer--;
            break;

        case "PyramidSwingLimit":
            _activeD6Joint?.PyramidSwingLimit = _transientLimitPyramid;
            _stateStackPointer--;
            break;

        case "DriveX":
            _activeD6Joint?.DriveX = _transientJointDrive;
            _stateStackPointer--;
            break;

        case "DriveY":
            _activeD6Joint?.DriveY = _transientJointDrive;
            _stateStackPointer--;
            break;

        case "DriveZ":
            _activeD6Joint?.DriveZ = _transientJointDrive;
            _stateStackPointer--;
            break;

        case "DriveSwing":
            _activeD6Joint?.DriveSwing = _transientJointDrive;
            _stateStackPointer--;
            break;

        case "DriveTwist":
            _activeD6Joint?.DriveTwist = _transientJointDrive;
            _stateStackPointer--;
            break;

        case "DriveSlerp":
            _activeD6Joint?.DriveSlerp = _transientJointDrive;
            _stateStackPointer--;
            break;

        case "Quaternion":
            var parsedRotation = new Quaternion(_activeQuatX, _activeQuatY, _activeQuatZ, _activeQuatW);

            if (_activeShape is not null 
                && _stateStackPointer >= 2 
                && _stateStack[_stateStackPointer - 2] == (byte)LoadingState.Shape)
            {
                _activeShape.LocalPose = new PxTransform(_activeVector, parsedRotation);
            }

            _stateStackPointer--; 
            break;

        case "LocalPose":
        case "GlobalPose":
            _stateStackPointer--;
            break;

        case "PxD6Joint":
            if (_activeD6Joint is not null)
            {
                if (!string.IsNullOrEmpty(_activeActorName))
                {
                    _activeD6Joint.Name = _activeActorName;
                }
                _collectionBuilder[_collectionCount++] = _activeD6Joint;
                _activeD6Joint = null;
            }
            _activeActorName = string.Empty;
            _stateStackPointer--;
            break;

        case "PxArticulationJoint":
            _activeArticulationLink?.InboundJoint = _activeArticulationJoint;
            _stateStackPointer--;
            break;

        case "PxArticulationLink":
            _stateStackPointer--;
            break;

        case "PxArticulation":
            if (_activeArticulation is not null)
            {
                _collectionBuilder[_collectionCount++] = _activeArticulation;
                _activeArticulation = null;
            }
            _stateStackPointer--;
            break;

        case "BG3Physics":
            FinalizeCollections();
            _stateStackPointer--;
            break;
    }
}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FinalizeCollections()
    {
        if (Collection.IsDefaultOrEmpty && _collectionCount > 0)
        {
            Collection = ImmutableArray.Create(_collectionBuilder, 0, _collectionCount);
            Materials = FrozenDictionary.Create(_materialsBuilder.AsSpan(0, _materialsCount));
            Actors = FrozenDictionary.Create(StringComparer.Ordinal, _actorsBuilder.AsSpan(0, _actorsCount));
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FreeRentedBuffers()
    {
        _collectionBuilder.AsSpan(0, _collectionCount).Clear();
        _actorsBuilder.AsSpan(0, _actorsCount).Clear();
        _shapesBuilder.AsSpan(0, _shapesCount).Clear();
        _materialsBuilder.AsSpan(0, _materialsCount).Clear();
        ArrayPool<IPxBase>.Shared.Return(_collectionBuilder);
        ArrayPool<KeyValuePair<uint, PxMaterial>>.Shared.Return(_materialsBuilder);
        ArrayPool<KeyValuePair<string, PxRigidActor>>.Shared.Return(_actorsBuilder);
        ArrayPool<PxShape>.Shared.Return(_shapesBuilder);
        ArrayPool<byte>.Shared.Return(_stateStack);
    }

    public void OnXmlDeclaration(ReadOnlySpan<char> version, ReadOnlySpan<char> encoding, ReadOnlySpan<char> standalone,
    int line, int column)
    {
    }

    public void OnProcessingInstruction(ReadOnlySpan<char> target, ReadOnlySpan<char> data, int line, int column)
    {
    }

    public void OnEndTagEmpty()
    {
    }

    public void OnAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value, int nameLine, int nameColumn,
    int valueLine, int valueColumn)
    {
    }

    public void OnComment(ReadOnlySpan<char> comment, int line, int column)
    {
    }

    public void OnCData(ReadOnlySpan<char> cdata, int line, int column)
    {
    }

    public void OnError(string message, int line, int column)
    {
    }
}