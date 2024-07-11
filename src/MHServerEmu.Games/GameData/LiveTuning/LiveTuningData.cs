using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.LiveTuning
{
    public class LiveTuningData
    {
        public const float DefaultTuningVarValue = 1f;

        private static readonly Logger Logger = LogManager.CreateLogger();

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

        }

        public void Copy(LiveTuningData target)
        {

        }

        public void UpdateLiveTuningVar(PrototypeId tuningVarProtoRef, int tuningVarEnum, float tuningVarValue)
        {

        }

        public void UpdateLiveGlobalTuningVar(GlobalTuningVar tuningVarEnum, float tuningVarValue)
        {

        }

        public NetMessageLiveTuningUpdate GetLiveTuningUpdate()
        {
            return NetMessageLiveTuningUpdate.DefaultInstance;
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
            return true;
        }

        private bool InitPerLootTableTuningVars()
        {
            return true;
        }

        private bool InitPerMissionTuningVars()
        {
            return true;
        }

        private bool InitPerWorldEntityTuningVars()
        {
            return true;
        }

        private bool InitPerPopObjTuningVars()
        {
            return true;
        }

        private bool InitPerPowerTuningVars()
        {
            return true;
        }

        private bool InitPerRegionTuningVars()
        {
            return true;
        }

        private bool InitPerAvatarTuningVars()
        {
            return true;
        }

        private bool InitPerConditionTuningVars()
        {
            return true;
        }

        private bool InitPerPublicEventTuningVars()
        {
            return true;
        }

        private bool InitPerMetricsFrequencyTuningVars()
        {
            return true;
        }

        #endregion
    }
}
