using System.Diagnostics;
using LSLib.Granny.GR2;
using LSLib.LS;
using OpenTK.Mathematics;

namespace LSLib.Granny.Model;

internal class ColladaSource
{
    public String id;
    public Dictionary<String, List<Single>> FloatParams = new Dictionary<string, List<float>>();
    public Dictionary<String, List<Matrix4>> MatrixParams = new Dictionary<string, List<Matrix4>>();
    public Dictionary<String, List<String>> NameParams = new Dictionary<string, List<string>>();

    public static ColladaSource FromCollada(source src)
    {
        var source = new ColladaSource
        {
            id = src.id
        };

        var accessor = src.technique_common.accessor;
        // TODO: check src.#ID?

        float_array floats = null;
        Name_array names = null;
        if (src.Item is float_array)
        {
            floats = src.Item as float_array;
            // Workaround for empty arrays being null
            floats.Values ??= [];

            if ((int)floats.count != floats.Values.Length || floats.count < accessor.stride * accessor.count + accessor.offset)
                throw new ParsingException("Float source data size mismatch. Check source and accessor item counts.");
        }
        else if (src.Item is Name_array)
        {
            names = src.Item as Name_array;
            // Workaround for empty arrays being null
            names.Values ??= [];

            if ((int)names.count != names.Values.Length || names.count < accessor.stride * accessor.count + accessor.offset)
                throw new ParsingException("Name source data size mismatch. Check source and accessor item counts.");
        }
        else
            throw new ParsingException("Unsupported source data format.");

        var paramOffset = 0;
        foreach (var param in accessor.param)
        {
            param.name ??= "default";
            if (param.type == "float" || param.type == "double")
            {
                var items = new List<Single>((int)accessor.count);
                var offset = (int)accessor.offset;
                for (var i = 0; i < (int)accessor.count; i++)
                {
                    items.Add((float)floats.Values[offset + paramOffset]);
                    offset += (int)accessor.stride;
                }

                source.FloatParams.Add(param.name, items);
            }
            else if (param.type == "float4x4")
            {
                var items = new List<Matrix4>((int)accessor.count);
                var offset = (int)accessor.offset;
                for (var i = 0; i < (int)accessor.count; i++)
                {
                    var itemOff = offset + paramOffset;
                    var mat = new Matrix4(
                        (float)floats.Values[itemOff + 0], (float)floats.Values[itemOff + 1], (float)floats.Values[itemOff + 2], (float)floats.Values[itemOff + 3],
                        (float)floats.Values[itemOff + 4], (float)floats.Values[itemOff + 5], (float)floats.Values[itemOff + 6], (float)floats.Values[itemOff + 7],
                        (float)floats.Values[itemOff + 8], (float)floats.Values[itemOff + 9], (float)floats.Values[itemOff + 10], (float)floats.Values[itemOff + 11],
                        (float)floats.Values[itemOff + 12], (float)floats.Values[itemOff + 13], (float)floats.Values[itemOff + 14], (float)floats.Values[itemOff + 15]
                    );
                    items.Add(mat);
                    offset += (int)accessor.stride;
                }

                source.MatrixParams.Add(param.name, items);
            }
            else if (param.type.ToLower() == "name")
            {
                var items = new List<String>((int)accessor.count);
                var offset = (int)accessor.offset;
                for (var i = 0; i < (int)accessor.count; i++)
                {
                    items.Add(names.Values[offset + paramOffset]);
                    offset += (int)accessor.stride;
                }

                source.NameParams.Add(param.name, items);
            }
            else
                throw new ParsingException("Unsupported accessor param type: " + param.type);

            paramOffset++;
        }

        return source;
    }
}

class RootBoneInfo
{
    public node Bone;
    public List<node> Parents;
};

public class ColladaImporter
{
    [Serialization(Kind = SerializationKind.None)]
    public ExporterOptions Options = new();

    private bool ZUp = false;

    [Serialization(Kind = SerializationKind.None)]
    public Dictionary<string, Mesh> ColladaGeometries;

    [Serialization(Kind = SerializationKind.None)]
    public HashSet<string> SkinnedMeshes;

