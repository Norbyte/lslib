using LSLib.Granny.GR2;
using LSLib.LS;
using System.Xml;
using LSLib.LS.Enums;

namespace LSLib.Granny.Model;

public class ColladaMeshExporter(Mesh mesh, ExporterOptions options)
{
    private Mesh ExportedMesh = mesh;
    private ExporterOptions Options = options;
    private List<source> Sources;
    private List<InputLocal> Inputs;
    private List<InputLocalOffset> InputOffsets;
    private ulong LastInputOffset = 0;
    private XmlDocument Xml = new();

    private void AddInput(source collSource, string inputSemantic, string localInputSemantic = null, ulong setIndex = 0)
    {
        if (collSource != null)
        {
            Sources.Add(collSource);
        }

        if (inputSemantic != null)
        {
            var input = new InputLocal
            {
                semantic = inputSemantic,
                source = "#" + collSource.id
            };
            Inputs.Add(input);
        }

        if (localInputSemantic != null)
        {
            var vertexInputOff = new InputLocalOffset
            {
                semantic = localInputSemantic,
                source = "#" + collSource.id,
                offset = LastInputOffset++
            };
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
                            int uvIndex = Int32.Parse(component[^1..]);
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
                            int uvIndex = Int32.Parse(component[^1..]) - 1;
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
        Sources = [];
        Inputs = [];
        InputOffsets = [];
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

        var colladaMesh = new mesh
        {
            vertices = new vertices
            {
                id = ExportedMesh.Name + "-vertices",
                input = Inputs.ToArray()
            },
            source = Sources.ToArray(),
            Items = [triangles],
            extra =
            [
                new extra
                {
                    technique =
                    [
                        ExportLSLibProfile()
                    ]
                }
            ]
        };

        return colladaMesh;
    }
}


public class ColladaExporter
{
    [Serialization(Kind = SerializationKind.None)]
    public ExporterOptions Options = new();

    private XmlDocument Xml = new();

    private void ExportMeshBinding(Model model, string skelRef, MeshBinding meshBinding, List<geometry> geometries, List<controller> controllers, List<node> geomNodes)
    {
        var exporter = new ColladaMeshExporter(meshBinding.Mesh, Options);
        var mesh = exporter.Export();
        var geom = new geometry
        {
            id = meshBinding.Mesh.Name + "-geom",
            name = meshBinding.Mesh.Name,
            Item = mesh
        };
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
            ctrl = new controller
            {
                id = meshBinding.Mesh.Name + "-skin",
                name = meshBinding.Mesh.Name + "_Skin",
                Item = skin
            };
            controllers.Add(ctrl);
        }

        var geomNode = new node
        {
            id = geom.name + "-node",
            name = geom.name,
            type = NodeType.NODE
        };

