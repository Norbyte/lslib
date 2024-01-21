using System.Diagnostics;
using OpenTK.Mathematics;
using System.Reflection;
using System.IO.Hashing;

namespace LSLib.Granny.GR2;

public class GrannyString
{
    public String String;

    public GrannyString()
    {
    }

    public GrannyString(String s)
    {
        String = s;
    }

    public override string ToString()
    {
        return String;
    }
}

public class Transform
{
    public enum TransformFlags : uint
    {
        HasTranslation = 0x01,
        HasRotation = 0x02,
        HasScaleShear = 0x04
    }

    public UInt32 Flags = 0;
    public Vector3 Translation = Vector3.Zero;
    public Quaternion Rotation = Quaternion.Identity;
    public Matrix3 ScaleShear = Matrix3.Identity;

    public bool HasTranslation
    {
        get { return ((Flags & (uint)TransformFlags.HasTranslation) != 0); }
    }

    public bool HasRotation
    {
        get { return ((Flags & (uint)TransformFlags.HasRotation) != 0); }
    }

    public bool HasScaleShear
    {
        get { return ((Flags & (uint)TransformFlags.HasScaleShear) != 0); }
    }

    public void SetTranslation(Vector3 translation)
    {
        if (translation.Length > 0.0001f)
        {
            Translation = translation;
            Flags |= (uint)TransformFlags.HasTranslation;
        }
        else
        {
            Translation = Vector3.Zero;
            Flags &= ~(uint)TransformFlags.HasTranslation;
        }
    }

    public void SetRotation(Quaternion rotation)
    {
        if (rotation.Length > 0.0001f
            && (Math.Abs(rotation.X) >= 0.001f
            || Math.Abs(rotation.Y) >= 0.001f
            || Math.Abs(rotation.Z) >= 0.001f))
        {
            Rotation = rotation;
            Flags |= (uint)TransformFlags.HasRotation;
        }
        else
        {
            Rotation = Quaternion.Identity;
            Flags &= ~(uint)TransformFlags.HasRotation;
        }
    }

    public void SetScale(Vector3 scale)
    {
        ScaleShear = Matrix3.Identity;
        if ((scale - Vector3.One).Length > 0.0001f)
        {
            ScaleShear[0, 0] = scale[0];
            ScaleShear[1, 1] = scale[1];
            ScaleShear[2, 2] = scale[2];
            Flags |= (uint)TransformFlags.HasScaleShear;
        }
        else
        {
            Flags &= ~(uint)TransformFlags.HasScaleShear;
        }
    }

    public void SetScaleShear(Matrix3 scaleShear)
    {
        if ((scaleShear.Diagonal - Vector3.One).Length > 0.0001f)
        {
            ScaleShear = scaleShear;
            Flags |= (uint)TransformFlags.HasScaleShear;
        }
        else
        {
            Flags &= ~(uint)TransformFlags.HasScaleShear;
        }
    }

    public static Transform FromMatrix4(Matrix4 mat)
    {
        var transform = new Transform();
        transform.SetTranslation(mat.ExtractTranslation());
        transform.SetRotation(mat.ExtractRotation());
        transform.SetScale(mat.ExtractScale());
        return transform;
    }

    public Matrix4 ToMatrix4Composite()
    {
        Matrix3 transform3 = Matrix3.CreateFromQuaternion(Rotation);

        if (HasScaleShear)
        {
            transform3 = ScaleShear * transform3;
        }

        Matrix4 transform = Matrix4.Identity;
        transform[0, 0] = transform3[0, 0];
        transform[0, 1] = transform3[0, 1];
        transform[0, 2] = transform3[0, 2];
        transform[1, 0] = transform3[1, 0];
        transform[1, 1] = transform3[1, 1];
        transform[1, 2] = transform3[1, 2];
        transform[2, 0] = transform3[2, 0];
        transform[2, 1] = transform3[2, 1];
        transform[2, 2] = transform3[2, 2];

        transform[3, 0] = Translation[0];
        transform[3, 1] = Translation[1];
        transform[3, 2] = Translation[2];

        return transform;
    }

    public Matrix4 ToMatrix4()
    {
        Matrix4 transform = Matrix4.Identity;
        if (HasTranslation)
        {
            transform = Matrix4.CreateTranslation(Translation);
        }

        if (HasRotation)
        {
            transform = Matrix4.CreateFromQuaternion(Rotation) * transform;
        }

        if (HasScaleShear)
        {
            Matrix4 scaleShear = Matrix4.Identity;
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                    scaleShear[i, j] = ScaleShear[i, j];
            }

            transform = scaleShear * transform;
        }
        
