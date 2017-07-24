using System;
using System.Collections.Generic;
using System.Linq;
using Collada141;
using LSLib.Granny.GR2;
using LSLib.LS;

namespace LSLib.Granny.Model
{
    public class ColladaMeshExporter
    {
        private Mesh ExportedMesh;
        private ExporterOptions Options;
        private List<source> Sources;
        private List<InputLocal> Inputs;
        private List<InputLocalOffset> InputOffsets;
        private ulong LastInputOffset = 0;


        public ColladaMeshExporter(Mesh mesh, ExporterOptions options)
        {
            ExportedMesh = mesh;
            Options = options;
        }

        private void AddInput(source collSource, string inputSemantic, string localInputSemantic = null)
        {
            if (collSource != null)
            {
                Sources.Add(collSource);
            }

            if (inputSemantic != null)
            {
                var input = new InputLocal();
                input.semantic = inputSemantic;
                input.source = "#" + collSource.id;
                Inputs.Add(input);
            }

            if (localInputSemantic != null)
            {
                var vertexInputOff = new InputLocalOffset();
                vertexInputOff.semantic = localInputSemantic;
                vertexInputOff.source = "#" + collSource.id;
                vertexInputOff.offset = LastInputOffset++;
                InputOffsets.Add(vertexInputOff);
            }
        }

        private void DetermineInputsFromComponentNames(List<string> componentNames)
        {
            foreach (var component in componentNames)
            {
                switch (component)
                {
                    case "Position":
                        {
                            var positions = ExportedMesh.PrimaryVertexData.MakeColladaPositions(ExportedMesh.Name);
                            AddInput(positions, "POSITION", "VERTEX");
                            break;
                        }

                    case "Normal":
                        {
                            if (Options.ExportNormals)
                            {
                                var normals = ExportedMesh.PrimaryVertexData.MakeColladaNormals(ExportedMesh.Name);
                                AddInput(normals, "NORMAL");
                            }
                            break;
                        }

                    case "Tangent":
                        {
                            if (Options.ExportTangents)
                            {
                                var tangents = ExportedMesh.PrimaryVertexData.MakeColladaTangents(ExportedMesh.Name);
                                AddInput(tangents, "TANGENT");
                            }
                            break;
                        }

                    case "Binormal":
                        {
                            if (Options.ExportTangents)
                            {
                                var binormals = ExportedMesh.PrimaryVertexData.MakeColladaBinormals(ExportedMesh.Name);
                                AddInput(binormals, "BINORMAL");
                            }
                            break;
                        }

                    case "TextureCoordinates0":
                    case "TextureCoordinates1":
                        {
                            if (Options.ExportUVs)
                            {
                                int uvIndex = Int32.Parse(component.Substring(component.Length - 1));
                                var uvs = ExportedMesh.PrimaryVertexData.MakeColladaUVs(ExportedMesh.Name, uvIndex);
                                AddInput(uvs, null, "TEXCOORD");
                            }
                            break;
                        }

                    // Same as TextureCoordinatesX, but with 1-based indices
                    case "MaxChannel_1":
                    case "MaxChannel_2":
                    case "UVChannel_1":
                    case "UVChannel_2":
                    case "map1":
                        {
                            if (Options.ExportUVs)
                            {
                                int uvIndex = Int32.Parse(component.Substring(component.Length - 1)) - 1;
                                var uvs = ExportedMesh.PrimaryVertexData.MakeColladaUVs(ExportedMesh.Name, uvIndex);
                                AddInput(uvs, null, "TEXCOORD");
                            }
                            break;
                        }

                    case "BoneWeights":
                    case "BoneIndices":
                        // These are handled in ExportSkin()
                        break;

                    case "DiffuseColor0":
                        // TODO: This is not exported at the moment.
                        break;

                    default:
                        throw new NotImplementedException("Vertex component not supported: " + component);
                }
            }
        }

