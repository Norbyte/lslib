using LSLib.Granny.GR2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
        public ExportFormat InputFormat;
        public string OutputPath;
        public ExportFormat OutputFormat;

        public bool Is64Bit = false;
        public bool AlternateSignature = false;
        public UInt32 VersionTag = Header.DefaultTag;
        public bool ExportNormals = true; // TODO: UNHANDLED
        public bool ExportTangents = true; // TODO: UNHANDLED
        public bool ExportUVs = true; // TODO: UNHANDLED
        public bool RecalculateNormals = false; // TODO: UNHANDLED
        public bool RecalculateTangents = false; // TODO: UNHANDLED
        public bool RecalculateIWT = false;
        public bool BuildDummySkeleton = false;
        public bool CompactIndices = false;
        public bool DeduplicateVertices = true; // TODO: Add Collada conforming vert. handling as well
        public bool DeduplicateUVs = true; // TODO: UNHANDLED
        public bool ApplyBasisTransforms = true;
        public bool UseObsoleteVersionTag = false;
        public string ConformSkeletonsPath;
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
            var root = new LSLib.Granny.Model.Root();
            root.VertexFormats = Options.VertexFormats;
            root.ImportFromCollada(inPath);
            return root;
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

        private void SaveDAE(string outPath, Root root)
        {
            root.ExportToCollada(outPath);
        }

        private void Save(string outPath, ExportFormat format, Root root)
        {
            switch (format)
            {
                case ExportFormat.GR2:
                    SaveGR2(outPath, root);
                    break;

                case ExportFormat.DAE:
                    SaveDAE(outPath, root);
                    break;

                default:
                    throw new NotImplementedException("Unsupported output format");
            }
        }

        private void GenerateDummySkeleton(Root root)
        {
            // TODO: Add an option to enable/disable dummy skeleton generation
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
        }

        private void ConformSkeletons(List<Skeleton> skeletons)
        {
            foreach (var skeleton in Root.Skeletons)
            {
                Skeleton conformingSkel = null;
                foreach (var skel in skeletons)
                {
                    if (skel.Name == skeleton.Name)
                    {
                        conformingSkel = skel;
                        break;
                    }
                }

                if (conformingSkel == null)
                {
                    string msg = String.Format("No matching skeleton found in source file for skeleton '{0}'.", skeleton.Name);
                    throw new ExportException(msg);
                }

                ConformSkeleton(skeleton, conformingSkel);
            }
        }

        private void ConformSkeletons(string inPath)
        {
            // We don't have any skeletons in this mesh, nothing to conform.
            if (Root.Skeletons == null || Root.Skeletons.Count == 0)
            {
                return;
            }

            var skelRoot = LoadGR2(inPath);
            if (skelRoot.Skeletons == null || skelRoot.Skeletons.Count == 0)
            {
                throw new ExportException("Source file contains no skeletons.");
            }

            ConformSkeletons(skelRoot.Skeletons);
        }

        public void Export()
        {
            Root = Load(Options.InputPath, Options.InputFormat);

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

            if (Options.ConformSkeletonsPath != null)
            {
                try
                {
                    ConformSkeletons(Options.ConformSkeletonsPath);
                }
                catch (ExportException e)
                {
                    throw new ExportException("Failed to conform skeleton:\n" + e.Message + "\nCheck bone counts and ordering.");
                }
            }

            if (Options.BuildDummySkeleton)
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

            Save(Options.OutputPath, Options.OutputFormat, Root);
        }
    }
}