    private ArtToolInfo ImportArtToolInfo(COLLADA collada)
    {
        ZUp = false;
        var toolInfo = new ArtToolInfo
        {
            FromArtToolName = "Unknown",
            ArtToolMajorRevision = 1,
            ArtToolMinorRevision = 0,
            ArtToolPointerSize = Options.Is64Bit ? 64 : 32,
            Origin = [0, 0, 0]
        };
        toolInfo.SetYUp();

        if (collada.asset != null)
        {
            if (collada.asset.unit != null)
            {
                if (collada.asset.unit.name == "meter")
                    toolInfo.UnitsPerMeter = (float)collada.asset.unit.meter;
                else if (collada.asset.unit.name == "centimeter")
                    toolInfo.UnitsPerMeter = (float)collada.asset.unit.meter * 100;
                else
                    throw new NotImplementedException("Unsupported asset unit type: " + collada.asset.unit.name);
            }

            if (collada.asset.contributor != null && collada.asset.contributor.Length > 0)
            {
                var contributor = collada.asset.contributor.First();
                if (contributor.authoring_tool != null)
                    toolInfo.FromArtToolName = contributor.authoring_tool;
            }

            switch (collada.asset.up_axis)
            {
                case UpAxisType.X_UP:
                    throw new Exception("X-up not supported yet!");

                case UpAxisType.Y_UP:
                    toolInfo.SetYUp();
                    break;

                case UpAxisType.Z_UP:
                    ZUp = true;
                    toolInfo.SetZUp();
                    break;
            }
        }

        return toolInfo;
    }

    private ExporterInfo ImportExporterInfo(COLLADA collada)
    {
        return new ExporterInfo
        {
            ExporterName = $"LSLib GR2 Exporter v{Common.LibraryVersion()}",
            ExporterMajorRevision = Common.MajorVersion,
            ExporterMinorRevision = Common.MinorVersion,
            ExporterBuildNumber = 0,
            ExporterCustomization = Common.PatchVersion
        };
    }

    private DivinityModelFlag DetermineSkeletonModelFlagsFromModels(Root root, Skeleton skeleton, DivinityModelFlag meshFlagOverrides)
    {
        DivinityModelFlag accumulatedFlags = 0;
        foreach (var model in root.Models ?? Enumerable.Empty<Model>())
        {
            if (model.Skeleton == skeleton && model.MeshBindings != null)
            {
                foreach (var meshBinding in model.MeshBindings)
                {
                    accumulatedFlags |= meshBinding.Mesh?.ExtendedData?.UserMeshProperties?.MeshFlags ?? meshFlagOverrides;
                }
            }
        }

        return accumulatedFlags;
    }

    private void BuildExtendedData(Root root)
    {
        if (Options.ModelInfoFormat == DivinityModelInfoFormat.None)
        {
            return;
        }

        var modelFlagOverrides = Options.ModelType;

        foreach (var mesh in root.Meshes ?? Enumerable.Empty<Mesh>())
        {
            DivinityModelFlag modelFlags = modelFlagOverrides;
            if (modelFlags == 0 && mesh.ExtendedData != null)
            {
                modelFlags = mesh.ExtendedData.UserMeshProperties.MeshFlags;
            }

            mesh.ExtendedData ??= DivinityMeshExtendedData.Make();
            mesh.ExtendedData.UserMeshProperties.MeshFlags = modelFlags;
            mesh.ExtendedData.UpdateFromModelInfo(mesh, Options.ModelInfoFormat);
        }

        foreach (var skeleton in root.Skeletons ?? Enumerable.Empty<Skeleton>())
        {
            if (Options.ModelInfoFormat == DivinityModelInfoFormat.None || Options.ModelInfoFormat == DivinityModelInfoFormat.LSMv3)
            {
                foreach (var bone in skeleton.Bones ?? Enumerable.Empty<Bone>())
                {
                    bone.ExtendedData = null;
                }
            }
            else
            {
                var accumulatedFlags = DetermineSkeletonModelFlagsFromModels(root, skeleton, modelFlagOverrides);

                foreach (var bone in skeleton.Bones ?? Enumerable.Empty<Bone>())
                {
                    bone.ExtendedData ??= new DivinityBoneExtendedData();
                    var userDefinedProperties = UserDefinedPropertiesHelpers.MeshFlagsToUserDefinedProperties(accumulatedFlags);
                    bone.ExtendedData.UserDefinedProperties = userDefinedProperties;
                    bone.ExtendedData.IsRigid = (accumulatedFlags.IsRigid()) ? 1 : 0;
                }
            }
        }
    }

    private void FindRootBones(List<node> parents, node node, List<RootBoneInfo> rootBones)
    {
        if (node.type == NodeType.JOINT)
        {
            var root = new RootBoneInfo
            {
                Bone = node,
                Parents = parents.Select(a => a).ToList()
            };
            rootBones.Add(root);
        }
        else if (node.type == NodeType.NODE)
        {
            if (node.node1 != null)
            {
                parents.Add(node);
                foreach (var child in node.node1)
                {
                    FindRootBones(parents, child, rootBones);
                }
                parents.RemoveAt(parents.Count - 1);
            }
        }
    }

    public static technique FindExporterExtraData(extra[] extras)
    {
        foreach (var extra in extras ?? Enumerable.Empty<extra>())
        {
            foreach (var technique in extra.technique ?? Enumerable.Empty<technique>())
            {
                if (technique.profile == "LSTools")
                {
                    return technique;
                }
            }
        }

        return null;
    }

