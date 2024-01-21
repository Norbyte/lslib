namespace LSLib.LS.Story;

public class Story
{
    public byte MinorVersion;
    public byte MajorVersion;
    // Use 16-bit instead of 32-bit type IDs, BG3 Patch8+
    public bool ShortTypeIds;
    public SaveFileHeader Header;
    public Dictionary<uint, OsirisEnum> Enums;
    public Dictionary<uint, OsirisType> Types;
    public List<OsirisDivObject> DivObjects;
    public List<Function> Functions;
    public Dictionary<uint, Node> Nodes;
    public Dictionary<uint, Adapter> Adapters;
    public Dictionary<uint, Database> Databases;
    public Dictionary<uint, Goal> Goals;
    public List<Call> GlobalActions;
    public List<string> ExternalStringTable;
    public Dictionary<string, Function> FunctionSignatureMap;

    public uint Version
    {
        get
        {
            return ((uint)MajorVersion << 8) | (uint)MinorVersion;
        }
    }

    public void DebugDump(TextWriter writer)
    {
        writer.WriteLine(" --- ENUMS ---");
        foreach (var e in Enums)
        {
            e.Value.DebugDump(writer);
        }
        
        writer.WriteLine(" --- TYPES ---");
        foreach (var type in Types)
        {
            type.Value.DebugDump(writer);
        }

        writer.WriteLine();
        writer.WriteLine(" --- DIV OBJECTS ---");
        foreach (var obj in DivObjects)
        {
            obj.DebugDump(writer);
        }

        writer.WriteLine();
        writer.WriteLine(" --- FUNCTIONS ---");
        foreach (var function in Functions)
        {
            function.DebugDump(writer, this);
        }

        writer.WriteLine();
        writer.WriteLine(" --- NODES ---");
        foreach (var node in Nodes)
        {
            writer.Write("#{0} ", node.Key);
            node.Value.DebugDump(writer, this);
            writer.WriteLine();
        }

        writer.WriteLine();
        writer.WriteLine(" --- ADAPTERS ---");
        foreach (var adapter in Adapters)
        {
            writer.Write("#{0} ", adapter.Key);
            adapter.Value.DebugDump(writer, this);
        }

        writer.WriteLine();
        writer.WriteLine(" --- DATABASES ---");
        foreach (var database in Databases)
        {
            writer.Write("#{0} ", database.Key);
            database.Value.DebugDump(writer, this);
        }

        writer.WriteLine();
        writer.WriteLine(" --- GOALS ---");
        foreach (var goal in Goals)
        {
            writer.Write("#{0} ", goal.Key);
            goal.Value.DebugDump(writer, this);
            writer.WriteLine();
        }

        writer.WriteLine();
        writer.WriteLine(" --- GLOBAL ACTIONS ---");
        foreach (var call in GlobalActions)
        {
            call.DebugDump(writer, this);
            writer.WriteLine();
        }
    }

    public uint FindBuiltinTypeId(uint typeId)
    {
        var aliasId = typeId;

        while (typeId != 0 && Types[aliasId].Alias != 0)
        {
            aliasId = Types[aliasId].Alias;
        }

        return aliasId;
    }
}

public class StoryReader
{
    public StoryReader()
    {

    }

    private List<string> ReadStrings(OsiReader reader)
    {
        var stringTable = new List<string>();
        var count = reader.ReadUInt32();
        while (count-- > 0)
        {
            stringTable.Add(reader.ReadString());
        }

        return stringTable;
    }

    private Dictionary<uint, OsirisType> ReadTypes(OsiReader reader)
    {
        var types = new Dictionary<uint, OsirisType>();
        var count = reader.ReadUInt32();
        while (count-- > 0)
        {
            var type = new OsirisType();
            type.Read(reader);
            types.Add(type.Index, type);
        }

        return types;
    }

