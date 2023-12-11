using LSLib.LS.Story.Compiler;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LSTools.StoryCompiler;

public interface Logger
{
    void CompilationStarted();
    void CompilationFinished(bool succeeded);

    void TaskStarted(string name);
    void TaskFinished();

    void CompilationDiagnostic(Diagnostic message);
}

public class ConsoleLogger : Logger
{
    private Stopwatch compilationTimer = new Stopwatch();
    private Stopwatch taskTimer = new Stopwatch();

    public void CompilationStarted()
    {
        compilationTimer.Restart();
    }

    public void CompilationFinished(bool succeeded)
    {
        compilationTimer.Stop();
        Console.WriteLine("Compilation took: {0} ms", compilationTimer.Elapsed.Seconds * 1000 + compilationTimer.Elapsed.Milliseconds);
    }

    public void TaskStarted(string name)
    {
        Console.Write(name + " ... ");
        taskTimer.Restart();
    }

    public void TaskFinished()
    {
        taskTimer.Stop();
        Console.WriteLine("{0} ms", taskTimer.Elapsed.Seconds * 1000 + taskTimer.Elapsed.Milliseconds);
    }

    public void CompilationDiagnostic(Diagnostic message)
    {
        switch (message.Level)
        {
            case MessageLevel.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("ERR! ");
                break;

            case MessageLevel.Warning:
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("WARN ");
                break;
        }

        if (message.Location != null)
        {
            Console.Write($"{message.Location.FileName}:{message.Location.StartLine}:{message.Location.StartColumn}: ");
        }

        Console.WriteLine("[{0}] {1}", message.Code, message.Message);
        Console.ResetColor();
    }
}

public class JsonLogConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return
            objectType.Equals(typeof(JsonLoggerOutput))
            || objectType.Equals(typeof(Diagnostic));
    }
    
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is JsonLoggerOutput)
        {
            var output = value as JsonLoggerOutput;
            writer.WriteStartObject();

            writer.WritePropertyName("successful");
            writer.WriteValue(output.Succeeded);

            writer.WritePropertyName("stats");
            writer.WriteStartObject();
            foreach (var time in output.StepTimes)
            {
                writer.WritePropertyName(time.Key);
                writer.WriteValue(time.Value);
            }
            writer.WriteEndObject();

            writer.WritePropertyName("messages");
            writer.WriteStartArray();
            foreach (var diagnostic in output.Diagnostics)
            {
                serializer.Serialize(writer, diagnostic);
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
        else if (value is Diagnostic)
        {
            var diagnostic = value as Diagnostic;
            writer.WriteStartObject();

            writer.WritePropertyName("location");
            if (diagnostic.Location != null)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("file");
                writer.WriteValue(diagnostic.Location.FileName);

                writer.WritePropertyName("StartLine");
                writer.WriteValue(diagnostic.Location.StartLine);

                writer.WritePropertyName("StartColumn");
                writer.WriteValue(diagnostic.Location.StartColumn);

                writer.WritePropertyName("EndLine");
                writer.WriteValue(diagnostic.Location.EndLine);

                writer.WritePropertyName("EndColumn");
                writer.WriteValue(diagnostic.Location.EndColumn);

                writer.WriteEndObject();
            }
            else
            {
                writer.WriteNull();
            }

            writer.WritePropertyName("code");
            writer.WriteValue(diagnostic.Code);

            writer.WritePropertyName("level");
            writer.WriteValue(diagnostic.Level);

            writer.WritePropertyName("message");
            writer.WriteValue(diagnostic.Message);

            writer.WriteEndObject();
        }
        else
        {
            throw new InvalidOperationException();
        }
    }
}

class JsonLoggerOutput
{
    public Dictionary<string, int> StepTimes = new Dictionary<string, int>();
    public List<Diagnostic> Diagnostics = new List<Diagnostic>();
    public bool Succeeded;
}

class JsonLogger : Logger
{
    private Stopwatch TaskTimer = new Stopwatch();
    private JsonLoggerOutput Output = new JsonLoggerOutput();
    private String CurrentStep;

    public void CompilationStarted()
    {
    }

    public void CompilationFinished(bool succeeded)
    {
        Output.Succeeded = succeeded;
        var serializer = new JsonSerializer();
        serializer.Converters.Add(new JsonLogConverter());

        using (var memory = new MemoryStream())
        {
            using (var stream = new StreamWriter(memory))
            using (JsonWriter writer = new JsonTextWriter(stream))
            {
                serializer.Serialize(writer, Output);
            }

            var json = Encoding.UTF8.GetString(memory.ToArray());
            Console.Write(json);
        }
    }

    public void TaskStarted(string name)
    {
        CurrentStep = name;
        TaskTimer.Restart();
    }

    public void TaskFinished()
    {
        TaskTimer.Stop();
        Output.StepTimes.Add(CurrentStep, TaskTimer.Elapsed.Seconds * 60 + TaskTimer.Elapsed.Milliseconds);
    }

    public void CompilationDiagnostic(Diagnostic message)
    {
        Output.Diagnostics.Add(message);
    }
}
