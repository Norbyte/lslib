using LSLib.LS;
using LSLib.LS.Save;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConverterApp
{
    class VariableDumper : IDisposable
    {
        private FileStream File;
        private StreamWriter Writer;
        private OsirisVariableHelper VariablesHelper;

        public VariableDumper(string outputPath)
        {
            File = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            Writer = new StreamWriter(File, Encoding.UTF8);
        }

        public void Dispose()
        {
            Writer.Dispose();
            File.Dispose();
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
                    key += " (Player " + (string)playerData.Attributes["Name"].Value + ")";
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
            var variables = variableMgr.GetAll();
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

        public void Dump(Resource resource)
        {
            Node osiHelper = resource.Regions["OsirisVariableHelper"];
            VariablesHelper = new OsirisVariableHelper();
            VariablesHelper.Load(osiHelper);

            var globalVarsNode = osiHelper.Children["VariableManager"][0];
            Writer.WriteLine(" === DUMP OF GLOBALS === ");
            DumpGlobals(globalVarsNode);

            Writer.WriteLine();
            Writer.WriteLine(" === DUMP OF CHARACTERS === ");
            var characters = resource.Regions["Characters"].Children["CharacterFactory"][0].Children["Characters"][0].Children["Character"];
            foreach (var character in characters)
            {
                DumpCharacter(character);
            }

            Writer.WriteLine();
            Writer.WriteLine(" === DUMP OF ITEMS === ");
            var items = resource.Regions["Items"].Children["ItemFactory"][0].Children["Items"][0].Children["Item"];
            foreach (var item in items)
            {
                DumpItem(item);
            }
        }
    }
}