    private Dictionary<uint, OsirisEnum> ReadEnums(OsiReader reader)
    {
        var enums = new Dictionary<uint, OsirisEnum>();
        var count = reader.ReadUInt32();
        while (count-- > 0)
        {
            var e = new OsirisEnum();
            e.Read(reader);
            enums.Add(e.UnderlyingType, e);
        }

        return enums;
    }

    private Dictionary<uint, Node> ReadNodes(OsiReader reader)
    {
        var nodes = new Dictionary<uint, Node>();
        var count = reader.ReadUInt32();
        while (count-- > 0)
        {
            Node node = null;
            var type = reader.ReadByte();
            var nodeId = reader.ReadUInt32();
            switch ((Node.Type)type)
            {
                case Node.Type.Database:
                    node = new DatabaseNode();
                    break;

                case Node.Type.Proc:
                    node = new ProcNode();
                    break;

                case Node.Type.DivQuery:
                    node = new DivQueryNode();
                    break;

                case Node.Type.InternalQuery:
                    node = new InternalQueryNode();
                    break;

                case Node.Type.And:
                    node = new AndNode();
                    break;

                case Node.Type.NotAnd:
                    node = new NotAndNode();
                    break;

                case Node.Type.RelOp:
                    node = new RelOpNode();
                    break;

                case Node.Type.Rule:
                    node = new RuleNode();
                    break;

                case Node.Type.UserQuery:
                    node = new UserQueryNode();
                    break;

                default:
                    throw new NotImplementedException("No serializer found for this node type");
            }

            node.Read(reader);
            nodes.Add(nodeId, node);
        }

        return nodes;
    }

    private Dictionary<uint, Adapter> ReadAdapters(OsiReader reader)
    {
        var adapters = new Dictionary<uint, Adapter>();
        var count = reader.ReadUInt32();
        while (count-- > 0)
        {
            var adapter = new Adapter();
            adapter.Read(reader);
            adapters.Add(adapter.Index, adapter);
        }

        return adapters;
    }

    private Dictionary<uint, Database> ReadDatabases(OsiReader reader)
    {
        var databases = new Dictionary<uint, Database>();
        var count = reader.ReadUInt32();
        while (count-- > 0)
        {
            var database = new Database();
            database.Read(reader);
            databases.Add(database.Index, database);
        }

        return databases;
    }

    private Dictionary<uint, Goal> ReadGoals(OsiReader reader, Story story)
    {
        var goals = new Dictionary<uint, Goal>();
        var count = reader.ReadUInt32();
        while (count-- > 0)
        {
            var goal = new Goal(story);
            goal.Read(reader);
            goals.Add(goal.Index, goal);
        }

        return goals;
    }

    private Dictionary<uint, OsirisType> ReadTypes(OsiReader reader, Story story)
    {
        if (reader.Ver < OsiVersion.VerAddTypeMap)
        {
            return new Dictionary<uint, OsirisType>();
        }

        var types = ReadTypes(reader);

        // Find outermost types
        foreach (var type in types)
        {
            if (type.Value.Alias != 0)
            {
                var aliasId = type.Value.Alias;

                while (aliasId != 0 && types.ContainsKey(aliasId) && types[aliasId].Alias != 0)
                {
                    aliasId = types[aliasId].Alias;
                }

                reader.TypeAliases.Add(type.Key, aliasId);
            }
        }

        return types;
    }