    private void LoadLSLibProfileMeshType(DivinityMeshExtendedData props, string meshType)
    {
        var meshProps = props.UserMeshProperties;

        switch (meshType)
        {
            // Compatibility flag, not used anymore
            case "Normal": break;
            case "Cloth": meshProps.MeshFlags |= DivinityModelFlag.Cloth; props.Cloth = 1; break;
            case "Rigid": meshProps.MeshFlags |= DivinityModelFlag.Rigid; props.Rigid = 1; break;
            case "MeshProxy": meshProps.MeshFlags |= DivinityModelFlag.MeshProxy | DivinityModelFlag.HasProxyGeometry; props.MeshProxy = 1; break;
            case "ProxyGeometry": meshProps.MeshFlags |= DivinityModelFlag.HasProxyGeometry; break;
            case "Spring": meshProps.MeshFlags |= DivinityModelFlag.Spring; props.Spring = 1; break;
            case "Occluder": meshProps.MeshFlags |= DivinityModelFlag.Occluder; props.Occluder = 1; break;
            case "Cloth01": meshProps.ClothFlags |= DivinityClothFlag.Cloth01; break;
            case "Cloth02": meshProps.ClothFlags |= DivinityClothFlag.Cloth02; break;
            case "Cloth04": meshProps.ClothFlags |= DivinityClothFlag.Cloth04; break;
            case "ClothPhysics": meshProps.ClothFlags |= DivinityClothFlag.ClothPhysics; break;
            default:
                Utils.Warn($"Unrecognized model type in <DivModelType> tag: {meshType}");
                break;
        }
    }

    private void LoadLSLibProfileExportOrder(Mesh mesh, string order)
    {
        if (Int32.TryParse(order, out int parsedOrder))
        {
            if (parsedOrder >= 0 && parsedOrder < 100)
            {
                mesh.ExportOrder = parsedOrder;
            }
        }
    }

    private void LoadLSLibProfileLOD(DivinityMeshExtendedData props, string lod)
    {
        if (Int32.TryParse(lod, out int parsedLod))
        {
            if (parsedLod >= 0 && parsedLod < 100)
            {
                props.LOD = parsedLod;
                if (parsedLod == 0)
                {
                    props.UserMeshProperties.Lod[0] = -1;
                }
                else
                {
                    props.UserMeshProperties.Lod[0] = parsedLod;
                }
            }
        }
    }

    private void LoadLSLibProfileImpostor(DivinityMeshExtendedData props, string impostor)
    {
        if (Int32.TryParse(impostor, out int isImpostor))
        {
            if (isImpostor == 1)
            {
                props.UserMeshProperties.IsImpostor[0] = 1;
            }
        }
    }

    private void LoadLSLibProfileLODDistance(DivinityMeshProperties props, string lodDistance)
    {
        if (Single.TryParse(lodDistance, out float parsedLodDistance))
        {
            if (parsedLodDistance >= 0.0f)
            {
                props.LodDistance[0] = parsedLodDistance;
            }
        }
    }

    private void MakeExtendedData(mesh mesh, Mesh loaded)
    {
        var modelFlagOverrides = Options.ModelType;

        DivinityModelFlag modelFlags = modelFlagOverrides;
        if (modelFlags == 0 && loaded.ExtendedData != null)
        {
            modelFlags = loaded.ExtendedData.UserMeshProperties.MeshFlags;
        }

        loaded.ExtendedData = DivinityMeshExtendedData.Make();
        loaded.ExtendedData.UserMeshProperties.MeshFlags = modelFlags;
        loaded.ExtendedData.UpdateFromModelInfo(loaded, Options.ModelInfoFormat);
        LoadColladaLSLibProfileData(mesh, loaded);
    }

    private void LoadColladaLSLibProfileData(mesh mesh, Mesh loaded)
    {
        var technique = FindExporterExtraData(mesh.extra);
        if (technique == null || technique.Any == null) return;

        var meshProps = loaded.ExtendedData.UserMeshProperties;

        foreach (var setting in technique.Any)
        {
            switch (setting.LocalName)
            {
                case "DivModelType":
                    LoadLSLibProfileMeshType(loaded.ExtendedData, setting.InnerText.Trim());
                    break;
                    
                case "IsImpostor":
                    LoadLSLibProfileImpostor(loaded.ExtendedData, setting.InnerText.Trim());
                    break;

                case "ExportOrder":
                    LoadLSLibProfileExportOrder(loaded, setting.InnerText.Trim());
                    break;

                case "LOD":
                    LoadLSLibProfileLOD(loaded.ExtendedData, setting.InnerText.Trim());
                    break;
                    
                case "LODDistance":
                    LoadLSLibProfileLODDistance(meshProps, setting.InnerText.Trim());
                    break;

                default:
                    Utils.Warn($"Unrecognized LSLib profile attribute: {setting.LocalName}");
                    break;
            }
        }
    }

