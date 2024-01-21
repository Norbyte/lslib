using LSLib.Granny.GR2;
using OpenTK.Mathematics;

namespace LSLib.Granny.Model;

public class Root
{
    public ArtToolInfo ArtToolInfo;
    public ExporterInfo ExporterInfo;
    public string FromFileName;
    [Serialization(Type = MemberType.ArrayOfReferences)]
    public List<Texture> Textures;
    [Serialization(Type = MemberType.ArrayOfReferences)]
    public List<Material> Materials;
    [Serialization(Section = SectionType.Skeleton, Type = MemberType.ArrayOfReferences)]
    public List<Skeleton> Skeletons;
    [Serialization(Type = MemberType.ArrayOfReferences, SectionSelector = typeof(VertexDataSectionSelector))]
    public List<VertexData> VertexDatas;
    [Serialization(Type = MemberType.ArrayOfReferences, SectionSelector = typeof(TriTopologySectionSelector))]
    public List<TriTopology> TriTopologies;
    [Serialization(Section = SectionType.Mesh, Type = MemberType.ArrayOfReferences)]
    public List<Mesh> Meshes;
    [Serialization(Type = MemberType.ArrayOfReferences)]
    public List<Model> Models;
    [Serialization(Section = SectionType.TrackGroup, Type = MemberType.ArrayOfReferences)]
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

        ArtToolInfo?.SetYUp();

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

        foreach (var vertexData in VertexDatas ?? Enumerable.Empty<VertexData>())
        {
            vertexData.PostLoad();
        }

        foreach (var triTopology in TriTopologies ?? Enumerable.Empty<TriTopology>())
        {
            triTopology.PostLoad();
        }

        Meshes?.ForEach(m => m.PostLoad());

        var modelIndex = 0;
        foreach (var model in Models ?? Enumerable.Empty<Model>())
        {
            foreach (var binding in model.MeshBindings ?? Enumerable.Empty<MeshBinding>())
            {
                binding.Mesh.ExportOrder = modelIndex++;
            }
        }

        foreach (var skeleton in Skeletons ?? Enumerable.Empty<Skeleton>())
        {
            skeleton.PostLoad(this);
        }

        // Upgrade legacy animation formats
        foreach (var group in TrackGroups ?? Enumerable.Empty<TrackGroup>())
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

    public void PreSave()
    {
    }
}
