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

    /// <summary>
    /// Describes the properties (Position, Normal, Tangent, ...) this vertex format has
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class VertexDescriptionAttribute : System.Attribute
    {
        public bool Position = true;
        public bool BoneWeights = false;
        public bool BoneIndices = false;
        public bool Normal = false;
        public bool Tangent = false;
        public bool Binormal = false;
        public int DiffuseColors = 0;
        public int TextureCoordinates = 0;
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
        public Vector2 TextureCoordinates1;

        public Vector2 GetUV(int index)
        {
            if (index == 0)
                return TextureCoordinates0;
            else if (index == 1)
                return TextureCoordinates1;
            else
                throw new ArgumentException("At most 2 UV sets are supported.");
        }

        public void SetUV(int index, Vector2 uv)
        {
            if (index == 0)
                TextureCoordinates0 = uv;
            else if (index == 1)
                TextureCoordinates1 = uv;
            else
                throw new ArgumentException("At most 2 UV sets are supported.");
        }

        public Vector4 GetColor(int index)
        {
            if (index == 0)
                return DiffuseColor0;
            else
                throw new ArgumentException("At most 1 diffuse color set is supported.");
        }

        public void SetColor(int index, Vector4 color)
        {
            if (index == 0)
                DiffuseColor0 = color;
            else
                throw new ArgumentException("At most 1 diffuse color set is supported.");
        }

        protected Vector2 ReadVector2(GR2Reader reader)
        {
            Vector2 v;
            v.X = reader.Reader.ReadSingle();
            v.Y = reader.Reader.ReadSingle();
            return v;
        }

        protected Vector2 ReadHalfVector2(GR2Reader reader)
        {
            Vector2 v;
            v.X = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            v.Y = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
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

        protected Vector3 ReadHalfVector3(GR2Reader reader)
        {
            Vector3 v;
            v.X = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            v.Y = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            v.Z = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            return v;
        }

        protected Vector3 ReadHalfVector4As3(GR2Reader reader)
        {
            Vector3 v;
            v.X = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            v.Y = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            v.Z = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            reader.Reader.ReadUInt16();
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

        protected Vector4 ReadHalfVector4(GR2Reader reader)
        {
            Vector4 v;
            v.X = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            v.Y = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            v.Z = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            v.W = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            return v;
        }

        protected BoneWeight ReadInfluences2(GR2Reader reader)
        {
            BoneWeight v;
            v.A = reader.Reader.ReadByte();
            v.B = reader.Reader.ReadByte();
            v.C = 0;
            v.D = 0;
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

        protected void WriteHalfVector2(WritableSection section, Vector2 v)
        {
            section.Writer.Write(HalfHelpers.SingleToHalf(v.X));
            section.Writer.Write(HalfHelpers.SingleToHalf(v.Y));
        }

        protected void WriteVector3(WritableSection section, Vector3 v)
        {
            section.Writer.Write(v.X);
            section.Writer.Write(v.Y);
            section.Writer.Write(v.Z);
        }

        protected void WriteHalfVector3(WritableSection section, Vector3 v)
        {
            section.Writer.Write(HalfHelpers.SingleToHalf(v.X));
            section.Writer.Write(HalfHelpers.SingleToHalf(v.Y));
            section.Writer.Write(HalfHelpers.SingleToHalf(v.Z));
        }

        protected void WriteHalfVector3As4(WritableSection section, Vector3 v)
        {
            section.Writer.Write(HalfHelpers.SingleToHalf(v.X));
            section.Writer.Write(HalfHelpers.SingleToHalf(v.Y));
            section.Writer.Write(HalfHelpers.SingleToHalf(v.Z));
            section.Writer.Write((ushort)0);
        }

        protected void WriteVector4(WritableSection section, Vector4 v)
        {
            section.Writer.Write(v.X);
            section.Writer.Write(v.Y);
            section.Writer.Write(v.Z);
            section.Writer.Write(v.W);
        }

        protected void WriteHalfVector4(WritableSection section, Vector4 v)
        {
            section.Writer.Write(HalfHelpers.SingleToHalf(v.X));
            section.Writer.Write(HalfHelpers.SingleToHalf(v.Y));
            section.Writer.Write(HalfHelpers.SingleToHalf(v.Z));
            section.Writer.Write(HalfHelpers.SingleToHalf(v.W));
        }

        protected void WriteInfluences2(WritableSection section, BoneWeight v)
        {
            section.Writer.Write(v.A);
            section.Writer.Write(v.B);
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

        public void Transform(Matrix4 transformation)
        {
            Position = Vector3.TransformPosition(Position, transformation);
            Normal = Vector3.TransformNormal(Normal, transformation);
            Tangent = Vector3.TransformNormal(Tangent, transformation);
            Binormal = Vector3.TransformNormal(Binormal, transformation);
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

        public static VertexDescriptionAttribute Description(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(VertexDescriptionAttribute), true);
            if (attrs.Length > 0)
            {
                return attrs[0] as VertexDescriptionAttribute;
            }

            throw new ArgumentException("Class doesn't have a vertex format descriptor");
        }

        public Type Prototype()
        {
            return Prototype(GetType());
        }


        public abstract List<String> ComponentNames();
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

            Register(typeof(P3));
            Register(typeof(PN33));
            Register(typeof(PNG333));
            Register(typeof(PNGB3333));
            Register(typeof(PNGBDT333342));
            Register(typeof(PNGBT33332));
            Register(typeof(PNGBTT333322));
            Register(typeof(PNGT3332));
            Register(typeof(PNT332));
            Register(typeof(PNTT3322));
            Register(typeof(PNTG3323));
            Register(typeof(PT32));
            Register(typeof(PTT322));
            Register(typeof(PWN323));
            Register(typeof(PWN343));
            Register(typeof(PWNG3233));
            Register(typeof(PWNG3433));
            Register(typeof(PWNGB32333));
            Register(typeof(PWNGB34333));
            Register(typeof(PWNGBT323332));
            Register(typeof(PWNGBT343332));
            Register(typeof(PWNGBDT3433342));
            Register(typeof(PWNGBTT3433322));
            Register(typeof(PWNGT32332));
            Register(typeof(PWNGT34332));
            Register(typeof(PWNT3232));
            Register(typeof(PWNT3432));
            Register(typeof(PHNGBT34444));
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

            ThrowUnknownVertexFormatError(defn);
            return null;
        }

        private static void ThrowUnknownVertexFormatError(StructDefinition defn)
        {
            string formatDesc = "";
            foreach (var field in defn.Members)
            {
                string format = field.Name + ": " + field.Type.ToString() + "[" + field.ArraySize.ToString() + "]";
                formatDesc += format + Environment.NewLine;
            }

            throw new Exception("The specified vertex format was not recognized. Format descriptor: " + Environment.NewLine + formatDesc);
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

            if (Vertex.Description(vertices[0].GetType()).BoneWeights)
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
