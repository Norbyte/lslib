// #define DEBUG_GR2_SERIALIZATION
// #define DEBUG_GR2_FORMAT_DIFFERENCES

using System.Diagnostics;
using LSLib.Native;

namespace LSLib.Granny.GR2;

public class ParsingException(string message) : Exception(message)
{
}

public class GR2Reader(Stream stream)
{
    internal Stream InputStream = stream;
    internal BinaryReader InputReader;
    internal Stream Stream;
    internal BinaryReader Reader;
    internal Magic Magic;
    internal Header Header;
    internal List<Section> Sections = [];
    internal Dictionary<StructReference, StructDefinition> Types = [];
    private readonly Dictionary<UInt32, object> CachedStructs = [];
#if DEBUG_GR2_SERIALIZATION
    private HashSet<StructReference> DebugPendingResolve = [];
#endif

    public UInt32 Tag
    {
        get { return Header.tag; }
    }

    public void Dispose()
    {
        Stream?.Dispose();
    }

    public void Read(object root)
    {
        using (this.InputReader = new BinaryReader(InputStream))
        {
            Magic = ReadMagic();

            if (Magic.format != Magic.Format.LittleEndian32 && Magic.format != Magic.Format.LittleEndian64)
                throw new ParsingException("Only little-endian GR2 files are supported");

            Header = ReadHeader();
            for (int i = 0; i < Header.numSections; i++)
            {
                var section = new Section
                {
                    Header = ReadSectionHeader()
                };
                Sections.Add(section);
            }

            Debug.Assert(InputStream.Position == Magic.headersSize);

            UncompressStream();

            foreach (var section in Sections)
            {
                ReadSectionRelocations(section);
            }

            if (Magic.IsLittleEndian != BitConverter.IsLittleEndian)
            {
                // TODO: This should be done before applying relocations?
                foreach (var section in Sections)
                {
                    ReadSectionMixedMarshallingRelocations(section);
                }
            }

            var rootStruct = new StructReference
            {
                Offset = Sections[(int)Header.rootType.Section].Header.offsetInFile + Header.rootType.Offset
            };

            Seek(Header.rootNode);
            ReadStruct(rootStruct.Resolve(this), MemberType.Inline, root, null);
        }
    }

    private Magic ReadMagic()
    {
        var magic = new Magic
        {
            signature = InputReader.ReadBytes(16),
            headersSize = InputReader.ReadUInt32(),
            headerFormat = InputReader.ReadUInt32(),
            reserved1 = InputReader.ReadUInt32(),
            reserved2 = InputReader.ReadUInt32()
        };
        magic.format = Magic.FormatFromSignature(magic.signature);

        if (magic.headerFormat != 0)
            throw new ParsingException("Compressed GR2 files are not supported");

        Debug.Assert(magic.reserved1 == 0);
        Debug.Assert(magic.reserved2 == 0);

#if DEBUG_GR2_SERIALIZATION
        Debug.WriteLine(" ===== GR2 Magic ===== ");
        Debug.WriteLine(String.Format("Format: {0}", magic.format));
        Debug.WriteLine(String.Format("Headers size: {0:X8}, format: ", magic.headersSize, magic.headerFormat));
        Debug.WriteLine(String.Format("Reserved1-2: {0:X8} {1:X8}", magic.reserved1, magic.reserved2));
#endif
        return magic;
    }

