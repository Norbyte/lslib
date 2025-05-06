using LSLib.Granny.GR2;
using LSLib.LS;
using LSLib.LS.Enums;
using OpenTK.Mathematics;

namespace LSLib.Granny.Model;

public class ExportException(string message) : Exception(message)
{
}

public enum ExportFormat
{
    GR2,
    DAE,
    GLTF,
    GLB
};

public enum DivinityModelInfoFormat
{
    // No ExtendedInfo on bones and meshes
    None,
    // User the UserDefinedProperties string to add properties
    UserDefinedProperties,
    // Use LSM UserMeshProperties
    LSMv0,
    // Use LSM UserMeshProperties and FormatDescs
    LSMv1,
    // Use BG3 extended LSM UserMeshProperties and FormatDescs
    LSMv3
};

public class ExporterOptions
{
    public string InputPath;
    public Root Input;
    public ExportFormat InputFormat;
    public string OutputPath;
    public ExportFormat OutputFormat;

    // Export 64-bit GR2
    public bool Is64Bit = false;
    // Use alternate GR2 signature when saving
    // (This is the signature D:OS EE and D:OS 2 uses, but GR2 tools
    // don't recognize it as legitimate.)
    public bool AlternateSignature = false;
    // GR2 run-time tag that that'll appear in the output file
    // If the GR2 tag doesn't match, the game will convert the GR2 to the latest tag,
    // which is a slow process. The advantage of a mismatched tag is that we don't
    // have to 1:1 match the GR2 structs for that version, as it won't just
    // memcpy the struct from the GR2 file directly.
    public UInt32 VersionTag = GR2.Header.DefaultTag;
    // Flip the V coord of UV-s (GR2 stores them in flipped format)
    public bool FlipUVs = true;
    // Create a dummy skeleton if none exists in the mesh
    // Some games will crash if they encounter a mesh without a skeleton
    public bool BuildDummySkeleton = false;
    // Save 16-bit vertex indices, if possible
    public bool CompactIndices = true;
    public bool DeduplicateVertices = true; // TODO: Add Collada conforming vert. handling as well
    public bool ApplyBasisTransforms = true;
    // Use an obsolete version tag to prevent Granny from memory mapping the structs
    public bool UseObsoleteVersionTag = false;
    public string ConformGR2Path;
    public bool ConformSkeletons = true;
    public bool ConformSkeletonsCopy = false;
    public bool ConformAnimations = true;
    public bool ConformMeshBoneBindings = true;
    public bool ConformModels = true;
    // Extended model info format to use when exporting to D:OS
    public DivinityModelInfoFormat ModelInfoFormat = DivinityModelInfoFormat.None;
    // Model flags to use when exporting
    public DivinityModelFlag ModelType = 0;
    // Flip mesh on X axis
    public bool FlipMesh = false;
    // Flip skeleton on X axis
    public bool FlipSkeleton = false;
    // Apply Y-up transforms on skeletons?
    public bool TransformSkeletons = true;
    // Ignore cases where we couldn't calculate tangents from UVs because of non-manifold geometry
    public bool IgnoreUVNaN = false;
    // Remove animation keys that are a linear interpolation of the preceding and following keys
    // Disabled by default, as D:OS doesn't support sparse knot values in anim curves.
    public bool RemoveTrivialAnimationKeys = false;
    // Recalculate mesh bone binding OBBs
    public bool RecalculateOBBs = true;
    // Allow encoding tangents/binormals as QTangents
    // See: Spherical Skinning with Dual-Quaternions and QTangents, Crytek R&D
    public bool EnableQTangents = true;

    public List<string> DisabledAnimations = [];
    public List<string> DisabledModels = [];
    public List<string> DisabledSkeletons = [];

