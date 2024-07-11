using Gazillion;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.LiveTuning
{
    public class LiveTuningManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private LiveTuningData _liveTuningData = new();
        private int _lastUpdateChangeNum = 0;

        public static LiveTuningManager Instance { get; } = new();

        private LiveTuningManager() { }

        public bool Initialize()
        {
            string savedLiveTuningDataPath = Path.Combine(FileHelper.DataDirectory, "Game", "LiveTuningData.json");

            if (File.Exists(savedLiveTuningDataPath))
            {
                var updateValues = FileHelper.DeserializeJson<LiveTuningUpdateValue[]>(savedLiveTuningDataPath);
                UpdateLiveTuningData(updateValues.Select(value => value.ToProtobuf()), true);
                Logger.Info($"Loaded {updateValues.Length} live tuning settings");
            }

            return true;
        }

        public void UpdateLiveTuningData(IEnumerable<NetStructLiveTuningSettingProtoEnumValue> protoEnumValues, bool resetToDefaults)
        {
            lock (_liveTuningData)
            {
                if (resetToDefaults)
                    _liveTuningData.ResetToDefaults();

                foreach (NetStructLiveTuningSettingProtoEnumValue protoEnumValue in protoEnumValues)
                {
                    PrototypeGuid tuningVarProtoId = (PrototypeGuid)protoEnumValue.TuningVarProtoId;
                    int tuningVarEnum = protoEnumValue.TuningVarEnum;
                    float tuningVarValue = protoEnumValue.TuningVarValue;

                    if (tuningVarProtoId != PrototypeGuid.Invalid)
                    {
                        PrototypeId tuningVarProtoRef = GameDatabase.GetDataRefByPrototypeGuid(tuningVarProtoId);

                        if (tuningVarProtoRef == PrototypeId.Invalid)
                        {
                            Logger.Warn($"UpdateLiveTuningData(): Attempted to Update Live Tuning Setting for a prototype not in the GameDatabase.  Prototype: {tuningVarProtoId}");
                            continue;
                        }

                        _liveTuningData.UpdateLiveTuningVar(tuningVarProtoRef, tuningVarEnum, tuningVarValue);
                        Logger.Trace($"Updated Live Tuning Setting on this GIS.  Prototype: {tuningVarProtoRef.GetName()}  Enumeration: {GetEnumName(tuningVarEnum, tuningVarProtoRef)} Value: {tuningVarValue}");
                    }
                    else
                    {
                        _liveTuningData.UpdateLiveGlobalTuningVar((GlobalTuningVar)tuningVarEnum, tuningVarValue);
                        Logger.Trace($"Updated Live Tuning Setting on this GIS.  Prototype: GLOBAL  Enumeration: {(GlobalTuningVar)tuningVarEnum} Value: {tuningVarValue}");
                    }
                }

                _liveTuningData.ChangeNum = ++_lastUpdateChangeNum;
            }
        }

        public bool CopyLiveTuningData(LiveTuningData target)
        {
            if (target.ChangeNum == _lastUpdateChangeNum)
                return false;

            lock (_liveTuningData)
            {
                if (_liveTuningData.ChangeNum != _lastUpdateChangeNum)
                    Logger.Warn("CopyLiveTuningData(): _liveTuningData.ChangeNum != _lastUpdateChangeNum");

                _liveTuningData.Copy(target);
                return true;
            }
        }

        public static float GetLiveGlobalTuningVar(GlobalTuningVar tuningVarEnum)
        {
            return 0f;
        }

        public static float GetLiveAreaTuningVar(AreaPrototype areaProto, AreaTuningVar tuningVarEnum)
        {
            return 0f;
        }

        public static float GetLiveWorldEntityTuningVar(WorldEntityPrototype worldEntityProto, WorldEntityTuningVar tuningVarEnum)
        {
            return 0f;
        }

        public static float GetLiveAvatarTuningVar(AvatarPrototype avatarProto, AvatarEntityTuningVar tuningVarEnum)
        {
            return 0f;
        }

        public static float GetLivePopObjTuningVar(PopulationObjectPrototype popObjProto, PopObjTuningVar tuningVarEnum)
        {
            return 0f;
        }

        public static float GetLivePowerTuningVar(PowerPrototype powerProto, PowerTuningVar tuningVarEnum)
        {
            return 0f;
        }

        public static float GetLiveRegionTuningVar(RegionPrototype regionProto, RegionTuningVar tuningVarEnum)
        {
            return 0f;
        }

        public static float GetLiveLootTableTuningVar(LootTablePrototype lootTableProto, LootTableTuningVar tuningVarEnum)
        {
            return 0f;
        }

        public static float GetLiveMissionTuningVar(MissionPrototype missionProto, MissionTuningVar tuningVarEnum)
        {
            return 0f;
        }

        public static float GetLiveConditionTuningVar(ConditionPrototype conditionProto, ConditionTuningVar tuningVarEnum)
        {
            return 0f;
        }

        public static float GetLivePublicEventTuningVar(PublicEventPrototype publicEventProto, PublicEventTuningVar tuningVarEnum)
        {
            return 0f;
        }

        public static float GetLiveMetricsFrequencyTuningVar(MetricsFrequencyPrototype metricsFrequencyProto, MetricsFrequencyTuningVar tuningVarEnum)
        {
            return 0f;
        }

        // TODO
        // public static void GetLiveLootGroup()

        private static string GetEnumName(int tuningVarEnum, PrototypeId tuningVarProtoRef = PrototypeId.Invalid)
        {
            Prototype prototype = GameDatabase.GetPrototype<Prototype>(tuningVarProtoRef);
            return prototype switch
            {
                AreaPrototype               => ((AreaTuningVar)tuningVarEnum).ToString(),
                AvatarPrototype             => ((AvatarEntityTuningVar)tuningVarEnum).ToString(),
                WorldEntityPrototype        => ((WorldEntityTuningVar)tuningVarEnum).ToString(),
                PopulationObjectPrototype   => ((PopObjTuningVar)tuningVarEnum).ToString(),
                PowerPrototype              => ((PowerTuningVar)tuningVarEnum).ToString(),
                RegionPrototype             => ((RegionTuningVar)tuningVarEnum).ToString(),
                LootTablePrototype          => ((LootTableTuningVar)tuningVarEnum).ToString(),
                MissionPrototype            => ((MissionTuningVar)tuningVarEnum).ToString(),
                ConditionPrototype          => ((ConditionTuningVar)tuningVarEnum).ToString(),
                PublicEventPrototype        => ((PublicEventTuningVar)tuningVarEnum).ToString(),
                MetricsFrequencyPrototype   => ((MetricsFrequencyTuningVar)tuningVarEnum).ToString(),
                _                           => tuningVarEnum.ToString()
            }; ;
        }
    }
}
