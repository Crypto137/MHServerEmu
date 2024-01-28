using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Invalid)]
    public enum MarkerType  // SpawnMarkers/PopulationType.type? Doesn't match exactly
    {
        Invalid = 0,
        Enemies = 1,    // Officer / Trash
        Encounter = 2,
        QuestGiver = 3,
        Transition = 4,
        Prop = 5,
    }

    [AssetEnum((int)Default)]
    public enum SpawnOrientationTweak
    {
        Default,
        Offset15,
        Random,
    }

    #endregion

    public class PopulationPrototype : Prototype
    {
        public PrototypeId RespawnMethod { get; protected set; }
        public float ClusterDensityPct { get; protected set; }
        public float ClusterDensityPeak { get; protected set; }
        public float EncounterDensityBase { get; protected set; }
        public float SpawnMapDensityMin { get; protected set; }
        public float SpawnMapDensityMax { get; protected set; }
        public float SpawnMapDensityStep { get; protected set; }
        public int SpawnMapHeatReturnPerSecond { get; protected set; }
        public EvalPrototype SpawnMapHeatReturnPerSecondEval { get; protected set; }
        public float SpawnMapHeatBleed { get; protected set; }
        public float SpawnMapCrowdSupression { get; protected set; }
        public int SpawnMapCrowdSupressionStart { get; protected set; }
        public EncounterDensityOverrideEntryPrototype[] EncounterDensityOverrides { get; protected set; }
        public PopulationObjectListPrototype GlobalEncounters { get; protected set; }
        public PopulationObjectListPrototype Themes { get; protected set; }
        public int SpawnMapDistributeDistance { get; protected set; }
        public int SpawnMapDistributeSpread { get; protected set; }
        public bool SpawnMapEnabled { get; protected set; }
    }

    public class SpawnMarkerPrototype : Prototype
    {
        public MarkerType Type { get; protected set; }
        public PrototypeId Shape { get; protected set; }
        public AssetId EditorIcon { get; protected set; }
    }

    public class PopulationMarkerPrototype : SpawnMarkerPrototype
    {
    }

    public class PropMarkerPrototype : SpawnMarkerPrototype
    {
    }

    public class PopulatablePrototype : Prototype
    {
    }

    public class PopulationInfoPrototype : PopulatablePrototype
    {
        public PrototypeId[] Ranks { get; protected set; }
        public bool Unique { get; protected set; }
    }

    public class RespawnMethodPrototype : Prototype
    {
        public float PlayerPresentDeferral { get; protected set; }
        public int DeferralMax { get; protected set; }
        public float RandomTimeOffset { get; protected set; }
    }

    public class RespawnReducerByThresholdPrototype : RespawnMethodPrototype
    {
        public float BaseRespawnTime { get; protected set; }
        public float RespawnReductionThreshold { get; protected set; }
        public float ReducedRespawnTime { get; protected set; }
        public float MinimumRespawnTime { get; protected set; }
    }

    public class PopulationObjectPrototype : Prototype
    {
        public PrototypeId AllianceOverride { get; protected set; }
        public bool AllowCrossMissionHostility { get; protected set; }
        public PrototypeId EntityActionTimelineScript { get; protected set; }
        public EntityFilterSettingsPrototype[] EntityFilterSettings { get; protected set; }
        public PrototypeId[] EntityFilterSettingTemplates { get; protected set; }
        public EvalPrototype EvalSpawnProperties { get; protected set; }
        public FormationTypePrototype Formation { get; protected set; }
        public PrototypeId FormationTemplate { get; protected set; }
        public int GameModeScoreValue { get; protected set; }
        public bool IgnoreBlackout { get; protected set; }
        public bool IgnoreNaviCheck { get; protected set; }
        public float LeashDistance { get; protected set; }
        public PrototypeId OnDefeatLootTable { get; protected set; }
        public SpawnOrientationTweak OrientationTweak { get; protected set; }
        public PopulationRiderPrototype[] Riders { get; protected set; }
        public bool UseMarkerOrientation { get; protected set; }
        public PrototypeId UsePopulationMarker { get; protected set; }
        public PrototypeId CleanUpPolicy { get; protected set; }
    }

    public class PopulationEntityPrototype : PopulationObjectPrototype
    {
        public PrototypeId Entity { get; protected set; }
    }

    public class PopulationRiderPrototype : Prototype
    {
    }

    public class PopulationRiderEntityPrototype : PopulationRiderPrototype
    {
        public PrototypeId Entity { get; protected set; }
    }

    public class PopulationRiderBlackOutPrototype : PopulationRiderPrototype
    {
        public PrototypeId BlackOutZone { get; protected set; }
    }

    public class PopulationRequiredObjectPrototype : Prototype
    {
        public PopulationObjectPrototype Object { get; protected set; }
        public PrototypeId ObjectTemplate { get; protected set; }
        public short Count { get; protected set; }
        public EvalPrototype EvalSpawnProperties { get; protected set; }
        public PrototypeId RankOverride { get; protected set; }
        public bool Critical { get; protected set; }
        public float Density { get; protected set; }
        public AssetId[] RestrictToCells { get; protected set; }
        public PrototypeId[] RestrictToAreas { get; protected set; }
        public PrototypeId RestrictToDifficultyMin { get; protected set; }
        public PrototypeId RestrictToDifficultyMax { get; protected set; }
    }

    public class PopulationRequiredObjectListPrototype : Prototype
    {
        public PopulationRequiredObjectPrototype[] RequiredObjects { get; protected set; }
    }

    public class BoxFormationTypePrototype : FormationTypePrototype
    {
    }

    public class LineRowInfoPrototype : Prototype
    {
        public int Num { get; protected set; }
        public FormationFacing Facing { get; protected set; }
    }

    public class LineFormationTypePrototype : FormationTypePrototype
    {
        public LineRowInfoPrototype[] Rows { get; protected set; }
    }

    public class ArcFormationTypePrototype : FormationTypePrototype
    {
        public int ArcDegrees { get; protected set; }
    }

    public class FormationSlotPrototype : FormationTypePrototype
    {
        public float X { get; protected set; }
        public float Y { get; protected set; }
        public float Yaw { get; protected set; }
    }

    public class FixedFormationTypePrototype : FormationTypePrototype
    {
        public FormationSlotPrototype[] Slots { get; protected set; }
    }

    public class CleanUpPolicyPrototype : Prototype
    {
    }

    public class EntityCountEntryPrototype : Prototype
    {
        public PrototypeId Entity { get; protected set; }
        public int Count { get; protected set; }
    }

    public class PopulationClusterFixedPrototype : PopulationObjectPrototype
    {
        public PrototypeId[] Entities { get; protected set; }
        public EntityCountEntryPrototype[] EntityEntries { get; protected set; }
    }

    public class PopulationClusterPrototype : PopulationObjectPrototype
    {
        public short Max { get; protected set; }
        public short Min { get; protected set; }
        public float RandomOffset { get; protected set; }
        public PrototypeId Entity { get; protected set; }
    }

    public class PopulationClusterMixedPrototype : PopulationObjectPrototype
    {
        public short Max { get; protected set; }
        public short Min { get; protected set; }
        public float RandomOffset { get; protected set; }
        public PopulationObjectPrototype[] Choices { get; protected set; }
    }

    public class PopulationLeaderPrototype : PopulationObjectPrototype
    {
        public PrototypeId Leader { get; protected set; }
        public PopulationObjectPrototype[] Henchmen { get; protected set; }
    }

    public class PopulationEncounterPrototype : PopulationObjectPrototype
    {
        public AssetId EncounterResource { get; protected set; }
    }

    public class PopulationFormationPrototype : PopulationObjectPrototype
    {
        public PopulationRequiredObjectPrototype[] Objects { get; protected set; }
    }

    public class PopulationListTagObjectPrototype : Prototype
    {
    }

    public class PopulationListTagEncounterPrototype : Prototype
    {
    }

    public class PopulationListTagThemePrototype : Prototype
    {
    }

    public class PopulationObjectInstancePrototype : Prototype
    {
        public short Weight { get; protected set; }
        public PrototypeId Object { get; protected set; }
    }

    public class PopulationObjectListPrototype : Prototype
    {
        public PopulationObjectInstancePrototype[] List { get; protected set; }
    }

    public class PopulationGroupPrototype : PopulationObjectPrototype
    {
        public PopulationObjectPrototype[] EntitiesAndGroups { get; protected set; }
    }

    public class PopulationThemePrototype : Prototype
    {
        public PopulationObjectListPrototype Enemies { get; protected set; }
        public int EnemyPicks { get; protected set; }
        public PopulationObjectListPrototype Encounters { get; protected set; }
    }

    public class PopulationThemeSetPrototype : Prototype
    {
        public PrototypeId[] Themes { get; protected set; }
    }

    public class EncounterDensityOverrideEntryPrototype : Prototype
    {
        public PrototypeId MarkerType { get; protected set; }
        public float Density { get; protected set; }
    }
}
