﻿using OpenTK.Mathematics;
using LSLib.Granny.GR2;
using System.Xml;

namespace LSLib.Granny.Model;

public class DivinityBoneExtendedData
{
    public String UserDefinedProperties;
    public Int32 IsRigid;
}

public class Bone
{
    public string Name;
    public int ParentIndex;
    public Transform Transform;
    [Serialization(ArraySize = 16)]
    public float[] InverseWorldTransform;
    public float LODError;
    [Serialization(Type = MemberType.VariantReference)]
    public DivinityBoneExtendedData ExtendedData;

    [Serialization(Kind = SerializationKind.None)]
    public string TransformSID;
    [Serialization(Kind = SerializationKind.None)]
    public Matrix4 OriginalTransform;
    [Serialization(Kind = SerializationKind.None)]
    public Matrix4 WorldTransform;
    [Serialization(Kind = SerializationKind.None)]
    public int ExportIndex = -1;

    public bool IsRoot { get { return ParentIndex == -1; } }

    public void UpdateWorldTransforms(List<Bone> bones)
    {
        var localTransform = Transform.ToMatrix4Composite();
        if (IsRoot)
        {
            WorldTransform = localTransform;
        }
        else
        {
            var parentBone = bones[ParentIndex];
            WorldTransform = localTransform * parentBone.WorldTransform;
        }

        var iwt = WorldTransform.Inverted();
        InverseWorldTransform = [
            iwt[0, 0], iwt[0, 1], iwt[0, 2], iwt[0, 3],
            iwt[1, 0], iwt[1, 1], iwt[1, 2], iwt[1, 3],
            iwt[2, 0], iwt[2, 1], iwt[2, 2], iwt[2, 3],
            iwt[3, 0], iwt[3, 1], iwt[3, 2], iwt[3, 3]
        ];
    }

    private void ImportLSLibProfile(node node)
    {
        var extraData = ColladaImporter.FindExporterExtraData(node.extra);
        if (extraData != null && extraData.Any != null)
        {
            foreach (var setting in extraData.Any)
            {
                switch (setting.LocalName)
                {
                    case "BoneIndex":
                        ExportIndex = Int32.Parse(setting.InnerText.Trim());
                        break;

                    default:
                        Utils.Warn($"Unrecognized LSLib bone attribute: {setting.LocalName}");
                        break;
                }
            }
        }
    }

    public static Bone FromCollada(node bone, int parentIndex, List<Bone> bones, Dictionary<string, Bone> boneSIDs, Dictionary<string, Bone> boneIDs)
    {
        var transMat = ColladaHelpers.TransformFromNode(bone);
        var myIndex = bones.Count;
        var colladaBone = new Bone
        {
            TransformSID = transMat.TransformSID,
            ParentIndex = parentIndex,
            Name = bone.name,
            LODError = 0, // TODO
            OriginalTransform = transMat.transform,
            Transform = Transform.FromMatrix4(transMat.transform)
        };

        if (bone.id != null)
        {
            boneIDs.Add(bone.id, colladaBone);
        }

        bones.Add(colladaBone);
        boneSIDs.Add(bone.sid, colladaBone);

        colladaBone.UpdateWorldTransforms(bones);
        colladaBone.ImportLSLibProfile(bone);

        if (bone.node1 != null)
        {
            foreach (var node in bone.node1)
            {
                if (node.type == NodeType.JOINT)
                {
                    FromCollada(node, myIndex, bones, boneSIDs, boneIDs);
                }
            }
        }

        return colladaBone;
    }
    private technique ExportLSLibProfile(XmlDocument Xml)
    {
        var profile = new technique()
        {
            profile = "LSTools"
        };

        var props = new List<XmlElement>();
        var prop = Xml.CreateElement("BoneIndex");
        prop.InnerText = ExportIndex.ToString();
        props.Add(prop);
        profile.Any = props.ToArray();
        return profile;
    }

    public node MakeCollada(XmlDocument Xml)
    {
        var mat = Transform.ToMatrix4();
        mat.Transpose();

        return new node
        {
            id = "Bone_" + Name.Replace(' ', '_'),
            name = Name, // .Replace(' ', '_');
            sid = Name.Replace(' ', '_'),
            type = NodeType.JOINT,

            Items = [
                new matrix
                {
                    sid = "Transform",
                    Values = [
                        mat[0, 0], mat[0, 1], mat[0, 2], mat[0, 3],
                        mat[1, 0], mat[1, 1], mat[1, 2], mat[1, 3],
                        mat[2, 0], mat[2, 1], mat[2, 2], mat[2, 3],
                        mat[3, 0], mat[3, 1], mat[3, 2], mat[3, 3]
                    ]
                }
            ],
            ItemsElementName = [ItemsChoiceType2.matrix],

            extra =
            [
                new extra
                {
                    technique =
                    [
                        ExportLSLibProfile(Xml)
                    ]
                }
            ]
        };
    }

