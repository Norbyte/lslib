// #define DEBUG_GR2_SERIALIZATION

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LSLib.Granny.GR2
{
    public class ParsingException : Exception
    {
        public ParsingException(string message)
            : base(message)
        { }
    }

    public class GR2Reader
    {
        internal Stream Stream;
        internal BinaryReader Reader;
        internal Magic Magic;
        internal Header Header;
        internal List<Section> Sections = new List<Section>();
        internal Dictionary<StructReference, StructDefinition> Types = new Dictionary<StructReference, StructDefinition>();
        private Dictionary<UInt32, object> CachedStructs = new Dictionary<UInt32, object>();
#if DEBUG_GR2_SERIALIZATION
        private HashSet<StructReference> DebugPendingResolve = new HashSet<StructReference>();
#endif

        public GR2Reader(Stream stream)
        {
            // Load the whole file, as we'll update its contents because of relocations and marshalling fixups
            byte[] contents = new byte[stream.Length];
            stream.Read(contents, 0, (int)stream.Length);
            this.Stream = new MemoryStream(contents);
        }

        public void Dispose()
        {
            Stream.Dispose();
        }

        public void Read(object root)
        {
            using (this.Reader = new BinaryReader(Stream))
            {
                Magic = ReadMagic();

                if (Magic.format != Magic.Format.LittleEndian32)
                    throw new ParsingException("Only 32-bit little-endian GR2 files are supported");

                Header = ReadHeader();
                for (int i = 0; i < Header.numSections; i++)
                {
                    var section = new Section();
                    section.Header = ReadSectionHeader();
                    Sections.Add(section);
                }

                Debug.Assert(Stream.Position == Magic.headersSize);
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

                var rootStruct = new StructReference();
                rootStruct.Offset = Sections[(int)Header.rootType.Section].Header.offsetInFile + Header.rootType.Offset;

                Seek(Header.rootNode);
                ReadStruct(rootStruct.Resolve(this), MemberType.Inline, root, null);
            }
        }

        private Magic ReadMagic()
        {
            var magic = new Magic();
            magic.signature = Reader.ReadBytes(16);
            magic.format = Magic.FormatFromSignature(magic.signature);

            magic.headersSize = Reader.ReadUInt32();
            magic.headerFormat = Reader.ReadUInt32();
            magic.reserved1 = Reader.ReadUInt32();
            magic.reserved2 = Reader.ReadUInt32();

            if (magic.headerFormat != 0)
                throw new ParsingException("Compressed GR2 files are not supported");

            Debug.Assert(magic.reserved1 == 0);
            Debug.Assert(magic.reserved2 == 0);

#if DEBUG_GR2_SERIALIZATION
            System.Console.WriteLine(" ===== GR2 Magic ===== ");
            System.Console.WriteLine(String.Format("Format: {0}", magic.format));
            System.Console.WriteLine(String.Format("Headers size: {0:X8}, format: ", magic.headersSize, magic.headerFormat));
            System.Console.WriteLine(String.Format("Reserved1-2: {0:X8} {1:X8}", magic.reserved1, magic.reserved2));
#endif
            return magic;
        }

        private Header ReadHeader()
        {
            var header = new Header();
            header.version = Reader.ReadUInt32();
            header.fileSize = Reader.ReadUInt32();
            header.crc = Reader.ReadUInt32();
            header.sectionsOffset = Reader.ReadUInt32();
            header.numSections = Reader.ReadUInt32();
            header.rootType = ReadSectionReferenceUnchecked();
            header.rootNode = ReadSectionReferenceUnchecked();
            header.tag = Reader.ReadUInt32();
            header.extraTags = new UInt32[Header.ExtraTagCount];
            for (int i = 0; i < Header.ExtraTagCount; i++)
                header.extraTags[i] = Reader.ReadUInt32();
            header.stringTableCrc = Reader.ReadUInt32();
            header.reserved1 = Reader.ReadUInt32();
            header.reserved2 = Reader.ReadUInt32();
            header.reserved3 = Reader.ReadUInt32();

            if (header.version != Header.Version)
                throw new ParsingException(String.Format("Unsupported GR2 version; expected {0}, got {1}", Header.Version, header.version));

            // if (header.tag != Header.Tag)
            //    throw new ParsingException(String.Format("Incorrect header tag; expected {0:X8}, got {1:X8}", Header.Tag, header.tag));

            Debug.Assert(header.fileSize <= Stream.Length);
            Debug.Assert(header.CalculateCRC(Stream) == header.crc);
            Debug.Assert(header.sectionsOffset == Header.HeaderSize);
            Debug.Assert(header.rootType.Section < header.numSections);
            // TODO: check rootTypeOffset after serialization
            Debug.Assert(header.stringTableCrc == 0);
            Debug.Assert(header.reserved1 == 0);
            Debug.Assert(header.reserved2 == 0);
            Debug.Assert(header.reserved3 == 0);

#if DEBUG_GR2_SERIALIZATION
            System.Console.WriteLine(" ===== GR2 Header ===== ");
            System.Console.WriteLine(String.Format("Version {0}, Size {1}, CRC {2:X8}", header.version, header.fileSize, header.crc));
            System.Console.WriteLine(String.Format("Offset of sections: {0}, num sections: {1}", header.sectionsOffset, header.numSections));
            System.Console.WriteLine(String.Format("Root type section {0}, Root type offset {1:X8}", header.rootType.Section, header.rootType.Offset));
            System.Console.WriteLine(String.Format("Root node section {0} {1:X8}", header.rootNode.Section, header.rootNode.Offset));
            System.Console.WriteLine(String.Format("Tag: {0:X16}, Strings CRC: {1:X16}", header.tag, header.stringTableCrc));
            System.Console.WriteLine(String.Format("Extra tags: {0:X16} {1:X16} {2:X16} {3:X16}", header.extraTags[0], header.extraTags[1], header.extraTags[2], header.extraTags[3]));
            System.Console.WriteLine(String.Format("Reserved: {0:X16} {1:X16} {2:X16}", new object[] { header.reserved1, header.reserved2, header.reserved3 }));
#endif

            return header;
        }

        private SectionHeader ReadSectionHeader()
        {
            var header = new SectionHeader();
            header.compression = Reader.ReadUInt32();
            header.offsetInFile = Reader.ReadUInt32();
            header.compressedSize = Reader.ReadUInt32();
            header.uncompressedSize = Reader.ReadUInt32();
            header.alignment = Reader.ReadUInt32();
            header.secondaryDataOffset = Reader.ReadUInt32();
            header.secondaryDataOffset2 = Reader.ReadUInt32();
            header.relocationsOffset = Reader.ReadUInt32();
            header.numRelocations = Reader.ReadUInt32();
            header.mixedMarshallingDataOffset = Reader.ReadUInt32();
            header.numMixedMarshallingData = Reader.ReadUInt32();

            if (header.compression != 0)
                throw new ParsingException("Compressed GR2 files are not supported");

            Debug.Assert(header.offsetInFile <= Header.fileSize);
            Debug.Assert(header.compressedSize == header.uncompressedSize);

            Debug.Assert(header.offsetInFile + header.uncompressedSize <= Header.fileSize);
            // TODO: check alignment, secondaryDataOffset[2]
            Debug.Assert(header.relocationsOffset <= Header.fileSize);
            Debug.Assert(header.relocationsOffset + header.numRelocations * 12 <= Header.fileSize);
            Debug.Assert(header.mixedMarshallingDataOffset <= Header.fileSize);
            Debug.Assert(header.mixedMarshallingDataOffset + header.numMixedMarshallingData * 16 <= Header.fileSize);

#if DEBUG_GR2_SERIALIZATION
            System.Console.WriteLine(" ===== Section Header ===== ");
            System.Console.WriteLine(String.Format("Compression: {0:X8}", header.compression));
            System.Console.WriteLine(String.Format("Offset {0:X8} Comp/UncompSize {1:X8}/{2:X8}", header.offsetInFile, header.compressedSize, header.uncompressedSize));
            System.Console.WriteLine(String.Format("Alignment {0}", header.alignment));
            System.Console.WriteLine(String.Format("Secondary data offsets: {0:X8}/{1:X8}", header.secondaryDataOffset, header.secondaryDataOffset2));
            System.Console.WriteLine(String.Format("Relocations: {0:X8} count {1}", header.relocationsOffset, header.numRelocations));
            System.Console.WriteLine(String.Format("Marshalling data: {0:X8} count {1}", header.mixedMarshallingDataOffset, header.numMixedMarshallingData));
#endif
            return header;
        }

        private void ReadSectionRelocations(Section section)
        {
#if DEBUG_GR2_SERIALIZATION
            System.Console.WriteLine(String.Format(" ===== Relocations for section at {0:X8} ===== ", section.Header.offsetInFile));
#endif

            Stream.Seek(section.Header.relocationsOffset, SeekOrigin.Begin);
            for (int i = 0; i < section.Header.numRelocations; i++)
            {
                UInt32 offsetInSection = Reader.ReadUInt32();
                Debug.Assert(offsetInSection <= section.Header.uncompressedSize);
                var reference = ReadSectionReference();

                var oldPos = Stream.Position;
                Stream.Position = section.Header.offsetInFile + offsetInSection;
                var fixupAddress = Sections[(int)reference.Section].Header.offsetInFile + reference.Offset;
                Stream.Write(BitConverter.GetBytes(fixupAddress), 0, 4);
                Stream.Position = oldPos;

#if DEBUG_GR2_SERIALIZATION
                System.Console.WriteLine(String.Format("    {0:X8} --> {1}:{2:X8}", offsetInSection, (SectionType)reference.Section, reference.Offset));
#endif
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

        private void ReadSectionMixedMarshallingRelocations(Section section)
        {
#if DEBUG_GR2_SERIALIZATION
            System.Console.WriteLine(String.Format(" ===== Mixed marshalling relocations for section at {0:X8} ===== ", section.Header.offsetInFile));
#endif

            Stream.Seek(section.Header.mixedMarshallingDataOffset, SeekOrigin.Begin);
            for (int i = 0; i < section.Header.numMixedMarshallingData; i++)
            {
                UInt32 count = Reader.ReadUInt32();
                UInt32 offsetInSection = Reader.ReadUInt32();
                Debug.Assert(offsetInSection <= section.Header.uncompressedSize);
                var type = ReadSectionReference();
                var typeDefn = new StructReference();
                typeDefn.Offset = Sections[(int)type.Section].Header.offsetInFile + type.Offset;

                var oldOffset = Stream.Position;
                Seek(section, offsetInSection);
                MixedMarshal(count, typeDefn.Resolve(this));
                Stream.Seek(oldOffset, SeekOrigin.Begin);

#if DEBUG_GR2_SERIALIZATION
                System.Console.WriteLine(String.Format("    {0:X8} [{1}] --> {2}:{3:X8}", offsetInSection, count, (SectionType)type.Section, type.Offset));
#endif
            }
        }

        public SectionReference ReadSectionReferenceUnchecked()
        {
            var reference = new SectionReference();
            reference.Section = Reader.ReadUInt32();
            reference.Offset = Reader.ReadUInt32();
            return reference;
        }

        public SectionReference ReadSectionReference()
        {
            var reference = ReadSectionReferenceUnchecked();
            Debug.Assert(reference.Section < Sections.Count);
            Debug.Assert(reference.Offset <= Sections[(int)reference.Section].Header.uncompressedSize);
            return reference;
        }

        public RelocatableReference ReadReference()
        {
            var reference = new RelocatableReference();
            reference.Offset = Reader.ReadUInt32();
            return reference;
        }

        public StructReference ReadStructReference()
        {
            var reference = new StructReference();
            reference.Offset = Reader.ReadUInt32();
            return reference;
        }

        public StringReference ReadStringReference()
        {
            var reference = new StringReference();
            reference.Offset = Reader.ReadUInt32();
            return reference;
        }

        public ArrayReference ReadArrayReference()
        {
            var reference = new ArrayReference();
            reference.Size = Reader.ReadUInt32();
            reference.Offset = Reader.ReadUInt32();
            return reference;
        }

        public ArrayIndicesReference ReadArrayIndicesReference()
        {
            var reference = new ArrayIndicesReference();
            reference.Size = Reader.ReadUInt32();
            reference.Offset = Reader.ReadUInt32();
            Debug.Assert(!reference.IsValid || reference.Offset + reference.Size * 4 <= Header.fileSize);
            return reference;
        }

        public MemberDefinition ReadMemberDefinition()
        {
            var defn = new MemberDefinition();
            int typeId = Reader.ReadInt32();
            if (typeId > (uint)MemberType.Max)
                throw new ParsingException(String.Format("Unsupported member type: {0}", typeId));

            defn.Type = (MemberType)typeId;
            var name = ReadStringReference();
            Debug.Assert(!defn.IsValid || name.IsValid);
            if (defn.IsValid)
                defn.Name = name.Resolve(this);
            defn.Definition = ReadStructReference();
            defn.ArraySize = Reader.ReadUInt32();
            defn.Extra = new UInt32[MemberDefinition.ExtraTagCount];
            for (var i = 0; i < MemberDefinition.ExtraTagCount; i++)
                defn.Extra[i] = Reader.ReadUInt32();
            defn.Unknown = Reader.ReadUInt32();

            Debug.Assert(defn.Unknown == 0);

            if (defn.Type == MemberType.Inline || defn.Type == MemberType.Reference || defn.Type == MemberType.ArrayOfReferences ||
                defn.Type == MemberType.ReferenceToArray)
                Debug.Assert(defn.Definition.IsValid);

#if DEBUG_GR2_SERIALIZATION
            string description;
            if (defn.IsValid)
            {
                if (defn.ArraySize != 0)
                    description = String.Format("    {0}: {1}[{2}]", defn.Name, defn.Type.ToString(), defn.ArraySize);
                else
                    description = String.Format("    {0}: {1}", defn.Name, defn.Type.ToString());

                if (defn.Definition.IsValid)
                {
                    if (!DebugPendingResolve.Contains(defn.Definition))
                    {
                        DebugPendingResolve.Add(defn.Definition);
                        System.Console.WriteLine(String.Format(" ===== Debug resolve for {0:X8} ===== ", defn.Definition.Offset));
                        defn.Definition.Resolve(this);
                        System.Console.WriteLine(String.Format(" ===== End debug resolve for {0:X8} ===== ", defn.Definition.Offset));
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

            System.Console.WriteLine(description);
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
                System.Console.WriteLine(String.Format("Skipped cached struct {1} at {0:X8}", offset, node.ToString()));
#endif
                Stream.Position += definition.Size(this);
                return cachedNode;
            }

            if (node != null)
            {
                // Don't save inline structs in the cached struct map, as they can occupy the same address as a non-inline struct
                // if they're at the beginning of said struct.
                // They also cannot be referenced from multiple locations, so caching them is of no use.
                if (memberType != MemberType.Inline)
                    CachedStructs.Add(offset, node);

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
                    if (node == null)
                        node = Helpers.CreateInstance(propertyType);

                    var items = node as System.Collections.IList;
                    var type = items.GetType().GetGenericArguments().Single();
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
                    System.Console.WriteLine(String.Format(" === Inline Struct {0} === ", definition.Name));
#endif
                    if (kind == SerializationKind.UserElement || kind == SerializationKind.UserMember)
                        node = definition.Serializer.Read(this, definition.Definition.Resolve(this), definition, 0, parent);
                    else
                        node = ReadStruct(definition.Definition.Resolve(this), definition.Type, node, parent);
#if DEBUG_GR2_SERIALIZATION
                    System.Console.WriteLine(" === End Struct === ");
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
                            System.Console.WriteLine(String.Format(" === Struct <{0}> at {1:X8} === ", definition.Name, Stream.Position));
#endif
                            if (kind == SerializationKind.UserElement || kind == SerializationKind.UserMember)
                                node = definition.Serializer.Read(this, definition.Definition.Resolve(this), definition, 0, parent);
                            else
                                node = ReadStruct(definition.Definition.Resolve(this), definition.Type, node, parent);
#if DEBUG_GR2_SERIALIZATION
                            System.Console.WriteLine(" === End Struct === ");
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
                                System.Console.WriteLine(String.Format(" === Typed Struct {0} === ", definition.Name));
#endif
                                if (kind == SerializationKind.UserElement || kind == SerializationKind.UserMember)
                                    node = definition.Serializer.Read(this, structDefn, definition, 0, parent);
                                else
                                    node = ReadStruct(structRef.Resolve(this), definition.Type, node, parent);
#if DEBUG_GR2_SERIALIZATION
                                System.Console.WriteLine(" === End Struct === ");
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

                        Debug.Assert(indices.IsValid == (indices.Size != 0));

                        if (indices.IsValid && node != null && parent != null)
                        {
                            var items = node as System.Collections.IList;
                            var type = items.GetType().GetGenericArguments().Single();

                            var refs = indices.Resolve(this);
                            var originalPos = Stream.Position;
                            for (int i = 0; i < refs.Count; i++)
                            {
                                Seek(refs[i]);
#if DEBUG_GR2_SERIALIZATION
                                System.Console.WriteLine(String.Format(" === Struct <{0}> at {1:X8} === ", definition.Name, Stream.Position));
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
                                System.Console.WriteLine(" === End Struct === ");
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

                        Debug.Assert(itemsRef.IsValid == (itemsRef.Size != 0));

                        if (itemsRef.IsValid && 
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
                                    System.Console.WriteLine(String.Format(" === Struct <{0}> at {1:X8} === ", definition.Name, Stream.Position));
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
                                    System.Console.WriteLine(" === End Struct === ");
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
                System.Console.WriteLine(String.Format("    {0}: {1}", definition.Name, node.ToString()));
            else
                System.Console.WriteLine(String.Format("    {0}: <null>", definition.Name));
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
            Debug.Assert(reference.Offset <= Header.fileSize);
            Stream.Position = reference.Offset;
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
}
