using LSLib.LS.Story;
using LSLib.LS.Story.GoalParser;
using LSLib.LS.Story.HeaderParser;
using LSLib.LS.Story.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LSTools.StoryCompiler
{
    class ModCompiler
    {
        private Compiler Compiler = new Compiler();
        private List<string> ScriptPaths = new List<string>();

        private static void EnumerateFiles(List<string> paths, string rootPath, string currentPath, string extension)
        {
            foreach (string filePath in Directory.GetFiles(currentPath))
            {
                var fileExtension = Path.GetExtension(filePath);
                if (fileExtension.ToLower() == extension)
                {
                    var relativePath = filePath.Substring(rootPath.Length);
                    if (relativePath[0] == '/' || relativePath[0] == '\\')
                    {
                        relativePath = relativePath.Substring(1);
                    }

                    paths.Add(relativePath);
                }
            }

            foreach (string directoryPath in Directory.GetDirectories(currentPath))
            {
                EnumerateFiles(paths, rootPath, directoryPath, extension);
            }
        }

        private static void EnumerateScripts(List<string> paths, string rootPath)
        {
            var localPaths = new List<string>();
            EnumerateFiles(localPaths, rootPath, rootPath, ".txt");
            foreach (var path in localPaths)
            {
                paths.Add(rootPath + "\\" + path);
            }
        }

        public void LoadStoryHeaders(string path)
        {
            var hdrLoader = new StoryHeaderLoader(Compiler.Context);
            var declarations = hdrLoader.ParseHeader(path);
            if (declarations == null)
            {
                throw new Exception("Failed to parse story header file");
            }

            hdrLoader.LoadHeader(declarations);
        }

        public void AddMod(string path)
        {
            EnumerateScripts(ScriptPaths, path);
        }

        public bool Compile(string outputPath)
        {
            Stopwatch sw = new Stopwatch();
            var asts = new Dictionary<String, ASTGoal>();
            var goalLoader = new IRGenerator(Compiler.Context);

            Console.Write("Generating AST ... ");
            sw.Restart();
            foreach (var path in ScriptPaths)
            {
                var goalName = Path.GetFileNameWithoutExtension(path);
                var goalAst = goalLoader.ParseGoal(path);
                asts.Add(goalName, goalAst);
            }
            var orderedGoalAsts = asts.OrderBy(rule => rule.Key).ToList();
            sw.Stop();
            Console.WriteLine("{0} ms", sw.Elapsed.Milliseconds);
            
            Console.Write("Generating IR ... ");
            sw.Restart();
            var irs = new List<IRGoal>();
            foreach (var ast in orderedGoalAsts)
            {
                var goalIr = goalLoader.GenerateGoalIR(ast.Value);
                goalIr.Name = ast.Key;
                irs.Add(goalIr);
                Compiler.AddGoal(goalIr);
            }
            sw.Stop();
            Console.WriteLine("{0} ms", sw.Elapsed.Milliseconds);


            Console.Write("Propagating rule types ... ");
            sw.Restart();
            // TODO - this should be changed to dynamic pass count detection
            Compiler.PropagateRuleTypes();
            Compiler.PropagateRuleTypes();
            Compiler.PropagateRuleTypes();
            sw.Stop();
            Console.WriteLine("{0} ms", sw.Elapsed.Milliseconds);

            Console.Write("Checking for unresolved references ... ");
            sw.Restart();
            Compiler.VerifyIR();
            sw.Stop();

            Console.WriteLine("{0} ms", sw.Elapsed.Milliseconds);

            bool hasErrors = false;
            foreach (var message in Compiler.Context.Log.Log)
            {
                switch (message.Level)
                {
                    case MessageLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("ERR! ");
                        hasErrors = true;
                        break;

                    case MessageLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("WARN ");
                        break;
                }

                Console.WriteLine("[{0}] {1}", message.Code, message.Message);
                Console.ResetColor();
            }

            if (hasErrors)
            {
                return false;
            }

            Console.Write("Generating story nodes ... ");
            sw.Restart();
            var emitter = new StoryEmitter(Compiler.Context);
            var story = emitter.EmitStory();
            sw.Stop();
            Console.WriteLine("{0} ms", sw.Elapsed.Milliseconds);

            Console.Write("Saving story binary ... ");
            sw.Restart();
            using (var file = new FileStream("StoryDebug.osi", FileMode.Create, FileAccess.Write))
            {
                var writer = new StoryWriter();
                writer.Write(file, story);
            }
            sw.Stop();
            Console.WriteLine("{0} ms", sw.Elapsed.Milliseconds);
            return true;
        }
    }

    class Program
    {
        static void DebugDump(string storyPath, string debugPath)
        {
            Story story;
            using (var file = new FileStream(storyPath, FileMode.Open, FileAccess.Read))
            {
                var reader = new StoryReader();
                story = reader.Read(file);
            }
            
            using (var debugFile = new FileStream(debugPath, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(debugFile))
                {
                    story.DebugDump(writer);
                }
            }
        }

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: StoryCompiler <output path> <mod path 1> [<mod path 2> ...]");
                Environment.Exit(1);
                return;
            }

            var storyHeadersPath = args[1] + @"\Story\RawFiles\story_header.div";
            if (!File.Exists(storyHeadersPath))
            {
                Console.WriteLine("Story header file not found: {0}", storyHeadersPath);
                Environment.Exit(2);
                return;
            }

            var modCompiler = new ModCompiler();
            modCompiler.LoadStoryHeaders(storyHeadersPath);
            foreach (var modPath in args.Skip(1))
            {
                modCompiler.AddMod(modPath + @"\Story\RawFiles\Goals");
            }

            if (!modCompiler.Compile(args[0]))
            {
                Environment.Exit(3);
            }
        }
    }
}
