using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MHServerEmu.Games.GameData.PatchManager
{
    public class PrototypePatchEntry
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
        [JsonIgnore]
        public bool Patched { get; set; }

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

            Patched = false;
        }
    }

    public class PatchEntryConverter : JsonConverter<PrototypePatchEntry>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
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

            if (valueType == ValueType.Properties) entry.Patched = true;

            return entry;
        }

        public static ValueBase GetValueBase(JsonElement jsonElement, ValueType valueType)
        {
            return valueType switch
            {
                ValueType.String => new SimpleValue<string>(jsonElement.GetString(), valueType),
                ValueType.Boolean => new SimpleValue<bool>(jsonElement.GetBoolean(), valueType),
                ValueType.Float => new SimpleValue<float>(jsonElement.GetSingle(), valueType),
                ValueType.Integer => new SimpleValue<int>(jsonElement.GetInt32(), valueType),
                ValueType.Enum => new SimpleValue<string>(jsonElement.GetString(), valueType),
                ValueType.PrototypeGuid => new SimpleValue<PrototypeGuid>((PrototypeGuid)jsonElement.GetUInt64(), valueType),
                ValueType.PrototypeId or 
                ValueType.PrototypeDataRef => new SimpleValue<PrototypeId>((PrototypeId)jsonElement.GetUInt64(), valueType),
                ValueType.LocaleStringId => new SimpleValue<LocaleStringId>((LocaleStringId)jsonElement.GetUInt64(), valueType),
                ValueType.PrototypeIdArray or
                ValueType.PrototypeDataRefArray => new ArrayValue<PrototypeId>(jsonElement, valueType, x => (PrototypeId)x.GetUInt64()),
                ValueType.Prototype => new SimpleValue<Prototype>(ParseJsonPrototype(jsonElement), valueType),
                ValueType.PrototypeArray => new ArrayValue<Prototype>(jsonElement, valueType, ParseJsonPrototype),
                ValueType.Vector3 => new SimpleValue<Vector3>(ParseJsonVector3(jsonElement), valueType),
                ValueType.Properties => new SimpleValue<PropertyCollection>(ParseJsonProperties(jsonElement), valueType),
                _ => throw new NotSupportedException($"Type {valueType} not support.")
            };
        }

        private static Vector3 ParseJsonVector3(JsonElement jsonElement)
        {
            if (jsonElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Json element is not array");

            var jsonArray = jsonElement.EnumerateArray().ToArray();
            if (jsonArray.Length != 3) 
                throw new InvalidOperationException("Json element is not Vector3");

            return new Vector3(jsonArray[0].GetSingle(), jsonArray[1].GetSingle(), jsonArray[2].GetSingle());
        }

        public static Prototype ParseJsonPrototype(JsonElement jsonElement)
        {

            var referenceType = (PrototypeId)jsonElement.GetProperty("ParentDataRef").GetUInt64();
            Type classType = GameDatabase.DataDirectory.GetPrototypeClassType(referenceType);
            var prototype = GameDatabase.PrototypeClassManager.AllocatePrototype(classType);

            CalligraphySerializer.CopyPrototypeDataRefFields(prototype, referenceType);
            prototype.ParentDataRef = referenceType;

            foreach (var property in jsonElement.EnumerateObject())
            {
                if (property.Name == "ParentDataRef") continue;
                var fieldInfo = prototype.GetType().GetProperty(property.Name);
                if (fieldInfo == null) continue;
                Type fieldType = fieldInfo.PropertyType;
                var element = ParseJsonElement(property.Value, fieldType);
                try
                {
                    object convertedValue = PrototypePatchManager.ConvertValue(element, fieldType);
                    fieldInfo.SetValue(prototype, convertedValue);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException(ex, $"ParseJsonPrototype can't convert {element} in {fieldType.Name}");
                }

            }

            return prototype;
        }

        public static PropertyCollection ParseJsonProperties(JsonElement jsonElement)
        {
            PropertyCollection properties = new ();
            var infoTable = GameDatabase.PropertyInfoTable;

            foreach (var property in jsonElement.EnumerateObject())
            {
                var propEnum = (PropertyEnum)Enum.Parse(typeof(PropertyEnum), property.Name);
                PropertyInfo propertyInfo = infoTable.LookupPropertyInfo(propEnum);
                PropertyId propId = ParseJsonPropertyId(property.Value, propEnum, propertyInfo);
                PropertyValue propValue = ParseJsonPropertyValue(property.Value, propertyInfo);
                properties.SetProperty(propValue, propId);
            }

            return properties;
        }

        public static PropertyId ParseJsonPropertyId(JsonElement jsonElement, PropertyEnum propEnum, PropertyInfo propInfo)
        {
            int paramCount = propInfo.ParamCount;
            if (paramCount == 0) return new(propEnum);

            var jsonArray = jsonElement.EnumerateArray().ToArray();
            Span<PropertyParam> paramValues = stackalloc PropertyParam[Property.MaxParamCount];
            propInfo.DefaultParamValues.CopyTo(paramValues);

            for (int i = 0; i < paramCount; i++)
            {
                if (i >= 4) break;
                if (i >= jsonArray.Length) continue;

                var paramValue = jsonArray[i];

                switch (propInfo.GetParamType(i))
                {
                    case PropertyParamType.Asset:
                        var assetParam = (AssetId)ParseJsonElement(paramValue, typeof(AssetId));
                        paramValues[i] = Property.ToParam(assetParam);
                        break;

                    case PropertyParamType.Prototype:
                        var protoRefParam = (PrototypeId)ParseJsonElement(paramValue, typeof(PrototypeId));
                        paramValues[i] = Property.ToParam(propEnum, i, protoRefParam);
                        break;

                    case PropertyParamType.Integer:
                        if (paramValue.TryGetInt64(out long decimalValue))
                            paramValues[i] = (PropertyParam)(int)decimalValue;
                        break;

                    default:
                        throw new InvalidOperationException("Encountered an unknown prop param type in an ParseJsonPropertyId!");
                }
            }

            return new(propEnum, paramValues);
        }

        public static PropertyValue ParseJsonPropertyValue(JsonElement jsonElement, PropertyInfo propInfo)
        {
            if (propInfo.ParamCount > 0)
            {
                var jsonArray = jsonElement.EnumerateArray().ToArray();
                jsonElement = jsonArray[^1];
            }

            switch (propInfo.DataType)
            {
                case PropertyDataType.Integer:
                    if (jsonElement.TryGetInt64(out long decimalValue))
                        return (PropertyValue)decimalValue;
                    break;

                case PropertyDataType.Real:
                    if (jsonElement.TryGetDouble(out double doubleValue))
                        return (PropertyValue)(float)doubleValue;
                    break;

                case PropertyDataType.Boolean:
                    return (PropertyValue)jsonElement.GetBoolean();

                case PropertyDataType.Prototype:
                    var protoRefValue = (PrototypeId)ParseJsonElement(jsonElement, typeof(PrototypeId));
                    return (PropertyValue)protoRefValue;

                case PropertyDataType.Asset:
                    AssetId assetValue = (AssetId)ParseJsonElement(jsonElement, typeof(AssetId));
                    return (PropertyValue)assetValue;

                default:
                    throw new InvalidOperationException($"[ParseJsonPropertyValue] Assignment into invalid property (property type is not int/float/bool)! Property: {propInfo.PropertyName}");
            }

            return propInfo.DefaultValue;
        }

        public static object ParseJsonElement(JsonElement value, Type fieldType)
        {
            if (fieldType == typeof(PrototypeId))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetUInt64(out ulong ulongValue))
                    return (PrototypeId)ulongValue;
            }

            if (fieldType == typeof(AssetId))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetUInt64(out ulong ulongValue))
                    return (AssetId)ulongValue;
            }

            if (fieldType == typeof(PrototypeGuid))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetUInt64(out ulong ulongValue))
                    return (PrototypeGuid)ulongValue;
            }

            if (fieldType == typeof(LocaleStringId))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetUInt64(out ulong ulongValue))
                    return (LocaleStringId)ulongValue;
            }

            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    return value.GetString();
                case JsonValueKind.Number:
                    if (value.TryGetUInt64(out ulong ulongValue))
                        return ulongValue;
                    else if (value.TryGetInt64(out long decimalValue))
                        return decimalValue;
                    else if (value.TryGetDouble(out double doubleValue))
                        return doubleValue;
                    else
                        return value.GetRawText();
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return value.GetBoolean();
                default:
                    return value.ToString();
            }
        }

        public override void Write(Utf8JsonWriter writer, PrototypePatchEntry value, JsonSerializerOptions options)
        {
            throw new NotImplementedException(); 
        }
    }

    public enum ValueType
    {
        String,
        Boolean,
        Float,
        Integer,
        Enum,
        PrototypeGuid,
        PrototypeId,
        PrototypeIdArray,
        LocaleStringId,
        PrototypeDataRef,
        PrototypeDataRefArray,
        Prototype,
        PrototypeArray,
        Vector3,
        Properties
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
