using LSLib.LS;
using LSLib.LS.Save;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ConverterApp;

class VariableDumper : IDisposable
{
    private StreamWriter Writer;
    private Resource Rsrc;
    private OsirisVariableHelper VariablesHelper;

    public bool IncludeDeletedVars { get; set; }
    public bool IncludeLocalScopes { get; set; }

    public VariableDumper(Stream outputStream)
    {
        Writer = new StreamWriter(outputStream, Encoding.UTF8);
        IncludeDeletedVars = false;
        IncludeLocalScopes = false;
    }

    public void Dispose()
    {
        Writer.Dispose();
    }

    private void DumpCharacter(Node characterNode)
    {
        if (characterNode.Children.TryGetValue("VariableManager", out var varNodes))
        {
            var characterVars = new VariableManager(VariablesHelper);
            characterVars.Load(varNodes[0]);

            var key = characterNode.Attributes["CurrentTemplate"].Value.ToString();
            if (characterNode.Attributes.ContainsKey("Stats"))
            {
                key += " (" + (string)characterNode.Attributes["Stats"].Value + ")";
            }
            else if (characterNode.Children.ContainsKey("PlayerData"))
            {
                var playerData = characterNode.Children["PlayerData"][0]
                    .Children["PlayerCustomData"][0];
                if (playerData.Attributes.TryGetValue("Name", out NodeAttribute name))
                {
                    key += " (Player " + (string)name.Value + ")";
                }
            }

            DumpVariables(key, characterVars);
        }
    }

    private void DumpItem(Node itemNode)
    {
        if (itemNode.Children.TryGetValue("VariableManager", out var varNodes))
        {
            var itemVars = new VariableManager(VariablesHelper);
            itemVars.Load(varNodes[0]);

            var key = itemNode.Attributes["CurrentTemplate"].Value.ToString();
            if (itemNode.Attributes.ContainsKey("Stats"))
            {
                key += " (" + (string)itemNode.Attributes["Stats"].Value + ")";
            }

            DumpVariables(key, itemVars);
        }
    }

    private void DumpGlobals(Node globalVarsNode)
    {
        var vars = new VariableManager(VariablesHelper);
        vars.Load(globalVarsNode);
        DumpVariables("Globals", vars);
    }

    private void DumpVariables(string label, VariableManager variableMgr)
    {
        var variables = variableMgr.GetAll(IncludeDeletedVars);

        if (!IncludeLocalScopes)
        {
            variables = variables
                .Where(kv => !kv.Key.Contains('.'))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        if (variables.Count > 0)
        {
            Writer.WriteLine($"{label}:");
            foreach (var kv in variables)
            {
                Writer.WriteLine($"\t{kv.Key}: {kv.Value}");
            }

            Writer.WriteLine("");
        }
    }

    public bool Load(Resource resource)
    {
        Rsrc = resource;
        Node osiHelper = resource.Regions["OsirisVariableHelper"];
        if (!osiHelper.Children.ContainsKey("IdentifierTable"))
        {
            return false;
        }

        VariablesHelper = new OsirisVariableHelper();
        VariablesHelper.Load(osiHelper);
        return true;
    }

    public void DumpGlobals()
    {
        Node osiHelper = Rsrc.Regions["OsirisVariableHelper"];
        var globalVarsNode = osiHelper.Children["VariableManager"][0];

        Writer.WriteLine(" === DUMP OF GLOBALS === ");
        DumpGlobals(globalVarsNode);
    }

    public void DumpCharacters()
    {
        Writer.WriteLine();
        Writer.WriteLine(" === DUMP OF CHARACTERS === ");
        var characters = Rsrc.Regions["Characters"].Children["CharacterFactory"][0].Children["Characters"][0].Children["Character"];
        foreach (var character in characters)
        {
            DumpCharacter(character);
        }
    }

    public void DumpItems()
    {
        Writer.WriteLine();
        Writer.WriteLine(" === DUMP OF ITEMS === ");
        var items = Rsrc.Regions["Items"].Children["ItemFactory"][0].Children["Items"][0].Children["Item"];
        foreach (var item in items)
        {
            DumpItem(item);
        }
    }
}
