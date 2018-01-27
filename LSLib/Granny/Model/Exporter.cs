using LSLib.Granny.GR2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LSLib.LS;
using OpenTK;

namespace LSLib.Granny.Model
{
    public class ExportException : Exception
    {
        public ExportException(string message)
            : base(message)
        { }
    }

    public enum ExportFormat
    {
        GR2,
        DAE
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
        // Export vertex normals to DAE/GR2 file
        public bool ExportNormals = true;
        // Export tangents/binormals to DAE/GR2 file
        public bool ExportTangents = true;
        // Export UV-s to DAE/GR2 file
        public bool ExportUVs = true;
        // Export vertex colors to DAE/GR2 file
        public bool ExportColors = true;
        // Flip the V coord of UV-s (GR2 stores them in flipped format)
        public bool FlipUVs = true;
        // Recalculate normals, even if they're available in the source mesh
        // (They'll be recalculated automatically if unavailable)
        public bool RecalculateNormals = false;
        // Recalculate tangents/binormals, even if they're available in the source mesh
        // (They'll be recalculated automatically if unavailable)
        public bool RecalculateTangents = false;
        // Recalculate bone inverse world transforms
        public bool RecalculateIWT = false;
        // Create a dummy skeleton if none exists in the mesh
        // Some games will crash if they encounter a mesh without a skeleton
        public bool BuildDummySkeleton = false;
        // Save 16-bit vertex indices, if possible
        public bool CompactIndices = false;
        public bool DeduplicateVertices = true; // TODO: Add Collada conforming vert. handling as well
        public bool DeduplicateUVs = true; // TODO: UNHANDLED
        public bool ApplyBasisTransforms = true;
        // Use an obsolete version tag to prevent Granny from memory mapping the structs
        public bool UseObsoleteVersionTag = false;
        public string ConformGR2Path;
        public bool ConformSkeletons = true;
        public bool ConformAnimations = true;
        public bool ConformMeshBoneBindings = true;
        public bool ConformModels = true;
        public Dictionary<string, string> VertexFormats = new Dictionary<string,string>();
    }


    public class Exporter
    {
        public ExporterOptions Options = new ExporterOptions();
        private Root Root;

        private Root LoadGR2(string inPath)
        {
            var root = new LSLib.Granny.Model.Root();
            FileStream fs = new FileStream(inPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
            var gr2 = new LSLib.Granny.GR2.GR2Reader(fs);
            gr2.Read(root);
            root.PostLoad();
            fs.Close();
            fs.Dispose();
            return root;
        }

        private Root LoadDAE(string inPath)
        {
            var importer = new ColladaImporter();
            importer.Options = Options;
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

                default:
                    throw new NotImplementedException("Unsupported input format");
            }
        }

        private void SaveGR2(string outPath, Root root)
        {
            root.PreSave();
            var writer = new LSLib.Granny.GR2.GR2Writer();

            writer.Format = Options.Is64Bit ? Magic.Format.LittleEndian64 : Magic.Format.LittleEndian32;
            writer.AlternateMagic = Options.AlternateSignature;
            writer.VersionTag = Options.VersionTag;


            if (Options.UseObsoleteVersionTag)
            {
                // Use an obsolete version tag to prevent Granny from memory mapping the structs
                writer.VersionTag -= 1;
            }

            var body = writer.Write(root);
            writer.Dispose();

            FileStream f = new FileStream(outPath, FileMode.Create, System.IO.FileAccess.Write, FileShare.None);
            f.Write(body, 0, body.Length);
            f.Close();
            f.Dispose();
        }