    private Header ReadHeader()
    {
        var header = new Header
        {
            version = InputReader.ReadUInt32(),
            fileSize = InputReader.ReadUInt32(),
            crc = InputReader.ReadUInt32(),
            sectionsOffset = InputReader.ReadUInt32(),
            numSections = InputReader.ReadUInt32(),
            rootType = ReadSectionReferenceUnchecked(),
            rootNode = ReadSectionReferenceUnchecked(),
            tag = InputReader.ReadUInt32(),
            extraTags = new UInt32[Header.ExtraTagCount]
        };
        for (int i = 0; i < Header.ExtraTagCount; i++)
            header.extraTags[i] = InputReader.ReadUInt32();

        if (header.version >= 7)
        {
            header.stringTableCrc = InputReader.ReadUInt32();
            header.reserved1 = InputReader.ReadUInt32();
            header.reserved2 = InputReader.ReadUInt32();
            header.reserved3 = InputReader.ReadUInt32();
        }

        if (header.version < 6 || header.version > 7)
            throw new ParsingException(String.Format("Unsupported GR2 version; file is version {0}, supported versions are 6 and 7", header.version));

        // if (header.tag != Header.Tag)
        //    throw new ParsingException(String.Format("Incorrect header tag; expected {0:X8}, got {1:X8}", Header.Tag, header.tag));

        Debug.Assert(header.fileSize <= InputStream.Length);
        Debug.Assert(header.CalculateCRC(InputStream) == header.crc);
        Debug.Assert(header.sectionsOffset == header.Size());
        Debug.Assert(header.rootType.Section < header.numSections);
        // TODO: check rootTypeOffset after serialization
        Debug.Assert(header.stringTableCrc == 0);
        Debug.Assert(header.reserved1 == 0);
        Debug.Assert(header.reserved2 == 0);
        Debug.Assert(header.reserved3 == 0);

#if DEBUG_GR2_SERIALIZATION
        Debug.WriteLine(" ===== GR2 Header ===== ");
        Debug.WriteLine(String.Format("Version {0}, Size {1}, CRC {2:X8}", header.version, header.fileSize, header.crc));
        Debug.WriteLine(String.Format("Offset of sections: {0}, num sections: {1}", header.sectionsOffset, header.numSections));
        Debug.WriteLine(String.Format("Root type section {0}, Root type offset {1:X8}", header.rootType.Section, header.rootType.Offset));
        Debug.WriteLine(String.Format("Root node section {0} {1:X8}", header.rootNode.Section, header.rootNode.Offset));
        Debug.WriteLine(String.Format("Tag: {0:X8}, Strings CRC: {1:X8}", header.tag, header.stringTableCrc));
        Debug.WriteLine(String.Format("Extra tags: {0:X8} {1:X8} {2:X8} {3:X8}", header.extraTags[0], header.extraTags[1], header.extraTags[2], header.extraTags[3]));
        Debug.WriteLine(String.Format("Reserved: {0:X8} {1:X8} {2:X8}", new object[] { header.reserved1, header.reserved2, header.reserved3 }));
#endif

        return header;
    }

    private SectionHeader ReadSectionHeader()
    {
        var header = new SectionHeader
        {
            compression = InputReader.ReadUInt32(),
            offsetInFile = InputReader.ReadUInt32(),
            compressedSize = InputReader.ReadUInt32(),
            uncompressedSize = InputReader.ReadUInt32(),
            alignment = InputReader.ReadUInt32(),
            first16bit = InputReader.ReadUInt32(),
            first8bit = InputReader.ReadUInt32(),
            relocationsOffset = InputReader.ReadUInt32(),
            numRelocations = InputReader.ReadUInt32(),
            mixedMarshallingDataOffset = InputReader.ReadUInt32(),
            numMixedMarshallingData = InputReader.ReadUInt32()
        };

        Debug.Assert(header.offsetInFile <= Header.fileSize);

        if (header.compression != 0)
        {
            Debug.Assert(header.offsetInFile + header.compressedSize <= Header.fileSize);
        }
        else
        {
            Debug.Assert(header.compressedSize == header.uncompressedSize);
            Debug.Assert(header.offsetInFile + header.uncompressedSize <= Header.fileSize);
        }

#if DEBUG_GR2_SERIALIZATION
        Debug.WriteLine(" ===== Section Header ===== ");
        Debug.WriteLine(String.Format("Compression: {0}", header.compression));
        Debug.WriteLine(String.Format("Offset {0:X8} Comp/UncompSize {1:X8}/{2:X8}", header.offsetInFile, header.compressedSize, header.uncompressedSize));
        Debug.WriteLine(String.Format("Alignment {0}", header.alignment));
        Debug.WriteLine(String.Format("First 16/8bit: {0:X8}/{1:X8}", header.first16bit, header.first8bit));
        Debug.WriteLine(String.Format("Relocations: {0:X8} count {1}", header.relocationsOffset, header.numRelocations));
        Debug.WriteLine(String.Format("Marshalling data: {0:X8} count {1}", header.mixedMarshallingDataOffset, header.numMixedMarshallingData));
#endif
        return header;
    }

    private void UncompressStream()
    {
#if DEBUG_GR2_SERIALIZATION
        Debug.WriteLine(String.Format(" ===== Repacking sections ===== "));
#endif

        uint totalSize = 0;
        foreach (var section in Sections)
        {
            totalSize += section.Header.uncompressedSize;
        }

        // Copy the whole file, as we'll update its contents because of relocations and marshalling fixups
        byte[] uncompressedStream = new byte[totalSize];
        this.Stream = new MemoryStream(uncompressedStream);
        this.Reader = new BinaryReader(this.Stream);

        for (int i = 0; i < Sections.Count; i++)
        {
            var section = Sections[i];
            var hdr = section.Header;
            byte[] sectionContents = new byte[hdr.compressedSize];
            InputStream.Position = hdr.offsetInFile;
            InputStream.Read(sectionContents, 0, (int)hdr.compressedSize);

            var originalOffset = hdr.offsetInFile;
            hdr.offsetInFile = (uint)Stream.Position;
            if (section.Header.compression == 0)
            {
                Stream.Write(sectionContents, 0, sectionContents.Length);
            }
            else if (section.Header.uncompressedSize > 0)
            {
                if (hdr.compression == 4)
                {
                    var uncompressed = Granny2Compressor.Decompress4(
                        sectionContents, (int)hdr.uncompressedSize);
                    Stream.Write(uncompressed, 0, uncompressed.Length);
                }
                else
                {
                    var uncompressed = Granny2Compressor.Decompress(
                        (int)hdr.compression,
                        sectionContents, (int)hdr.uncompressedSize,
                        (int)hdr.first16bit, (int)hdr.first8bit, (int)hdr.uncompressedSize);
                    Stream.Write(uncompressed, 0, uncompressed.Length);
                }
            }

#if DEBUG_GR2_SERIALIZATION
            Debug.WriteLine(String.Format("    {0}: {1:X8} ({2}) --> {3:X8} ({4})", i, originalOffset, hdr.compressedSize, hdr.offsetInFile, hdr.uncompressedSize));
#endif
        }
    }

