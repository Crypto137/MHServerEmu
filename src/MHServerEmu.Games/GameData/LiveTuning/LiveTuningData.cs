using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.LiveTuning
{
    public class LiveTuningData
    {
        public const float DefaultTuningVarValue = 1f;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly TuningVarArray _globalTuningVars = new((int)GlobalTuningVar.eGTV_NumGlobalTuningVars);

        private List<TuningVarArray> _perAreaTuningVars;
        private List<TuningVarArray> _perLootTableTuningVars;
        private List<TuningVarArray> _perMissionTuningVars;
        private List<TuningVarArray> _perWorldEntityTuningVars;
        private List<TuningVarArray> _perPopObjTuningVars;
        private List<TuningVarArray> _perPowerTuningVars;
        private List<TuningVarArray> _perRegionTuningVars;
        private List<TuningVarArray> _perAvatarTuningVars;
        private List<TuningVarArray> _perConditionTuningVars;
        private List<TuningVarArray> _perPublicEventTuningVars;
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private List<TuningVarArray> _perMetricsFrequencyTuningVars;
#endif
#if GAME_VERSION_1_53
        private List<TuningVarArray> _perDifficultyTuningTuningVars;
#endif

        private readonly Dictionary<int, List<WorldEntityPrototype>> _lootGroups = new();

        private NetMessageLiveTuningUpdate _updateProtobuf = NetMessageLiveTuningUpdate.DefaultInstance;
        private bool _updateProtobufOutOfDate = false;

        // Store LiveTuningData used by game instances per thread to reduce memory usage.
        [ThreadStatic]
        internal static LiveTuningData Current;

        public int ChangeNum { get; set; } = 0;

        // Custom data not in Gazillion's implementation of Live Tuning. We use this for any hot swappable data we want in game thread's local storage.
        public List<PrototypeId> EventDailyGifts { get; } = new();

        public LiveTuningData()
        {
            // InitClientWhitelistBits()
            InitPerAreaTuningVars();
            InitPerLootTableTuningVars();
            InitPerMissionTuningVars();
            InitPerWorldEntityTuningVars();
            InitPerPopObjTuningVars();
            InitPerPowerTuningVars();
            InitPerRegionTuningVars();
            InitPerAvatarTuningVars();
            InitPerConditionTuningVars();
            InitPerPublicEventTuningVars();
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            InitPerMetricsFrequencyTuningVars();
#endif
#if GAME_VERSION_1_53
            InitDifficultyTuningTuningVars();
#endif
        }

        public void ResetToDefaults()
        {
            _globalTuningVars.Clear();

            foreach (TuningVarArray tuningVarArray in _perAreaTuningVars)               tuningVarArray.Clear();
            foreach (TuningVarArray tuningVarArray in _perLootTableTuningVars)          tuningVarArray.Clear();
            foreach (TuningVarArray tuningVarArray in _perMissionTuningVars)            tuningVarArray.Clear();
            foreach (TuningVarArray tuningVarArray in _perWorldEntityTuningVars)        tuningVarArray.Clear();
            foreach (TuningVarArray tuningVarArray in _perPopObjTuningVars)             tuningVarArray.Clear();
            foreach (TuningVarArray tuningVarArray in _perPowerTuningVars)              tuningVarArray.Clear();
            foreach (TuningVarArray tuningVarArray in _perRegionTuningVars)             tuningVarArray.Clear();
            foreach (TuningVarArray tuningVarArray in _perAvatarTuningVars)             tuningVarArray.Clear();
            foreach (TuningVarArray tuningVarArray in _perConditionTuningVars)          tuningVarArray.Clear();
            foreach (TuningVarArray tuningVarArray in _perPublicEventTuningVars)        tuningVarArray.Clear();
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            foreach (TuningVarArray tuningVarArray in _perMetricsFrequencyTuningVars)   tuningVarArray.Clear();
#endif
#if GAME_VERSION_1_53
            foreach (TuningVarArray tuningVarArray in _perDifficultyTuningTuningVars)   tuningVarArray.Clear();
#endif

            ClearLootGroups();

            ChangeNum = 0;
            _updateProtobuf = NetMessageLiveTuningUpdate.DefaultInstance;
            _updateProtobufOutOfDate = false;
        }

        public void Copy(LiveTuningData other)
        {
            if (ChangeNum == other.ChangeNum) return;

            _globalTuningVars.Copy(other._globalTuningVars);

            for (int i = 0; i < _perAreaTuningVars.Count; i++)
                _perAreaTuningVars[i].Copy(other._perAreaTuningVars[i]);

            for (int i = 0; i < _perLootTableTuningVars.Count; i++)
                _perLootTableTuningVars[i].Copy(other._perLootTableTuningVars[i]);

            for (int i = 0; i < _perMissionTuningVars.Count; i++)
                _perMissionTuningVars[i].Copy(other._perMissionTuningVars[i]);

            for (int i = 0; i < _perWorldEntityTuningVars.Count; i++)
                _perWorldEntityTuningVars[i].Copy(other._perWorldEntityTuningVars[i]);

            for (int i = 0; i < _perPopObjTuningVars.Count; i++)
                _perPopObjTuningVars[i].Copy(other._perPopObjTuningVars[i]);

            for (int i = 0; i < _perPowerTuningVars.Count; i++)
                _perPowerTuningVars[i].Copy(other._perPowerTuningVars[i]);

            for (int i = 0; i < _perRegionTuningVars.Count; i++)
                _perRegionTuningVars[i].Copy(other._perRegionTuningVars[i]);

            for (int i = 0; i < _perAvatarTuningVars.Count; i++)
                _perAvatarTuningVars[i].Copy(other._perAvatarTuningVars[i]);

            for (int i = 0; i < _perConditionTuningVars.Count; i++)
                _perConditionTuningVars[i].Copy(other._perConditionTuningVars[i]);

            for (int i = 0; i < _perPublicEventTuningVars.Count; i++)
                _perPublicEventTuningVars[i].Copy(other._perPublicEventTuningVars[i]);

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            for (int i = 0; i < _perMetricsFrequencyTuningVars.Count; i++)
                _perMetricsFrequencyTuningVars[i].Copy(other._perMetricsFrequencyTuningVars[i]);
#endif

#if GAME_VERSION_1_53
            for (int i = 0; i < _perDifficultyTuningTuningVars.Count; i++)
                _perDifficultyTuningTuningVars[i].Copy(other._perDifficultyTuningTuningVars[i]);
#endif

            ClearLootGroups();

            foreach (var kvp in other._lootGroups)
            {
                List<WorldEntityPrototype> lootGroupCopy = new(kvp.Value);
                _lootGroups.Add(kvp.Key, lootGroupCopy);
            }

            ChangeNum = other.ChangeNum;
            _updateProtobufOutOfDate = true;
        }

        public void UpdateCustomTuningData()
        {
            EventDailyGifts.Clear();
            LiveTuningEventScheduler.Instance.GetDailyGifts(EventDailyGifts);
        }

        public void UpdateLiveGlobalTuningVar(GlobalTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < GlobalTuningVar.eGTV_NumGlobalTuningVars)) return;

            _globalTuningVars[(int)tuningVarEnum] = tuningVarValue;
            _updateProtobufOutOfDate = true;
        }

        public void UpdateLiveTuningVar(PrototypeId tuningVarProtoRef, int tuningVarEnum, float tuningVarValue)
        {
            if (!Verify.IsTrue(tuningVarProtoRef != PrototypeId.Invalid)) return;

            Prototype prototype = GameDatabase.GetPrototype<Prototype>(tuningVarProtoRef);

            if (prototype is AvatarPrototype)
                UpdateLiveAvatarTuningVar(tuningVarProtoRef, (AvatarEntityTuningVar)tuningVarEnum, tuningVarValue);
            else if (prototype is WorldEntityPrototype)
                UpdateLiveWorldEntityTuningVar(tuningVarProtoRef, (WorldEntityTuningVar)tuningVarEnum, tuningVarValue);
            else if (prototype is PowerPrototype)
                UpdateLivePowerTuningVar(tuningVarProtoRef, (PowerTuningVar)tuningVarEnum, tuningVarValue);
            else if (prototype is AreaPrototype)
                UpdateLiveAreaTuningVar(tuningVarProtoRef, (AreaTuningVar)tuningVarEnum, tuningVarValue);
            else if (prototype is RegionPrototype)
                UpdateLiveRegionTuningVar(tuningVarProtoRef, (RegionTuningVar)tuningVarEnum, tuningVarValue);
            else if (prototype is PopulationObjectPrototype)
                UpdateLivePopObjTuningVar(tuningVarProtoRef, (PopObjTuningVar)tuningVarEnum, tuningVarValue);
            else if (prototype is MissionPrototype)
                UpdateLiveMissionTuningVar(tuningVarProtoRef, (MissionTuningVar)tuningVarEnum, tuningVarValue);
            else if (prototype is LootTablePrototype)
                UpdateLiveLootTableTuningVar(tuningVarProtoRef, (LootTableTuningVar)tuningVarEnum, tuningVarValue);
            else if (prototype is ConditionPrototype)
                UpdateLiveConditionTuningVar(tuningVarProtoRef, (ConditionTuningVar)tuningVarEnum, tuningVarValue);
            else if (prototype is PublicEventPrototype)
                UpdateLivePublicEventTuningVar(tuningVarProtoRef, (PublicEventTuningVar)tuningVarEnum, tuningVarValue);
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            else if (prototype is MetricsFrequencyPrototype)
                UpdateLiveMetricsFrequencyTuningVar(tuningVarProtoRef, (MetricsFrequencyTuningVar)tuningVarEnum, tuningVarValue);
#endif
#if GAME_VERSION_1_53
            else if (prototype is TuningPrototype)
                UpdateLiveDifficultyTuningTuningVar(tuningVarProtoRef, (DifficultyTuningTuningVar)tuningVarEnum, tuningVarValue);
#endif
        }

        public NetMessageLiveTuningUpdate GetLiveTuningUpdate()
        {
            // If our cached protobuf is up to date, we don't need to do anything
            if (_updateProtobufOutOfDate == false)
                return _updateProtobuf;

            // Generate a new protobuf if the one we have is out of date
            var updateBuilder = NetMessageLiveTuningUpdate.CreateBuilder();
            DataDirectory dataDirectory = DataDirectory.Instance;

            // NOTE: In the client there are bit array filters for each tuning var category (see LiveTuningData::initClientWhitelistBits()),
            // but most of them just enable the whole category, so we are going to just do all the relevant checks in here instead to simplify things a little.

            // Global
            for (int i = 0; i < (int)GlobalTuningVar.eGTV_NumGlobalTuningVars; i++)
            {
                float tuningVarValue = GetLiveGlobalTuningVar((GlobalTuningVar)i);
                if (tuningVarValue == DefaultTuningVarValue)
                    continue;

                updateBuilder.AddTuningTypeKeyValueSettings(NetStructLiveTuningSettingProtoEnumValue.CreateBuilder()
                    .SetTuningVarProtoId((ulong)PrototypeId.Invalid)
                    .SetTuningVarEnum(i)
                    .SetTuningVarValue(tuningVarValue));
            }

            // Power
            BlueprintId powerBlueprintRef = GetPowerBlueprintDataRef();

            for (int i = 0; i < _perPowerTuningVars.Count; i++)
            {
                PrototypeId powerProtoRef = dataDirectory.GetPrototypeFromEnumValue(i, powerBlueprintRef);
                if (powerProtoRef == PrototypeId.Invalid)
                    continue;

                for (int j = 0; j < (int)PowerTuningVar.ePTV_NumPowerTuningVars; j++)
                {
                    float tuningVarValue = GetLivePowerTuningVar(i, (PowerTuningVar)j);
                    if (tuningVarValue == DefaultTuningVarValue)
                        continue;

                    updateBuilder.AddTuningTypeKeyValueSettings(NetStructLiveTuningSettingProtoEnumValue.CreateBuilder()
                        .SetTuningVarProtoId((ulong)dataDirectory.GetPrototypeGuid(powerProtoRef))
                        .SetTuningVarEnum(j)
                        .SetTuningVarValue(tuningVarValue));
                }
            }

            // Region
            BlueprintId regionBlueprintRef = GetRegionBlueprintDataRef();

            for (int i = 0; i < _perRegionTuningVars.Count; i++)
            {
                PrototypeId regionProtoRef = dataDirectory.GetPrototypeFromEnumValue(i, regionBlueprintRef);
                if (regionProtoRef == PrototypeId.Invalid)
                    continue;

                for (int j = 0; j < (int)RegionTuningVar.eRTV_NumRegionTuningVars; j++)
                {
                    float tuningVarValue = GetLiveRegionTuningVar(i, (RegionTuningVar)j);
                    if (tuningVarValue == DefaultTuningVarValue)
                        continue;

                    updateBuilder.AddTuningTypeKeyValueSettings(NetStructLiveTuningSettingProtoEnumValue.CreateBuilder()
                        .SetTuningVarProtoId((ulong)dataDirectory.GetPrototypeGuid(regionProtoRef))
                        .SetTuningVarEnum(j)
                        .SetTuningVarValue(tuningVarValue));
                }
            }

            // Public Event
            BlueprintId publicEventBlueprintRef = GetPublicEventBlueprintDataRef();

            for (int i = 0; i < _perPublicEventTuningVars.Count; i++)
            {
                PrototypeId publicEventProtoRef = dataDirectory.GetPrototypeFromEnumValue(i, publicEventBlueprintRef);
                if (publicEventProtoRef == PrototypeId.Invalid)
                    continue;

                for (int j = 0; j < (int)PublicEventTuningVar.ePETV_NumPublicEventTuningVars; j++)
                {
                    float tuningVarValue = GetLivePublicEventTuningVar(i, (PublicEventTuningVar)j);
                    if (tuningVarValue == DefaultTuningVarValue)
                        continue;

                    updateBuilder.AddTuningTypeKeyValueSettings(NetStructLiveTuningSettingProtoEnumValue.CreateBuilder()
                        .SetTuningVarProtoId((ulong)dataDirectory.GetPrototypeGuid(publicEventProtoRef))
                        .SetTuningVarEnum(j)
                        .SetTuningVarValue(tuningVarValue));
                }
            }

            // World Entity
            BlueprintId worldEntityBlueprintRef = GetWorldEntityBlueprintDataRef();

            for (int i = 0; i < _perWorldEntityTuningVars.Count; i++)
            {
                PrototypeId worldEntityProtoRef = dataDirectory.GetPrototypeFromEnumValue(i, worldEntityBlueprintRef);
                if (worldEntityProtoRef == PrototypeId.Invalid)
                    continue;

                for (int j = 0; j < (int)WorldEntityTuningVar.eWETV_NumWorldEntityTuningVars; j++)
                {
                    // Not all world entity tuning vars are sent to the client
                    if (ShouldSendTuningVarToClient((WorldEntityTuningVar)j) == false)
                        continue;

                    float tuningVarValue = GetLiveWorldEntityTuningVar(i, (WorldEntityTuningVar)j);
                    if (tuningVarValue == DefaultTuningVarValue)
                        continue;

                    updateBuilder.AddTuningTypeKeyValueSettings(NetStructLiveTuningSettingProtoEnumValue.CreateBuilder()
                        .SetTuningVarProtoId((ulong)dataDirectory.GetPrototypeGuid(worldEntityProtoRef))
                        .SetTuningVarEnum(j)
                        .SetTuningVarValue(tuningVarValue));
                }
            }

            // Avatar
            BlueprintId avatarBlueprintRef = GetAvatarBlueprintDataRef();

            for (int i = 0; i < _perAvatarTuningVars.Count; i++)
            {
                PrototypeId avatarProtoRef = dataDirectory.GetPrototypeFromEnumValue(i, avatarBlueprintRef);
                if (avatarProtoRef == PrototypeId.Invalid)
                    continue;

                for (int j = 0; j < (int)AvatarEntityTuningVar.eAETV_NumAvatarEntityTuningVars; j++)
                {
                    float tuningVarValue = GetLiveAvatarTuningVar(i, (AvatarEntityTuningVar)j);
                    if (tuningVarValue == DefaultTuningVarValue)
                        continue;

                    updateBuilder.AddTuningTypeKeyValueSettings(NetStructLiveTuningSettingProtoEnumValue.CreateBuilder()
                        .SetTuningVarProtoId((ulong)dataDirectory.GetPrototypeGuid(avatarProtoRef))
                        .SetTuningVarEnum(j)
                        .SetTuningVarValue(tuningVarValue));
                }
            }

            _updateProtobuf = updateBuilder.Build();
            _updateProtobufOutOfDate = false;

            Logger.Info($"Generated live tuning update for change num {ChangeNum}");

            return _updateProtobuf;
        }

        public bool GetLiveLootGroup(int lootGroupNum, out IReadOnlyList<WorldEntityPrototype> lootGroup)
        {
            bool found = _lootGroups.TryGetValue(lootGroupNum, out List<WorldEntityPrototype> worldEntityProtoList);
            lootGroup = worldEntityProtoList;
            return found;
        }

        private void UpdateLiveLootGroup(WorldEntityPrototype worldEntityProto, float value)
        {
            int worldEntityEnumVal = worldEntityProto.WorldEntityPrototypeEnumValue;
            int currentLootGroupNum = (int)_perWorldEntityTuningVars[worldEntityEnumVal][(int)WorldEntityTuningVar.eWETV_LootGroupNum];
            int newLootGroupNum = (int)value;
            
            // No need to update loot group
            if (newLootGroupNum == currentLootGroupNum)
                return;

            // Remove from the current group if its not default value
            // NOTE: Switch to using HashSet here to improve removal performance if needed
            if (currentLootGroupNum != DefaultTuningVarValue)
            {
                if (_lootGroups.TryGetValue(currentLootGroupNum, out List<WorldEntityPrototype> lootGroup))
                    lootGroup.Remove(worldEntityProto);
            }

            // Add to the new group if its not default value
            if (newLootGroupNum != DefaultTuningVarValue)
            {
                if (_lootGroups.TryGetValue(newLootGroupNum, out List<WorldEntityPrototype> lootGroup) == false)
                {
                    lootGroup = new();
                    _lootGroups.Add(newLootGroupNum, lootGroup);
                }

                lootGroup.Add(worldEntityProto);
            }
        }

        private void ClearLootGroups()
        {
            _lootGroups.Clear();
        }

        public static string GetLiveTuningVarEnumName(int tuningVarEnum, PrototypeId tuningVarProtoRef = PrototypeId.Invalid)
        {
            if (tuningVarProtoRef == PrototypeId.Invalid) return ((GlobalTuningVar)tuningVarEnum).ToString();

            Prototype prototype = GameDatabase.GetPrototype<Prototype>(tuningVarProtoRef);

            if (prototype is AvatarPrototype) return ((AvatarEntityTuningVar)tuningVarEnum).ToString();
            if (prototype is WorldEntityPrototype) return ((WorldEntityTuningVar)tuningVarEnum).ToString();
            if (prototype is PowerPrototype) return ((PowerTuningVar)tuningVarEnum).ToString();
            if (prototype is AreaPrototype) return ((AreaTuningVar)tuningVarEnum).ToString();
            if (prototype is RegionPrototype) return ((RegionTuningVar)tuningVarEnum).ToString();
            if (prototype is PopulationObjectPrototype) return ((PopObjTuningVar)tuningVarEnum).ToString();
            if (prototype is MissionPrototype) return ((MissionTuningVar)tuningVarEnum).ToString();
            if (prototype is LootTablePrototype) return ((LootTableTuningVar)tuningVarEnum).ToString();
            if (prototype is ConditionPrototype) return ((ConditionTuningVar)tuningVarEnum).ToString();
            if (prototype is PublicEventPrototype) return ((PublicEventTuningVar)tuningVarEnum).ToString();
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            if (prototype is MetricsFrequencyPrototype) return ((MetricsFrequencyTuningVar)tuningVarEnum).ToString();
#endif
#if GAME_VERSION_1_53
            if (prototype is TuningPrototype) return ((DifficultyTuningTuningVar)tuningVarEnum).ToString();
#endif

            return tuningVarEnum.ToString();
        }

        private static bool ShouldSendTuningVarToClient(WorldEntityTuningVar tuningVarEnum)
        {
            // This is a more straightforward replacement for LiveTuningData::initClientWhitelistBits() and bit arrays from client code.

            switch (tuningVarEnum)
            {
                case WorldEntityTuningVar.eWETV_Enabled:
                case WorldEntityTuningVar.eWETV_EternitySplinterPrice:  // NOTE: EternitySplinterPrice is excluded in client code.
                case WorldEntityTuningVar.eWETV_Visible:
                    return true;

                default:
                    return false;
            }
        }

        #region Tuning Var Accesors

        public float GetLiveGlobalTuningVar(GlobalTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < GlobalTuningVar.eGTV_NumGlobalTuningVars)) return DefaultTuningVarValue;

            return _globalTuningVars[(int)tuningVarEnum];
        }

        public float GetLiveAreaTuningVar(AreaPrototype areaProto, AreaTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < AreaTuningVar.eATV_NumAreaTuningVars)) return DefaultTuningVarValue;

            int areaEnumVal = areaProto.AreaPrototypeEnumValue;
            if (!Verify.IsTrue(areaEnumVal >= 0 && areaEnumVal < _perAreaTuningVars.Count)) return DefaultTuningVarValue;

            return _perAreaTuningVars[areaEnumVal][(int)tuningVarEnum];
        }

        public float GetLiveLootTableTuningVar(LootTablePrototype lootTableProto, LootTableTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < LootTableTuningVar.eLTTV_NumLootTableTuningVars)) return DefaultTuningVarValue;

            int lootTableEnumVal = lootTableProto.LootTablePrototypeEnumValue;
            if (!Verify.IsTrue(lootTableEnumVal >= 0 && lootTableEnumVal < _perLootTableTuningVars.Count)) return DefaultTuningVarValue;

            return _perLootTableTuningVars[lootTableEnumVal][(int)tuningVarEnum];
        }

        public float GetLiveMissionTuningVar(MissionPrototype missionProto, MissionTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < MissionTuningVar.eMTV_NumMissionTuningVars)) return DefaultTuningVarValue;

            int missionEnumVal = missionProto.MissionPrototypeEnumValue;
            if (!Verify.IsTrue(missionEnumVal >= 0 && missionEnumVal < _perMissionTuningVars.Count)) return DefaultTuningVarValue;

            return _perMissionTuningVars[missionEnumVal][(int)tuningVarEnum];
        }

        public float GetLiveWorldEntityTuningVar(WorldEntityPrototype worldEntityProto, WorldEntityTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < WorldEntityTuningVar.eWETV_NumWorldEntityTuningVars)) return DefaultTuningVarValue;

            int worldEntityEnumVal = worldEntityProto.WorldEntityPrototypeEnumValue;
            if (!Verify.IsTrue(worldEntityEnumVal >= 0 && worldEntityEnumVal < _perWorldEntityTuningVars.Count)) return DefaultTuningVarValue;

            return _perWorldEntityTuningVars[worldEntityEnumVal][(int)tuningVarEnum];
        }

        public float GetLivePopObjTuningVar(PopulationObjectPrototype popObjProto, PopObjTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < PopObjTuningVar.ePOTV_NumPopulationObjectTuningVars)) return DefaultTuningVarValue;

            int popObjEnumVal = popObjProto.PopulationObjectPrototypeEnumValue;
            if (!Verify.IsTrue(popObjEnumVal >= 0 && popObjEnumVal < _perPopObjTuningVars.Count)) return DefaultTuningVarValue;

            return _perPopObjTuningVars[popObjEnumVal][(int)tuningVarEnum];
        }

        public float GetLivePowerTuningVar(PowerPrototype powerProto, PowerTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < PowerTuningVar.ePTV_NumPowerTuningVars)) return DefaultTuningVarValue;

            int powerEnumVal = powerProto.PowerPrototypeEnumValue;
            if (!Verify.IsTrue(powerEnumVal >= 0 && powerEnumVal < _perPowerTuningVars.Count)) return DefaultTuningVarValue;

            return _perPowerTuningVars[powerEnumVal][(int)tuningVarEnum];
        }

        public float GetLiveRegionTuningVar(RegionPrototype regionProto, RegionTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < RegionTuningVar.eRTV_NumRegionTuningVars)) return DefaultTuningVarValue;

            int regionEnumVal = regionProto.RegionPrototypeEnumValue;
            if (!Verify.IsTrue(regionEnumVal >= 0 && regionEnumVal < _perRegionTuningVars.Count)) return DefaultTuningVarValue;

            return _perRegionTuningVars[regionEnumVal][(int)tuningVarEnum];
        }

        public float GetLiveAvatarTuningVar(AvatarPrototype avatarProto, AvatarEntityTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < AvatarEntityTuningVar.eAETV_NumAvatarEntityTuningVars)) return DefaultTuningVarValue;

            int avatarEnumVal = avatarProto.AvatarPrototypeEnumValue;
            if (!Verify.IsTrue(avatarEnumVal >= 0 && avatarEnumVal < _perAvatarTuningVars.Count)) return DefaultTuningVarValue;

            return _perAvatarTuningVars[avatarEnumVal][(int)tuningVarEnum];
        }

        public float GetLiveConditionTuningVar(ConditionPrototype conditionProto, ConditionTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < ConditionTuningVar.eCTV_NumConditionTuningVars)) return DefaultTuningVarValue;

            int conditionEnumVal = conditionProto.ConditionPrototypeEnumValue;
            if (!Verify.IsTrue(conditionEnumVal >= 0 && conditionEnumVal < _perConditionTuningVars.Count)) return DefaultTuningVarValue;

            return _perConditionTuningVars[conditionEnumVal][(int)tuningVarEnum];
        }

        public float GetLivePublicEventTuningVar(PublicEventPrototype publicEventProto, PublicEventTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < PublicEventTuningVar.ePETV_NumPublicEventTuningVars)) return DefaultTuningVarValue;

            int publicEventEnumVal = publicEventProto.PublicEventPrototypeEnumValue;
            if (!Verify.IsTrue(publicEventEnumVal >= 0 && publicEventEnumVal < _perPublicEventTuningVars.Count)) return DefaultTuningVarValue;

            return _perPublicEventTuningVars[publicEventEnumVal][(int)tuningVarEnum];
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public float GetLiveMetricsFrequencyTuningVar(MetricsFrequencyPrototype metricsFrequencyProto, MetricsFrequencyTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < MetricsFrequencyTuningVar.eMFTV_NumMetricsFrequencyTuningVars)) return DefaultTuningVarValue;

            int metricsFrequencyEnumVal = metricsFrequencyProto.MetricsFrequencyPrototypeEnumValue;
            if (!Verify.IsTrue(metricsFrequencyEnumVal >= 0 && metricsFrequencyEnumVal < _perMetricsFrequencyTuningVars.Count)) return DefaultTuningVarValue;

            return _perMetricsFrequencyTuningVars[metricsFrequencyEnumVal][(int)tuningVarEnum];
        }