    private void ValidateLSLibProfileMetadataVersion(string ver)
    {
        if (Int32.TryParse(ver, out int version))
        {
            if (version > Common.ColladaMetadataVersion)
            {
                throw new ParsingException(
                    $"Collada file is using a newer LSLib metadata format than this LSLib version supports, please upgrade.\r\n" +
                    $"File version: {version}, exporter version: {Common.ColladaMetadataVersion}");
            }
        }
    }

    private void LoadColladaLSLibProfileData(COLLADA collada)
    {
        var technique = FindExporterExtraData(collada.extra);
        if (technique == null || technique.Any == null) return;

        foreach (var setting in technique.Any)
        {
            switch (setting.LocalName)
            {
                case "MetadataVersion":
                    ValidateLSLibProfileMetadataVersion(setting.InnerText.Trim());
                    break;

                case "LSLibMajor":
                case "LSLibMinor":
                case "LSLibPatch":
                    break;

                default:
                    Utils.Warn($"Unrecognized LSLib root profile attribute: {setting.LocalName}");
                    break;
            }
        }
    }

    private Mesh ImportMesh(geometry geom, mesh mesh, VertexDescriptor vertexFormat)
    {
        var collada = new ColladaMesh();
        bool isSkinned = SkinnedMeshes.Contains(geom.id);
        collada.ImportFromCollada(mesh, vertexFormat, isSkinned, Options);

        var m = new Mesh
        {
            VertexFormat = collada.InternalVertexType,
            Name = "Unnamed",

            PrimaryVertexData = new VertexData
            {
                Vertices = collada.ConsolidatedVertices
            },

            PrimaryTopology = new TriTopology
            {
                Indices = collada.ConsolidatedIndices,
                Groups = [
                    new TriTopologyGroup
                    {
                        MaterialIndex = 0,
                        TriFirst = 0,
                        TriCount = collada.TriangleCount
                    }
                ]
            },

            MaterialBindings = [new MaterialBinding()],
            OriginalToConsolidatedVertexIndexMap = collada.OriginalToConsolidatedVertexIndexMap
        };

        if (!Options.StripMetadata)
        {
            var components = m.VertexFormat.ComponentNames().Select(s => new GrannyString(s)).ToList();
            m.PrimaryVertexData.VertexComponentNames = components;
        }
        else
        {
            m.PrimaryVertexData.VertexComponentNames = null;
        }

        MakeExtendedData(mesh, m);

        Utils.Info(String.Format("Imported {0} mesh ({1} tri groups, {2} tris)", 
            (m.VertexFormat.HasBoneWeights ? "skinned" : "rigid"), 
            m.PrimaryTopology.Groups.Count, 
            collada.TriangleCount));

        return m;
    }

    private Mesh ImportMesh(Root root, string name, geometry geom, mesh mesh, VertexDescriptor vertexFormat)
    {
        var m = ImportMesh(geom, mesh, vertexFormat);
        m.Name = name;
        root.VertexDatas.Add(m.PrimaryVertexData);
        root.TriTopologies.Add(m.PrimaryTopology);
        root.Meshes.Add(m);
        return m;
    }