        return transform;
    }

    public override string ToString()
    {
        return $"Rotation: ({Rotation.X}, {Rotation.Y}, {Rotation.Z}); Translation: ({Translation.X}, {Translation.Y}, {Translation.Z}); Scale: ({ScaleShear[0, 0]}, {ScaleShear[1, 1]}, {ScaleShear[2, 2]})";
    }
}

/// <summary>
/// All Granny files start with this magic structure that defines endianness, bitness and header format.
/// </summary>
public class Magic
{
    /// <summary>
    /// Magic value used for version 7 little-endian 32-bit Granny files
    /// </summary>
    private static readonly byte[] LittleEndian32Magic = [0x29, 0xDE, 0x6C, 0xC0, 0xBA, 0xA4, 0x53, 0x2B, 0x25, 0xF5, 0xB7, 0xA5, 0xF6, 0x66, 0xE2, 0xEE];

    /// <summary>
    /// Magic value used for version 7 little-endian 32-bit Granny files
    /// </summary>
    private static readonly byte[] LittleEndian32Magic2 = [0x29, 0x75, 0x31, 0x82, 0xBA, 0x02, 0x11, 0x77, 0x25, 0x3A, 0x60, 0x2F, 0xF6, 0x6A, 0x8C, 0x2E];
    
    /// <summary>
    /// Magic value used for version 6 little-endian 32-bit Granny files
    /// </summary>
    private static readonly byte[] LittleEndian32MagicV6 = [0xB8, 0x67, 0xB0, 0xCA, 0xF8, 0x6D, 0xB1, 0x0F, 0x84, 0x72, 0x8C, 0x7E, 0x5E, 0x19, 0x00, 0x1E];

    /// <summary>
    /// Magic value used for version 7 big-endian 32-bit Granny files
    /// </summary>
    private static readonly byte[] BigEndian32Magic = [0x0E, 0x11, 0x95, 0xB5, 0x6A, 0xA5, 0xB5, 0x4B, 0xEB, 0x28, 0x28, 0x50, 0x25, 0x78, 0xB3, 0x04];

    /// <summary>
    /// Magic value used for version 7 big-endian 32-bit Granny files
    /// </summary>
    private static readonly byte[] BigEndian32Magic2 = [0x0E, 0x74, 0xA2, 0x0A, 0x6A, 0xEB, 0xEB, 0x64, 0xEB, 0x4E, 0x1E, 0xAB, 0x25, 0x91, 0xDB, 0x8F];

    /// <summary>
    /// Magic value used for version 7 little-endian 64-bit Granny files
    /// </summary>
    private static readonly byte[] LittleEndian64Magic = [0xE5, 0x9B, 0x49, 0x5E, 0x6F, 0x63, 0x1F, 0x14, 0x1E, 0x13, 0xEB, 0xA9, 0x90, 0xBE, 0xED, 0xC4];

    /// <summary>
    /// Magic value used for version 7 little-endian 64-bit Granny files
    /// </summary>
    private static readonly byte[] LittleEndian64Magic2 = [0xE5, 0x2F, 0x4A, 0xE1, 0x6F, 0xC2, 0x8A, 0xEE, 0x1E, 0xD2, 0xB4, 0x4C, 0x90, 0xD7, 0x55, 0xAF];

    /// <summary>
    /// Magic value used for version 7 big-endian 64-bit Granny files
    /// </summary>
    private static readonly byte[] BigEndian64Magic = [0x31, 0x95, 0xD4, 0xE3, 0x20, 0xDC, 0x4F, 0x62, 0xCC, 0x36, 0xD0, 0x3A, 0xB1, 0x82, 0xFF, 0x89];

    /// <summary>
    /// Magic value used for version 7 big-endian 64-bit Granny files
    /// </summary>
    private static readonly byte[] BigEndian64Magic2 = [0x31, 0xC2, 0x4E, 0x7C, 0x20, 0x40, 0xA3, 0x25, 0xCC, 0xE1, 0xC2, 0x7A, 0xB1, 0x32, 0x49, 0xF3];

    /// <summary>
    /// Size of magic value structure, in bytes
    /// </summary>
    public const UInt32 MagicSize = 0x20;

    /// <summary>
    /// Defines endianness and address size
    /// </summary>
    public enum Format
    {
        LittleEndian32,
        BigEndian32,
        LittleEndian64,
        BigEndian64
    };

    /// <summary>
    /// Indicates the 32-bitness of the GR2 file.
    /// </summary>
    public bool Is32Bit
    {
        get { return format == Format.LittleEndian32 || format == Format.BigEndian32; }
    }

    /// <summary>
    /// Indicates the 64-bitness of the GR2 file.
    /// </summary>
    public bool Is64Bit
    {
        get { return format == Format.LittleEndian64 || format == Format.BigEndian64; }
    }

