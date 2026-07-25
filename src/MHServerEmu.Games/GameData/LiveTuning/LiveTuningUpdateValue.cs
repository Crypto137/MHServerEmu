using System.Text.Json.Serialization;
using Gazillion;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.GameData.LiveTuning
{
    public readonly struct LiveTuningUpdateValue
    {
        public string Prototype { get; }
        public string Setting { get; }
        public float Value { get; }

        [JsonConstructor]
        public LiveTuningUpdateValue(string prototype, string setting, float value)
        {
            Prototype = prototype;
            Setting = setting;
            Value = value;
        }

        public NetStructLiveTuningSettingProtoEnumValue ToProtobuf()
        {
            PrototypeGuid prototypeGuid = PrototypeGuid.Invalid;

            if (Prototype != string.Empty)
            {
                PrototypeId prototypeId = GameDatabase.GetPrototypeRefByName(Prototype);
                if (!Verify.IsTrue(prototypeId != PrototypeId.Invalid, $"Invalid prototype name {Prototype}"))
                    return null;

                prototypeGuid = GameDatabase.GetPrototypeGuid(prototypeId);
            }

            int tuningVarEnum = ParseTuningVarEnum(Setting, out bool isGlobal);
            if (!Verify.IsTrue(tuningVarEnum != -1, $"Invalid setting {Setting} for prototype {Prototype}"))
                return null;

            if (!Verify.IsTrue(isGlobal || prototypeGuid != PrototypeGuid.Invalid, $"Setting {Setting} requires a valid prototype"))
                return null;

            return NetStructLiveTuningSettingProtoEnumValue.CreateBuilder()
                .SetTuningVarProtoId((ulong)prototypeGuid)
                .SetTuningVarEnum(tuningVarEnum)
                .SetTuningVarValue(Value)
                .Build();
        }

        private static int ParseTuningVarEnum(string tuningVarEnum, out bool isGlobal)
        {
            isGlobal = false;
            string prefix = tuningVarEnum.Split('_')[0];

            switch (prefix)
            {
                case "eGTV":
                    if (Enum.TryParse(tuningVarEnum, out GlobalTuningVar globalTuningVar) == false)
                        return -1;

                    isGlobal = true;
                    return (int)globalTuningVar;

                case "eATV":
                    if (Enum.TryParse(tuningVarEnum, out AreaTuningVar areaTuningVar) == false)
                        return -1;

                    return (int)areaTuningVar;

                case "eWETV":
                    if (Enum.TryParse(tuningVarEnum, out WorldEntityTuningVar worldEntityTuningVar) == false)
                        return -1;

                    return (int)worldEntityTuningVar;

                case "eAETV":
                    if (Enum.TryParse(tuningVarEnum, out AvatarEntityTuningVar avatarEntityTuningVar) == false)
                        return -1;

                    return (int)avatarEntityTuningVar;

                case "ePOTV":
                    if (Enum.TryParse(tuningVarEnum, out PopObjTuningVar popObjTuningVar) == false)
                        return -1;

                    return (int)popObjTuningVar;

                case "ePTV":
                    if (Enum.TryParse(tuningVarEnum, out PowerTuningVar powerTuningVar) == false)
                        return -1;

                    return (int)powerTuningVar;

                case "eRT":
                case "eRTV":
                    if (Enum.TryParse(tuningVarEnum, out RegionTuningVar regionTuningVar) == false)
                        return -1;

                    return (int)regionTuningVar;

                case "eLTTV":
                    if (Enum.TryParse(tuningVarEnum, out LootTableTuningVar lootTableTuningVar) == false)
                        return -1;

                    return (int)lootTableTuningVar;

                case "eMTV":
                    if (Enum.TryParse(tuningVarEnum, out MissionTuningVar missionTuningVar) == false)
                        return -1;

                    return (int)missionTuningVar;

                case "eCTV":
                    if (Enum.TryParse(tuningVarEnum, out ConditionTuningVar conditionTuningVar) == false)
                        return -1;

                    return (int)conditionTuningVar;

                case "ePETV":
                    if (Enum.TryParse(tuningVarEnum, out PublicEventTuningVar publicEventTuningVar) == false)
                        return -1;

                    return (int)publicEventTuningVar;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                case "eMFTV":
                    if (Enum.TryParse(tuningVarEnum, out MetricsFrequencyTuningVar metricsFrequencyTuningVar) == false)
                        return -1;

                    return (int)metricsFrequencyTuningVar;
#endif

                default:
                    return -1;
            }
        }
    }
}
