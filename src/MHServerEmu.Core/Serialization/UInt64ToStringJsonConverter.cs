using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MHServerEmu.Core.Serialization
{
    /// <summary>
    /// Converts a <see cref="ulong"/> value to hex string for serialization.
    /// </summary>
    /// <remarks>
    /// We need this because JavaScript is dumb, and it parses our 64 bit integer ids as floats with limited precision.
    /// We convert our ids to hex strings server-side to circumvent this and represent everything in the web frontend accurately.
    /// </remarks>
    public class UInt64ToHexStringJsonConverter : JsonConverter<ulong>
    {
        public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string hexString = reader.GetString();
            return ulong.Parse(hexString, NumberStyles.HexNumber);
        }

        public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
        {
            string hexString = value.ToString("X16");
            writer.WriteStringValue(hexString);
        }
    }
}