    private bool Mirror(string from, string to)
    {
        int pos = 0;
        while (true)
        {
            pos = Name.IndexOf(from, pos);
            if (pos == -1)
            {
                return false;
            }

            if (pos + 2 == Name.Length || Name[pos+2] == '_')
            {
                Name = Name[..pos] + to + Name.Substring(pos+2);
                return true;
            }

            pos += 2;
        }
    }

    public bool Mirror()
    {
        return Mirror("_l", "_r")
            || Mirror("_L", "_R")
            || Mirror("_r", "_l")
            || Mirror("_R", "_L");
    }
}

public class Skeleton
{
    public string Name;
    public List<Bone> Bones;
    public int LODType;
    [Serialization(Type = MemberType.VariantReference, MinVersion = 0x80000027)]
    public object ExtendedData;

    [Serialization(Kind = SerializationKind.None)]
    public Dictionary<string, Bone> BonesBySID;

    [Serialization(Kind = SerializationKind.None)]
    public Dictionary<string, Bone> BonesByID;

    [Serialization(Kind = SerializationKind.None)]
    public bool IsDummy = false;

    public static Skeleton CreateEmpty(string name)
    {
        return new Skeleton
        {
            Bones = [],
            LODType = 1,
            Name = name,
            BonesBySID = [],
            BonesByID = []
        };
    }

    public static Skeleton FromCollada(node root)
    {
        var skeleton = new Skeleton
        {
            Bones = [],
            LODType = 1,
            Name = root.name,
            BonesBySID = [],
            BonesByID = []
        };
        Bone.FromCollada(root, -1, skeleton.Bones, skeleton.BonesBySID, skeleton.BonesByID);
        return skeleton;
    }

    public Bone GetBoneByName(string name)
    {
        return Bones.FirstOrDefault(b => b.Name == name);
    }

    public void TransformRoots(Matrix4 transform)
    {
        foreach (var bone in Bones)
        {
            if (bone.IsRoot)
            {
                var boneTransform = bone.Transform.ToMatrix4() * transform;
                bone.Transform = GR2.Transform.FromMatrix4(boneTransform);
            }
        }

        UpdateWorldTransforms();
    }

    public void Mirror()
    {
        foreach (var bone in Bones)
        {
            bone.Mirror();
        }
    }

    public void UpdateWorldTransforms()
    {
        foreach (var bone in Bones)
        {
            bone.UpdateWorldTransforms(Bones);
        }
    }

    public void ReorderBones()
    {
        // Reorder bones based on their ExportOrder
        if (Bones.Any(m => m.ExportIndex > -1))
        {
            var newBones = Bones.ToList();
            newBones.Sort((a, b) => a.ExportIndex - b.ExportIndex);

            // Fix up parent indices
            foreach (var bone in newBones)
            {
                if (bone.ParentIndex != -1)
                {
                    var parent = Bones[bone.ParentIndex];
                    bone.ParentIndex = newBones.IndexOf(parent);
                }
            }

            Bones = newBones;
        }
    }

    private bool CheckIsDummy(Root root)
    {
        // If we have any skinned meshes, the skeleton cannot be dummy
        var hasSkinnedMeshes = root.Models != null 
            && root.Models.Any((model) => model.Skeleton == this) // We have a binding for this skeleton
            && root.Meshes != null
            && root.Meshes.Any((mesh) => mesh.IsSkinned()); // ... and the mesh has bone weights
        if (hasSkinnedMeshes) return false;

        // If we have animations (that have skeleton bindings), the skeleton cannot be dummy
        if (root.Animations != null && root.Animations.Count > 0) return false;

        // If we don't have any meshes (i.e. only exporting the skeleton resource), always include
        // the skeleton even if it's a dummy skel
        if (root.Meshes == null || root.Meshes.Count == 0) return false;

        // Check if the skeleton conforms to one of the dummy patterns:
        //  1) A single dummy root bone
        if (Bones.Count == 1) return true;

        //  2) A bone for each mesh parented to a dummy root bone
        if (Bones.Count == 1 + root.Meshes.Count)
        {
            foreach (var bone in Bones)
            {
                if (!bone.IsRoot && bone.ParentIndex != 0) return false;
            }

            HashSet<string> marked = [];
            foreach (var mesh in root.Meshes)
            {
                if (mesh.BoneBindings == null
                    || mesh.BoneBindings.Count != 1)
                {
                    return false;
                }

                if (marked.Contains(mesh.BoneBindings[0].BoneName)) return false;
                marked.Add(mesh.BoneBindings[0].BoneName);
            }

            return true;
        }

        return false;
    }

    public void PostLoad(Root root)
    {
        if (CheckIsDummy(root))
        {
            IsDummy = true;
        }

        for (var i = 0; i < Bones.Count; i++)
        {
            Bones[i].ExportIndex = i;
        }
    }
}
