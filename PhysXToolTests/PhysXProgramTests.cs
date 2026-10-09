using PhysXTool;

namespace Tests;

public static class PhysXProgramPipelineTests
{
    [Fact]
    public static void Main_InvalidArgumentsCount_ReturnsExitCodeOneAndPrintsUsage()
    {
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        
        ReadOnlySpan<string> invalidArgs = ["onlyOneArg.xml"];
        int exitCode = Program.Main(invalidArgs.ToArray());
        #pragma warning disable xUnit2013
        Assert.Equal(1, exitCode);
        #pragma warning restore xUnit2013
        
        var output = stringWriter.ToString();
        Assert.Contains(ResourceStrings.UsageMessage, output, StringComparison.Ordinal);
    }

    [Fact]
    public static void Main_UnsupportedInputExtension_ReturnsExitCodeOneAndPrintsDataRejectionError()
    {
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        
        ReadOnlySpan<string> badArgs = ["input.txt", "output.xml"];
        int exitCode = Program.Main(badArgs.ToArray());
        #pragma warning disable xUnit2013
        Assert.Equal(1, exitCode);
        #pragma warning restore xUnit2013
        
        var output = stringWriter.ToString();
        Assert.Contains("PIPELINE DATA REJECTION ERROR", output, StringComparison.Ordinal);
        Assert.Contains(ResourceStrings.InvalidInputMessage, output, StringComparison.Ordinal);
    }

    [Fact]
    public static void Main_UnsupportedOutputExtension_ReturnsExitCodeOneAndPrintsDataRejectionError()
    {
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        
        ReadOnlySpan<string> badArgs = ["input.xml", "output.json"];
        int exitCode = Program.Main(badArgs.ToArray());
        #pragma warning disable xUnit2013
        Assert.Equal(1, exitCode);
        #pragma warning restore xUnit2013
        
        var output = stringWriter.ToString();
        Assert.Contains("PIPELINE DATA REJECTION ERROR", output, StringComparison.Ordinal);
        Assert.Contains(ResourceStrings.InvalidOutputMessage, output, StringComparison.Ordinal);
    }

    [Fact]
    public static void Main_NonExistentInputFile_TriggersIoExceptionCatchBlock()
    {
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        
        string nonExistentPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xml");
        string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bin");
        
        ReadOnlySpan<string> args = [nonExistentPath, outputPath];
        int exitCode = Program.Main(args.ToArray());
        #pragma warning disable xUnit2013
        Assert.Equal(1, exitCode);
        #pragma warning restore xUnit2013
        
        var output = stringWriter.ToString();
        Assert.Contains("PIPELINE FILE SYSTEM CRASH", output, StringComparison.Ordinal);
    }

    [Fact]
    public static void Main_EmptyInputFile_TriggersDecodeFailureBlock()
    {
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        string emptyInputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xml");
        string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bin");
        File.WriteAllBytes(emptyInputPath, ReadOnlySpan<byte>.Empty);
        
        ReadOnlySpan<string> args = [emptyInputPath, outputPath];

        try
        {
            int exitCode = Program.Main(args.ToArray());
            #pragma warning disable xUnit2013
            Assert.Equal(1, exitCode);
            #pragma warning restore xUnit2013
            
            var output = stringWriter.ToString();
            Assert.Contains("PIPELINE DATA REJECTION ERROR", output, StringComparison.Ordinal);
            Assert.Contains(ResourceStrings.DecodeFailureMessage, output, StringComparison.Ordinal);
        }
        finally
        {
            TryDeleteFile(emptyInputPath);
            TryDeleteFile(outputPath);
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Suppress secondary deletions to avoid disrupting runtime execution frames
        }
    }
}
