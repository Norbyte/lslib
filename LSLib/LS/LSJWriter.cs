using System.IO;
using Newtonsoft.Json;

namespace LSLib.LS
{
    public class LSJWriter
    {
        private Stream stream;
        private JsonTextWriter writer;
        public bool PrettyPrint = false;

        public LSJWriter(Stream stream)
        {
            this.stream = stream;
        }

        public void Write(Resource rsrc)
        {
            var settings = new JsonSerializerSettings();
            settings.Formatting = Newtonsoft.Json.Formatting.Indented;
            settings.Converters.Add(new LSJResourceConverter());
            var serializer = JsonSerializer.Create(settings);

            using (var streamWriter = new StreamWriter(stream))
            using (this.writer = new JsonTextWriter(streamWriter))
            {
                writer.IndentChar = '\t';
                writer.Indentation = 1;
                serializer.Serialize(writer, rsrc);
            }
        }
    }
}