    private void ReadSectionRelocationsInternal(Section section, Stream relocationsStream)
    {
#if DEBUG_GR2_SERIALIZATION
        Debug.WriteLine(String.Format(" ===== Relocations for section at {0:X8} ===== ", section.Header.offsetInFile));
#endif

        using var relocationsReader = new BinaryReader(relocationsStream, Encoding.Default, true);
        for (int i = 0; i < section.Header.numRelocations; i++)
        {
            UInt32 offsetInSection = relocationsReader.ReadUInt32();
            Debug.Assert(offsetInSection <= section.Header.uncompressedSize);
            var reference = ReadSectionReference(relocationsReader);

            Stream.Position = section.Header.offsetInFile + offsetInSection;
            var fixupAddress = Sections[(int)reference.Section].Header.offsetInFile + reference.Offset;
            Stream.Write(BitConverter.GetBytes(fixupAddress), 0, 4);

#if DEBUG_GR2_SERIALIZATION
            Debug.WriteLine(String.Format("    LOCAL  {0:X8} --> {1}:{2:X8}", offsetInSection, (SectionType)reference.Section, reference.Offset));
            Debug.WriteLine(String.Format("    GLOBAL {0:X8} --> {1:X8}",
                offsetInSection + section.Header.offsetInFile,
                reference.Offset + Sections[(int)reference.Section].Header.offsetInFile));
#endif
        }
    }

    private void ReadSectionRelocations(Section section)
    {
        if (section.Header.numRelocations == 0) return;

        InputStream.Seek(section.Header.relocationsOffset, SeekOrigin.Begin);
        if (section.Header.compression == 4)
        {
            using var reader = new BinaryReader(InputStream, Encoding.Default, true);
            UInt32 compressedSize = reader.ReadUInt32();
            byte[] compressed = reader.ReadBytes((int)compressedSize);
            var uncompressed = Granny2Compressor.Decompress4(
                compressed, (int)(section.Header.numRelocations * 12));
            using var ms = new MemoryStream(uncompressed);
            ReadSectionRelocationsInternal(section, ms);
        }
        else
        {
            ReadSectionRelocationsInternal(section, InputStream);
        }
    }

    private void MixedMarshal(UInt32 count, StructDefinition definition)
    {
        for (var arrayIdx = 0; arrayIdx < count; arrayIdx++)
        {
            foreach (var member in definition.Members)
            {
                var size = member.Size(this);
                if (member.Type == MemberType.Inline)
                {
                    MixedMarshal(member.ArraySize == 0 ? 1 : member.ArraySize, member.Definition.Resolve(this));
                }
                else if (member.MarshallingSize() > 1)
                {
                    var marshalSize = member.MarshallingSize();
                    byte[] data = new byte[size];
                    Stream.Read(data, 0, (int)size);
                    for (var j = 0; j < size / marshalSize; j++)
                    {
                        // Byte swap for 2, 4, 8-byte values
                        for (var off = 0; off < marshalSize / 2; off++)
                        {
                            var tmp = data[j * marshalSize + off];
                            data[j * marshalSize + off] = data[j * marshalSize + marshalSize - 1 - off];
                            data[j * marshalSize + marshalSize - 1 - off] = tmp;
                        }
                    }

                    Stream.Seek(-size, SeekOrigin.Current);
                    Stream.Write(data, 0, (int)size);
                    Stream.Seek(-size, SeekOrigin.Current);
                }

                Stream.Seek(size, SeekOrigin.Current);
            }
        }
    }

