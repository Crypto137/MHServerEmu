using Gazillion;
using System.Text.Json.Serialization;

namespace MHServerEmu.Games.GameData.LiveTuning
{
    public readonly struct LiveTuningUpdateValue
    {
        public PrototypeGuid TuningVarProtoId { get; }
        public int TuningVarEnum { get; }
        public float TuningVarValue { get; }

        [JsonConstructor]
        public LiveTuningUpdateValue(PrototypeGuid tuningVarProtoId, int tuningVarEnum, float tuningVarValue)
        {
            TuningVarProtoId = tuningVarProtoId;
            TuningVarEnum = tuningVarEnum;
            TuningVarValue = tuningVarValue;
        }

        public NetStructLiveTuningSettingProtoEnumValue ToProtobuf()
        {
            return NetStructLiveTuningSettingProtoEnumValue.CreateBuilder()
                .SetTuningVarProtoId((ulong)TuningVarProtoId)
                .SetTuningVarEnum(TuningVarEnum)
                .SetTuningVarValue(TuningVarValue)
                .Build();
        }
    }
}
