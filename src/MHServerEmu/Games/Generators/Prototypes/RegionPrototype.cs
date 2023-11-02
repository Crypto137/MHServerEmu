using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class RegionPrototype : Prototype
    {
        public ulong[] AccessChecks;
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
        public ulong MissionTrackerFilterList;
        public ulong Music;
        public RegionMusicBehavior MusicBehavior;
        public float LifetimeInMinutes;
        public ulong LoadingScreensConsole;
        public ulong LoadingScreens;
        public ulong[] LootTables;
        public ObjectiveGraphSettingsPrototype ObjectiveGraph;
        public bool PartyFormationAllowed;
        public RegionQueueMethod RegionQueueMethod;
        public ulong[] RegionQueueStates;
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
        public ulong[] TransitionUITypes;
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
        public ulong[] DividedStartLocations;
        public ulong AvatarObjectiveInfoOverride;
        public ulong[] AvatarPowers;
        public ulong Tuning;
        public int PlayerLimit;
        public ulong[] Keywords;

        public RegionPrototype(Prototype proto) { FillPrototype(typeof(RegionPrototype), proto); }
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

    public class RegionDifficultySettingsPrototype : Prototype
    {
        public ulong TuningTable;
        public RegionDifficultySettingsPrototype(Prototype proto) { FillPrototype(typeof(RegionDifficultySettingsPrototype), proto); }
    }

    public class ObjectiveGraphSettingsPrototype : Prototype
    {
        public ObjectiveGraphModeAsset Mode;
        public ObjectiveGraphSettingsPrototype(Prototype proto) { FillPrototype(typeof(ObjectiveGraphSettingsPrototype), proto); }
    }

    public enum ObjectiveGraphModeAsset {
	    Off,
	    PathDistance,
	    PathNavi,
    }
}
