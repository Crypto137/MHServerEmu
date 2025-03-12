using System.Text.Json.Serialization;

namespace MHServerEmu.Games.GameData.HardTuning
{
    public readonly struct HardTuningUpdateValue
    {
        public string Prototype { get; }
        public string Path { get; }
        public string Description { get; }
        public string Value { get; }

        [JsonIgnore]
        public string СlearPath { get; }
        [JsonIgnore]
        public string FieldName { get; }

        [JsonConstructor]
        public HardTuningUpdateValue(string prototype, string path, string description, string value)
        {
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
        }
    }

}
