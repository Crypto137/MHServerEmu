namespace MHServerEmu.Games.GameData.Prototypes
{

    public class PopulationPrototype : Prototype
    {
        public ulong RespawnMethod;
        public float ClusterDensityPct;
        public float ClusterDensityPeak;
        public float EncounterDensityBase;
        public float SpawnMapDensityMin;
        public float SpawnMapDensityMax;
        public float SpawnMapDensityStep;
        public int SpawnMapHeatReturnPerSecond;
        public EvalPrototype SpawnMapHeatReturnPerSecondEval;
        public float SpawnMapHeatBleed;
        public float SpawnMapCrowdSupression;
        public int SpawnMapCrowdSupressionStart;
        public EncounterDensityOverrideEntryPrototype[] EncounterDensityOverrides;
        public PopulationObjectListPrototype GlobalEncounters;
        public PopulationObjectListPrototype Themes;
        public int SpawnMapDistributeDistance;
        public int SpawnMapDistributeSpread;
        public bool SpawnMapEnabled;
        public PopulationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationPrototype), proto); }
    }

    public class SpawnMarkerPrototype : Prototype
    {
        public MarkerType Type;
        public ulong Shape;
        public ulong EditorIcon;
        public SpawnMarkerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SpawnMarkerPrototype), proto); }
    }
    public enum MarkerType
    {
        Enemies = 1,
        Encounter = 2,
        QuestGiver = 3,
        Transition = 4,
        Prop = 5,
    }
    public class PopulationMarkerPrototype : SpawnMarkerPrototype
    {
        public PopulationMarkerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationMarkerPrototype), proto); }
    }

    public class PropMarkerPrototype : SpawnMarkerPrototype
    {
        public PropMarkerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PropMarkerPrototype), proto); }
    }

    public class PopulatablePrototype : Prototype
    {
        public PopulatablePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulatablePrototype), proto); }
    }

    public class PopulationInfoPrototype : PopulatablePrototype
    {
        public ulong Ranks;
        public bool Unique;
        public PopulationInfoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationInfoPrototype), proto); }
    }

    public class RespawnMethodPrototype : Prototype
    {
        public float PlayerPresentDeferral;
        public int DeferralMax;
        public float RandomTimeOffset;
        public RespawnMethodPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RespawnMethodPrototype), proto); }
    }

    public class RespawnReducerByThresholdPrototype : RespawnMethodPrototype
    {
        public float BaseRespawnTime;
        public float RespawnReductionThreshold;
        public float ReducedRespawnTime;
        public float MinimumRespawnTime;
        public RespawnReducerByThresholdPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RespawnReducerByThresholdPrototype), proto); }
    }


    public class PopulationObjectPrototype : Prototype
    {
        public ulong AllianceOverride;
        public bool AllowCrossMissionHostility;
        public ulong EntityActionTimelineScript;
        public EntityFilterSettingsPrototype[] EntityFilterSettings;
        public ulong[] EntityFilterSettingTemplates;
        public EvalPrototype EvalSpawnProperties;
        public FormationTypePrototype Formation;
        public ulong FormationTemplate;
        public int GameModeScoreValue;
        public bool IgnoreBlackout;
        public bool IgnoreNaviCheck;
        public float LeashDistance;
        public ulong OnDefeatLootTable;
        public SpawnOrientationTweak OrientationTweak;
        public PopulationRiderPrototype[] Riders;
        public bool UseMarkerOrientation;
        public ulong UsePopulationMarker;
        public ulong CleanUpPolicy;

        public PopulationObjectPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationObjectPrototype), proto); }
    }


    public class PopulationEntityPrototype : PopulationObjectPrototype
    {
        public ulong Entity;
        public PopulationEntityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationEntityPrototype), proto); }
    }

    public class PopulationRiderPrototype : Prototype
    {
        public PopulationRiderPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationRiderPrototype), proto); }
    }

    public class PopulationRiderEntityPrototype : PopulationRiderPrototype
    {
        public ulong Entity;
        public PopulationRiderEntityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationRiderEntityPrototype), proto); }

    }

    public class PopulationRiderBlackOutPrototype : PopulationRiderPrototype
    {
        public ulong BlackOutZone;
        public PopulationRiderBlackOutPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationRiderBlackOutPrototype), proto); }

    }


    public enum SpawnOrientationTweak
    {
        Default,
        Offset15,
        Random,
    }

    public class PopulationRequiredObjectPrototype : Prototype
    {
        public PopulationObjectPrototype Object;
        public ulong ObjectTemplate;
        public short Count;
        public EvalPrototype EvalSpawnProperties;
        public ulong RankOverride;
        public bool Critical;
        public float Density;
        public ulong[] RestrictToCells;
        public ulong[] RestrictToAreas;
        public ulong RestrictToDifficultyMin;
        public ulong RestrictToDifficultyMax;

        public PopulationRequiredObjectPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationRequiredObjectPrototype), proto); }
    }

    public class PopulationRequiredObjectListPrototype : Prototype
    {
        public PopulationRequiredObjectPrototype[] RequiredObjects;
        public PopulationRequiredObjectListPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationRequiredObjectListPrototype), proto); }
    }

    public class BoxFormationTypePrototype : FormationTypePrototype
    {
        public BoxFormationTypePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(BoxFormationTypePrototype), proto); }
    }

    public class LineRowInfoPrototype : Prototype
    {
        public int Num;
        public FormationFacingEnum Facing;
        public LineRowInfoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LineRowInfoPrototype), proto); }
    }

    public class LineFormationTypePrototype : FormationTypePrototype
    {
        public LineRowInfoPrototype[] Rows;
        public LineFormationTypePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LineFormationTypePrototype), proto); }
    }

    public class ArcFormationTypePrototype : FormationTypePrototype
    {
        public int ArcDegrees;
        public ArcFormationTypePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ArcFormationTypePrototype), proto); }
    }

    public class FormationSlotPrototype : FormationTypePrototype
    {
        public float X;
        public float Y;
        public float Yaw;
        public FormationSlotPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(FormationSlotPrototype), proto); }
    }

    public class FixedFormationTypePrototype : FormationTypePrototype
    {
        public FormationSlotPrototype[] Slots;
        public FixedFormationTypePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(FixedFormationTypePrototype), proto); }
    }

    public class CleanUpPolicyPrototype : Prototype
    {
        public CleanUpPolicyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CleanUpPolicyPrototype), proto); }
    }


    public class EntityCountEntryPrototype : Prototype
    {
        public ulong Entity;
        public int Count;
        public EntityCountEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityCountEntryPrototype), proto); }
    }

    public class PopulationClusterFixedPrototype : PopulationObjectPrototype
    {
        public ulong[] Entities;
        public EntityCountEntryPrototype[] EntityEntries;
        public PopulationClusterFixedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationClusterFixedPrototype), proto); }
    }

    public class PopulationClusterPrototype : PopulationObjectPrototype
    {
        public short Max;
        public short Min;
        public float RandomOffset;
        public ulong Entity;
        public PopulationClusterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationClusterPrototype), proto); }
    }

    public class PopulationClusterMixedPrototype : PopulationObjectPrototype
    {
        public short Max;
        public short Min;
        public float RandomOffset;
        public PopulationObjectPrototype[] Choices;
        public PopulationClusterMixedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationClusterMixedPrototype), proto); }
    }

    public class PopulationLeaderPrototype : PopulationObjectPrototype
    {
        public ulong Leader;
        public PopulationObjectPrototype[] Henchmen;
        public PopulationLeaderPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationLeaderPrototype), proto); }
    }

    public class PopulationEncounterPrototype : PopulationObjectPrototype
    {
        public ulong EncounterResource;
        public PopulationEncounterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationEncounterPrototype), proto); }
    }

    public class PopulationFormationPrototype : PopulationObjectPrototype
    {
        public PopulationRequiredObjectPrototype[] Objects;
        public PopulationFormationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationFormationPrototype), proto); }
    }

    public class PopulationListTagObjectPrototype : Prototype
    {
        public PopulationListTagObjectPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationListTagObjectPrototype), proto); }
    }

    public class PopulationListTagEncounterPrototype : Prototype
    {
        public PopulationListTagEncounterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationListTagEncounterPrototype), proto); }
    }

    public class PopulationListTagThemePrototype : Prototype
    {
        public PopulationListTagThemePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationListTagThemePrototype), proto); }
    }

    public class PopulationObjectInstancePrototype : Prototype
    {
        public short Weight;
        public ulong Object;
        public PopulationObjectInstancePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationObjectInstancePrototype), proto); }
    }

    public class PopulationObjectListPrototype : Prototype
    {
        public PopulationObjectInstancePrototype[] List;
        public PopulationObjectListPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationObjectListPrototype), proto); }
    }

    public class PopulationGroupPrototype : PopulationObjectPrototype
    {
        public PopulationObjectPrototype[] EntitiesAndGroups;
        public PopulationGroupPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationGroupPrototype), proto); }
    }

    public class PopulationThemePrototype : Prototype
    {
        public PopulationObjectListPrototype Enemies;
        public int EnemyPicks;
        public PopulationObjectListPrototype Encounters;
        public PopulationThemePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationThemePrototype), proto); }
    }

    public class PopulationThemeSetPrototype : Prototype
    {
        public ulong[] Themes;
        public PopulationThemeSetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationThemeSetPrototype), proto); }
    }

    public class EncounterDensityOverrideEntryPrototype : Prototype
    {
        public ulong MarkerType;
        public float Density;
        public EncounterDensityOverrideEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EncounterDensityOverrideEntryPrototype), proto); }
    }
}

