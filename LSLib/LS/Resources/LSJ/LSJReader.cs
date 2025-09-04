﻿using Newtonsoft.Json;

namespace LSLib.LS;

using System.Globalization;

public class LSJReader(Stream stream) : IDisposable
{
    private readonly Stream stream = stream;
    public NodeSerializationSettings SerializationSettings = new();

    public void Dispose()
    {
        stream.Dispose();
    }

    public Resource Read()
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new LSJResourceConverter(SerializationSettings));
        settings.Culture = CultureInfo.InvariantCulture;
        var serializer = JsonSerializer.Create(settings);

        using var streamReader = new StreamReader(stream);
        using var reader = new JsonTextReader(streamReader);
        return serializer.Deserialize<Resource>(reader);
    }
}