    private void ImportSkin(Root root, skin skin)
    {
        if (skin.source1[0] != '#')
            throw new ParsingException("Only ID references are supported for skin geometries");

        if (!ColladaGeometries.TryGetValue(skin.source1[1..], out Mesh mesh))
            throw new ParsingException("Skin references nonexistent mesh: " + skin.source1);

        if (!mesh.VertexFormat.HasBoneWeights)
        {
            var msg = String.Format("Tried to apply skin to mesh ({0}) with non-skinned vertices", 
                mesh.Name);
            throw new ParsingException(msg);
        }

        var sources = new Dictionary<String, ColladaSource>();
        foreach (var source in skin.source)
        {
            var src = ColladaSource.FromCollada(source);
            sources.Add(src.id, src);
        }

        List<Bone> joints = null;
        List<Matrix4> invBindMatrices = null;
        foreach (var input in skin.joints.input)
        {
            if (input.source[0] != '#')
                throw new ParsingException("Only ID references are supported for joint input sources");

            if (!sources.TryGetValue(input.source.Substring(1), out ColladaSource inputSource))
                throw new ParsingException("Joint input source does not exist: " + input.source);

            if (input.semantic == "JOINT")
            {
                List<string> jointNames = inputSource.NameParams.Values.SingleOrDefault();
                if (jointNames == null)
                    throw new ParsingException("Joint input source 'JOINT' must contain array of names.");

                var skeleton = root.Skeletons[0];
                joints = [];
                foreach (var name in jointNames)
                {
                    var lookupName = name.Replace("_x0020_", " ");
                    if (!skeleton.BonesBySID.TryGetValue(lookupName, out Bone bone))
                        throw new ParsingException("Joint name list references nonexistent bone: " + lookupName);

                    joints.Add(bone);
                }
            }
            else if (input.semantic == "INV_BIND_MATRIX")
            {
                invBindMatrices = inputSource.MatrixParams.Values.SingleOrDefault();
                if (invBindMatrices == null)
                    throw new ParsingException("Joint input source 'INV_BIND_MATRIX' must contain a single array of matrices.");
            }
            else
            {
                throw new ParsingException("Unsupported joint semantic: " + input.semantic);
            }
        }

        if (joints == null)
            throw new ParsingException("Required joint input semantic missing: JOINT");

        if (invBindMatrices == null)
            throw new ParsingException("Required joint input semantic missing: INV_BIND_MATRIX");

        var influenceCounts = ColladaHelpers.StringsToIntegers(skin.vertex_weights.vcount);
        var influences = ColladaHelpers.StringsToIntegers(skin.vertex_weights.v);

        foreach (var count in influenceCounts)
        {
            if (count > Vertex.MaxBoneInfluences)
                throw new ParsingException($"GR2 only supports at most {Vertex.MaxBoneInfluences} vertex influences");
        }

        // TODO
        if (influenceCounts.Count != mesh.OriginalToConsolidatedVertexIndexMap.Count)
            Utils.Warn(String.Format("Vertex influence count ({0}) differs from vertex count ({1})", influenceCounts.Count, mesh.OriginalToConsolidatedVertexIndexMap.Count));

        List<Single> weights = null;

        int jointInputIndex = -1, weightInputIndex = -1;
        foreach (var input in skin.vertex_weights.input)
        {
            if (input.semantic == "JOINT")
            {
                jointInputIndex = (int)input.offset;
            }
            else if (input.semantic == "WEIGHT")
            {
                weightInputIndex = (int)input.offset;

                if (input.source[0] != '#')
                    throw new ParsingException("Only ID references are supported for weight input sources");

                if (!sources.TryGetValue(input.source[1..], out ColladaSource inputSource))
                    throw new ParsingException("Weight input source does not exist: " + input.source);

                if (!inputSource.FloatParams.TryGetValue("WEIGHT", out weights))
                    weights = inputSource.FloatParams.Values.SingleOrDefault();

                if (weights == null)
                    throw new ParsingException("Weight input source " + input.source + " must have WEIGHT float attribute");
            }
            else
                throw new ParsingException("Unsupported skin input semantic: " + input.semantic);
        }

        if (jointInputIndex == -1)
            throw new ParsingException("Required vertex weight input semantic missing: JOINT");

        if (weightInputIndex == -1)
            throw new ParsingException("Required vertex weight input semantic missing: WEIGHT");

        // Remove bones that are not actually influenced from the binding list
        var boundBones = new HashSet<Bone>();
        int offset = 0;
        int stride = skin.vertex_weights.input.Length;
        while (offset < influences.Count)
        {
            var jointIndex = influences[offset + jointInputIndex];
            var weightIndex = influences[offset + weightInputIndex];
            var joint = joints[jointIndex];
            var weight = weights[weightIndex];
            boundBones.Add(joint);

            offset += stride;
        }

        if (boundBones.Count > 127)
            throw new ParsingException("D:OS supports at most 127 bound bones per mesh.");

        mesh.BoneBindings = [];
        var boneToIndexMaps = new Dictionary<Bone, int>();
        for (var i = 0; i < joints.Count; i++)
        {
            if (boundBones.Contains(joints[i]))
            {
                // Collada allows one inverse bind matrix for each skin, however Granny
                // only has one matrix for one bone, even if said bone is used from multiple meshes.
                // Hopefully the Collada ones are all equal ...
                var iwt = invBindMatrices[i];
                // iwt.Transpose();
                joints[i].InverseWorldTransform = [
                    iwt[0, 0], iwt[1, 0], iwt[2, 0], iwt[3, 0],
                    iwt[0, 1], iwt[1, 1], iwt[2, 1], iwt[3, 1],
                    iwt[0, 2], iwt[1, 2], iwt[2, 2], iwt[3, 2],
                    iwt[0, 3], iwt[1, 3], iwt[2, 3], iwt[3, 3]
                ];

                // Bind all bones that affect vertices to the mesh, so we can reference them
                // later from the vertexes BoneIndices.
                var binding = new BoneBinding
                {
                    BoneName = joints[i].Name,
                    // TODO
                    // Use small bounding box values, as it interferes with object placement
                    // in D:OS 2 (after the Gift Bag 2 update)
                    OBBMin = [-0.1f, -0.1f, -0.1f],
                    OBBMax = [0.1f, 0.1f, 0.1f]
                };
                mesh.BoneBindings.Add(binding);
                boneToIndexMaps.Add(joints[i], boneToIndexMaps.Count);
            }
        }

        Span<float> vertErrors = stackalloc float[Vertex.MaxBoneInfluences];
        Span<byte> vertWeights = stackalloc byte[Vertex.MaxBoneInfluences];

        offset = 0;
        for (var vertexIndex = 0; vertexIndex < influenceCounts.Count; vertexIndex++)
        {
            var influenceCount = influenceCounts[vertexIndex];
            float influenceSum = 0.0f;
            for (var i = 0; i < influenceCount; i++)
            {
                var weightIndex = influences[offset + i * stride + weightInputIndex];
                influenceSum += weights[weightIndex];
            }

            byte totalEncoded = 0;
            for (var i = 0; i < influenceCount; i++)
            {
                var weightIndex = influences[offset + i * stride + weightInputIndex];
                var weight = weights[weightIndex] / influenceSum * 255.0f;
                var encodedWeight = (byte)Math.Round(weight);
                totalEncoded += encodedWeight;
                vertErrors[i] = Math.Abs(encodedWeight - weight);
                vertWeights[i] = encodedWeight;
            }

            while (totalEncoded != 0 && totalEncoded < 255)
            {
                float firstHighest = 0.0f;
                int errorIndex = -1;
                for (var i = 0; i < influenceCount; i++)
                {
                    if (vertErrors[i] > firstHighest)
                    {
                        firstHighest = vertErrors[i];
                        errorIndex = i;
                    }
                }

                var weightIndex = influences[offset + errorIndex * stride + weightInputIndex];
                var weight = weights[weightIndex] / influenceSum * 255.0f;

                vertWeights[errorIndex]++;
                vertErrors[errorIndex] = Math.Abs(vertWeights[errorIndex] - weight);
                totalEncoded++;
            }

            Debug.Assert(totalEncoded == 0 || totalEncoded == 255);

            for (var i = 0; i < influenceCount; i++)
            {
                // Not all vertices are actually used in triangles, we may have unused verts in the
                // source list (though this is rare) which won't show up in the consolidated vertex map.
                if (mesh.OriginalToConsolidatedVertexIndexMap.TryGetValue(vertexIndex, out List<int> consolidatedIndices))
                {
                    var jointIndex = influences[offset + jointInputIndex];
                    var joint = joints[jointIndex];

                    foreach (var consolidatedIndex in consolidatedIndices)
                    {
                        var vertex = mesh.PrimaryVertexData.Vertices[consolidatedIndex];
                        vertex.AddInfluence((byte)boneToIndexMaps[joint], vertWeights[i]);
                    }
                }

                offset += stride;
            }
        }

        foreach (var vertex in mesh.PrimaryVertexData.Vertices)
        {
            vertex.FinalizeInfluences();
        }

        // Warn if we have vertices that are not influenced by any bone
        int notInfluenced = 0;
        foreach (var vertex in mesh.PrimaryVertexData.Vertices)
        {
            if (vertex.BoneWeights[0] == 0) notInfluenced++;
        }

        if (notInfluenced > 0)
            Utils.Warn(String.Format("{0} vertices are not influenced by any bone", notInfluenced));

        if (skin.bind_shape_matrix != null)
        {
            var bindShapeFloats = skin.bind_shape_matrix.Trim().Split([' ']).Select(s => Single.Parse(s)).ToArray();
            var bindShapeMat = ColladaHelpers.FloatsToMatrix(bindShapeFloats);
            bindShapeMat.Transpose();

            // Deform geometries that were affected by our bind shape matrix
            mesh.PrimaryVertexData.Transform(bindShapeMat);
        }

        if (Options.RecalculateOBBs)
        {
            UpdateOBBs(root.Skeletons.Single(), mesh);
        }
    }