    /// <summary>
    /// Indicates the endianness of the GR2 file.
    /// </summary>
    public bool IsLittleEndian
    {
        get { return format == Format.LittleEndian32 || format == Format.LittleEndian64; }
    }

    /// <summary>
    /// 16-byte long file signature, one of the *Magic constants.
    /// </summary>
    public byte[] signature;
    /// <summary>
    /// Size of file header; offset of the start of section data
    /// </summary>
    public UInt32 headersSize;
    /// <summary>
    /// Header format (0 = uncompressed, 1-2 = Oodle0/1 ?)
    /// </summary>
    public UInt32 headerFormat;
    /// <summary>
    /// Reserved field
    /// </summary>
    public UInt32 reserved1;
    /// <summary>
    /// Reserved field
    /// </summary>
    public UInt32 reserved2;

    /// <summary>
    /// Endianness and address size of the file (derived from the signature)
    /// </summary>
    public Format format;

    public static Format FormatFromSignature(byte[] sig)
    {
        if (sig.SequenceEqual(LittleEndian32Magic) || sig.SequenceEqual(LittleEndian32Magic2) || sig.SequenceEqual(LittleEndian32MagicV6))
            return Format.LittleEndian32;

        if (sig.SequenceEqual(BigEndian32Magic) || sig.SequenceEqual(BigEndian32Magic2))
            return Format.BigEndian32;

        if (sig.SequenceEqual(LittleEndian64Magic) || sig.SequenceEqual(LittleEndian64Magic2))
            return Format.LittleEndian64;

        if (sig.SequenceEqual(BigEndian64Magic) || sig.SequenceEqual(BigEndian64Magic2))
            return Format.BigEndian64;

        throw new ParsingException("Incorrect header signature (maybe not a Granny .GR2 file?)");
    }

    public static byte[] SignatureFromFormat(Format format)
    {
        return format switch
        {
            Format.LittleEndian32 => LittleEndian32Magic,
            Format.LittleEndian64 => LittleEndian64Magic,
            Format.BigEndian32 => BigEndian32Magic,
            Format.BigEndian64 => BigEndian64Magic,
            _ => throw new ArgumentException(),
        };
    }

    public void SetFormat(Format format, bool alternateSignature)
    {
        this.format = format;

        if (alternateSignature)
        {
            this.signature = format switch
            {
                Format.LittleEndian32 => LittleEndian32Magic2,
                Format.LittleEndian64 => LittleEndian64Magic2,
                Format.BigEndian32 => BigEndian32Magic2,
                Format.BigEndian64 => BigEndian64Magic2,
                _ => throw new InvalidDataException("Invalid GR2 signature")
            };
        }
        else
        {
            this.signature = format switch
            {
                Format.LittleEndian32 => LittleEndian32Magic,
                Format.LittleEndian64 => LittleEndian64Magic,
                Format.BigEndian32 => BigEndian32Magic,
                Format.BigEndian64 => BigEndian64Magic,
                _ => throw new InvalidDataException("Invalid GR2 signature")
            };
        }
    }
}

public class Header
{
    /// <summary>
    /// Default GR2 tag used for serialization (D:OS)
    /// </summary>
    public const UInt32 DefaultTag = 0x80000037;

    /// <summary>
    /// D:OS vanilla version tag
    /// </summary>
    public const UInt32 Tag_DOS = 0x80000037;

    /// <summary>
    /// D:OS EE version tag
    /// </summary>
    public const UInt32 Tag_DOSEE = 0x80000039;

    /// <summary>
    /// D:OS:2 DE LSM version tag
    /// </summary>
    public const UInt32 Tag_DOS2DE = 0xE57F0039;

    /// <summary>
    /// Granny file format we support for writing (currently only version 7)
    /// </summary>
    public const UInt32 Version = 7;
    /// <summary>
    /// Size of header structure for V6 headers, in bytes
    /// </summary>
    public const UInt32 HeaderSize_V6 = 0x38;
    /// <summary>
    /// Size of header structure for V7 headers, in bytes
    /// </summary>
    public const UInt32 HeaderSize_V7 = 0x48;
    /// <summary>
    /// Number of user-defined tags in the header
    /// </summary>
    public const UInt32 ExtraTagCount = 4;

