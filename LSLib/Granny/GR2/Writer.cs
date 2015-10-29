using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK;

namespace LSLib.Granny.GR2
{
    public class MixedMarshallingData
    {
        public object Obj;
        public UInt32 Count;
        public StructDefinition Type;
    }

    public class WritableSection : Section
    {
        public SectionType Type;
        public MemoryStream MainStream;
        public MemoryStream DataStream;

        public BinaryWriter MainWriter;
        public BinaryWriter DataWriter;

        public BinaryWriter Writer;
        public GR2Writer GR2;

        public Dictionary<UInt32, object> Fixups = new Dictionary<UInt32, object>();
        // Fixups for the data area that we'll need to update after serialization is finished
        public Dictionary<UInt32, object> DataFixups = new Dictionary<UInt32, object>();

        public List<MixedMarshallingData> MixedMarshalling = new List<MixedMarshallingData>();

        public WritableSection(SectionType type, GR2Writer writer)
        {
            Type = type;
            MainStream = new MemoryStream();
            MainWriter = new BinaryWriter(MainStream);

            DataStream = new MemoryStream();
            DataWriter = new BinaryWriter(DataStream);

            Writer = MainWriter;
            Header = InitHeader();
            GR2 = writer;
        }

        public void Finish()
        {
            var dataOffset = (UInt32)MainStream.Length;
            
            foreach (var dataFixup in DataFixups)
            {
                Fixups.Add(dataFixup.Key + dataOffset, dataFixup.Value);
            }

            MainWriter.Write(DataStream.ToArray());
        }

        private SectionHeader InitHeader()
        {
            var header = new SectionHeader();
            header.compression = 0;
            header.offsetInFile = 0; // Set after serialization is finished
            header.compressedSize = 0; // Set after serialization is finished
            header.uncompressedSize = 0; // Set after serialization is finished
            if (Type == SectionType.RigidVertex || Type == SectionType.DeformableVertex)
                header.alignment = 32;
            else
                header.alignment = 4;
            header.secondaryDataOffset = 0; // Set after serialization is finished
            header.secondaryDataOffset2 = 0; // Set after serialization is finished
            header.relocationsOffset = 0; // Set after serialization is finished
            header.numRelocations = 0; // Set after serialization is finished
            header.mixedMarshallingDataOffset = 0; // Set after serialization is finished
            header.numMixedMarshallingData = 0; // Set after serialization is finished

            return header;
        }

        public void AddFixup(object o)
        {
            if (Writer == MainWriter)
            {
                Fixups.Add((UInt32)MainStream.Position, o);
            }
            else
            {
                DataFixups.Add((UInt32)DataStream.Position, o);
            }
        }

        internal void AddMixedMarshalling(object o, UInt32 count, StructDefinition type)
        {
            var marshal = new MixedMarshallingData();
            marshal.Obj = o;
            marshal.Count = count;
            marshal.Type = type;
            MixedMarshalling.Add(marshal);
        }

        internal void CheckMixedMarshalling(object o, Type type, UInt32 count)
        {
            if (type.IsClass)
            {
                var defn = GR2.LookupStructDefinition(type);
                if (defn.MixedMarshal)
                {
                    AddMixedMarshalling(o, count, defn);
                }
            }
        }

        internal void CheckMixedMarshalling(object o, UInt32 count)
        {
            CheckMixedMarshalling(o, o.GetType(), count);
        }

        public void WriteReference(object o)
        {
            if (o != null)
            {
                AddFixup(o);
            }

            Writer.Write((UInt32)0);
        }

        public void WriteStructReference(StructDefinition defn)
        {
            if (defn != null)
            {
                AddFixup(defn);

                if (!GR2.Types.ContainsKey(defn.Type))
                {
                    GR2.Types.Add(defn.Type, defn);
                }
            }
            
            Writer.Write((UInt32)0);
        }