        private void DetermineInputsFromVertex(Vertex vertex)
        {
            var desc = Vertex.Description(vertex.GetType());
            if (!desc.Position)
            {
                throw new NotImplementedException("Cannot import vertices without position");
            }

            // Vertex positions
            var positions = ExportedMesh.PrimaryVertexData.MakeColladaPositions(ExportedMesh.Name);
            AddInput(positions, "POSITION", "VERTEX");

            // Normals
            if (desc.Normal && Options.ExportNormals)
            {
                var normals = ExportedMesh.PrimaryVertexData.MakeColladaNormals(ExportedMesh.Name);
                AddInput(normals, "NORMAL");
            }

            // Tangents
            if (desc.Tangent && Options.ExportTangents)
            {
                var normals = ExportedMesh.PrimaryVertexData.MakeColladaTangents(ExportedMesh.Name);
                AddInput(normals, "TANGENT");
            }

            // Binormals
            if (desc.Binormal && Options.ExportTangents)
            {
                var normals = ExportedMesh.PrimaryVertexData.MakeColladaBinormals(ExportedMesh.Name);
                AddInput(normals, "BINORMAL");
            }

            // Texture coordinates
            if (Options.ExportUVs)
            {
                for (var uvIndex = 0; uvIndex < desc.TextureCoordinates; uvIndex++)
                {
                    var uvs = ExportedMesh.PrimaryVertexData.MakeColladaUVs(ExportedMesh.Name, uvIndex);
                    AddInput(uvs, null, "TEXCOORD");
                }
            }

            // BoneWeights and BoneIndices are handled in ExportSkin()
            // TODO: DiffuseColor0 is not exported at the moment.
        }

        public mesh Export()
        {
            Sources = new List<source>();
            Inputs = new List<InputLocal>();
            InputOffsets = new List<InputLocalOffset>();
            LastInputOffset = 0;

            var vertexData = ExportedMesh.PrimaryVertexData;
            if (vertexData.Vertices != null
                && vertexData.Vertices.Count > 0)
            {
                var vertex = vertexData.Vertices[0];
                DetermineInputsFromVertex(vertex);
            }
            else
            {
                var componentNames = ExportedMesh.VertexComponentNames();
                DetermineInputsFromComponentNames(componentNames);
            }

            // TODO: model transform/inverse transform?
            var triangles = ExportedMesh.PrimaryTopology.MakeColladaTriangles(
                InputOffsets.ToArray(),
                vertexData.Deduplicator.VertexDeduplicationMap,
                vertexData.Deduplicator.UVDeduplicationMaps
            );

            var colladaMesh = new mesh();
            colladaMesh.vertices = new vertices();
            colladaMesh.vertices.id = ExportedMesh.Name + "-vertices";
            colladaMesh.vertices.input = Inputs.ToArray();
            colladaMesh.source = Sources.ToArray();
            colladaMesh.Items = new object[] { triangles };

            return colladaMesh;
        }
    }


    public class ColladaExporter
    {
        [Serialization(Kind = SerializationKind.None)]
        public ExporterOptions Options = new ExporterOptions();


        private void ExportMeshBinding(Model model, string skelRef, MeshBinding meshBinding, List<geometry> geometries, List<controller> controllers, List<node> geomNodes)
        {
            var exporter = new ColladaMeshExporter(meshBinding.Mesh, Options);
            var mesh = exporter.Export();
            var geom = new geometry();
            geom.id = meshBinding.Mesh.Name + "-geom";
            geom.name = meshBinding.Mesh.Name;
            geom.Item = mesh;
            geometries.Add(geom);

            bool hasSkin = skelRef != null && meshBinding.Mesh.IsSkinned();
            skin skin = null;
            controller ctrl = null;
            if (hasSkin)
            {
                var boneNames = new Dictionary<string, Bone>();
                foreach (var bone in model.Skeleton.Bones)
                {
                    boneNames.Add(bone.Name, bone);
                }

                skin = ExportSkin(meshBinding.Mesh, model.Skeleton.Bones, boneNames, geom.id);
                ctrl = new controller();
                ctrl.id = meshBinding.Mesh.Name + "-skin";
                ctrl.name = meshBinding.Mesh.Name + "_Skin";
                ctrl.Item = skin;
                controllers.Add(ctrl);
            }

            var geomNode = new node();
            geomNode.id = geom.name + "-node";
            geomNode.name = geom.name;
            geomNode.type = NodeType.NODE;

            if (hasSkin)
            {
                var controllerInstance = new instance_controller();
                controllerInstance.url = "#" + ctrl.id;
                controllerInstance.skeleton = new string[] { "#" + skelRef };
                geomNode.instance_controller = new instance_controller[] { controllerInstance };
            }
            else
            {
                var geomInstance = new instance_geometry();
                geomInstance.url = "#" + geom.id;
                geomNode.instance_geometry = new instance_geometry[] { geomInstance };
            }

            geomNodes.Add(geomNode);
        }

