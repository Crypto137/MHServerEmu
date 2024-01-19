using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Invalid)]
    public enum DistributionType
    {
        Invalid,
        AllInOpenMissionRegion,
        Participants,
        Contributors,
    }

    [AssetEnum((int)Type1)]
    public enum ManaType
    {
        Type1 = 0,
        Type2 = 1,
        TypeAll = 3,
    }

    [AssetEnum((int)Undefined)]
    public enum TriBool
    {
        Undefined = -1,
        False = 0,
        True = 1,
    }

    [AssetEnum((int)NoChange)]
    public enum EntityTriggerEnum
    {
        NoChange = 0,
        Enabled = 1,
        Disabled = 2,
        Pulse = 3,
    }

    #endregion

    public class MissionActionPrototype : Prototype
    {
    }

    public class IncrementalActionEntryPrototype : Prototype
    {
        public int TriggerCount { get; protected set; }
        public MissionActionPrototype[] Actions { get; protected set; }
    }

    public class WeightedMissionEntryPrototype : Prototype
    {
        public ulong Mission { get; protected set; }
        public int Weight { get; protected set; }
    }

    public class MissionActionAvatarResetUltimateCooldownPrototype : MissionActionPrototype
    {
        public DistributionType ApplyTo { get; protected set; }
    }

    public class MissionActionSetActiveChapterPrototype : MissionActionPrototype
    {
        public ulong Chapter { get; protected set; }
    }

    public class MissionActionSetAvatarEndurancePrototype : MissionActionPrototype
    {
        public DistributionType ApplyTo { get; protected set; }
        public float Percentage { get; protected set; }
        public ManaType ManaType { get; protected set; }
    }

    public class MissionActionSetAvatarHealthPrototype : MissionActionPrototype
    {
        public DistributionType ApplyTo { get; protected set; }
        public float Percentage { get; protected set; }
    }

    public class MissionActionDangerRoomReturnScenarioItemPrototype : MissionActionPrototype
    {
    }

    public class MissionActionEncounterSpawnPrototype : MissionActionPrototype
    {
        public ulong EncounterResource { get; protected set; }
        public int Phase { get; protected set; }
        public bool MissionSpawnOnly { get; protected set; }
    }

    public class MissionActionDifficultyOverridePrototype : MissionActionPrototype
    {
        public int DifficultyIncrement { get; protected set; }
        public int DifficultyIndex { get; protected set; }
        public ulong DifficultyOverride { get; protected set; }
    }

    public class MissionActionRegionScorePrototype : MissionActionPrototype
    {
        public int Amount { get; protected set; }
    }

    public class MissionActionEntityTargetPrototype : MissionActionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public bool AllowWhenDead { get; protected set; }
    }

    public class MissionActionEntityCreatePrototype : MissionActionPrototype
    {
        public ulong EntityPrototype { get; protected set; }
    }

    public class MissionActionEntityDestroyPrototype : MissionActionEntityTargetPrototype
    {
    }

    public class MissionActionEntityKillPrototype : MissionActionEntityTargetPrototype
    {
        public bool SpawnLoot { get; protected set; }
        public bool GivePlayerCredit { get; protected set; }
    }

    public class MissionActionEntityPerformPowerPrototype : MissionActionEntityTargetPrototype
    {
        public ulong PowerPrototype { get; protected set; }
        public bool PowerRemove { get; protected set; }
        public ulong BrainOverride { get; protected set; }
        public bool BrainOverrideRemove { get; protected set; }
        public bool MissionReferencedPowerRemove { get; protected set; }
        public EvalPrototype EvalProperties { get; protected set; }
    }

    public class MissionActionEntitySetStatePrototype : MissionActionEntityTargetPrototype
    {
        public ulong EntityState { get; protected set; }
        public TriBool Interactable { get; protected set; }
    }

    public class MissionActionEventTeamAssignPrototype : MissionActionPrototype
    {
        public ulong Team { get; protected set; }
    }

    public class MissionActionFactionSetPrototype : MissionActionPrototype
    {
        public ulong Faction { get; protected set; }
        public DistributionType SendTo { get; protected set; }
    }

    public class MissionActionSpawnerTriggerPrototype : MissionActionEntityTargetPrototype
    {
        public EntityTriggerEnum Trigger { get; protected set; }
    }

    public class MissionActionHideHUDTutorialPrototype : MissionActionPrototype
    {
        public HUDTutorialPrototype HUDTutorial { get; protected set; }
        public DistributionType SendTo { get; protected set; }
    }

    public class MissionActionInventoryGiveAvatarPrototype : MissionActionPrototype
    {
        public ulong AvatarPrototype { get; protected set; }
    }

    public class MissionActionInventoryGiveTeamUpPrototype : MissionActionPrototype
    {
        public ulong TeamUpPrototype { get; protected set; }
    }

    public class MissionActionInventoryRemoveItemPrototype : MissionActionPrototype
    {
        public ulong ItemPrototype { get; protected set; }
        public long Count { get; protected set; }
        public MissionActionPrototype[] OnRemoveActions { get; protected set; }
    }

    public class MissionActionMetaStateWaveForcePrototype : MissionActionPrototype
    {
        public ulong SetStateProto { get; protected set; }
        public ulong WaveStateProto { get; protected set; }
    }

    public class MissionActionMissionActivatePrototype : MissionActionPrototype
    {
        public ulong MissionPrototype { get; protected set; }
        public WeightedMissionEntryPrototype[] WeightedMissionPickList { get; protected set; }
        public bool WeightedMissionPickUseRegionSeed { get; protected set; }
    }

    public class MissionActionRegionShutdownPrototype : MissionActionPrototype
    {
        public ulong RegionPrototype { get; protected set; }
    }

    public class MissionActionResetAllMissionsPrototype : MissionActionPrototype
    {
        public ulong MissionPrototype { get; protected set; }
    }

    public class MissionActionTimedActionPrototype : MissionActionPrototype
    {
        public MissionActionPrototype[] ActionsToPerform { get; protected set; }
        public double DelayInSeconds { get; protected set; }
        public bool Repeat { get; protected set; }
    }

    public class MissionActionScoringEventTimerEndPrototype : MissionActionPrototype
    {
        public ulong Timer { get; protected set; }
    }

    public class MissionActionScoringEventTimerStartPrototype : MissionActionPrototype
    {
        public ulong Timer { get; protected set; }
    }

    public class MissionActionScoringEventTimerStopPrototype : MissionActionPrototype
    {
        public ulong Timer { get; protected set; }
    }

    public class MissionActionStoryNotificationPrototype : MissionActionPrototype
    {
        public StoryNotificationPrototype StoryNotification { get; protected set; }
        public DistributionType SendTo { get; protected set; }
    }

    public class MissionActionShowBannerMessagePrototype : MissionActionPrototype
    {
        public BannerMessagePrototype BannerMessage { get; protected set; }
        public DistributionType SendTo { get; protected set; }
    }

    public class MissionActionShowHUDTutorialPrototype : MissionActionPrototype
    {
        public HUDTutorialPrototype HUDTutorial { get; protected set; }
        public DistributionType SendTo { get; protected set; }
    }

    public class MissionActionShowWaypointNotificationPrototype : MissionActionPrototype
    {
        public ulong Waypoint { get; protected set; }
        public DistributionType SendTo { get; protected set; }
    }

    public class MissionActionHideWaypointNotificationPrototype : MissionActionPrototype
    {
        public ulong Waypoint { get; protected set; }
        public DistributionType SendTo { get; protected set; }
    }

    public class MissionActionEnableRegionAvatarSwapPrototype : MissionActionPrototype
    {
    }

    public class MissionActionDisableRegionAvatarSwapPrototype : MissionActionPrototype
    {
    }

    public class MissionActionSwapAvatarPrototype : MissionActionPrototype
    {
        public ulong AvatarPrototype { get; protected set; }
        public bool UseAvatarSwapPowers { get; protected set; }
    }

    public class MissionActionEnableRegionRestrictedRosterPrototype : MissionActionPrototype
    {
    }

    public class MissionActionDisableRegionRestrictedRosterPrototype : MissionActionPrototype
    {
    }

    public class MissionActionUnlockUISystemPrototype : MissionActionPrototype
    {
        public ulong UISystem { get; protected set; }
    }

    public class MissionActionShowMotionComicPrototype : MissionActionPrototype
    {
        public ulong MotionComic { get; protected set; }
        public ulong DownloadChunkOverride { get; protected set; }
        public DistributionType SendTo { get; protected set; }
    }

    public class MissionActionUpdateMatchPrototype : MissionActionPrototype
    {
        public int MatchPhase { get; protected set; }
    }

    public class MissionActionShowOverheadTextPrototype : MissionActionEntityTargetPrototype
    {
        public ulong DisplayText { get; protected set; }
        public int DurationMS { get; protected set; }
    }

    public class MissionActionWaypointUnlockPrototype : MissionActionPrototype
    {
        public ulong WaypointToUnlock { get; protected set; }
    }

    public class MissionActionWaypointLockPrototype : MissionActionPrototype
    {
        public ulong WaypointToLock { get; protected set; }
    }

    public class MissionActionPlayBanterPrototype : MissionActionPrototype
    {
        public ulong BanterAsset { get; protected set; }
        public DistributionType SendTo { get; protected set; }
    }

    public class MissionActionPlayKismetSeqPrototype : MissionActionPrototype
    {
        public ulong KismetSeqPrototype { get; protected set; }
        public DistributionType SendTo { get; protected set; }
    }

    public class MissionActionParticipantPerformPowerPrototype : MissionActionPrototype
    {
        public ulong Power { get; protected set; }
        public DistributionType SendTo { get; protected set; }
    }

    public class MissionActionOpenUIPanelPrototype : MissionActionPrototype
    {
        public ulong PanelName { get; protected set; }
        public DistributionType SendTo { get; protected set; }
    }

    public class MissionActionPlayerTeleportPrototype : MissionActionPrototype
    {
        public ulong TeleportRegionTarget { get; protected set; }
        public DistributionType SendTo { get; protected set; }
    }

    public class MissionActionRemoveConditionsKwdPrototype : MissionActionPrototype
    {
        public ulong Keyword { get; protected set; }
        public DistributionType SendTo { get; protected set; }
    }

    public class MissionActionEntSelEvtBroadcastPrototype : MissionActionEntityTargetPrototype
    {
        public EntitySelectorActionEventType EventToBroadcast { get; protected set; }
    }

    public class MissionActionAllianceSetPrototype : MissionActionEntityTargetPrototype
    {
        public ulong Alliance { get; protected set; }
    }

    public class MissionActionShowTeamSelectDialogPrototype : MissionActionPrototype
    {
        public ulong PublicEvent { get; protected set; }
    }
}