    /// <summary>
    /// File format version; should be Header.Version
    /// </summary>
    public UInt32 version;
    /// <summary>
    /// Total size of .GR2 file, including headers
    /// </summary>
    public UInt32 fileSize;
    /// <summary>
    /// CRC-32 hash of the data starting after the header (offset = HeaderSize) to the end of the file (Header.fileSize - HeaderSize bytes)
    /// </summary>
    public UInt32 crc;
    /// <summary>
    /// Offset of the section list relative to the beginning of the file
    /// </summary>
    public UInt32 sectionsOffset;
    /// <summary>
    /// Number of Sections in the .GR2 file
    /// </summary>
    public UInt32 numSections;
    /// <summary>
    /// Reference to the type descriptor of the root element in the hierarchy
    /// </summary>
    public SectionReference rootType;
    public SectionReference rootNode;
    /// <summary>
    /// File format version tag
    /// </summary>
    public UInt32 tag;
    /// <summary>
    /// Extra application-defined tags
    /// </summary>
    public UInt32[] extraTags;
    /// <summary>
    /// CRC of string table; seems to be unused?
    /// </summary>
    public UInt32 stringTableCrc;
    public UInt32 reserved1;
    public UInt32 reserved2;
    public UInt32 reserved3;

    public UInt32 Size()
    {
        var headerSize = version switch
        {
            6 => HeaderSize_V6,
            7 => HeaderSize_V7,
            _ => throw new InvalidDataException("Cannot calculate CRC for unknown header versions."),
        };
        return headerSize;
    }

    public UInt32 CalculateCRC(Stream stream)
    {
        var originalPos = stream.Position;
        var totalHeaderSize = Size() + Magic.MagicSize;
        stream.Seek(totalHeaderSize, SeekOrigin.Begin);
        byte[] body = new byte[fileSize - totalHeaderSize];
        stream.Read(body, 0, (int)(fileSize - totalHeaderSize));
        UInt32 crc = Crc32.HashToUInt32(body);
        stream.Seek(originalPos, SeekOrigin.Begin);
        return crc;
    }
};

public enum SectionType : uint
{
    Main = 0,
    TrackGroup = 1,
    Skeleton = 2,
    Mesh = 3,
    StructDefinitions = 4,
    FirstVertexData = 5,
    Invalid = 0xffffffff
};

public class SectionHeader
{
    /// <summary>
    /// Type of compression used; 0 = no compression; 1-2 = Oodle 1/2 compression
    /// </summary>
    public UInt32 compression;
    /// <summary>
    /// Absolute position of the section data in the GR2 file
    /// </summary>
    public UInt32 offsetInFile;
    /// <summary>
    /// Uncompressed size of section data
    /// </summary>
    public UInt32 compressedSize;
    /// <summary>
    /// Compressed size of section data
    /// </summary>
    public UInt32 uncompressedSize;
    public UInt32 alignment;
    /// <summary>
    /// Oodle1 compressor stops
    /// </summary>
    public UInt32 first16bit;
    public UInt32 first8bit;
    /// <summary>
    /// Absolute position of the relocation data in the GR2 file
    /// </summary>
    public UInt32 relocationsOffset;
    /// <summary>
    /// Number of relocations for this section
    /// </summary>
    public UInt32 numRelocations;
    /// <summary>
    /// Absolute position of the mixed-endianness marshalling data in the GR2 file
    /// </summary>
    public UInt32 mixedMarshallingDataOffset;
    /// <summary>
    /// Number of mixed-marshalling entries for this section
    /// </summary>
    public UInt32 numMixedMarshallingData;
};

public class Section
{
    public SectionHeader Header;
}

public enum MemberType : uint
{
    None = 0,
    Inline = 1,
    Reference = 2,
    ReferenceToArray = 3,
    ArrayOfReferences = 4,
    VariantReference = 5,
    ReferenceToVariantArray = 7,
    String = 8,
    Transform = 9,
    Real32 = 10,
    Int8 = 11,
    UInt8 = 12,
    BinormalInt8 = 13,
    NormalUInt8 = 14,
    Int16 = 15,
    UInt16 = 16,
    BinormalInt16 = 17,
    NormalUInt16 = 18,
    Int32 = 19,
    UInt32 = 20,
    Real16 = 21,
    EmptyReference = 22,
    Max = EmptyReference,
    Invalid = 0xffffffff
};

/// <summary>
/// Reference to an absolute address within the GR2 file
/// </summary>
public class SectionReference
{
    /// <summary>
    /// Zero-based index of referenced section (0 .. Header.numSections - 1)
    /// </summary>
    public UInt32 Section = (UInt32)SectionType.Invalid;

    /// <summary>
    /// Offset in bytes from the beginning of the section
    /// </summary>
    public UInt32 Offset = 0;

    /// <summary>
    /// Returns if the reference points to a valid address within the file
    /// </summary>
    public bool IsValid
    {
        get { return Section != (UInt32)SectionType.Invalid; }
    }

    public SectionReference()
    {
    }

    public SectionReference(SectionType section, UInt32 offset)
    {
        Section = (UInt32)section;
        Offset = offset;
    }

