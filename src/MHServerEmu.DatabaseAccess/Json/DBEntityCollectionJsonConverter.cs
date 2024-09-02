using System.Text.Json;
using System.Text.Json.Serialization;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.DatabaseAccess.Json
{
    public class DBEntityCollectionJsonConverter : JsonConverter<DBEntityCollection>
    {
        public override DBEntityCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            DBEntity[] entities = JsonSerializer.Deserialize<DBEntity[]>(ref reader, options);
            return new(entities);
        }

        public override void Write(Utf8JsonWriter writer, DBEntityCollection value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.Entries.ToArray(), options);
        }
    }
}
