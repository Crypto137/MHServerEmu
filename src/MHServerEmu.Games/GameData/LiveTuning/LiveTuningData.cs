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

        private NetMessageLiveTuningUpdate _updateProtobuf = NetMessageLiveTuningUpdate.DefaultInstance;
        private bool _updateProtobufOutOfDate = false;

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

            // TODO: Copy loot groups

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
            // TODO
            return NetMessageLiveTuningUpdate.DefaultInstance;
        }

        private void UpdateLiveLootGroup(WorldEntityPrototype worldEntityProto, float value)
        {
            // TODO
        }

        private void ClearLootGroups()
        {
            // TODO
        }

        #region Tuning Var Accesors

        public float GetLiveGlobalTuningVar(GlobalTuningVar tuningVarEnum)
        {
            return DefaultTuningVarValue;
        }

        public float GetLiveAreaTuningVar(AreaPrototype areaProto, AreaTuningVar tuningVarEnum)
        {
            return DefaultTuningVarValue;
        }

        public float GetLiveLootTableTuningVar(LootTablePrototype lootTableProto, LootTableTuningVar tuningVarEnum)
        {
            return DefaultTuningVarValue;
        }

        public float GetLiveMissionTuningVar(MissionPrototype missionProto, MissionTuningVar tuningVarEnum)
        {
            return DefaultTuningVarValue;
        }

        public float GetLiveWorldEntityTuningVar(WorldEntityPrototype worldEntityProto, WorldEntityTuningVar tuningVarEnum)
        {
            return DefaultTuningVarValue;
        }

        public float GetLivePopObjTuningVar(PopulationObjectPrototype popObjProto, PopObjTuningVar tuningVarEnum)
        {
            return DefaultTuningVarValue;
        }

        public float GetLivePowerTuningVar(PowerPrototype powerProto, PowerTuningVar tuningVarEnum)
        {
            return DefaultTuningVarValue;
        }

        public float GetLiveRegionTuningVar(RegionPrototype regionProto, RegionTuningVar tuningVarEnum)
        {
            return DefaultTuningVarValue;
        }

        public float GetLiveAvatarTuningVar(AvatarPrototype avatarProto, AvatarEntityTuningVar tuningVarEnum)
        {
            return DefaultTuningVarValue;
        }

        public float GetLiveConditionTuningVar(ConditionPrototype conditionProto, ConditionTuningVar tuningVarEnum)
        {
            return DefaultTuningVarValue;
        }

        public float GetLivePublicEventTuningVar(PublicEventPrototype publicEventProto, PublicEventTuningVar tuningVarEnum)
        {
            return DefaultTuningVarValue;
        }

        public float GetLiveMetricsFrequencyTuningVar(MetricsFrequencyPrototype metricsFrequencyProto, MetricsFrequencyTuningVar tuningVarEnum)
        {
            return DefaultTuningVarValue;
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

        private bool UpdateLivePowerTuningVar(PrototypeId tuningVarProtoRef, PowerTuningVar tuningVarEnum, float tuningVarValue)
        {
            return true;
        }

        private bool UpdateLiveAreaTuningVar(PrototypeId tuningVarProtoRef, AreaTuningVar tuningVarEnum, float tuningVarValue)
        {
            return true;
        }

        private bool UpdateLiveRegionTuningVar(PrototypeId tuningVarProtoRef, RegionTuningVar tuningVarEnum, float tuningVarValue)
        {
            return true;
        }

        private bool UpdateLivePopObjTuningVar(PrototypeId tuningVarProtoRef, PopObjTuningVar tuningVarEnum, float tuningVarValue)
        {
            return true;
        }

        private bool UpdateLiveMissionTuningVar(PrototypeId tuningVarProtoRef, MissionTuningVar tuningVarEnum, float tuningVarValue)
        {
            return true;
        }

        private bool UpdateLiveLootTableTuningVar(PrototypeId tuningVarProtoRef, LootTableTuningVar tuningVarEnum, float tuningVarValue)
        {
            return true;
        }

        private bool UpdateLiveConditionTuningVar(PrototypeId tuningVarProtoRef, ConditionTuningVar tuningVarEnum, float tuningVarValue)
        {
            return true;
        }

        private bool UpdateLivePublicEventTuningVar(PrototypeId tuningVarProtoRef, PublicEventTuningVar tuningVarEnum, float tuningVarValue)
        {
            return true;
        }

        private bool UpdateLiveMetricsFrequencyTuningVar(PrototypeId tuningVarProtoRef, MetricsFrequencyTuningVar tuningVarEnum, float tuningVarValue)
        {
            return true;
        }

        #endregion
    }
}