    class OBB
    {
        public Vector3 Min, Max;
        public int NumVerts = 0;
    }

    private void UpdateOBBs(Skeleton skeleton, Mesh mesh)
    {
        if (mesh.BoneBindings == null || mesh.BoneBindings.Count == 0) return;
        
        var obbs = new List<OBB>(mesh.BoneBindings.Count);
        for (var i = 0; i < mesh.BoneBindings.Count; i++)
        {
            obbs.Add(new OBB
            {
                Min = new Vector3(1000.0f, 1000.0f, 1000.0f),
                Max = new Vector3(-1000.0f, -1000.0f, -1000.0f),
            });
        }
        
        foreach (var vert in mesh.PrimaryVertexData.Vertices)
        {
            for (var i = 0; i < Vertex.MaxBoneInfluences; i++)
            {
                if (vert.BoneWeights[i] > 0)
                {
                    var bi = vert.BoneIndices[i];
                    var obb = obbs[bi];
                    obb.NumVerts++;

                    var bone = skeleton.GetBoneByName(mesh.BoneBindings[bi].BoneName);
                    var invWorldTransform = ColladaHelpers.FloatsToMatrix(bone.InverseWorldTransform);
                    var transformed = Vector3.TransformPosition(vert.Position, invWorldTransform);

                    obb.Min.X = Math.Min(obb.Min.X, transformed.X);
                    obb.Min.Y = Math.Min(obb.Min.Y, transformed.Y);
                    obb.Min.Z = Math.Min(obb.Min.Z, transformed.Z);

                    obb.Max.X = Math.Max(obb.Max.X, transformed.X);
                    obb.Max.Y = Math.Max(obb.Max.Y, transformed.Y);
                    obb.Max.Z = Math.Max(obb.Max.Z, transformed.Z);
                }
            }
        }

        for (var i = 0; i < obbs.Count; i++)
        {
            var obb = obbs[i];
            if (obb.NumVerts > 0)
            {
                mesh.BoneBindings[i].OBBMin = [obb.Min.X, obb.Min.Y, obb.Min.Z];
                mesh.BoneBindings[i].OBBMax = [obb.Max.X, obb.Max.Y, obb.Max.Z];
            }
            else
            {
                mesh.BoneBindings[i].OBBMin = [0.0f, 0.0f, 0.0f];
                mesh.BoneBindings[i].OBBMax = [0.0f, 0.0f, 0.0f];
            }
        }
    }

