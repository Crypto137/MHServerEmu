using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum DistributionType
    {
        AllInOpenMissionRegion,
        Participants,
        Contributors,
    }

    [AssetEnum]
    public enum ManaType
    {
        Type1 = 0,
        Type2 = 1,
        TypeAll = 3,
    }

    [AssetEnum]
    public enum TriBool
    {
        Undefined = -1,
        False = 0,
        True = 1,
    }

    [AssetEnum]
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
        public int TriggerCount { get; private set; }
        public MissionActionPrototype[] Actions { get; private set; }
    }

    public class WeightedMissionEntryPrototype : Prototype
    {
        public ulong Mission { get; private set; }
        public int Weight { get; private set; }
    }

    public class MissionActionAvatarResetUltimateCooldownPrototype : MissionActionPrototype
    {
        public DistributionType ApplyTo { get; private set; }
    }

    public class MissionActionSetActiveChapterPrototype : MissionActionPrototype
    {
        public ulong Chapter { get; private set; }
    }

    public class MissionActionSetAvatarEndurancePrototype : MissionActionPrototype
    {
        public DistributionType ApplyTo { get; private set; }
        public float Percentage { get; private set; }
        public ManaType ManaType { get; private set; }
    }

    public class MissionActionSetAvatarHealthPrototype : MissionActionPrototype
    {
        public DistributionType ApplyTo { get; private set; }
        public float Percentage { get; private set; }
    }

    public class MissionActionDangerRoomReturnScenarioItemPrototype : MissionActionPrototype
    {
    }

    public class MissionActionEncounterSpawnPrototype : MissionActionPrototype
    {
        public ulong EncounterResource { get; private set; }
        public int Phase { get; private set; }
        public bool MissionSpawnOnly { get; private set; }
    }

    public class MissionActionDifficultyOverridePrototype : MissionActionPrototype
    {
        public int DifficultyIncrement { get; private set; }
        public int DifficultyIndex { get; private set; }
        public ulong DifficultyOverride { get; private set; }
    }

    public class MissionActionRegionScorePrototype : MissionActionPrototype
    {
        public int Amount { get; private set; }
    }

    public class MissionActionEntityTargetPrototype : MissionActionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; private set; }
        public bool AllowWhenDead { get; private set; }
    }

    public class MissionActionEntityCreatePrototype : MissionActionPrototype
    {
        public ulong EntityPrototype { get; private set; }
    }

    public class MissionActionEntityDestroyPrototype : MissionActionEntityTargetPrototype
    {
    }

    public class MissionActionEntityKillPrototype : MissionActionEntityTargetPrototype
    {
        public bool SpawnLoot { get; private set; }
        public bool GivePlayerCredit { get; private set; }
    }

    public class MissionActionEntityPerformPowerPrototype : MissionActionEntityTargetPrototype
    {
        public ulong PowerPrototype { get; private set; }
        public bool PowerRemove { get; private set; }
        public ulong BrainOverride { get; private set; }
        public bool BrainOverrideRemove { get; private set; }
        public bool MissionReferencedPowerRemove { get; private set; }
        public EvalPrototype EvalProperties { get; private set; }
    }

    public class MissionActionEntitySetStatePrototype : MissionActionEntityTargetPrototype
    {
        public ulong EntityState { get; private set; }
        public TriBool Interactable { get; private set; }
    }

    public class MissionActionEventTeamAssignPrototype : MissionActionPrototype
    {
        public ulong Team { get; private set; }
    }

    public class MissionActionFactionSetPrototype : MissionActionPrototype
    {
        public ulong Faction { get; private set; }
        public DistributionType SendTo { get; private set; }
    }

    public class MissionActionSpawnerTriggerPrototype : MissionActionEntityTargetPrototype
    {
        public EntityTriggerEnum Trigger { get; private set; }
    }

    public class MissionActionHideHUDTutorialPrototype : MissionActionPrototype
    {
        public HUDTutorialPrototype HUDTutorial { get; private set; }
        public DistributionType SendTo { get; private set; }
    }

    public class MissionActionInventoryGiveAvatarPrototype : MissionActionPrototype
    {
        public ulong AvatarPrototype { get; private set; }
    }

    public class MissionActionInventoryGiveTeamUpPrototype : MissionActionPrototype
    {
        public ulong TeamUpPrototype { get; private set; }
    }

    public class MissionActionInventoryRemoveItemPrototype : MissionActionPrototype
    {
        public ulong ItemPrototype { get; private set; }
        public long Count { get; private set; }
        public MissionActionPrototype[] OnRemoveActions { get; private set; }
    }

    public class MissionActionMetaStateWaveForcePrototype : MissionActionPrototype
    {
        public ulong SetStateProto { get; private set; }
        public ulong WaveStateProto { get; private set; }
    }

    public class MissionActionMissionActivatePrototype : MissionActionPrototype
    {
        public ulong MissionPrototype { get; private set; }
        public WeightedMissionEntryPrototype[] WeightedMissionPickList { get; private set; }
        public bool WeightedMissionPickUseRegionSeed { get; private set; }
    }

    public class MissionActionRegionShutdownPrototype : MissionActionPrototype
    {
        public ulong RegionPrototype { get; private set; }
    }

    public class MissionActionResetAllMissionsPrototype : MissionActionPrototype
    {
        public ulong MissionPrototype { get; private set; }
    }

    public class MissionActionTimedActionPrototype : MissionActionPrototype
    {
        public MissionActionPrototype[] ActionsToPerform { get; private set; }
        public double DelayInSeconds { get; private set; }
        public bool Repeat { get; private set; }
    }

    public class MissionActionScoringEventTimerEndPrototype : MissionActionPrototype
    {
        public ulong Timer { get; private set; }
    }

    public class MissionActionScoringEventTimerStartPrototype : MissionActionPrototype
    {
        public ulong Timer { get; private set; }
    }

    public class MissionActionScoringEventTimerStopPrototype : MissionActionPrototype
    {
        public ulong Timer { get; private set; }
    }

    public class MissionActionStoryNotificationPrototype : MissionActionPrototype
    {
        public StoryNotificationPrototype StoryNotification { get; private set; }
        public DistributionType SendTo { get; private set; }
    }

    public class MissionActionShowBannerMessagePrototype : MissionActionPrototype
    {
        public BannerMessagePrototype BannerMessage { get; private set; }
        public DistributionType SendTo { get; private set; }
    }

    public class MissionActionShowHUDTutorialPrototype : MissionActionPrototype
    {
        public HUDTutorialPrototype HUDTutorial { get; private set; }
        public DistributionType SendTo { get; private set; }
    }

    public class MissionActionShowWaypointNotificationPrototype : MissionActionPrototype
    {
        public ulong Waypoint { get; private set; }
        public DistributionType SendTo { get; private set; }
    }

    public class MissionActionHideWaypointNotificationPrototype : MissionActionPrototype
    {
        public ulong Waypoint { get; private set; }
        public DistributionType SendTo { get; private set; }
    }

    public class MissionActionEnableRegionAvatarSwapPrototype : MissionActionPrototype
    {
    }

    public class MissionActionDisableRegionAvatarSwapPrototype : MissionActionPrototype
    {
    }

    public class MissionActionSwapAvatarPrototype : MissionActionPrototype
    {
        public ulong AvatarPrototype { get; private set; }
        public bool UseAvatarSwapPowers { get; private set; }
    }

    public class MissionActionEnableRegionRestrictedRosterPrototype : MissionActionPrototype
    {
    }

    public class MissionActionDisableRegionRestrictedRosterPrototype : MissionActionPrototype
    {
    }

    public class MissionActionUnlockUISystemPrototype : MissionActionPrototype
    {
        public ulong UISystem { get; private set; }
    }

    public class MissionActionShowMotionComicPrototype : MissionActionPrototype
    {
        public ulong MotionComic { get; private set; }
        public ulong DownloadChunkOverride { get; private set; }
        public DistributionType SendTo { get; private set; }
    }

    public class MissionActionUpdateMatchPrototype : MissionActionPrototype
    {
        public int MatchPhase { get; private set; }
    }

    public class MissionActionShowOverheadTextPrototype : MissionActionEntityTargetPrototype
    {
        public ulong DisplayText { get; private set; }
        public int DurationMS { get; private set; }
    }

    public class MissionActionWaypointUnlockPrototype : MissionActionPrototype
    {
        public ulong WaypointToUnlock { get; private set; }
    }

    public class MissionActionWaypointLockPrototype : MissionActionPrototype
    {
        public ulong WaypointToLock { get; private set; }
    }

    public class MissionActionPlayBanterPrototype : MissionActionPrototype
    {
        public ulong BanterAsset { get; private set; }
        public DistributionType SendTo { get; private set; }
    }

    public class MissionActionPlayKismetSeqPrototype : MissionActionPrototype
    {
        public ulong KismetSeqPrototype { get; private set; }
        public DistributionType SendTo { get; private set; }
    }

    public class MissionActionParticipantPerformPowerPrototype : MissionActionPrototype
    {
        public ulong Power { get; private set; }
        public DistributionType SendTo { get; private set; }
    }

    public class MissionActionOpenUIPanelPrototype : MissionActionPrototype
    {
        public ulong PanelName { get; private set; }
        public DistributionType SendTo { get; private set; }
    }

    public class MissionActionPlayerTeleportPrototype : MissionActionPrototype
    {
        public ulong TeleportRegionTarget { get; private set; }
        public DistributionType SendTo { get; private set; }
    }

    public class MissionActionRemoveConditionsKwdPrototype : MissionActionPrototype
    {
        public ulong Keyword { get; private set; }
        public DistributionType SendTo { get; private set; }
    }

    public class MissionActionEntSelEvtBroadcastPrototype : MissionActionEntityTargetPrototype
    {
        public EntitySelectorActionEventType EventToBroadcast { get; private set; }
    }

    public class MissionActionAllianceSetPrototype : MissionActionEntityTargetPrototype
    {
        public ulong Alliance { get; private set; }
    }

    public class MissionActionShowTeamSelectDialogPrototype : MissionActionPrototype
    {
        public ulong PublicEvent { get; private set; }
    }
}
