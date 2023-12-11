using LSLib.LS.Story;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ConverterApp;

class DatabaseDumper : IDisposable
{
    private StreamWriter Writer;

    public bool DumpUnnamedDbs { get; set; }

    public DatabaseDumper(Stream outputStream)
    {
        Writer = new StreamWriter(outputStream, Encoding.UTF8);
        DumpUnnamedDbs = false;
    }

    public void Dispose()
    {
        Writer.Dispose();
    }
    
    private void DumpFact(Story story, Fact fact)
    {
        Writer.Write("(");
        for (var i = 0; i < fact.Columns.Count; i++)
        {
            fact.Columns[i].DebugDump(Writer, story);
            if (i + 1 < fact.Columns.Count)
            {
                Writer.Write(", ");
            }
        }
        Writer.WriteLine(")");
    }

    public void DumpDatabase(Story story, Database database)
    {
        if (database.OwnerNode != null)
        {
            if (database.OwnerNode.Name.Length > 0)
            {
                Writer.Write($"Database '{database.OwnerNode.Name}'");
            }
            else
            {
                Writer.Write($"Database #{database.Index} <{database.OwnerNode.TypeName()}>");
            }
        }
        else
        {
            Writer.Write($"Database #{database.Index}");
        }

        var types = String.Join(", ", database.Parameters.Types.Select(ty => story.Types[ty].Name));
        Writer.WriteLine($" ({types}):");

        foreach (var fact in database.Facts)
        {
            Writer.Write("\t");
            DumpFact(story, fact);
        }
    }

    public void DumpAll(Story story)
    {
        Writer.WriteLine(" === DUMP OF DATABASES === ");
        foreach (var db in story.Databases)
        {
            if (DumpUnnamedDbs || (db.Value.OwnerNode != null && db.Value.OwnerNode.Name.Length > 0))
            {
                DumpDatabase(story, db.Value);
                Writer.WriteLine("");
            }
        }
    }
}