    private void LoadColladaLSLibProfileData(animation anim, TrackGroup loaded)
    {
        var technique = FindExporterExtraData(anim.extra);
        if (technique == null || technique.Any == null) return;

        foreach (var setting in technique.Any)
        {
            switch (setting.LocalName)
            {
                case "SkeletonResourceID":
                    loaded.ExtendedData = new BG3TrackGroupExtendedData
                    {
                        SkeletonResourceID = setting.InnerText.Trim()
                    };
                    break;

                default:
                    Utils.Warn($"Unrecognized LSLib animation profile attribute: {setting.LocalName}");
                    break;
            }
        }
    }

    public void ImportAnimations(IEnumerable<animation> anims, Root root, Skeleton skeleton)
    {
        var trackGroup = new TrackGroup
        {
            Name = (skeleton != null) ? skeleton.Name : "Dummy_Root",
            TransformTracks = [],
            InitialPlacement = new Transform(),
            AccumulationFlags = 2,
            LoopTranslation = [0, 0, 0]
        };

        var animation = new Animation
        {
            Name = "Default",
            TimeStep = 0.016667f, // 60 FPS
            Oversampling = 1,
            DefaultLoopCount = 1,
            Flags = 1,
            Duration = .0f,
            TrackGroups = [trackGroup]
        };

        foreach (var colladaTrack in anims)
        {
            ImportAnimation(colladaTrack, animation, trackGroup, skeleton);
        }

        if (trackGroup.TransformTracks.Count > 0)
        {
            // Reorder transform tracks in lexicographic order
            // This is needed by Granny; otherwise it'll fail to find animation tracks
            trackGroup.TransformTracks.Sort((t1, t2) => String.Compare(t1.Name, t2.Name, StringComparison.Ordinal));
            
            root.TrackGroups.Add(trackGroup);
            root.Animations.Add(animation);
        }
    }

    public void ImportAnimation(animation colladaAnim, Animation animation, TrackGroup trackGroup, Skeleton skeleton)
    {
        var childAnims = 0;
        foreach (var item in colladaAnim.Items)
        {
            if (item is animation)
            {
                ImportAnimation(item as animation, animation, trackGroup, skeleton);
                childAnims++;
            }
        }

        var duration = .0f;
        if (childAnims < colladaAnim.Items.Length)
        {
            ColladaAnimation importAnim = new();
            if (importAnim.ImportFromCollada(colladaAnim, skeleton))
            {
                duration = Math.Max(duration, importAnim.Duration);
                var track = importAnim.MakeTrack(Options.RemoveTrivialAnimationKeys);
                trackGroup.TransformTracks.Add(track);
                LoadColladaLSLibProfileData(colladaAnim, trackGroup);
            }
        }

        animation.Duration = Math.Max(animation.Duration, duration);
    }