        public void WriteStringReference(string s)
        {
            if (s != null)
            {
                AddFixup(s);

                if (!GR2.Strings.Contains(s))
                {
                    GR2.Strings.Add(s);
                    GR2.QueueStringWrite(SectionType.Main, s);
                }
            }

            Writer.Write((UInt32)0);
        }

        public void WriteArrayReference(System.Collections.IList list)
        {
            if (list != null && list.Count > 0)
            {
                Writer.Write((UInt32)list.Count);
                AddFixup(list);
            }
            else
            {
                Writer.Write((UInt32)0);
            }

            Writer.Write((UInt32)0);
        }

        public void WriteArrayIndicesReference(System.Collections.IList list)
        {
            WriteArrayReference(list);
        }

        public void WriteMemberDefinition(MemberDefinition defn)
        {
            Writer.Write((UInt32)defn.Type);
            WriteStringReference(defn.GrannyName);
            WriteStructReference(defn.WriteDefinition);
            Writer.Write(defn.ArraySize);
            for (var i = 0; i < MemberDefinition.ExtraTagCount; i++)
                Writer.Write(defn.Extra[i]);
            Writer.Write(defn.Unknown);
        }

        public void WriteStructDefinition(StructDefinition defn)
        {
            Debug.Assert(Writer == MainWriter);
            GR2.ObjectOffsets[defn] = new SectionReference(Type, (UInt32)MainStream.Position);
            foreach (var member in defn.Members)
            {
                WriteMemberDefinition(member);
            }

            var end = new MemberDefinition();
            end.Type = MemberType.None;
            end.Extra = new UInt32[] { 0, 0, 0 };
            WriteMemberDefinition(end);
        }

        internal void WriteStruct(object node, bool allowRecursion = true)
        {
            WriteStruct(node.GetType(), node, allowRecursion);
        }

        internal void WriteStruct(Type type, object node, bool allowRecursion = true)
        {
            WriteStruct(GR2.LookupStructDefinition(type), node, allowRecursion);
        }

        internal void StoreObjectOffset(object o)
        {
            if (Writer == MainWriter)
            {
                GR2.ObjectOffsets[o] = new SectionReference(Type, (UInt32)MainStream.Position);
            }
            else
            {
                GR2.DataObjectOffsets[o] = new SectionReference(Type, (UInt32)DataStream.Position);
            }
        }

        internal void AlignWrite()
        {
            if (Writer == MainWriter)
            {
                // Align the struct so its size (and the address of the subsequent struct) is a multiple of 4
                while ((MainStream.Position % 4) != 0)
                {
                    Writer.Write((Byte)0);
                }
            }
            else
            {
                // Align the struct so its size (and the address of the subsequent struct) is a multiple of 4
                while ((DataStream.Position % 4) != 0)
                {
                    Writer.Write((Byte)0);
                }
            }
        }

        internal void WriteStruct(StructDefinition definition, object node, bool allowRecursion = true)
        {
            if (node == null) throw new ArgumentNullException();

            AlignWrite();
            StoreObjectOffset(node);

            foreach (var member in definition.Members)
            {
                var value = member.CachedField.GetValue(node);
                if (member.SerializationKind == SerializationKind.UserRaw)
                    member.Serializer.Write(this.GR2, this, member, value);
                else
                    WriteInstance(member, member.CachedField.FieldType, value);
            }

            // When the struct is empty, we need to write a dummy byte to make sure that another 
            // struct won't have the same address.
            if (definition.Members.Count == 0)
            {
                Writer.Write((Byte)0);
            }

            if (Writer == MainWriter && allowRecursion)
            {
                // We need to write all child structs directly after the parent struct
                // (at least this is how granny2.dll does it)
                GR2.FlushPendingWrites();
            }
        }

