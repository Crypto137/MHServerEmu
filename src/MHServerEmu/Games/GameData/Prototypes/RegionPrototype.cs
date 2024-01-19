using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum MissionTrackerFilterType
    {
        None = -1,
        Standard = 0,
        PvE = 1,
        PvP = 2,
        Daily = 3,
        Challenge = 4,
    }

    [AssetEnum]
    public enum RegionBehaviorAsset     // Regions/RegionBehavior.type
    {
        Invalid = -1,
        Town = 0,
        PublicCombatZone = 1,
        PrivateStory = 2,
        PrivateNonStory = 3,
        PrivateRaid = 5,
        MatchPlay = 4,
    }

    [AssetEnum]
    public enum RegionMusicBehaviorAsset
    {
        None,
        Default,
        Mission,
    }

    [AssetEnum]
    public enum FootstepTraceBehaviorAsset
    {
        None,
        Enable,
        Disable,
    }

    [AssetEnum]
    public enum RegionQueueMethod
    {
        None = 0,
        PvPQueue = 1,
        DailyQueue = 5,
    }

    [AssetEnum]
    public enum ObjectiveGraphModeAsset         // Regions/EnumObjectiveGraphMode.type
    {
        Off,
        PathDistance,
        PathNavi,
    }

    [AssetEnum]
    public enum RegionTransitionDirectionality  // Regions/RegionConnectionType.type
    {
        BiDirectional = 0,
        OneWay = 1,
        Disabled = 2,
    }

    #endregion

    public class RegionPrototype : Prototype
    {
        public ulong ClientMap { get; protected set; }
        public ulong BodySliderTarget { get; protected set; }
        public ulong StartTarget { get; protected set; }
        public ulong[] Music { get; protected set; }
        public RegionGeneratorPrototype RegionGenerator { get; protected set; }
        public RegionBehaviorAsset Behavior { get; protected set; }
        public ulong RegionName { get; protected set; }
        public ulong[] MetaGames { get; protected set; }
        public bool ForceSimulation { get; protected set; }
        public ulong[] LoadingScreens { get; protected set; }
        public bool AlwaysRevealFullMap { get; protected set; }
        public ulong Chapter { get; protected set; }
        public int PlayerLimit { get; protected set; }
        public float LifetimeInMinutes { get; protected set; }
        public ulong WaypointAutoUnlock { get; protected set; }
        public bool PartyFormationAllowed { get; protected set; }
        public TransitionUIPrototype[] TransitionUITypes { get; protected set; }
        public ulong AmbientSfx { get; protected set; }
        public ulong[] PowerKeywordBlacklist { get; protected set; }
        public bool CloseWhenReservationsReachesZero { get; protected set; }
        public float UIMapWallThickness { get; protected set; }
        public ulong[] PopulationOverrides { get; protected set; }
        public int Level { get; protected set; }
        public MissionTrackerFilterType[] MissionTrackerFilterList { get; protected set; }
        public bool AllowAutoPartyOnEnter { get; protected set; }
        public float AutoPartyWindowSecs { get; protected set; }
        public bool DailyCheckpointStartTarget { get; protected set; }
        public int LowPopulationPlayerLimit { get; protected set; }
        public ulong RespawnOverride { get; protected set; }
        public ulong PlayerCameraSettings { get; protected set; }
        public RegionQueueMethod RegionQueueMethod { get; protected set; }
        public EvalPrototype EvalAccessRestriction { get; protected set; }
        public ulong[] WaypointAutoUnlockList { get; protected set; }
        public bool AlwaysShutdownWhenVacant { get; protected set; }
        public bool SynergyEditAllowed { get; protected set; }
        public ulong[] Keywords { get; protected set; }
        public ulong UITopPanel { get; protected set; }
        public ulong[] AltRegions { get; protected set; }
        public RegionAccessCheckPrototype[] AccessChecks { get; protected set; }
        public ulong UIDescription { get; protected set; }
        public ulong UILocation { get; protected set; }
        public bool PausesBoostConditions { get; protected set; }
        public bool ShowTransitionIndicators { get; protected set; }
        public RegionQueueStateEntryPrototype[] RegionQueueStates { get; protected set; }
        public ulong MarkerFilter { get; protected set; }
        public bool LevelBandedRegionUsesPlayerLevel { get; protected set; }
        public FootstepTraceBehaviorAsset FootstepTraceOverride { get; protected set; }
        public bool QueueDoNotWaitToFull { get; protected set; }
        public bool DisplayCommunityNews { get; protected set; }
        public ulong UnrealClass { get; protected set; }
        public bool RespawnDestructibles { get; protected set; }
        public ulong PropertyGameModeSetOnEntry { get; protected set; }
        public bool UsePrevRegionPlayerDeathCount { get; protected set; }
        public LootTableAssignmentPrototype[] LootTables { get; protected set; }
        public ulong AffixTable { get; protected set; }
        public ObjectiveGraphSettingsPrototype ObjectiveGraph { get; protected set; }
        public DividedStartLocationPrototype[] DividedStartLocations { get; protected set; }
        public RegionMusicBehaviorAsset MusicBehavior { get; protected set; }
        public ulong AvatarObjectiveInfoOverride { get; protected set; }
        public RegionDifficultySettingsPrototype DifficultySettings { get; protected set; }
        public bool LevelOverridesCharacterLevel { get; protected set; }
        public bool LevelUseAreaOffset { get; protected set; }
        public ulong EvalAccessRestrictionMessage { get; protected set; }
        public bool BodySliderOneWay { get; protected set; }
        public bool EnableAvatarSwap { get; protected set; }
        public ulong[] RestrictedRoster { get; protected set; }
        public ulong[] AvatarPowers { get; protected set; }
        public bool IsNPE { get; protected set; }
        public ulong PresenceStatusText { get; protected set; }
        public ulong[] AccessDifficulties { get; protected set; }
        public ulong Tuning { get; protected set; }
        public int BonusItemFindMultiplier { get; protected set; }
        public ulong PlayerCameraSettingsOrbis { get; protected set; }
        public ulong[] LoadingScreensConsole { get; protected set; }
        public bool AllowLocalCoopMode { get; protected set; }
    }

    public class RegionConnectionTargetPrototype : Prototype
    {
        public ulong Region { get; protected set; }
        public ulong Area { get; protected set; }
        public ulong Cell { get; protected set; }
        public ulong Entity { get; protected set; }
        public ulong IntroKismetSeq { get; protected set; }
        public ulong Name { get; protected set; }
        public bool EnabledByDefault { get; protected set; }
        public int UISortOrder { get; protected set; }
    }

    public class ObjectiveGraphSettingsPrototype : Prototype
    {
        public ObjectiveGraphModeAsset Mode { get; protected set; }
    }

    public class FactionLimitPrototype : Prototype
    {
        public ulong Faction { get; protected set; }
        public int PlayerLimit { get; protected set; }
    }

    public class RegionAccessCheckPrototype : Prototype
    {
        public bool NoAccessOnFail { get; protected set; }
        public bool NoDisplayOnFail { get; protected set; }
    }

    public class LevelAccessCheckPrototype : RegionAccessCheckPrototype
    {
        public ulong UIResponseMessage { get; protected set; }
        public ulong UILevelRangeFormat { get; protected set; }
        public ulong UIMapDescriptionTag { get; protected set; }
        public ulong UIWaypointNameTag { get; protected set; }
        public int LevelMin { get; protected set; }
        public int LevelMax { get; protected set; }
    }

    public class RegionQueueStateEntryPrototype : Prototype
    {
        public ulong StateParent { get; protected set; }
        public ulong State { get; protected set; }
        public ulong QueueText { get; protected set; }
    }

    public class DividedStartLocationPrototype : Prototype
    {
        public ulong Target { get; protected set; }
        public int Players { get; protected set; }
    }

    public class RegionPortalControlEntryPrototype : Prototype
    {
        public ulong Region { get; protected set; }
        public int UnlockDurationMinutes { get; protected set; }
        public int UnlockPeriodMinutes { get; protected set; }
    }

    public class RegionConnectionNodePrototype : Prototype
    {
        public ulong Origin { get; protected set; }
        public ulong Target { get; protected set; }
        public RegionTransitionDirectionality Type { get; protected set; }
    }

    public class ZoneLevelPrototype : Prototype
    {
    }

    public class ZoneLevelFixedPrototype : ZoneLevelPrototype
    {
        public short level { get; protected set; }
    }

    public class ZoneLevelRelativePrototype : ZoneLevelPrototype
    {
        public short modmax { get; protected set; }
        public short modmin { get; protected set; }
    }

    public class BlackOutZonePrototype : Prototype
    {
        public float BlackOutRadius { get; protected set; }
    }
}
