using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Collada141;
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

        public static Bone FromCollada(node bone, int parentIndex, List<Bone> bones, Dictionary<string, Bone> boneSIDs, Dictionary<string, Bone> boneIDs)
        {
            var transMat = ColladaHelpers.TransformFromNode(bone);
            var colladaBone = new Bone();
            colladaBone.TransformSID = transMat.TransformSID;
            var myIndex = bones.Count;
            bones.Add(colladaBone);
            boneSIDs.Add(bone.sid, colladaBone);
            boneIDs.Add(bone.id, colladaBone);
            colladaBone.ParentIndex = parentIndex;
            colladaBone.Name = bone.name;
            colladaBone.LODError = 0; // TODO
            colladaBone.Transform = transMat.transform;

            var iwt = colladaBone.CalculateInverseWorldTransform(bones);
            colladaBone.InverseWorldTransform = new float[] {
                iwt[0, 0], iwt[0, 1], iwt[0, 2], iwt[0, 3], 
                iwt[1, 0], iwt[1, 1], iwt[1, 2], iwt[1, 3], 
                iwt[2, 0], iwt[2, 1], iwt[2, 2], iwt[2, 3], 
                iwt[3, 0], iwt[3, 1], iwt[3, 2], iwt[3, 3]
            };

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

            if (false) // Separate transforms
            {
                var rotationX = new rotate();
                rotationX.sid = "RotateX";
                transforms.Add(rotationX);
                transformTypes.Add(ItemsChoiceType2.rotate);

                var rotationY = new rotate();
                rotationY.sid = "RotateY";
                transforms.Add(rotationY);
                transformTypes.Add(ItemsChoiceType2.rotate);

                var rotationZ = new rotate();
                rotationZ.sid = "RotateZ";
                transforms.Add(rotationZ);
                transformTypes.Add(ItemsChoiceType2.rotate);

                if ((Transform.Flags & (uint)Transform.TransformFlags.HasRotation) != 0)
                {
                    var rot = Transform.Rotation.Normalized();
                    //var x = Math.Atan2(2 * (rot.W * rot.X + rot.Y * rot.Z), 1 - 2 * (rot.X * rot.X + rot.Y * rot.Y));
                    //var y = Math.Asin(2 * (rot.W * rot.Y - rot.X * rot.Z));
                    //var z = Math.Atan2(2 * (rot.W * rot.Z + rot.X * rot.Y), 1 - 2 * (rot.Y * rot.Y + rot.Z * rot.Z));

                    //var x = Math.Atan2(2 * rot.Y * rot.W - 2 * rot.X * rot.Z, 1 - 2 * (rot.Y * rot.Y + rot.Z * rot.Z));
                    //var y = Math.Asin(2 * (rot.X * rot.Y + rot.Z * rot.W));
                    //var z = Math.Atan2(2 * rot.X * rot.W - 2 * rot.Y * rot.Z, 1 - 2 * (rot.X * rot.X + rot.Z * rot.Z));
                    var q = new float[] { rot.X, rot.Y, rot.Z, rot.W };

                    // sedris z-y-x
                    /*var x = Math.Atan2(q[2] * q[3] + q[0] * q[1], 0.5f - (q[1] * q[1] + q[2] * q[2]));
                    var y = Math.Asin(-2 * (q[1] * q[3] - q[0] * q[2]));
                    var z = Math.Atan2(q[1] * q[2] + q[0] * q[3], 0.5f - (q[2] * q[2] + q[3] * q[3]));*/

                    var sqw = rot.W * rot.W;
                    var sqx = rot.X * rot.X;
                    var sqy = rot.Y * rot.Y;
                    var sqz = rot.Z * rot.Z;
                    var x = Math.Atan2(2.0 * (rot.X * rot.Y + rot.Z * rot.W), (sqx - sqy - sqz + sqw));
                    var z = Math.Atan2(2.0 * (rot.Y * rot.Z + rot.X * rot.W), (-sqx - sqy + sqz + sqw));
                    var y = Math.Asin(-2.0 * (rot.X * rot.Z - rot.Y * rot.W) / (sqx + sqy + sqz + sqw));

                    rotationX.Values = new double[] { 1.0, 0.0, 0.0, x * 180 / Math.PI };
                    rotationY.Values = new double[] { 0.0, 1.0, 0.0, y * 180 / Math.PI };
                    rotationZ.Values = new double[] { 0.0, 0.0, 1.0, z * 180 / Math.PI };

                    var axisAngle = Transform.Rotation.ToAxisAngle();
                    //rotation.Values = new double[] { axisAngle.X, axisAngle.Y, axisAngle.Z, axisAngle.W * 180 / Math.PI };
                    //rotationX.Values = new double[] { axisAngle.X, 0.0, 0.0, axisAngle.W * 180 / Math.PI };
                    //rotationY.Values = new double[] { 0.0, axisAngle.Y, 0.0, axisAngle.W * 180 / Math.PI };
                    //rotationZ.Values = new double[] { 0.0, 0.0, axisAngle.Z, axisAngle.W * 180 / Math.PI };
                    /*rotationX.Values = new double[] { 1.0, 0.0, 0.0, axisAngle.X * axisAngle.W * 180 / Math.PI };
                    rotationY.Values = new double[] { 0.0, 1.0, 0.0, axisAngle.Y * axisAngle.W * 180 / Math.PI };
                    rotationZ.Values = new double[] { 0.0, 0.0, 1.0, axisAngle.Z * axisAngle.W * 180 / Math.PI };*/
                }
                else
                {
                    rotationX.Values = new double[] { 1.0, 0.0, 0.0, 0.0 };
                    rotationY.Values = new double[] { 0.0, 0.0, 0.0, 0.0 };
                    rotationZ.Values = new double[] { 0.0, 0.0, 0.0, 0.0 };
                }

                var scale = new TargetableFloat3();
                scale.sid = "Scale";
                transforms.Add(scale);
                transformTypes.Add(ItemsChoiceType2.scale);

                if ((Transform.Flags & (uint)Transform.TransformFlags.HasScaleShear) != 0)
                {
                    var scaleShear = Transform.ScaleShear.Diagonal;
                    scale.Values = new double[] { scaleShear.X, scaleShear.Y, scaleShear.Z };
                }
                else
                {
                    scale.Values = new double[] { 1.0, 1.0, 1.0 };
                }

                var translate = new TargetableFloat3();
                translate.sid = "Translate";
                transforms.Add(translate);
                transformTypes.Add(ItemsChoiceType2.translate);

                if ((Transform.Flags & (uint)Transform.TransformFlags.HasTranslation) != 0)
                {
                    var transVec = Transform.Translation;
                    translate.Values = new double[] { transVec.X, transVec.Y, transVec.Z };
                }
                else
                {
                    translate.Values = new double[] { 0.0, 0.0, 0.0 };
                }
            }
            else
            {
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
            }

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

        [Serialization(Kind = SerializationKind.None)]
        public Dictionary<string, Bone> BonesBySID;

        [Serialization(Kind = SerializationKind.None)]
        public Dictionary<string, Bone> BonesByID;

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
    }
}
