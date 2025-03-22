using LSLib.Granny.GR2;
using LSLib.LS;
using OpenTK.Mathematics;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;

namespace LSLib.Granny.Model;

public class GLTFImporter
{
    public ExporterOptions Options = new();
    public List<Mesh> ImportedMeshes;

    private ExporterInfo MakeExporterInfo()
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

        if (!Options.StripMetadata)
        {
            var components = m.VertexFormat.ComponentNames().Select(s => new GrannyString(s)).ToList();
            m.PrimaryVertexData.VertexComponentNames = components;
        }
        else
        {
            m.PrimaryVertexData.VertexComponentNames = null;
        }

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

    private int ImportBone(Skeleton skeleton, int parentIndex, NodeBuilder node, GLTFSceneExtensions ext)
    {
        var transform = node.LocalTransform;
        var tm = transform.Matrix;
        var myIndex = skeleton.Bones.Count;

        var bone = new Bone
        {
            ParentIndex = parentIndex,
            Name = node.Name,
            LODError = 0, // TODO
            OriginalTransform = new Matrix4(
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

        return myIndex;
    }

    private void ImportBoneTree(Skeleton skeleton, int parentIndex, NodeBuilder node, GLTFSceneExtensions ext)
    {
        if (ext != null && !ext.BoneOrder.ContainsKey(node.Name)) return;

        var boneIndex = ImportBone(skeleton, parentIndex, node, ext);

        foreach (var child in node.VisualChildren)
        {
            ImportBoneTree(skeleton, boneIndex, child, ext);
        }
    }

    private Skeleton ImportSkeleton(string name, NodeBuilder root, GLTFSceneExtensions ext)
    {
        var skeleton = Skeleton.CreateEmpty(name);

        if (ext != null && ext.BoneOrder.Count > 0)
        {
            // Try to figure out what the real root bone is
            if (!ext.BoneOrder.ContainsKey(root.Name))
            {
                // Find real root among 1st-level children
                var roots = root.VisualChildren.Where(n => ext.BoneOrder.ContainsKey(n.Name)).ToList();
                if (roots.Count == 1)
                {
                    ImportBoneTree(skeleton, -1, roots[0], ext);
                    return skeleton;
                }
                else
                {
                    throw new ParsingException("Unable to determine real root bone of skeleton.");
                }
            }
        }

        ImportBoneTree(skeleton, -1, root, ext);
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
        root.ExporterInfo = Options.StripMetadata ? null : MakeExporterInfo();
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

        // TODO: make this an option!
        if (root.Skeletons.Count > 0)
            root.Skeletons[0].UpdateWorldTransforms();
        root.PostLoad(GR2.Header.DefaultTag);

        BuildExtendedData(root);

        return root;
    }
}