#endif

#if GAME_VERSION_1_53
        public float GetLiveDifficultyTuningTuningVar(TuningPrototype tuningProto, DifficultyTuningTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < DifficultyTuningTuningVar.eDTTV_NumDifficultyTuningTuningVars)) return DefaultTuningVarValue;

            int difficultyTuningEnumVal = tuningProto.DifficultyTuningPrototypeEnumValue;
            if (!Verify.IsTrue(difficultyTuningEnumVal >= 0 && difficultyTuningEnumVal < _perDifficultyTuningTuningVars.Count)) return DefaultTuningVarValue;

            return _perDifficultyTuningTuningVars[difficultyTuningEnumVal][(int)tuningVarEnum];
        }
#endif

        #endregion

        #region Private Tuning Var Accessors (for protobuf generation)

        private float GetLiveWorldEntityTuningVar(int worldEntityEnumVal, WorldEntityTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < WorldEntityTuningVar.eWETV_NumWorldEntityTuningVars)) return DefaultTuningVarValue;
            if (!Verify.IsTrue(worldEntityEnumVal >= 0 && worldEntityEnumVal < _perWorldEntityTuningVars.Count)) return DefaultTuningVarValue;

            return _perWorldEntityTuningVars[worldEntityEnumVal][(int)tuningVarEnum];
        }

        private float GetLivePowerTuningVar(int powerEnumVal, PowerTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < PowerTuningVar.ePTV_NumPowerTuningVars)) return DefaultTuningVarValue;
            if (!Verify.IsTrue(powerEnumVal >= 0 && powerEnumVal < _perPowerTuningVars.Count)) return DefaultTuningVarValue;

            return _perPowerTuningVars[powerEnumVal][(int)tuningVarEnum];
        }

        private float GetLiveRegionTuningVar(int regionEnumVal, RegionTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < RegionTuningVar.eRTV_NumRegionTuningVars)) return DefaultTuningVarValue;
            if (!Verify.IsTrue(regionEnumVal >= 0 && regionEnumVal < _perRegionTuningVars.Count)) return DefaultTuningVarValue;

            return _perRegionTuningVars[regionEnumVal][(int)tuningVarEnum];
        }

        private float GetLiveAvatarTuningVar(int avatarEnumVal, AvatarEntityTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < AvatarEntityTuningVar.eAETV_NumAvatarEntityTuningVars)) return DefaultTuningVarValue;
            if (!Verify.IsTrue(avatarEnumVal >= 0 && avatarEnumVal < _perAvatarTuningVars.Count)) return DefaultTuningVarValue;

            return _perAvatarTuningVars[avatarEnumVal][(int)tuningVarEnum];
        }

        private float GetLivePublicEventTuningVar(int publicEventEnumVal, PublicEventTuningVar tuningVarEnum)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < PublicEventTuningVar.ePETV_NumPublicEventTuningVars)) return DefaultTuningVarValue;
            if (!Verify.IsTrue(publicEventEnumVal >= 0 && publicEventEnumVal < _perPublicEventTuningVars.Count)) return DefaultTuningVarValue;

            return _perPublicEventTuningVars[publicEventEnumVal][(int)tuningVarEnum];
        }

        #endregion

        #region Global Bluepring Data Ref Accessors

        public static BlueprintId GetAreaBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;
            if (!Verify.IsNotNull(globalsProto)) return BlueprintId.Invalid;
            if (!Verify.IsTrue(globalsProto.AreaPrototype != PrototypeId.Invalid)) return BlueprintId.Invalid;

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.AreaPrototype);
        }

        public static BlueprintId GetLootTableBlueprintDataRef()
        {
            LootGlobalsPrototype lootGlobalsProto = GameDatabase.LootGlobalsPrototype;
            if (!Verify.IsNotNull(lootGlobalsProto)) return BlueprintId.Invalid;
            if (!Verify.IsTrue(lootGlobalsProto.LootTableBlueprint != PrototypeId.Invalid)) return BlueprintId.Invalid;

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(lootGlobalsProto.LootTableBlueprint);
        }

        public static BlueprintId GetMissionBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;
            if (!Verify.IsNotNull(globalsProto)) return BlueprintId.Invalid;
            if (!Verify.IsTrue(globalsProto.MissionPrototype != PrototypeId.Invalid)) return BlueprintId.Invalid;

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.MissionPrototype);
        }

        public static BlueprintId GetWorldEntityBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;
            if (!Verify.IsNotNull(globalsProto)) return BlueprintId.Invalid;
            if (!Verify.IsTrue(globalsProto.WorldEntityPrototype != PrototypeId.Invalid)) return BlueprintId.Invalid;

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.WorldEntityPrototype);
        }

        public static BlueprintId GetPopulationObjectBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;
            if (!Verify.IsNotNull(globalsProto)) return BlueprintId.Invalid;
            if (!Verify.IsTrue(globalsProto.PopulationObjectPrototype != PrototypeId.Invalid)) return BlueprintId.Invalid;

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.PopulationObjectPrototype);
        }

        public static BlueprintId GetPowerBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;
            if (!Verify.IsNotNull(globalsProto)) return BlueprintId.Invalid;
            if (!Verify.IsTrue(globalsProto.PowerPrototype != PrototypeId.Invalid)) return BlueprintId.Invalid;

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.PowerPrototype);
        }

        public static BlueprintId GetRegionBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;
            if (!Verify.IsNotNull(globalsProto)) return BlueprintId.Invalid;
            if (!Verify.IsTrue(globalsProto.RegionPrototype != PrototypeId.Invalid)) return BlueprintId.Invalid;

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.RegionPrototype);
        }

        public static BlueprintId GetAvatarBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;
            if (!Verify.IsNotNull(globalsProto)) return BlueprintId.Invalid;
            if (!Verify.IsTrue(globalsProto.AvatarPrototype != PrototypeId.Invalid)) return BlueprintId.Invalid;

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.AvatarPrototype);
        }

        public static BlueprintId GetConditionBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;
            if (!Verify.IsNotNull(globalsProto)) return BlueprintId.Invalid;
            if (!Verify.IsTrue(globalsProto.ConditionPrototype != PrototypeId.Invalid)) return BlueprintId.Invalid;

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.ConditionPrototype);
        }

        public static BlueprintId GetPublicEventBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;
            if (!Verify.IsNotNull(globalsProto)) return BlueprintId.Invalid;
            if (!Verify.IsTrue(globalsProto.PublicEventPrototype != PrototypeId.Invalid)) return BlueprintId.Invalid;

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.PublicEventPrototype);
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public static BlueprintId GetMetricsFrequencyBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;
            if (!Verify.IsNotNull(globalsProto)) return BlueprintId.Invalid;
            if (!Verify.IsTrue(globalsProto.MetricsFrequencyPrototype != PrototypeId.Invalid)) return BlueprintId.Invalid;

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.MetricsFrequencyPrototype);
        }
