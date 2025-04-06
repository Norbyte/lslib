using LSLib.Granny.GR2;
using LSLib.LS;
using SharpGLTF.Animations;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using System.Numerics;

namespace LSLib.Granny.Model;

class GLTFImportedSkeleton
{
    public Dictionary<string, NodeBuilder> Joints = [];
}

public class GLTFImporter
{
    public ExporterOptions Options = new();
    public List<Mesh> ImportedMeshes;
    private HashSet<string> AnimationNames = [];
    private Dictionary<Skeleton, GLTFImportedSkeleton> Skeletons = [];

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

    private void MakeExtendedData(ContentTransformer content, GLTFMeshExtensions ext, Mesh loaded)
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

        if (ext != null)
        {
            ext.Apply(loaded, loaded.ExtendedData);
        }
    }

    private static GLTFMeshExtensions FindMeshExtension(ModelRoot root, string name)
    {
        foreach (var mesh in root.LogicalMeshes)
        {
            if (mesh.Name == name)
            {
                return mesh.GetExtension<GLTFMeshExtensions>();
            }
        }

        return null;
    }

    private Mesh ImportMesh(ModelRoot modelRoot, ContentTransformer content, string name)
    {
        var collada = new GLTFMesh();
        collada.ImportFromGLTF(content, Options);

        var m = new Mesh
        {
            VertexFormat = collada.InternalVertexType,
            Name = name,

            PrimaryVertexData = new VertexData
            {
                Vertices = collada.Vertices
            },

            PrimaryTopology = new TriTopology
            {
                Indices = collada.Indices,
                Groups = [
                    new TriTopologyGroup
                    {
                        MaterialIndex = 0,
                        TriFirst = 0,
                        TriCount = collada.TriangleCount
                    }
                ]
            },

            MaterialBindings = [new MaterialBinding()]
        };

        var components = m.VertexFormat.ComponentNames().Select(s => new GrannyString(s)).ToList();
        m.PrimaryVertexData.VertexComponentNames = components;

        var ext = FindMeshExtension(modelRoot, name);
        MakeExtendedData(content, ext, m);

        Utils.Info(String.Format("Imported {0} mesh ({1} tri groups, {2} tris)", 
            (m.VertexFormat.HasBoneWeights ? "skinned" : "rigid"), 
            m.PrimaryTopology.Groups.Count, 
            collada.TriangleCount));

        return m;
    }

    private void AddMeshToRoot(Root root, Mesh mesh)
    {
        root.VertexDatas.Add(mesh.PrimaryVertexData);
        root.TriTopologies.Add(mesh.PrimaryTopology);
        root.Meshes.Add(mesh);
        root.Models[0].MeshBindings.Add(new MeshBinding
        {
            Mesh = mesh
        });
    }

    private TrackGroup ImportTrackGroup(Animation anim, GLTFImportedSkeleton skeleton, string name, GLTFSceneExtensions ext)
    {
        var trackGroup = new TrackGroup
        {
            Name = name,
            TransformTracks = [],
            InitialPlacement = new Transform(),
            AccumulationFlags = 2,
            LoopTranslation = [0, 0, 0],
            ExtendedData = new BG3TrackGroupExtendedData
            {
                SkeletonResourceID = ext?.SkeletonResourceID ?? ""
            }
        };

        foreach (var (jointName, joint) in skeleton.Joints)
        {
            var track = ImportTrack(anim, joint, name);
            if (track != null)
            {
                track.Name = jointName;
                trackGroup.TransformTracks.Add(track);
            }
        }

        // Reorder transform tracks in lexicographic order
        // This is needed by Granny; otherwise it'll fail to find animation tracks
        trackGroup.TransformTracks.Sort((t1, t2) => String.Compare(t1.Name, t2.Name, StringComparison.Ordinal));

        return trackGroup;
    }

    private void ImportAnimations(Root root, Skeleton skeleton, GLTFSceneExtensions ext)
    {
        var gltfSkel = Skeletons[skeleton];

        var animation = new Animation
        {
            Name = skeleton.Name,
            TimeStep = 0.016667f, // 60 FPS
            Oversampling = 1,
            DefaultLoopCount = 1,
            Flags = 1,
            Duration = .0f,
            TrackGroups = []
        };

        foreach (var animName in AnimationNames)
        {
            var trackGroup = ImportTrackGroup(animation, gltfSkel, animName, ext);
            animation.TrackGroups.Add(trackGroup);
            root.TrackGroups.Add(trackGroup);
        }

        root.Animations.Add(animation);
    }

    private TransformTrack ImportTrack(Animation anim, NodeBuilder joint, string animName)
    {
        if (!joint.HasAnimations) return null;

        var translate = joint.Translation?.Tracks.GetValueOrDefault(animName);
        var rotate = joint.Rotation?.Tracks.GetValueOrDefault(animName);
        var scale = joint.Scale?.Tracks.GetValueOrDefault(animName);

        if (translate == null && rotate == null && scale == null) return null;

        var keyframes = new KeyframeTrack();

        if (translate != null)
        {
            var curve = (CurveBuilder<Vector3>)translate;
            foreach (var key in curve.Keys)
            {
                var t = curve.GetPoint(key);
                keyframes.AddTranslation(key, t.ToOpenTK());
            }
        }

        if (rotate != null)
        {
            var curve = (CurveBuilder<Quaternion>)rotate;
            foreach (var key in curve.Keys)
            {
                var q = curve.GetPoint(key);
                keyframes.AddRotation(key, q.ToOpenTK());
            }
        }

        if (scale != null)
        {
            var curve = (CurveBuilder<Vector3>)scale;
            foreach (var key in curve.Keys)
            {
                var s = curve.GetPoint(key);
                var m = new OpenTK.Mathematics.Matrix3(
                    s.X, 0.0f, 0.0f,
                    0.0f, s.Y, 0.0f,
                    0.0f, 0.0f, s.Z
                );
                keyframes.AddScaleShear(key, m);
            }
        }

        var track = TransformTrack.FromKeyframes(keyframes);
        track.Flags = 0;
        anim.Duration = Math.Max(anim.Duration, keyframes.Keyframes.Last().Key);

        return track;
    }

    private int ImportBone(Skeleton skeleton, int parentIndex, NodeBuilder node, GLTFSceneExtensions ext, GLTFImportedSkeleton imported)
    {
        var transform = node.LocalTransform;
        var tm = transform.Matrix;
        var myIndex = skeleton.Bones.Count;

        var bone = new Bone
        {
            ParentIndex = parentIndex,
            Name = node.Name,
            LODError = 0, // TODO
            OriginalTransform = new OpenTK.Mathematics.Matrix4(
                tm.M11, tm.M12, tm.M13, tm.M14,
                tm.M21, tm.M22, tm.M23, tm.M24,
                tm.M31, tm.M32, tm.M33, tm.M34,
                tm.M41, tm.M42, tm.M43, tm.M44
            ),
            Transform = Transform.FromGLTF(transform)
        };

        skeleton.Bones.Add(bone);

        bone.UpdateWorldTransforms(skeleton.Bones);

        if (ext != null && ext.BoneOrder.TryGetValue(bone.Name, out var order) && order > 0)
        {
            bone.ExportIndex = order - 1;
        }

        if (node.HasAnimations)
        {
            foreach (var anim in node.AnimationTracksNames)
            {
                AnimationNames.Add(anim);
            }
        }

        imported.Joints.Add(node.Name, node);
        return myIndex;
    }

    private void ImportBoneTree(Skeleton skeleton, int parentIndex, NodeBuilder node, GLTFSceneExtensions ext, GLTFImportedSkeleton imported)
    {
        if (ext != null && !ext.BoneOrder.ContainsKey(node.Name)) return;

        var boneIndex = ImportBone(skeleton, parentIndex, node, ext, imported);

        foreach (var child in node.VisualChildren)
        {
            ImportBoneTree(skeleton, boneIndex, child, ext, imported);
        }
    }

    private Skeleton ImportSkeleton(string name, NodeBuilder root, GLTFSceneExtensions ext)
    {
        var skeleton = Skeleton.CreateEmpty(name);
        var imported = new GLTFImportedSkeleton();

        if (ext != null && ext.BoneOrder.Count > 0)
        {
            // Try to figure out what the real root bone is
            if (!ext.BoneOrder.ContainsKey(root.Name))
            {
                // Find real root among 1st-level children
                var roots = root.VisualChildren.Where(n => ext.BoneOrder.ContainsKey(n.Name)).ToList();
                if (roots.Count == 1)
                {
                    ImportBoneTree(skeleton, -1, roots[0], ext, imported);
                    return skeleton;
                }
                else
                {
                    throw new ParsingException("Unable to determine real root bone of skeleton.");
                }
            }
        }

        ImportBoneTree(skeleton, -1, root, ext, imported);

        Skeletons[skeleton] = imported;
        return skeleton;
    }

    public Root Import(string inputPath)
    {
        GLTFExtensions.RegisterExtensions();
        ModelRoot modelRoot = ModelRoot.Load(inputPath);

        if (modelRoot.LogicalScenes.Count != 1)
        {
            throw new ParsingException($"GLTF file is expected to have a single scene, got {modelRoot.LogicalScenes.Count}");
        }

        if (modelRoot.LogicalSkins.Count > 1)
        {
            throw new ParsingException("GLTF files containing multiple skeletons are not supported");
        }

        var sceneExt = modelRoot.DefaultScene.GetExtension<GLTFSceneExtensions>();
        if (sceneExt != null)
        {
            if (sceneExt.MetadataVersion > Common.GLTFMetadataVersion)
            {
                throw new ParsingException(
                    $"GLTF file is using a newer LSLib metadata format than this LSLib version supports, please upgrade.\r\n" +
                    $"File version: {sceneExt.MetadataVersion}, exporter version: {Common.GLTFMetadataVersion}");
            }
        }

        var scene = SceneBuilder.CreateFrom(modelRoot).First();

        var root = Root.CreateEmpty();
        root.ArtToolInfo = ArtToolInfo.CreateDefault();
        root.ArtToolInfo.SetYUp();
        root.ExporterInfo = ExporterInfo.MakeCurrent();
        root.FromFileName = inputPath;

        ImportedMeshes = [];

        foreach (var geometry in scene.Instances)
        {
            if (geometry.Content?.HasRenderableContent == true)
            {
                var content = geometry.Content;
                var name = geometry.Name ?? content.Name ?? content.GetGeometryAsset().Name;
                var mesh = ImportMesh(modelRoot, content, name);
                ImportedMeshes.Add(mesh);

                if (content is SkinnedTransformer skin)
                {
                    var joints = skin.GetJointBindings();
                    mesh.BoneBindings = [];
                    if (joints.Length > 0)
                    {
                        foreach (var (joint, inverseBindMatrix) in joints)
                        {
                            var binding = new BoneBinding
                            {
                                BoneName = joint.Name,
                                OBBMin = [-0.1f, -0.1f, -0.1f],
                                OBBMax = [0.1f, 0.1f, 0.1f]
                            };
                            mesh.BoneBindings.Add(binding);
                        }
                    }

                    if (Options.RecalculateOBBs)
                    {
                        // FIXME! VertexHelpers.UpdateOBBs(root.Skeletons.Single(), mesh);
                    }
                }
            }
            else
            {
                var skeletonRoot = geometry.Content?.GetArmatureRoot();
                if (skeletonRoot != null && skeletonRoot == ((RigidTransformer)geometry.Content).Transform)
                {
                    var skel = ImportSkeleton(geometry.Name, skeletonRoot, sceneExt);
                    root.Skeletons.Add(skel);
                }
            }
        }

        var rootModel = new Model
        {
            Name = "Unnamed", // TODO
            InitialPlacement = new Transform(),
            MeshBindings = new List<MeshBinding>()
        };

        if (root.Skeletons.Count > 0)
        {
            rootModel.Skeleton = root.Skeletons[0];
            rootModel.Name = rootModel.Skeleton.Bones[0].Name;
        }

        root.Models.Add(rootModel);

        // Reorder meshes based on their ExportOrder
        if (ImportedMeshes.Any(m => m.ExportOrder > 0))
        {
            ImportedMeshes.Sort((a, b) => a.ExportOrder - b.ExportOrder);
        }

        foreach (var mesh in ImportedMeshes)
        {
            AddMeshToRoot(root, mesh);
        }

        if (AnimationNames.Count > 0)
        {
            if (root.Skeletons.Count != 1)
            {
                throw new ParsingException("GLTF file must contain exactly one skeleton for animation import");
            }

            ImportAnimations(root, root.Skeletons.FirstOrDefault(), sceneExt);
        }

        // TODO: make this an option!
        if (root.Skeletons.Count > 0)
            root.Skeletons[0].UpdateWorldTransforms();
        root.PostLoad(GR2.Header.DefaultTag);

        BuildExtendedData(root);

        return root;
    }
}