        private skin ExportSkin(Mesh mesh, List<Bone> bones, Dictionary<string, Bone> nameMaps, string geometryId)
        {
            var sources = new List<source>();
            var joints = new List<string>();
            var poses = new List<float>();

            var boundBones = new HashSet<string>();
            var orderedBones = new List<Bone>();
            foreach (var boneBinding in mesh.BoneBindings)
            {
                boundBones.Add(boneBinding.BoneName);
                orderedBones.Add(nameMaps[boneBinding.BoneName]);
            }

            /*
             * Append all bones to the end of the bone list, even if they're not influencing the mesh.
             * We need this because some tools (eg. Blender) expect all bones to be present, otherwise their
             * inverse world transform would reset to identity.
             */
            foreach (var bone in bones)
            {
                if (!boundBones.Contains(bone.Name))
                {
                    orderedBones.Add(bone);
                }
            }

            foreach (var bone in orderedBones)
            {
                boundBones.Add(bone.Name);
                joints.Add(bone.Name);

                var invWorldTransform = ColladaHelpers.FloatsToMatrix(bone.InverseWorldTransform);
                invWorldTransform.Transpose();

                poses.AddRange(new float[] {
                    invWorldTransform.M11, invWorldTransform.M12, invWorldTransform.M13, invWorldTransform.M14,
                    invWorldTransform.M21, invWorldTransform.M22, invWorldTransform.M23, invWorldTransform.M24,
                    invWorldTransform.M31, invWorldTransform.M32, invWorldTransform.M33, invWorldTransform.M34,
                    invWorldTransform.M41, invWorldTransform.M42, invWorldTransform.M43, invWorldTransform.M44
                });
            }

            var jointSource = ColladaUtils.MakeNameSource(mesh.Name, "joints", new string[] { "JOINT" }, joints.ToArray());
            var poseSource = ColladaUtils.MakeFloatSource(mesh.Name, "poses", new string[] { "TRANSFORM" }, poses.ToArray(), 16, "float4x4");
            var weightsSource = mesh.PrimaryVertexData.MakeBoneWeights(mesh.Name);

            var vertices = mesh.PrimaryVertexData.Deduplicator.DeduplicatedPositions;
            var vertexInfluenceCounts = new List<int>(vertices.Count);
            var vertexInfluences = new List<int>(vertices.Count);
            int weightIdx = 0;
            foreach (var vertex in vertices)
            {
                int influences = 0;
                var indices = vertex.BoneIndices;
                var weights = vertex.BoneWeights;
                for (int i = 0; i < 4; i++)
                {
                    if (weights[i] > 0)
                    {
                        influences++;
                        vertexInfluences.Add(indices[i]);
                        vertexInfluences.Add(weightIdx++);
                    }
                }

                vertexInfluenceCounts.Add(influences);
            }

            var jointOffsets = new InputLocalOffset();
            jointOffsets.semantic = "JOINT";
            jointOffsets.source = "#" + jointSource.id;
            jointOffsets.offset = 0;

            var weightOffsets = new InputLocalOffset();
            weightOffsets.semantic = "WEIGHT";
            weightOffsets.source = "#" + weightsSource.id;
            weightOffsets.offset = 1;

            var vertWeights = new skinVertex_weights();
            vertWeights.count = (ulong)vertices.Count;
            vertWeights.input = new InputLocalOffset[] { jointOffsets, weightOffsets };
            vertWeights.v = string.Join(" ", vertexInfluences.Select(x => x.ToString()).ToArray());
            vertWeights.vcount = string.Join(" ", vertexInfluenceCounts.Select(x => x.ToString()).ToArray());

            var skin = new skin();
            skin.source1 = "#" + geometryId;
            skin.bind_shape_matrix = "1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1";

            var skinJoints = new skinJoints();
            var skinJointInput = new InputLocal();
            skinJointInput.semantic = "JOINT";
            skinJointInput.source = "#" + jointSource.id;
            var skinInvBindInput = new InputLocal();
            skinInvBindInput.semantic = "INV_BIND_MATRIX";
            skinInvBindInput.source = "#" + poseSource.id;
            skinJoints.input = new InputLocal[] { skinJointInput, skinInvBindInput };

            skin.joints = skinJoints;
            skin.source = new source[] { jointSource, poseSource, weightsSource };
            skin.vertex_weights = vertWeights;

            return skin;
        }

        private node ExportBone(Skeleton skeleton, string name, int index, Bone bone)
        {
            var node = bone.MakeCollada(name);
            var children = new List<node>();
            for (int i = 0; i < skeleton.Bones.Count; i++)
            {
                if (skeleton.Bones[i].ParentIndex == index)
                    children.Add(ExportBone(skeleton, name, i, skeleton.Bones[i]));
            }

            node.node1 = children.ToArray();
            return node;
        }

