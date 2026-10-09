using System.Buffers;
using System.Collections.Immutable;
using System.Text;

namespace PhysXTool;

public static class ResourceStrings
{
    public static string UsageMessage => "Usage: PurePhysXTool <input file path> <output file path>";
    public static string InitFailureMessage => "CRITICAL: Failed to initialize internal pure C# PhysX engine registries.";
    public static string InvalidInputMessage => "Invalid input target file. Extension format must be strictly '.bin' or '.xml'.";
    public static string InvalidOutputMessage => "Invalid output target destination. Extension format must be strictly '.bin' or '.xml'.";
    public static string DecodeFailureMessage => "Unable to decode physics resource data components from the source file.";
    public static string SuccessFormatMessage => "SUCCESS: Successfully converted '{0}' -> '{1}'.";
    public static string DataExceptionFormatMessage => "PIPELINE DATA REJECTION ERROR: {0}";
    public static string IoExceptionFormatMessage => "PIPELINE FILE SYSTEM CRASH: {0}";
    public static string AuthExceptionFormatMessage => "PIPELINE FILE ACCESS DENIED: {0}";
}

public static class Program
{
    private const string XmlExtension = ".XML";
    private const string BinExtension = ".BIN";

    public static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine(ResourceStrings.UsageMessage);
            return 1;
        }

        string inputPath = args[0];
        string outputPath = args[1];

        string inputExt = Path.GetExtension(inputPath).ToUpperInvariant();
        string outputExt = Path.GetExtension(outputPath).ToUpperInvariant();

        byte[]? rentedOutputBuffer = null;

        try
        {
            if (inputExt != BinExtension && inputExt != XmlExtension)
            {
                throw new InvalidDataException(ResourceStrings.InvalidInputMessage);
            }
            if (outputExt != BinExtension && outputExt != XmlExtension)
            {
                throw new InvalidDataException(ResourceStrings.InvalidOutputMessage);
            }

            bool inputIsXml = inputExt == XmlExtension;
            bool outputIsXml = outputExt == XmlExtension;

            PhysXConverter converter = new();
            if (!converter.InitPhysX())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceStrings.InitFailureMessage);
                Console.ResetColor();
                return 1;
            }

            byte[] inputBytes = File.ReadAllBytes(inputPath);
            ImmutableArray<IPxBase> collection;

            if (inputIsXml)
            {
                int charCount = Encoding.UTF8.GetCharCount(inputBytes);
                char[] rentedChars = ArrayPool<char>.Shared.Rent(charCount);
                
                try
                {
                    int totalChars = Encoding.UTF8.GetChars(inputBytes, rentedChars);
                    ReadOnlySpan<char> charSpanWindow = rentedChars.AsSpan(0, totalChars);
                    collection = converter.LoadCollectionFromXml(charSpanWindow);
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(rentedChars, clearArray: true);
                }
            }
            else
            {
                collection = converter.LoadCollectionFromBinary(inputBytes);
            }

            if (collection.IsEmpty)
            {
                throw new InvalidDataException(ResourceStrings.DecodeFailureMessage);
            }

            if (outputIsXml)
            {
                converter.SaveCollectionToXml(collection, out rentedOutputBuffer, out int writtenLength);
                ReadOnlySpan<byte> outputWindow = rentedOutputBuffer.AsSpan(0, writtenLength);
                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
                fileStream.Write(outputWindow);
            }
            else
            {
                converter.SaveCollectionToBinary(collection, out rentedOutputBuffer, out int writtenLength);
                ReadOnlySpan<byte> outputWindow = rentedOutputBuffer.AsSpan(0, writtenLength);
                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
                fileStream.Write(outputWindow);
            }
            
            converter.ShutdownPhysX();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(ResourceStrings.SuccessFormatMessage, Path.GetFileName(inputPath), Path.GetFileName(outputPath));
            Console.ResetColor();
        }
        catch (InvalidDataException dataEx)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ResourceStrings.DataExceptionFormatMessage, dataEx.Message);
            Console.ResetColor();
            return 1;
        }
        catch (IOException ioEx)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ResourceStrings.IoExceptionFormatMessage, ioEx.Message);
            Console.ResetColor();
            return 1;
        }
        catch (UnauthorizedAccessException authEx)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ResourceStrings.AuthExceptionFormatMessage, authEx.Message);
            Console.ResetColor();
            return 1;
        }
        finally
        {
            if (rentedOutputBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedOutputBuffer, clearArray: true);
            }
        }

        return 0;
    }
}
