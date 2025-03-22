using LSLib.Granny.GR2;
using System.Text.RegularExpressions;
using SharpGLTF.Transforms;
using System.Numerics;
using SharpGLTF.Scenes;
using LSLib.LS;
using SharpGLTF.Schema2;

namespace LSLib.Granny.Model;

public class GLTFExporter
{
    [Serialization(Kind = SerializationKind.None)]
    public ExporterOptions Options = new();

    private Dictionary<Mesh, string> MeshIds = new();
    private Dictionary<Skeleton, List<NodeBuilder>> Skeletons = new();

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
        var exporter = new GLTFMeshExporter(meshBinding.Mesh, meshId);
        var mesh = exporter.Export();

        if (skeleton == null || !meshBinding.Mesh.VertexFormat.HasBoneWeights)
        {
            scene.AddRigidMesh(mesh, new AffineTransform(Matrix4x4.Identity));
        }
        else
        {
            scene.AddSkinnedMesh(mesh, Matrix4x4.Identity, Skeletons[skeleton].ToArray());
        }
    }

    private List<NodeBuilder> ExportSkeleton(NodeBuilder root, Skeleton skeleton)
    {
        var joints = new List<NodeBuilder>();
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
                node = joints[joint.ParentIndex].CreateNode(joint.Name);
            }

            node.LocalTransform = ToGLTFTransform(joint.Transform);
            joints.Add(node);
        }

        return joints;
    }

    private void ExportSceneExtensions(Root root, GLTFSceneExtensions ext)
    {
        ext.MetadataVersion = Common.GLTFMetadataVersion;
        ext.LSLibMajor = Common.MajorVersion;
        ext.LSLibMinor = Common.MinorVersion;
        ext.LSLibPatch = Common.PatchVersion;
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
            new Quaternion(t.Rotation.X, t.Rotation.Y, t.Rotation.Z, t.Rotation.W),
            new Vector3(t.Translation.X, t.Translation.Y, t.Translation.Z)
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


    private SceneBuilder ExportScene(Root root)
    {
        var scene = new SceneBuilder();
        GenerateUniqueMeshIds(root.Meshes ?? []);

        foreach (var skeleton in root.Skeletons ?? [])
        {
            //var skelRoot = new NodeBuilder();
            //skelRoot.Name = skeleton.Name;
            //scene.AddNode(skelRoot);

            var joints = ExportSkeleton(null, skeleton);
            Skeletons.Add(skeleton, joints);
        }

        foreach (var model in root.Models ?? [])
        {
            ExportModel(root, model, scene);
        }

        return scene;
    }


    public void Export(Root root, string outputPath)
    {
        GLTFExtensions.RegisterExtensions();
        var scene = ExportScene(root);
        var modelRoot = scene.ToGltf2();

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
