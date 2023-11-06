using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class RegionPrototype : Prototype
    {
        public RegionAccessCheckPrototype[] AccessChecks;
        public ulong[] AccessDifficulties;
        public ulong[] AltRegions;
        public bool LevelBandedRegionUsesPlayerLevel;
        public bool AllowAutoPartyOnEnter;
        public bool AllowLocalCoopMode;
        public bool AlwaysRevealFullMap;
        public bool AlwaysShutdownWhenVacant;
        public bool QueueDoNotWaitToFull;
        public ulong AmbientSfx;
        public float AutoPartyWindowSecs;
        public RegionEnums Behavior;
        public bool RespawnDestructibles;
        public ulong BodySliderTarget;
        public bool BodySliderOneWay;
        public int BonusItemFindMultiplier;
        public ulong Chapter;
        public ulong ClientMap;
        public bool CloseWhenReservationsReachesZero;
        public RegionDifficultySettingsPrototype DifficultySettings;
        public bool DisplayCommunityNews;
        public bool EnableAvatarSwap;
        public EvalPrototype EvalAccessRestriction;
        public ulong EvalAccessRestrictionMessage;
        public FootstepTrace FootstepTraceOverride;
        public bool ForceSimulation;
        public bool IsNPE;
        public ulong MarkerFilter;
        public ulong MetaGames;
        public MissionTrackerFilterType[] MissionTrackerFilterList;
        public ulong Music;
        public RegionMusicBehavior MusicBehavior;
        public float LifetimeInMinutes;
        public ulong LoadingScreensConsole;
        public ulong LoadingScreens;
        public LootTableAssignmentPrototype[] LootTables;
        public ObjectiveGraphSettingsPrototype ObjectiveGraph;
        public bool PartyFormationAllowed;
        public RegionQueueMethod RegionQueueMethod;
        public RegionQueueStateEntryPrototype[] RegionQueueStates;
        public bool PausesBoostConditions;
        public int Level;
        public bool LevelOverridesboolacterLevel;
        public bool LevelUseAreaOffset;
        public ulong[] PopulationOverrides;
        public ulong[] PowerKeywordBlacklist;
        public ulong PresenceStatusText;
        public ulong PropertyGameModeSetOnEntry;
        public RegionGeneratorPrototype RegionGenerator;
        public ulong RegionName;
        public ulong[] RestrictedRoster;
        public bool ShowTransitionIndicators;
        public ulong StartTarget;
        public bool SynergyEditAllowed;
        public TransitionUIPrototype[] TransitionUITypes;
        public float UIMapWallThickness;
        public ulong WaypointAutoUnlock;
        public ulong WaypointAutoUnlockList;
        public bool DailyCheckpointStartTarget;
        public int LowPopulationPlayerLimit;
        public ulong RespawnOverride;
        public ulong PlayerCameraSettings;
        public ulong PlayerCameraSettingsOrbis;
        public ulong UIDescription;
        public ulong UILocation;
        public ulong UITopPanel;
        public ulong UnrealClass;
        public bool UsePrevRegionPlayerDeathCount;
        public ulong AffixTable;
        public DividedStartLocationPrototype[] DividedStartLocations;
        public ulong AvatarObjectiveInfoOverride;
        public ulong[] AvatarPowers;
        public ulong Tuning;
        public int PlayerLimit;
        public ulong[] Keywords;

        public RegionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionPrototype), proto); }

        public ulong GetDefaultArea(Region region)
        {
            ulong defaultArea = 0;

            if (StartTarget != 0)
            {
                RegionConnectionTargetPrototype target = new(StartTarget.GetPrototype());
                defaultArea = target.Area;
            }
            
            if (defaultArea == 0)
            {
                return RegionGenerator.GetStartAreaRef(region); // TODO check return
            }

            return defaultArea;
        }
    }

    public class RegionConnectionTargetPrototype : Prototype
    {
        public ulong Region;
        public ulong Area;
        public ulong Cell;
        public ulong Entity;
        public bool EnabledByDefault;
        public ulong IntroKismetSeq;
        public ulong Name;
        public int UISortOrder;
        public RegionConnectionTargetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionConnectionTargetPrototype), proto); }
    }

    #region Enum
    public enum MissionTrackerFilterType
    {
	    None = -1,
	    Standard = 0,
	    PvE = 1,
	    PvP = 2,
	    Daily = 3,
	    Challenge = 4,
    }

    public enum RegionEnums
    {
	    Invalid = -1,
	    Town = 0,
	    PublicCombatZone = 1,
	    PrivateStory = 2,
	    PrivateNonStory = 3,
	    PrivateRaid = 5,
	    MatchPlay = 4,
    }

    public enum RegionMusicBehavior
    {
        None,
        Default,
        Mission,
    }

    public enum FootstepTrace
    {
        None,
        Enable,
        Disable,
    }
    public enum RegionQueueMethod {
	    None = 0,
	    PvPQueue = 1,
	    DailyQueue = 5,
    }
    #endregion

    public class RegionDifficultySettingsPrototype : Prototype
    {
        public ulong TuningTable;
        public RegionDifficultySettingsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionDifficultySettingsPrototype), proto); }
    }

    public class ObjectiveGraphSettingsPrototype : Prototype
    {
        public ObjectiveGraphModeAsset Mode;
        public ObjectiveGraphSettingsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ObjectiveGraphSettingsPrototype), proto); }
    }

    public enum ObjectiveGraphModeAsset {
	    Off,
	    PathDistance,
	    PathNavi,
    }

    public class RegionAccessCheckPrototype : Prototype
    {
        public bool NoAccessOnFail;
        public bool NoDisplayOnFail;

        public RegionAccessCheckPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionAccessCheckPrototype), proto); }
    }

    public class LevelAccessCheckPrototype : RegionAccessCheckPrototype
    {
        public ulong UIResponseMessage;
        public ulong UILevelRangeFormat;
        public ulong UIMapDescriptionTag;
        public ulong UIWaypointNameTag;
        public int LevelMin;
        public int LevelMax;

        public LevelAccessCheckPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LevelAccessCheckPrototype), proto); }
    }
    public class RegionQueueStateEntryPrototype : Prototype
    {
        public ulong StateParent;
        public ulong State;
        public ulong QueueText;

        public RegionQueueStateEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionQueueStateEntryPrototype), proto); }
    }

    public class DividedStartLocationPrototype : Prototype
    {
        public ulong Target;
        public int Players;
        public DividedStartLocationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DividedStartLocationPrototype), proto); }
    }
}
