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
                        Logger.Trace($"Updated Live Tuning Setting on this GIS.  Prototype: {tuningVarProtoRef.GetName()}  Enumeration: {LiveTuningData.GetLiveTuningVarEnumName(tuningVarEnum, tuningVarProtoRef)} Value: {tuningVarValue}");
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

        public bool CopyLiveTuningData(LiveTuningData output)
        {
            if (output.ChangeNum == _lastUpdateChangeNum)
                return false;

            lock (_liveTuningData)
            {
                if (_liveTuningData.ChangeNum != _lastUpdateChangeNum)
                    Logger.Warn("CopyLiveTuningData(): _liveTuningData.ChangeNum != _lastUpdateChangeNum");

                output.Copy(_liveTuningData);
                return true;
            }
        }

        public static float GetLiveGlobalTuningVar(GlobalTuningVar tuningVarEnum)
        {
            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (liveTuningData == null) return Logger.WarnReturn(LiveTuningData.DefaultTuningVarValue, "GetLiveGlobalTuningVar(): liveTuningData == null");
                return liveTuningData.GetLiveGlobalTuningVar(tuningVarEnum);
            }
            else
            {
                lock (Instance._liveTuningData)
                    return Instance._liveTuningData.GetLiveGlobalTuningVar(tuningVarEnum);
            }
        }

        public static float GetLiveAreaTuningVar(AreaPrototype areaProto, AreaTuningVar tuningVarEnum)
        {
            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (liveTuningData == null) return Logger.WarnReturn(LiveTuningData.DefaultTuningVarValue, "GetLiveAreaTuningVar(): liveTuningData == null");
                return liveTuningData.GetLiveAreaTuningVar(areaProto, tuningVarEnum);
            }
            else
            {
                lock (Instance._liveTuningData)
                    return Instance._liveTuningData.GetLiveAreaTuningVar(areaProto, tuningVarEnum);
            }
        }

        public static float GetLiveWorldEntityTuningVar(WorldEntityPrototype worldEntityProto, WorldEntityTuningVar tuningVarEnum)
        {
            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (liveTuningData == null) return Logger.WarnReturn(LiveTuningData.DefaultTuningVarValue, "GetLiveWorldEntityTuningVar(): liveTuningData == null");
                return liveTuningData.GetLiveWorldEntityTuningVar(worldEntityProto, tuningVarEnum);
            }
            else
            {
                lock (Instance._liveTuningData)
                    return Instance._liveTuningData.GetLiveWorldEntityTuningVar(worldEntityProto, tuningVarEnum);
            }
        }

        public static float GetLiveAvatarTuningVar(AvatarPrototype avatarProto, AvatarEntityTuningVar tuningVarEnum)
        {
            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (liveTuningData == null) return Logger.WarnReturn(LiveTuningData.DefaultTuningVarValue, "GetLiveAvatarTuningVar(): liveTuningData == null");
                return liveTuningData.GetLiveAvatarTuningVar(avatarProto, tuningVarEnum);
            }
            else
            {
                lock (Instance._liveTuningData)
                    return Instance._liveTuningData.GetLiveAvatarTuningVar(avatarProto, tuningVarEnum);
            }
        }

        public static float GetLivePopObjTuningVar(PopulationObjectPrototype popObjProto, PopObjTuningVar tuningVarEnum)
        {
            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (liveTuningData == null) return Logger.WarnReturn(LiveTuningData.DefaultTuningVarValue, "GetLivePopObjTuningVar(): liveTuningData == null");
                return liveTuningData.GetLivePopObjTuningVar(popObjProto, tuningVarEnum);
            }
            else
            {
                lock (Instance._liveTuningData)
                    return Instance._liveTuningData.GetLivePopObjTuningVar(popObjProto, tuningVarEnum);
            }
        }

        public static float GetLivePowerTuningVar(PowerPrototype powerProto, PowerTuningVar tuningVarEnum)
        {
            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (liveTuningData == null) return Logger.WarnReturn(LiveTuningData.DefaultTuningVarValue, "GetLivePowerTuningVar(): liveTuningData == null");
                return liveTuningData.GetLivePowerTuningVar(powerProto, tuningVarEnum);
            }
            else
            {
                lock (Instance._liveTuningData)
                    return Instance._liveTuningData.GetLivePowerTuningVar(powerProto, tuningVarEnum);
            }
        }

        public static float GetLiveRegionTuningVar(RegionPrototype regionProto, RegionTuningVar tuningVarEnum)
        {
            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (liveTuningData == null) return Logger.WarnReturn(LiveTuningData.DefaultTuningVarValue, "GetLiveRegionTuningVar(): liveTuningData == null");
                return liveTuningData.GetLiveRegionTuningVar(regionProto, tuningVarEnum);
            }
            else
            {
                lock (Instance._liveTuningData)
                    return Instance._liveTuningData.GetLiveRegionTuningVar(regionProto, tuningVarEnum);
            }
        }

        public static float GetLiveLootTableTuningVar(LootTablePrototype lootTableProto, LootTableTuningVar tuningVarEnum)
        {
            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (liveTuningData == null) return Logger.WarnReturn(LiveTuningData.DefaultTuningVarValue, "GetLiveLootTableTuningVar(): liveTuningData == null");
                return liveTuningData.GetLiveLootTableTuningVar(lootTableProto, tuningVarEnum);
            }
            else
            {
                lock (Instance._liveTuningData)
                    return Instance._liveTuningData.GetLiveLootTableTuningVar(lootTableProto, tuningVarEnum);
            }
        }

        public static float GetLiveMissionTuningVar(MissionPrototype missionProto, MissionTuningVar tuningVarEnum)
        {
            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (liveTuningData == null) return Logger.WarnReturn(LiveTuningData.DefaultTuningVarValue, "GetLiveMissionTuningVar(): liveTuningData == null");
                return liveTuningData.GetLiveMissionTuningVar(missionProto, tuningVarEnum);
            }
            else
            {
                lock (Instance._liveTuningData)
                    return Instance._liveTuningData.GetLiveMissionTuningVar(missionProto, tuningVarEnum);
            }
        }

        public static float GetLiveConditionTuningVar(ConditionPrototype conditionProto, ConditionTuningVar tuningVarEnum)
        {
            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (liveTuningData == null) return Logger.WarnReturn(LiveTuningData.DefaultTuningVarValue, "GetLiveConditionTuningVar(): liveTuningData == null");
                return liveTuningData.GetLiveConditionTuningVar(conditionProto, tuningVarEnum);
            }
            else
            {
                lock (Instance._liveTuningData)
                    return Instance._liveTuningData.GetLiveConditionTuningVar(conditionProto, tuningVarEnum);
            }
        }

        public static float GetLivePublicEventTuningVar(PublicEventPrototype publicEventProto, PublicEventTuningVar tuningVarEnum)
        {
            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (liveTuningData == null) return Logger.WarnReturn(LiveTuningData.DefaultTuningVarValue, "GetLivePublicEventTuningVar(): liveTuningData == null");
                return liveTuningData.GetLivePublicEventTuningVar(publicEventProto, tuningVarEnum);
            }
            else
            {
                lock (Instance._liveTuningData)
                    return Instance._liveTuningData.GetLivePublicEventTuningVar(publicEventProto, tuningVarEnum);
            }
        }

        public static float GetLiveMetricsFrequencyTuningVar(MetricsFrequencyPrototype metricsFrequencyProto, MetricsFrequencyTuningVar tuningVarEnum)
        {
            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (liveTuningData == null) return Logger.WarnReturn(LiveTuningData.DefaultTuningVarValue, "GetLiveMetricsFrequencyTuningVar(): liveTuningData == null");
                return liveTuningData.GetLiveMetricsFrequencyTuningVar(metricsFrequencyProto, tuningVarEnum);
            }
            else
            {
                lock (Instance._liveTuningData)
                    return Instance._liveTuningData.GetLiveMetricsFrequencyTuningVar(metricsFrequencyProto, tuningVarEnum);
            }
        }

        public static bool GetLiveLootGroup(int lootGroupNum, out IEnumerable<WorldEntityPrototype> lootGroup)
        {
            lootGroup = Array.Empty<WorldEntityPrototype>();

            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (liveTuningData == null) return Logger.WarnReturn(false, "GetLiveLootGroup(): liveTuningData == null");
                return liveTuningData.GetLiveLootGroup(lootGroupNum, out lootGroup);
            }
            else
            {
                lock (Instance._liveTuningData)
                    return Instance._liveTuningData.GetLiveLootGroup(lootGroupNum, out lootGroup);
            }
        }
    }
}