        internal void WriteArray(MemberDefinition arrayDefn, Type elementType, System.Collections.IList list)
        {
            bool dataArea = arrayDefn.DataArea || (Writer == DataWriter);
            AlignWrite();

            switch (arrayDefn.Type)
            {
                case MemberType.ArrayOfReferences:
                    {
                        // Serializing as a struct member is nooooot a very good idea here.
                        // Debug.Assert(kind != SerializationKind.UserMember);

                        // Reference lists are always written to the data area
                        var oldWriter = Writer;
                        Writer = DataWriter;

                        StoreObjectOffset(list);
                        for (int i = 0; i < list.Count; i++)
                        {
                            WriteReference(list[i]);
                            GR2.QueueStructWrite(Type, dataArea, arrayDefn, elementType, list[i]);
                        }

                        Writer = oldWriter;
                        break;
                    }

                case MemberType.ReferenceToArray:
                case MemberType.ReferenceToVariantArray:
                    {
                        StoreObjectOffset(list);

                        if (arrayDefn.SerializationKind == SerializationKind.UserMember)
                        {
                            arrayDefn.Serializer.Write(this.GR2, this, arrayDefn, list);
                        }
                        else if (arrayDefn.SerializationKind == SerializationKind.UserElement)
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                StoreObjectOffset(list[i]);
                                arrayDefn.Serializer.Write(this.GR2, this, arrayDefn, list[i]);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                WriteStruct(elementType, list[i], false);
                            }
                        }

                        GR2.FlushPendingWrites();

                        break;
                    }