    public override bool Equals(object o)
    {
        if (o == null)
            return false;

        return o is SectionReference reference && reference.Section == Section && reference.Offset == Offset;
    }

    public bool Equals(SectionReference reference)
    {
        return reference != null && reference.Section == Section && reference.Offset == Offset;
    }

    public override int GetHashCode()
    {
        return (int)Section * 31 + (int)Offset * 31 * 23;
    }
}

/// <summary>
/// A reference whose final section and offset is stored in the relocation map
/// </summary>
public class RelocatableReference
{
    /// <summary>
    /// Offset in bytes from the beginning of the section
    /// </summary>
    public UInt64 Offset = 0;

    /// <summary>
    /// Returns if the reference points to a valid address within the file
    /// </summary>
    public bool IsValid
    {
        get { return Offset != 0; }
    }

    public override bool Equals(object o)
    {
        if (o == null)
            return false;

        return o is RelocatableReference reference && reference.Offset == Offset;
    }

    public bool Equals(RelocatableReference reference)
    {
        return reference != null && reference.Offset == Offset;
    }

    public override int GetHashCode()
    {
        return (int)Offset;
    }
}

/// <summary>
/// Absolute reference to a structure type definition within the GR2 file
/// </summary>
public class StructReference : RelocatableReference
{
    // Cached type value for this reference
    public StructDefinition Type;

    public StructDefinition Resolve(GR2Reader gr2)
    {
        Debug.Assert(IsValid);
        // Type definitions use a 2-level cache
        // First we'll check the reference itself, if it has a cached ref to the resolved type
        // If it has, we have nothing to do

        // If the struct wasn't resolved yet, try the type definition cache
        // When a type definition is read from the GR2 file, it is stored here using its definition address as a key
        if (Type == null)
            gr2.Types.TryGetValue(this, out Type);

        if (Type == null)
        {
            // We haven't seen this type before, read its definition from the file and cache it
#if DEBUG_GR2_SERIALIZATION
            Debug.WriteLine(String.Format(" ===== Struct definition at {0:X8} ===== ", Offset));
#endif
            var originalPos = gr2.Stream.Position;
            gr2.Seek(this);
            Type = gr2.ReadStructDefinition();
            gr2.Stream.Seek(originalPos, SeekOrigin.Begin);
            gr2.Types[this] = Type;
        }

        return Type;
    }

    //
    //
    // TODO: REWORK --- Move Read(reader), Resolve(reader), PreSave(writer), Save(writer) here!
    //
    //
}

/// <summary>
/// Absolute reference to a null-terminated string value within the GR2 file
/// </summary>
public class StringReference : RelocatableReference
{
    // Cached string value for this reference
    public string Value;

    public string Resolve(GR2Reader gr2)
    {
        Debug.Assert(IsValid);
        // Don't use a global string cache here, as string constants are rarely referenced twice,
        // unlike struct definitions
        if (Value == null)
        {
            var originalPos = gr2.Stream.Position;
            gr2.Seek(this);
            Value = gr2.ReadString();
            gr2.Stream.Seek(originalPos, SeekOrigin.Begin);
        }

        return Value;
    }
}

/// <summary>
/// Absolute reference to an array of something (either indirect index references or structs)
/// </summary>
public class ArrayReference : RelocatableReference
{
    /// <summary>
    /// Number of items in this array
    /// </summary>
    public UInt32 Size;
}

/// <summary>
/// Absolute reference to an array of references
/// </summary>
public class ArrayIndicesReference : ArrayReference
{
    // Cached ref list for this reference
    public List<RelocatableReference> Items;

    public List<RelocatableReference> Resolve(GR2Reader gr2)
    {
        Debug.Assert(IsValid);
        if (Items == null)
        {
#if DEBUG_GR2_SERIALIZATION
            Debug.WriteLine(String.Format("    (Reference list at {0:X8})", Offset));
#endif
            var originalPos = gr2.Stream.Position;
            gr2.Seek(this);
            Items = [];
            for (int i = 0; i < Size; i++)
            {
                Items.Add(gr2.ReadReference());
#if DEBUG_GR2_SERIALIZATION
                Debug.WriteLine(String.Format("        {0:X8}", r.Offset));
#endif
            }
            gr2.Stream.Seek(originalPos, SeekOrigin.Begin);
        }

        return Items;
    }
}

public class MemberDefinition
{
    public const UInt32 ExtraTagCount = 3;

    public MemberType Type = MemberType.Invalid;
    public string Name;
    public string GrannyName;
    public StructReference Definition;
    public UInt32 ArraySize;
    /// <summary>
    /// Extra application-defined data
    /// </summary>
    public UInt32[] Extra;
    public UInt32 Unknown;

    // We need to keep a separate cached flag, as we can cache null fields as well
    public bool HasCachedField = false;
    public System.Reflection.FieldInfo CachedField;

