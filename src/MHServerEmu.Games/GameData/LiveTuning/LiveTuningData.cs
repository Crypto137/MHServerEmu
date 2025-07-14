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
        private List<TuningVarArray> _perMetricsFrequencyTuningVars;

        private readonly Dictionary<int, List<WorldEntityPrototype>> _lootGroupDict = new();

        private NetMessageLiveTuningUpdate _updateProtobuf = NetMessageLiveTuningUpdate.DefaultInstance;
        private bool _updateProtobufOutOfDate = false;

        // Store LiveTuningData used by game instances per thread to reduce memory usage.
        [ThreadStatic]
        internal static LiveTuningData Current;

        public int ChangeNum { get; set; } = 0;

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
            InitPerMetricsFrequencyTuningVars();
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
            foreach (TuningVarArray tuningVarArray in _perMetricsFrequencyTuningVars)   tuningVarArray.Clear();

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

            for (int i = 0; i < _perMetricsFrequencyTuningVars.Count; i++)
                _perMetricsFrequencyTuningVars[i].Copy(other._perMetricsFrequencyTuningVars[i]);

            ClearLootGroups();

            foreach (var kvp in other._lootGroupDict)
            {
                List<WorldEntityPrototype> lootGroupCopy = new(kvp.Value);
                _lootGroupDict.Add(kvp.Key, lootGroupCopy);
            }

            ChangeNum = other.ChangeNum;
            _updateProtobufOutOfDate = true;
        }

        public bool UpdateLiveGlobalTuningVar(GlobalTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= GlobalTuningVar.eGTV_NumGlobalTuningVars)
                return Logger.WarnReturn(false, "UpdateLiveGlobalTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= GlobalTuningVar.eGTV_NumGlobalTuningVars");

            _globalTuningVars[(int)tuningVarEnum] = tuningVarValue;
            _updateProtobufOutOfDate = true;

            return true;
        }

        public bool UpdateLiveTuningVar(PrototypeId tuningVarProtoRef, int tuningVarEnum, float tuningVarValue)
        {
            if (tuningVarProtoRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, "UpdateLiveTuningVar(): tuningVarProtoRef == PrototypeId.Invalid");

            Prototype prototype = GameDatabase.GetPrototype<Prototype>(tuningVarProtoRef);

            if (prototype is AvatarPrototype)
                return UpdateLiveAvatarTuningVar(tuningVarProtoRef, (AvatarEntityTuningVar)tuningVarEnum, tuningVarValue);

            if (prototype is WorldEntityPrototype)
                return UpdateLiveWorldEntityTuningVar(tuningVarProtoRef, (WorldEntityTuningVar)tuningVarEnum, tuningVarValue);

            if (prototype is PowerPrototype)
                return UpdateLivePowerTuningVar(tuningVarProtoRef, (PowerTuningVar)tuningVarEnum, tuningVarValue);

            if (prototype is AreaPrototype)
                return UpdateLiveAreaTuningVar(tuningVarProtoRef, (AreaTuningVar)tuningVarEnum, tuningVarValue);

            if (prototype is RegionPrototype)
                return UpdateLiveRegionTuningVar(tuningVarProtoRef, (RegionTuningVar)tuningVarEnum, tuningVarValue);

            if (prototype is PopulationObjectPrototype)
                return UpdateLivePopObjTuningVar(tuningVarProtoRef, (PopObjTuningVar)tuningVarEnum, tuningVarValue);

            if (prototype is MissionPrototype)
                return UpdateLiveMissionTuningVar(tuningVarProtoRef, (MissionTuningVar)tuningVarEnum, tuningVarValue);

            if (prototype is LootTablePrototype)
                return UpdateLiveLootTableTuningVar(tuningVarProtoRef, (LootTableTuningVar)tuningVarEnum, tuningVarValue);

            if (prototype is ConditionPrototype)
                return UpdateLiveConditionTuningVar(tuningVarProtoRef, (ConditionTuningVar)tuningVarEnum, tuningVarValue);

            if (prototype is PublicEventPrototype)
                return UpdateLivePublicEventTuningVar(tuningVarProtoRef, (PublicEventTuningVar)tuningVarEnum, tuningVarValue);

            if (prototype is MetricsFrequencyPrototype)
                return UpdateLiveMetricsFrequencyTuningVar(tuningVarProtoRef, (MetricsFrequencyTuningVar)tuningVarEnum, tuningVarValue);

            return false;
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
                    // Only these two world entity tuning vars are sent to the client
                    if (j != (int)WorldEntityTuningVar.eWETV_Enabled && j != (int)WorldEntityTuningVar.eWETV_Visible)
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
            bool found = _lootGroupDict.TryGetValue(lootGroupNum, out List<WorldEntityPrototype> worldEntityProtoList);
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
                if (_lootGroupDict.TryGetValue(currentLootGroupNum, out List<WorldEntityPrototype> lootGroup))
                    lootGroup.Remove(worldEntityProto);
            }

            // Add to the new group if its not default value
            if (newLootGroupNum != DefaultTuningVarValue)
            {
                if (_lootGroupDict.TryGetValue(newLootGroupNum, out List<WorldEntityPrototype> lootGroup) == false)
                {
                    lootGroup = new();
                    _lootGroupDict.Add(newLootGroupNum, lootGroup);
                }

                lootGroup.Add(worldEntityProto);
            }
        }

        private void ClearLootGroups()
        {
            _lootGroupDict.Clear();
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
            if (prototype is MetricsFrequencyPrototype) return ((MetricsFrequencyTuningVar)tuningVarEnum).ToString();

            return tuningVarEnum.ToString();
        }

        #region Tuning Var Accesors

        public float GetLiveGlobalTuningVar(GlobalTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= GlobalTuningVar.eGTV_NumGlobalTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveGlobalTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= GlobalTuningVar.eGTV_NumGlobalTuningVars");

            return _globalTuningVars[(int)tuningVarEnum];
        }

        public float GetLiveAreaTuningVar(AreaPrototype areaProto, AreaTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= AreaTuningVar.eATV_NumAreaTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveAreaTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= AreaTuningVar.eATV_NumAreaTuningVars");

            int areaEnumVal = areaProto.AreaPrototypeEnumValue;
            if (areaEnumVal < 0 || areaEnumVal >= _perAreaTuningVars.Count)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveAreaTuningVar(): areaEnumVal < 0 || areaEnumVal >= _perAreaTuningVars.Count");

            return _perAreaTuningVars[areaEnumVal][(int)tuningVarEnum];
        }

        public float GetLiveLootTableTuningVar(LootTablePrototype lootTableProto, LootTableTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= LootTableTuningVar.eLTTV_NumLootTableTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveLootTableTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= LootTableTuningVar.eLTTV_NumLootTableTuningVars");

            int lootTableEnumVal = lootTableProto.LootTablePrototypeEnumValue;
            if (lootTableEnumVal < 0 || lootTableEnumVal >= _perLootTableTuningVars.Count)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveLootTableTuningVar(): lootTableEnumVal < 0 || lootTableEnumVal >= _perLootTableTuningVars.Count");

            return _perLootTableTuningVars[lootTableEnumVal][(int)tuningVarEnum];
        }

        public float GetLiveMissionTuningVar(MissionPrototype missionProto, MissionTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= MissionTuningVar.eMTV_NumMissionTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveMissionTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= MissionTuningVar.eMTV_NumMissionTuningVars");

            int missionEnumVal = missionProto.MissionPrototypeEnumValue;
            if (missionEnumVal < 0 || missionEnumVal >= _perMissionTuningVars.Count)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveLootTableTuningVar(): missionEnumVal < 0 || missionEnumVal >= _perMissionTuningVars.Count");

            return _perMissionTuningVars[missionEnumVal][(int)tuningVarEnum];
        }

        public float GetLiveWorldEntityTuningVar(WorldEntityPrototype worldEntityProto, WorldEntityTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= WorldEntityTuningVar.eWETV_NumWorldEntityTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveWorldEntityTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= WorldEntityTuningVar.eWETV_NumWorldEntityTuningVars");

            int worldEntityEnumVal = worldEntityProto.WorldEntityPrototypeEnumValue;
            if (worldEntityEnumVal < 0 || worldEntityEnumVal >= _perWorldEntityTuningVars.Count)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveWorldEntityTuningVar(): worldEntityEnumVal < 0 || worldEntityEnumVal >= _perWorldEntityTuningVars.Count");

            return _perWorldEntityTuningVars[worldEntityEnumVal][(int)tuningVarEnum];
        }

        public float GetLivePopObjTuningVar(PopulationObjectPrototype popObjProto, PopObjTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= PopObjTuningVar.ePOTV_NumPopulationObjectTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLivePopObjTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= PopObjTuningVar.ePOTV_NumPopulationObjectTuningVars");

            int popObjEnumVal = popObjProto.PopulationObjectPrototypeEnumValue;
            if (popObjEnumVal < 0 || popObjEnumVal >= _perPopObjTuningVars.Count)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLivePopObjTuningVar(): popObjEnumVal < 0 || popObjEnumVal >= _perPopObjTuningVars.Count");

            return _perPopObjTuningVars[popObjEnumVal][(int)tuningVarEnum];
        }

        public float GetLivePowerTuningVar(PowerPrototype powerProto, PowerTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= PowerTuningVar.ePTV_NumPowerTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLivePowerTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= PowerTuningVar.ePTV_NumPowerTuningVars");

            int powerEnumVal = powerProto.PowerPrototypeEnumValue;
            if (powerEnumVal < 0 || powerEnumVal >= _perPowerTuningVars.Count)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLivePowerTuningVar(): powerEnumVal < 0 || powerEnumVal >= _perPowerTuningVars.Count");

            return _perPowerTuningVars[powerEnumVal][(int)tuningVarEnum];
        }

        public float GetLiveRegionTuningVar(RegionPrototype regionProto, RegionTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= RegionTuningVar.eRTV_NumRegionTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveRegionTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= RegionTuningVar.eRTV_NumRegionTuningVars");

            int regionEnumVal = regionProto.RegionPrototypeEnumValue;
            if (regionEnumVal < 0 || regionEnumVal >= _perRegionTuningVars.Count)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveRegionTuningVar(): regionEnumVal < 0 || regionEnumVal >= _perRegionTuningVars.Count");

            return _perRegionTuningVars[regionEnumVal][(int)tuningVarEnum];
        }

        public float GetLiveAvatarTuningVar(AvatarPrototype avatarProto, AvatarEntityTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= AvatarEntityTuningVar.eAETV_NumAvatarEntityTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveAvatarTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= AvatarEntityTuningVar.eAETV_NumAvatarEntityTuningVars");

            int avatarEnumVal = avatarProto.AvatarPrototypeEnumValue;
            if (avatarEnumVal < 0 || avatarEnumVal >= _perAvatarTuningVars.Count)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveAvatarTuningVar(): avatarEnumVal < 0 || avatarEnumVal >= _perAvatarTuningVars.Count");

            return _perAvatarTuningVars[avatarEnumVal][(int)tuningVarEnum];
        }

        public float GetLiveConditionTuningVar(ConditionPrototype conditionProto, ConditionTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= ConditionTuningVar.eCTV_NumConditionTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveConditionTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= ConditionTuningVar.eCTV_NumConditionTuningVars");

            int conditionEnumVal = conditionProto.ConditionPrototypeEnumValue;
            if (conditionEnumVal < 0 || conditionEnumVal >= _perConditionTuningVars.Count)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveConditionTuningVar(): conditionEnumVal < 0 || conditionEnumVal >= _perConditionTuningVars.Count");

            return _perConditionTuningVars[conditionEnumVal][(int)tuningVarEnum];
        }

        public float GetLivePublicEventTuningVar(PublicEventPrototype publicEventProto, PublicEventTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= PublicEventTuningVar.ePETV_NumPublicEventTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLivePublicEventTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= PublicEventTuningVar.ePETV_NumPublicEventTuningVars");

            int publicEventEnumVal = publicEventProto.PublicEventPrototypeEnumValue;
            if (publicEventEnumVal < 0 || publicEventEnumVal >= _perPublicEventTuningVars.Count)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLivePublicEventTuningVar(): publicEventEnumVal < 0 || publicEventEnumVal >= _perPublicEventTuningVars.Count");

            return _perPublicEventTuningVars[publicEventEnumVal][(int)tuningVarEnum];
        }

        public float GetLiveMetricsFrequencyTuningVar(MetricsFrequencyPrototype metricsFrequencyProto, MetricsFrequencyTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= MetricsFrequencyTuningVar.eMFTV_NumMetricsFrequencyTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLivePublicEventTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= MetricsFrequencyTuningVar.eMFTV_NumMetricsFrequencyTuningVars");

            int metricsFrequencyEnumVal = metricsFrequencyProto.MetricsFrequencyPrototypeEnumValue;
            if (metricsFrequencyEnumVal < 0 || metricsFrequencyEnumVal >= _perMetricsFrequencyTuningVars.Count)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveMetricsFrequencyTuningVar(): metricsFrequencyEnumVal < 0 || metricsFrequencyEnumVal >= _perMetricsFrequencyTuningVars.Count");

            return _perMetricsFrequencyTuningVars[metricsFrequencyEnumVal][(int)tuningVarEnum];
        }

        #endregion

        #region Private Tuning Var Accessors (for protobuf generation)

        private float GetLiveWorldEntityTuningVar(int worldEntityEnumVal, WorldEntityTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= WorldEntityTuningVar.eWETV_NumWorldEntityTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveWorldEntityTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= WorldEntityTuningVar.eWETV_NumWorldEntityTuningVars");

            if (worldEntityEnumVal < 0 || worldEntityEnumVal >= _perWorldEntityTuningVars.Count)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveWorldEntityTuningVar(): worldEntityEnumVal < 0 || worldEntityEnumVal >= _perWorldEntityTuningVars.Count");

            return _perWorldEntityTuningVars[worldEntityEnumVal][(int)tuningVarEnum];
        }

        private float GetLivePowerTuningVar(int powerEnumVal, PowerTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= PowerTuningVar.ePTV_NumPowerTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLivePowerTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= PowerTuningVar.ePTV_NumPowerTuningVars");

            if (powerEnumVal < 0 || powerEnumVal >= _perPowerTuningVars.Count)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLivePowerTuningVar(): powerEnumVal < 0 || powerEnumVal >= _perPowerTuningVars.Count");

            return _perPowerTuningVars[powerEnumVal][(int)tuningVarEnum];
        }

        private float GetLiveRegionTuningVar(int regionEnumVal, RegionTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= RegionTuningVar.eRTV_NumRegionTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveRegionTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= RegionTuningVar.eRTV_NumRegionTuningVars");

            if (regionEnumVal < 0 || regionEnumVal >= _perRegionTuningVars.Count)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveRegionTuningVar(): regionEnumVal < 0 || regionEnumVal >= _perRegionTuningVars.Count");

            return _perRegionTuningVars[regionEnumVal][(int)tuningVarEnum];
        }

        private float GetLiveAvatarTuningVar(int avatarEnumVal, AvatarEntityTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= AvatarEntityTuningVar.eAETV_NumAvatarEntityTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveAvatarTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= AvatarEntityTuningVar.eAETV_NumAvatarEntityTuningVars");

            if (avatarEnumVal < 0 || avatarEnumVal >= _perAvatarTuningVars.Count)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLiveAvatarTuningVar(): avatarEnumVal < 0 || avatarEnumVal >= _perAvatarTuningVars.Count");

            return _perAvatarTuningVars[avatarEnumVal][(int)tuningVarEnum];
        }

        private float GetLivePublicEventTuningVar(int publicEventEnumVal, PublicEventTuningVar tuningVarEnum)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= PublicEventTuningVar.ePETV_NumPublicEventTuningVars)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLivePublicEventTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= PublicEventTuningVar.ePETV_NumPublicEventTuningVars");

            if (publicEventEnumVal < 0 || publicEventEnumVal >= _perPublicEventTuningVars.Count)
                return Logger.WarnReturn(DefaultTuningVarValue, $"GetLivePublicEventTuningVar(): publicEventEnumVal < 0 || publicEventEnumVal >= _perPublicEventTuningVars.Count");

            return _perPublicEventTuningVars[publicEventEnumVal][(int)tuningVarEnum];
        }

        #endregion

        #region Global Bluepring Data Ref Accessors

        public static BlueprintId GetAreaBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;

            if (globalsProto == null)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetAreaBlueprintDataRef(): globalsProto == null");

            if (globalsProto.AreaPrototype == PrototypeId.Invalid)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetAreaBlueprintDataRef(): globalsProto.AreaPrototype == PrototypeId.Invalid");

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.AreaPrototype);
        }

        public static BlueprintId GetLootTableBlueprintDataRef()
        {
            LootGlobalsPrototype lootGlobalsProto = GameDatabase.LootGlobalsPrototype;

            if (lootGlobalsProto == null)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetLootTableBlueprintDataRef(): lootGlobalsProto == null");

            if (lootGlobalsProto.LootTableBlueprint == PrototypeId.Invalid)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetLootTableBlueprintDataRef(): lootGlobalsProto.LootTableBlueprint == PrototypeId.Invalid");

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(lootGlobalsProto.LootTableBlueprint);
        }

        public static BlueprintId GetMissionBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;

            if (globalsProto == null)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetMissionBlueprintDataRef(): globalsProto == null");

            if (globalsProto.MissionPrototype == PrototypeId.Invalid)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetMissionBlueprintDataRef(): globalsProto.MissionPrototype == PrototypeId.Invalid");

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.MissionPrototype);
        }

        public static BlueprintId GetWorldEntityBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;

            if (globalsProto == null)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetWorldEntityBlueprintDataRef(): globalsProto == null");

            if (globalsProto.WorldEntityPrototype == PrototypeId.Invalid)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetWorldEntityBlueprintDataRef(): globalsProto.WorldEntityPrototype == PrototypeId.Invalid");

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.WorldEntityPrototype);
        }

        public static BlueprintId GetPopulationObjectBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;

            if (globalsProto == null)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetPopulationObjectBlueprintDataRef(): globalsProto == null");

            if (globalsProto.PopulationObjectPrototype == PrototypeId.Invalid)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetPopulationObjectBlueprintDataRef(): globalsProto.PopulationObjectPrototype == PrototypeId.Invalid");

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.PopulationObjectPrototype);
        }

        public static BlueprintId GetPowerBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;

            if (globalsProto == null)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetPowerBlueprintDataRef(): globalsProto == null");

            if (globalsProto.PowerPrototype == PrototypeId.Invalid)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetPowerBlueprintDataRef(): globalsProto.PowerPrototype == PrototypeId.Invalid");

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.PowerPrototype);
        }

        public static BlueprintId GetRegionBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;

            if (globalsProto == null)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetRegionBlueprintDataRef(): globalsProto == null");

            if (globalsProto.RegionPrototype == PrototypeId.Invalid)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetRegionBlueprintDataRef(): globalsProto.RegionPrototype == PrototypeId.Invalid");

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.RegionPrototype);
        }

        public static BlueprintId GetAvatarBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;

            if (globalsProto == null)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetAvatarBlueprintDataRef(): globalsProto == null");

            if (globalsProto.AvatarPrototype == PrototypeId.Invalid)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetAvatarBlueprintDataRef(): globalsProto.AvatarPrototype == PrototypeId.Invalid");

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.AvatarPrototype);
        }

        public static BlueprintId GetConditionBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;

            if (globalsProto == null)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetConditionBlueprintDataRef(): globalsProto == null");

            if (globalsProto.ConditionPrototype == PrototypeId.Invalid)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetConditionBlueprintDataRef(): globalsProto.ConditionPrototype == PrototypeId.Invalid");

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.ConditionPrototype);
        }

        public static BlueprintId GetPublicEventBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;

            if (globalsProto == null)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetPublicEventBlueprintDataRef(): globalsProto == null");

            if (globalsProto.PublicEventPrototype == PrototypeId.Invalid)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetPublicEventBlueprintDataRef(): globalsProto.PublicEventPrototype == PrototypeId.Invalid");

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.PublicEventPrototype);
        }

        public static BlueprintId GetMetricsFrequencyBlueprintDataRef()
        {
            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;

            if (globalsProto == null)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetMetricsFrequencyBlueprintDataRef(): globalsProto == null");

            if (globalsProto.MetricsFrequencyPrototype == PrototypeId.Invalid)
                return Logger.WarnReturn(BlueprintId.Invalid, "GetMetricsFrequencyBlueprintDataRef(): globalsProto.MetricsFrequencyPrototype == PrototypeId.Invalid");

            return DataDirectory.Instance.GetPrototypeBlueprintDataRef(globalsProto.MetricsFrequencyPrototype);
        }

        #endregion

        #region Data Init

        private bool InitPerAreaTuningVars()
        {
            BlueprintId areaBlueprintRef = GetAreaBlueprintDataRef();
            if (areaBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, "InitPerAreaTuningVars(): areaBlueprintRef == BlueprintId.Invalid");

            int numAreaPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(areaBlueprintRef) + 1;
            _perAreaTuningVars = new(numAreaPrototypes);
            for (int i = 0; i < numAreaPrototypes; i++)
                _perAreaTuningVars.Add(new TuningVarArray((int)AreaTuningVar.eATV_NumAreaTuningVars));

            return true;
        }

        private bool InitPerLootTableTuningVars()
        {
            BlueprintId lootTableBlueprintRef = GetLootTableBlueprintDataRef();
            if (lootTableBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, "InitPerLootTableTuningVars(): lootTableBlueprintRef == BlueprintId.Invalid");

            int numLootTablePrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(lootTableBlueprintRef) + 1;
            _perLootTableTuningVars = new(numLootTablePrototypes);
            for (int i = 0; i < numLootTablePrototypes; i++)
                _perLootTableTuningVars.Add(new TuningVarArray((int)LootTableTuningVar.eLTTV_NumLootTableTuningVars));

            return true;
        }

        private bool InitPerMissionTuningVars()
        {
            BlueprintId missionBlueprintRef = GetMissionBlueprintDataRef();
            if (missionBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, "InitPerMissionTuningVars(): missionBlueprintRef == BlueprintId.Invalid");

            int numMissionPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(missionBlueprintRef) + 1;
            _perMissionTuningVars = new(numMissionPrototypes);
            for (int i = 0; i < numMissionPrototypes; i++)
                _perMissionTuningVars.Add(new TuningVarArray((int)MissionTuningVar.eMTV_NumMissionTuningVars));

            return true;
        }

        private bool InitPerWorldEntityTuningVars()
        {
            BlueprintId worldEntityBlueprintRef = GetWorldEntityBlueprintDataRef();
            if (worldEntityBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, "InitPerWorldEntityTuningVars(): worldEntityBlueprintRef == BlueprintId.Invalid");

            int numWorldEntityPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(worldEntityBlueprintRef) + 1;
            _perWorldEntityTuningVars = new(numWorldEntityPrototypes);
            for (int i = 0; i < numWorldEntityPrototypes; i++)
                _perWorldEntityTuningVars.Add(new TuningVarArray((int)WorldEntityTuningVar.eWETV_NumWorldEntityTuningVars));

            return true;
        }

        private bool InitPerPopObjTuningVars()
        {
            BlueprintId popObjBlueprintRef = GetPopulationObjectBlueprintDataRef();
            if (popObjBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, "InitPerPopObjTuningVars(): popObjBlueprintRef == BlueprintId.Invalid");

            int numPopObjPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(popObjBlueprintRef) + 1;
            _perPopObjTuningVars = new(numPopObjPrototypes);
            for (int i = 0; i < numPopObjPrototypes; i++)
                _perPopObjTuningVars.Add(new TuningVarArray((int)PopObjTuningVar.ePOTV_NumPopulationObjectTuningVars));

            return true;
        }

        private bool InitPerPowerTuningVars()
        {
            BlueprintId powerBlueprintRef = GetPowerBlueprintDataRef();
            if (powerBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, "InitPerPowerTuningVars(): powerBlueprintRef == BlueprintId.Invalid");

            int numPowerPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(powerBlueprintRef) + 1;
            _perPowerTuningVars = new(numPowerPrototypes);
            for (int i = 0; i < numPowerPrototypes; i++)
                _perPowerTuningVars.Add(new TuningVarArray((int)PowerTuningVar.ePTV_NumPowerTuningVars));

            return true;
        }

        private bool InitPerRegionTuningVars()
        {
            BlueprintId regionBlueprintRef = GetRegionBlueprintDataRef();
            if (regionBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, "InitPerRegionTuningVars(): regionBlueprintRef == BlueprintId.Invalid");

            int numRegionPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(regionBlueprintRef) + 1;
            _perRegionTuningVars = new(numRegionPrototypes);
            for (int i = 0; i < numRegionPrototypes; i++)
                _perRegionTuningVars.Add(new TuningVarArray((int)RegionTuningVar.eRTV_NumRegionTuningVars));

            return true;
        }

        private bool InitPerAvatarTuningVars()
        {
            BlueprintId avatarBlueprintRef = GetAvatarBlueprintDataRef();
            if (avatarBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, "InitPerAvatarTuningVars(): avatarBlueprintRef == BlueprintId.Invalid");

            int numAvatarPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(avatarBlueprintRef) + 1;
            _perAvatarTuningVars = new(numAvatarPrototypes);
            for (int i = 0; i < numAvatarPrototypes; i++)
                _perAvatarTuningVars.Add(new TuningVarArray((int)AvatarEntityTuningVar.eAETV_NumAvatarEntityTuningVars));

            return true;
        }

        private bool InitPerConditionTuningVars()
        {
            BlueprintId conditionBlueprintRef = GetConditionBlueprintDataRef();
            if (conditionBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, "InitPerConditionTuningVars(): conditionBlueprintRef == BlueprintId.Invalid");

            int numConditionPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(conditionBlueprintRef) + 1;
            _perConditionTuningVars = new(numConditionPrototypes);
            for (int i = 0; i < numConditionPrototypes; i++)
                _perConditionTuningVars.Add(new TuningVarArray((int)ConditionTuningVar.eCTV_NumConditionTuningVars));

            return true;
        }

        private bool InitPerPublicEventTuningVars()
        {
            BlueprintId publicEventBlueprintRef = GetPublicEventBlueprintDataRef();
            if (publicEventBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, "InitPerPublicEventTuningVars(): publicEventBlueprintRef == BlueprintId.Invalid");

            int numPublicEventPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(publicEventBlueprintRef) + 1;
            _perPublicEventTuningVars = new(numPublicEventPrototypes);
            for (int i = 0; i < numPublicEventPrototypes; i++)
                _perPublicEventTuningVars.Add(new TuningVarArray((int)PublicEventTuningVar.ePETV_NumPublicEventTuningVars));

            return true;
        }

        private bool InitPerMetricsFrequencyTuningVars()
        {
            BlueprintId metricsFrequencyBlueprintRef = GetMetricsFrequencyBlueprintDataRef();
            if (metricsFrequencyBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, "InitPerMetricsFrequencyTuningVars(): metricsFrequencyBlueprintRef == BlueprintId.Invalid");

            int numMetricsFrequencyPrototypes = DataDirectory.Instance.GetPrototypeMaxEnumValue(metricsFrequencyBlueprintRef) + 1;
            _perMetricsFrequencyTuningVars = new(numMetricsFrequencyPrototypes);
            for (int i = 0; i < numMetricsFrequencyPrototypes; i++)
                _perMetricsFrequencyTuningVars.Add(new TuningVarArray((int)MetricsFrequencyTuningVar.eMFTV_NumMetricsFrequencyTuningVars));

            return true;
        }

        #endregion

        #region Tuning Var Update Methods

        private bool UpdateLiveAvatarTuningVar(PrototypeId avatarProtoRef, AvatarEntityTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= AvatarEntityTuningVar.eAETV_NumAvatarEntityTuningVars)
                return Logger.WarnReturn(false, $"UpdateLiveAvatarTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= AvatarEntityTuningVar.eAETV_NumAvatarEntityTuningVars");

            if (avatarProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, $"UpdateLiveAvatarTuningVar(): avatarProtoRef == PrototypeId.Invalid");

            BlueprintId avatarBlueprintRef = GetAvatarBlueprintDataRef();
            if (avatarBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, $"UpdateLiveAvatarTuningVar(): avatarBlueprintRef == BlueprintId.Invalid");

            int avatarEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(avatarProtoRef, avatarBlueprintRef);
            if (avatarEnumVal < 0 || avatarEnumVal >= _perAvatarTuningVars.Count)
                return Logger.WarnReturn(false, $"UpdateLiveAvatarTuningVar(): avatarEnumVal < 0 || avatarEnumVal >= _perAvatarTuningVars.Count");

            _perAvatarTuningVars[avatarEnumVal][(int)tuningVarEnum] = tuningVarValue;
            _updateProtobufOutOfDate = true;

            return true;
        }

        private bool UpdateLiveWorldEntityTuningVar(PrototypeId worldEntityProtoRef, WorldEntityTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= WorldEntityTuningVar.eWETV_NumWorldEntityTuningVars)
                return Logger.WarnReturn(false, $"UpdateLiveWorldEntityTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= WorldEntityTuningVar.eWETV_NumWorldEntityTuningVars");

            WorldEntityPrototype worldEntityProto = GameDatabase.GetPrototype<WorldEntityPrototype>(worldEntityProtoRef);
            if (worldEntityProto == null) return Logger.WarnReturn(false, "UpdateLiveWorldEntityTuningVar(): worldEntityProto == null");

            int worldEntityEnumVal = worldEntityProto.WorldEntityPrototypeEnumValue;

            if (tuningVarEnum == WorldEntityTuningVar.eWETV_LootGroupNum)
                UpdateLiveLootGroup(worldEntityProto, tuningVarValue);

            if (worldEntityEnumVal < 0 || worldEntityEnumVal >= _perWorldEntityTuningVars.Count)
                return Logger.WarnReturn(false, $"UpdateLiveWorldEntityTuningVar(): worldEntityEnumVal < 0 || worldEntityEnumVal >= _perWorldEntityTuningVars.Count");

            _perWorldEntityTuningVars[worldEntityEnumVal][(int)tuningVarEnum] = tuningVarValue;
            // No update protobuf invalidation?

            return true;
        }

        private bool UpdateLivePowerTuningVar(PrototypeId powerProtoRef, PowerTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= PowerTuningVar.ePTV_NumPowerTuningVars)
                return Logger.WarnReturn(false, $"UpdateLivePowerTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= PowerTuningVar.ePTV_NumPowerTuningVars");

            if (powerProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, $"UpdateLivePowerTuningVar(): powerProtoRef == PrototypeId.Invalid");

            BlueprintId powerBlueprintRef = GetPowerBlueprintDataRef();
            if (powerBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, $"UpdateLivePowerTuningVar(): powerBlueprintRef == BlueprintId.Invalid");

            int powerEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(powerProtoRef, powerBlueprintRef);
            if (powerEnumVal < 0 || powerEnumVal >= _perPowerTuningVars.Count)
                return Logger.WarnReturn(false, $"UpdateLivePowerTuningVar(): powerEnumVal < 0 || powerEnumVal >= _perPowerTuningVars.Count");

            _perPowerTuningVars[powerEnumVal][(int)tuningVarEnum] = tuningVarValue;
            _updateProtobufOutOfDate = true;

            return true;
        }

        private bool UpdateLiveAreaTuningVar(PrototypeId areaProtoRef, AreaTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= AreaTuningVar.eATV_NumAreaTuningVars)
                return Logger.WarnReturn(false, $"UpdateLiveAreaTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= AreaTuningVar.eATV_NumAreaTuningVars");

            if (areaProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, $"UpdateLiveAreaTuningVar(): areaProtoRef == PrototypeId.Invalid");

            BlueprintId areaBlueprintRef = GetAreaBlueprintDataRef();
            if (areaBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, $"UpdateLiveAreaTuningVar(): areaBlueprintRef == BlueprintId.Invalid");

            int areaEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(areaProtoRef, areaBlueprintRef);
            if (areaEnumVal < 0 || areaEnumVal >= _perAreaTuningVars.Count)
                return Logger.WarnReturn(false, $"UpdateLiveAreaTuningVar(): areaEnumVal < 0 || areaEnumVal >= _perAreaTuningVars.Count");

            _perAreaTuningVars[areaEnumVal][(int)tuningVarEnum] = tuningVarValue;
            // Server-only live tuning?

            return true;
        }

        private bool UpdateLiveRegionTuningVar(PrototypeId regionProtoRef, RegionTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= RegionTuningVar.eRTV_NumRegionTuningVars)
                return Logger.WarnReturn(false, $"UpdateLiveRegionTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= RegionTuningVar.eRTV_NumRegionTuningVars");

            if (regionProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, $"UpdateLiveRegionTuningVar(): regionProtoRef == PrototypeId.Invalid");

            BlueprintId regionBlueprintRef = GetRegionBlueprintDataRef();
            if (regionBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, $"UpdateLiveRegionTuningVar(): regionBlueprintRef == BlueprintId.Invalid");

            int regionEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(regionProtoRef, regionBlueprintRef);
            if (regionEnumVal < 0 || regionEnumVal >= _perRegionTuningVars.Count)
                return Logger.WarnReturn(false, $"UpdateLiveRegionTuningVar(): regionEnumVal < 0 || regionEnumVal >= _perRegionTuningVars.Count");

            _perRegionTuningVars[regionEnumVal][(int)tuningVarEnum] = tuningVarValue;
            _updateProtobufOutOfDate = true;

            return true;
        }

        private bool UpdateLivePopObjTuningVar(PrototypeId popObjProtoRef, PopObjTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= PopObjTuningVar.ePOTV_NumPopulationObjectTuningVars)
                return Logger.WarnReturn(false, $"UpdateLivePopObjTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= PopObjTuningVar.ePOTV_NumPopulationObjectTuningVars");

            if (popObjProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, $"UpdateLivePopObjTuningVar(): popObjProtoRef == PrototypeId.Invalid");

            BlueprintId popObjBlueprintRef = GetPopulationObjectBlueprintDataRef();
            if (popObjBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, $"UpdateLivePopObjTuningVar(): popObjBlueprintRef == BlueprintId.Invalid");

            int popObjEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(popObjProtoRef, popObjBlueprintRef);
            if (popObjEnumVal < 0 || popObjEnumVal >= _perPopObjTuningVars.Count)
                return Logger.WarnReturn(false, $"UpdateLivePopObjTuningVar(): popObjEnumVal < 0 || popObjEnumVal >= _perPopObjTuningVars.Count");

            _perPopObjTuningVars[popObjEnumVal][(int)tuningVarEnum] = tuningVarValue;
            // Server-only live tuning?

            return true;
        }

        private bool UpdateLiveMissionTuningVar(PrototypeId missionProtoRef, MissionTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= MissionTuningVar.eMTV_NumMissionTuningVars)
                return Logger.WarnReturn(false, $"UpdateLiveMissionTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= MissionTuningVar.eMTV_NumMissionTuningVars");

            if (missionProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, $"UpdateLiveMissionTuningVar(): missionProtoRef == PrototypeId.Invalid");

            BlueprintId missionBlueprintRef = GetMissionBlueprintDataRef();
            if (missionBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, $"UpdateLiveMissionTuningVar(): missionBlueprintRef == BlueprintId.Invalid");

            int missionEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(missionProtoRef, missionBlueprintRef);
            if (missionEnumVal < 0 || missionEnumVal >= _perMissionTuningVars.Count)
                return Logger.WarnReturn(false, $"UpdateLiveMissionTuningVar(): missionEnumVal < 0 || missionEnumVal >= _perMissionTuningVars.Count");

            _perMissionTuningVars[missionEnumVal][(int)tuningVarEnum] = tuningVarValue;
            // Server-only live tuning?

            return true;
        }

        private bool UpdateLiveLootTableTuningVar(PrototypeId lootTableProtoRef, LootTableTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= LootTableTuningVar.eLTTV_NumLootTableTuningVars)
                return Logger.WarnReturn(false, $"UpdateLiveLootTableTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= LootTableTuningVar.eLTTV_NumLootTableTuningVars");

            if (lootTableProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, $"UpdateLiveLootTableTuningVar(): lootTableProtoRef == PrototypeId.Invalid");

            BlueprintId lootTableBlueprintRef = GetLootTableBlueprintDataRef();
            if (lootTableBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, $"UpdateLiveLootTableTuningVar(): lootTableBlueprintRef == BlueprintId.Invalid");

            int lootTableEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(lootTableProtoRef, lootTableBlueprintRef);
            if (lootTableEnumVal < 0 || lootTableEnumVal >= _perLootTableTuningVars.Count)
                return Logger.WarnReturn(false, $"UpdateLiveLootTableTuningVar(): lootTableEnumVal < 0 || lootTableEnumVal >= _perLootTableTuningVars.Count");

            _perLootTableTuningVars[lootTableEnumVal][(int)tuningVarEnum] = tuningVarValue;
            // Server-only live tuning?

            return true;
        }

        private bool UpdateLiveConditionTuningVar(PrototypeId conditionProtoRef, ConditionTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= ConditionTuningVar.eCTV_NumConditionTuningVars)
                return Logger.WarnReturn(false, $"UpdateLiveConditionTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= ConditionTuningVar.eCTV_NumConditionTuningVars");

            if (conditionProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, $"UpdateLiveConditionTuningVar(): conditionProtoRef == PrototypeId.Invalid");

            BlueprintId conditionBlueprintRef = GetConditionBlueprintDataRef();
            if (conditionBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, $"UpdateLiveConditionTuningVar(): conditionBlueprintRef == BlueprintId.Invalid");

            int conditionEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(conditionProtoRef, conditionBlueprintRef);
            if (conditionEnumVal < 0 || conditionEnumVal >= _perConditionTuningVars.Count)
                return Logger.WarnReturn(false, $"UpdateLiveConditionTuningVar(): conditionEnumVal < 0 || conditionEnumVal >= _perConditionTuningVars.Count");

            _perConditionTuningVars[conditionEnumVal][(int)tuningVarEnum] = tuningVarValue;
            _updateProtobufOutOfDate = true;

            return true;
        }

        private bool UpdateLivePublicEventTuningVar(PrototypeId publicEventProtoRef, PublicEventTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= PublicEventTuningVar.ePETV_NumPublicEventTuningVars)
                return Logger.WarnReturn(false, $"UpdateLivePublicEventTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= PublicEventTuningVar.ePETV_NumPublicEventTuningVars");

            if (publicEventProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, $"UpdateLivePublicEventTuningVar(): publicEventProtoRef == PrototypeId.Invalid");

            BlueprintId publicEventBlueprintRef = GetPublicEventBlueprintDataRef();
            if (publicEventBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, $"UpdateLivePublicEventTuningVar(): publicEventBlueprintRef == BlueprintId.Invalid");

            int publicEventEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(publicEventProtoRef, publicEventBlueprintRef);
            if (publicEventEnumVal < 0 || publicEventEnumVal >= _perPublicEventTuningVars.Count)
                return Logger.WarnReturn(false, $"UpdateLivePublicEventTuningVar(): publicEventEnumVal < 0 || publicEventEnumVal >= _perPublicEventTuningVars.Count");

            _perPublicEventTuningVars[publicEventEnumVal][(int)tuningVarEnum] = tuningVarValue;
            _updateProtobufOutOfDate = true;

            return true;
        }

        private bool UpdateLiveMetricsFrequencyTuningVar(PrototypeId metricsFrequencyProtoRef, MetricsFrequencyTuningVar tuningVarEnum, float tuningVarValue)
        {
            if (tuningVarEnum < 0 || tuningVarEnum >= MetricsFrequencyTuningVar.eMFTV_NumMetricsFrequencyTuningVars)
                return Logger.WarnReturn(false, $"UpdateLiveMetricsFrequencyTuningVar(): tuningVarEnum < 0 || tuningVarEnum >= MetricsFrequencyTuningVar.eMFTV_NumMetricsFrequencyTuningVars");

            if (metricsFrequencyProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, $"UpdateLiveMetricsFrequencyTuningVar(): metricsFrequencyProtoRef == PrototypeId.Invalid");

            BlueprintId metricsFrequencyBlueprintRef = GetMetricsFrequencyBlueprintDataRef();
            if (metricsFrequencyBlueprintRef == BlueprintId.Invalid) return Logger.WarnReturn(false, $"UpdateLiveMetricsFrequencyTuningVar(): metricsFrequencyBlueprintRef == BlueprintId.Invalid");

            int metricsFrequencyEnumVal = DataDirectory.Instance.GetPrototypeEnumValue(metricsFrequencyProtoRef, metricsFrequencyBlueprintRef);
            if (metricsFrequencyEnumVal < 0 || metricsFrequencyEnumVal >= _perMetricsFrequencyTuningVars.Count)
                return Logger.WarnReturn(false, $"UpdateLiveMetricsFrequencyTuningVar(): metricsFrequencyEnumVal < 0 || metricsFrequencyEnumVal >= _perMetricsFrequencyTuningVars.Count");

            _perMetricsFrequencyTuningVars[metricsFrequencyEnumVal][(int)tuningVarEnum] = tuningVarValue;
            // Server-only live tuning

            return true;
        }

        #endregion
    }
}
