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
        public ulong RespawnMethod { get; private set; }
        public float ClusterDensityPct { get; private set; }
        public float ClusterDensityPeak { get; private set; }
        public float EncounterDensityBase { get; private set; }
        public float SpawnMapDensityMin { get; private set; }
        public float SpawnMapDensityMax { get; private set; }
        public float SpawnMapDensityStep { get; private set; }
        public int SpawnMapHeatReturnPerSecond { get; private set; }
        public EvalPrototype SpawnMapHeatReturnPerSecondEval { get; private set; }
        public float SpawnMapHeatBleed { get; private set; }
        public float SpawnMapCrowdSupression { get; private set; }
        public int SpawnMapCrowdSupressionStart { get; private set; }
        public EncounterDensityOverrideEntryPrototype[] EncounterDensityOverrides { get; private set; }
        public PopulationObjectListPrototype GlobalEncounters { get; private set; }
        public PopulationObjectListPrototype Themes { get; private set; }
        public int SpawnMapDistributeDistance { get; private set; }
        public int SpawnMapDistributeSpread { get; private set; }
        public bool SpawnMapEnabled { get; private set; }
    }

    public class SpawnMarkerPrototype : Prototype
    {
        public MarkerType Type { get; private set; }
        public ulong Shape { get; private set; }
        public ulong EditorIcon { get; private set; }
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
        public ulong Ranks { get; private set; }
        public bool Unique { get; private set; }
    }

    public class RespawnMethodPrototype : Prototype
    {
        public float PlayerPresentDeferral { get; private set; }
        public int DeferralMax { get; private set; }
        public float RandomTimeOffset { get; private set; }
    }

    public class RespawnReducerByThresholdPrototype : RespawnMethodPrototype
    {
        public float BaseRespawnTime { get; private set; }
        public float RespawnReductionThreshold { get; private set; }
        public float ReducedRespawnTime { get; private set; }
        public float MinimumRespawnTime { get; private set; }
    }

    public class PopulationObjectPrototype : Prototype
    {
        public ulong AllianceOverride { get; private set; }
        public bool AllowCrossMissionHostility { get; private set; }
        public ulong EntityActionTimelineScript { get; private set; }
        public EntityFilterSettingsPrototype[] EntityFilterSettings { get; private set; }
        public ulong[] EntityFilterSettingTemplates { get; private set; }
        public EvalPrototype EvalSpawnProperties { get; private set; }
        public FormationTypePrototype Formation { get; private set; }
        public ulong FormationTemplate { get; private set; }
        public int GameModeScoreValue { get; private set; }
        public bool IgnoreBlackout { get; private set; }
        public bool IgnoreNaviCheck { get; private set; }
        public float LeashDistance { get; private set; }
        public ulong OnDefeatLootTable { get; private set; }
        public SpawnOrientationTweak OrientationTweak { get; private set; }
        public PopulationRiderPrototype[] Riders { get; private set; }
        public bool UseMarkerOrientation { get; private set; }
        public ulong UsePopulationMarker { get; private set; }
        public ulong CleanUpPolicy { get; private set; }
    }

    public class PopulationEntityPrototype : PopulationObjectPrototype
    {
        public ulong Entity { get; private set; }
    }

    public class PopulationRiderPrototype : Prototype
    {
    }

    public class PopulationRiderEntityPrototype : PopulationRiderPrototype
    {
        public ulong Entity { get; private set; }
    }

    public class PopulationRiderBlackOutPrototype : PopulationRiderPrototype
    {
        public ulong BlackOutZone { get; private set; }
    }

    public class PopulationRequiredObjectPrototype : Prototype
    {
        public PopulationObjectPrototype Object { get; private set; }
        public ulong ObjectTemplate { get; private set; }
        public short Count { get; private set; }
        public EvalPrototype EvalSpawnProperties { get; private set; }
        public ulong RankOverride { get; private set; }
        public bool Critical { get; private set; }
        public float Density { get; private set; }
        public ulong[] RestrictToCells { get; private set; }
        public ulong[] RestrictToAreas { get; private set; }
        public ulong RestrictToDifficultyMin { get; private set; }
        public ulong RestrictToDifficultyMax { get; private set; }
    }

    public class PopulationRequiredObjectListPrototype : Prototype
    {
        public PopulationRequiredObjectPrototype[] RequiredObjects { get; private set; }
    }

    public class BoxFormationTypePrototype : FormationTypePrototype
    {
    }

    public class LineRowInfoPrototype : Prototype
    {
        public int Num { get; private set; }
        public FormationFacing Facing { get; private set; }
    }

    public class LineFormationTypePrototype : FormationTypePrototype
    {
        public LineRowInfoPrototype[] Rows { get; private set; }
    }

    public class ArcFormationTypePrototype : FormationTypePrototype
    {
        public int ArcDegrees { get; private set; }
    }

    public class FormationSlotPrototype : FormationTypePrototype
    {
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Yaw { get; private set; }
    }

    public class FixedFormationTypePrototype : FormationTypePrototype
    {
        public FormationSlotPrototype[] Slots { get; private set; }
    }

    public class CleanUpPolicyPrototype : Prototype
    {
    }

    public class EntityCountEntryPrototype : Prototype
    {
        public ulong Entity { get; private set; }
        public int Count { get; private set; }
    }

    public class PopulationClusterFixedPrototype : PopulationObjectPrototype
    {
        public ulong[] Entities { get; private set; }
        public EntityCountEntryPrototype[] EntityEntries { get; private set; }
    }

    public class PopulationClusterPrototype : PopulationObjectPrototype
    {
        public short Max { get; private set; }
        public short Min { get; private set; }
        public float RandomOffset { get; private set; }
        public ulong Entity { get; private set; }
    }

    public class PopulationClusterMixedPrototype : PopulationObjectPrototype
    {
        public short Max { get; private set; }
        public short Min { get; private set; }
        public float RandomOffset { get; private set; }
        public PopulationObjectPrototype[] Choices { get; private set; }
    }

    public class PopulationLeaderPrototype : PopulationObjectPrototype
    {
        public ulong Leader { get; private set; }
        public PopulationObjectPrototype[] Henchmen { get; private set; }
    }

    public class PopulationEncounterPrototype : PopulationObjectPrototype
    {
        public ulong EncounterResource { get; private set; }
    }

    public class PopulationFormationPrototype : PopulationObjectPrototype
    {
        public PopulationRequiredObjectPrototype[] Objects { get; private set; }
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
        public short Weight { get; private set; }
        public ulong Object { get; private set; }
    }

    public class PopulationObjectListPrototype : Prototype
    {
        public PopulationObjectInstancePrototype[] List { get; private set; }
    }

    public class PopulationGroupPrototype : PopulationObjectPrototype
    {
        public PopulationObjectPrototype[] EntitiesAndGroups { get; private set; }
    }

    public class PopulationThemePrototype : Prototype
    {
        public PopulationObjectListPrototype Enemies { get; private set; }
        public int EnemyPicks { get; private set; }
        public PopulationObjectListPrototype Encounters { get; private set; }
    }

    public class PopulationThemeSetPrototype : Prototype
    {
        public ulong[] Themes { get; private set; }
    }

    public class EncounterDensityOverrideEntryPrototype : Prototype
    {
        public ulong MarkerType { get; private set; }
        public float Density { get; private set; }
    }
}