#endif

#if GAME_VERSION_1_53
        public static BlueprintId GetDifficultyTuningBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;
            if (!Verify.IsNotNull(globalsProto)) return BlueprintId.Invalid;
            if (!Verify.IsTrue(globalsProto.DifficultyTuningPrototype != PrototypeId.Invalid)) return BlueprintId.Invalid;

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.DifficultyTuningPrototype);
        }
#endif

        #endregion

        #region Data Init

        private void InitPerAreaTuningVars()
        {
            BlueprintId areaBlueprintRef = GetAreaBlueprintDataRef();
            if (!Verify.IsTrue(areaBlueprintRef != BlueprintId.Invalid)) return;

            int numAreaPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(areaBlueprintRef) + 1;
            _perAreaTuningVars = new(numAreaPrototypes);
            for (int i = 0; i < numAreaPrototypes; i++)
                _perAreaTuningVars.Add(new TuningVarArray((int)AreaTuningVar.eATV_NumAreaTuningVars));
        }

        private void InitPerLootTableTuningVars()
        {
            BlueprintId lootTableBlueprintRef = GetLootTableBlueprintDataRef();
            if (!Verify.IsTrue(lootTableBlueprintRef != BlueprintId.Invalid)) return;

            int numLootTablePrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(lootTableBlueprintRef) + 1;
            _perLootTableTuningVars = new(numLootTablePrototypes);
            for (int i = 0; i < numLootTablePrototypes; i++)
                _perLootTableTuningVars.Add(new TuningVarArray((int)LootTableTuningVar.eLTTV_NumLootTableTuningVars));
        }

        private void InitPerMissionTuningVars()
        {
            BlueprintId missionBlueprintRef = GetMissionBlueprintDataRef();
            if (!Verify.IsTrue(missionBlueprintRef != BlueprintId.Invalid)) return;

            int numMissionPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(missionBlueprintRef) + 1;
            _perMissionTuningVars = new(numMissionPrototypes);
            for (int i = 0; i < numMissionPrototypes; i++)
                _perMissionTuningVars.Add(new TuningVarArray((int)MissionTuningVar.eMTV_NumMissionTuningVars));
        }

        private void InitPerWorldEntityTuningVars()
        {
            BlueprintId worldEntityBlueprintRef = GetWorldEntityBlueprintDataRef();
            if (!Verify.IsTrue(worldEntityBlueprintRef != BlueprintId.Invalid)) return;

            int numWorldEntityPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(worldEntityBlueprintRef) + 1;
            _perWorldEntityTuningVars = new(numWorldEntityPrototypes);
            for (int i = 0; i < numWorldEntityPrototypes; i++)
                _perWorldEntityTuningVars.Add(new TuningVarArray((int)WorldEntityTuningVar.eWETV_NumWorldEntityTuningVars));
        }

        private void InitPerPopObjTuningVars()
        {
            BlueprintId popObjBlueprintRef = GetPopulationObjectBlueprintDataRef();
            if (!Verify.IsTrue(popObjBlueprintRef != BlueprintId.Invalid)) return;

            int numPopObjPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(popObjBlueprintRef) + 1;
            _perPopObjTuningVars = new(numPopObjPrototypes);
            for (int i = 0; i < numPopObjPrototypes; i++)
                _perPopObjTuningVars.Add(new TuningVarArray((int)PopObjTuningVar.ePOTV_NumPopulationObjectTuningVars));
        }

        private void InitPerPowerTuningVars()
        {
            BlueprintId powerBlueprintRef = GetPowerBlueprintDataRef();
            if (!Verify.IsTrue(powerBlueprintRef != BlueprintId.Invalid)) return;

            int numPowerPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(powerBlueprintRef) + 1;
            _perPowerTuningVars = new(numPowerPrototypes);
            for (int i = 0; i < numPowerPrototypes; i++)
                _perPowerTuningVars.Add(new TuningVarArray((int)PowerTuningVar.ePTV_NumPowerTuningVars));
        }

        private void InitPerRegionTuningVars()
        {
            BlueprintId regionBlueprintRef = GetRegionBlueprintDataRef();
            if (!Verify.IsTrue(regionBlueprintRef != BlueprintId.Invalid)) return;

            int numRegionPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(regionBlueprintRef) + 1;
            _perRegionTuningVars = new(numRegionPrototypes);
            for (int i = 0; i < numRegionPrototypes; i++)
                _perRegionTuningVars.Add(new TuningVarArray((int)RegionTuningVar.eRTV_NumRegionTuningVars));
        }

        private void InitPerAvatarTuningVars()
        {
            BlueprintId avatarBlueprintRef = GetAvatarBlueprintDataRef();
            if (!Verify.IsTrue(avatarBlueprintRef != BlueprintId.Invalid)) return;

            int numAvatarPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(avatarBlueprintRef) + 1;
            _perAvatarTuningVars = new(numAvatarPrototypes);
            for (int i = 0; i < numAvatarPrototypes; i++)
                _perAvatarTuningVars.Add(new TuningVarArray((int)AvatarEntityTuningVar.eAETV_NumAvatarEntityTuningVars));
        }

        private void InitPerConditionTuningVars()
        {
            BlueprintId conditionBlueprintRef = GetConditionBlueprintDataRef();
            if (!Verify.IsTrue(conditionBlueprintRef != BlueprintId.Invalid)) return;

            int numConditionPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(conditionBlueprintRef) + 1;
            _perConditionTuningVars = new(numConditionPrototypes);
            for (int i = 0; i < numConditionPrototypes; i++)
                _perConditionTuningVars.Add(new TuningVarArray((int)ConditionTuningVar.eCTV_NumConditionTuningVars));
        }

        private void InitPerPublicEventTuningVars()
        {
            BlueprintId publicEventBlueprintRef = GetPublicEventBlueprintDataRef();
            if (!Verify.IsTrue(publicEventBlueprintRef != BlueprintId.Invalid)) return;

            int numPublicEventPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(publicEventBlueprintRef) + 1;
            _perPublicEventTuningVars = new(numPublicEventPrototypes);
            for (int i = 0; i < numPublicEventPrototypes; i++)
                _perPublicEventTuningVars.Add(new TuningVarArray((int)PublicEventTuningVar.ePETV_NumPublicEventTuningVars));
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private void InitPerMetricsFrequencyTuningVars()
        {
            BlueprintId metricsFrequencyBlueprintRef = GetMetricsFrequencyBlueprintDataRef();
            if (!Verify.IsTrue(metricsFrequencyBlueprintRef != BlueprintId.Invalid)) return;

            int numMetricsFrequencyPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(metricsFrequencyBlueprintRef) + 1;
            _perMetricsFrequencyTuningVars = new(numMetricsFrequencyPrototypes);
            for (int i = 0; i < numMetricsFrequencyPrototypes; i++)
                _perMetricsFrequencyTuningVars.Add(new TuningVarArray((int)MetricsFrequencyTuningVar.eMFTV_NumMetricsFrequencyTuningVars));
        }
#endif

#if GAME_VERSION_1_53
        private void InitDifficultyTuningTuningVars()
        {
            BlueprintId difficultyTuningBlueprintRef = GetDifficultyTuningBlueprintDataRef();
            if (!Verify.IsTrue(difficultyTuningBlueprintRef != BlueprintId.Invalid)) return;

            int numDifficultyTuningPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(difficultyTuningBlueprintRef) + 1;
            _perDifficultyTuningTuningVars = new(numDifficultyTuningPrototypes);
            for (int i = 0; i < numDifficultyTuningPrototypes; i++)
                _perDifficultyTuningTuningVars.Add(new TuningVarArray((int)DifficultyTuningTuningVar.eDTTV_NumDifficultyTuningTuningVars));
        }
#endif

        #endregion

        #region Tuning Var Update Methods

        private void UpdateLiveAvatarTuningVar(PrototypeId avatarProtoRef, AvatarEntityTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < AvatarEntityTuningVar.eAETV_NumAvatarEntityTuningVars)) return;
            if (!Verify.IsTrue(avatarProtoRef != PrototypeId.Invalid)) return;

            BlueprintId avatarBlueprintRef = GetAvatarBlueprintDataRef();
            if (!Verify.IsTrue(avatarBlueprintRef != BlueprintId.Invalid)) return;

            int avatarEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(avatarProtoRef, avatarBlueprintRef);
            if (!Verify.IsTrue(avatarEnumVal >= 0 && avatarEnumVal < _perAvatarTuningVars.Count)) return;

            _perAvatarTuningVars[avatarEnumVal][(int)tuningVarEnum] = tuningVarValue;
            _updateProtobufOutOfDate = true;
        }

        private void UpdateLiveWorldEntityTuningVar(PrototypeId worldEntityProtoRef, WorldEntityTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < WorldEntityTuningVar.eWETV_NumWorldEntityTuningVars)) return;

            WorldEntityPrototype worldEntityProto = GameDatabase.GetPrototype<WorldEntityPrototype>(worldEntityProtoRef);
            if (!Verify.IsNotNull(worldEntityProto)) return;

            int worldEntityEnumVal = worldEntityProto.WorldEntityPrototypeEnumValue;

            if (tuningVarEnum == WorldEntityTuningVar.eWETV_LootGroupNum)
                UpdateLiveLootGroup(worldEntityProto, tuningVarValue);

            if (!Verify.IsTrue(worldEntityEnumVal >= 0 && worldEntityEnumVal < _perWorldEntityTuningVars.Count)) return;

            _perWorldEntityTuningVars[worldEntityEnumVal][(int)tuningVarEnum] = tuningVarValue;
            _updateProtobufOutOfDate |= ShouldSendTuningVarToClient(tuningVarEnum); // NOTE: No invalidation in client code here
        }

        private void UpdateLivePowerTuningVar(PrototypeId powerProtoRef, PowerTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < PowerTuningVar.ePTV_NumPowerTuningVars)) return;
            if (!Verify.IsTrue(powerProtoRef != PrototypeId.Invalid)) return;

            BlueprintId powerBlueprintRef = GetPowerBlueprintDataRef();
            if (!Verify.IsTrue(powerBlueprintRef != BlueprintId.Invalid)) return;

            int powerEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(powerProtoRef, powerBlueprintRef);
            if (!Verify.IsTrue(powerEnumVal >= 0 && powerEnumVal < _perPowerTuningVars.Count)) return;

            _perPowerTuningVars[powerEnumVal][(int)tuningVarEnum] = tuningVarValue;
            _updateProtobufOutOfDate = true;
        }

        private void UpdateLiveAreaTuningVar(PrototypeId areaProtoRef, AreaTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < AreaTuningVar.eATV_NumAreaTuningVars)) return;
            if (!Verify.IsTrue(areaProtoRef != PrototypeId.Invalid)) return;

            BlueprintId areaBlueprintRef = GetAreaBlueprintDataRef();
            if (!Verify.IsTrue(areaBlueprintRef != BlueprintId.Invalid)) return;

            int areaEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(areaProtoRef, areaBlueprintRef);
            if (!Verify.IsTrue(areaEnumVal >= 0 && areaEnumVal < _perAreaTuningVars.Count)) return;

            _perAreaTuningVars[areaEnumVal][(int)tuningVarEnum] = tuningVarValue;
            // Server-only live tuning?
        }

        private void UpdateLiveRegionTuningVar(PrototypeId regionProtoRef, RegionTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < RegionTuningVar.eRTV_NumRegionTuningVars)) return;
            if (!Verify.IsTrue(regionProtoRef != PrototypeId.Invalid)) return;

            BlueprintId regionBlueprintRef = GetRegionBlueprintDataRef();
            if (!Verify.IsTrue(regionBlueprintRef != BlueprintId.Invalid)) return;
            
            int regionEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(regionProtoRef, regionBlueprintRef);
            if (!Verify.IsTrue(regionEnumVal >= 0 && regionEnumVal < _perRegionTuningVars.Count)) return;

            _perRegionTuningVars[regionEnumVal][(int)tuningVarEnum] = tuningVarValue;
            _updateProtobufOutOfDate = true;
        }

        private void UpdateLivePopObjTuningVar(PrototypeId popObjProtoRef, PopObjTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < PopObjTuningVar.ePOTV_NumPopulationObjectTuningVars)) return;
            if (!Verify.IsTrue(popObjProtoRef != PrototypeId.Invalid)) return;
            
            BlueprintId popObjBlueprintRef = GetPopulationObjectBlueprintDataRef();
            if (!Verify.IsTrue(popObjBlueprintRef != BlueprintId.Invalid)) return;
            
            int popObjEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(popObjProtoRef, popObjBlueprintRef);
            if (!Verify.IsTrue(popObjEnumVal >= 0 && popObjEnumVal < _perPopObjTuningVars.Count)) return;

            _perPopObjTuningVars[popObjEnumVal][(int)tuningVarEnum] = tuningVarValue;
            // Server-only live tuning?
        }

        private void UpdateLiveMissionTuningVar(PrototypeId missionProtoRef, MissionTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < MissionTuningVar.eMTV_NumMissionTuningVars)) return;
            if (!Verify.IsTrue(missionProtoRef != PrototypeId.Invalid)) return;
            
            BlueprintId missionBlueprintRef = GetMissionBlueprintDataRef();
            if (!Verify.IsTrue(missionBlueprintRef != BlueprintId.Invalid)) return;

            int missionEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(missionProtoRef, missionBlueprintRef);
            if (!Verify.IsTrue(missionEnumVal >= 0 && missionEnumVal < _perMissionTuningVars.Count)) return;
            
            _perMissionTuningVars[missionEnumVal][(int)tuningVarEnum] = tuningVarValue;
            // Server-only live tuning?
        }

        private void UpdateLiveLootTableTuningVar(PrototypeId lootTableProtoRef, LootTableTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < LootTableTuningVar.eLTTV_NumLootTableTuningVars)) return;
            if (!Verify.IsTrue(lootTableProtoRef != PrototypeId.Invalid)) return;

            BlueprintId lootTableBlueprintRef = GetLootTableBlueprintDataRef();
            if (!Verify.IsTrue(lootTableBlueprintRef != BlueprintId.Invalid)) return;
            
            int lootTableEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(lootTableProtoRef, lootTableBlueprintRef);
            if (!Verify.IsTrue(lootTableEnumVal >= 0 && lootTableEnumVal < _perLootTableTuningVars.Count)) return;
            
            _perLootTableTuningVars[lootTableEnumVal][(int)tuningVarEnum] = tuningVarValue;
            // Server-only live tuning?
        }

        private void UpdateLiveConditionTuningVar(PrototypeId conditionProtoRef, ConditionTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < ConditionTuningVar.eCTV_NumConditionTuningVars)) return;
            if (!Verify.IsTrue(conditionProtoRef != PrototypeId.Invalid)) return;

            BlueprintId conditionBlueprintRef = GetConditionBlueprintDataRef();
            if (!Verify.IsTrue(conditionBlueprintRef != BlueprintId.Invalid)) return;

            int conditionEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(conditionProtoRef, conditionBlueprintRef);
            if (!Verify.IsTrue(conditionEnumVal >= 0 && conditionEnumVal < _perConditionTuningVars.Count)) return;

            _perConditionTuningVars[conditionEnumVal][(int)tuningVarEnum] = tuningVarValue;
            _updateProtobufOutOfDate = true;
        }

        private void UpdateLivePublicEventTuningVar(PrototypeId publicEventProtoRef, PublicEventTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < PublicEventTuningVar.ePETV_NumPublicEventTuningVars)) return;
            if (!Verify.IsTrue(publicEventProtoRef != PrototypeId.Invalid)) return;

            BlueprintId publicEventBlueprintRef = GetPublicEventBlueprintDataRef();
            if (!Verify.IsTrue(publicEventBlueprintRef != BlueprintId.Invalid)) return;

            int publicEventEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(publicEventProtoRef, publicEventBlueprintRef);
            if (!Verify.IsTrue(publicEventEnumVal >= 0 && publicEventEnumVal < _perPublicEventTuningVars.Count)) return;

            _perPublicEventTuningVars[publicEventEnumVal][(int)tuningVarEnum] = tuningVarValue;
            _updateProtobufOutOfDate = true;
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private void UpdateLiveMetricsFrequencyTuningVar(PrototypeId metricsFrequencyProtoRef, MetricsFrequencyTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < MetricsFrequencyTuningVar.eMFTV_NumMetricsFrequencyTuningVars)) return;
            if (!Verify.IsTrue(metricsFrequencyProtoRef != PrototypeId.Invalid)) return;

            BlueprintId metricsFrequencyBlueprintRef = GetMetricsFrequencyBlueprintDataRef();
            if (!Verify.IsTrue(metricsFrequencyBlueprintRef != BlueprintId.Invalid)) return;
            
            int metricsFrequencyEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(metricsFrequencyProtoRef, metricsFrequencyBlueprintRef);
            if (!Verify.IsTrue(metricsFrequencyEnumVal >= 0 && metricsFrequencyEnumVal < _perMetricsFrequencyTuningVars.Count)) return;
            
            _perMetricsFrequencyTuningVars[metricsFrequencyEnumVal][(int)tuningVarEnum] = tuningVarValue;
            // Server-only live tuning
        }
#endif

#if GAME_VERSION_1_53
        private void UpdateLiveDifficultyTuningTuningVar(PrototypeId difficultyTuningProtoRef, DifficultyTuningTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (!Verify.IsTrue(tuningVarEnum >= 0 && tuningVarEnum < DifficultyTuningTuningVar.eDTTV_NumDifficultyTuningTuningVars)) return;
            if (!Verify.IsTrue(difficultyTuningProtoRef != PrototypeId.Invalid)) return;

            BlueprintId difficultyTuningBlueprintRef = GetDifficultyTuningBlueprintDataRef();
            if (!Verify.IsTrue(difficultyTuningBlueprintRef != BlueprintId.Invalid)) return;

            int difficultyTuningEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(difficultyTuningProtoRef, difficultyTuningBlueprintRef);
            if (!Verify.IsTrue(difficultyTuningEnumVal >= 0 && difficultyTuningEnumVal < _perDifficultyTuningTuningVars.Count)) return;

            _perDifficultyTuningTuningVars[difficultyTuningEnumVal][(int)tuningVarEnum] = tuningVarValue;
            // Server-only live tuning
        }
#endif

        #endregion
    }
}
