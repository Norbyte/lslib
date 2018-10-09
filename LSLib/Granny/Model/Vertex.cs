using LSLib.Granny.GR2;
using OpenTK;
using System;
using System.Collections.Generic;

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

    public enum NormalType
    {
        None,
        Float3,
        Half4,
        QTangent
    };

    public enum DiffuseColorType
    {
        None,
        Float4,
        Byte4
    };

    public enum TextureCoordinateType
    {
        None,
        Float2,
        Half2
    };

    /// <summary>
    /// Describes the properties (Position, Normal, Tangent, ...) of the vertex format
    /// </summary>
    public class VertexDescriptor
    {
        public bool HasPosition = true;
        public bool HasBoneWeights = false;
        public int NumBoneInfluences = 4;
        public NormalType NormalType = NormalType.None;
        public NormalType TangentType = NormalType.None;
        public NormalType BinormalType = NormalType.None;
        public DiffuseColorType DiffuseType = DiffuseColorType.None;
        public int DiffuseColors = 0;
        public TextureCoordinateType TextureCoordinateType = TextureCoordinateType.None;
        public int TextureCoordinates = 0;
        private Type VertexType;

        public List<String> ComponentNames()
        {
            var names = new List<String>();
            if (HasPosition)
            {
                names.Add("Position");
            }

            if (HasBoneWeights)
            {
                names.Add("BoneWeights");
                names.Add("BoneIndices");
            }

            if (NormalType != NormalType.None)
            {
                if (NormalType == NormalType.QTangent)
                {
                    names.Add("QTangent");
                }
                else
                {
                    names.Add("Normal");
                }
            }

            if (TangentType != NormalType.None
                && TangentType != NormalType.QTangent)
            {
                names.Add("Tangent");
            }

            if (BinormalType != NormalType.None
                && BinormalType != NormalType.QTangent)
            {
                names.Add("Binormal");
            }

            if (DiffuseType != DiffuseColorType.None)
            {
                for (int i = 0; i < DiffuseColors; i++)
                {
                    names.Add("DiffuseColor_" + i.ToString());
                }
            }

            if (TextureCoordinateType != TextureCoordinateType.None)
            {
                for (int i = 0; i < TextureCoordinates; i++)
                {
                    names.Add("TextureCoordinate_" + i.ToString());
                }
            }

            return names;
        }

        public String Name()
        {
            string vertexFormat;
            vertexFormat = "P";
            string attributeCounts = "3";

            if (HasBoneWeights)
            {
                vertexFormat += "W";
                attributeCounts += NumBoneInfluences.ToString();
            }
            
            switch (NormalType)
            {
                case NormalType.None:
                    break;

                case NormalType.Float3:
                    vertexFormat += "N";
                    attributeCounts += "3";
                    break;

                case NormalType.Half4:
                    vertexFormat += "HN";
                    attributeCounts += "4";
                    break;

                case NormalType.QTangent:
                    vertexFormat += "QN";
                    attributeCounts += "4";
                    break;
            }

            switch (TangentType)
            {
                case NormalType.None:
                    break;

                case NormalType.Float3:
                    vertexFormat += "G";
                    attributeCounts += "3";
                    break;

                case NormalType.Half4:
                    vertexFormat += "HG";
                    attributeCounts += "4";
                    break;
            }

            switch (BinormalType)
            {
                case NormalType.None:
                    break;

                case NormalType.Float3:
                    vertexFormat += "B";
                    attributeCounts += "3";
                    break;

                case NormalType.Half4:
                    vertexFormat += "HB";
                    attributeCounts += "4";
                    break;
            }

            for (var i = 0; i < DiffuseColors; i++)
            {
                switch (DiffuseType)
                {
                    case DiffuseColorType.None:
                        break;

                    case DiffuseColorType.Float4:
                        vertexFormat += "D";
                        attributeCounts += "4";
                        break;

                    case DiffuseColorType.Byte4:
                        vertexFormat += "CD";
                        attributeCounts += "4";
                        break;
                }
            }

            for (var i = 0; i < TextureCoordinates; i++)
            {
                switch (TextureCoordinateType)
                {
                    case TextureCoordinateType.None:
                        break;

                    case TextureCoordinateType.Float2:
                        vertexFormat += "T";
                        attributeCounts += "2";
                        break;

                    case TextureCoordinateType.Half2:
                        vertexFormat += "HT";
                        attributeCounts += "2";
                        break;
                }
            }

            return vertexFormat + attributeCounts;
        }

        public Vertex CreateInstance()
        {
            if (VertexType == null)
            {
                var typeName = "Vertex_" + Name();
                VertexType = VertexTypeBuilder.CreateVertexSubtype(typeName);
            }

            var vert = Activator.CreateInstance(VertexType) as Vertex;
            vert.Format = this;
            return vert;
        }
    }

    [StructSerialization(TypeSelector = typeof(VertexDefinitionSelector))]
    public class Vertex
    {
        public VertexDescriptor Format;
        public Vector3 Position;
        public BoneWeight BoneWeights;
        public BoneWeight BoneIndices;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector3 Binormal;
        public Vector4 DiffuseColor0;
        public Vector2 TextureCoordinates0;
        public Vector2 TextureCoordinates1;

        protected Vertex() { }

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

        public void Serialize(WritableSection section)
        {
            VertexSerializationHelpers.Serialize(section, this);
        }

        public void Unserialize(GR2Reader reader)
        {
            VertexSerializationHelpers.Unserialize(reader, this);
        }
    }
    

    public class VertexSerializer : NodeSerializer, SectionSelector
    {
        private Dictionary<object, VertexDescriptor> VertexTypeCache = new Dictionary<object, VertexDescriptor>();

        public SectionType SelectSection(MemberDefinition member, Type type, object obj)
        {
            var vertices = obj as List<Vertex>;
            if (vertices == null || vertices.Count == 0)
                return SectionType.RigidVertex;

            if (vertices[0].Format.HasBoneWeights)
                return SectionType.DeformableVertex;
            else
                return SectionType.RigidVertex;
        }

        public VertexDescriptor ConstructDescriptor(MemberDefinition memberDefn, StructDefinition defn, object parent)
        {
            var desc = new VertexDescriptor();
            
            foreach (var member in defn.Members)
            {
                switch (member.Name)
                {
                    case "Position":
                        if (member.Type != MemberType.Real32
                            || member.ArraySize != 3)
                        {
                            throw new Exception("Vertex position must be a Vector3");
                        }
                        desc.HasPosition = true;
                        break;

                    case "BoneWeights":
                        if (member.Type != MemberType.NormalUInt8)
                        {
                            throw new Exception("Bone weight must be a NormalUInt8");
                        }

                        if (member.ArraySize != 2 && member.ArraySize != 4)
                        {
                            throw new Exception("Unsupported bone influence count");
                        }

                        desc.HasBoneWeights = true;
                        desc.NumBoneInfluences = (int)member.ArraySize;
                        break;

                    case "BoneIndices":
                        if (member.Type != MemberType.UInt8)
                        {
                            throw new Exception("Bone index must be an UInt8");
                        }
                        break;

                    case "Normal":
                        if (member.Type == MemberType.Real32 && member.ArraySize == 3)
                        {
                            desc.NormalType = NormalType.Float3;
                        }
                        else if (member.Type == MemberType.Real16 && member.ArraySize == 4)
                        {
                            desc.NormalType = NormalType.Half4;
                        }
                        else
                        {
                            throw new Exception("Unsupported vertex normal format");
                        }
                        break;

                    case "QTangent":
                        if (member.Type == MemberType.BinormalInt16 && member.ArraySize == 4)
                        {
                            desc.NormalType = NormalType.QTangent;
                            desc.TangentType = NormalType.QTangent;
                            desc.BinormalType = NormalType.QTangent;
                        }
                        else
                        {
                            throw new Exception("Unsupported QTangent format");
                        }
                        break;

                    case "Tangent":
                        if (member.Type == MemberType.Real32 && member.ArraySize == 3)
                        {
                            desc.TangentType = NormalType.Float3;
                        }
                        else if (member.Type == MemberType.Real16 && member.ArraySize == 4)
                        {
                            desc.NormalType = NormalType.Half4;
                        }
                        else
                        {
                            throw new Exception("Unsupported vertex tangent format");
                        }
                        break;

                    case "Binormal":
                        if (member.Type == MemberType.Real32 && member.ArraySize == 3)
                        {
                            desc.BinormalType = NormalType.Float3;
                        }
                        else if (member.Type == MemberType.Real16 && member.ArraySize == 4)
                        {
                            desc.NormalType = NormalType.Half4;
                        }
                        else
                        {
                            throw new Exception("Unsupported vertex binormal format");
                        }
                        break;

                    case "DiffuseColor0":
                        desc.DiffuseColors = 1;
                        if (member.Type == MemberType.Real32 && member.ArraySize == 4)
                        {
                            desc.DiffuseType = DiffuseColorType.Float4;
                        }
                        else if (member.Type == MemberType.NormalUInt8 && member.ArraySize == 4)
                        {
                            desc.DiffuseType = DiffuseColorType.Byte4;
                        }
                        else
                        {
                            throw new Exception("Unsupported vertex diffuse color type");
                        }
                        break;

                    case "TextureCoordinates0":
                        desc.TextureCoordinates = 1;
                        if (member.Type == MemberType.Real32 && member.ArraySize == 2)
                        {
                            desc.TextureCoordinateType = TextureCoordinateType.Float2;
                        }
                        else if (member.Type == MemberType.Real16 && member.ArraySize == 2)
                        {
                            desc.TextureCoordinateType = TextureCoordinateType.Half2;
                        }
                        else
                        {
                            throw new Exception("Unsupported vertex binormal format");
                        }
                        break;

                    case "TextureCoordinates1":
                        desc.TextureCoordinates = 2;
                        break;

                    default:
                        throw new Exception($"Unknown vertex property: {member.Name}");
                }
            }

            return desc;
        }

        public object Read(GR2Reader reader, StructDefinition definition, MemberDefinition member, uint arraySize, object parent)
        {
            VertexDescriptor descriptor;
            if (!VertexTypeCache.TryGetValue(parent, out descriptor))
            {
                descriptor = ConstructDescriptor(member, definition, parent);
                VertexTypeCache.Add(parent, descriptor);
            }

            var vertex = descriptor.CreateInstance();
            vertex.Unserialize(reader);
            return vertex;
        }

        public void Write(GR2Writer writer, WritableSection section, MemberDefinition member, object obj)
        {
            (obj as Vertex).Serialize(section);
        }
    }
}
