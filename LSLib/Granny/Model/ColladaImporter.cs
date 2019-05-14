using System;
using System.Collections.Generic;
using System.Linq;
using LSLib.Granny.GR2;
using LSLib.LS;
using OpenTK;

namespace LSLib.Granny.Model
{
    internal class ColladaSource
    {
        public String id;
        public Dictionary<String, List<Single>> FloatParams = new Dictionary<string, List<float>>();
        public Dictionary<String, List<Matrix4>> MatrixParams = new Dictionary<string, List<Matrix4>>();
        public Dictionary<String, List<String>> NameParams = new Dictionary<string, List<string>>();

        public static ColladaSource FromCollada(source src)
        {
            var source = new ColladaSource();
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

    public class ColladaImporter
    {
        [Serialization(Kind = SerializationKind.None)]
        public ExporterOptions Options = new ExporterOptions();

        private bool ZUp = false;

        [Serialization(Kind = SerializationKind.None)]
        public Dictionary<string, Mesh> ColladaGeometries;

        [Serialization(Kind = SerializationKind.None)]
        public HashSet<string> SkinnedMeshes;

        private ArtToolInfo ImportArtToolInfo(COLLADA collada)
        {
            ZUp = false;
            var toolInfo = new ArtToolInfo();
            toolInfo.FromArtToolName = "Unknown";
            toolInfo.ArtToolMajorRevision = 1;
            toolInfo.ArtToolMinorRevision = 0;
            toolInfo.ArtToolPointerSize = Options.Is64Bit ? 64 : 32;
            toolInfo.Origin = new float[] { 0, 0, 0 };
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
            var exporterInfo = new ExporterInfo();
            exporterInfo.ExporterName = String.Format("LSLib GR2 Exporter v{0}", Common.LibraryVersion());
            exporterInfo.ExporterMajorRevision = Common.MajorVersion;
            exporterInfo.ExporterMinorRevision = Common.MinorVersion;
            exporterInfo.ExporterBuildNumber = 0;
            exporterInfo.ExporterCustomization = Common.PatchVersion;
            return exporterInfo;
        }

        private void UpdateUserDefinedProperties(Root root)
        {
            if (Options.ModelInfoFormat == DivinityModelInfoFormat.None)
            {
                return;
            }

            var modelFlags = Options.ModelType;
            if (modelFlags == 0)
            {
                modelFlags = DivinityHelpers.DetermineModelFlags(root);
            }

            var userDefinedProperties = "";

            if (root.Meshes != null)
            {
                userDefinedProperties = DivinityHelpers.ModelFlagsToUserDefinedProperties(modelFlags);

                foreach (var mesh in root.Meshes)
                {
                    mesh.ExtendedData = DivinityHelpers.MakeMeshExtendedData(mesh, Options.ModelInfoFormat, Options.ModelType);
                }
            }

            if (root.Skeletons != null)
            {
                foreach (var skeleton in root.Skeletons)
                {
                    if (skeleton.Bones != null)
                    {
                        foreach (var bone in skeleton.Bones)
                        {
                            if (bone.ExtendedData == null)
                            {
                                bone.ExtendedData = new DivinityBoneExtendedData();
                            }
                            
                            bone.ExtendedData.UserDefinedProperties = userDefinedProperties;
                            bone.ExtendedData.IsRigid = (modelFlags.IsRigid()) ? 1 : 0;
                        }
                    }
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

        private technique FindExporterExtraData(extra[] extras)
        {
            if (extras != null)
            {
                foreach (var extra in extras)
                {
                    if (extra.technique != null)
                    {
                        foreach (var technique in extra.technique)
                        {
                            if (technique.profile == "LSTools")
                            {
                                return technique;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private DivinityModelFlag FindDivModelType(mesh mesh)
        {
            DivinityModelFlag flags = 0;
            var technique = FindExporterExtraData(mesh.extra);
            if (technique != null)
            {
                if (technique.Any != null)
                {
                    foreach (var setting in technique.Any)
                    {
                        if (setting.LocalName == "DivModelType")
                        {
                            switch (setting.InnerText.Trim())
                            {
                                // Compatibility flag, not used anymore
                                case "Normal": break;
                                case "Cloth": flags |= DivinityModelFlag.Cloth; break;
                                case "Rigid": flags |= DivinityModelFlag.Rigid; break;
                                case "MeshProxy": flags |= DivinityModelFlag.MeshProxy | DivinityModelFlag.HasProxyGeometry; break;
                                default:
                                    Utils.Warn($"Unrecognized model type in <DivModelType> tag: {setting.Value}");
                                    break;
                            }
                        }
                    }
                }
            }

            return flags;
        }

        private Mesh ImportMesh(geometry geom, mesh mesh, VertexDescriptor vertexFormat)
        {
            var collada = new ColladaMesh();
            bool isSkinned = SkinnedMeshes.Contains(geom.id);
            collada.ImportFromCollada(mesh, vertexFormat, isSkinned, Options);

            var m = new Mesh();
            m.VertexFormat = collada.InternalVertexType;
            m.Name = "Unnamed";

            m.PrimaryVertexData = new VertexData();
            m.PrimaryVertexData.Vertices = collada.ConsolidatedVertices;

            if (!Options.StripMetadata)
            {
                var components = m.VertexFormat.ComponentNames().Select(s => new GrannyString(s)).ToList();
                m.PrimaryVertexData.VertexComponentNames = components;
            }
            else
            {
                m.PrimaryVertexData.VertexComponentNames = null;
            }

            m.PrimaryTopology = new TriTopology();
            m.PrimaryTopology.Indices = collada.ConsolidatedIndices;
            m.PrimaryTopology.Groups = new List<TriTopologyGroup>();
            var triGroup = new TriTopologyGroup();
            triGroup.MaterialIndex = 0;
            triGroup.TriFirst = 0;
            triGroup.TriCount = collada.TriangleCount;
            m.PrimaryTopology.Groups.Add(triGroup);

            m.MaterialBindings = new List<MaterialBinding>();
            m.MaterialBindings.Add(new MaterialBinding());

            // m.BoneBindings; - TODO

            m.OriginalToConsolidatedVertexIndexMap = collada.OriginalToConsolidatedVertexIndexMap;

            var divModelType = FindDivModelType(mesh);
            if (divModelType != 0)
            {
                m.ModelType = divModelType;
            }

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

            Mesh mesh = null;
            if (!ColladaGeometries.TryGetValue(skin.source1.Substring(1), out mesh))
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

                ColladaSource inputSource = null;
                if (!sources.TryGetValue(input.source.Substring(1), out inputSource))
                    throw new ParsingException("Joint input source does not exist: " + input.source);

                if (input.semantic == "JOINT")
                {
                    List<string> jointNames = inputSource.NameParams.Values.SingleOrDefault();
                    if (jointNames == null)
                        throw new ParsingException("Joint input source 'JOINT' must contain array of names.");

                    var skeleton = root.Skeletons.Single();
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

                    ColladaSource inputSource = null;
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

            if (boundBones.Count > 127)
                throw new ParsingException("D:OS supports at most 127 bound bones per mesh.");

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
                float influenceSum = 0.0f;
                for (var i = 0; i < influenceCount; i++)
                {
                    var weightIndex = influences[offset + i * stride + weightInputIndex];
                    influenceSum += weights[weightIndex];
                }

                for (var i = 0; i < influenceCount; i++)
                {
                    var jointIndex = influences[offset + jointInputIndex];
                    var weightIndex = influences[offset + weightInputIndex];
                    var joint = joints[jointIndex];
                    var weight = weights[weightIndex] / influenceSum;
                    // Not all vertices are actually used in triangles, we may have unused verts in the
                    // source list (though this is rare) which won't show up in the consolidated vertex map.
                    if (mesh.OriginalToConsolidatedVertexIndexMap.TryGetValue(vertexIndex, out List<int> consolidatedIndices))
                    {
                        foreach (var consolidatedIndex in consolidatedIndices)
                        {
                            var vertex = mesh.PrimaryVertexData.Vertices[consolidatedIndex];
                            vertex.AddInfluence((byte)boneToIndexMaps[joint], weight);
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
                var bindShapeFloats = skin.bind_shape_matrix.Trim().Split(new char[] { ' ' }).Select(s => Single.Parse(s)).ToArray();
                var bindShapeMat = ColladaHelpers.FloatsToMatrix(bindShapeFloats);
                bindShapeMat.Transpose();

                // Deform geometries that were affected by our bind shape matrix
                mesh.PrimaryVertexData.Transform(bindShapeMat);
            }
        }

        public void ImportAnimations(IEnumerable<animation> anims, Root root, Skeleton skeleton)
        {
            var trackGroup = new TrackGroup
            {
                Name = (skeleton != null) ? skeleton.Name : "Dummy_Root",
                TransformTracks = new List<TransformTrack>(),
                InitialPlacement = new Transform(),
                AccumulationFlags = 2,
                LoopTranslation = new float[] { 0, 0, 0 }
            };

            var animation = new Animation
            {
                Name = "Default",
                TimeStep = 0.016667f, // 60 FPS
                Oversampling = 1,
                DefaultLoopCount = 1,
                Flags = 1,
                Duration = .0f,
                TrackGroups = new List<TrackGroup> { trackGroup }
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
                ColladaAnimation importAnim = new ColladaAnimation();
                if (importAnim.ImportFromCollada(colladaAnim, skeleton))
                {
                    duration = Math.Max(duration, importAnim.Duration);
                    var track = importAnim.MakeTrack();
                    trackGroup.TransformTracks.Add(track);
                }
            }

            animation.Duration = Math.Max(animation.Duration, duration);
        }

        public Root Import(string inputPath)
        {
            var collada = COLLADA.Load(inputPath);
            var root = new Root();
            root.ArtToolInfo = ImportArtToolInfo(collada);
            if (!Options.StripMetadata)
            {
                root.ExporterInfo = ImportExporterInfo(collada);
            }

            root.FromFileName = inputPath;

            root.Skeletons = new List<Skeleton>();
            root.VertexDatas = new List<VertexData>();
            root.TriTopologies = new List<TriTopology>();
            root.Meshes = new List<Mesh>();
            root.Models = new List<Model>();
            root.TrackGroups = new List<TrackGroup>();
            root.Animations = new List<Animation>();

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
                            if (scene.node != null)
                            {
                                foreach (var node in scene.node)
                                {
                                    FindRootBones(null, node, rootBones);
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
                    Utils.Warn(String.Format("Library {0} is unsupported and will be ignored", item.GetType().Name));
                }
            }

            foreach (var bone in rootBones)
            {
                var skeleton = Skeleton.FromCollada(bone);
                root.Skeletons.Add(skeleton);
            }

            foreach (var geometry in collGeometries)
            {
                VertexDescriptor vertexFormat = null;
                // Use the override vertex format, if one was specified
                Options.VertexFormats.TryGetValue(geometry.name, out vertexFormat);
                var mesh = ImportMesh(root, geometry.name, geometry, geometry.Item as mesh, vertexFormat);
                ColladaGeometries.Add(geometry.id, mesh);
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

            this.UpdateUserDefinedProperties(root);

            return root;
        }
    }
}