    public Root Import(string inputPath)
    {
        COLLADA collada = null;
        using (var stream = File.OpenRead(inputPath))
        {
            collada = COLLADA.Load(stream);
        }

        LoadColladaLSLibProfileData(collada);

        var root = new Root
        {
            ArtToolInfo = ImportArtToolInfo(collada),
            ExporterInfo = Options.StripMetadata ? null : ImportExporterInfo(collada),

            FromFileName = inputPath,

            Skeletons = [],
            VertexDatas = [],
            TriTopologies = [],
            Meshes = [],
            Models = [],
            TrackGroups = [],
            Animations = []
        };

        ColladaGeometries = [];
        SkinnedMeshes = [];

        var collGeometries = new List<geometry>();
        var collSkins = new List<skin>();
        var collNodes = new List<node>();
        var collAnimations = new List<animation>();
        var rootBones = new List<RootBoneInfo>();

        // Import skinning controllers after skeleton and geometry loading has finished, as
        // we reference both of them during skin import
        foreach (var item in collada.Items)
        {
            if (item is library_controllers)
            {
                var controllers = item as library_controllers;
                if (controllers.controller != null)
                {
                    foreach (var controller in controllers.controller)
                    {
                        if (controller.Item is skin)
                        {
                            collSkins.Add(controller.Item as skin);
                            SkinnedMeshes.Add((controller.Item as skin).source1[1..]);
                        }
                        else
                        {
                            Utils.Warn(String.Format("Controller {0} is unsupported and will be ignored", controller.Item.GetType().Name));
                        }
                    }
                }
            }
            else if (item is library_visual_scenes)
            {
                var scenes = item as library_visual_scenes;
                if (scenes.visual_scene != null)
                {
                    foreach (var scene in scenes.visual_scene)
                    {
                        if (scene.node != null)
                        {
                            foreach (var node in scene.node)
                            {
                                collNodes.Add(node);
                                FindRootBones([], node, rootBones);
                            }
                        }
                    }
                }
            }
            else if (item is library_geometries)
            {
                var geometries = item as library_geometries;
                if (geometries.geometry != null)
                {
                    foreach (var geometry in geometries.geometry)
                    {
                        if (geometry.Item is mesh)
                        {
                            collGeometries.Add(geometry);
                        }
                        else
                        {
                            Utils.Warn(String.Format("Geometry type {0} is unsupported and will be ignored", geometry.Item.GetType().Name));
                        }
                    }
                }
            }
            else if (item is library_animations)
            {
                var animations = item as library_animations;
                if (animations.animation != null)
                {
                    collAnimations.AddRange(animations.animation);
                }
            }
            else
            {
                Utils.Warn($"Library {item.GetType().Name} is unsupported and will be ignored");
            }
        }

        foreach (var bone in rootBones)
        {
            var skeleton = Skeleton.FromCollada(bone.Bone);
            var rootTransform = NodeHelpers.GetTransformHierarchy(bone.Parents);
            skeleton.TransformRoots(rootTransform.Inverted());
            skeleton.ReorderBones();
            root.Skeletons.Add(skeleton);
        }

        foreach (var geometry in collGeometries)
        {
            // Use the override vertex format, if one was specified
            Options.VertexFormats.TryGetValue(geometry.name, out VertexDescriptor vertexFormat);
            var mesh = ImportMesh(root, geometry.name, geometry, geometry.Item as mesh, vertexFormat);
            ColladaGeometries.Add(geometry.id, mesh);
        }

        // Reorder meshes based on their ExportOrder
        if (root.Meshes.Any(m => m.ExportOrder > -1))
        {
            root.Meshes.Sort((a, b) => a.ExportOrder - b.ExportOrder);
        }

        // Import skinning controllers after skeleton and geometry loading has finished, as
        // we reference both of them during skin import
        if (rootBones.Count > 0)
        {
            foreach (var skin in collSkins)
            {
                ImportSkin(root, skin);
            }
        }

        if (collAnimations.Count > 0)
        {
            ImportAnimations(collAnimations, root, root.Skeletons.FirstOrDefault());
        }

        var rootModel = new Model();
        rootModel.Name = "Unnamed"; // TODO
        if (root.Skeletons.Count > 0)
        {
            rootModel.Skeleton = root.Skeletons[0];
            rootModel.Name = rootModel.Skeleton.Bones[0].Name;
        }
        rootModel.InitialPlacement = new Transform();
        rootModel.MeshBindings = new List<MeshBinding>();
        foreach (var mesh in root.Meshes)
        {
            var binding = new MeshBinding();
            binding.Mesh = mesh;
            rootModel.MeshBindings.Add(binding);
        }

        root.Models.Add(rootModel);
        // TODO: make this an option!
        if (root.Skeletons.Count > 0)
            root.Skeletons[0].UpdateWorldTransforms();
        root.ZUp = ZUp;
        root.PostLoad(GR2.Header.DefaultTag);

        BuildExtendedData(root);

        return root;
    }
}
