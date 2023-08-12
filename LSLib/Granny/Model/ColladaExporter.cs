using System;
using System.Collections.Generic;
using System.Linq;
using LSLib.Granny.GR2;
using LSLib.LS;
using Alphaleonis.Win32.Filesystem;
using System.Xml;
using System.Xml.Linq;
using LSLib.LS.Enums;

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
        private XmlDocument Xml = new XmlDocument();


        public ColladaMeshExporter(Mesh mesh, ExporterOptions options)
        {
            ExportedMesh = mesh;
            Options = options;
        }

        private void AddInput(source collSource, string inputSemantic, string localInputSemantic = null, ulong setIndex = 0)
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
                if (localInputSemantic == "TEXCOORD" || localInputSemantic == "COLOR")
                {
                    vertexInputOff.set = setIndex;
                }

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
                                AddInput(tangents, "TEXTANGENT");
                            }
                            break;
                        }

                    case "Binormal":
                        {
                            if (Options.ExportTangents)
                            {
                                var binormals = ExportedMesh.PrimaryVertexData.MakeColladaBinormals(ExportedMesh.Name);
                                AddInput(binormals, "TEXBINORMAL");
                            }
                            break;
                        }

                    case "TextureCoordinates0":
                    case "TextureCoordinates1":
                    case "TextureCoordinates2":
                    case "TextureCoordinates3":
                    case "TextureCoordinates4":
                    case "TextureCoordinates5":
                        {
                            if (Options.ExportUVs)
                            {
                                int uvIndex = Int32.Parse(component.Substring(component.Length - 1));
                                var uvs = ExportedMesh.PrimaryVertexData.MakeColladaUVs(ExportedMesh.Name, uvIndex, Options.FlipUVs);
                                AddInput(uvs, null, "TEXCOORD", (ulong)uvIndex);
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
                                var uvs = ExportedMesh.PrimaryVertexData.MakeColladaUVs(ExportedMesh.Name, uvIndex, Options.FlipUVs);
                                AddInput(uvs, null, "TEXCOORD", (ulong)uvIndex);
                            }
                            break;
                        }

                    case "BoneWeights":
                    case "BoneIndices":
                        // These are handled in ExportSkin()
                        break;

                    case "DiffuseColor0":
                        {
                            if (Options.ExportColors)
                            {
                                var colors = ExportedMesh.PrimaryVertexData.MakeColladaColors(ExportedMesh.Name, 0);
                                AddInput(colors, null, "COLOR", 0);
                            }
                            break;
                        }

                    default:
                        throw new NotImplementedException("Vertex component not supported: " + component);
                }
            }
        }

        private void DetermineInputsFromVertex(Vertex vertex)
        {
            var desc = vertex.Format;
            if (desc.PositionType == PositionType.None)
            {
                throw new NotImplementedException("Cannot import vertices without position");
            }

            // Vertex positions
            var positions = ExportedMesh.PrimaryVertexData.MakeColladaPositions(ExportedMesh.Name);
            AddInput(positions, "POSITION", "VERTEX");

            // Normals
            if (desc.NormalType != NormalType.None && Options.ExportNormals)
            {
                var normals = ExportedMesh.PrimaryVertexData.MakeColladaNormals(ExportedMesh.Name);
                AddInput(normals, null, "NORMAL");
            }

            // Tangents
            if (desc.TangentType != NormalType.None && Options.ExportTangents)
            {
                var normals = ExportedMesh.PrimaryVertexData.MakeColladaTangents(ExportedMesh.Name);
                AddInput(normals, null, "TEXTANGENT");
            }

            // Binormals
            if (desc.BinormalType != NormalType.None && Options.ExportTangents)
            {
                var normals = ExportedMesh.PrimaryVertexData.MakeColladaBinormals(ExportedMesh.Name);
                AddInput(normals, null, "TEXBINORMAL");
            }

            // Texture coordinates
            if (Options.ExportUVs)
            {
                for (var uvIndex = 0; uvIndex < desc.TextureCoordinates; uvIndex++)
                {
                    var uvs = ExportedMesh.PrimaryVertexData.MakeColladaUVs(ExportedMesh.Name, uvIndex, Options.FlipUVs);
                    AddInput(uvs, null, "TEXCOORD", (ulong)uvIndex);
                }
            }

            // Vertex colors
            if (Options.ExportColors)
            {
                for (var colorIndex = 0; colorIndex < desc.ColorMaps; colorIndex++)
                {
                    var colors = ExportedMesh.PrimaryVertexData.MakeColladaColors(ExportedMesh.Name, colorIndex);
                    AddInput(colors, null, "COLOR", (ulong)colorIndex);
                }
            }

            // BoneWeights and BoneIndices are handled in ExportSkin()
        }

        private void AddTechniqueProperty(List<XmlElement> props, string property, string value)
        {
            var prop = Xml.CreateElement(property);
            prop.InnerText = value;
            props.Add(prop);
        }

        private technique ExportLSLibProfile()
        {
            var profile = new technique()
            {
                profile = "LSTools"
            };

            var props = new List<XmlElement>();

            if (ExportedMesh.ExportOrder != -1)
            {
                AddTechniqueProperty(props, "ExportOrder", ExportedMesh.ExportOrder.ToString());
            }

            var userProps = ExportedMesh.ExtendedData?.UserMeshProperties;
            if (userProps != null)
            {
                var flags = userProps.MeshFlags;
                var clothFlags = userProps.ClothFlags;

                if (flags.IsMeshProxy())
                {
                    AddTechniqueProperty(props, "DivModelType", "MeshProxy");
                }

                if (flags.IsCloth())
                {
                    AddTechniqueProperty(props, "DivModelType", "Cloth");
                }

                if (flags.HasProxyGeometry())
                {
                    AddTechniqueProperty(props, "DivModelType", "ProxyGeometry");
                }

                if (flags.IsRigid())
                {
                    AddTechniqueProperty(props, "DivModelType", "Rigid");
                }

                if (flags.IsSpring())
                {
                    AddTechniqueProperty(props, "DivModelType", "Spring");
                }

                if (flags.IsOccluder())
                {
                    AddTechniqueProperty(props, "DivModelType", "Occluder");
                }

                if (clothFlags.HasClothFlag01())
                {
                    AddTechniqueProperty(props, "DivModelType", "Cloth01");
                }

                if (clothFlags.HasClothFlag02())
                {
                    AddTechniqueProperty(props, "DivModelType", "Cloth02");
                }

                if (clothFlags.HasClothFlag04())
                {
                    AddTechniqueProperty(props, "DivModelType", "Cloth04");
                }

                if (clothFlags.HasClothPhysics())
                {
                    AddTechniqueProperty(props, "DivModelType", "ClothPhysics");
                }

                if (userProps.IsImpostor != null && userProps.IsImpostor[0] == 1)
                {
                    AddTechniqueProperty(props, "IsImpostor", "1");
                }

                if (userProps.Lod != null && userProps.Lod[0] != -1)
                {
                    AddTechniqueProperty(props, "LOD", $"{userProps.Lod[0]}");
                }

                if (userProps.LodDistance != null && userProps.LodDistance[0] > 0 && userProps.LodDistance[0] < 1.0E+30f)
                {
                    AddTechniqueProperty(props, "LODDistance", $"{userProps.LodDistance[0]}");
                }
            }

            profile.Any = props.ToArray();
            return profile;
        }

        public mesh Export()
        {
            // Jank we need to create XMLElements on the fly
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
                vertexData.Deduplicator.Vertices.DeduplicationMap,
                vertexData.Deduplicator.Normals.DeduplicationMap,
                vertexData.Deduplicator.UVs.Select(uv => uv.DeduplicationMap).ToList(),
                vertexData.Deduplicator.Colors.Select(color => color.DeduplicationMap).ToList()
            );

            var colladaMesh = new mesh();
            colladaMesh.vertices = new vertices();
            colladaMesh.vertices.id = ExportedMesh.Name + "-vertices";
            colladaMesh.vertices.input = Inputs.ToArray();
            colladaMesh.source = Sources.ToArray();
            colladaMesh.Items = new object[] { triangles };
            colladaMesh.extra = new extra[]
            {
                new extra
                {
                    technique = new technique[]
                    {
                        ExportLSLibProfile()
                    }
                }
            };

            return colladaMesh;
        }
    }


    public class ColladaExporter
    {
        [Serialization(Kind = SerializationKind.None)]
        public ExporterOptions Options = new ExporterOptions();

        private XmlDocument Xml = new XmlDocument();

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

            var vertices = mesh.PrimaryVertexData.Deduplicator.Vertices.Uniques;
            var vertexInfluenceCounts = new List<int>(vertices.Count);
            var vertexInfluences = new List<int>(vertices.Count);
            int weightIdx = 0;
            foreach (var vertex in vertices)
            {
                int influences = 0;
                var indices = vertex.Indices;
                var weights = vertex.Weights;
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
            var node = bone.MakeCollada(Xml);
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
            int rootIndex = -1;

            // Find the root bone and export it
            for (var i = 0; i < skeleton.Bones.Count; i++)
            {
                if (skeleton.Bones[i].IsRoot)
                {
                    if (rootIndex == -1)
                    {
                        rootIndex = i;
                    }
                    else
                    {
                        throw new ParsingException(
                            "Model has multiple root bones! Please use the \"Conform to GR2\" option to " +
                            "make sure that all bones from the base mesh are included in the export.");
                    }
                }
            }

            if (rootIndex == -1)
            {
                throw new ParsingException("Model has no root bone!");
            }
            
            return ExportBone(skeleton, name, rootIndex, skeleton.Bones[rootIndex]);
        }

        private void ExportModels(Root root, List<geometry> geometries, List<controller> controllers, List<node> geomNodes)
        {
            if (root.Models == null)
            {
                return;
            }

            foreach(var model in root.Models)
			{
                string skelRef = null;
                if (model.Skeleton != null && !model.Skeleton.IsDummy && model.Skeleton.Bones.Count > 1 && root.Skeletons.Any(s => s.Name == model.Skeleton.Name))
                {
					Utils.Info($"Exporting model {model.Name} with skeleton {model.Skeleton.Name}");
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

        private technique ExportAnimationLSLibProfile(BG3TrackGroupExtendedData extData)
        {
            var profile = new technique()
            {
                profile = "LSTools"
            };

            var props = new List<XmlElement>();
            if (extData != null && extData.SkeletonResourceID != null && extData.SkeletonResourceID != "")
            {
                var prop = Xml.CreateElement("SkeletonResourceID");
                prop.InnerText = extData.SkeletonResourceID;
                props.Add(prop);
            }

            profile.Any = props.ToArray();
            return profile;
        }

        public List<animation> ExportKeyframeTrack(TransformTrack transformTrack, BG3TrackGroupExtendedData extData, string name, string target)
        {
            var track = transformTrack.ToKeyframes();
            track.MergeAdjacentFrames();
            track.InterpolateFrames();

            var anims = new List<animation>();
            var inputs = new List<InputLocal>();

            var outputs = new List<float>(track.Keyframes.Count * 16);
            foreach (var keyframe in track.Keyframes.Values)
            {
                var transform = keyframe.ToTransform().ToMatrix4();
                transform.Transpose();
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                        outputs.Add(transform[i, j]);
                }
            }

            var interpolations = new List<string>(track.Keyframes.Count);
            for (int i = 0; i < track.Keyframes.Count; i++)
            {
                interpolations.Add("LINEAR");
            }

            var knots = new List<float>(track.Keyframes.Count);
            foreach (var keyframe in track.Keyframes)
            {
                knots.Add(keyframe.Key);
            }

            /*
             * Fix up animations that have only one keyframe by adding another keyframe at
             * the end of the animation.
             * (This mainly applies to DaIdentity and DnConstant32f)
             */
            if (track.Keyframes.Count == 1)
            {
                knots.Add(transformTrack.ParentAnimation.Duration);
                for (int i = 0; i < 16; i++)
                    outputs.Add(outputs[i]);
                interpolations.Add(interpolations[0]);
            }

            var knotsSource = ColladaUtils.MakeFloatSource(name, "inputs", new string[] { "TIME" }, knots.ToArray());
            var knotsInput = new InputLocal();
            knotsInput.semantic = "INPUT";
            knotsInput.source = "#" + knotsSource.id;
            inputs.Add(knotsInput);

            var outSource = ColladaUtils.MakeFloatSource(name, "outputs", new string[] { "TRANSFORM" }, outputs.ToArray(), 16, "float4x4");
            var outInput = new InputLocal();
            outInput.semantic = "OUTPUT";
            outInput.source = "#" + outSource.id;
            inputs.Add(outInput);

            var interpSource = ColladaUtils.MakeNameSource(name, "interpolations", new string[] { "INTERPOLATION" }, interpolations.ToArray());

            var interpInput = new InputLocal();
            interpInput.semantic = "INTERPOLATION";
            interpInput.source = "#" + interpSource.id;
            inputs.Add(interpInput);

            var sampler = new sampler();
            sampler.id = name + "_sampler";
            sampler.input = inputs.ToArray();

            var channel = new channel();
            channel.source = "#" + sampler.id;
            channel.target = target;

            var animation = new animation();
            animation.id = name;
            animation.name = name;
            var animItems = new List<object>();
            animItems.Add(knotsSource);
            animItems.Add(outSource);
            animItems.Add(interpSource);
            animItems.Add(sampler);
            animItems.Add(channel);
            animation.Items = animItems.ToArray();

            animation.extra = new extra[]
            {
                new extra
                {
                    technique = new technique[]
                    {
                        ExportAnimationLSLibProfile(extData)
                    }
                }
            };

            anims.Add(animation);
            return anims;
        }

        public List<animation> ExportTrack(TransformTrack track, BG3TrackGroupExtendedData extData)
        {
            var anims = new List<animation>();
            var name = track.Name.Replace(' ', '_');
            var boneName = "Bone_" + track.Name.Replace(' ', '_');

            // Export all tracks in a single transform
            anims.AddRange(ExportKeyframeTrack(track, extData, name + "_Transform", boneName + "/Transform"));

            return anims;
        }

        public List<animation> ExportTracks(TrackGroup trackGroup)
        {
            var anims = new List<animation>();
            foreach (var track in trackGroup.TransformTracks)
            {
                anims.AddRange(ExportTrack(track, trackGroup.ExtendedData));
            }

            return anims;
        }

        public List<animation> ExportAnimations(Animation animation)
        {
            var animations = new List<animation>();
            foreach (var trackGroup in animation.TrackGroups)
            {
                /*
                 * We need to propagate animation data as the track exporter may need information from it
                 * (Duration and TimeStep usually)
                 */
                foreach (var track in trackGroup.TransformTracks)
                {
                    track.ParentAnimation = animation;
                    track.OrientationCurve.CurveData.ParentAnimation = animation;
                    track.PositionCurve.CurveData.ParentAnimation = animation;
                    track.ScaleShearCurve.CurveData.ParentAnimation = animation;
                }

                animations.AddRange(ExportTracks(trackGroup));
            }

            return animations;
        }

        private Game DetectGame(Root root)
        {
            if (root.GR2Tag == Header.Tag_DOS)
            {
                return Game.DivinityOriginalSin;
            }

            if (root.GR2Tag == Header.Tag_DOS2DE)
            {
                return Game.DivinityOriginalSin2DE;
            }

            if (root.GR2Tag == Header.Tag_DOSEE)
            {
                foreach (var mesh in root.Meshes ?? Enumerable.Empty<Mesh>())
                {
                    if (mesh.ExtendedData != null)
                    {
                        if (mesh.ExtendedData.LSMVersion == 0)
                        {
                            return Game.DivinityOriginalSinEE;
                        }

                        if (mesh.ExtendedData.LSMVersion == 1)
                        {
                            return Game.DivinityOriginalSin2DE;
                        }
                    }
                }

                return Game.BaldursGate3;
            }

            return Game.Unset;
        }

        private technique ExportRootLSLibProfile(Root root)
        {
            var profile = new technique()
            {
                profile = "LSTools"
            };

            var props = new List<XmlElement>();

            var prop = Xml.CreateElement("LSLibMajor");
            prop.InnerText = Common.MajorVersion.ToString();
            props.Add(prop);

            prop = Xml.CreateElement("LSLibMinor");
            prop.InnerText = Common.MinorVersion.ToString();
            props.Add(prop);

            prop = Xml.CreateElement("LSLibPatch");
            prop.InnerText = Common.PatchVersion.ToString();
            props.Add(prop);

            prop = Xml.CreateElement("MetadataVersion");
            prop.InnerText = Common.ColladaMetadataVersion.ToString();
            props.Add(prop);

            var game = DetectGame(root);
            if (game != LS.Enums.Game.Unset)
            {
                prop = Xml.CreateElement("Game");
                prop.InnerText = game.ToString();
                props.Add(prop);
            }

            profile.Any = props.ToArray();
            return profile;
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

            foreach (var anim in root.Animations ?? Enumerable.Empty<Animation>())
            {
                var anims = ExportAnimations(anim);
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

            collada.extra = new extra[]
            {
                new extra
                {
                    technique = new technique[]
                    {
                        ExportRootLSLibProfile(root)
                    }
                }
            };

            using (var stream = File.Open(outputPath, System.IO.FileMode.Create))
            {
                collada.Save(stream);
            }
        }
    }
}
