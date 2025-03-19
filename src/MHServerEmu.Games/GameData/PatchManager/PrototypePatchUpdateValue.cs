using System.Text.Json.Serialization;

namespace MHServerEmu.Games.GameData.PatchManager
{
    public readonly struct PrototypePatchUpdateValue
    {
        public bool Enabled { get; }
        public string Prototype { get; }
        public string Path { get; }
        public string Description { get; }
        public string Value { get; }

        [JsonIgnore]
        public string СlearPath { get; }
        [JsonIgnore]
        public string FieldName { get; }
        [JsonIgnore]
        public bool InsertValue { get; }

        [JsonConstructor]
        public PrototypePatchUpdateValue(bool enabled, string prototype, string path, string description, string value)
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

}
