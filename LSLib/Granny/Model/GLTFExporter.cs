using LSLib.Granny.GR2;
using System.Text.RegularExpressions;
using SharpGLTF.Transforms;
using System.Numerics;
using SharpGLTF.Scenes;
using LSLib.LS;
using SharpGLTF.Schema2;

namespace LSLib.Granny.Model;

internal class GLTFSkeletonExportData
{
    public NodeBuilder Root;
    public List<(NodeBuilder, Matrix4x4)> Joints;
    public Dictionary<string, NodeBuilder> Names;
    public bool UsedForSkinning;
}

public class GLTFExporter
{
    [Serialization(Kind = SerializationKind.None)]
    public ExporterOptions Options = new();

    private Dictionary<Mesh, string> MeshIds = new();
    private Dictionary<Skeleton, GLTFSkeletonExportData> Skeletons = new();

    private void GenerateUniqueMeshIds(List<Mesh> meshes)
    {
        HashSet<string> namesInUse = [];
        var charRe = new Regex("[^a-zA-Z0-9_.-]", RegexOptions.CultureInvariant);
        foreach (var mesh in meshes)
        {
            // Sanitize name to make sure it satisfies Collada xsd:NCName requirements
            mesh.Name = charRe.Replace(mesh.Name, "_");
            var name = mesh.Name;

            var nameNum = 1;
            while (namesInUse.Contains(name))
            {
                name = mesh.Name + "_" + nameNum.ToString();
                nameNum++;
            }

            namesInUse.Add(name);
            MeshIds[mesh] = name;
        }
    }

    private void ExportMeshBinding(Model model, Skeleton skeleton, MeshBinding meshBinding, SceneBuilder scene)
    {
        var meshId = MeshIds[meshBinding.Mesh];

        if (skeleton != null && meshBinding.Mesh.VertexFormat.HasBoneWeights)
        {
            var joints = meshBinding.Mesh.GetInfluencingJoints(skeleton);

            var exporter = new GLTFMeshExporter(meshBinding.Mesh, meshId, joints.BindRemaps);
            var mesh = exporter.Export();

            Skeletons[skeleton].UsedForSkinning = true;

            List<(NodeBuilder, Matrix4x4)> bindings = [];
            foreach (var jointIndex in joints.SkeletonJoints)
            {
                bindings.Add(Skeletons[skeleton].Joints[jointIndex]);
            }

            scene.AddSkinnedMesh(mesh, bindings.ToArray());
        }
        else
        {
            var exporter = new GLTFMeshExporter(meshBinding.Mesh, meshId, null);
            var mesh = exporter.Export();
            scene.AddRigidMesh(mesh, new AffineTransform(Matrix4x4.Identity));
        }
    }

    private GLTFSkeletonExportData ExportSkeleton(NodeBuilder root, Skeleton skeleton)
    {
        var joints = new List<(NodeBuilder, Matrix4x4)>();
        var names = new Dictionary<string, NodeBuilder>();
        foreach (var joint in skeleton.Bones)
        {
            NodeBuilder node;
            if (joint.ParentIndex == -1)
            {
                // FIXME - parent to dummy root proxy?
                // node = root.CreateNode(joint.Name);
                node = new NodeBuilder(joint.Name);
            }
            else
            {
                node = joints[joint.ParentIndex].Item1.CreateNode(joint.Name);
            }

            node.LocalTransform = ToGLTFTransform(joint.Transform);
            var t = joint.InverseWorldTransform;
            var iwt = new Matrix4x4(
                t[0], t[1], t[2], t[3],
                t[4], t[5], t[6], t[7],
                t[8], t[9], t[10], t[11],
                t[12], t[13], t[14], t[15]
            );
            joints.Add((node, iwt));
            names.Add(joint.Name, node);
        }

        return new GLTFSkeletonExportData { 
            Joints = joints,
            Names = names,
            Root = joints[0].Item1,
            UsedForSkinning = false
        };
    }

