using LSLib.Granny.GR2;
using LSLib.Granny.Model.VertexFormat;
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

    /// <summary>
    /// Describes the type we use for serializing this vertex format
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class VertexPrototypeAttribute : System.Attribute
    {
        /// <summary>
        /// The Granny prototype we should save when serializing this vertex format
        /// (Used to provide a type definition skeleton for the serializer)
        /// </summary>
        public Type Prototype;
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

        public static Type Prototype(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(VertexPrototypeAttribute), true);
            if (attrs.Length > 0)
            {
                VertexPrototypeAttribute proto = attrs[0] as VertexPrototypeAttribute;
                return proto.Prototype;
            }

            throw new ArgumentException("Class doesn't have a vertex prototype");
        }

        public Type Prototype()
        {
            return Prototype(GetType());
        }

        public abstract bool HasBoneInfluences();
        public abstract void Serialize(WritableSection section);
        public abstract void Unserialize(GR2Reader reader);
    }

    public class VertexFormatRegistry
    {
        private static Dictionary<String, Type> NameToTypeMap;
        private static Dictionary<Type, Type> PrototypeMap;

        private static void Register(Type type)
        {
            NameToTypeMap.Add(type.Name, type);
            PrototypeMap.Add(Vertex.Prototype(type), type);
        }

        private static void Init()
        {
            if (NameToTypeMap != null)
            {
                return;
            }

            NameToTypeMap = new Dictionary<String, Type>();
            PrototypeMap = new Dictionary<Type, Type>();

            Register(typeof(PNGBDT333342));
            Register(typeof(PNGBT33332));
            Register(typeof(PWNGBT343332));
            Register(typeof(PWNT3432));
        }

        public static Type Resolve(String name)
        {
            Init();

            Type type = null;
            if (!NameToTypeMap.TryGetValue(name, out type))
                throw new ParsingException("Unsupported vertex format: " + name);

            return type;
        }

        public static Type FindByStruct(StructDefinition defn)
        {
            Init();

            foreach (var proto in PrototypeMap)
            {
                if (CompareType(defn, proto.Key))
                {
                    return proto.Value;
                }
            }

            throw new Exception("The specified vertex format was not recognized.");
        }

        public static Dictionary<String, Type> GetAllTypes()
        {
            Init();

            return NameToTypeMap;
        }

        private static bool CompareType(StructDefinition defn, Type type)
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
    }


    public class VertexSerializer : VariantTypeSelector, NodeSerializer, SectionSelector
    {
        private Dictionary<object, Type> VertexTypeCache = new Dictionary<object,Type>();

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
            return VertexFormatRegistry.FindByStruct(defn);
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