    public NodeSerializer Serializer;
    public VariantTypeSelector TypeSelector;
    public SectionSelector SectionSelector;
    public SerializationKind SerializationKind = SerializationKind.Builtin;
    // Only available when writing a GR2 file!
    public StructDefinition WriteDefinition;
    public SectionType PreferredSection = SectionType.Invalid;
    /// <summary>
    /// Should we save this member to the data area?
    /// </summary>
    public bool DataArea = false;
    /// <summary>
    /// The Granny type we should save when serializing this field
    /// (Mainly used to provide a type definition for user-defined serializers)
    /// </summary>
    public Type Prototype;
    /// <summary>
    /// Minimum GR2 file version this member should be exported to
    /// </summary>
    public UInt32 MinVersion = 0;
    /// <summary>
    /// Maximum GR2 file version this member should be exported to
    /// </summary>
    public UInt32 MaxVersion = 0;

    public bool IsValid
    {
        get { return Type != (UInt32)MemberType.None; }
    }

    public bool IsScalar
    {
        get { return Type > MemberType.ReferenceToVariantArray; }
    }

    public UInt32 Size(GR2Reader gr2)
    {
        return Type switch
        {
            MemberType.Inline => Definition.Resolve(gr2).Size(gr2),

            MemberType.Int8 => 1,
            MemberType.BinormalInt8 => 1,
            MemberType.UInt8 => 1,
            MemberType.NormalUInt8 => 1,

            MemberType.Int16 => 2,
            MemberType.BinormalInt16 => 2,
            MemberType.UInt16 => 2,
            MemberType.NormalUInt16 => 2,
            MemberType.Real16 => 2,

            MemberType.Reference => gr2.Magic.Is32Bit ? 4u : 8,

            MemberType.String => 4,
            MemberType.Real32 => 4,
            MemberType.Int32 => 4,
            MemberType.UInt32 => 4,

            MemberType.VariantReference => gr2.Magic.Is32Bit ? 8u : 16,
            MemberType.ArrayOfReferences => gr2.Magic.Is32Bit ? 8u : 12,
            MemberType.ReferenceToArray => gr2.Magic.Is32Bit ? 8u : 12,
            MemberType.ReferenceToVariantArray => gr2.Magic.Is32Bit ? 12u : 20,

            MemberType.Transform => 17 * 4,

            _ => throw new ParsingException($"Unhandled member type: {Type}")
        };
    }

    public UInt32 MarshallingSize()
    {
        switch (Type)
        {
            case MemberType.Inline:
            case MemberType.Reference:
            case MemberType.VariantReference:
            case MemberType.EmptyReference:
                return 0;

            case MemberType.Int8:
            case MemberType.BinormalInt8:
            case MemberType.UInt8:
            case MemberType.NormalUInt8:
                return 1;

            case MemberType.Int16:
            case MemberType.BinormalInt16:
            case MemberType.UInt16:
            case MemberType.NormalUInt16:
            case MemberType.Real16:
                return 2;

            case MemberType.String:
            case MemberType.Transform:
            case MemberType.Real32:
            case MemberType.Int32:
            case MemberType.UInt32:
            case MemberType.ReferenceToArray:
            case MemberType.ArrayOfReferences:
            case MemberType.ReferenceToVariantArray:
                return 4;

            default:
                throw new ParsingException(String.Format("Unhandled member type: {0}", Type.ToString()));
        }
    }

    public bool ShouldSerialize(UInt32 version)
    {
        return ((MinVersion == 0 || MinVersion <= version) &&
            (MaxVersion == 0 || MaxVersion >= version));
    }

    private void LoadAttributes(FieldInfo info, GR2Writer writer)
    {
        var attrs = info.GetCustomAttributes(typeof(SerializationAttribute), true);
        if (attrs.Length > 0)
        {
            SerializationAttribute serialization = attrs[0] as SerializationAttribute;

            if (serialization.Section != SectionType.Invalid)
                PreferredSection = serialization.Section;

            DataArea = serialization.DataArea;

            if (serialization.Type != MemberType.Invalid)
                Type = serialization.Type;

            if (serialization.TypeSelector != null)
                TypeSelector = Activator.CreateInstance(serialization.TypeSelector) as VariantTypeSelector;

            if (serialization.SectionSelector != null)
                SectionSelector = Activator.CreateInstance(serialization.SectionSelector) as SectionSelector;

            if (serialization.Serializer != null)
                Serializer = Activator.CreateInstance(serialization.Serializer) as NodeSerializer;

            if (writer != null && serialization.Prototype != null)
                WriteDefinition = writer.LookupStructDefinition(serialization.Prototype, serialization.Prototype);

            if (serialization.Name != null)
                GrannyName = serialization.Name;

            Prototype = serialization.Prototype;
            SerializationKind = serialization.Kind;
            ArraySize = serialization.ArraySize;
            MinVersion = serialization.MinVersion;
            MaxVersion = serialization.MaxVersion;
        }
    }

