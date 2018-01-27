using System.Collections.Generic;
using System.Linq;
using OpenTK;
using LSLib.Granny.GR2;

namespace LSLib.Granny.Model
{
    public class BoneMaxProperties
    {
        public sbyte Hide;
        public sbyte Freeze;
        public sbyte SeeThrough;
        public sbyte DisplayAsBox;
        public sbyte BackfaceCull;
        public sbyte Trajectory;
        public sbyte VertexTicks;
        public sbyte IgnoreExtents;
        public sbyte ShowFrozenInGray;
        public sbyte Renderable;
        public sbyte InheritVisibility;
        public sbyte VisibleToCamera;
        public sbyte VisibleToReflectionRefraction;
        public sbyte ReceiveShadows;
        public sbyte CastShadows;
        public sbyte ApplyAtmospherics;
        public sbyte RenderOccludedObjects;
        [Serialization(ArraySize = 3)]
        public sbyte[] Unused;
    }

    public class BoneExtendedData
    {
        public string UserDefinedProperties;
        [Serialization(DataArea = true)]
        public BoneMaxProperties MaxProperties;
        public int GBufferObjId;
        public int MotionBlur_Type;
        public float MotionBlur_Multiplier;
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
        public BoneExtendedData ExtendedData;

        [Serialization(Kind = SerializationKind.None)]
        public string TransformSID;
        [Serialization(Kind = SerializationKind.None)]
        public Matrix4 OriginalTransform;

        public Matrix4 CalculateInverseWorldTransform(List<Bone> bones)
        {
            var iwt = Matrix4.Identity;
            var currentBone = this;
            while (true)
            {
                var untranslated = currentBone.Transform.ToMatrix4();
                iwt = iwt * untranslated;
                if (currentBone.ParentIndex == -1) break;
                currentBone = bones[currentBone.ParentIndex];
            }

            return iwt.Inverted();
        }

        public void UpdateInverseWorldTransform(List<Bone> bones)
        {
            var iwt = CalculateInverseWorldTransform(bones);
            InverseWorldTransform = new float[] {
                iwt[0, 0], iwt[0, 1], iwt[0, 2], iwt[0, 3],
                iwt[1, 0], iwt[1, 1], iwt[1, 2], iwt[1, 3],
                iwt[2, 0], iwt[2, 1], iwt[2, 2], iwt[2, 3],
                iwt[3, 0], iwt[3, 1], iwt[3, 2], iwt[3, 3]
            };
        }

        public static Bone FromCollada(node bone, int parentIndex, List<Bone> bones, Dictionary<string, Bone> boneSIDs, Dictionary<string, Bone> boneIDs)
        {
            var transMat = ColladaHelpers.TransformFromNode(bone);
            var colladaBone = new Bone();
            colladaBone.TransformSID = transMat.TransformSID;
            var myIndex = bones.Count;
            bones.Add(colladaBone);
            boneSIDs.Add(bone.sid, colladaBone);
            if (bone.id != null)
            {
                boneIDs.Add(bone.id, colladaBone);
            }

            colladaBone.ParentIndex = parentIndex;
            colladaBone.Name = bone.name;
            colladaBone.LODError = 0; // TODO
            colladaBone.OriginalTransform = transMat.transform;
            colladaBone.Transform = Transform.FromMatrix4(transMat.transform);
            colladaBone.UpdateInverseWorldTransform(bones);

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

        public node MakeCollada(string parentName)
        {
            var node = new node();
            node.id = "Bone_" + Name.Replace(' ', '_');
            node.name = Name; // .Replace(' ', '_');
            node.sid = Name.Replace(' ', '_');
            node.type = NodeType.JOINT;

            var transforms = new List<object>();
            var transformTypes = new List<ItemsChoiceType2>();

            var transform = new matrix();
            transform.sid = "Transform";
            var mat = Transform.ToMatrix4();
            mat.Transpose();
            transform.Values = new double[] {
                mat[0, 0], mat[0, 1], mat[0, 2], mat[0, 3],
                mat[1, 0], mat[1, 1], mat[1, 2], mat[1, 3],
                mat[2, 0], mat[2, 1], mat[2, 2], mat[2, 3],
                mat[3, 0], mat[3, 1], mat[3, 2], mat[3, 3]
            };
            transforms.Add(transform);
            transformTypes.Add(ItemsChoiceType2.matrix);

            node.Items = transforms.ToArray();
            node.ItemsElementName = transformTypes.ToArray();
            return node;
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

        public static Skeleton FromCollada(node root)
        {
            var skeleton = new Skeleton();
            skeleton.Bones = new List<Bone>();
            skeleton.LODType = 0;
            skeleton.Name = root.name;
            skeleton.BonesBySID = new Dictionary<string, Bone>();
            skeleton.BonesByID = new Dictionary<string, Bone>();
            Bone.FromCollada(root, -1, skeleton.Bones, skeleton.BonesBySID, skeleton.BonesByID);
            return skeleton;
        }

        public Bone GetBoneByName(string name)
        {
            return Bones.FirstOrDefault(b => b.Name == name);
        }

        public void UpdateInverseWorldTransforms()
        {
            foreach (var bone in Bones)
            {
                bone.UpdateInverseWorldTransform(Bones);
            }
        }
    }
}
