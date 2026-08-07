using Gazillion;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.LiveTuning
{
    public class LiveTuningManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static readonly string LiveTuningDataDirectory = Path.Combine(FileHelper.DataDirectory, "Game", "LiveTuning");

        private readonly LiveTuningData _liveTuningData = new();
        private int _lastUpdateChangeNum = 0;

        private readonly Dictionary<(ulong, int), float> _lastKnownSettings = new();

        public static LiveTuningManager Instance { get; } = new();

        private LiveTuningManager() { }

        public bool Initialize()
        {
            LoadLiveTuningData(false);
            return true;
        }

        public bool LoadLiveTuningData(bool sendToServices)
        {
            if (!Verify.IsTrue(Directory.Exists(LiveTuningDataDirectory), "Live Tuning data directory not found"))
                return false;

            List<NetStructLiveTuningSettingProtoEnumValue> settings = new();

            // Read all .json files that start with LiveTuningData
            foreach (string filePath in FileHelper.GetFilesWithPrefix(LiveTuningDataDirectory, "LiveTuningData", "json"))
                LoadLiveTuningDataFromFile(filePath, settings);

            // Get additional event Live Tuning based on the current day
            LiveTuningEventScheduler.Instance.GetLiveTuningSettings(settings);

            UpdateLiveTuningData(settings, true);
            Logger.Info($"Loaded {settings.Count} Live Tuning settings");

            CacheLoadedSettings(settings, sendToServices);

            if (sendToServices)
                LiveTuningEventScheduler.Instance.SendEventMessageTextToGroupingManager();

            return true;
        }

        public static bool LoadLiveTuningDataFromFile(string filePath, List<NetStructLiveTuningSettingProtoEnumValue> settings)
        {
            ReadOnlySpan<char> fileName = Path.GetFileName(filePath.AsSpan());

            LiveTuningUpdateValue[] updateValues = FileHelper.DeserializeJson<LiveTuningUpdateValue[]>(filePath);
            if (!Verify.IsNotNull(updateValues, $"Failed to parse {fileName.ToString()}"))
                return false;

            foreach (LiveTuningUpdateValue value in updateValues)
            {
                NetStructLiveTuningSettingProtoEnumValue protobuf = value.ToProtobuf();
                if (protobuf == null)
                    continue;

                settings.Add(protobuf);
            }

            Logger.Trace($"Parsed Live Tuning data from {fileName}");
            return true;
        }

        private void CacheLoadedSettings(List<NetStructLiveTuningSettingProtoEnumValue> loadedSettings, bool sendToGameInstances)
        {
            // Filter out tuning settings we know game instances don't need to react to in advance to reduce workload on individual game instances.
            List<NetStructLiveTuningSettingProtoEnumValue> settingsToSend = null;

            // Iterate in reverse and skip already encountered proto refs to send only the latest data if a setting is set multiple times.
            using var protoRefsHandle = HashSetPool<PrototypeId>.Get(out HashSet<PrototypeId> protoRefs);

            for (int i = loadedSettings.Count - 1; i >= 0; i--)
            {
                NetStructLiveTuningSettingProtoEnumValue setting = loadedSettings[i];

                PrototypeId tuningProtoRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)setting.TuningVarProtoId);
                if (tuningProtoRef == PrototypeId.Invalid)
                    continue;

                if (protoRefs.Add(tuningProtoRef) == false)
                    continue;

                Prototype tuningProto = GameDatabase.GetPrototype<Prototype>(tuningProtoRef);
                if (!Verify.IsNotNull(tuningProto))
                    continue;

                bool passesFilter = tuningProto switch
                {
                    WorldEntityPrototype => setting.TuningVarEnum == (int)WorldEntityTuningVar.eWETV_Visible && tuningProto is not AvatarPrototype,
                    MissionPrototype     => setting.TuningVarEnum == (int)MissionTuningVar.eMTV_EventInstance,
                    PublicEventPrototype => setting.TuningVarEnum == (int)PublicEventTuningVar.ePETV_EventInstance,
                    _                    => false,
                };

                // Check against our internal cache if this value is different from what was sent last time.
                if (passesFilter)
                {
                    var key = (setting.TuningVarProtoId, setting.TuningVarEnum);

                    // I don't think CacheLoadedSettings() is going to be called from multiple threads in practice, but better safe than sorry!
                    lock (_lastKnownSettings)
                    {
                        if (_lastKnownSettings.TryGetValue(key, out float lastKnownValue) && lastKnownValue == setting.TuningVarValue)
                            passesFilter = false;
                        else
                            _lastKnownSettings[key] = setting.TuningVarValue;
                    }
                }

                if (passesFilter && sendToGameInstances)
                {
                    settingsToSend ??= new();
                    settingsToSend.Add(setting);
                }
            }

            // Skip sending if there is nothing new or we are initializing for the first time.
            if (settingsToSend != null && sendToGameInstances)
            {
                ServiceMessage.SetLiveTuningValues message = new(settingsToSend);
                ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
            }
        }

        public void UpdateLiveTuningData(List<NetStructLiveTuningSettingProtoEnumValue> protoEnumValues, bool resetToDefaults)
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
                        if (!Verify.IsTrue(tuningVarProtoRef != PrototypeId.Invalid, $"Attempted to Update Live Tuning Setting for a prototype not in the GameDatabase.  Prototype: {tuningVarProtoId}"))
                            continue;

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

        /// <summary>
        /// Updates the provided <see cref="LiveTuningData"/> if needed. Returns <see langword="true"/> if data was updated.
        /// </summary>
        public bool CopyLiveTuningData(LiveTuningData output)
        {
            if (output.ChangeNum == _lastUpdateChangeNum)
                return false;

            lock (_liveTuningData)
            {
                Verify.IsTrue(_liveTuningData.ChangeNum == _lastUpdateChangeNum);
                output.Copy(_liveTuningData);
            }

            output.UpdateCustomTuningData();
            return true;
        }

        public static float GetLiveGlobalTuningVar(GlobalTuningVar tuningVarEnum)
        {
            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (!Verify.IsNotNull(liveTuningData)) return LiveTuningData.DefaultTuningVarValue;
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
                if (!Verify.IsNotNull(liveTuningData)) return LiveTuningData.DefaultTuningVarValue;
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
                if (!Verify.IsNotNull(liveTuningData)) return LiveTuningData.DefaultTuningVarValue;
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
                if (!Verify.IsNotNull(liveTuningData)) return LiveTuningData.DefaultTuningVarValue;
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
                if (!Verify.IsNotNull(liveTuningData)) return LiveTuningData.DefaultTuningVarValue;
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
                if (!Verify.IsNotNull(liveTuningData)) return LiveTuningData.DefaultTuningVarValue;
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
                if (!Verify.IsNotNull(liveTuningData)) return LiveTuningData.DefaultTuningVarValue;
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
                if (!Verify.IsNotNull(liveTuningData)) return LiveTuningData.DefaultTuningVarValue;
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
                if (!Verify.IsNotNull(liveTuningData)) return LiveTuningData.DefaultTuningVarValue;
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
                if (!Verify.IsNotNull(liveTuningData)) return LiveTuningData.DefaultTuningVarValue;
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
                if (!Verify.IsNotNull(liveTuningData)) return LiveTuningData.DefaultTuningVarValue;
                return liveTuningData.GetLivePublicEventTuningVar(publicEventProto, tuningVarEnum);
            }
            else
            {
                lock (Instance._liveTuningData)
                    return Instance._liveTuningData.GetLivePublicEventTuningVar(publicEventProto, tuningVarEnum);
            }
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public static float GetLiveMetricsFrequencyTuningVar(MetricsFrequencyPrototype metricsFrequencyProto, MetricsFrequencyTuningVar tuningVarEnum)
        {
            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (!Verify.IsNotNull(liveTuningData)) return LiveTuningData.DefaultTuningVarValue;
                return liveTuningData.GetLiveMetricsFrequencyTuningVar(metricsFrequencyProto, tuningVarEnum);
            }
            else
            {
                lock (Instance._liveTuningData)
                    return Instance._liveTuningData.GetLiveMetricsFrequencyTuningVar(metricsFrequencyProto, tuningVarEnum);
            }
        }
#endif

#if GAME_VERSION_1_53
        public static float GetLiveDifficultyTuningTuningVar(TuningPrototype tuningProto, DifficultyTuningTuningVar tuningVarEnum)
        {
            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (!Verify.IsNotNull(liveTuningData)) return LiveTuningData.DefaultTuningVarValue;
                return liveTuningData.GetLiveDifficultyTuningTuningVar(tuningProto, tuningVarEnum);
            }
            else
            {
                lock (Instance._liveTuningData)
                    return Instance._liveTuningData.GetLiveDifficultyTuningTuningVar(tuningProto, tuningVarEnum);
            }
        }
#endif

        public static bool GetLiveLootGroup(int lootGroupNum, out IReadOnlyList<WorldEntityPrototype> lootGroup)
        {
            lootGroup = Array.Empty<WorldEntityPrototype>();

            Game game = Game.Current;
            if (game != null)
            {
                LiveTuningData liveTuningData = game.LiveTuningData;
                if (!Verify.IsNotNull(liveTuningData)) return false;
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
