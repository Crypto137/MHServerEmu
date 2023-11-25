namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    public enum MissionTrackerFilterType
    {
        None = -1,
        Standard = 0,
        PvE = 1,
        PvP = 2,
        Daily = 3,
        Challenge = 4,
    }

    public enum RegionBehaviorAsset
    {
        Invalid = -1,
        Town = 0,
        PublicCombatZone = 1,
        PrivateStory = 2,
        PrivateNonStory = 3,
        PrivateRaid = 5,
        MatchPlay = 4,
    }

    public enum RegionMusicBehaviorAsset
    {
        None,
        Default,
        Mission,
    }

    public enum FootstepTraceBehaviorAsset
    {
        None,
        Enable,
        Disable,
    }

    public enum RegionQueueMethod
    {
        None = 0,
        PvPQueue = 1,
        DailyQueue = 5,
    }

    public enum ObjectiveGraphModeAsset
    {
        Off,
        PathDistance,
        PathNavi,
    }

    public enum RegionTransitionDirectionality
    {
        BiDirectional = 0,
        OneWay = 1,
        Disabled = 2,
    }

    #endregion

    public class RegionPrototype : Prototype
    {
        public ulong ClientMap { get; set; }
        public ulong BodySliderTarget { get; set; }
        public ulong StartTarget { get; set; }
        public ulong[] Music { get; set; }
        public RegionGeneratorPrototype RegionGenerator { get; set; }
        public RegionBehaviorAsset Behavior { get; set; }
        public ulong RegionName { get; set; }
        public ulong MetaGames { get; set; }
        public bool ForceSimulation { get; set; }
        public ulong LoadingScreens { get; set; }
        public bool AlwaysRevealFullMap { get; set; }
        public ulong Chapter { get; set; }
        public int PlayerLimit { get; set; }
        public float LifetimeInMinutes { get; set; }
        public ulong WaypointAutoUnlock { get; set; }
        public bool PartyFormationAllowed { get; set; }
        public TransitionUIPrototype[] TransitionUITypes { get; set; }
        public ulong AmbientSfx { get; set; }
        public ulong[] PowerKeywordBlacklist { get; set; }
        public bool CloseWhenReservationsReachesZero { get; set; }
        public float UIMapWallThickness { get; set; }
        public ulong[] PopulationOverrides { get; set; }
        public int Level { get; set; }
        public MissionTrackerFilterType[] MissionTrackerFilterList { get; set; }
        public bool AllowAutoPartyOnEnter { get; set; }
        public float AutoPartyWindowSecs { get; set; }
        public bool DailyCheckpointStartTarget { get; set; }
        public int LowPopulationPlayerLimit { get; set; }
        public ulong RespawnOverride { get; set; }
        public ulong PlayerCameraSettings { get; set; }
        public RegionQueueMethod RegionQueueMethod { get; set; }
        public EvalPrototype EvalAccessRestriction { get; set; }
        public ulong WaypointAutoUnlockList { get; set; }
        public bool AlwaysShutdownWhenVacant { get; set; }
        public bool SynergyEditAllowed { get; set; }
        public ulong[] Keywords { get; set; }
        public ulong UITopPanel { get; set; }
        public ulong[] AltRegions { get; set; }
        public RegionAccessCheckPrototype[] AccessChecks { get; set; }
        public ulong UIDescription { get; set; }
        public ulong UILocation { get; set; }
        public bool PausesBoostConditions { get; set; }
        public bool ShowTransitionIndicators { get; set; }
        public RegionQueueStateEntryPrototype[] RegionQueueStates { get; set; }
        public ulong MarkerFilter { get; set; }
        public bool LevelBandedRegionUsesPlayerLevel { get; set; }
        public FootstepTraceBehaviorAsset FootstepTraceOverride { get; set; }
        public bool QueueDoNotWaitToFull { get; set; }
        public bool DisplayCommunityNews { get; set; }
        public ulong UnrealClass { get; set; }
        public bool RespawnDestructibles { get; set; }
        public ulong PropertyGameModeSetOnEntry { get; set; }
        public bool UsePrevRegionPlayerDeathCount { get; set; }
        public LootTableAssignmentPrototype[] LootTables { get; set; }
        public ulong AffixTable { get; set; }
        public ObjectiveGraphSettingsPrototype ObjectiveGraph { get; set; }
        public DividedStartLocationPrototype[] DividedStartLocations { get; set; }
        public RegionMusicBehaviorAsset MusicBehavior { get; set; }
        public ulong AvatarObjectiveInfoOverride { get; set; }
        public RegionDifficultySettingsPrototype DifficultySettings { get; set; }
        public bool LevelOverridesCharacterLevel { get; set; }
        public bool LevelUseAreaOffset { get; set; }
        public ulong EvalAccessRestrictionMessage { get; set; }
        public bool BodySliderOneWay { get; set; }
        public bool EnableAvatarSwap { get; set; }
        public ulong[] RestrictedRoster { get; set; }
        public ulong[] AvatarPowers { get; set; }
        public bool IsNPE { get; set; }
        public ulong PresenceStatusText { get; set; }
        public ulong[] AccessDifficulties { get; set; }
        public ulong Tuning { get; set; }
        public int BonusItemFindMultiplier { get; set; }
        public ulong PlayerCameraSettingsOrbis { get; set; }
        public ulong LoadingScreensConsole { get; set; }
        public bool AllowLocalCoopMode { get; set; }
    }

    public class RegionConnectionTargetPrototype : Prototype
    {
        public ulong Region { get; set; }
        public ulong Area { get; set; }
        public ulong Cell { get; set; }
        public ulong Entity { get; set; }
        public ulong IntroKismetSeq { get; set; }
        public ulong Name { get; set; }
        public bool EnabledByDefault { get; set; }
        public int UISortOrder { get; set; }
    }

    public class ObjectiveGraphSettingsPrototype : Prototype
    {
        public ObjectiveGraphModeAsset Mode { get; set; }
    }

    public class FactionLimitPrototype : Prototype
    {
        public ulong Faction { get; set; }
        public int PlayerLimit { get; set; }
    }

    public class RegionAccessCheckPrototype : Prototype
    {
        public bool NoAccessOnFail { get; set; }
        public bool NoDisplayOnFail { get; set; }
    }

    public class LevelAccessCheckPrototype : RegionAccessCheckPrototype
    {
        public ulong UIResponseMessage { get; set; }
        public ulong UILevelRangeFormat { get; set; }
        public ulong UIMapDescriptionTag { get; set; }
        public ulong UIWaypointNameTag { get; set; }
        public int LevelMin { get; set; }
        public int LevelMax { get; set; }
    }

    public class RegionQueueStateEntryPrototype : Prototype
    {
        public ulong StateParent { get; set; }
        public ulong State { get; set; }
        public ulong QueueText { get; set; }
    }

    public class DividedStartLocationPrototype : Prototype
    {
        public ulong Target { get; set; }
        public int Players { get; set; }
    }

    public class RegionPortalControlEntryPrototype : Prototype
    {
        public ulong Region { get; set; }
        public int UnlockDurationMinutes { get; set; }
        public int UnlockPeriodMinutes { get; set; }
    }

    public class RegionConnectionNodePrototype : Prototype
    {
        public ulong Origin { get; set; }
        public ulong Target { get; set; }
        public RegionTransitionDirectionality Type { get; set; }
    }

    public class ZoneLevelPrototype : Prototype
    {
    }

    public class ZoneLevelFixedPrototype : ZoneLevelPrototype
    {
        public short level { get; set; }
    }

    public class ZoneLevelRelativePrototype : ZoneLevelPrototype
    {
        public short modmax { get; set; }
        public short modmin { get; set; }
    }

    public class BlackOutZonePrototype : Prototype
    {
        public float BlackOutRadius { get; set; }
    }
}