    private void ReadSectionMixedMarshallingRelocationsInternal(Section section, Stream relocationsStream)
    {
#if DEBUG_GR2_SERIALIZATION
        Debug.WriteLine(String.Format(" ===== Mixed marshalling relocations for section at {0:X8} ===== ", section.Header.offsetInFile));
#endif

        using var relocationsReader = new BinaryReader(relocationsStream, Encoding.Default, true);
        for (int i = 0; i < section.Header.numMixedMarshallingData; i++)
        {
            UInt32 count = relocationsReader.ReadUInt32();
            UInt32 offsetInSection = relocationsReader.ReadUInt32();
            Debug.Assert(offsetInSection <= section.Header.uncompressedSize);
            var type = ReadSectionReference(relocationsReader);
            var typeDefn = new StructReference
            {
                Offset = Sections[(int)type.Section].Header.offsetInFile + type.Offset
            };

            Seek(section, offsetInSection);
            MixedMarshal(count, typeDefn.Resolve(this));

#if DEBUG_GR2_SERIALIZATION
            Debug.WriteLine(String.Format("    {0:X8} [{1}] --> {2}:{3:X8}", offsetInSection, count, (SectionType)type.Section, type.Offset));
#endif
        }
    }

    private void ReadSectionMixedMarshallingRelocations(Section section)
    {
        if (section.Header.numMixedMarshallingData == 0) return;

        InputStream.Seek(section.Header.mixedMarshallingDataOffset, SeekOrigin.Begin);
        if (section.Header.compression == 4)
        {
            using var reader = new BinaryReader(InputStream, Encoding.Default, true);
            UInt32 compressedSize = reader.ReadUInt32();
            byte[] compressed = reader.ReadBytes((int)compressedSize);
            var uncompressed = Granny2Compressor.Decompress4(
                compressed, (int)(section.Header.numMixedMarshallingData * 16));
            using var ms = new MemoryStream(uncompressed);
            ReadSectionMixedMarshallingRelocationsInternal(section, ms);
        }
        else
        {
            ReadSectionMixedMarshallingRelocationsInternal(section, InputStream);
        }
    }

    public SectionReference ReadSectionReferenceUnchecked(BinaryReader reader)
    {
        return new SectionReference
        {
            Section = reader.ReadUInt32(),
            Offset = reader.ReadUInt32()
        };
    }

    public SectionReference ReadSectionReferenceUnchecked()
    {
        return ReadSectionReferenceUnchecked(InputReader);
    }

    public SectionReference ReadSectionReference(BinaryReader reader)
    {
        var reference = ReadSectionReferenceUnchecked(reader);
        Debug.Assert(reference.Section < Sections.Count);
        Debug.Assert(reference.Offset <= Sections[(int)reference.Section].Header.uncompressedSize);
        return reference;
    }

    public SectionReference ReadSectionReference()
    {
        return ReadSectionReference(InputReader);
    }

    public RelocatableReference ReadReference()
    {
        var reference = new RelocatableReference();
        if (Magic.Is32Bit)
            reference.Offset = Reader.ReadUInt32();
        else
            reference.Offset = Reader.ReadUInt64();
        return reference;
    }

    public StructReference ReadStructReference()
    {
        var reference = new StructReference();
        if (Magic.Is32Bit)
            reference.Offset = Reader.ReadUInt32();
        else
            reference.Offset = Reader.ReadUInt64();
        return reference;
    }

    public StringReference ReadStringReference()
    {
        var reference = new StringReference();
        if (Magic.Is32Bit)
            reference.Offset = Reader.ReadUInt32();
        else
            reference.Offset = Reader.ReadUInt64();
        return reference;
    }

    public ArrayReference ReadArrayReference()
    {
        var reference = new ArrayReference
        {
            Size = Reader.ReadUInt32()
        };
        if (Magic.Is32Bit)
            reference.Offset = Reader.ReadUInt32();
        else
            reference.Offset = Reader.ReadUInt64();
        return reference;
    }

    public ArrayIndicesReference ReadArrayIndicesReference()
    {
        var reference = new ArrayIndicesReference
        {
            Size = Reader.ReadUInt32()
        };
        if (Magic.Is32Bit)
            reference.Offset = Reader.ReadUInt32();
        else
            reference.Offset = Reader.ReadUInt64();
        Debug.Assert(!reference.IsValid || reference.Size == 0 || reference.Offset + reference.Size * 4 <= (ulong)Stream.Length);
        return reference;
    }

