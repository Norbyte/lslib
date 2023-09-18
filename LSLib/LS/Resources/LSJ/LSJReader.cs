using System;
using System.IO;
using Newtonsoft.Json;

namespace LSLib.LS
{
    public class LSJReader : IDisposable
    {
        private Stream stream;
        private JsonTextReader reader;
        public NodeSerializationSettings SerializationSettings = new NodeSerializationSettings();

        public LSJReader(Stream stream)
        {
            this.stream = stream;
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        public Resource Read()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new LSJResourceConverter(SerializationSettings));
            var serializer = JsonSerializer.Create(settings);

            using (var streamReader = new StreamReader(stream))
            using (this.reader = new JsonTextReader(streamReader))
            {
                return serializer.Deserialize<Resource>(this.reader);
            }
        }
    }
}