                default:
                    throw new ParsingException(String.Format("Unhandled array member type: {0}", arrayDefn.Type.ToString()));
            }
        }

        internal void WriteInstance(MemberDefinition definition, Type propertyType, object node)
        {
            if (definition.ArraySize == 0)
            {
                WriteElement(definition, propertyType, node);
                return;
            }

            if (propertyType.IsArray)
            {
                // If the property is a native array (ie. SomeType[]), create an array instance and set its values
                var elementType = propertyType.GetElementType();

                Array arr = node as Array;
                Debug.Assert(arr.Length == definition.ArraySize);
                for (int i = 0; i < definition.ArraySize; i++)
                {
                    WriteElement(definition, elementType, arr.GetValue(i));
                }
            }
            else
            {
                // For non-native arrays we always assume the property is an IList<T>
                var items = node as System.Collections.IList;
                var elementType = items.GetType().GetGenericArguments().Single();
                foreach (var element in items)
                {
                    WriteElement(definition, elementType, element);
                }
            }
        }

        private void WriteElement(MemberDefinition definition, Type propertyType, object node)
        {
            var type = definition.CachedField.FieldType;
            bool dataArea = definition.DataArea || (Writer == DataWriter);

            switch (definition.Type)
            {
                case MemberType.Inline:
                    if (definition.SerializationKind == SerializationKind.UserMember)
                        definition.Serializer.Write(this.GR2, this, definition, node);
                    else
                        WriteStruct(type, node, false);
                    break;

                case MemberType.Reference:
                    {
                        WriteReference(node);
                        if (node != null)
                        {
                            GR2.QueueStructWrite(Type, dataArea, definition, type, node);
                        }
                        break;
                    }

                case MemberType.VariantReference:
                    {
                        if (node != null)
                        {
                            var inferredType = node.GetType();
                            if (definition.TypeSelector != null)
                            {
                                var variantType = definition.TypeSelector.SelectType(definition, node);
                                if (variantType != null)
                                    inferredType = variantType;
                            }

                            WriteStructReference(GR2.LookupStructDefinition(inferredType));
                            WriteReference(node);

                            GR2.QueueStructWrite(Type, dataArea, definition, inferredType, node);
                        }
                        else
                        {
                            WriteStructReference(null);
                            WriteReference(null);
                        }
                        break;
                    }

                case MemberType.ArrayOfReferences:
                    {
                        // Serializing as a struct member is nooooot a very good idea here.
                        // Debug.Assert(kind != SerializationKind.UserMember);
                        var list = node as System.Collections.IList;
                        WriteArrayIndicesReference(list);

                        if (list != null && list.Count > 0)
                        {
                            GR2.QueueArrayWrite(Type, dataArea, type.GetGenericArguments().Single(), definition, list);
                        }

                        break;
                    }

                case MemberType.ReferenceToArray:
                    {
                        var list = node as System.Collections.IList;
                        WriteArrayIndicesReference(list);

                        if (list != null && list.Count > 0)
                        {
                            GR2.QueueArrayWrite(Type, dataArea, type.GetGenericArguments().Single(), definition, list);
                        }
                        break;
                    }

                case MemberType.ReferenceToVariantArray:
                    {
                        var list = node as System.Collections.IList;

                        if (list != null && list.Count > 0)
                        {
                            var inferredType = list[0].GetType();
                            if (definition.TypeSelector != null)
                            {
                                var variantType = definition.TypeSelector.SelectType(definition, node);
                                if (variantType != null)
                                    inferredType = variantType;
                            }

                            WriteStructReference(GR2.LookupStructDefinition(inferredType));
                            WriteArrayIndicesReference(list);
                            GR2.QueueArrayWrite(Type, dataArea, inferredType, definition, list);
                        }
                        else
                        {
                            WriteStructReference(null);
                            WriteArrayIndicesReference(list);
                        }
                        break;
                    }

                case MemberType.String:
                    WriteStringReference(node as string);
                    break;

                case MemberType.Transform:
                    var transform = node as Transform;
                    Writer.Write(transform.Flags);

                    for (int i = 0; i < 3; i++)
                        Writer.Write(transform.Translation[i]);

                    Writer.Write(transform.Rotation.X);
                    Writer.Write(transform.Rotation.Y);
                    Writer.Write(transform.Rotation.Z);
                    Writer.Write(transform.Rotation.W);

                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                            Writer.Write(transform.ScaleShear[i, j]);
                    }
                    break;

                case MemberType.Real16:
                    Writer.Write((Half)node);
                    break;

                case MemberType.Real32:
                    Writer.Write((Single)node);
                    break;

                case MemberType.Int8:
                case MemberType.BinormalInt8:
                    Writer.Write((SByte)node);
                    break;

                case MemberType.UInt8:
                case MemberType.NormalUInt8:
                    Writer.Write((Byte)node);
                    break;

                case MemberType.Int16:
                case MemberType.BinormalInt16:
                    Writer.Write((Int16)node);
                    break;

                case MemberType.UInt16:
                case MemberType.NormalUInt16:
                    Writer.Write((UInt16)node);
                    break;

                case MemberType.Int32:
                    Writer.Write((Int32)node);
                    break;

                case MemberType.UInt32:
                    Writer.Write((UInt32)node);
                    break;

                default:
                    throw new ParsingException(String.Format("Unhandled member type: {0}", definition.Type.ToString()));
            }
        }

        internal void WriteString(string s)
        {
            GR2.DataObjectOffsets[s] = new SectionReference(Type, (UInt32)DataStream.Position);
            var bytes = Encoding.UTF8.GetBytes(s);
            DataWriter.Write(bytes);
            DataWriter.Write((Byte)0);
        }

        internal void WriteSectionRelocations(WritableSection section)
        {
            section.Header.numRelocations = (UInt32)section.Fixups.Count;
            section.Header.relocationsOffset = (UInt32)MainStream.Position;

            foreach (var fixup in section.Fixups)
            {
                Writer.Write(fixup.Key);
                WriteSectionReference(GR2.ObjectOffsets[fixup.Value]);
            }
        }

        internal void WriteSectionMixedMarshallingRelocations(WritableSection section)
        {
            section.Header.numMixedMarshallingData = (UInt32)section.MixedMarshalling.Count;
            section.Header.mixedMarshallingDataOffset = (UInt32)MainStream.Position;

            foreach (var marshal in section.MixedMarshalling)
            {
                Writer.Write(marshal.Count);
                Writer.Write(GR2.ObjectOffsets[marshal.Obj].Offset);
                WriteSectionReference(GR2.ObjectOffsets[marshal.Type]);
            }
        }

        internal void WriteSectionReference(SectionReference r)
        {
            Writer.Write((UInt32)r.Section);
            Writer.Write(r.Offset);
        }
    };

    public class GR2Writer
    {
        struct QueuedSerialization
        {
            public SectionType section;
            public bool dataArea;
            public MemberDefinition member;
            public Type type;
            public object obj;
        }

        struct QueuedArraySerialization
        {
            public SectionType section;
            public bool dataArea;
            public Type elementType;
            public MemberDefinition member;
            public System.Collections.IList list;
        }

        struct QueuedStringSerialization
        {
            public SectionType section;
            public String str;
        }

        internal MemoryStream Stream;
        internal BinaryWriter Writer;
        internal Magic Magic;
        internal Header Header;
        internal WritableSection CurrentSection;
        internal List<WritableSection> Sections = new List<WritableSection>();
        internal Dictionary<Type, StructDefinition> Types = new Dictionary<Type, StructDefinition>();

        private List<QueuedSerialization> StructWrites = new List<QueuedSerialization>();
        private List<QueuedArraySerialization> ArrayWrites = new List<QueuedArraySerialization>();
        private List<QueuedStringSerialization> StringWrites = new List<QueuedStringSerialization>();

        internal Dictionary<object, SectionReference> ObjectOffsets = new Dictionary<object, SectionReference>();
        internal Dictionary<object, SectionReference> DataObjectOffsets = new Dictionary<object, SectionReference>();
        internal HashSet<string> Strings = new HashSet<string>();

        // Version tag that will be written to the GR2 file
        public UInt32 VersionTag = Header.Tag;

        public GR2Writer()
        {
            this.Stream = new MemoryStream();
        }

        public void Dispose()
        {
            Stream.Dispose();
        }

        public void FlushPendingWrites()
        {
            if (ArrayWrites.Count == 0 && StructWrites.Count == 0 && StringWrites.Count == 0)
            {
                return;
            }

            var arrayWrites = ArrayWrites;
            var structWrites = StructWrites;
            var stringWrites = StringWrites;
            ArrayWrites = new List<QueuedArraySerialization>();
            StructWrites = new List<QueuedSerialization>();
            StringWrites = new List<QueuedStringSerialization>();

            foreach (var write in structWrites)
            {
                if (!ObjectOffsets.ContainsKey(write.obj))
                {
                    var section = Sections[(int)write.section];
                    var oldWriter = section.Writer;
                    if (write.dataArea)
                    {
                        section.Writer = section.DataWriter;
                    }

                    section.WriteStruct(write.type, write.obj);
                    section.Writer = oldWriter;
                }
            }

            foreach (var write in arrayWrites)
            {
                if (!ObjectOffsets.ContainsKey(write.list))
                {
                    var section = Sections[(int)write.section];
                    var oldWriter = section.Writer;
                    if (write.dataArea)
                    {
                        section.Writer = section.DataWriter;
                    }

                    section.WriteArray(write.member, write.elementType, write.list);
                    section.Writer = oldWriter;
                }
            }

            foreach (var write in stringWrites)
            {
                Sections[(int)write.section].WriteString(write.str);
            }
        }

        internal void FinalizeOffsets()
        {
            foreach (var offset in DataObjectOffsets)
            {
                offset.Value.Offset += (UInt32)Sections[(int)offset.Value.Section].MainStream.Length;
                ObjectOffsets.Add(offset.Key, offset.Value);
            }
        }

        public byte[] Write(object root)
        {
            using (this.Writer = new BinaryWriter(Stream))
            {
                Magic = InitMagic();
                WriteMagic(Magic);

                Header = InitHeader();
                WriteHeader(Header);

                for (int i = 0; i < Header.numSections; i++)
                {
                    var section = new WritableSection((SectionType)i, this);
                    WriteSectionHeader(section.Header);
                    Sections.Add(section);
                }

                Magic.headersSize = (UInt32)Stream.Position;

                CurrentSection = Sections[(int)SectionType.Main];
                CurrentSection.WriteStruct(root);

                while (ArrayWrites.Count > 0 || StructWrites.Count > 0 || StringWrites.Count > 0)
                {
                    FlushPendingWrites();
                }

                foreach (var defn in Types.Values)
                {
                    Sections[(int)SectionType.Discardable].WriteStructDefinition(defn);
                }

                // We need to do this again to flush strings written by WriteMemberDefinition()
                FlushPendingWrites();

                FinalizeOffsets();

                foreach (var section in Sections)
                {
                    section.Header.secondaryDataOffset = (UInt32)section.MainStream.Length;
                    section.Header.secondaryDataOffset2 = (UInt32)section.MainStream.Length;
                    section.Finish();
                }

                var relocSection = Sections[(int)SectionType.Discardable];
                foreach (var section in Sections)
                {
                    relocSection.WriteSectionRelocations(section);
                    relocSection.WriteSectionMixedMarshallingRelocations(section);
                }

                foreach (var section in Sections)
                {
                    // Pad section size to a multiple of 4
                    while (section.MainStream.Position % 4 > 0)
                        section.Writer.Write((Byte)0);

                    section.MainStream.Flush();
                    section.Header.offsetInFile = (UInt32)Stream.Position;
                    section.Header.uncompressedSize = (UInt32)section.MainStream.Length;
                    section.Header.compressedSize = (UInt32)section.MainStream.Length;
                    Writer.Write(section.MainStream.ToArray());
                }

                foreach (var section in Sections)
                {
                    section.Header.relocationsOffset += relocSection.Header.offsetInFile;
                    section.Header.mixedMarshallingDataOffset += relocSection.Header.offsetInFile;
                }

                var rootStruct = LookupStructDefinition(root.GetType());
                Header.rootType = ObjectOffsets[rootStruct];
                Header.rootNode = new SectionReference(SectionType.Main, 0);
                Header.fileSize = (UInt32)Stream.Length;

                Stream.Seek(Magic.MagicSize + Header.HeaderSize, SeekOrigin.Begin);

                foreach (var section in Sections)
                {
                    WriteSectionHeader(section.Header);
                }

                Header.crc = Header.CalculateCRC(Stream);
                Stream.Seek(0, SeekOrigin.Begin);
                WriteMagic(Magic);
                WriteHeader(Header);

                return Stream.ToArray();
            }
        }

        private Magic InitMagic()
        {
            var magic = new Magic();
            magic.format = Magic.Format.LittleEndian32;
            magic.signature = Magic.SignatureFromFormat(magic.format);

            magic.headersSize = 0; // Updated after headers are serialized
            magic.headerFormat = 0;
            magic.reserved1 = 0;
            magic.reserved2 = 0;

            return magic;
        }

        private void WriteMagic(Magic magic)
        {
            Writer.Write(magic.signature);
            Writer.Write(magic.headersSize);
            Writer.Write(magic.headerFormat);
            Writer.Write(magic.reserved1);
            Writer.Write(magic.reserved2);
        }

        private Header InitHeader()
        {
            var header = new Header();
            header.version = Header.Version;
            header.fileSize = 0; // Set after serialization is finished
            header.crc = 0; // Set after serialization is finished
            header.sectionsOffset = Header.HeaderSize;
            header.rootType = new SectionReference(); // Updated after serialization is finished
            header.rootNode = new SectionReference(); // Updated after serialization is finished
            header.numSections = (UInt32)SectionType.Max + 1;
            header.tag = VersionTag;
            header.extraTags = new UInt32[Header.ExtraTagCount];
            for (int i = 0; i < Header.ExtraTagCount; i++)
                header.extraTags[i] = 0;
            header.stringTableCrc = 0;
            header.reserved1 = 0;
            header.reserved2 = 0;
            header.reserved3 = 0;

            return header;
        }

        private void WriteHeader(Header header)
        {
            Writer.Write(header.version);
            Writer.Write(header.fileSize);
            Writer.Write(header.crc);
            Writer.Write(header.sectionsOffset);
            Writer.Write(header.numSections);
            WriteSectionReference(header.rootType);
            WriteSectionReference(header.rootNode);
            Writer.Write(header.tag);
            for (int i = 0; i < Header.ExtraTagCount; i++)
                Writer.Write(header.extraTags[i]);
            Writer.Write(header.stringTableCrc);
            Writer.Write(header.reserved1);
            Writer.Write(header.reserved2);
            Writer.Write(header.reserved3);
        }

        private void WriteSectionHeader(SectionHeader header)
        {
            Writer.Write(header.compression);
            Writer.Write(header.offsetInFile);
            Writer.Write(header.compressedSize);
            Writer.Write(header.uncompressedSize);
            Writer.Write(header.alignment);
            Writer.Write(header.secondaryDataOffset);
            Writer.Write(header.secondaryDataOffset2);
            Writer.Write(header.relocationsOffset);
            Writer.Write(header.numRelocations);
            Writer.Write(header.mixedMarshallingDataOffset);
            Writer.Write(header.numMixedMarshallingData);
        }

        public void WriteSectionReference(SectionReference r)
        {
            Writer.Write((UInt32)r.Section);
            Writer.Write(r.Offset);
        }

        internal StructDefinition LookupStructDefinition(Type type)
        {
            if (type.GetInterfaces().Contains(typeof(System.Collections.IList)) || type.IsArray || type.IsPrimitive)
                throw new ArgumentException("Cannot create a struct definition for array or primitive types");

            StructDefinition defn = null;
            if (!Types.TryGetValue(type, out defn))
            {
                defn = new StructDefinition();
                Types.Add(type, defn);
                defn.LoadFromType(type, this);
            }

            return defn;
        }

        internal void QueueStructWrite(SectionType section, bool dataArea, MemberDefinition member, Type type, object obj)
        {
            QueuedSerialization serialization;
            serialization.section = section;
            serialization.dataArea = dataArea;
            if (member.PreferredSection != SectionType.Invalid)
                serialization.section = member.PreferredSection;
            else if (member.SectionSelector != null)
                serialization.section = member.SectionSelector.SelectSection(member, type, obj);
            serialization.type = type;
            serialization.member = member;
            serialization.obj = obj;

            Sections[(int)serialization.section].CheckMixedMarshalling(obj, type, 1);
            StructWrites.Add(serialization);
        }

        internal void QueueArrayWrite(SectionType section, bool dataArea, Type elementType, MemberDefinition member, System.Collections.IList list)
        {
            QueuedArraySerialization serialization;
            serialization.section = section;
            serialization.dataArea = dataArea;
            if (member.PreferredSection != SectionType.Invalid)
                serialization.section = member.PreferredSection;
            else if (member.SectionSelector != null)
                serialization.section = member.SectionSelector.SelectSection(member, elementType, list);
            serialization.elementType = elementType;
            serialization.member = member;
            serialization.list = list;

            Sections[(int)serialization.section].CheckMixedMarshalling(list[0], elementType, (UInt32)list.Count);
            ArrayWrites.Add(serialization);
        }

        internal void QueueStringWrite(SectionType section, String s)
        {
            QueuedStringSerialization serialization;
            serialization.section = section;
            serialization.str = s;
            StringWrites.Add(serialization);
        }
    }
}
