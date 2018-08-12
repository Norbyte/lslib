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
        private Logger Logger;
        private Compiler Compiler = new Compiler();
        private List<string> ScriptPaths = new List<string>();

        public ModCompiler(Logger logger)
        {
            Logger = logger;
        }

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
            Logger.CompilationStarted();
            var asts = new Dictionary<String, ASTGoal>();
            var goalLoader = new IRGenerator(Compiler.Context);

            Logger.TaskStarted("Generating AST");
            foreach (var path in ScriptPaths)
            {
                var goalName = Path.GetFileNameWithoutExtension(path);
                var goalAst = goalLoader.ParseGoal(path);
                asts.Add(goalName, goalAst);
            }
            var orderedGoalAsts = asts.OrderBy(rule => rule.Key).ToList();
            Logger.TaskFinished();

            Logger.TaskStarted("Generating IR");
            var irs = new List<IRGoal>();
            foreach (var ast in orderedGoalAsts)
            {
                var goalIr = goalLoader.GenerateGoalIR(ast.Value);
                goalIr.Name = ast.Key;
                irs.Add(goalIr);
                Compiler.AddGoal(goalIr);
            }
            Logger.TaskFinished();


            Logger.TaskStarted("Propagating rule types");
            // TODO - this should be changed to dynamic pass count detection
            Compiler.PropagateRuleTypes();
            Compiler.PropagateRuleTypes();
            Compiler.PropagateRuleTypes();
            Logger.TaskFinished();

            Logger.TaskStarted("Checking for unresolved references");
            Compiler.VerifyIR();
            Logger.TaskFinished();

            bool hasErrors = false;
            foreach (var message in Compiler.Context.Log.Log)
            {
                Logger.CompilationDiagnostic(message);
                if (message.Level == MessageLevel.Error)
                {
                    hasErrors = true;
                }
            }

            if (!hasErrors)
            {
                Logger.TaskStarted("Generating story nodes");
                var emitter = new StoryEmitter(Compiler.Context);
                var story = emitter.EmitStory();
                Logger.TaskFinished();

                Logger.TaskStarted("Saving story binary");
                using (var file = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    var writer = new StoryWriter();
                    writer.Write(file, story);
                }
                Logger.TaskFinished();
            }

            Logger.CompilationFinished(!hasErrors);
            return !hasErrors;
        }
    }
}
