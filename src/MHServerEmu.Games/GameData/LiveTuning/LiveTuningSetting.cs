using System.Text.Json.Serialization;
using Gazillion;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.LiveTuning
{
    public class LiveTuningSetting
    {
        private object _typedEnum;

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
            if (_typedEnum == null) DetermineEnumType();
            if (_typedEnum is GlobalTuningVar) return $"GLOBAL {_typedEnum} = {TuningVarValue}";
            return $"{GameDatabase.GetPrototypeNameByGuid(TuningVarProtoId)}: {_typedEnum} = {TuningVarValue}";
        }

        private void DetermineEnumType()
        {
            if (TuningVarProtoId == PrototypeGuid.Invalid)
            {
                _typedEnum = Enum.ToObject(typeof(GlobalTuningVar), TuningVarEnum);
                return;
            }

            var prototype = GameDatabase.GetPrototype<Prototype>(GameDatabase.GetDataRefByPrototypeGuid(TuningVarProtoId));
            Type enumType = prototype switch
            {
                AreaPrototype               => typeof(AreaTuningVar),
                AvatarPrototype             => typeof(AvatarEntityTuningVar),
                WorldEntityPrototype        => typeof(WorldEntityTuningVar),
                PopulationObjectPrototype   => typeof(PopObjTuningVar),
                PowerPrototype              => typeof(PowerTuningVar),
                RegionPrototype             => typeof(RegionTuningVar),
                LootTablePrototype          => typeof(LootTableTuningVar),
                MissionPrototype            => typeof(MissionTuningVar),
                ConditionPrototype          => typeof(ConditionTuningVar),
                PublicEventPrototype        => typeof(PublicEventTuningVar),
                MetricsFrequencyPrototype   => typeof(MetricsFrequencyTuningVar),
                _ => throw new NotSupportedException($"No matching tuning var enum type for prototype {GameDatabase.GetPrototypeNameByGuid(TuningVarProtoId)}."),
            };
            _typedEnum = Enum.ToObject(enumType, TuningVarEnum);
        }
    }
}
