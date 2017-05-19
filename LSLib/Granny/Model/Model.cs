using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Collada141;
using LSLib.Granny.GR2;
using OpenTK;

namespace LSLib.Granny.Model
{
    public class MeshBinding
    {
        public Mesh Mesh;
    }

    public class Model
    {
        public string Name;
        public Skeleton Skeleton;
        public Transform InitialPlacement;
        [Serialization(DataArea = true)]
        public List<MeshBinding> MeshBindings;
        [Serialization(Type = MemberType.VariantReference, MinVersion = 0x80000027)]
        public object ExtendedData;

        public node MakeBone(int index, Bone bone)
        {
            var node = bone.MakeCollada(Name);
            var children = new List<node>();
            for (int i = 0; i < Skeleton.Bones.Count; i++)
            {
                if (Skeleton.Bones[i].ParentIndex == index)
                    children.Add(MakeBone(i, Skeleton.Bones[i]));
            }

            node.node1 = children.ToArray();
            return node;
        }

        public node MakeSkeleton()
        {
            // Find the root bone and export it
            for (int i = 0; i < Skeleton.Bones.Count; i++)
            {
                if (Skeleton.Bones[i].ParentIndex == -1)
                {
                    var root = MakeBone(i, Skeleton.Bones[i]);
                    // root.id = Name + "-skeleton";
                    return root;
                }
            }

            throw new ParsingException("Model has no root bone!");
        }
    }
}
