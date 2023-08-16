using System.Text.Json;
using System.Text.Json.Serialization;
using MHServerEmu.GameServer.Data.Gpak.FileFormats;

namespace MHServerEmu.GameServer.Data.Gpak
{
    // Contains converters needed to correctly serialize all fields to JSON in interface dictionaries

    public class GDirectoryEntryConverter : JsonConverter<IGDirectoryEntry>
    {
        public override IGDirectoryEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, IGDirectoryEntry value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    JsonSerializer.Serialize(writer, (IGDirectoryEntry)null, options);
                    break;

                default:
                    var type = value.GetType();
                    JsonSerializer.Serialize(writer, value, type, options);
                    break;
            }
        }
    }
}
