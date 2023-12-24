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
        public ulong ClientMap { get; private set; }
        public ulong BodySliderTarget { get; private set; }
        public ulong StartTarget { get; private set; }
        public ulong[] Music { get; private set; }
        public RegionGeneratorPrototype RegionGenerator { get; private set; }
        public RegionBehaviorAsset Behavior { get; private set; }
        public ulong RegionName { get; private set; }
        public ulong MetaGames { get; private set; }
        public bool ForceSimulation { get; private set; }
        public ulong LoadingScreens { get; private set; }
        public bool AlwaysRevealFullMap { get; private set; }
        public ulong Chapter { get; private set; }
        public int PlayerLimit { get; private set; }
        public float LifetimeInMinutes { get; private set; }
        public ulong WaypointAutoUnlock { get; private set; }
        public bool PartyFormationAllowed { get; private set; }
        public TransitionUIPrototype[] TransitionUITypes { get; private set; }
        public ulong AmbientSfx { get; private set; }
        public ulong[] PowerKeywordBlacklist { get; private set; }
        public bool CloseWhenReservationsReachesZero { get; private set; }
        public float UIMapWallThickness { get; private set; }
        public ulong[] PopulationOverrides { get; private set; }
        public int Level { get; private set; }
        public MissionTrackerFilterType[] MissionTrackerFilterList { get; private set; }
        public bool AllowAutoPartyOnEnter { get; private set; }
        public float AutoPartyWindowSecs { get; private set; }
        public bool DailyCheckpointStartTarget { get; private set; }
        public int LowPopulationPlayerLimit { get; private set; }
        public ulong RespawnOverride { get; private set; }
        public ulong PlayerCameraSettings { get; private set; }
        public RegionQueueMethod RegionQueueMethod { get; private set; }
        public EvalPrototype EvalAccessRestriction { get; private set; }
        public ulong WaypointAutoUnlockList { get; private set; }
        public bool AlwaysShutdownWhenVacant { get; private set; }
        public bool SynergyEditAllowed { get; private set; }
        public ulong[] Keywords { get; private set; }
        public ulong UITopPanel { get; private set; }
        public ulong[] AltRegions { get; private set; }
        public RegionAccessCheckPrototype[] AccessChecks { get; private set; }
        public ulong UIDescription { get; private set; }
        public ulong UILocation { get; private set; }
        public bool PausesBoostConditions { get; private set; }
        public bool ShowTransitionIndicators { get; private set; }
        public RegionQueueStateEntryPrototype[] RegionQueueStates { get; private set; }
        public ulong MarkerFilter { get; private set; }
        public bool LevelBandedRegionUsesPlayerLevel { get; private set; }
        public FootstepTraceBehaviorAsset FootstepTraceOverride { get; private set; }
        public bool QueueDoNotWaitToFull { get; private set; }
        public bool DisplayCommunityNews { get; private set; }
        public ulong UnrealClass { get; private set; }
        public bool RespawnDestructibles { get; private set; }
        public ulong PropertyGameModeSetOnEntry { get; private set; }
        public bool UsePrevRegionPlayerDeathCount { get; private set; }
        public LootTableAssignmentPrototype[] LootTables { get; private set; }
        public ulong AffixTable { get; private set; }
        public ObjectiveGraphSettingsPrototype ObjectiveGraph { get; private set; }
        public DividedStartLocationPrototype[] DividedStartLocations { get; private set; }
        public RegionMusicBehaviorAsset MusicBehavior { get; private set; }
        public ulong AvatarObjectiveInfoOverride { get; private set; }
        public RegionDifficultySettingsPrototype DifficultySettings { get; private set; }
        public bool LevelOverridesCharacterLevel { get; private set; }
        public bool LevelUseAreaOffset { get; private set; }
        public ulong EvalAccessRestrictionMessage { get; private set; }
        public bool BodySliderOneWay { get; private set; }
        public bool EnableAvatarSwap { get; private set; }
        public ulong[] RestrictedRoster { get; private set; }
        public ulong[] AvatarPowers { get; private set; }
        public bool IsNPE { get; private set; }
        public ulong PresenceStatusText { get; private set; }
        public ulong[] AccessDifficulties { get; private set; }
        public ulong Tuning { get; private set; }
        public int BonusItemFindMultiplier { get; private set; }
        public ulong PlayerCameraSettingsOrbis { get; private set; }
        public ulong LoadingScreensConsole { get; private set; }
        public bool AllowLocalCoopMode { get; private set; }
    }

    public class RegionConnectionTargetPrototype : Prototype
    {
        public ulong Region { get; private set; }
        public ulong Area { get; private set; }
        public ulong Cell { get; private set; }
        public ulong Entity { get; private set; }
        public ulong IntroKismetSeq { get; private set; }
        public ulong Name { get; private set; }
        public bool EnabledByDefault { get; private set; }
        public int UISortOrder { get; private set; }
    }

    public class ObjectiveGraphSettingsPrototype : Prototype
    {
        public ObjectiveGraphModeAsset Mode { get; private set; }
    }

    public class FactionLimitPrototype : Prototype
    {
        public ulong Faction { get; private set; }
        public int PlayerLimit { get; private set; }
    }

    public class RegionAccessCheckPrototype : Prototype
    {
        public bool NoAccessOnFail { get; private set; }
        public bool NoDisplayOnFail { get; private set; }
    }

    public class LevelAccessCheckPrototype : RegionAccessCheckPrototype
    {
        public ulong UIResponseMessage { get; private set; }
        public ulong UILevelRangeFormat { get; private set; }
        public ulong UIMapDescriptionTag { get; private set; }
        public ulong UIWaypointNameTag { get; private set; }
        public int LevelMin { get; private set; }
        public int LevelMax { get; private set; }
    }

    public class RegionQueueStateEntryPrototype : Prototype
    {
        public ulong StateParent { get; private set; }
        public ulong State { get; private set; }
        public ulong QueueText { get; private set; }
    }

    public class DividedStartLocationPrototype : Prototype
    {
        public ulong Target { get; private set; }
        public int Players { get; private set; }
    }

    public class RegionPortalControlEntryPrototype : Prototype
    {
        public ulong Region { get; private set; }
        public int UnlockDurationMinutes { get; private set; }
        public int UnlockPeriodMinutes { get; private set; }
    }

    public class RegionConnectionNodePrototype : Prototype
    {
        public ulong Origin { get; private set; }
        public ulong Target { get; private set; }
        public RegionTransitionDirectionality Type { get; private set; }
    }

    public class ZoneLevelPrototype : Prototype
    {
    }

    public class ZoneLevelFixedPrototype : ZoneLevelPrototype
    {
        public short level { get; private set; }
    }

    public class ZoneLevelRelativePrototype : ZoneLevelPrototype
    {
        public short modmax { get; private set; }
        public short modmin { get; private set; }
    }

    public class BlackOutZonePrototype : Prototype
    {
        public float BlackOutRadius { get; private set; }
    }
}
