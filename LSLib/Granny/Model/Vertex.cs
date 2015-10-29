using LSLib.Granny.GR2;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LSLib.Granny.Model
{
    public struct BoneWeight
    {
        public byte A, B, C, D;

        /// <summary>
        /// Gets or sets the value at the index of the weight vector.
        /// </summary>
        public byte this[int index]
        {
            get
            {
                if (index == 0) return A;
                else if (index == 1) return B;
                else if (index == 2) return C;
                else if (index == 3) return D;
                throw new IndexOutOfRangeException("Illegal bone influence index: " + index);
            }
            set
            {
                if (index == 0) A = value;
                else if (index == 1) B = value;
                else if (index == 2) C = value;
                else if (index == 3) D = value;
                else throw new IndexOutOfRangeException("Illegal bone influence index: " + index);
            }
        }
    }

    public abstract class Vertex
    {
        public Vector3 Position;
        public BoneWeight BoneWeights;
        public BoneWeight BoneIndices;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector3 Binormal;
        public Vector4 DiffuseColor0;
        public Vector2 TextureCoordinates0;

        protected Vector2 ReadVector2(GR2Reader reader)
        {
            Vector2 v;
            v.X = reader.Reader.ReadSingle();
            v.Y = reader.Reader.ReadSingle();
            return v;
        }

        protected Vector3 ReadVector3(GR2Reader reader)
        {
            Vector3 v;
            v.X = reader.Reader.ReadSingle();
            v.Y = reader.Reader.ReadSingle();
            v.Z = reader.Reader.ReadSingle();
            return v;
        }

        protected Vector4 ReadVector4(GR2Reader reader)
        {
            Vector4 v;
            v.X = reader.Reader.ReadSingle();
            v.Y = reader.Reader.ReadSingle();
            v.Z = reader.Reader.ReadSingle();
            v.W = reader.Reader.ReadSingle();
            return v;
        }

        protected BoneWeight ReadInfluences(GR2Reader reader)
        {
            BoneWeight v;
            v.A = reader.Reader.ReadByte();
            v.B = reader.Reader.ReadByte();
            v.C = reader.Reader.ReadByte();
            v.D = reader.Reader.ReadByte();
            return v;
        }

        protected void WriteVector2(WritableSection section, Vector2 v)
        {
            section.Writer.Write(v.X);
            section.Writer.Write(v.Y);
        }

        protected void WriteVector3(WritableSection section, Vector3 v)
        {
            section.Writer.Write(v.X);
            section.Writer.Write(v.Y);
            section.Writer.Write(v.Z);
        }

        protected void WriteVector4(WritableSection section, Vector4 v)
        {
            section.Writer.Write(v.X);
            section.Writer.Write(v.Y);
            section.Writer.Write(v.Z);
            section.Writer.Write(v.W);
        }

        protected void WriteInfluences(WritableSection section, BoneWeight v)
        {
            section.Writer.Write(v.A);
            section.Writer.Write(v.B);
            section.Writer.Write(v.C);
            section.Writer.Write(v.D);
        }

        public Vertex Clone()
        {
            return MemberwiseClone() as Vertex;
        }

        public void AddInfluence(byte boneIndex, float weight)
        {
            // Get the first zero vertex influence and update it with the new one
            for (var influence = 0; influence < 4; influence++)
            {
                if (BoneWeights[influence] == 0)
                {
                    // BoneIndices refers to Mesh.BoneBindings[index], not Skeleton.Bones[index] !
                    BoneIndices[influence] = boneIndex;
                    BoneWeights[influence] = (byte)(Math.Round(weight * 255));
                    break;
                }
            }
        }

        public void FinalizeInfluences()
        {
            for (var influence = 1; influence < 4; influence++)
            {
                if (BoneWeights[influence] == 0)
                {
                    BoneIndices[influence] = BoneIndices[0];
                }
            }
        }

        public abstract bool HasBoneInfluences();
        public abstract void Serialize(WritableSection section);
        public abstract void Unserialize(GR2Reader reader);
        public abstract Type Prototype();
    }

    internal class Vertex_PNGBT33332_Prototype
    {
        [Serialization(ArraySize = 3)]
        public float[] Position;
        [Serialization(ArraySize = 3)]
        public float[] Normal;
        [Serialization(ArraySize = 3)]
        public float[] Tangent;
        [Serialization(ArraySize = 3)]
        public float[] Binormal;
        [Serialization(ArraySize = 2)]
        public float[] TextureCoordinates0;
    }

    public class Vertex_PNGBT33332 : Vertex
    {
        public override bool HasBoneInfluences()
        {
            return false;
        }

        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
            WriteVector3(section, Normal);
            WriteVector3(section, Tangent);
            WriteVector3(section, Binormal);
            WriteVector2(section, TextureCoordinates0);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
            Normal = ReadVector3(reader);
            Tangent = ReadVector3(reader);
            Binormal = ReadVector3(reader);
            TextureCoordinates0 = ReadVector2(reader);
        }

        public override Type Prototype()
        {
            return typeof(Vertex_PNGBT33332_Prototype);
        }
    }

    internal class Vertex_PNGBDT333342_Prototype
    {
        [Serialization(ArraySize = 3)]
        public float[] Position;
        [Serialization(ArraySize = 3)]
        public float[] Normal;
        [Serialization(ArraySize = 3)]
        public float[] Tangent;
        [Serialization(ArraySize = 3)]
        public float[] Binormal;
        [Serialization(ArraySize = 4)]
        public float[] DiffuseColor0;
        [Serialization(ArraySize = 2)]
        public float[] TextureCoordinates0;
    }

    public class Vertex_PNGBDT333342 : Vertex
    {
        public override bool HasBoneInfluences()
        {
            return false;
        }

        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
            WriteVector3(section, Normal);
            WriteVector3(section, Tangent);
            WriteVector3(section, Binormal);
            WriteVector4(section, DiffuseColor0);
            WriteVector2(section, TextureCoordinates0);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
            Normal = ReadVector3(reader);
            Tangent = ReadVector3(reader);
            Binormal = ReadVector3(reader);
            DiffuseColor0 = ReadVector4(reader);
            TextureCoordinates0 = ReadVector2(reader);
        }

        public override Type Prototype()
        {
            return typeof(Vertex_PNGBDT333342_Prototype);
        }
    }

    internal class Vertex_PWNGBT343332_Prototype
    {
        [Serialization(ArraySize = 3)]
        public float[] Position;
        [Serialization(ArraySize = 4)]
        public byte[] BoneWeights;
        [Serialization(ArraySize = 4)]
        public byte[] BoneIndices;
        [Serialization(ArraySize = 3)]
        public float[] Normal;
        [Serialization(ArraySize = 3)]
        public float[] Tangent;
        [Serialization(ArraySize = 3)]
        public float[] Binormal;
        [Serialization(ArraySize = 2)]
        public float[] TextureCoordinates0;
    }

    public class Vertex_PWNGBT343332 : Vertex
    {
        public override bool HasBoneInfluences()
        {
            return true;
        }

        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
            WriteInfluences(section, BoneWeights);
            WriteInfluences(section, BoneIndices);
            WriteVector3(section, Normal);
            WriteVector3(section, Tangent);
            WriteVector3(section, Binormal);
            WriteVector2(section, TextureCoordinates0);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
            BoneWeights = ReadInfluences(reader);
            BoneIndices = ReadInfluences(reader);
            Normal = ReadVector3(reader);
            Tangent = ReadVector3(reader);
            Binormal = ReadVector3(reader);
            TextureCoordinates0 = ReadVector2(reader);
        }

        public override Type Prototype()
        {
            return typeof(Vertex_PWNGBT343332_Prototype);
        }
    }

    internal class Vertex_PWNT343332_Prototype
    {
        [Serialization(ArraySize = 3)]
        public float[] Position;
        [Serialization(ArraySize = 4)]
        public byte[] BoneWeights;
        [Serialization(ArraySize = 4)]
        public byte[] BoneIndices;
        [Serialization(ArraySize = 3)]
        public float[] Normal;
        [Serialization(ArraySize = 2)]
        public float[] TextureCoordinates0;
    }

    public class Vertex_PWNT343332 : Vertex
    {
        public override bool HasBoneInfluences()
        {
            return true;
        }

        public override void Serialize(WritableSection section)
        {
            WriteVector3(section, Position);
            WriteInfluences(section, BoneWeights);
            WriteInfluences(section, BoneIndices);
            WriteVector3(section, Normal);
            WriteVector2(section, TextureCoordinates0);
        }

        public override void Unserialize(GR2Reader reader)
        {
            Position = ReadVector3(reader);
            BoneWeights = ReadInfluences(reader);
            BoneIndices = ReadInfluences(reader);
            Normal = ReadVector3(reader);
            TextureCoordinates0 = ReadVector2(reader);
        }

        public override Type Prototype()
        {
            return typeof(Vertex_PWNT343332_Prototype);
        }
    }

    public class VertexSerializer : VariantTypeSelector, NodeSerializer, SectionSelector
    {
        private Dictionary<object, Type> VertexTypeCache = new Dictionary<object,Type>();

        private bool CompareType(StructDefinition defn, Type type)
        {
            var fields = type.GetFields();
            if (defn.Members.Count != fields.Length)
                return false;

            for (var i = 0; i < fields.Length; i++)
            {
                if (fields[i].Name != defn.Members[i].Name)
                    return false;

                if (fields[i].FieldType.IsArray)
                {
                    var attrs = fields[i].GetCustomAttributes(typeof(SerializationAttribute), true);
                    if (attrs.Length == 0)
                        throw new InvalidOperationException("Array fields must have a valid SerializationAttribute");

                    var attr = attrs[0] as SerializationAttribute;
                    if (attr.ArraySize != defn.Members[i].ArraySize)
                        return false;
                }
            }

            return true;
        }

        public SectionType SelectSection(MemberDefinition member, Type type, object obj)
        {
            var vertices = obj as System.Collections.IList;
            if (vertices == null || vertices.Count == 0)
                return SectionType.RigidVertex;

            if ((vertices[0] as Vertex).HasBoneInfluences())
                return SectionType.DeformableVertex;
            else
                return SectionType.RigidVertex;
        }

        public Type SelectType(MemberDefinition member, object node)
        {
            var list = node as System.Collections.IList;
            if (list == null || list.Count == 0)
                return null;

            return (list[0] as Vertex).Prototype();
        }

        public Type SelectType(MemberDefinition member, StructDefinition defn, object parent)
        {
            if (CompareType(defn, typeof(Vertex_PNGBT33332_Prototype)))
            {
                return typeof(Vertex_PNGBT33332);
            }
            else if (CompareType(defn, typeof(Vertex_PNGBDT333342_Prototype)))
            {
                return typeof(Vertex_PNGBDT333342);
            }
            else if (CompareType(defn, typeof(Vertex_PWNGBT343332_Prototype)))
            {
                return typeof(Vertex_PWNGBT343332);
            }
            else
                throw new Exception("The specified vertex format was not recognized.");
        }

        public object Read(GR2Reader reader, StructDefinition definition, MemberDefinition member, uint arraySize, object parent)
        {
            Type type;
            if (!VertexTypeCache.TryGetValue(parent, out type))
            {
                type = SelectType(member, definition, parent);
                VertexTypeCache.Add(parent, type);
            }

            var vertex = Helpers.CreateInstance(type);
            (vertex as Vertex).Unserialize(reader);
            return vertex;
        }

        public void Write(GR2Writer writer, WritableSection section, MemberDefinition member, object obj)
        {
            (obj as Vertex).Serialize(section);
        }
    }
}