    public Story Read(Stream stream)
    {
        var story = new Story();
        using (var reader = new OsiReader(stream, story))
        {
            var header = new SaveFileHeader();
            header.Read(reader);
            reader.MinorVersion = header.MinorVersion;
            reader.MajorVersion = header.MajorVersion;
            story.MinorVersion = header.MinorVersion;
            story.MajorVersion = header.MajorVersion;

            if (reader.Ver > OsiVersion.VerLastSupported)
            {
                var msg = String.Format(
                    "Osiris version v{0}.{1} unsupported; this tool supports loading up to version 1.12.",
                    reader.MajorVersion, reader.MinorVersion
                );
                throw new InvalidDataException(msg);
            }

            if (reader.Ver < OsiVersion.VerRemoveExternalStringTable)
            {
                reader.ShortTypeIds = false;
            }
            else if (reader.Ver >= OsiVersion.VerEnums)
            {
                reader.ShortTypeIds = true;
            }

            if (reader.Ver >= OsiVersion.VerScramble)
                reader.Scramble = 0xAD;

            story.Types = ReadTypes(reader, story);

            if (reader.Ver >= OsiVersion.VerExternalStringTable && reader.Ver < OsiVersion.VerRemoveExternalStringTable)
                story.ExternalStringTable = ReadStrings(reader);
            else
                story.ExternalStringTable = new List<string>();

            story.Types[0] = OsirisType.MakeBuiltin(0, "UNKNOWN");
            story.Types[1] = OsirisType.MakeBuiltin(1, "INTEGER");

            if (reader.Ver >= OsiVersion.VerEnhancedTypes)
            {
                story.Types[2] = OsirisType.MakeBuiltin(2, "INTEGER64");
                story.Types[3] = OsirisType.MakeBuiltin(3, "REAL");
                story.Types[4] = OsirisType.MakeBuiltin(4, "STRING");
                // BG3 defines GUIDSTRING in the .osi file
                if (!story.Types.ContainsKey(5))
                {
                    story.Types[5] = OsirisType.MakeBuiltin(5, "GUIDSTRING");
                }
            }
            else
            {
                story.Types[2] = OsirisType.MakeBuiltin(2, "FLOAT");
                story.Types[3] = OsirisType.MakeBuiltin(3, "STRING");

                // Populate custom type IDs for versions that had no type alias map
                if (reader.Ver < OsiVersion.VerAddTypeMap)
                {
                    for (byte typeId = 4; typeId <= 17; typeId++)
                    {
                        story.Types[typeId] = OsirisType.MakeBuiltin(typeId, $"TYPE{typeId}");
                        story.Types[typeId].Alias = 3;
                        reader.TypeAliases.Add(typeId, 3);
                    }
                }
            }

            if (reader.Ver >= OsiVersion.VerEnums)
            {
                story.Enums = ReadEnums(reader);
            }
            else
            {
                story.Enums = new Dictionary<uint, OsirisEnum>();
            }

            story.DivObjects = reader.ReadList<OsirisDivObject>();
            story.Functions = reader.ReadList<Function>();
            story.Nodes = ReadNodes(reader);
            story.Adapters = ReadAdapters(reader);
            story.Databases = ReadDatabases(reader);
            story.Goals = ReadGoals(reader, story);
            story.GlobalActions = reader.ReadList<Call>();
            story.ShortTypeIds = (bool)reader.ShortTypeIds;

            story.FunctionSignatureMap = new Dictionary<string, Function>();
            foreach (var func in story.Functions)
            {
                story.FunctionSignatureMap.Add(func.Name.Name + "/" + func.Name.Parameters.Types.Count.ToString(), func);
            }

            foreach (var node in story.Nodes)
            {
                node.Value.PostLoad(story);
            }

            return story;
        }
    }
}

public class StoryWriter
{
    private OsiWriter Writer;

    public StoryWriter()
    {

    }

    private void WriteStrings(List<string> stringTable)
    {
        Writer.Write((UInt32)stringTable.Count);
        foreach (var s in stringTable)
        {
            Writer.Write(s);
        }
    }

    private void WriteTypes(IList<OsirisType> types, Story story)
    {
        Writer.Write((UInt32)types.Count);
        foreach (var type in types)
        {
            type.Write(Writer);
            if (type.Alias != 0)
            {
                Writer.TypeAliases.Add(type.Index, story.FindBuiltinTypeId(type.Index));
            }
        }
    }

    private void WriteNodes(Dictionary<uint, Node> nodes)
    {
        Writer.Write((UInt32)nodes.Count);
        foreach (var node in nodes)
        {
            Writer.Write((byte)node.Value.NodeType());
            Writer.Write(node.Key);
            node.Value.Write(Writer);
        }
    }

