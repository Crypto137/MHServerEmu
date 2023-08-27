using System.Text.Json;
using System.Text.Json.Serialization;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;
using MHServerEmu.GameServer.GameData.Gpak.JsonOutput;

namespace MHServerEmu.GameServer.GameData.Gpak
{
    // Contains converters needed to correctly serialize all fields to JSON in interface dictionaries and add string representations where appropriate

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

    public class BlueprintConverter : JsonConverter<Blueprint>
    {
        Dictionary<ulong, string> _prototypeDict;
        Dictionary<ulong, string> _curveDict;
        Dictionary<ulong, string> _typeDict;

        public BlueprintConverter(Dictionary<ulong, string> prototypeDict, Dictionary<ulong, string> curveDict, Dictionary<ulong, string> typeDict)
        {
            _prototypeDict = prototypeDict;
            _curveDict = curveDict;
            _typeDict = typeDict;
        }

        public override Blueprint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Blueprint value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    JsonSerializer.Serialize(writer, (Blueprint)null, options);
                    break;

                default:
                    JsonSerializer.Serialize(writer, new BlueprintJson(value, _prototypeDict, _curveDict, _typeDict), options);
                    break;
            }
        }
    }

    public class PrototypeConverter : JsonConverter<Prototype>
    {
        Dictionary<ulong, string> _prototypeDict;
        Dictionary<ulong, string> _prototypeFieldDict;
        Dictionary<ulong, string> _curveDict;
        Dictionary<ulong, string> _assetDict;
        Dictionary<ulong, string> _assetTypeDict;

        public PrototypeConverter(Dictionary<ulong, string> prototypeDict, Dictionary<ulong, string> prototypeFieldDict,
            Dictionary<ulong, string> curveDict, Dictionary<ulong, string> assetDict, Dictionary<ulong, string> assetTypeDict)
        {
            _prototypeDict = prototypeDict;
            _prototypeFieldDict = prototypeFieldDict;
            _curveDict = curveDict;
            _assetDict = assetDict;
            _assetTypeDict = assetTypeDict;
        }

        public override Prototype Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Prototype value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    JsonSerializer.Serialize(writer, (Prototype)null, options);
                    break;

                default:
                    JsonSerializer.Serialize(writer, new PrototypeJson(value, _prototypeDict, _prototypeFieldDict, _curveDict, _assetDict, _assetTypeDict), options);
                    break;
            }
        }
    }
}
