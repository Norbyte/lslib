using System;
using System.Collections.Generic;
using System.Linq;
using LSLib.Granny.GR2;
using OpenTK;

namespace LSLib.Granny.Model
{
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
        public bool ZUp = false;
        [Serialization(Kind = SerializationKind.None)]
        public UInt32 GR2Tag;


        public void TransformVertices(Matrix4 transformation)
        {
            if (VertexDatas != null)
            {
                foreach (var vertexData in VertexDatas)
                {
                    vertexData.Transform(transformation);
                }
            }
        }

        public void TransformSkeletons(Matrix4 transformation)
        {
            if (Skeletons != null)
            {
                foreach (var skeleton in Skeletons)
                {
                    skeleton.TransformRoots(transformation);
                }
            }
        }

        public void ConvertToYUp(bool transformSkeletons)
        {
            if (!ZUp) return;

            var transform = Matrix4.CreateRotationX((float)(-0.5 * Math.PI));
            TransformVertices(transform);
            if (transformSkeletons)
            {
                TransformSkeletons(transform);
            }

            if (ArtToolInfo != null)
            {
                ArtToolInfo.SetYUp();
            }

            ZUp = false;
        }

        public void Flip(bool flipMesh, bool flipSkeleton)
        {
            if (flipMesh && VertexDatas != null)
            {
                foreach (var vertexData in VertexDatas)
                {
                    vertexData.Flip();
                }
            }

            if (flipSkeleton && Skeletons != null)
            {
                foreach (var skeleton in Skeletons)
                {
                    skeleton.Flip();
                }
            }

            if (flipMesh && TriTopologies != null)
            {
                foreach (var topology in TriTopologies)
                {
                    topology.ChangeWindingOrder();
                }
            }
        }

        public void PostLoad(UInt32 tag)
        {
            GR2Tag = tag;

            if (tag == Header.Tag_DOS2DE)
            {
                Flip(true, true);
            }

            if (VertexDatas != null)
            {
                foreach (var vertexData in VertexDatas)
                {
                    vertexData.PostLoad();
                }
            }

            if (TriTopologies != null)
            {
                foreach (var triTopology in TriTopologies)
                {
                    triTopology.PostLoad();
                }
            }

            if (Meshes != null)
            {
                Meshes.ForEach(m => m.PostLoad());
            }

            if (Skeletons != null)
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

            // Upgrade legacy animation formats
            if (TrackGroups != null)
            {
                foreach (var group in TrackGroups)
                {
                    if (group.TransformTracks != null)
                    {
                        foreach (var track in group.TransformTracks)
                        {
                            track.OrientationCurve.UpgradeToGr7();
                            track.PositionCurve.UpgradeToGr7();
                            track.ScaleShearCurve.UpgradeToGr7();
                        }
                    }
                }
            }
        }

        public void PreSave()
        {
        }
    }
}