    public MemberDefinition ReadMemberDefinition()
    {
#if DEBUG_GR2_SERIALIZATION
        var defnOffset = Stream.Position;
#endif
        var defn = new MemberDefinition();
        int typeId = Reader.ReadInt32();
        if (typeId > (uint)MemberType.Max)
            throw new ParsingException(String.Format("Unsupported member type: {0}", typeId));

        defn.Type = (MemberType)typeId;
        var name = ReadStringReference();
        Debug.Assert(!defn.IsValid || name.IsValid);
        if (defn.IsValid)
        {
            defn.Name = name.Resolve(this);

            // Remove "The Divinity Engine" prefix from LSM fields
            if (defn.Name.StartsWith("The Divinity Engine", StringComparison.Ordinal))
            {
                defn.Name = defn.Name[19..];
            }

            defn.GrannyName = defn.Name;
        }
        defn.Definition = ReadStructReference();
        defn.ArraySize = Reader.ReadUInt32();
        defn.Extra = new UInt32[MemberDefinition.ExtraTagCount];
        for (var i = 0; i < MemberDefinition.ExtraTagCount; i++)
            defn.Extra[i] = Reader.ReadUInt32();
        // TODO 64-bit: ???
        if (Magic.Is32Bit)
            defn.Unknown = Reader.ReadUInt32();
        else
            defn.Unknown = (UInt32)Reader.ReadUInt64();

        Debug.Assert(!defn.IsValid || defn.Unknown == 0);

        if (defn.Type == MemberType.Inline || defn.Type == MemberType.Reference || defn.Type == MemberType.ArrayOfReferences ||
            defn.Type == MemberType.ReferenceToArray)
            Debug.Assert(defn.Definition.IsValid);

#if DEBUG_GR2_SERIALIZATION
        string description;
        if (defn.IsValid)
        {
            if (defn.ArraySize != 0)
                description = String.Format("    [{0:X8}] {1}: {2}[{3}]", defnOffset, defn.Name, defn.Type.ToString(), defn.ArraySize);
            else
                description = String.Format("    [{0:X8}] {1}: {2}", defnOffset, defn.Name, defn.Type.ToString());

            if (defn.Definition.IsValid)
            {
                if (!DebugPendingResolve.Contains(defn.Definition))
                {
                    DebugPendingResolve.Add(defn.Definition);
                    Debug.WriteLine(String.Format(" ===== Debug resolve for {0:X8} ===== ", defn.Definition.Offset));
                    defn.Definition.Resolve(this);
                    Debug.WriteLine(String.Format(" ===== End debug resolve for {0:X8} ===== ", defn.Definition.Offset));
                }
                description += String.Format(" <struct {0:X8}>", defn.Definition.Offset);
            }

            if (defn.Extra[0] != 0 || defn.Extra[1] != 0 || defn.Extra[2] != 0)
                description += String.Format(" Extra: {0} {1} {2}", defn.Extra[0], defn.Extra[1], defn.Extra[2]);
        }
        else
        {
            description = String.Format("    <invalid>: {0}", defn.Type.ToString());
        }

        Debug.WriteLine(description);
#endif
        return defn;
    }

    public StructDefinition ReadStructDefinition()
    {
        var defn = new StructDefinition();
        while (true)
        {
            var member = ReadMemberDefinition();
            if (member.IsValid)
                defn.Members.Add(member);
            else
                break;
        }

        return defn;
    }

    internal object ReadStruct(StructDefinition definition, MemberType memberType, object node, object parent)
    {
        var offset = (UInt32)Stream.Position;
        object cachedNode = null;
        if (memberType != MemberType.Inline && CachedStructs.TryGetValue(offset, out cachedNode))
        {
#if DEBUG_GR2_SERIALIZATION
            Debug.WriteLine(String.Format("Skipped cached struct {1} at {0:X8}", offset, node.ToString()));
#endif
            Stream.Position += definition.Size(this);
            return cachedNode;
        }

        // Work around serialization of UserData and ExtendedData fields
        // whose structure may differ depending on the game and GR2 version
        if (node != null && node.GetType() == typeof(System.Object))
        {
            node = null;
        }

