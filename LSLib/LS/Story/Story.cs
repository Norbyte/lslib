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
                        "Osiris version v{0}.{1} unsupported; this tool supports versions up to v1.11.",
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

                List<string> stringTable;
                if (reader.MajorVersion > 1 || (reader.MajorVersion == 1 && reader.MinorVersion >= 11))
                    stringTable = ReadStrings(reader);
                else
                    stringTable = new List<string>();
                
                story.Types[0] = new OsirisType();
                story.Types[0].Index = 0;
                story.Types[0].Name = "UNKNOWN";

                if (reader.MajorVersion > 1 || (reader.MajorVersion == 1 && reader.MinorVersion >= 11))
                {
                    story.Types[1] = new OsirisType();
                    story.Types[1].Index = 1;
                    story.Types[1].Name = "INTEGER";
                    story.Types[2] = new OsirisType();
                    story.Types[2].Index = 2;
                    story.Types[2].Name = "INTEGER64";
                    story.Types[3] = new OsirisType();
                    story.Types[3].Index = 3;
                    story.Types[3].Name = "REAL";
                    story.Types[4] = new OsirisType();
                    story.Types[4].Index = 4;
                    story.Types[4].Name = "STRING";
                    story.Types[5] = new OsirisType();
                    story.Types[5].Index = 5;
                    story.Types[5].Name = "GUIDSTRING";
                }
                else
                {
                    story.Types[1] = new OsirisType();
                    story.Types[1].Index = 1;
                    story.Types[1].Name = "INTEGER";
                    story.Types[2] = new OsirisType();
                    story.Types[2].Index = 2;
                    story.Types[2].Name = "FLOAT";
                    story.Types[3] = new OsirisType();
                    story.Types[3].Index = 3;
                    story.Types[3].Name = "STRING";
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
                    if (node.Value.DatabaseRef.IsValid())
                    {
                        var database = story.Databases[node.Value.DatabaseRef.DatabaseIndex];
                        if (database.OwnerNode != null)
                        {
                            throw new InvalidDataException("A database cannot be assigned to multiple database nodes!");
                        }

                        database.OwnerNode = node.Value;
                    }

                    if (node.Value is RuleNode)
                    {
                        // Remove the __DEF__ postfix that is added to the end of Query nodes
                        var rule = node.Value as RuleNode;
                        if (rule.IsQuery)
                        {
                            var ruleRoot = rule.GetRoot(story);
                            if (ruleRoot.Name != null && 
                                ruleRoot.Name.Length > 7 &&
                                ruleRoot.Name.Substring(ruleRoot.Name.Length - 7) == "__DEF__")
                            {
                                ruleRoot.Name = ruleRoot.Name.Substring(0, ruleRoot.Name.Length - 7);
                            }
                        }
                    }

                    if (node.Value is DataNode)
                    {
                        var data = node.Value as DataNode;
                        foreach (var reference in data.ReferencedBy)
                        {
                            if (reference.NodeRef.IsValid())
                            {
                                var ruleNode = story.Nodes[reference.NodeRef.NodeIndex];
                                if (reference.GoalId > 0 &&
                                    ruleNode is RuleNode)
                                {
                                    (ruleNode as RuleNode).DerivedGoalId = reference.GoalId;
                                }
                            }
                        }
                    }

                    if (node.Value is TreeNode)
                    {
                        var tree = node.Value as TreeNode;
                        if (tree.NextNode.NodeRef.IsValid())
                        {
                            var nextNode = story.Nodes[tree.NextNode.NodeRef.NodeIndex];
                            if (nextNode is RuleNode)
                            {
                                (nextNode as RuleNode).DerivedGoalId = tree.NextNode.GoalId;
                            }
                        }
                    }
                    
                    if (node.Value is RelNode)
                    {
                        var rel = node.Value as RelNode;
                        if (rel.AdapterRef.IsValid())
                        {
                            var adapter = story.Adapters[rel.AdapterRef.AdapterIndex];
                            if (adapter.OwnerNode != null)
                            {
                                throw new InvalidDataException("An adapter cannot be assigned to multiple join/rel nodes!");
                            }

                            adapter.OwnerNode = node.Value;
                        }
                    }
                    else if (node.Value is JoinNode)
                    {
                        var join = node.Value as JoinNode;
                        if (join.Adapter1Ref.IsValid())
                        {
                            var adapter = story.Adapters[join.Adapter1Ref.AdapterIndex];
                            if (adapter.OwnerNode != null)
                            {
                                throw new InvalidDataException("An adapter cannot be assigned to multiple join/rel nodes!");
                            }

                            adapter.OwnerNode = node.Value;
                        }

                        if (join.Adapter2Ref.IsValid())
                        {
                            var adapter = story.Adapters[join.Adapter2Ref.AdapterIndex];
                            if (adapter.OwnerNode != null)
                            {
                                throw new InvalidDataException("An adapter cannot be assigned to multiple join/rel nodes!");
                            }

                            adapter.OwnerNode = node.Value;
                        }
                    }
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

        private void WriteTypes(Dictionary<uint, OsirisType> types)
        {
            Writer.Write((UInt32)types.Count);
            foreach (var type in types)
            {
                type.Value.Write(Writer);
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
                Writer.MajorVersion = story.MajorVersion;
                Writer.MinorVersion = story.MinorVersion;

                var header = new OsirisHeader();
                header.Version = "Osiris save file dd. 02/10/15 12:44:13. Version 1.5.";
                header.MajorVersion = story.MajorVersion;
                header.MinorVersion = story.MinorVersion;
                header.BigEndian = false;
                header.Unused = 0;
                // Debug flags used in D:OS EE
                header.DebugFlags = 0x000CA010;
                header.Write(Writer);

                if (Writer.MajorVersion > 1 || (Writer.MajorVersion == 1 && Writer.MinorVersion > 7))
                {
                    var msg = String.Format(
                        "Osiris version v{0}.{1} unsupported; this tool supports versions up to v1.7.",
                        Writer.MajorVersion, Writer.MinorVersion
                    );
                    throw new InvalidDataException(msg);
                }

                if (Writer.MajorVersion > 1 || (Writer.MajorVersion == 1 && Writer.MinorVersion >= 4))
                    Writer.Scramble = 0xAD;


                if (Writer.MajorVersion > 1 || (Writer.MajorVersion == 1 && Writer.MinorVersion >= 5))
                    WriteTypes(story.Types);

                Writer.WriteList(story.DivObjects);
                Writer.WriteList(story.Functions);
                WriteNodes(story.Nodes);
                WriteAdapters(story.Adapters);
                WriteDatabases(story.Databases);
                WriteGoals(story.Goals);
                Writer.WriteList(story.GlobalActions);
            }
        }
    }
}