    private void WriteAdapters(Dictionary<uint, Adapter> adapters)
    {
        Writer.Write((UInt32)adapters.Count);
        foreach (var adapter in adapters)
        {
            Writer.Write(adapter.Key);
            adapter.Value.Write(Writer);
        }
    }

    private void WriteDatabases(Dictionary<uint, Database> databases)
    {
        Writer.Write((UInt32)databases.Count);
        foreach (var database in databases)
        {
            Writer.Write(database.Key);
            database.Value.Write(Writer);
        }
    }

    private void WriteGoals(Dictionary<uint, Goal> goals)
    {
        Writer.Write((UInt32)goals.Count);
        foreach (var goal in goals)
        {
            goal.Value.Write(Writer);
        }
    }

    public void Write(Stream stream, Story story, bool leaveOpen)
    {
        using (Writer = new OsiWriter(stream, leaveOpen))
        {
            foreach (var node in story.Nodes)
            {
                node.Value.PreSave(story);
            }

            Writer.MajorVersion = story.MajorVersion;
            Writer.MinorVersion = story.MinorVersion;
            Writer.ShortTypeIds = story.ShortTypeIds;
            Writer.Enums = story.Enums;

            var header = new SaveFileHeader();
            if (Writer.Ver >= OsiVersion.VerExternalStringTable)
            {
                if (Writer.ShortTypeIds)
                {
                    header.Version = "Osiris save file dd. 07/09/22 00:20:54. Version 1.8.";
                }
                else
                {
                    header.Version = "Osiris save file dd. 03/30/17 07:28:20. Version 1.8.";
                }
            }
            else
            {
                header.Version = "Osiris save file dd. 02/10/15 12:44:13. Version 1.5.";
            }
            header.MajorVersion = story.MajorVersion;
            header.MinorVersion = story.MinorVersion;
            header.BigEndian = false;
            header.Unused = 0;
            // Debug flags used in D:OS EE and D:OS 2
            header.DebugFlags = 0x000C10A0;
            header.Write(Writer);

            if (Writer.Ver > OsiVersion.VerLastSupported)
            {
                var msg = String.Format(
                    "Osiris version v{0}.{1} unsupported; this tool supports saving up to version 1.11.",
                    Writer.MajorVersion, Writer.MinorVersion
                );
                throw new InvalidDataException(msg);
            }

            if (Writer.Ver >= OsiVersion.VerScramble)
                Writer.Scramble = 0xAD;

            if (Writer.Ver >= OsiVersion.VerAddTypeMap)
            {
                List<OsirisType> types;
                if (Writer.Ver >= OsiVersion.VerEnums)
                {
                    // BG3 Patch 9 writes all types to the blob except type 0
                    types = story.Types.Values.Where(t => t.Name != "UNKNOWN").ToList();
                }
                else
                {
                    // Don't export builtin types, only externally declared ones
                    types = story.Types.Values.Where(t => !t.IsBuiltin).ToList();
                }

                WriteTypes(types, story);
            }

            if (Writer.Ver >= OsiVersion.VerEnums)
            {
                Writer.WriteList(story.Enums.Values.ToList());
            }

            // TODO: regenerate string table?
            if (Writer.Ver >= OsiVersion.VerExternalStringTable && Writer.Ver < OsiVersion.VerRemoveExternalStringTable)
                WriteStrings(story.ExternalStringTable);

            Writer.WriteList(story.DivObjects);
            Writer.WriteList(story.Functions);
            WriteNodes(story.Nodes);
            WriteAdapters(story.Adapters);
            WriteDatabases(story.Databases);
            WriteGoals(story.Goals);
            Writer.WriteList(story.GlobalActions);

            foreach (var node in story.Nodes)
            {
                node.Value.PostSave(story);
            }
        }
    }
}
