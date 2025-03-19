using System.Text.Json;
using System.Text.Json.Serialization;

namespace MHServerEmu.Games.GameData.PatchManager
{
    public readonly struct PrototypePatchEntry
    {
        public bool Enabled { get; }
        public string Prototype { get; }
        public string Path { get; }
        public string Description { get; }
        public ValueBase Value { get; }

        [JsonIgnore]
        public string СlearPath { get; }
        [JsonIgnore]
        public string FieldName { get; }
        [JsonIgnore]
        public bool InsertValue { get; }

        [JsonConstructor]
        public PrototypePatchEntry(bool enabled, string prototype, string path, string description, ValueBase value)
        {
            Enabled = enabled;
            Prototype = prototype;
            Path = path;
            Description = description;
            Value = value;

            int lastDotIndex = path.LastIndexOf('.');
            if (lastDotIndex == -1)
            {
                СlearPath = string.Empty;
                FieldName = path;
            }
            else
            {
                СlearPath = path[..lastDotIndex];
                FieldName = path[(lastDotIndex + 1)..];
            }

            InsertValue = false;
            int index = FieldName.LastIndexOf('[');
            if (index != -1)
            {
                InsertValue = true;
                FieldName = FieldName[..index];
            }
        }
    }

    public class PatchEntryConverter : JsonConverter<PrototypePatchEntry>
    {
        public override PrototypePatchEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            string valueTypeString = root.GetProperty("ValueType").GetString();
            valueTypeString = valueTypeString.Replace("[]", "Array");
            var valueType = Enum.Parse<ValueType>(valueTypeString);
            var entry = new PrototypePatchEntry
            (
                root.GetProperty("Enabled").GetBoolean(),
                root.GetProperty("Prototype").GetString(),
                root.GetProperty("Path").GetString(),
                root.GetProperty("Description").GetString(),
                GetValueBase(root.GetProperty("Value"), valueType)
            );
            return entry;
        }

        public static ValueBase GetValueBase(JsonElement jsonElement, ValueType valueType)
        {
            return valueType switch
            {
                ValueType.String => new SimpleValue<string>(jsonElement.GetString(), valueType),
                ValueType.Float => new SimpleValue<float>(jsonElement.GetSingle(), valueType),
                ValueType.Integer => new SimpleValue<int>(jsonElement.GetInt32(), valueType),
                ValueType.Enum => new SimpleValue<string>(jsonElement.GetString(), valueType),
                ValueType.PrototypeId => new SimpleValue<PrototypeId>((PrototypeId)jsonElement.GetUInt64(), valueType),
                ValueType.LocaleStringId => new SimpleValue<LocaleStringId>((LocaleStringId)jsonElement.GetUInt64(), valueType),
                ValueType.PrototypeIdArray => new ArrayValue<PrototypeId>(jsonElement, valueType, x => (PrototypeId)x.GetUInt64()),
                _ => throw new NotSupportedException($"Type {valueType} not support.")
            };
        }

        public override void Write(Utf8JsonWriter writer, PrototypePatchEntry value, JsonSerializerOptions options)
        {
            throw new NotImplementedException(); 
        }
    }

    public enum ValueType
    {
        String,
        Float,
        Integer,
        Enum,
        PrototypeId,
        PrototypeIdArray,
        LocaleStringId,
        Prototype,
        PrototypeArray
    }

    public abstract class ValueBase
    {
        public abstract ValueType ValueType { get; }
        public abstract object GetValue();
    }

    public class SimpleValue<T> : ValueBase
    {
        public override ValueType ValueType { get; }
        public T Value { get; }

        public SimpleValue(T value, ValueType valueType)
        {
            Value = value;
            ValueType = valueType;
        }

        public override object GetValue() => Value;
    }

    public class PrototypeIdArrayValue : SimpleValue<PrototypeId[]>
    {
        public PrototypeIdArrayValue(JsonElement jsonElement) : base(ParseJsonElement(jsonElement), ValueType.PrototypeIdArray) { }
        private static PrototypeId[] ParseJsonElement(JsonElement jsonElement)
        {
            var jsonArray = jsonElement.EnumerateArray().ToArray();
            if (jsonArray.Length == 0) return [];
            return Array.ConvertAll(jsonArray, x => (PrototypeId)x.GetUInt64());
        }
    }

    public class ArrayValue<T> : SimpleValue<T[]>
    {
        public ArrayValue(JsonElement jsonElement, ValueType valueType, Func<JsonElement, T> elementParser)
            : base(ParseJsonElement(jsonElement, elementParser), valueType) { }

        private static T[] ParseJsonElement(JsonElement jsonElement, Func<JsonElement, T> elementParser)
        {
            if (jsonElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Json element is not array");

            var jsonArray = jsonElement.EnumerateArray().ToArray();
            if (jsonArray.Length == 0) return [];

            var result = new T[jsonArray.Length];
            for (int i = 0; i < jsonArray.Length; i++)
                result[i] = elementParser(jsonArray[i]);

            return result;
        }
    }
}