        private void SaveDAE(Root root, ExporterOptions options)
        {
            var exporter = new ColladaExporter();
            exporter.Options = options;
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
                    Utils.Info(String.Format("Generating dummy skeleton for model '{0}'", model.Name));
                    var skeleton = new Skeleton();
                    skeleton.Name = model.Name;
                    skeleton.LODType = 0;
                    skeleton.IsDummy = true;
                    root.Skeletons.Add(skeleton);

                    var bone = new Bone();
                    bone.Name = model.Name;
                    bone.ParentIndex = -1;
                    skeleton.Bones = new List<Bone> { bone };
                    bone.Transform = new Transform();

                    // TODO: Transform / IWT is not always identity on dummy bones!
                    skeleton.UpdateInverseWorldTransforms();
                    model.Skeleton = skeleton;

                    foreach (var mesh in model.MeshBindings)
                    {
                        if (mesh.Mesh.BoneBindings != null && mesh.Mesh.BoneBindings.Count > 0)
                        {
                            throw new ParsingException("Failed to generate dummy skeleton: Mesh already has bone bindings.");
                        }

                        var binding = new BoneBinding();
                        binding.BoneName = bone.Name;
                        // TODO: Calculate bounding box!
                        binding.OBBMin = new float[] { -10, -10, -10 };
                        binding.OBBMax = new float[] { 10, 10, 10 };
                        mesh.Mesh.BoneBindings = new List<BoneBinding> { binding };
                    }
                }
            }
        }

        private void ConformAnimationBindPoses(Skeleton skeleton, Skeleton conformToSkeleton)
        {
            foreach (var trackGroup in Root.TrackGroups)
            {
                for (var i = 0; i < trackGroup.TransformTracks.Count; i++)
                {
                    var track = trackGroup.TransformTracks[i];
                    var bone = skeleton.GetBoneByName(track.Name);
                    if (bone == null)
                    {
                        string msg = String.Format("Animation track references bone '{0}' that cannot be found in the skeleton.", track.Name);
                        throw new ExportException(msg);
                    }

                    var conformingBone = conformToSkeleton.GetBoneByName(track.Name);
                    if (conformingBone == null)
                    {
                        string msg = String.Format("Animation track references bone '{0}' that cannot be found in the conforming skeleton.", track.Name);
                        throw new ExportException(msg);
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
                    string msg = String.Format(
                        "No matching bone found for conforming bone '{1}' in skeleton '{0}'.",
                        skeleton.Name, conformBone.Name
                    );
                    throw new ExportException(msg);
                }

                // Bones must have the same parent. We check this in two steps:
                // 1) Either both of them are root bones (no parent index) or none of them are.
                if ((conformBone.ParentIndex == -1) != (inputBone.ParentIndex == -1))
                {
                    string msg = String.Format(
                        "Cannot map non-root bones to root bone '{1}' for skeleton '{0}'.",
                        skeleton.Name, conformBone.Name
                    );
                    throw new ExportException(msg);
                }

                // 2) The name of their parent bones is the same (index may differ!)
                if (conformBone.ParentIndex != -1)
                {
                    var conformParent = conformToSkeleton.Bones[conformBone.ParentIndex];
                    var inputParent = skeleton.Bones[inputBone.ParentIndex];
                    if (conformParent.Name != inputParent.Name)
                    {
                        string msg = String.Format(
                            "Conforming parent ({1}) for bone '{2}' differs from input parent ({3}) for skeleton '{0}'.",
                            skeleton.Name, conformParent.Name, conformBone.Name, inputParent.Name
                        );
                        throw new ExportException(msg);
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
            foreach (var trackGroup in Root.TrackGroups)
            {
                foreach (var track in trackGroup.TransformTracks)
                {
                    var bone = skeleton.GetBoneByName(track.Name);
                    if (bone == null)
                    {
                        string msg = String.Format("Animation track references bone '{0}' that cannot be found in the skeleton.", track.Name);
                        throw new ExportException(msg);
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
                        string msg = String.Format("Skeleton source file should contain exactly one skeleton.");
                        throw new ExportException(msg);
                    }

                    var skeleton = Root.Skeletons.First();

                    // Generate a dummy model if there isn't one, otherwise we won't
                    // be able to bind the animations to anything
                    if (Root.Models == null)
                    {
                        Root.Models = new List<Model>();
                        var model = new Model();
                        model.InitialPlacement = new Transform();
                        model.Name = skeleton.Name;
                        model.Skeleton = skeleton;
                        Root.Models.Add(model);
                    }

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
                if (conformingSkel == null && skeletons.Count() == 1 && Root.Skeletons.Count() == 1)
                {
                    conformingSkel = skeletons.First();
                }

                if (conformingSkel == null)
                {
                    string msg = String.Format("No matching skeleton found in source file for skeleton '{0}'.", skeleton.Name);
                    throw new ExportException(msg);
                }

                ConformSkeleton(skeleton, conformingSkel);
            }
        }

        private void ConformMeshBoneBindings(Mesh mesh, Mesh conformToMesh)
        {
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
                    inputBone = new BoneBinding();
                    inputBone.BoneName = conformBone.BoneName;
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
                    string msg = String.Format("No matching mesh found in source file for mesh '{0}'.", mesh.Name);
                    throw new ExportException(msg);
                }

                ConformMeshBoneBindings(mesh, conformingMesh);
            }
        }

        private Mesh GenerateDummyMesh(MeshBinding meshBinding)
        {
            var vertexData = new VertexData();
            vertexData.VertexComponentNames = meshBinding.Mesh.PrimaryVertexData.VertexComponentNames
                .Select(name => new GrannyString(name.String)).ToList();
            vertexData.Vertices = new List<Vertex>();
            var dummyVertex = Helpers.CreateInstance(meshBinding.Mesh.VertexFormat) as Vertex;
            vertexData.Vertices.Add(dummyVertex);
            Root.VertexDatas.Add(vertexData);

            var topology = new TriTopology();
            topology.Groups = new List<TriTopologyGroup>();
            var group = new TriTopologyGroup();
            group.MaterialIndex = 0;
            group.TriCount = 0;
            group.TriFirst = 0;
            topology.Groups.Add(group);

            topology.Indices = new List<int>();
            Root.TriTopologies.Add(topology);

            var mesh = new Mesh();
            mesh.Name = meshBinding.Mesh.Name;
            mesh.VertexFormat = meshBinding.Mesh.VertexFormat;
            mesh.PrimaryTopology = topology;
            mesh.PrimaryVertexData = vertexData;
            if (meshBinding.Mesh.BoneBindings != null)
            {
                mesh.BoneBindings = new List<BoneBinding>();
                ConformMeshBoneBindings(mesh, meshBinding.Mesh);
            }

            return mesh;
        }

        private Model MakeDummyModel(Model original)
        {
            var newModel = new Model();
            newModel.InitialPlacement = original.InitialPlacement;
            newModel.Name = original.Name;

            if (original.Skeleton != null)
            {
                var skeleton = Root.Skeletons.Where(skel => skel.Name == original.Skeleton.Name).FirstOrDefault();
                if (skeleton == null)
                {
                    string msg = String.Format("Model '{0}' references skeleton '{0}' that does not exist in the source file.", original.Name, original.Skeleton.Name);
                    throw new ExportException(msg);
                }

                newModel.Skeleton = skeleton;
            }

            if (original.MeshBindings != null)
            {
                newModel.MeshBindings = new List<MeshBinding>();
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

                    var binding = new MeshBinding();
                    binding.Mesh = mesh;
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
            Root.Models = new List<Model>();

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
            }

            // If the new GR2 contains models that are not in the original GR2,
            // append them to the end of the model list
            Root.Models.AddRange(originalModels.Where(m => !Root.Models.Contains(m)));
        }

        private void Conform(string inPath)
        {
            var conformRoot = LoadGR2(inPath);

            if (Options.ConformSkeletons)
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

            if (Options.OutputFormat == ExportFormat.GR2)
            {
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
            }

            if (Options.ApplyBasisTransforms)
            {
                Root.ConvertToYUp();
            }

            if (Options.RecalculateIWT && Root.Skeletons != null)
            {
                foreach (var skeleton in Root.Skeletons)
                {
                    skeleton.UpdateInverseWorldTransforms();
                }
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
                                if (index > 0x7fff)
                                {
                                    hasHighIndex = true;
                                    break;
                                }
                            }

                            if (!hasHighIndex)
                            {
                                topology.Indices16 = new List<short>(topology.Indices.Count);
                                foreach (var index in topology.Indices)
                                {
                                    topology.Indices16.Add((short)index);
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
}