        if (node != null)
        {
            // Don't save inline structs in the cached struct map, as they can occupy the same address as a non-inline struct
            // if they're at the beginning of said struct.
            // They also cannot be referenced from multiple locations, so caching them is of no use.
            if (memberType != MemberType.Inline)
                CachedStructs.Add(offset, node);

#if DEBUG_GR2_FORMAT_DIFFERENCES
            // Create a struct definition from this instance and check if the GR2 type differs from the local type.
            var localDefn = new StructDefinition();
            localDefn.LoadFromType(node.GetType(), null);

            var localMembers = localDefn.Members.Where(m => m.ShouldSerialize(Header.tag)).ToList();
            var defnMembers = definition.Members.Where(m => m.ShouldSerialize(Header.tag)).ToList();

            if (localMembers.Count != defnMembers.Count)
            {
                Trace.TraceWarning(String.Format("Struct {0} differs: Field count differs ({1} vs {2})", node.GetType().Name, localMembers.Count, defnMembers.Count));
                for (int i = 0; i < defnMembers.Count; i++)
                {
                    var member = defnMembers[i];
                    Trace.TraceWarning(String.Format("\tField {0}: {1}[{2}]", member.Name, member.Type, member.ArraySize));
                }
            }
            else
            {
                for (int i = 0; i < localMembers.Count; i++)
                {
                    var member = localMembers[i];
                    var local = defnMembers[i];
                    if (member.Type != local.Type)
                    {
                        Trace.TraceWarning(String.Format(
                            "Struct {0}: Field {1} type differs ({2} vs {3})",
                            node.GetType().Name, local.Name, local.Type, member.Type
                        ));
                    }

                    if (!member.GrannyName.Equals(local.GrannyName))
                    {
                        Trace.TraceWarning(String.Format(
                            "Struct {0}: Field {1} name differs ({2} vs {3})",
                            node.GetType().Name, local.Name, local.GrannyName, member.GrannyName
                        ));
                    }

                    if (member.ArraySize != local.ArraySize)
                    {
                        Trace.TraceWarning(String.Format(
                            "Struct {0}: Field {1} array size differs ({2} vs {3})",
                            node.GetType().Name, local.Name, local.ArraySize, member.ArraySize
                        ));
                    }
                }
            }
#endif

            definition.MapType(node);
            foreach (var member in definition.Members)
            {
                var field = member.LookupFieldInfo(node);
                if (field != null)
                {
                    var value = ReadInstance(member, field.GetValue(node), field.FieldType, node);
                    field.SetValue(node, value);
                }
                else
                {
                    ReadInstance(member, null, null, node);
                }
            }
        }
        else
        {
#if DEBUG_GR2_FORMAT_DIFFERENCES
            var defnMembers = definition.Members.Where(m => m.ShouldSerialize(Header.tag)).ToList();
            Trace.TraceWarning("Unnamed struct not defined locally");
            for (int i = 0; i < defnMembers.Count; i++)
            {
                var member = defnMembers[i];
                Trace.TraceWarning(String.Format("\tField {0}: {1}[{2}]", member.Name, member.Type, member.ArraySize));
            }
#endif

            foreach (var member in definition.Members)
            {
                ReadInstance(member, null, null, null);
            }
        }

        return node;
    }

    internal object ReadInstance(MemberDefinition definition, object node, Type propertyType, object parent)
    {
        if (definition.SerializationKind == SerializationKind.UserRaw)
            return definition.Serializer.Read(this, null, definition, 0, parent);

        if (definition.ArraySize == 0)
        {
            return ReadElement(definition, node, propertyType, parent);
        }

        Type elementType = null;
        if (propertyType != null)
        {
            if (definition.SerializationKind == SerializationKind.UserMember)
            {
                // Do unserialization directly on the whole array if per-member serialization was requested.
                // This mode is a bit odd, as we resolve StructRef-s for non-arrays, but don't for array types.
                StructDefinition defn = null;
                if (definition.Definition.IsValid)
                    defn = definition.Definition.Resolve(this);
                return definition.Serializer.Read(this, defn, definition, definition.ArraySize, parent);
            }
            else if (propertyType.IsArray)
            {
                // If the property is a native array (ie. SomeType[]), create an array instance and set its values
                elementType = propertyType.GetElementType();

                Array objs = Helpers.CreateArrayInstance(propertyType, (int)definition.ArraySize) as Array;
                for (int i = 0; i < definition.ArraySize; i++)
                {
                    objs.SetValue(ReadElement(definition, objs.GetValue(i), elementType, parent), i);
                }
                return objs;
            }
            else
            {
                // For non-native arrays we always assume the property is an IList<T>
                node ??= Helpers.CreateInstance(propertyType);

                var items = node as System.Collections.IList;
                for (int i = 0; i < definition.ArraySize; i++)
                {
                    items.Add(ReadElement(definition, null, elementType, parent));
                }

                return items;
            }
        }
        else
        {
            for (int i = 0; i < definition.ArraySize; i++)
                ReadElement(definition, null, null, parent);
            return null;
        }
    }

    private object ReadElement(MemberDefinition definition, object node, Type propertyType, object parent)
    {
#if DEBUG_GR2_SERIALIZATION
        var offsetInFile = Stream.Position;
#endif

        var kind = definition.SerializationKind;
        Debug.Assert(kind == SerializationKind.Builtin || !definition.IsScalar);
        if (node == null &&
            propertyType != null &&
            !definition.IsScalar &&
            (kind == SerializationKind.Builtin || kind == SerializationKind.UserElement) &&
            // Variant construction is a special case as we don't know the struct defn beforehand
            definition.Type != MemberType.VariantReference)
        {
            node = Helpers.CreateInstance(propertyType);
        }

