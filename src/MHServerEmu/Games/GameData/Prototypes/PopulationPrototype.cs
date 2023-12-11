using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum MarkerType  // SpawnMarkers/PopulationType.type? Doesn't match exactly
    {
        Enemies = 1,    // Officer / Trash
        Encounter = 2,
        QuestGiver = 3,
        Transition = 4,
        Prop = 5,
    }

    [AssetEnum]
    public enum SpawnOrientationTweak
    {
        Default,
        Offset15,
        Random,
    }

    #endregion

    public class PopulationPrototype : Prototype
    {
        public ulong RespawnMethod { get; set; }
        public float ClusterDensityPct { get; set; }
        public float ClusterDensityPeak { get; set; }
        public float EncounterDensityBase { get; set; }
        public float SpawnMapDensityMin { get; set; }
        public float SpawnMapDensityMax { get; set; }
        public float SpawnMapDensityStep { get; set; }
        public int SpawnMapHeatReturnPerSecond { get; set; }
        public EvalPrototype SpawnMapHeatReturnPerSecondEval { get; set; }
        public float SpawnMapHeatBleed { get; set; }
        public float SpawnMapCrowdSupression { get; set; }
        public int SpawnMapCrowdSupressionStart { get; set; }
        public EncounterDensityOverrideEntryPrototype[] EncounterDensityOverrides { get; set; }
        public PopulationObjectListPrototype GlobalEncounters { get; set; }
        public PopulationObjectListPrototype Themes { get; set; }
        public int SpawnMapDistributeDistance { get; set; }
        public int SpawnMapDistributeSpread { get; set; }
        public bool SpawnMapEnabled { get; set; }
    }

    public class SpawnMarkerPrototype : Prototype
    {
        public MarkerType Type { get; set; }
        public ulong Shape { get; set; }
        public ulong EditorIcon { get; set; }
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
        public ulong Ranks { get; set; }
        public bool Unique { get; set; }
    }

    public class RespawnMethodPrototype : Prototype
    {
        public float PlayerPresentDeferral { get; set; }
        public int DeferralMax { get; set; }
        public float RandomTimeOffset { get; set; }
    }

    public class RespawnReducerByThresholdPrototype : RespawnMethodPrototype
    {
        public float BaseRespawnTime { get; set; }
        public float RespawnReductionThreshold { get; set; }
        public float ReducedRespawnTime { get; set; }
        public float MinimumRespawnTime { get; set; }
    }

    public class PopulationObjectPrototype : Prototype
    {
        public ulong AllianceOverride { get; set; }
        public bool AllowCrossMissionHostility { get; set; }
        public ulong EntityActionTimelineScript { get; set; }
        public EntityFilterSettingsPrototype[] EntityFilterSettings { get; set; }
        public ulong[] EntityFilterSettingTemplates { get; set; }
        public EvalPrototype EvalSpawnProperties { get; set; }
        public FormationTypePrototype Formation { get; set; }
        public ulong FormationTemplate { get; set; }
        public int GameModeScoreValue { get; set; }
        public bool IgnoreBlackout { get; set; }
        public bool IgnoreNaviCheck { get; set; }
        public float LeashDistance { get; set; }
        public ulong OnDefeatLootTable { get; set; }
        public SpawnOrientationTweak OrientationTweak { get; set; }
        public PopulationRiderPrototype[] Riders { get; set; }
        public bool UseMarkerOrientation { get; set; }
        public ulong UsePopulationMarker { get; set; }
        public ulong CleanUpPolicy { get; set; }
    }

    public class PopulationEntityPrototype : PopulationObjectPrototype
    {
        public ulong Entity { get; set; }
    }

    public class PopulationRiderPrototype : Prototype
    {
    }

    public class PopulationRiderEntityPrototype : PopulationRiderPrototype
    {
        public ulong Entity { get; set; }
    }

    public class PopulationRiderBlackOutPrototype : PopulationRiderPrototype
    {
        public ulong BlackOutZone { get; set; }
    }

    public class PopulationRequiredObjectPrototype : Prototype
    {
        public PopulationObjectPrototype Object { get; set; }
        public ulong ObjectTemplate { get; set; }
        public short Count { get; set; }
        public EvalPrototype EvalSpawnProperties { get; set; }
        public ulong RankOverride { get; set; }
        public bool Critical { get; set; }
        public float Density { get; set; }
        public ulong[] RestrictToCells { get; set; }
        public ulong[] RestrictToAreas { get; set; }
        public ulong RestrictToDifficultyMin { get; set; }
        public ulong RestrictToDifficultyMax { get; set; }
    }

    public class PopulationRequiredObjectListPrototype : Prototype
    {
        public PopulationRequiredObjectPrototype[] RequiredObjects { get; set; }
    }

    public class BoxFormationTypePrototype : FormationTypePrototype
    {
    }

    public class LineRowInfoPrototype : Prototype
    {
        public int Num { get; set; }
        public FormationFacing Facing { get; set; }
    }

    public class LineFormationTypePrototype : FormationTypePrototype
    {
        public LineRowInfoPrototype[] Rows { get; set; }
    }

    public class ArcFormationTypePrototype : FormationTypePrototype
    {
        public int ArcDegrees { get; set; }
    }

    public class FormationSlotPrototype : FormationTypePrototype
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Yaw { get; set; }
    }

    public class FixedFormationTypePrototype : FormationTypePrototype
    {
        public FormationSlotPrototype[] Slots { get; set; }
    }

    public class CleanUpPolicyPrototype : Prototype
    {
    }

    public class EntityCountEntryPrototype : Prototype
    {
        public ulong Entity { get; set; }
        public int Count { get; set; }
    }

    public class PopulationClusterFixedPrototype : PopulationObjectPrototype
    {
        public ulong[] Entities { get; set; }
        public EntityCountEntryPrototype[] EntityEntries { get; set; }
    }

    public class PopulationClusterPrototype : PopulationObjectPrototype
    {
        public short Max { get; set; }
        public short Min { get; set; }
        public float RandomOffset { get; set; }
        public ulong Entity { get; set; }
    }

    public class PopulationClusterMixedPrototype : PopulationObjectPrototype
    {
        public short Max { get; set; }
        public short Min { get; set; }
        public float RandomOffset { get; set; }
        public PopulationObjectPrototype[] Choices { get; set; }
    }

    public class PopulationLeaderPrototype : PopulationObjectPrototype
    {
        public ulong Leader { get; set; }
        public PopulationObjectPrototype[] Henchmen { get; set; }
    }

    public class PopulationEncounterPrototype : PopulationObjectPrototype
    {
        public ulong EncounterResource { get; set; }
    }

    public class PopulationFormationPrototype : PopulationObjectPrototype
    {
        public PopulationRequiredObjectPrototype[] Objects { get; set; }
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
        public short Weight { get; set; }
        public ulong Object { get; set; }
    }

    public class PopulationObjectListPrototype : Prototype
    {
        public PopulationObjectInstancePrototype[] List { get; set; }
    }

    public class PopulationGroupPrototype : PopulationObjectPrototype
    {
        public PopulationObjectPrototype[] EntitiesAndGroups { get; set; }
    }

    public class PopulationThemePrototype : Prototype
    {
        public PopulationObjectListPrototype Enemies { get; set; }
        public int EnemyPicks { get; set; }
        public PopulationObjectListPrototype Encounters { get; set; }
    }

    public class PopulationThemeSetPrototype : Prototype
    {
        public ulong[] Themes { get; set; }
    }

    public class EncounterDensityOverrideEntryPrototype : Prototype
    {
        public ulong MarkerType { get; set; }
        public float Density { get; set; }
    }
}
