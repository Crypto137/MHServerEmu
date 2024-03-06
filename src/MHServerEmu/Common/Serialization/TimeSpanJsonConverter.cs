using System.Text.Json;
using System.Text.Json.Serialization;

namespace MHServerEmu.Common.Serialization
{
    /// <summary>
    /// Serializes <see cref="TimeSpan"/> values using the underlying number of ticks.
    /// </summary>
    public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new(reader.GetInt64());
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Ticks);
        }
    }
}
