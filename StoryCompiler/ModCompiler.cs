using LSLib.LS.Story;
using LSLib.LS.Story.Compiler;
using LSLib.LS.Story.GoalParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

        public void SetWarningOptions(Dictionary<string, bool> options)
        {
            foreach (var option in options)
            {
                Compiler.Context.Log.WarningSwitches[option.Key] = option.Value;
            }
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
            Console.WriteLine("{0}s {1} ms", sw.Elapsed.Seconds, sw.Elapsed.Milliseconds);

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
            Console.WriteLine("{0}s {1} ms", sw.Elapsed.Seconds, sw.Elapsed.Milliseconds);


            Console.Write("Propagating rule types ... ");
            sw.Restart();
            // TODO - this should be changed to dynamic pass count detection
            Compiler.PropagateRuleTypes();
            Compiler.PropagateRuleTypes();
            Compiler.PropagateRuleTypes();
            sw.Stop();
            Console.WriteLine("{0}s {1} ms", sw.Elapsed.Seconds, sw.Elapsed.Milliseconds);

            Console.Write("Checking for unresolved references ... ");
            sw.Restart();
            Compiler.VerifyIR();
            sw.Stop();

            Console.WriteLine("{0}s {1} ms", sw.Elapsed.Seconds, sw.Elapsed.Milliseconds);

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
            Console.WriteLine("{0}s {1} ms", sw.Elapsed.Seconds, sw.Elapsed.Milliseconds);

            Console.Write("Saving story binary ... ");
            sw.Restart();
            using (var file = new FileStream("StoryDebug.osi", FileMode.Create, FileAccess.Write))
            {
                var writer = new StoryWriter();
                writer.Write(file, story);
            }
            sw.Stop();
            Console.WriteLine("{0}s {1} ms", sw.Elapsed.Seconds, sw.Elapsed.Milliseconds);
            return true;
        }
    }
}
