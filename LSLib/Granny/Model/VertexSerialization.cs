using LSLib.Granny.GR2;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace LSLib.Granny.Model
{
    public static class VertexSerializationHelpers
    {
        public static Vector2 ReadVector2(GR2Reader reader)
        {
            Vector2 v;
            v.X = reader.Reader.ReadSingle();
            v.Y = reader.Reader.ReadSingle();
            return v;
        }

        public static Vector2 ReadHalfVector2(GR2Reader reader)
        {
            Vector2 v;
            v.X = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            v.Y = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            return v;
        }

        public static Vector3 ReadVector3(GR2Reader reader)
        {
            Vector3 v;
            v.X = reader.Reader.ReadSingle();
            v.Y = reader.Reader.ReadSingle();
            v.Z = reader.Reader.ReadSingle();
            return v;
        }

        public static Vector3 ReadHalfVector3(GR2Reader reader)
        {
            Vector3 v;
            v.X = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            v.Y = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            v.Z = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            return v;
        }

        public static Vector3 ReadHalfVector4As3(GR2Reader reader)
        {
            Vector3 v;
            v.X = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            v.Y = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            v.Z = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            reader.Reader.ReadUInt16();
            return v;
        }

        public static Vector4 ReadBinormalShortVector4(GR2Reader reader)
        {
            Vector4 v;
            v.X = reader.Reader.ReadInt16() / 32767.0f;
            v.Y = reader.Reader.ReadInt16() / 32767.0f;
            v.Z = reader.Reader.ReadInt16() / 32767.0f;
            v.W = reader.Reader.ReadInt16() / 32767.0f;
            return v;
        }

        public static Vector4 ReadVector4(GR2Reader reader)
        {
            Vector4 v;
            v.X = reader.Reader.ReadSingle();
            v.Y = reader.Reader.ReadSingle();
            v.Z = reader.Reader.ReadSingle();
            v.W = reader.Reader.ReadSingle();
            return v;
        }

        public static Vector4 ReadHalfVector4(GR2Reader reader)
        {
            Vector4 v;
            v.X = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            v.Y = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            v.Z = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            v.W = HalfHelpers.HalfToSingle(reader.Reader.ReadUInt16());
            return v;
        }

        public static Vector4 ReadNormalByteVector4(GR2Reader reader)
        {
            Vector4 v;
            v.X = reader.Reader.ReadByte() * 255.0f;
            v.Y = reader.Reader.ReadByte() * 255.0f;
            v.Z = reader.Reader.ReadByte() * 255.0f;
            v.W = reader.Reader.ReadByte() * 255.0f;
            return v;
        }

        public static Matrix3 ReadQTangent(GR2Reader reader)
        {
            Vector4 qTangent = ReadBinormalShortVector4(reader);
            return QTangentToMatrix(qTangent);
        }

        private static Vector4 MatrixToQTangent(Matrix3 m)
        {
            float f = 1.0f;

            var dir =
                (m[0, 0] * m[1, 1] * m[2, 2] +
                m[0, 1] * m[1, 2] * m[2, 0] +
                m[0, 2] * m[1, 0] * m[2, 1]) -
                (m[0, 2] * m[1, 1] * m[2, 0] +
                m[0, 1] * m[1, 0] * m[2, 2] +
                m[0, 0] * m[1, 2] * m[2, 1]);
            if (dir < 0.0)
            {
                f = -1.0f;
                m.Row2 = -m.Row2;
            }

            float t = m[0, 0] + (m[1, 1] + m[2, 2]);
            Vector4 r;

            if (t > 2.9999999f)
            {
                r = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            }
            else if (t > 0.0000001f)
            {
                float s = (float)Math.Sqrt(1.0f + t) * 2.0f;
                r = new Vector4(
                    (m[1, 2] - m[2, 1]) / s, 
                    (m[2, 0] - m[0, 2]) / s, 
                    (m[0, 1] - m[1, 0]) / s,
                    s * 0.25f);
            }
            else if ((m[0, 0] > m[1, 1]) && (m[0, 0] > m[2, 2]))
            {
                float s = (float)Math.Sqrt(1.0f + m[0, 0] - m[1, 1] + m[2, 2]) * 2.0f;
                r = new Vector4(
                    s * 0.25f, 
                    (m[1, 0] - m[0, 1]) / s, 
                    (m[2, 0] - m[0, 2]) / s, 
                    (m[0, 2] - m[2, 1]) / s);
            }
            else if (m[1, 1] > m[2, 2])
            {
                float s = (float)Math.Sqrt(1.0f + m[1, 1] - m[0, 0] + m[2, 2]) * 2.0f;
                r = new Vector4(
                    (m[1, 0] + m[0, 1]) / s,
                    s * 0.25f,
                    (m[2, 1] + m[1, 2]) / s,
                    (m[2, 0] - m[0, 2]) / s);
            }
            else
            {
                float s = (float)Math.Sqrt(1.0f + m[2, 2] - m[0, 0] + m[1, 1]) * 2.0f;
                r = new Vector4(
                    (m[2, 0] + m[0, 2]) / s,
                    (m[2, 1] + m[1, 2]) / s,
                    s * 0.25f,
                    (m[0, 1] - m[1, 0]) / s);
            }

            r.Normalize();

            const float threshold = 1.0f / 32767.0f;

            if (r.W <= threshold)
            {
                var mul = (float)Math.Sqrt(1.0f - (threshold * threshold));
                r = new Vector4(
                    r.X * mul,
                    r.Y * mul,
                    r.Z * mul,
                    (r.W > 0.0f) ? threshold : -threshold
                );
            }

            if (((f < 0.0f) && (r.W >= 0.0f)) || ((f >= 0.0f) && (r.W < 0.0f)))
            {
                r = -r;
            }

            return r;
        }

        private static Matrix3 QTangentToMatrix(Vector4 qTangent)
        {
            var q = qTangent.Normalized();
            float qx2 = q.X + q.X,
                  qy2 = q.Y + q.Y,
                  qz2 = q.Z + q.Z,
                  qxqx2 = q.X * qx2,
                  qxqy2 = q.X * qy2,
                  qxqz2 = q.X * qz2,
                  qxqw2 = q.W * qx2,
                  qyqy2 = q.Y * qy2,
                  qyqz2 = q.Y * qz2,
                  qyqw2 = q.W * qy2,
                  qzqz2 = q.Z * qz2,
                  qzqw2 = q.W * qz2;
            Matrix3 m = new Matrix3(
                1.0f - (qyqy2 + qzqz2), qxqy2 + qzqw2, qxqz2 - qyqw2,
                qxqy2 - qzqw2, 1.0f - (qxqx2 + qzqz2), qyqz2 + qxqw2,
                qxqz2 + qyqw2, qyqz2 - qxqw2, 1.0f - (qxqx2 + qyqy2)
            );

            var row1 = Vector3.Cross(m.Row0, m.Row1).Normalized() * ((q.W < 0.0f) ? -1.0f : 1.0f);
            m.Row1 = row1;
            return m;
        }

        public static BoneWeight ReadInfluences2(GR2Reader reader)
        {
            BoneWeight v;
            v.A = reader.Reader.ReadByte();
            v.B = reader.Reader.ReadByte();
            v.C = 0;
            v.D = 0;
            return v;
        }

        public static BoneWeight ReadInfluences(GR2Reader reader)
        {
            BoneWeight v;
            v.A = reader.Reader.ReadByte();
            v.B = reader.Reader.ReadByte();
            v.C = reader.Reader.ReadByte();
            v.D = reader.Reader.ReadByte();
            return v;
        }

        public static void WriteVector2(WritableSection section, Vector2 v)
        {
            section.Writer.Write(v.X);
            section.Writer.Write(v.Y);
        }

        public static void WriteHalfVector2(WritableSection section, Vector2 v)
        {
            section.Writer.Write(HalfHelpers.SingleToHalf(v.X));
            section.Writer.Write(HalfHelpers.SingleToHalf(v.Y));
        }

        public static void WriteVector3(WritableSection section, Vector3 v)
        {
            section.Writer.Write(v.X);
            section.Writer.Write(v.Y);
            section.Writer.Write(v.Z);
        }

        public static void WriteHalfVector3(WritableSection section, Vector3 v)
        {
            section.Writer.Write(HalfHelpers.SingleToHalf(v.X));
            section.Writer.Write(HalfHelpers.SingleToHalf(v.Y));
            section.Writer.Write(HalfHelpers.SingleToHalf(v.Z));
        }

        public static void WriteHalfVector3As4(WritableSection section, Vector3 v)
        {
            section.Writer.Write(HalfHelpers.SingleToHalf(v.X));
            section.Writer.Write(HalfHelpers.SingleToHalf(v.Y));
            section.Writer.Write(HalfHelpers.SingleToHalf(v.Z));
            section.Writer.Write((ushort)0);
        }

        public static void WriteBinormalShortVector4(WritableSection section, Vector4 v)
        {
            section.Writer.Write((Int16)(v.X * 32767.0f));
            section.Writer.Write((Int16)(v.Y * 32767.0f));
            section.Writer.Write((Int16)(v.Z * 32767.0f));
            section.Writer.Write((Int16)(v.W * 32767.0f));
        }

        public static void WriteVector4(WritableSection section, Vector4 v)
        {
            section.Writer.Write(v.X);
            section.Writer.Write(v.Y);
            section.Writer.Write(v.Z);
            section.Writer.Write(v.W);
        }

        public static void WriteHalfVector4(WritableSection section, Vector4 v)
        {
            section.Writer.Write(HalfHelpers.SingleToHalf(v.X));
            section.Writer.Write(HalfHelpers.SingleToHalf(v.Y));
            section.Writer.Write(HalfHelpers.SingleToHalf(v.Z));
            section.Writer.Write(HalfHelpers.SingleToHalf(v.W));
        }

        public static void WriteNormalByteVector4(WritableSection section, Vector4 v)
        {
            section.Writer.Write((byte)(v.X * 255));
            section.Writer.Write((byte)(v.Y * 255));
            section.Writer.Write((byte)(v.Z * 255));
            section.Writer.Write((byte)(v.W * 255));
        }

        public static void WriteInfluences2(WritableSection section, BoneWeight v)
        {
            section.Writer.Write(v.A);
            section.Writer.Write(v.B);
        }

        public static void WriteInfluences(WritableSection section, BoneWeight v)
        {
            section.Writer.Write(v.A);
            section.Writer.Write(v.B);
            section.Writer.Write(v.C);
            section.Writer.Write(v.D);
        }

        public static void WriteQTangent(WritableSection section, Vector3 normal, Vector3 tangent, Vector3 binormal)
        {
            Matrix3 normals = new Matrix3(normal, tangent, binormal);
            var qTangent = MatrixToQTangent(normals);
            WriteBinormalShortVector4(section, qTangent);
        }

        public static void Serialize(WritableSection section, Vertex v)
        {
            var d = v.Format;

            if (d.HasPosition)
            {
                WriteVector3(section, v.Position);
            }

            if (d.HasBoneWeights)
            {
                if (d.NumBoneInfluences == 2)
                {
                    WriteInfluences2(section, v.BoneWeights);
                    WriteInfluences2(section, v.BoneIndices);
                }
                else
                {
                    WriteInfluences(section, v.BoneWeights);
                    WriteInfluences(section, v.BoneIndices);
                }
            }

            switch (d.NormalType)
            {
                case NormalType.None: break;
                case NormalType.Float3: WriteVector3(section, v.Normal); break;
                case NormalType.Half4: WriteHalfVector3As4(section, v.Normal); break;
                case NormalType.QTangent: WriteQTangent(section, v.Normal, v.Tangent, v.Binormal); break;
            }

            switch (d.TangentType)
            {
                case NormalType.None: break;
                case NormalType.Float3: WriteVector3(section, v.Tangent); break;
                case NormalType.Half4: WriteHalfVector3As4(section, v.Tangent); break;
                case NormalType.QTangent: break; // Tangent saved into QTangent
            }

            switch (d.BinormalType)
            {
                case NormalType.None: break;
                case NormalType.Float3: WriteVector3(section, v.Binormal); break;
                case NormalType.Half4: WriteHalfVector3As4(section, v.Binormal); break;
                case NormalType.QTangent: break; // Binormal saved into QTangent
            }

            switch (d.DiffuseType)
            {
                case DiffuseColorType.None: break;
                case DiffuseColorType.Float4: WriteVector4(section, v.DiffuseColor0); break;
                case DiffuseColorType.Byte4: WriteNormalByteVector4(section, v.DiffuseColor0); break;
            }

            if (d.TextureCoordinates > 0)
            {
                switch (d.TextureCoordinateType)
                {
                    case TextureCoordinateType.None: break;
                    case TextureCoordinateType.Float2: WriteVector2(section, v.TextureCoordinates0); break;
                    case TextureCoordinateType.Half2: WriteHalfVector2(section, v.TextureCoordinates0); break;
                }

                if (d.TextureCoordinates > 1)
                {
                    switch (d.TextureCoordinateType)
                    {
                        case TextureCoordinateType.None: break;
                        case TextureCoordinateType.Float2: WriteVector2(section, v.TextureCoordinates1); break;
                        case TextureCoordinateType.Half2: WriteHalfVector2(section, v.TextureCoordinates1); break;
                    }
                }
            }
        }

        public static void Unserialize(GR2Reader reader, Vertex v)
        {
            var d = v.Format;

            if (d.HasPosition)
            {
                v.Position = ReadVector3(reader);
            }

            if (d.HasBoneWeights)
            {
                if (d.NumBoneInfluences == 2)
                {
                    v.BoneWeights = ReadInfluences2(reader);
                    v.BoneIndices = ReadInfluences2(reader);
                }
                else
                {
                    v.BoneWeights = ReadInfluences(reader);
                    v.BoneIndices = ReadInfluences(reader);
                }
            }

            switch (d.NormalType)
            {
                case NormalType.None: break;
                case NormalType.Float3: v.Normal = ReadVector3(reader); break;
                case NormalType.Half4: v.Normal = ReadHalfVector4As3(reader); break;
                case NormalType.QTangent:
                    {
                        var qTangent = ReadQTangent(reader);
                        v.Normal = qTangent.Row0;
                        v.Tangent = qTangent.Row1;
                        v.Binormal = qTangent.Row2;
                        break;
                    }
            }

            switch (d.TangentType)
            {
                case NormalType.None: break;
                case NormalType.Float3: v.Tangent = ReadVector3(reader); break;
                case NormalType.Half4: v.Tangent = ReadHalfVector4As3(reader); break;
                case NormalType.QTangent: break; // Tangent read from QTangent
            }

            switch (d.BinormalType)
            {
                case NormalType.None: break;
                case NormalType.Float3: v.Binormal = ReadVector3(reader); break;
                case NormalType.Half4: v.Binormal = ReadHalfVector4As3(reader); break;
                case NormalType.QTangent: break; // Binormal read from QTangent
            }

            switch (d.DiffuseType)
            {
                case DiffuseColorType.None: break;
                case DiffuseColorType.Float4: v.DiffuseColor0 = ReadVector4(reader); break;
                case DiffuseColorType.Byte4: v.DiffuseColor0 = ReadNormalByteVector4(reader); break;
            }

            if (d.TextureCoordinates > 0)
            {
                switch (d.TextureCoordinateType)
                {
                    case TextureCoordinateType.None: break;
                    case TextureCoordinateType.Float2: v.TextureCoordinates0 = ReadVector2(reader); break;
                    case TextureCoordinateType.Half2: v.TextureCoordinates0 = ReadHalfVector2(reader); break;
                }

                if (d.TextureCoordinates > 1)
                {
                    switch (d.TextureCoordinateType)
                    {
                        case TextureCoordinateType.None: break;
                        case TextureCoordinateType.Float2: v.TextureCoordinates1 = ReadVector2(reader); break;
                        case TextureCoordinateType.Half2: v.TextureCoordinates1 = ReadHalfVector2(reader); break;
                    }
                }
            }
        }
    }

    public static class VertexTypeBuilder
    {
        private static ModuleBuilder ModBuilder;

        private static ModuleBuilder GetModuleBuilder()
        {
            if (ModBuilder != null)
            {
                return ModBuilder;
            }

            var an = new AssemblyName("VertexFactoryAssembly");
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("VertexFactoryClasses");
            ModBuilder = moduleBuilder;
            return ModBuilder;
        }

        public static Type CreateVertexSubtype(string className)
        {
            var cls = GetModuleBuilder().GetType(className);
            if (cls != null)
            {
                return cls;
            }

            TypeBuilder tb = GetModuleBuilder().DefineType(className,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    null);
            ConstructorBuilder constructor = tb.DefineDefaultConstructor(
                MethodAttributes.Public | 
                MethodAttributes.SpecialName | 
                MethodAttributes.RTSpecialName);

            tb.SetParent(typeof(Vertex));

            return tb.CreateType();
        }
    }

    class VertexDefinitionSelector : StructDefinitionSelector
    {
        private void AddMember(StructDefinition defn, String name, MemberType type, UInt32 arraySize)
        {
            var member = new MemberDefinition
            {
                Type = type,
                Name = name,
                GrannyName = name,
                Definition = null,
                ArraySize = arraySize,
                Extra = new UInt32[] { 0, 0, 0 },
                Unknown = 0
            };
            defn.Members.Add(member);
        }

        public StructDefinition CreateStructDefinition(object instance)
        {
            var desc = (instance as Vertex).Format;
            var defn = new StructDefinition
            {
                Members = new List<MemberDefinition>(),
                MixedMarshal = false,
                Type = typeof(Vertex)
            };

            if (desc.HasPosition)
            {
                AddMember(defn, "Position", MemberType.Real32, 3);
            }

            if (desc.HasBoneWeights)
            {
                AddMember(defn, "BoneWeights", MemberType.NormalUInt8, (UInt32)desc.NumBoneInfluences);
                AddMember(defn, "BoneIndices", MemberType.UInt8, (UInt32)desc.NumBoneInfluences);
            }

            switch (desc.NormalType)
            {
                case NormalType.None: break;
                case NormalType.Float3: AddMember(defn, "Normal", MemberType.Real32, 3); break;
                case NormalType.Half4: AddMember(defn, "Normal", MemberType.Real16, 4); break;
                case NormalType.QTangent: AddMember(defn, "QTangent", MemberType.BinormalInt16, 4); break;
            }

            switch (desc.TangentType)
            {
                case NormalType.None: break;
                case NormalType.Float3: AddMember(defn, "Tangent", MemberType.Real32, 3); break;
                case NormalType.Half4: AddMember(defn, "Tangent", MemberType.Real16, 4); break;
                case NormalType.QTangent: break; // Tangent saved into QTangent
            }

            switch (desc.BinormalType)
            {
                case NormalType.None: break;
                case NormalType.Float3: AddMember(defn, "Binormal", MemberType.Real32, 3); break;
                case NormalType.Half4: AddMember(defn, "Binormal", MemberType.Real16, 4); break;
                case NormalType.QTangent: break; // Binormal saved into QTangent
            }

            for (int i = 0; i < desc.DiffuseColors; i++)
            {
                switch (desc.DiffuseType)
                {
                    case DiffuseColorType.Float4: AddMember(defn, "DiffuseColor" + i.ToString(), MemberType.Real32, 4); break;
                    case DiffuseColorType.Byte4: AddMember(defn, "DiffuseColor" + i.ToString(), MemberType.NormalUInt8, 4); break;
                }
            }

            for (int i = 0; i < desc.TextureCoordinates; i++)
            {
                switch (desc.TextureCoordinateType)
                {
                    case TextureCoordinateType.Float2: AddMember(defn, "TextureCoordinates" + i.ToString(), MemberType.Real32, 2); break;
                    case TextureCoordinateType.Half2: AddMember(defn, "TextureCoordinates" + i.ToString(), MemberType.Real16, 2); break;
                }
            }

            return defn;
        }
    }
}
