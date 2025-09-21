using SharpGLTF.Schema2;
using System.Text.Json;

namespace LSLib.Granny.Model;


partial class GLTFSceneExtensions : ExtraProperties
{
    internal GLTFSceneExtensions() { }

    public Int32 MetadataVersion = 0;
    public Int32 LSLibMajor = 0;
    public Int32 LSLibMinor = 0;
    public Int32 LSLibPatch = 0;

    public Dictionary<string, Int32> BoneOrder = [];
    public Dictionary<string, float> BoneScale = [];
    public string SkeletonResourceID;
    public string ModelName;

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        base.SerializeProperties(writer);

        SerializeProperty(writer, "MetadataVersion", MetadataVersion);
        SerializeProperty(writer, "LSLibMajor", LSLibMajor);
        SerializeProperty(writer, "LSLibMinor", LSLibMinor);
        SerializeProperty(writer, "LSLibPatch", LSLibPatch);

        SerializeProperty(writer, "BoneOrder", BoneOrder);
        SerializeProperty(writer, "BoneScale", BoneScale);
        SerializeProperty(writer, "SkeletonResourceID", SkeletonResourceID);
        SerializeProperty(writer, "ModelName", ModelName);
    }

    protected override void DeserializeProperty(string jsonPropertyName, ref Utf8JsonReader reader)
    {
        switch (jsonPropertyName)
        {
            case "MetadataVersion": MetadataVersion = DeserializePropertyValue<Int32>(ref reader); break;
            case "LSLibMajor": LSLibMajor = DeserializePropertyValue<Int32>(ref reader); break;
            case "LSLibMinor": LSLibMinor = DeserializePropertyValue<Int32>(ref reader); break;
            case "LSLibPatch": LSLibPatch = DeserializePropertyValue<Int32>(ref reader); break;

            case "BoneOrder": DeserializePropertyDictionary(ref reader, BoneOrder); break;
            case "BoneScale": DeserializePropertyDictionary(ref reader, BoneScale); break;
            case "SkeletonResourceID": SkeletonResourceID = DeserializePropertyValue<string>(ref reader); break;
            case "ModelName": ModelName = DeserializePropertyValue<string>(ref reader); break;

            default: base.DeserializeProperty(jsonPropertyName, ref reader); break;
        }
    }
}

public partial class GLTFMeshExtensions : ExtraProperties
{
    internal GLTFMeshExtensions() { }

