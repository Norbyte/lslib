using Newtonsoft.Json;

namespace LSLib.LS;

public class LSJWriter(Stream stream)
{
    private readonly Stream stream = stream;
    public bool PrettyPrint = false;
    public NodeSerializationSettings SerializationSettings = new();

    public void Write(Resource rsrc)
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        settings.Converters.Add(new LSJResourceConverter(SerializationSettings));
        var serializer = JsonSerializer.Create(settings);

        using var streamWriter = new StreamWriter(stream);
        using var writer = new JsonTextWriter(streamWriter);
        writer.IndentChar = '\t';
        writer.Indentation = 1;
        serializer.Serialize(writer, rsrc);
    }
}