    private void ExportSceneExtensions(Root root, GLTFSceneExtensions ext)
    {
        ext.MetadataVersion = Common.GLTFMetadataVersion;
        ext.LSLibMajor = Common.MajorVersion;
        ext.LSLibMinor = Common.MinorVersion;
        ext.LSLibPatch = Common.PatchVersion;

        foreach (var model in root.Models ?? [])
        {
            if (model.Name != "")
            {
                ext.ModelName = model.Name;
            }
        }

        if (ext.ModelName == "")
        {
            foreach (var skeleton in root.Skeletons ?? [])
            {
                if (skeleton.Name != "")
                {
                    ext.ModelName = skeleton.Name;
                }
            }
        }

        foreach (var group in root.TrackGroups ?? [])
        {
            if (group.ExtendedData != null && group.ExtendedData.SkeletonResourceID != "")
            {
                ext.SkeletonResourceID = group.ExtendedData.SkeletonResourceID;
            }
        }
    }

    private void ExportSkeletonExtensions(Skeleton skeleton, GLTFSceneExtensions ext)
    {
        ext.BoneOrder = [];
        foreach (var joint in skeleton.Bones)
        {
            ext.BoneOrder[joint.Name] = joint.ExportIndex + 1;
        }
    }

    private void ExportMeshExtensions(Mesh mesh, GLTFMeshExtensions ext)
    {
        var extd = mesh.ExtendedData;
        var user = extd.UserMeshProperties;
        ext.Rigid = user.MeshFlags.IsRigid() || extd.Rigid == 1;
        ext.Cloth = user.MeshFlags.IsCloth() || extd.Cloth == 1;
        ext.MeshProxy = user.MeshFlags.IsMeshProxy() || extd.MeshProxy == 1;
        ext.ProxyGeometry = user.MeshFlags.HasProxyGeometry();
        ext.Spring = user.MeshFlags.IsSpring() || extd.Spring == 1;
        ext.Occluder = user.MeshFlags.IsOccluder() || extd.Occluder == 1;
        ext.ClothPhysics = user.ClothFlags.HasClothPhysics();
        ext.Cloth01 = user.ClothFlags.HasClothFlag01();
        ext.Cloth02 = user.ClothFlags.HasClothFlag02();
        ext.Cloth04 = user.ClothFlags.HasClothFlag04();
        ext.Impostor = user.IsImpostor[0]  == 1;
        ext.ExportOrder = mesh.ExportOrder;
        ext.LOD = (user.Lod[0] >= 0) ? user.Lod[0] : 0;
        ext.LODDistance = (user.LodDistance[0] < 100000000.0f) ? user.LodDistance[0] : 0.0f;
        if (!mesh.IsSkinned() && mesh.BoneBindings != null && mesh.BoneBindings.Count == 1)
        {
            ext.ParentBone = mesh.BoneBindings[0].BoneName;
        }
    }

    private void ExportExtensions(Root root, ModelRoot modelRoot)
    {
        var sceneExt = modelRoot.LogicalScenes.First().UseExtension<GLTFSceneExtensions>();
        ExportSceneExtensions(root, sceneExt);

        foreach (var mesh in modelRoot.LogicalMeshes)
        {
            foreach (var grMesh in root.Meshes)
            {
                if (mesh.Name == grMesh.Name)
                {
                    var meshExt = mesh.UseExtension<GLTFMeshExtensions>();
                    ExportMeshExtensions(grMesh, meshExt);
                    break;
                }
            }
        }

        if (modelRoot.LogicalSkins.Count > 0)
        {
            ExportSkeletonExtensions(root.Skeletons[0], sceneExt);
        }
    }

    private AffineTransform ToGLTFTransform(Transform t)
    {
        return new AffineTransform(
            new Vector3(t.ScaleShear[0,0], t.ScaleShear[1,1], t.ScaleShear[2,2]),
            t.Rotation.ToNumerics(),
            t.Translation.ToNumerics()
        );
    }

    private void ExportModel(Root root, Model model, SceneBuilder scene)
    {
        Skeleton skel = null;
        if (model.Skeleton != null && !model.Skeleton.IsDummy && model.Skeleton.Bones.Count > 1 && root.Skeletons.Any(s => s.Name == model.Skeleton.Name))
        {
            skel = model.Skeleton;
        }

        foreach (var meshBinding in model.MeshBindings ?? [])
        {
            ExportMeshBinding(model, skel, meshBinding, scene);
        }
    }