    public bool Rigid = false;
    public bool Cloth = false;
    public bool MeshProxy = false;
    public bool ProxyGeometry = false;
    public bool Spring = false;
    public bool Occluder = false;
    public bool ClothPhysics = false;
    public bool Cloth01 = false;
    public bool Cloth02 = false;
    public bool Cloth04 = false;
    public bool Impostor = false;
    public Int32 ExportOrder = -1;
    public Int32 LOD = 0;
    public Single LODDistance = 0;
    public String ParentBone = "";

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        base.SerializeProperties(writer);
        SerializeProperty(writer, "Rigid", Rigid);
        SerializeProperty(writer, "Cloth", Cloth);
        SerializeProperty(writer, "MeshProxy", MeshProxy);
        SerializeProperty(writer, "ProxyGeometry", ProxyGeometry);
        SerializeProperty(writer, "Spring", Spring);
        SerializeProperty(writer, "Occluder", Occluder);
        SerializeProperty(writer, "ClothPhysics", ClothPhysics);
        SerializeProperty(writer, "Cloth01", Cloth01);
        SerializeProperty(writer, "Cloth02", Cloth02);
        SerializeProperty(writer, "Cloth04", Cloth04);
        SerializeProperty(writer, "Impostor", Impostor);
        SerializeProperty(writer, "ExportOrder", ExportOrder);
        SerializeProperty(writer, "LOD", LOD);
        SerializeProperty(writer, "LODDistance", LODDistance);
        SerializeProperty(writer, "ParentBone", ParentBone);
    }

    protected override void DeserializeProperty(string jsonPropertyName, ref Utf8JsonReader reader)
    {
        switch (jsonPropertyName)
        {
            case "Rigid": Rigid = DeserializePropertyValue<bool>(ref reader); break;
            case "Cloth": Cloth = DeserializePropertyValue<bool>(ref reader); break;
            case "MeshProxy": MeshProxy = DeserializePropertyValue<bool>(ref reader); break;
            case "ProxyGeometry": ProxyGeometry = DeserializePropertyValue<bool>(ref reader); break;
            case "Spring": Spring = DeserializePropertyValue<bool>(ref reader); break;
            case "Occluder": Occluder = DeserializePropertyValue<bool>(ref reader); break;
            case "ClothPhysics": ClothPhysics = DeserializePropertyValue<bool>(ref reader); break;
            case "Cloth01": Cloth01 = DeserializePropertyValue<bool>(ref reader); break;
            case "Cloth02": Cloth02 = DeserializePropertyValue<bool>(ref reader); break;
            case "Cloth04": Cloth04 = DeserializePropertyValue<bool>(ref reader); break;
            case "Impostor": Impostor = DeserializePropertyValue<bool>(ref reader); break;
            case "ExportOrder": ExportOrder = DeserializePropertyValue<Int32>(ref reader); break;
            case "LOD": LOD = DeserializePropertyValue<Int32>(ref reader); break;
            case "LODDistance": LODDistance = DeserializePropertyValue<Single>(ref reader); break;
            case "ParentBone": ParentBone = DeserializePropertyValue<String>(ref reader); break;
            default: base.DeserializeProperty(jsonPropertyName, ref reader); break;
        }
    }

    public void Apply(Mesh mesh, DivinityMeshExtendedData data)
    {
        if (Cloth)
        {
            data.UserMeshProperties.MeshFlags |= DivinityModelFlag.Cloth;
            data.Cloth = 1;
        }
        
        if (Rigid)
        {
            data.UserMeshProperties.MeshFlags |= DivinityModelFlag.Rigid;
            data.Rigid = 1;
        }
        
        if (MeshProxy)
        {
            data.UserMeshProperties.MeshFlags |= DivinityModelFlag.MeshProxy;
            data.MeshProxy = 1;
        }
        
        if (ProxyGeometry)
        {
            data.UserMeshProperties.MeshFlags |= DivinityModelFlag.HasProxyGeometry;
        }
        
        if (Spring)
        {
            data.UserMeshProperties.MeshFlags |= DivinityModelFlag.Spring;
            data.Spring = 1;
        }
        
        if (Occluder)
        {
            data.UserMeshProperties.MeshFlags |= DivinityModelFlag.Occluder;
            data.Occluder = 1;
        }
        
        if (Cloth01) data.UserMeshProperties.ClothFlags |= DivinityClothFlag.Cloth01;
        if (Cloth02) data.UserMeshProperties.ClothFlags |= DivinityClothFlag.Cloth02;
        if (Cloth04) data.UserMeshProperties.ClothFlags |= DivinityClothFlag.Cloth04;
        if (ClothPhysics) data.UserMeshProperties.ClothFlags |= DivinityClothFlag.ClothPhysics;

        data.UserMeshProperties.IsImpostor[0] = Impostor ? 1 : 0;
        mesh.ExportOrder = ExportOrder;

        if (LOD <= 0)
        {
            data.LOD = 0;
            data.UserMeshProperties.Lod[0] = -1;
        }
        else
        {
            data.LOD = LOD;
            data.UserMeshProperties.Lod[0] = LOD;
        }

        if (LODDistance <= 0)
        {
            data.UserMeshProperties.LodDistance[0] = 3.40282347E+38f;
        }
        else
        {
            data.UserMeshProperties.LodDistance[0] = LODDistance;
        }
    }
}


public static partial class GLTFExtensions
{
    private static bool Registered;

    public static void RegisterExtensions()
    {
        if (Registered) return;

        Registered = true;

        ExtensionsFactory.RegisterExtension<Scene, GLTFSceneExtensions>("EXT_lslib_profile", p => new GLTFSceneExtensions());
        ExtensionsFactory.RegisterExtension<SharpGLTF.Schema2.Mesh, GLTFMeshExtensions>("EXT_lslib_profile", p => new GLTFMeshExtensions());
    }
}