        public node ExportSkeleton(Skeleton skeleton, string name)
        {
            // Find the root bone and export it
            for (int i = 0; i < skeleton.Bones.Count; i++)
            {
                if (skeleton.Bones[i].ParentIndex == -1)
                {
                    var root = ExportBone(skeleton, name, i, skeleton.Bones[i]);
                    // root.id = Name + "-skeleton";
                    return root;
                }
            }

            throw new ParsingException("Model has no root bone!");
        }

        private void ExportModels(Root root, List<geometry> geometries, List<controller> controllers, List<node> geomNodes)
        {
            if (root.Models == null)
            {
                return;
            }

            foreach (var model in root.Models)
            {
                string skelRef = null;
                if (model.Skeleton != null && !model.Skeleton.IsDummy && model.Skeleton.Bones.Count > 1)
                {
                    var skeleton = ExportSkeleton(model.Skeleton, model.Name);
                    geomNodes.Add(skeleton);
                    skelRef = skeleton.id;
                }

                if (model.MeshBindings != null)
                {
                    foreach (var meshBinding in model.MeshBindings)
                    {
                        ExportMeshBinding(model, skelRef, meshBinding, geometries, controllers, geomNodes);
                    }
                }
            }
        }

        public void Export(Root root, string outputPath)
        {
            var collada = new COLLADA();
            var asset = new asset();
            var contributor = new assetContributor();
            if (root.ArtToolInfo != null)
                contributor.authoring_tool = root.ArtToolInfo.FromArtToolName;
            else
                contributor.authoring_tool = "LSLib COLLADA Exporter v" + Common.LibraryVersion();
            asset.contributor = new assetContributor[] { contributor };
            asset.created = DateTime.Now;
            asset.modified = DateTime.Now;
            asset.unit = new assetUnit();
            asset.unit.name = "meter";
            // TODO: Handle up vector, etc. properly?
            if (root.ArtToolInfo != null)
                asset.unit.meter = root.ArtToolInfo.UnitsPerMeter;
            else
                asset.unit.meter = 1;
            asset.up_axis = UpAxisType.Y_UP;
            collada.asset = asset;

            var geometries = new List<geometry>();
            var controllers = new List<controller>();
            var geomNodes = new List<node>();
            ExportModels(root, geometries, controllers, geomNodes);

            var animations = new List<animation>();
            var animationClips = new List<animation_clip>();
            if (root.Animations != null)
            {
                foreach (var anim in root.Animations)
                {
                    var anims = anim.ExportAnimations();
                    animations.AddRange(anims);
                    var clip = new animation_clip();
                    clip.id = anim.Name + "_Animation";
                    clip.name = anim.Name;
                    clip.start = 0.0;
                    clip.end = anim.Duration;
                    clip.endSpecified = true;

                    var animInstances = new List<InstanceWithExtra>();
                    foreach (var animChannel in anims)
                    {
                        var instance = new InstanceWithExtra();
                        instance.url = "#" + animChannel.id;
                        animInstances.Add(instance);
                    }

                    clip.instance_animation = animInstances.ToArray();
                    animationClips.Add(clip);
                }
            }

            var rootElements = new List<object>();

            if (animations.Count > 0)
            {
                var animationLib = new library_animations();
                animationLib.animation = animations.ToArray();
                rootElements.Add(animationLib);
            }

            if (animationClips.Count > 0)
            {
                var animationClipLib = new library_animation_clips();
                animationClipLib.animation_clip = animationClips.ToArray();
                rootElements.Add(animationClipLib);
            }

            if (geometries.Count > 0)
            {
                var geometryLib = new library_geometries();
                geometryLib.geometry = geometries.ToArray();
                rootElements.Add(geometryLib);
            }

            if (controllers.Count > 0)
            {
                var controllerLib = new library_controllers();
                controllerLib.controller = controllers.ToArray();
                rootElements.Add(controllerLib);
            }

            var visualScenes = new library_visual_scenes();
            var visualScene = new visual_scene();
            visualScene.id = "DefaultVisualScene";
            visualScene.name = "unnamed";

            visualScene.node = geomNodes.ToArray();
            visualScenes.visual_scene = new visual_scene[] { visualScene };

            var visualSceneInstance = new InstanceWithExtra();
            visualSceneInstance.url = "#DefaultVisualScene";
            rootElements.Add(visualScenes);

            var scene = new COLLADAScene();
            scene.instance_visual_scene = visualSceneInstance;
            collada.scene = scene;

            collada.Items = rootElements.ToArray();

            collada.Save(outputPath);
        }
    }
}