    private void ExportAnimationTrack(TransformTrack track, NodeBuilder joint, string animName)
    {
        var keyframes = track.ToKeyframes();

        var translate = joint.UseTranslation().UseTrackBuilder(animName);
        var rotation = joint.UseRotation().UseTrackBuilder(animName);
        var scale = joint.UseScale().UseTrackBuilder(animName);

        foreach (var (time, frame) in keyframes.Keyframes)
        {
            if (frame.HasTranslation)
            {
                var v = frame.Translation;
                translate.SetPoint(time, v.ToNumerics(), true);
            }

            if (frame.HasRotation)
            {
                var q = frame.Rotation;
                rotation.SetPoint(time, q.ToNumerics(), true);
            }

            if (frame.HasScaleShear)
            {
                var m = frame.ScaleShear;
                scale.SetPoint(time, new Vector3(m[0,0], m[1,1], m[2,2]), true);
            }
        }
    }

    private void ExportAnimation(Animation anim)
    {
        if (Skeletons.Count != 1)
        {
            throw new ParsingException("Exporting .GR2 animations without skeleton data is not supported");
        }

        if (anim.TrackGroups.Count != 1)
        {
            throw new ParsingException("Exporting .GR2 animations with multiple track groups is not supported");
        }

        var group = anim.TrackGroups[0];
        foreach (var track in group.TransformTracks)
        {
            var joint = Skeletons.First().Value.Names[track.Name];
            ExportAnimationTrack(track, joint, anim.Name);
        }
    }


    private SceneBuilder ExportScene(Root root)
    {
        var scene = new SceneBuilder();
        GenerateUniqueMeshIds(root.Meshes ?? []);

        foreach (var skeleton in root.Skeletons ?? [])
        {
            if (!skeleton.IsDummy)
            {
                var joints = ExportSkeleton(null, skeleton);
                Skeletons.Add(skeleton, joints);
            }
        }

        foreach (var model in root.Models ?? [])
        {
            ExportModel(root, model, scene);
        }

        if (root.Animations != null && root.Animations.Count > 1)
        {
            throw new ParsingException("Exporting .GR2 files with multiple animations is not supported");
        }

        foreach (var animation in root.Animations ?? [])
        {
            ExportAnimation(animation);
        }

        foreach (var skeleton in Skeletons)
        {
            if (!skeleton.Value.UsedForSkinning)
            {
                scene.AddNode(skeleton.Value.Root);
            }
        }

        return scene;
    }

    private SharpGLTF.Schema2.Node FindRoot(ModelRoot root, NodeBuilder node)
    {
        foreach (var n in root.LogicalNodes)
        {
            if (node.Name == n.Name && n.VisualParent == null)
            {
                return n;
            }
        }

        return null;
    }

    private SharpGLTF.Schema2.Node FindNode(ModelRoot root, NodeBuilder node)
    {
        foreach (var n in root.LogicalNodes)
        {
            if (node.Name == n.Name && n.VisualParent?.Name == node.Parent?.Name)
            {
                return n;
            }
        }

        return null;
    }

    private void ExportSkin(ModelRoot root, GLTFSkeletonExportData skeleton)
    {
        var skelRoot = FindRoot(root, skeleton.Root);

        List<(SharpGLTF.Schema2.Node Joint, Matrix4x4 InverseBindMatrix)> joints = [];
        foreach (var (joint, bindMat) in skeleton.Joints)
        {
            var mapped = FindNode(root, joint);
            if (mapped == null)
            {
                throw new ParsingException($"Unable to find bone {joint.Name} in gltf node tree");
            }

            joints.Add((mapped, bindMat));
        }

        var skin = skelRoot.LogicalParent.CreateSkin();
        skin.BindJoints(joints);
    }


    public void Export(Root root, string outputPath)
    {
        GLTFExtensions.RegisterExtensions();
        var scene = ExportScene(root);
        var modelRoot = scene.ToGltf2();

        // Add skins for skeletons that were not used for skinning any mesh
        foreach (var skeleton in Skeletons)
        {
            if (!skeleton.Value.UsedForSkinning)
            {
                ExportSkin(modelRoot, skeleton.Value);
            }
        }

        ExportExtensions(root, modelRoot);

        switch (Options.OutputFormat)
        {
            case ExportFormat.GLTF:
                modelRoot.SaveGLTF(Options.OutputPath);
                break;
            case ExportFormat.GLB:
                modelRoot.SaveGLB(Options.OutputPath);
                break;
            default:
                throw new NotImplementedException();
        }
    }
}