        if (hasSkin)
        {
            var controllerInstance = new instance_controller
            {
                url = "#" + ctrl.id,
                skeleton = ["#" + skelRef]
            };
            geomNode.instance_controller = [controllerInstance];
        }
        else
        {
            var geomInstance = new instance_geometry
            {
                url = "#" + geom.id
            };
            geomNode.instance_geometry = [geomInstance];
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

        var jointSource = ColladaUtils.MakeNameSource(mesh.Name, "joints", ["JOINT"], joints.ToArray());
        var poseSource = ColladaUtils.MakeFloatSource(mesh.Name, "poses", ["TRANSFORM"], poses.ToArray(), 16, "float4x4");
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

        var jointOffsets = new InputLocalOffset
        {
            semantic = "JOINT",
            source = "#" + jointSource.id,
            offset = 0
        };

        var weightOffsets = new InputLocalOffset
        {
            semantic = "WEIGHT",
            source = "#" + weightsSource.id,
            offset = 1
        };

        var vertWeights = new skinVertex_weights
        {
            count = (ulong)vertices.Count,
            input = [jointOffsets, weightOffsets],
            v = string.Join(" ", vertexInfluences.Select(x => x.ToString()).ToArray()),
            vcount = string.Join(" ", vertexInfluenceCounts.Select(x => x.ToString()).ToArray())
        };

        var skin = new skin
        {
            source1 = "#" + geometryId,
            bind_shape_matrix = "1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1",

            joints = new skinJoints
            {
                input = [
                    new InputLocal
                    {
                        semantic = "JOINT",
                        source = "#" + jointSource.id
                    },
                    new InputLocal
                    {
                        semantic = "INV_BIND_MATRIX",
                        source = "#" + poseSource.id
                    }
                ]
            },

            source = [jointSource, poseSource, weightsSource],
            vertex_weights = vertWeights
        };

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

        var knotsSource = ColladaUtils.MakeFloatSource(name, "inputs", ["TIME"], knots.ToArray());
        var outSource = ColladaUtils.MakeFloatSource(name, "outputs", ["TRANSFORM"], outputs.ToArray(), 16, "float4x4");
        var interpSource = ColladaUtils.MakeNameSource(name, "interpolations", ["INTERPOLATION"], interpolations.ToArray());

        var sampler = new sampler
        {
            id = name + "_sampler",
            input = 
            [
                new InputLocal
                {
                    semantic = "INTERPOLATION",
                    source = "#" + interpSource.id
                },
                new InputLocal
                {
                    semantic = "OUTPUT",
                    source = "#" + outSource.id
                },
                new InputLocal
                {
                    semantic = "INPUT",
                    source = "#" + knotsSource.id
                }
            ]
        };

        var channel = new channel
        {
            source = "#" + sampler.id,
            target = target
        };

        var animation = new animation
        {
            id = name,
            name = name,
            Items = [
                knotsSource,
                outSource,
                interpSource,
                sampler,
                channel
            ],
            extra =
            [
                new extra
                {
                    technique =
                    [
                        ExportAnimationLSLibProfile(extData)
                    ]
                }
            ]
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
        var contributor = new assetContributor();
        if (root.ArtToolInfo != null)
            contributor.authoring_tool = root.ArtToolInfo.FromArtToolName;
        else
            contributor.authoring_tool = "LSLib COLLADA Exporter v" + Common.LibraryVersion();

        var asset = new asset
        {
            contributor = [contributor],
            created = DateTime.Now,
            modified = DateTime.Now,
            unit = new assetUnit
            {
                name = "meter"
            },
            up_axis = UpAxisType.Y_UP
        };

        // TODO: Handle up vector, etc. properly?
        if (root.ArtToolInfo != null)
            asset.unit.meter = root.ArtToolInfo.UnitsPerMeter;
        else
            asset.unit.meter = 1;

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
            var clip = new animation_clip
            {
                id = anim.Name + "_Animation",
                name = anim.Name,
                start = 0.0,
                end = anim.Duration,
                endSpecified = true
            };

            var animInstances = new List<InstanceWithExtra>();
            foreach (var animChannel in anims)
            {
                var instance = new InstanceWithExtra
                {
                    url = "#" + animChannel.id
                };
                animInstances.Add(instance);
            }

            clip.instance_animation = animInstances.ToArray();
            animationClips.Add(clip);
        }

        var rootElements = new List<object>();

        if (animations.Count > 0)
        {
            var animationLib = new library_animations
            {
                animation = animations.ToArray()
            };
            rootElements.Add(animationLib);
        }

        if (animationClips.Count > 0)
        {
            var animationClipLib = new library_animation_clips
            {
                animation_clip = animationClips.ToArray()
            };
            rootElements.Add(animationClipLib);
        }

        if (geometries.Count > 0)
        {
            var geometryLib = new library_geometries
            {
                geometry = geometries.ToArray()
            };
            rootElements.Add(geometryLib);
        }

        if (controllers.Count > 0)
        {
            var controllerLib = new library_controllers
            {
                controller = controllers.ToArray()
            };
            rootElements.Add(controllerLib);
        }

        var visualScenes = new library_visual_scenes();
        var visualScene = new visual_scene
        {
            id = "DefaultVisualScene",
            name = "unnamed",
            node = geomNodes.ToArray()
        };
        visualScenes.visual_scene = [visualScene];

        var visualSceneInstance = new InstanceWithExtra
        {
            url = "#DefaultVisualScene"
        };
        rootElements.Add(visualScenes);

        var scene = new COLLADAScene
        {
            instance_visual_scene = visualSceneInstance
        };

        var collada = new COLLADA
        {
            asset = asset,
            scene = scene,
            Items = rootElements.ToArray(),
            extra =
            [
                new extra
                {
                    technique =
                    [
                        ExportRootLSLibProfile(root)
                    ]
                }
            ]
        };

        using var stream = File.Open(outputPath, FileMode.Create);
        collada.Save(stream);
    }
}
