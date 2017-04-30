using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSLib.LS.Story
{
    public class Story
    {
        public byte MinorVersion;
        public byte MajorVersion;
        public OsirisHeader Header;
        public Dictionary<uint, OsirisType> Types;
        public List<OsirisDivObject> DivObjects;
        public List<Function> Functions;
        public Dictionary<uint, Node> Nodes;
        public Dictionary<uint, Adapter> Adapters;
        public Dictionary<uint, Database> Databases;
        public Dictionary<uint, Goal> Goals;
        public List<Call> GlobalActions;
        public List<string> ExternalStringTable;

        public void DebugDump(TextWriter writer)
        {
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
                var adapterId = reader.ReadUInt32();
                var adapter = new Adapter();
                adapter.Read(reader);
                adapters.Add(adapterId, adapter);
            }

            return adapters;
        }

        private Dictionary<uint, Database> ReadDatabases(OsiReader reader)
        {
            var databases = new Dictionary<uint, Database>();
            var count = reader.ReadUInt32();
            while (count-- > 0)
            {
                var databaseId = reader.ReadUInt32();
                var database = new Database();
                database.Read(reader);
                databases.Add(databaseId, database);
            }

            return databases;
        }

        private Dictionary<uint, Goal> ReadGoals(OsiReader reader)
        {
            var goals = new Dictionary<uint, Goal>();
            var count = reader.ReadUInt32();
            while (count-- > 0)
            {
                var goal = new Goal();
                goal.Read(reader);
                goals.Add(goal.Index, goal);
            }

            return goals;
        }

        public Story Read(Stream stream)
        {
            using (var reader = new OsiReader(stream))
            {
                var story = new Story();
                var header = new OsirisHeader();
                header.Read(reader);
                reader.MinorVersion = header.MinorVersion;
                reader.MajorVersion = header.MajorVersion;
                story.MinorVersion = header.MinorVersion;
                story.MajorVersion = header.MajorVersion;

                if (reader.MajorVersion > 1 || (reader.MajorVersion == 1 && reader.MinorVersion > 11))
                {
                    var msg = String.Format(
                        "Osiris version v{0}.{1} unsupported; this tool supports loading up to version 1.11.",
                        reader.MajorVersion, reader.MinorVersion
                    );
                    throw new InvalidDataException(msg);
                }

                if (reader.MajorVersion > 1 || (reader.MajorVersion == 1 && reader.MinorVersion >= 4))
                    reader.Scramble = 0xAD;

                if (reader.MajorVersion > 1 || (reader.MajorVersion == 1 && reader.MinorVersion >= 5))
                {
                    story.Types = ReadTypes(reader);
                    foreach (var type in story.Types)
                    {
                        if (type.Value.Alias != 0)
                        {
                            reader.TypeAliases.Add(type.Key, type.Value.Alias);
                        }
                    }
                }
                else
                    story.Types = new Dictionary<uint, OsirisType>();
                
                if (reader.MajorVersion > 1 || (reader.MajorVersion == 1 && reader.MinorVersion >= 11))
                    story.ExternalStringTable = ReadStrings(reader);
                else
                    story.ExternalStringTable = new List<string>();

                story.Types[0] = OsirisType.MakeBuiltin(0, "UNKNOWN");
                story.Types[1] = OsirisType.MakeBuiltin(1, "INTEGER");

                if (reader.MajorVersion > 1 || (reader.MajorVersion == 1 && reader.MinorVersion >= 10))
                {
                    story.Types[2] = OsirisType.MakeBuiltin(2, "INTEGER64");
                    story.Types[3] = OsirisType.MakeBuiltin(3, "REAL");
                    story.Types[4] = OsirisType.MakeBuiltin(4, "STRING");
                    story.Types[5] = OsirisType.MakeBuiltin(5, "GUIDSTRING");
                }
                else
                {
                    story.Types[2] = OsirisType.MakeBuiltin(2, "FLOAT");
                    story.Types[3] = OsirisType.MakeBuiltin(3, "STRING");
                }

                story.DivObjects = reader.ReadList<OsirisDivObject>();
                story.Functions = reader.ReadList<Function>();
                story.Nodes = ReadNodes(reader);
                story.Adapters = ReadAdapters(reader);
                story.Databases = ReadDatabases(reader);
                story.Goals = ReadGoals(reader);
                story.GlobalActions = reader.ReadList<Call>();

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

        private void WriteTypes(IList<OsirisType> types)
        {
            Writer.Write((UInt32)types.Count);
            foreach (var type in types)
            {
                type.Write(Writer);
                if (type.Alias != 0
                    && (Writer.MajorVersion > 1 || (Writer.MajorVersion == 1 && Writer.MinorVersion >= 9)))
                {
                    Writer.TypeAliases.Add(type.Index, type.Alias);
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

        public void Write(Stream stream, Story story)
        {
            using (Writer = new OsiWriter(stream))
            {
                foreach (var node in story.Nodes)
                {
                    node.Value.PreSave(story);
                }

                Writer.MajorVersion = story.MajorVersion;
                Writer.MinorVersion = story.MinorVersion;

                var header = new OsirisHeader();
                if (Writer.MajorVersion > 1 || (Writer.MajorVersion == 1 && Writer.MinorVersion >= 11))
                {
                    header.Version = "Osiris save file dd. 03/30/17 07:28:20. Version 1.8.";
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

                if (Writer.MajorVersion > 1 || (Writer.MajorVersion == 1 && Writer.MinorVersion > 11))
                {
                    var msg = String.Format(
                        "Osiris version v{0}.{1} unsupported; this tool supports saving up to version 1.11.",
                        Writer.MajorVersion, Writer.MinorVersion
                    );
                    throw new InvalidDataException(msg);
                }

                if (Writer.MajorVersion > 1 || (Writer.MajorVersion == 1 && Writer.MinorVersion >= 4))
                    Writer.Scramble = 0xAD;

                if (Writer.MajorVersion > 1 || (Writer.MajorVersion == 1 && Writer.MinorVersion >= 5))
                {
                    // Don't export builtin types, only externally declared ones
                    var types = story.Types.Values.Where(t => !t.IsBuiltin).ToList();
                    WriteTypes(types);
                }

                // TODO: regenerate string table?
                if (Writer.MajorVersion > 1 || (Writer.MajorVersion == 1 && Writer.MinorVersion >= 11))
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
}
