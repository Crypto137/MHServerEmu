using System.Text.Json;
using System.Text.Json.Serialization;
using MHServerEmu.GameServer.GameData.Calligraphy;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;
using MHServerEmu.GameServer.GameData.Gpak.JsonOutput;
using MHServerEmu.GameServer.GameData.Prototypes;
using MHServerEmu.GameServer.GameData.Prototypes.Markers;

namespace MHServerEmu.GameServer.GameData.Gpak
{
    // Contains converters needed to correctly serialize all fields to JSON in interface dictionaries and add string representations where appropriate

    public class BlueprintConverter : JsonConverter<Blueprint>
    {
        private DataDirectory _prototypeDir;

        public BlueprintConverter(DataDirectory prototypeDir)
        {
            _prototypeDir = prototypeDir;
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
                    JsonSerializer.Serialize(writer, new BlueprintJson(value, _prototypeDir), options);
                    break;
            }
        }
    }

    public class PrototypeFileConverter : JsonConverter<PrototypeFile>
    {
        private DataDirectory _prototypeDir;

        public PrototypeFileConverter(DataDirectory prototypeDir)
        {
            _prototypeDir = prototypeDir;
        }

        public override PrototypeFile Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, PrototypeFile value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    JsonSerializer.Serialize(writer, (PrototypeFile)null, options);
                    break;

                default:
                    JsonSerializer.Serialize(writer, new PrototypeFileJson(value, _prototypeDir), options);
                    break;
            }
        }
    }

    public class MarkerPrototypeConverter : JsonConverter<MarkerPrototype>
    {
        public override MarkerPrototype Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, MarkerPrototype value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    JsonSerializer.Serialize(writer, (MarkerPrototype)null, options);
                    break;

                default:
                    var type = value.GetType();
                    JsonSerializer.Serialize(writer, value, type, options);
                    break;
            }
        }
    }

    public class NaviPatchPrototypeConverter : JsonConverter<NaviPatchPrototype>
    {
        public override NaviPatchPrototype Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, NaviPatchPrototype value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    JsonSerializer.Serialize(writer, (NaviPatchPrototype)null, options);
                    break;

                default:
                    var type = value.GetType();
                    JsonSerializer.Serialize(writer, new NaviPatchPrototypeJson(value), options);
                    break;
            }
        }
    }

    public class UIPanelPrototypeConverter : JsonConverter<UIPanelPrototype>
    {
        public override UIPanelPrototype Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, UIPanelPrototype value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    JsonSerializer.Serialize(writer, (UIPanelPrototype)null, options);
                    break;

                default:
                    var type = value.GetType();
                    JsonSerializer.Serialize(writer, value, type, options);
                    break;
            }
        }
    }
}
