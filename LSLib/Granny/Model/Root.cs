using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Collada141;
using LSLib.Granny.GR2;
using OpenTK;

namespace LSLib.Granny.Model
{
    internal class Source
    {
        public String id;
        public Dictionary<String, List<Single>> FloatParams = new Dictionary<string, List<float>>();
        public Dictionary<String, List<Matrix4>> MatrixParams = new Dictionary<string, List<Matrix4>>();
        public Dictionary<String, List<String>> NameParams = new Dictionary<string, List<string>>();

        public static Source FromCollada(source src)
        {
            var source = new Source();
            source.id = src.id;

            var accessor = src.technique_common.accessor;
            // TODO: check src.#ID?

            float_array floats = null;
            Name_array names = null;
            if (src.Item is float_array)
            {
                floats = src.Item as float_array;
                // Workaround for empty arrays being null
                if (floats.Values == null)
                    floats.Values = new double[] { };

                if ((int)floats.count != floats.Values.Length || floats.count < accessor.stride * accessor.count + accessor.offset)
                    throw new ParsingException("Float source data size mismatch. Check source and accessor item counts.");
            }
            else if (src.Item is Name_array)
            {
                names = src.Item as Name_array;
                // Workaround for empty arrays being null
                if (names.Values == null)
                    names.Values = new string[] { };

                if ((int)names.count != names.Values.Length || names.count < accessor.stride * accessor.count + accessor.offset)
                    throw new ParsingException("Name source data size mismatch. Check source and accessor item counts.");
            }
            else
                throw new ParsingException("Unsupported source data format.");

            var paramOffset = 0;
            foreach (var param in accessor.param)
            {
                if (param.name == null)
                    param.name = "default";
                if (param.type == "float")
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

    public class Root
    {
        public ArtToolInfo ArtToolInfo;
        public ExporterInfo ExporterInfo;
        public string FromFileName;
        [Serialization(Type = MemberType.ArrayOfReferences)]
        public List<Texture> Textures;
        [Serialization(Type = MemberType.ArrayOfReferences)]
        public List<Material> Materials;
        [Serialization(Type = MemberType.ArrayOfReferences)]
        public List<Skeleton> Skeletons;
        [Serialization(Type = MemberType.ArrayOfReferences)]
        public List<VertexData> VertexDatas;
        [Serialization(Type = MemberType.ArrayOfReferences)]
        public List<TriTopology> TriTopologies;
        [Serialization(Type = MemberType.ArrayOfReferences)]
        public List<Mesh> Meshes;
        [Serialization(Type = MemberType.ArrayOfReferences)]
        public List<Model> Models;
        [Serialization(Type = MemberType.ArrayOfReferences)]
        public List<TrackGroup> TrackGroups;
        [Serialization(Type = MemberType.ArrayOfReferences)]
        public List<Animation> Animations;
        [Serialization(Type = MemberType.VariantReference)]
        public object ExtendedData;

        [Serialization(Kind = SerializationKind.None)]
        public Dictionary<string, Mesh> ColladaGeometries;
        [Serialization(Kind = SerializationKind.None)]
        public HashSet<string> SkinnedMeshes;
        [Serialization(Kind = SerializationKind.None)]
        public Matrix4 BindShapeMatrix;

        [Serialization(Kind = SerializationKind.None)]
        bool ZUp = false;

        private void ImportArtToolInfo(COLLADA collada)
        {
            ArtToolInfo = new ArtToolInfo();
            ArtToolInfo.FromArtToolName = "Unknown";
            ArtToolInfo.ArtToolMajorRevision = 0;
            ArtToolInfo.ArtToolMinorRevision = 1;
            ArtToolInfo.ArtToolPointerSize = 32;
            ArtToolInfo.Origin = new float[] { 0, 0, 0 };
            ArtToolInfo.SetYUp();

            if (collada.asset != null)
            {
                if (collada.asset.unit.name == "meter")
                    ArtToolInfo.UnitsPerMeter = (float)collada.asset.unit.meter;
                else if (collada.asset.unit.name == "centimeter")
                    ArtToolInfo.UnitsPerMeter = (float)collada.asset.unit.meter * 100;
                else
                    throw new NotImplementedException("Unsupported asset unit type: " + collada.asset.unit.name);


                if (collada.asset.contributor != null && collada.asset.contributor.Length > 0)
                {
                    var contributor = collada.asset.contributor.First();
                    if (contributor.authoring_tool != null)
                        ArtToolInfo.FromArtToolName = contributor.authoring_tool;
                }

                if (collada.asset.up_axis != null)
                {
                    switch (collada.asset.up_axis)
                    {
                        case UpAxisType.X_UP:
                            throw new Exception("X-up not supported yet!");

                        case UpAxisType.Y_UP:
                            ArtToolInfo.SetYUp();
                            break;

                        case UpAxisType.Z_UP:
                            ZUp = true;
                            ArtToolInfo.SetZUp();
                            break;
                    }
                }
            }
        }

        private void ImportExporterInfo(COLLADA collada)
        {
            ExporterInfo = new ExporterInfo();
            ExporterInfo.ExporterName = "LSLib GR2 Exporter";
            ExporterInfo.ExporterMajorRevision = 0;
            ExporterInfo.ExporterMinorRevision = 1;
            ExporterInfo.ExporterBuildNumber = 0;
            ExporterInfo.ExporterCustomization = 0;
        }

        private Mesh ImportMesh(string name, mesh mesh, bool isSkinned)
        {
            var m = Mesh.ImportFromCollada(mesh, isSkinned);
            m.Name = name;
            VertexDatas.Add(m.PrimaryVertexData);
            TriTopologies.Add(m.PrimaryTopology);
            Meshes.Add(m);
            return m;
        }

        private void ImportSkin(skin skin)
        {
            if (skin.source1[0] != '#')
                throw new ParsingException("Only ID references are supported for skin geometries");

            Mesh mesh = null;
            if (!ColladaGeometries.TryGetValue(skin.source1.Substring(1), out mesh))
                throw new ParsingException("Skin references nonexistent mesh: " + skin.source1);

            var sources = new Dictionary<String, Source>();
            foreach (var source in skin.source)
            {
                var src = Source.FromCollada(source);
                sources.Add(src.id, src);
            }

            List<Bone> joints = null;
            List<Matrix4> invBindMatrices = null;
            foreach (var input in skin.joints.input)
            {
                if (input.source[0] != '#')
                    throw new ParsingException("Only ID references are supported for joint input sources");

                Source inputSource = null;
                if (!sources.TryGetValue(input.source.Substring(1), out inputSource))
                    throw new ParsingException("Joint input source does not exist: " + input.source);

                if (input.semantic == "JOINT")
                {
                    List<string> jointNames = inputSource.NameParams.Values.SingleOrDefault();
                    if (jointNames == null)
                        throw new ParsingException("Joint input source 'JOINT' must contain array of names.");

                    var skeleton = Skeletons.Single();
                    joints = new List<Bone>();
                    foreach (var name in jointNames)
                    {
                        Bone bone = null;
                        var lookupName = name.Replace("_x0020_", " ");
                        if (!skeleton.BonesBySID.TryGetValue(lookupName, out bone))
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
                if (count > 4)
                    throw new ParsingException("GR2 only supports at most 4 vertex influences");
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

                    Source inputSource = null;
                    if (!sources.TryGetValue(input.source.Substring(1), out inputSource))
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
                if (!boundBones.Contains(joint))
                    boundBones.Add(joint);

                offset += stride;
            }

            if (boundBones.Count > 255)
                throw new ParsingException("GR2 supports at most 255 bound bones per mesh.");

            mesh.BoneBindings = new List<BoneBinding>();
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
                    joints[i].InverseWorldTransform = new float[] {
                        iwt[0, 0], iwt[1, 0], iwt[2, 0], iwt[3, 0], 
                        iwt[0, 1], iwt[1, 1], iwt[2, 1], iwt[3, 1], 
                        iwt[0, 2], iwt[1, 2], iwt[2, 2], iwt[3, 2], 
                        iwt[0, 3], iwt[1, 3], iwt[2, 3], iwt[3, 3]
                    };

                    // Bind all bones that affect vertices to the mesh, so we can reference them
                    // later from the vertexes BoneIndices.
                    var binding = new BoneBinding();
                    binding.BoneName = joints[i].Name;
                    // TODO
                    binding.OBBMin = new float[] { -10, -10, -10 };
                    binding.OBBMax = new float[] { 10, 10, 10 };
                    mesh.BoneBindings.Add(binding);
                    boneToIndexMaps.Add(joints[i], boneToIndexMaps.Count);
                }
            }

            offset = 0;
            for (var vertexIndex = 0; vertexIndex < influenceCounts.Count; vertexIndex++)
            {
                var influenceCount = influenceCounts[vertexIndex];
                for (var i = 0; i < influenceCount; i++)
                {
                    var jointIndex = influences[offset + jointInputIndex];
                    var weightIndex = influences[offset + weightInputIndex];
                    var joint = joints[jointIndex];
                    var weight = weights[weightIndex];
                    foreach (var consolidatedIndex in mesh.OriginalToConsolidatedVertexIndexMap[vertexIndex])
                    {
                        var vertex = mesh.PrimaryVertexData.Vertices[consolidatedIndex];
                        vertex.AddInfluence((byte)boneToIndexMaps[joint], weight);
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
                var bindShapeFloats = skin.bind_shape_matrix.Split(new char[] { ' ' }).Select(s => Single.Parse(s)).ToArray();
                var mat = ColladaHelpers.FloatsToMatrix(bindShapeFloats);
                mat.Transpose();

                // if (mat != Matrix4.Identity)
                //     throw new Exception("Non-Identity bind shape matrices are not supported yet!");

                BindShapeMatrix = mat;

                // Deform geometries that were affected by our bind shape matrix (might not be correct!)
                foreach (var vertex in mesh.PrimaryVertexData.Vertices)
                {
                    vertex.Position = Vector3.Transform(vertex.Position, BindShapeMatrix);
                }
            }
            else
            {
                BindShapeMatrix = Matrix4.Identity;
            }
        }

        public void ImportAnimations(IEnumerable<animation> anims)
        {
            var animation = new Animation();
            animation.Name = "Default";
            animation.TimeStep = 0.016667f; // 60 FPS
            animation.Oversampling = 1;
            animation.DefaultLoopCount = 1;
            animation.Flags = 1;

            var trackGroup = new TrackGroup();
            trackGroup.Name = Skeletons[0].Name;
            trackGroup.TransformTracks = new List<TransformTrack>();
            trackGroup.InitialPlacement = new Transform();
            trackGroup.AccumulationFlags = 2;
            trackGroup.LoopTranslation = new float[] { 0, 0, 0 };
            foreach (var colladaTrack in anims)
            {
                ImportAnimation(colladaTrack, trackGroup);
            }

            if (trackGroup.TransformTracks.Count > 0)
            {
                animation.Duration = trackGroup.TransformTracks.Max(t => t.OrientationCurve.CurveData.Duration());
                animation.TrackGroups = new List<TrackGroup> { trackGroup };

                TrackGroups.Add(trackGroup);
                Animations.Add(animation);
            }
        }

        public void ImportAnimation(animation anim, TrackGroup trackGroup)
        {
            var childAnims = 0;
            foreach (var item in anim.Items)
            {
                if (item is animation)
                {
                    ImportAnimation(item as animation, trackGroup);
                    childAnims++;
                }
            }

            if (childAnims < anim.Items.Length)
            {
                ColladaAnimation collada = new ColladaAnimation();
                if (collada.ImportFromCollada(anim, Skeletons[0]))
                {
                    var track = collada.MakeTrack();
                    trackGroup.TransformTracks.Add(track);
                }
            }
        }

        private void FindRootBones(node parent, node node, List<node> rootBones)
        {
            if (node.type == NodeType.JOINT)
            {
                if (parent != null)
                {
                    Utils.Warn(String.Format("Joint {0} is not a top level node; parent transformations will be ignored!", node.name != null ? node.name : "(UNNAMED)"));
                }

                rootBones.Add(node);
            }
            else if (node.type == NodeType.NODE)
            {
                if (node.node1 != null)
                {
                    foreach (var child in node.node1)
                    {
                        FindRootBones(node, child, rootBones);
                    }
                }
            }
        }

        public void Transform(Matrix4 transformation)
        {
            if (VertexDatas != null)
            {
                foreach (var vertexData in VertexDatas)
                {
                    vertexData.Transform(transformation);
                }
            }
        }

        public void ConvertToYUp()
        {
            if (!ZUp) return;

            var transform = Matrix4.CreateRotationX((float)(-0.5 * Math.PI));
            Transform(transform);

            if (ArtToolInfo != null)
            {
                ArtToolInfo.SetYUp();
            }

            ZUp = false;
        }

        public void PostLoad()
        {
            foreach (var skeleton in Skeletons)
            {
                var hasSkinnedMeshes = Models.Any((model) => model.Skeleton == skeleton);
                if (!hasSkinnedMeshes || skeleton.Bones.Count == 1)
                {
                    skeleton.IsDummy = true;
                    Utils.Info(String.Format("Skeleton '{0}' marked as dummy", skeleton.Name));
                }
            }
        }

        public void PreSave()
        {
        }

        public void ImportFromCollada(string inputPath)
        {
            var collada = COLLADA.Load(inputPath);
            ImportArtToolInfo(collada);
            ImportExporterInfo(collada);
            FromFileName = inputPath;

            Skeletons = new List<Skeleton>();
            VertexDatas = new List<VertexData>();
            TriTopologies = new List<TriTopology>();
            Meshes = new List<Mesh>();
            Models = new List<Model>();
            TrackGroups = new List<TrackGroup>();
            Animations = new List<Animation>();

            ColladaGeometries = new Dictionary<string, Mesh>();
            SkinnedMeshes = new HashSet<string>();

            var collGeometries = new List<geometry>();
            var collSkins = new List<skin>();
            var collAnimations = new List<animation>();
            var rootBones = new List<node>();

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
                                SkinnedMeshes.Add((controller.Item as skin).source1.Substring(1));
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
                            foreach (var node in scene.node)
                            {
                                FindRootBones(null, node, rootBones);
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
                    Utils.Warn(String.Format("Library {0} is unsupported and will be ignored", item.GetType().Name));
                }
            }

            foreach (var bone in rootBones)
            {
                var skeleton = Skeleton.FromCollada(bone);
                Skeletons.Add(skeleton);
            }

            foreach (var geometry in collGeometries)
            {
                bool isSkinned = SkinnedMeshes.Contains(geometry.id);
                var mesh = ImportMesh(geometry.name, geometry.Item as mesh, isSkinned);
                ColladaGeometries.Add(geometry.id, mesh);
            }

            // Import skinning controllers after skeleton and geometry loading has finished, as
            // we reference both of them during skin import
            foreach (var skin in collSkins)
            {
                ImportSkin(skin);
            }

            if (collAnimations.Count > 0)
            {
                ImportAnimations(collAnimations);
            }

            var rootModel = new Model();
            rootModel.Name = "Unnamed"; // TODO
            if (Skeletons.Count > 0)
            {
                rootModel.Skeleton = Skeletons[0];
                rootModel.Name = rootModel.Skeleton.Bones[0].Name;
            }
            rootModel.InitialPlacement = new Transform();
            rootModel.MeshBindings = new List<MeshBinding>();
            foreach (var mesh in Meshes)
            {
                var binding = new MeshBinding();
                binding.Mesh = mesh;
                rootModel.MeshBindings.Add(binding);
            }

            Models.Add(rootModel);
            // TODO: make this an option!
            if (Skeletons.Count > 0)
                Skeletons[0].UpdateInverseWorldTransforms();
            PostLoad();
        }

        public void ExportToCollada(string outputPath)
        {
            var collada = new COLLADA();
            var asset = new asset();
            var contributor = new assetContributor();
            if (ArtToolInfo != null)
                contributor.authoring_tool = ArtToolInfo.FromArtToolName;
            else
                contributor.authoring_tool = "LSLib COLLADA Exporter";
            asset.contributor = new assetContributor[] { contributor };
            asset.created = DateTime.Now;
            asset.modified = DateTime.Now;
            asset.unit = new assetUnit();
            asset.unit.name = "meter";
            // TODO: Handle up vector, etc. properly?
            if (ArtToolInfo != null)
                asset.unit.meter = ArtToolInfo.UnitsPerMeter;
            else
                asset.unit.meter = 1;
            asset.up_axis = UpAxisType.Y_UP;
            collada.asset = asset;

            var geometries = new List<geometry>();
            var controllers = new List<controller>();
            var geomNodes = new List<node>();

            foreach (var model in Models)
            {
                string skelRef = null;
                if (model.Skeleton != null && !model.Skeleton.IsDummy && model.Skeleton.Bones.Count > 1)
                {
                    var skeleton = model.MakeSkeleton();
                    geomNodes.Add(skeleton);
                    skelRef = skeleton.id;
                }

                if (model.MeshBindings != null)
                {
                    foreach (var meshBinding in model.MeshBindings)
                    {
                        var mesh = meshBinding.Mesh.ExportToCollada();
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

                            skin = meshBinding.Mesh.ExportSkin(model.Skeleton.Bones, boneNames, geom.id);
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
                }
            }

            var animations = new List<animation>();
            var animationClips = new List<animation_clip>();
            if (Animations != null)
            {
                foreach (var anim in Animations)
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
