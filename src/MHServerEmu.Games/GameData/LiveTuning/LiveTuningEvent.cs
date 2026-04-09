using System.Text.Json;
using System.Text.Json.Serialization;

namespace MHServerEmu.Games.GameData.LiveTuning
{
    public class LiveTuningEvent
    {
        public string DisplayName { get; init; }
        public bool IsHidden { get; init; }
        public string FilePath { get; init; }
        public string DailyGift { get; init; }
        public string[] InstancedMissions { get; init; }

        public LiveTuningEvent() { }

        public override string ToString()
        {
            return DisplayName;
        }

        public static class JsonOptions
        {
            public static readonly JsonSerializerOptions Default = new()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            static JsonOptions()
            {
                Default.Converters.Add(new JsonStringEnumConverter());
            }
        }
    }
}
