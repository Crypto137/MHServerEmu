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
        Dictionary<ulong, string> _prototypeDict = new();
        Dictionary<ulong, string> _curveDict = new();

        public BlueprintConverter(Dictionary<string, DataDirectory> dataDirectoryDict)
        {
            foreach (IDataDirectoryEntry entry in dataDirectoryDict["Calligraphy/Prototype.directory"].Entries)
                _prototypeDict.Add(entry.Id1, entry.Name);

            foreach (IDataDirectoryEntry entry in dataDirectoryDict["Calligraphy/Curve.directory"].Entries)
                _curveDict.Add(entry.Id1, entry.Name);
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
                    JsonSerializer.Serialize(writer, new BlueprintJson(value, _prototypeDict, _curveDict), options);
                    break;
            }
        }
    }

    public class PrototypeConverter : JsonConverter<Prototype>
    {
        public PrototypeConverter()
        {
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
                    JsonSerializer.Serialize(writer, new PrototypeJson(value), options);
                    break;
            }
        }
    }
}