    public void LoadGameSettings(Game game)
    {
        switch (game)
        {
            case Game.DivinityOriginalSin:
                Is64Bit = false;
                AlternateSignature = false;
                VersionTag = Header.Tag_DOS;
                ModelInfoFormat = DivinityModelInfoFormat.None;
                break;
            case Game.DivinityOriginalSinEE:
                Is64Bit = true;
                AlternateSignature = true;
                VersionTag = Header.Tag_DOSEE;
                ModelInfoFormat = DivinityModelInfoFormat.UserDefinedProperties;
                break;
            case Game.DivinityOriginalSin2:
                Is64Bit = true;
                AlternateSignature = true;
                VersionTag = Header.Tag_DOSEE;
                ModelInfoFormat = DivinityModelInfoFormat.LSMv1;
                break;
            case Game.BaldursGate3:
                Is64Bit = true;
                AlternateSignature = false;
                VersionTag = Header.Tag_DOSEE;
                ModelInfoFormat = DivinityModelInfoFormat.LSMv3;
                break;
            case Game.DivinityOriginalSin2DE:
            default:
                Is64Bit = true;
                AlternateSignature = true;
                VersionTag = Header.Tag_DOS2DE;
                ModelInfoFormat = DivinityModelInfoFormat.LSMv1;
                break;
        }
    }
}


public class Exporter
{
    public ExporterOptions Options = new ExporterOptions();
    private Root Root;

