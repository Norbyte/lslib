using LSLib.Granny.GR2;
using OpenTK.Mathematics;
using System.Reflection;
using System.Reflection.Emit;

namespace LSLib.Granny.Model;

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

    public static Quaternion ReadBinormalShortVector4(GR2Reader reader)
    {
        return new Quaternion
        {
            X = reader.Reader.ReadInt16() / 32767.0f,
            Y = reader.Reader.ReadInt16() / 32767.0f,
            Z = reader.Reader.ReadInt16() / 32767.0f,
            W = reader.Reader.ReadInt16() / 32767.0f
        };
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
        v.X = reader.Reader.ReadByte() / 255.0f;
        v.Y = reader.Reader.ReadByte() / 255.0f;
        v.Z = reader.Reader.ReadByte() / 255.0f;
        v.W = reader.Reader.ReadByte() / 255.0f;
        return v;
    }

    public static Vector3 ReadNormalSWordVector4As3(GR2Reader reader)
    {
        Vector3 v;
        v.X = reader.Reader.ReadInt16() / 32767.0f;
        v.Y = reader.Reader.ReadInt16() / 32767.0f;
        v.Z = reader.Reader.ReadInt16() / 32767.0f;
        reader.Reader.ReadInt16(); // Unused word
        return v;
    }

    public static Vector3 ReadNormalSByteVector4As3(GR2Reader reader)
    {
        Vector3 v;
        v.X = reader.Reader.ReadSByte() / 127.0f;
        v.Y = reader.Reader.ReadSByte() / 127.0f;
        v.Z = reader.Reader.ReadSByte() / 127.0f;
        reader.Reader.ReadSByte(); // Unused byte
        return v;
    }

    public static Matrix3 ReadQTangent(GR2Reader reader)
    {
        Quaternion qTangent = ReadBinormalShortVector4(reader);
        return QTangentToMatrix(qTangent);
    }

    private static Matrix3 Orthonormalize(Matrix3 m)
    {
        Vector3 x = new Vector3(m.M11, m.M21, m.M31).Normalized();
        Vector3 y = Vector3.Cross(new Vector3(m.M13, m.M23, m.M33), x).Normalized();
        Vector3 z = Vector3.Cross(x, y);
        return new Matrix3(
            x.X, y.X, z.X,
            x.Y, y.Y, z.Y,
            x.Z, y.Z, z.Z
        );
    }

    private static Quaternion MatrixToQTangent(Matrix3 mm, bool reflect)
    {
        var m = Orthonormalize(mm);

        var quat = Quaternion.FromMatrix(m);
        quat.Normalize();

        if (quat.W < 0.0f)
        {
            quat.W = -quat.W;
        }
        else
        {
            quat.Conjugate();
        }

        // Make sure we don't end up with 0 as w component
        const float threshold16bit = 1.0f / 32767.0f;
        if (Math.Abs(quat.W) < threshold16bit)
        {
            var bias16bit = (float)Math.Sqrt(1.0f - (threshold16bit * threshold16bit));
            quat *= bias16bit;
            quat.W = threshold16bit;
        }
        
        // Encode reflection into quaternion's W element by making sign of W negative
        // if Y axis needs to be flipped, positive otherwise
        if (reflect)
        {
            quat = new Quaternion(-quat.Xyz, -quat.W);
        }

        return quat;
    }

    private static Matrix3 QTangentToMatrix(Quaternion q)
    {
        Matrix3 m = new Matrix3(
            1.0f - 2.0f * (q.Y * q.Y + q.Z * q.Z), 2 * (q.X * q.Y + q.W * q.Z), 2 * (q.X * q.Z - q.W * q.Y),
            2.0f * (q.X * q.Y - q.W * q.Z), 1 - 2 * (q.X * q.X + q.Z * q.Z), 2 * (q.Y * q.Z + q.W * q.X),
            0.0f, 0.0f, 0.0f
        );
        
        m.Row2 = Vector3.Cross(m.Row0, m.Row1) * ((q.W < 0.0f) ? -1.0f : 1.0f);
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

    public static void WriteBinormalShortVector4(WritableSection section, Quaternion v)
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
    public static void WriteNormalSWordVector3As4(WritableSection section, Vector3 v)
    {
        section.Writer.Write((Int16)(v.X * 32767));
        section.Writer.Write((Int16)(v.Y * 32767));
        section.Writer.Write((Int16)(v.Z * 32767));
        section.Writer.Write(0);
    }
    public static void WriteNormalSByteVector3As4(WritableSection section, Vector3 v)
    {
        section.Writer.Write((sbyte)(v.X * 127));
        section.Writer.Write((sbyte)(v.Y * 127));
        section.Writer.Write((sbyte)(v.Z * 127));
        section.Writer.Write(0);
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
        var n2 = Vector3.Cross(tangent, binormal).Normalized();
        var reflection = Vector3.Dot(normal, n2);
        Matrix3 normals = new Matrix3(tangent, binormal, n2);
        var qTangent = MatrixToQTangent(normals, reflection < 0.0f);
        WriteBinormalShortVector4(section, qTangent);
    }

    public static void Serialize(WritableSection section, Vertex v)
    {
        var d = v.Format;

        switch (d.PositionType)
        {
            case PositionType.None: break;
            case PositionType.Float3: WriteVector3(section, v.Position); break;
            case PositionType.Word4: WriteNormalSWordVector3As4(section, v.Position); break;
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
            case NormalType.Byte4: WriteNormalSByteVector3As4(section, v.Normal); break;
            case NormalType.QTangent: WriteQTangent(section, v.Normal, v.Tangent, v.Binormal); break;
        }

        switch (d.TangentType)
        {
            case NormalType.None: break;
            case NormalType.Float3: WriteVector3(section, v.Tangent); break;
            case NormalType.Half4: WriteHalfVector3As4(section, v.Tangent); break;
            case NormalType.Byte4: WriteNormalSByteVector3As4(section, v.Tangent); break;
            case NormalType.QTangent: break; // Tangent saved into QTangent
        }

        switch (d.BinormalType)
        {
            case NormalType.None: break;
            case NormalType.Float3: WriteVector3(section, v.Binormal); break;
            case NormalType.Half4: WriteHalfVector3As4(section, v.Binormal); break;
            case NormalType.Byte4: WriteNormalSByteVector3As4(section, v.Binormal); break;
            case NormalType.QTangent: break; // Binormal saved into QTangent
        }

        if (d.ColorMaps > 0)
        {
            for (var i = 0; i < d.ColorMaps; i++)
            {
                var color = v.GetColor(i);
                switch (d.ColorMapType)
                {
                    case ColorMapType.Float4: WriteVector4(section, color); break;
                    case ColorMapType.Byte4: WriteNormalByteVector4(section, color); break;
                    default: throw new Exception($"Cannot unserialize color map: Unsupported format {d.ColorMapType}");
                }
            }
        }

        if (d.TextureCoordinates > 0)
        {
            for (var i = 0; i < d.TextureCoordinates; i++)
            {
                var uv = v.GetUV(i);
                switch (d.TextureCoordinateType)
                {
                    case TextureCoordinateType.Float2: WriteVector2(section, uv); break;
                    case TextureCoordinateType.Half2: WriteHalfVector2(section, uv); break;
                    default: throw new Exception($"Cannot serialize UV map: Unsupported format {d.TextureCoordinateType}");
                }
            }
        }
    }

    public static void Unserialize(GR2Reader reader, Vertex v)
    {
        var d = v.Format;

        switch (d.PositionType)
        {
            case PositionType.None: break;
            case PositionType.Float3: v.Position = ReadVector3(reader); break;
            case PositionType.Word4: v.Position = ReadNormalSWordVector4As3(reader); break;
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
            case NormalType.Byte4: v.Normal = ReadNormalSByteVector4As3(reader); break;
            case NormalType.QTangent:
                {
                    var qTangent = ReadQTangent(reader);
                    v.Normal = qTangent.Row2;
                    v.Tangent = qTangent.Row1;
                    v.Binormal = qTangent.Row0;
                    break;
                }
        }

        switch (d.TangentType)
        {
            case NormalType.None: break;
            case NormalType.Float3: v.Tangent = ReadVector3(reader); break;
            case NormalType.Half4: v.Tangent = ReadHalfVector4As3(reader); break;
            case NormalType.Byte4: v.Tangent = ReadNormalSByteVector4As3(reader); break;
            case NormalType.QTangent: break; // Tangent read from QTangent
        }

        switch (d.BinormalType)
        {
            case NormalType.None: break;
            case NormalType.Float3: v.Binormal = ReadVector3(reader); break;
            case NormalType.Half4: v.Binormal = ReadHalfVector4As3(reader); break;
            case NormalType.Byte4: v.Binormal = ReadNormalSByteVector4As3(reader); break;
            case NormalType.QTangent: break; // Binormal read from QTangent
        }

        if (d.ColorMaps > 0)
        {
            for (var i = 0; i < d.ColorMaps; i++)
            {
                var color = d.ColorMapType switch
                {
                    ColorMapType.Float4 => ReadVector4(reader),
                    ColorMapType.Byte4 => ReadNormalByteVector4(reader),
                    _ => throw new Exception($"Cannot unserialize color map: Unsupported format {d.ColorMapType}"),
                };
                v.SetColor(i, color);
            }
        }

        if (d.TextureCoordinates > 0)
        {
            for (var i = 0; i < d.TextureCoordinates; i++)
            {
                var uv = d.TextureCoordinateType switch
                {
                    TextureCoordinateType.Float2 => ReadVector2(reader),
                    TextureCoordinateType.Half2 => ReadHalfVector2(reader),
                    _ => throw new Exception($"Cannot unserialize UV map: Unsupported format {d.TextureCoordinateType}"),
                };
                v.SetUV(i, uv);
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
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("VertexFactoryClasses");
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
            Extra = [0, 0, 0],
            Unknown = 0
        };
        defn.Members.Add(member);
    }

    public StructDefinition CreateStructDefinition(object instance)
    {
        var desc = (instance as Vertex).Format;
        var defn = new StructDefinition
        {
            Members = [],
            MixedMarshal = true,
            Type = typeof(Vertex)
        };

        switch (desc.PositionType)
        {
            case PositionType.None: break;
            case PositionType.Float3: AddMember(defn, "Position", MemberType.Real32, 3); break;
            case PositionType.Word4: AddMember(defn, "Position", MemberType.BinormalInt16, 4); break;
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
            case NormalType.Byte4: AddMember(defn, "Normal", MemberType.BinormalInt8, 4); break;
            case NormalType.QTangent: AddMember(defn, "QTangent", MemberType.BinormalInt16, 4); break;
        }

        switch (desc.TangentType)
        {
            case NormalType.None: break;
            case NormalType.Float3: AddMember(defn, "Tangent", MemberType.Real32, 3); break;
            case NormalType.Half4: AddMember(defn, "Tangent", MemberType.Real16, 4); break;
            case NormalType.Byte4: AddMember(defn, "Tangent", MemberType.BinormalInt8, 4); break;
            case NormalType.QTangent: break; // Tangent saved into QTangent
        }

        switch (desc.BinormalType)
        {
            case NormalType.None: break;
            case NormalType.Float3: AddMember(defn, "Binormal", MemberType.Real32, 3); break;
            case NormalType.Half4: AddMember(defn, "Binormal", MemberType.Real16, 4); break;
            case NormalType.Byte4: AddMember(defn, "Binormal", MemberType.BinormalInt8, 4); break;
            case NormalType.QTangent: break; // Binormal saved into QTangent
        }

        for (int i = 0; i < desc.ColorMaps; i++)
        {
            switch (desc.ColorMapType)
            {
                case ColorMapType.Float4: AddMember(defn, "DiffuseColor" + i.ToString(), MemberType.Real32, 4); break;
                case ColorMapType.Byte4: AddMember(defn, "DiffuseColor" + i.ToString(), MemberType.NormalUInt8, 4); break;
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