        switch (definition.Type)
        {
            case MemberType.Inline:
                Debug.Assert(definition.Definition.IsValid);
#if DEBUG_GR2_SERIALIZATION
                Debug.WriteLine(String.Format(" === Inline Struct {0} === ", definition.Name));
#endif
                if (kind == SerializationKind.UserElement || kind == SerializationKind.UserMember)
                    node = definition.Serializer.Read(this, definition.Definition.Resolve(this), definition, 0, parent);
                else
                    node = ReadStruct(definition.Definition.Resolve(this), definition.Type, node, parent);
#if DEBUG_GR2_SERIALIZATION
                Debug.WriteLine(" === End Struct === ");
#endif
                break;

            case MemberType.Reference:
                {
                    Debug.Assert(definition.Definition.IsValid);
                    var r = ReadReference();

                    if (r.IsValid && parent != null)
                    {
                        var originalPos = Stream.Position;
                        Seek(r);
#if DEBUG_GR2_SERIALIZATION
                        Debug.WriteLine(String.Format(" === Struct <{0}> at {1:X8} === ", definition.Name, Stream.Position));
#endif
                        if (kind == SerializationKind.UserElement || kind == SerializationKind.UserMember)
                            node = definition.Serializer.Read(this, definition.Definition.Resolve(this), definition, 0, parent);
                        else
                            node = ReadStruct(definition.Definition.Resolve(this), definition.Type, node, parent);
#if DEBUG_GR2_SERIALIZATION
                        Debug.WriteLine(" === End Struct === ");
#endif
                        Stream.Seek(originalPos, SeekOrigin.Begin);
                    }
                    else
                        node = null;
                    break;
                }

            case MemberType.VariantReference:
                {
                    var structRef = ReadStructReference();
                    var r = ReadReference();

                    if (r.IsValid && parent != null)
                    {
                        var structDefn = structRef.Resolve(this);
                        if (definition.TypeSelector != null && definition.Type == MemberType.VariantReference)
                            propertyType = definition.TypeSelector.SelectType(definition, structDefn, parent);
                        if (propertyType != null)
                            node = Helpers.CreateInstance(propertyType);

                        if (node != null)
                        {
                            var originalPos = Stream.Position;
                            Seek(r);
#if DEBUG_GR2_SERIALIZATION
                            Debug.WriteLine(String.Format(" === Variant Struct <{0}> at {1:X8} === ", definition.Name, Stream.Position));
#endif
                            if (kind == SerializationKind.UserElement || kind == SerializationKind.UserMember)
                                node = definition.Serializer.Read(this, structDefn, definition, 0, parent);
                            else
                                node = ReadStruct(structRef.Resolve(this), definition.Type, node, parent);
#if DEBUG_GR2_SERIALIZATION
                            Debug.WriteLine(" === End Struct === ");
#endif
                            Stream.Seek(originalPos, SeekOrigin.Begin);
                        }
                    }
                    else
                        node = null;
                    break;
                }

            case MemberType.ArrayOfReferences:
                {
                    // Serializing as a struct member is nooooot a very good idea here.
                    Debug.Assert(kind != SerializationKind.UserMember);
                    Debug.Assert(definition.Definition.IsValid);
                    var indices = ReadArrayIndicesReference();
#if DEBUG_GR2_SERIALIZATION
                    Debug.WriteLine(String.Format("    Array of references at [{0:X8}]", indices.Offset));
#endif

                    if (Header.version >= 7)
                    {
                        Debug.Assert(indices.IsValid == (indices.Size != 0));
                    }

                    if (indices.IsValid && indices.Size > 0 && node != null && parent != null)
                    {
                        var items = node as System.Collections.IList;
                        var type = items.GetType().GetGenericArguments().Single();

                        var refs = indices.Resolve(this);
                        var originalPos = Stream.Position;
                        for (int i = 0; i < refs.Count; i++)
                        {
                            Seek(refs[i]);
#if DEBUG_GR2_SERIALIZATION
                            Debug.WriteLine(String.Format(" === Struct <{0}> at {1:X8} === ", definition.Name, Stream.Position));
#endif
                            if (kind == SerializationKind.UserElement)
                            {
                                object element = definition.Serializer.Read(this, definition.Definition.Resolve(this), definition, 0, parent);
                                items.Add(element);
                            }
                            else
                            {
                                object element = Helpers.CreateInstance(type);
                                // TODO: Only create a new instance if we don't have a CachedStruct available!
                                element = ReadStruct(definition.Definition.Resolve(this), definition.Type, element, parent);
                                items.Add(element);

                            }
#if DEBUG_GR2_SERIALIZATION
                            Debug.WriteLine(" === End Struct === ");
#endif
                        }

                        Stream.Seek(originalPos, SeekOrigin.Begin);
                        node = items;
                    }
                    else
                        node = null;
                    break;
                }

            case MemberType.ReferenceToArray:
            case MemberType.ReferenceToVariantArray:
                {
                    StructReference structRef;
                    if (definition.Type == MemberType.ReferenceToVariantArray)
                        structRef = ReadStructReference();
                    else
                        structRef = definition.Definition;

                    var itemsRef = ReadArrayReference();

                    if (Header.version >= 7)
                    {
                        Debug.Assert(itemsRef.IsValid == (itemsRef.Size != 0));
                    }

                    if (itemsRef.IsValid &&
                        itemsRef.Size > 0 &&
                        parent != null &&
                        (node != null || kind == SerializationKind.UserMember))
                    {
                        Debug.Assert(structRef.IsValid);
                        var structType = structRef.Resolve(this);
                        var originalPos = Stream.Position;
                        Seek(itemsRef);

                        if (kind == SerializationKind.UserMember)
                        {
                            // For ReferenceTo(Variant)Array, we start serialization after resolving the array ref itself.
                            node = definition.Serializer.Read(this, structType, definition, itemsRef.Size, parent);
                        }
                        else
                        {
                            var items = node as System.Collections.IList;
                            var type = items.GetType().GetGenericArguments().Single();
                            if (definition.Type == MemberType.ReferenceToVariantArray &&
                                kind != SerializationKind.UserElement &&
                                definition.TypeSelector != null)
                                type = definition.TypeSelector.SelectType(definition, structType, parent);

                            for (int i = 0; i < itemsRef.Size; i++)
                            {
#if DEBUG_GR2_SERIALIZATION
                                Debug.WriteLine(String.Format(" === Struct <{0}> at {1:X8} === ", definition.Name, Stream.Position));
#endif
                                if (kind == SerializationKind.UserElement)
                                {
                                    object element = definition.Serializer.Read(this, structType, definition, 0, parent);
                                    items.Add(element);
                                }
                                else
                                {
                                    object element = Helpers.CreateInstance(type);
                                    element = ReadStruct(structType, definition.Type, element, parent);
                                    items.Add(element);
                                }
#if DEBUG_GR2_SERIALIZATION
                                Debug.WriteLine(" === End Struct === ");
#endif
                            }
                        }

                        Stream.Seek(originalPos, SeekOrigin.Begin);
                    }
                    else
                        node = null;
                    break;
                }

            case MemberType.String:
                var str = ReadStringReference();
                if (str.IsValid)
                    node = str.Resolve(this);
                else
                    node = null;
                break;

            case MemberType.Transform:
                var transform = new Transform();
                transform.Flags = Reader.ReadUInt32();

                for (int i = 0; i < 3; i++)
                    transform.Translation[i] = Reader.ReadSingle();

                transform.Rotation.X = Reader.ReadSingle();
                transform.Rotation.Y = Reader.ReadSingle();
                transform.Rotation.Z = Reader.ReadSingle();
                transform.Rotation.W = Reader.ReadSingle();

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                        transform.ScaleShear[i, j] = Reader.ReadSingle();
                }

