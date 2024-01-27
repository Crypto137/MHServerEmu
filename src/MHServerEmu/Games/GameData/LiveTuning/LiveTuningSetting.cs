using System.Text.Json.Serialization;
using Gazillion;

namespace MHServerEmu.Games.GameData.LiveTuning
{
    public class LiveTuningSetting
    {
        public PrototypeGuid TuningVarProtoId { get; set; }
        public int TuningVarEnum { get; set; }
        public float TuningVarValue { get; set; }

        public LiveTuningSetting(int tuningVarEnum, float tuningVarValue)
        {
            TuningVarEnum = tuningVarEnum;
            TuningVarValue = tuningVarValue;
        }

        [JsonConstructor]
        public LiveTuningSetting(PrototypeGuid tuningVarProtoId, int tuningVarEnum, float tuningVarValue)
        {
            TuningVarProtoId = tuningVarProtoId;
            TuningVarEnum = tuningVarEnum;
            TuningVarValue = tuningVarValue;
        }

        public LiveTuningSetting(NetStructLiveTuningSettingEnumValue netStruct)
        {
            TuningVarEnum = netStruct.TuningVarEnum;
            TuningVarValue = netStruct.TuningVarValue;
        }

        public LiveTuningSetting(NetStructLiveTuningSettingProtoEnumValue netStruct)
        {
            TuningVarProtoId = (PrototypeGuid)netStruct.TuningVarProtoId;
            TuningVarEnum = netStruct.TuningVarEnum;
            TuningVarValue = netStruct.TuningVarValue;
        }

        public NetStructLiveTuningSettingEnumValue ToNetStructEnumValue()
        {
            return NetStructLiveTuningSettingEnumValue.CreateBuilder()
                .SetTuningVarEnum(TuningVarEnum)
                .SetTuningVarValue(TuningVarValue)
                .Build();
        }

        public NetStructLiveTuningSettingProtoEnumValue ToNetStructProtoEnumValue()
        {
            return NetStructLiveTuningSettingProtoEnumValue.CreateBuilder()
                .SetTuningVarProtoId((ulong)TuningVarProtoId)
                .SetTuningVarEnum(TuningVarEnum)
                .SetTuningVarValue(TuningVarValue)
                .Build();
        }

        public override string ToString()
        {
            return $"Proto: {GameDatabase.GetPrototypeNameByGuid(TuningVarProtoId)} Enum: {TuningVarEnum} Value: {TuningVarValue}";
        }
    }
}