    public System.Reflection.FieldInfo LookupFieldInfo(object instance)
    {
        if (HasCachedField)
            return CachedField;

        var field = instance.GetType().GetField(Name);
        AssignFieldInfo(field);
        return field;
    }

    public void AssignFieldInfo(FieldInfo field)
    {
        Debug.Assert(!HasCachedField);
        CachedField = field;
        HasCachedField = true;

        if (field != null)
            LoadAttributes(field, null);
    }

    public static MemberDefinition CreateFromFieldInfo(FieldInfo info, GR2Writer writer)
    {
        var member = new MemberDefinition();
        var type = info.FieldType;
        member.Name = info.Name;
        member.GrannyName = info.Name;
        member.Extra = [0, 0, 0];
        member.CachedField = info;
        member.HasCachedField = true;

        member.LoadAttributes(info, writer);

        if (type.IsArray && member.SerializationKind != SerializationKind.None)
        {
            if (member.ArraySize == 0)
                throw new InvalidOperationException("SerializationAttribute.ArraySize must be set for fixed size arrays");
            type = type.GetElementType();
        }

        if (member.Type == MemberType.Invalid)
        {
            if (type == typeof(SByte))
                member.Type = MemberType.Int8;
            else if (type == typeof(Byte))
                member.Type = MemberType.UInt8;
            else if (type == typeof(Int16))
                member.Type = MemberType.Int16;
            else if (type == typeof(UInt16))
                member.Type = MemberType.UInt16;
            else if (type == typeof(Int32))
                member.Type = MemberType.Int32;
            else if (type == typeof(UInt32))
                member.Type = MemberType.UInt32;
            else if (type == typeof(OpenTK.Mathematics.Half))
                member.Type = MemberType.Real16;
            else if (type == typeof(Single))
                member.Type = MemberType.Real32;
            else if (type == typeof(string))
                member.Type = MemberType.String;
            else if (type == typeof(Transform))
                member.Type = MemberType.Transform;
            else if (type == typeof(object) || type.IsAbstract || type.IsInterface)
                member.Type = MemberType.VariantReference;
            else if (type.GetInterfaces().Contains(typeof(IList<object>)))
                member.Type = MemberType.ReferenceToVariantArray;
            else if (type.GetInterfaces().Contains(typeof(System.Collections.IList)))
                member.Type = MemberType.ReferenceToArray; // or ArrayOfReferences?
            else
                member.Type = MemberType.Reference; // or Inline?
        }

        if (member.SerializationKind != SerializationKind.None && member.WriteDefinition == null && writer != null)
        {
            if (member.Type == MemberType.Inline || member.Type == MemberType.Reference)
            {
                member.WriteDefinition = writer.LookupStructDefinition(type, null);
            }
            else if (member.Type == MemberType.ReferenceToArray || member.Type == MemberType.ArrayOfReferences)
            {
                member.WriteDefinition = writer.LookupStructDefinition(type.GetGenericArguments().Single(), null);
            }
        }


        return member;
    }
}

public class StructDefinition
{
    public Type Type;
    public List<MemberDefinition> Members = new List<MemberDefinition>();
    /// <summary>
    /// Should we do mixed marshalling on this struct?
    /// </summary>
    public bool MixedMarshal = false;

    public UInt32 Size(GR2Reader gr2)
    {
        UInt32 size = 0;
        foreach (var member in Members)
            size += member.Size(gr2);
        return size;
    }

    public void MapType(object instance)
    {
        if (Type == null)
        {
            Type = instance.GetType();
            foreach (var field in Type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var name = field.Name;
                var attrs = field.GetCustomAttributes(typeof(SerializationAttribute), true);
                if (attrs.Length > 0)
                {
                    SerializationAttribute serialization = attrs[0] as SerializationAttribute;
                    if (serialization.Name != null)
                    {
                        name = serialization.Name;
                    }
                }

                foreach (var member in Members)
                {
                    if (member.Name == name)
                    {
                        member.AssignFieldInfo(field);
                    }
                }
            }
        }

        // If this assertion is triggered it most likely is because multiple C# types
        // were assigned to the same Granny type in different places in the class definitions
        Debug.Assert(Type == instance.GetType());
    }