                node = transform;
                break;

            case MemberType.Real16:
                throw new NotImplementedException("TODO");

            case MemberType.Real32:
                node = Reader.ReadSingle();
                break;

            case MemberType.Int8:
            case MemberType.BinormalInt8:
                node = Reader.ReadSByte();
                break;

            case MemberType.UInt8:
            case MemberType.NormalUInt8:
                node = Reader.ReadByte();
                break;

            case MemberType.Int16:
            case MemberType.BinormalInt16:
                node = Reader.ReadInt16();
                break;

            case MemberType.UInt16:
            case MemberType.NormalUInt16:
                node = Reader.ReadUInt16();
                break;

            case MemberType.Int32:
                node = Reader.ReadInt32();
                break;

            case MemberType.UInt32:
                node = Reader.ReadUInt32();
                break;

            default:
                throw new ParsingException(String.Format("Unhandled member type: {0}", definition.Type.ToString()));
        }

#if DEBUG_GR2_SERIALIZATION
        if (node != null)
            Debug.WriteLine(String.Format("    [{0:X8}] {1}: {2}", offsetInFile, definition.Name, node.ToString()));
        else
            Debug.WriteLine(String.Format("    [{0:X8}] {1}: <null>", offsetInFile, definition.Name));
#endif

        return node;
    }

    internal string ReadString()
    {
        // Not terribly efficient, but it'll do for now
        var bytes = new List<byte>();
        while (true)
        {
            byte b = Reader.ReadByte();
            if (b != 0)
                bytes.Add(b);
            else
                break;
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    internal void Seek(SectionReference reference)
    {
        Debug.Assert(reference.IsValid);
        Seek(reference.Section, reference.Offset);
    }

    internal void Seek(RelocatableReference reference)
    {
        Debug.Assert(reference.IsValid);
        Debug.Assert(reference.Offset <= (ulong)Stream.Length);
        Stream.Position = (long)reference.Offset;
    }

    internal void Seek(UInt32 section, UInt32 offset)
    {
        Debug.Assert(section < Sections.Count);
        Debug.Assert(offset <= Sections[(int)section].Header.uncompressedSize);
        Stream.Position = Sections[(int)section].Header.offsetInFile + offset;
    }

    internal void Seek(Section section, UInt32 offset)
    {
        Debug.Assert(offset <= section.Header.uncompressedSize);
        Stream.Position = section.Header.offsetInFile + offset;
    }
}