    private Root LoadGR2(string inPath)
    {
        var root = new Root();
        FileStream fs = File.Open(inPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var gr2 = new GR2Reader(fs);
        gr2.Read(root);
        root.PostLoad(gr2.Tag);
        fs.Close();
        fs.Dispose();
        return root;
    }

    private Root LoadDAE(string inPath)
    {
        var importer = new ColladaImporter
        {
            Options = Options
        };
        return importer.Import(inPath);
    }

    private Root LoadGLTF(string inPath)
    {
        var importer = new GLTFImporter
        {
            Options = Options
        };
        return importer.Import(inPath);
    }

    private Root Load(string inPath, ExportFormat format)
    {
        switch (format)
        {
            case ExportFormat.GR2:
                return LoadGR2(inPath);

            case ExportFormat.DAE:
                return LoadDAE(inPath);

            case ExportFormat.GLTF:
            case ExportFormat.GLB:
                return LoadGLTF(inPath);

            default:
                throw new NotImplementedException("Unsupported input format");
        }
    }

    private void SaveGR2(string outPath, Root root)
    {
        root.PreSave();
        var writer = new GR2Writer
        {
            Format = Options.Is64Bit ? Magic.Format.LittleEndian64 : Magic.Format.LittleEndian32,
            AlternateMagic = Options.AlternateSignature,
            VersionTag = Options.VersionTag
        };

        if (Options.UseObsoleteVersionTag)
        {
            // Use an obsolete version tag to prevent Granny from memory mapping the structs
            writer.VersionTag -= 1;
        }

        var body = writer.Write(root, (root.Meshes != null) ? (uint)root.Meshes.Count : 0);
        writer.Dispose();

        FileStream f = File.Open(outPath, FileMode.Create, System.IO.FileAccess.Write, FileShare.None);
        f.Write(body, 0, body.Length);
        f.Close();
        f.Dispose();
    }

    private void SaveDAE(Root root, ExporterOptions options)
    {
        var exporter = new ColladaExporter
        {
            Options = options
        };
        exporter.Export(root, options.OutputPath);
    }

    private void SaveGLTF(Root root, ExporterOptions options)
    {
        var exporter = new GLTFExporter
        {
            Options = options
        };
        exporter.Export(root, options.OutputPath);
    }

    private void Save(Root root, ExporterOptions options)
    {
        switch (options.OutputFormat)
        {
            case ExportFormat.GR2:
                FileManager.TryToCreateDirectory(options.OutputPath);
                SaveGR2(options.OutputPath, root);
                break;

            case ExportFormat.DAE:
                SaveDAE(root, options);
                break;

            case ExportFormat.GLTF:
            case ExportFormat.GLB:
                SaveGLTF(root, options);
                break;

            default:
                throw new NotImplementedException("Unsupported output format");
        }
    }

    private void GenerateDummySkeleton(Root root)
    {
        foreach (var model in root.Models)
        {
            if (model.Skeleton == null)
            {
                Utils.Info($"Generating dummy skeleton for model '{model.Name}'");
                var rootBone = new Bone
                {
                    Name = model.Name,
                    ParentIndex = -1,
                    Transform = new Transform()
                };

                var skeleton = new Skeleton
                {
                    Name = model.Name,
                    LODType = 1,
                    IsDummy = true,
                    Bones = [rootBone]
                };
                root.Skeletons.Add(skeleton);

                foreach (var mesh in model.MeshBindings)
                {
                    if (mesh.Mesh.BoneBindings != null && mesh.Mesh.BoneBindings.Count > 0)
                    {
                        throw new ParsingException("Failed to generate dummy skeleton: Mesh already has bone bindings.");
                    }

                    var bone = new Bone
                    {
                        Name = mesh.Mesh.Name,
                        ParentIndex = 0,
                        Transform = new Transform()
                    };
                    skeleton.Bones.Add(bone);
                    (Vector3 min, Vector3 max) = mesh.Mesh.CalculateOBB();

                    var binding = new BoneBinding
                    {
                        BoneName = bone.Name,
                        // TODO: Use oriented bounding box instead of AABB
                        // (AABB should be fine here, as there are no transforms on the bones)
                        OBBMin = [min.X, min.Y, min.Z],
                        OBBMax = [max.X, max.Y, max.Z]
                    };
                    mesh.Mesh.BoneBindings = [binding];
                }

                // TODO: Transform / IWT is not always identity on dummy bones!
                skeleton.UpdateWorldTransforms();
                model.Skeleton = skeleton;
            }
        }
    }

    private void ConformAnimationBindPoses(Skeleton skeleton, Skeleton conformToSkeleton)
    {
        if (Root.TrackGroups == null) return;

        foreach (var trackGroup in Root.TrackGroups)
        {
            for (var i = 0; i < trackGroup.TransformTracks.Count; i++)
            {
                var track = trackGroup.TransformTracks[i];
                var bone = skeleton.GetBoneByName(track.Name);
                //Dummy_Foot -> Dummy_Foot_01
                bone ??= skeleton.GetBoneByName(track.Name + "_01");

                if (bone == null)
                {
                    throw new ExportException($"Animation track references bone '{track.Name}' that cannot be found in the skeleton '{skeleton.Name}'.");
                }

                var conformingBone = conformToSkeleton.GetBoneByName(bone.Name);
                if (conformingBone == null)
                {
                    throw new ExportException($"Animation track references bone '{bone.Name}' that cannot be found in the conforming skeleton '{conformToSkeleton.Name}'.");
                }

                var keyframes = track.ToKeyframes();
                keyframes.SwapBindPose(bone.OriginalTransform, conformingBone.Transform.ToMatrix4());
                var newTrack = TransformTrack.FromKeyframes(keyframes);
                newTrack.Flags = track.Flags;
                newTrack.Name = track.Name;
                newTrack.ParentAnimation = track.ParentAnimation;
                trackGroup.TransformTracks[i] = newTrack;
            }
        }
    }

    private void ConformSkeleton(Skeleton skeleton, Skeleton conformToSkeleton)
    {
        skeleton.LODType = conformToSkeleton.LODType;

        // TODO: Tolerate missing bones?
        foreach (var conformBone in conformToSkeleton.Bones)
        {
            Bone inputBone = null;
            foreach (var bone in skeleton.Bones)
            {
                if (bone.Name == conformBone.Name)
                {
                    inputBone = bone;
                    break;
                }
            }

            if (inputBone == null)
            {
                throw new ExportException($"No matching bone found for conforming bone '{conformBone.Name}' in skeleton '{skeleton.Name}'.");
            }

            // Bones must have the same parent. We check this in two steps:
            // 1) Either both of them are root bones (no parent index) or none of them are.
            if (conformBone.IsRoot != inputBone.IsRoot)
            {
                throw new ExportException($"Cannot map non-root bones to root bone '{conformBone.Name}' for skeleton '{skeleton.Name}'.");
            }

            // 2) The name of their parent bones is the same (index may differ!)
            if (conformBone.ParentIndex != -1)
            {
                var conformParent = conformToSkeleton.Bones[conformBone.ParentIndex];
                var inputParent = skeleton.Bones[inputBone.ParentIndex];
                if (conformParent.Name != inputParent.Name)
                {
                    throw new ExportException($"Conforming parent ({conformParent.Name}) for bone '{conformBone.Name}' " +
                        $"differs from input parent ({inputParent.Name}) for skeleton '{skeleton.Name}'.");
                }
            }


            // The bones match, copy relevant parameters from the conforming skeleton to the input.
            inputBone.InverseWorldTransform = conformBone.InverseWorldTransform;
            inputBone.LODError = conformBone.LODError;
            inputBone.Transform = conformBone.Transform;
        }

        if (Options.ConformAnimations)
        {
            ConformAnimationBindPoses(skeleton, conformToSkeleton);
        }
    }

    private void ConformSkeletonAnimations(Skeleton skeleton)
    {
        if (Root.TrackGroups == null) return;

        foreach (var trackGroup in Root.TrackGroups)
        {
            foreach (var track in trackGroup.TransformTracks)
            {
                //Dummy_Foot -> Dummy_Foot_01
                var bone = skeleton.GetBoneByName(track.Name) ?? skeleton.GetBoneByName(track.Name + "_01");

                if (bone == null)
                {
                    throw new ExportException($"Animation track references bone '{track.Name}' that cannot be found in the skeleton '{skeleton.Name}'.");
                }
            }
        }
    }

    private void ConformSkeletons(IEnumerable<Skeleton> skeletons)
    {
        // We don't have any skeletons in this mesh, nothing to conform.
        if (Root.Skeletons == null || Root.Skeletons.Count == 0)
        {
            // If we're exporting animations without a skeleton, copy the source skeleton
            // and check if all animation tracks are referencing existing bones.
            if (Root.Animations != null && Root.Animations.Count > 0)
            {
                Root.Skeletons = skeletons.ToList();
                if (Root.Skeletons.Count != 1)
                {
                    throw new ExportException($"Skeleton source file should contain exactly one skeleton. Skeleton Count: '{Root.Skeletons.Count}'.");
                }

                var skeleton = Root.Skeletons.First();

                // Generate a dummy model if there isn't one, otherwise we won't
                // be able to bind the animations to anything
                Root.Models ??= [
                    new Model
                    {
                        InitialPlacement = new Transform(),
                        Name = skeleton.Name,
                        Skeleton = skeleton
                    }
                ];

                ConformSkeletonAnimations(skeleton);
            }

            return;
        }

        foreach (var skeleton in Root.Skeletons)
        {
            // Check if there is a matching skeleton in the source file
            Skeleton conformingSkel = null;
            foreach (var skel in skeletons)
            {
                if (skel.Name == skeleton.Name)
                {
                    conformingSkel = skel;
                    break;
                }
            }

            // Allow name mismatches if there is only 1 skeleton in each file
            if (conformingSkel == null && skeletons.Count() == 1 && Root.Skeletons.Count == 1)
            {
                conformingSkel = skeletons.First();
            }

            if (conformingSkel == null)
            {
                throw new ExportException($"No matching skeleton found in source file for skeleton '{skeleton.Name}'.");
            }

            ConformSkeleton(skeleton, conformingSkel);
        }
    }

    private void ConformMeshBoneBindings(Mesh mesh, Mesh conformToMesh)
    {
        mesh.BoneBindings ??= [];

        foreach (var conformBone in conformToMesh.BoneBindings)
        {
            BoneBinding inputBone = null;
            foreach (var bone in mesh.BoneBindings)
            {
                if (bone.BoneName == conformBone.BoneName)
                {
                    inputBone = bone;
                    break;
                }
            }

            if (inputBone == null)
            {
                // Create a new "dummy" binding if it does not exist in the new mesh
                inputBone = new BoneBinding
                {
                    BoneName = conformBone.BoneName
                };
                mesh.BoneBindings.Add(inputBone);
            }

            // The bones match, copy relevant parameters from the conforming binding to the input.
            inputBone.OBBMin = conformBone.OBBMin;
            inputBone.OBBMax = conformBone.OBBMax;
        }
    }

    private void ConformMeshBoneBindings(IEnumerable<Mesh> meshes)
    {
        if (Root.Meshes == null)
        {
            return;
        }

        foreach (var mesh in Root.Meshes)
        {
            Mesh conformingMesh = null;
            foreach (var mesh2 in meshes)
            {
                if (mesh.Name == mesh2.Name)
                {
                    conformingMesh = mesh2;
                    break;
                }
            }

            if (conformingMesh == null)
            {
                throw new ExportException($"No matching mesh found in source file for mesh '{mesh.Name}'.");
            }

            ConformMeshBoneBindings(mesh, conformingMesh);
        }
    }

    private Mesh GenerateDummyMesh(MeshBinding meshBinding)
    {
        var vertexData = new VertexData();
        vertexData.VertexComponentNames = meshBinding.Mesh.PrimaryVertexData.VertexComponentNames
            .Select(name => new GrannyString(name.String)).ToList();
        vertexData.Vertices = [];
        var dummyVertex = meshBinding.Mesh.VertexFormat.CreateInstance();
        vertexData.Vertices.Add(dummyVertex);
        Root.VertexDatas.Add(vertexData);

        var topology = new TriTopology
        {
            Groups = [
                new TriTopologyGroup
                {
                    MaterialIndex = 0,
                    TriCount = 0,
                    TriFirst = 0
                }
            ],
            Indices = []
        };
        Root.TriTopologies.Add(topology);

        var mesh = new Mesh
        {
            Name = meshBinding.Mesh.Name,
            VertexFormat = meshBinding.Mesh.VertexFormat,
            PrimaryTopology = topology,
            PrimaryVertexData = vertexData
        };
        if (meshBinding.Mesh.BoneBindings != null)
        {
            mesh.BoneBindings = [];
            ConformMeshBoneBindings(mesh, meshBinding.Mesh);
        }

        return mesh;
    }

    private Model MakeDummyModel(Model original)
    {
        var newModel = new Model
        {
            InitialPlacement = original.InitialPlacement,
            Name = original.Name
        };

        if (original.Skeleton != null)
        {
            var skeleton = Root.Skeletons.Where(skel => skel.Name == original.Skeleton.Name).FirstOrDefault();
            if (skeleton == null)
            {
                throw new ExportException($"Model '{original.Name}' references skeleton '{original.Skeleton.Name}' that does not exist in the source file.");
            }

            newModel.Skeleton = skeleton;
        }

        if (original.MeshBindings != null)
        {
            newModel.MeshBindings = [];
            foreach (var meshBinding in original.MeshBindings)
            {
                // Try to bind the original mesh, if it exists in the source file.
                // If it doesn't, generate a dummy mesh with 0 vertices
                var mesh = Root.Meshes.Where(m => m.Name == meshBinding.Mesh.Name).FirstOrDefault();
                if (mesh == null)
                {
                    mesh = GenerateDummyMesh(meshBinding);
                    Root.Meshes.Add(mesh);
                }

                var binding = new MeshBinding
                {
                    Mesh = mesh
                };
                newModel.MeshBindings.Add(binding);
            }
        }

        Root.Models.Add(newModel);
        return newModel;
    }

    private void ConformModels(IEnumerable<Model> models)
    {
        if (Root.Models == null || Root.Models.Count == 0)
        {
            return;
        }

        // Rebuild the model list to match the order used in the original GR2
        // If a model is missing, generate a dummy model & mesh.
        var originalModels = Root.Models;
        Root.Models = [];

        foreach (var model in models)
        {
            Model newModel = null;
            foreach (var model2 in originalModels)
            {
                if (model.Name == model2.Name)
                {
                    newModel = model2;
                    break;
                }
            }

            if (newModel == null)
            {
                newModel = MakeDummyModel(model);
                Root.Models.Add(newModel);
            }
            else
            {
                newModel.InitialPlacement = model.InitialPlacement;
            }
        }

        // If the new GR2 contains models that are not in the original GR2,
        // append them to the end of the model list
        Root.Models.AddRange(originalModels.Where(m => !Root.Models.Contains(m)));
    }

    private void Conform(string inPath)
    {
        var conformRoot = LoadGR2(inPath);

        if (Options.ConformSkeletonsCopy)
        {
            Root.Skeletons = conformRoot.Skeletons;
            if (Root.Models != null)
            {
                foreach (var model in Root.Models)
                {
                    model.Skeleton = Root.Skeletons.First();
                }
            }
            else
            {
                Root.Models = conformRoot.Models;
            }
        }
        else if (Options.ConformSkeletons)
        {
            if (conformRoot.Skeletons == null || conformRoot.Skeletons.Count == 0)
            {
                throw new ExportException("Source file contains no skeletons.");
            }

            ConformSkeletons(conformRoot.Skeletons);
        }

        if (Options.ConformModels && conformRoot.Models != null)
        {
            ConformModels(conformRoot.Models);
        }

        if (Options.ConformMeshBoneBindings && conformRoot.Meshes != null)
        {
            ConformMeshBoneBindings(conformRoot.Meshes);
        }
    }


    private void UpdateSkeletonLODType(Skeleton skeleton)
    {
        bool hasSkinnedVerts = false;

        foreach (var mesh in Root.Meshes ?? [])
        {
            if (mesh.IsSkinned())
            {
                hasSkinnedVerts = true;
            }
        }

        foreach (var track in Root.TrackGroups ?? [])
        {
            if (track.TransformTracks.Count > 0)
            {
                hasSkinnedVerts = true;
            }
        }

        skeleton.LODType = hasSkinnedVerts ? 1 : 0;
    }

    public void Export()
    {
        if (Options.InputPath != null)
        {
            Root = Load(Options.InputPath, Options.InputFormat);
        }
        else
        {
            if (Options.Input == null)
            {
                throw new ExportException("No input model specified. Either the InputPath or the Input option must be specified.");
            }

            Root = Options.Input;
        }

        if (Options.DisabledAnimations.Count > 0)
        {
            Root.Animations = Root.Animations.Where(a => !Options.DisabledAnimations.Contains(a.Name)).ToList();
        }

        if (Options.DisabledModels.Count > 0)
        {
            Root.Models = Root.Models.Where(a => !Options.DisabledModels.Contains(a.Name)).ToList();
        }

        if (Options.DisabledSkeletons.Count > 0)
        {
            Root.Skeletons = Root.Skeletons.Where(a => !Options.DisabledSkeletons.Contains(a.Name)).ToList();
        }

        if (Options.DeduplicateVertices)
        {
            if (Root.VertexDatas != null)
            {
                foreach (var vertexData in Root.VertexDatas)
                {
                    vertexData.Deduplicate();
                }
            }
        }

        if (Options.ApplyBasisTransforms)
        {
            Root.ConvertToYUp(Options.TransformSkeletons);
        }

        // TODO: DeduplicateUVs

        if (Options.ConformGR2Path != null)
        {
            try
            {
                Conform(Options.ConformGR2Path);
            }
            catch (ExportException e)
            {
                throw new ExportException("Failed to conform skeleton:\n" + e.Message);
            }
        }

        if (Options.BuildDummySkeleton && Root.Models != null)
        {
            GenerateDummySkeleton(Root);
        }

        if (Options.FlipMesh || Options.FlipSkeleton)
        {
            Root.Flip(Options.FlipMesh, Options.FlipSkeleton);
        }

        foreach (var skeleton in Root.Skeletons ?? [])
        {
            UpdateSkeletonLODType(skeleton);
        }

        // This option should be handled after everything else, as it converts Indices
        // into Indices16 and breaks every other operation that manipulates tri topologies.
        if (Options.OutputFormat == ExportFormat.GR2 && Options.CompactIndices)
        {
            if (Root.TriTopologies != null)
            {
                foreach (var topology in Root.TriTopologies)
                {
                    if (topology.Indices != null)
                    {
                        // Make sure that we don't have indices over 32767. If we do,
                        // int16 won't be big enough to hold the index, so we won't convert.
                        bool hasHighIndex = false;
                        foreach (var index in topology.Indices)
                        {
                            if (index > 0xffff)
                            {
                                hasHighIndex = true;
                                break;
                            }
                        }

                        if (!hasHighIndex)
                        {
                            topology.Indices16 = new List<ushort>(topology.Indices.Count);
                            foreach (var index in topology.Indices)
                            {
                                topology.Indices16.Add((ushort)index);
                            }

                            topology.Indices = null;
                        }
                    }
                }
            }
        }

        Save(Root, Options);
    }
}
