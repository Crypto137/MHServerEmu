using System.Text.Json;
using System.Text.Json.Serialization;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Gpak
{
    // Contains converters needed to correctly serialize all fields to JSON in interface dictionaries

    public class DataDirectoryEntryConverter : JsonConverter<IDataDirectoryEntry>
    {
        public override IDataDirectoryEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, IDataDirectoryEntry value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    JsonSerializer.Serialize(writer, (IDataDirectoryEntry)null, options);
                    break;

                default:
                    var type = value.GetType();
                    JsonSerializer.Serialize(writer, value, type, options);
                    break;
            }
        }
    }
}