    public void LoadFromType(Type type, GR2Writer writer)
    {
        Type = type;

        var attrs = type.GetCustomAttributes(typeof(StructSerializationAttribute), true);
        if (attrs.Length > 0)
        {
            StructSerializationAttribute serialization = attrs[0] as StructSerializationAttribute;
            MixedMarshal = serialization.MixedMarshal;
        }

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            var member = MemberDefinition.CreateFromFieldInfo(field, writer);
            if (member.SerializationKind != SerializationKind.None)
                Members.Add(member);
        }
    }
}

/// <summary>
/// Determines the way a structure field is serialized.
/// </summary>
public enum SerializationKind
{
    /// <summary>
    /// Don't serialize this field
    /// (The parser may still read this field internally, but it will not set the relevant struct field)
    /// </summary>
    None,
    /// <summary>
    /// Do serialization via the builtin GR2 parser (this is the default)
    /// </summary>
    Builtin,
    /// <summary>
    /// Serialize raw Granny data via the user-defined serializer class.
    /// (This is almost the same as overriding Reader.ReadInstance(); the serializer doesn't create the
    /// struct, won't process relocations/references, etc. You're on your own.)
    /// </summary>
    UserRaw,
    /// <summary>
    /// Serialize the struct once per member field via the user-defined serializer class.
    ///  - For primitive/inline types this is the same as UserRaw.
    ///  - For [Variant]Reference, the parser will process the relocation automatically
    ///  - For arrays and RefTo*Arrays, the first relocation is processed automatically, the array itself
    ///    (and item references for Ref types) should be processed by the user-defined serializer.
    /// </summary>
    UserMember,
    /// <summary>
    /// Serialize the struct once for each array element in the member field via the user-defined serializer class.
    ///  - For primitive/inline types this is the same as UserRaw.
    ///  - For [Variant]Reference, the parser will process the relocation automatically and the serializer is called once
    ///  - For arrays and RefTo*Arrays, all relocations are processed automatically and the serializer is
    ///    called once for each array element.
    /// </summary>
    UserElement
};

public interface NodeSerializer
{
    object Read(GR2Reader reader, StructDefinition definition, MemberDefinition member, uint arraySize, object parent);
    void Write(GR2Writer writer, WritableSection section, MemberDefinition member, object obj);
}

public interface VariantTypeSelector
{
    Type SelectType(MemberDefinition member, object node);
    Type SelectType(MemberDefinition member, StructDefinition defn, object parent);
}

public interface SectionSelector
{
    SectionType SelectSection(MemberDefinition member, Type type, object obj);
}

public interface StructDefinitionSelector
{
    StructDefinition CreateStructDefinition(object instance);
}

/// <summary>
/// Tells the Granny serializer about the way we want it to write a field to the .GR2 file.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SerializationAttribute : System.Attribute
{
    /// <summary>
    /// Which section should this field be serialized into
    /// </summary>
    public SectionType Section = SectionType.Invalid;
    /// <summary>
    /// Should we save this member to the data area?
    /// </summary>
    public bool DataArea = false;
    /// <summary>
    /// Override Granny member type
    /// </summary>
    public MemberType Type = MemberType.Invalid;
    /// <summary>
    /// Size of static array - this *must* be set for array (ie. float[]) types!
    /// </summary>
    public UInt32 ArraySize;
    /// <summary>
    /// The Granny type we should save when serializing this field
    /// (Mainly used to provide a type definition for user-defined serializers)
    /// </summary>
    public Type Prototype;
    /// <summary>
    /// User-defined section selector class (must implement SectionSelector)
    /// </summary>
    public Type SectionSelector;
    /// <summary>
    /// User-defined type selector class (must implement VariantTypeSelector)
    /// </summary>
    public Type TypeSelector;
    /// <summary>
    /// User-defined serializer class (must implement NodeSerializer)
    /// </summary>
    public Type Serializer;
    /// <summary>
    /// In what way should we serialize this item
    /// </summary>
    public SerializationKind Kind = SerializationKind.Builtin;
    /// <summary>
    /// Member name in the serialized file
    /// </summary>
    public String Name;
    /// <summary>
    /// Minimum GR2 file version this member should be exported to
    /// </summary>
    public UInt32 MinVersion = 0;
    /// <summary>
    /// Maximum GR2 file version this member should be exported to
    /// </summary>
    public UInt32 MaxVersion = 0;
    /// <summary>
    /// Should we do mixed marshalling on this struct?
    /// </summary>
    public bool MixedMarshal = false;
}

/// <summary>
/// Tells the Granny serializer about the way we want it to write a struct to the .GR2 file.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class StructSerializationAttribute : System.Attribute
{
    /// <summary>
    /// Should we do mixed marshalling on this struct?
    /// </summary>
    public bool MixedMarshal = false;

    /// <summary>
    /// User-defined data structure selector class (must implement StructDefinitionSelector)
    /// </summary>
    public Type TypeSelector;
}
