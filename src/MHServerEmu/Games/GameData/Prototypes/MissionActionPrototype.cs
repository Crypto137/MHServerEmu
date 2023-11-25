namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    public enum DistributionType
    {
        AllInOpenMissionRegion,
        Participants,
        Contributors,
    }

    public enum ManaType
    {
        Type1 = 0,
        Type2 = 1,
        TypeAll = 3,
    }

    public enum TriBool
    {
        Undefined = -1,
        False = 0,
        True = 1,
    }

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
        public int TriggerCount { get; set; }
        public MissionActionPrototype[] Actions { get; set; }
    }

    public class WeightedMissionEntryPrototype : Prototype
    {
        public ulong Mission { get; set; }
        public int Weight { get; set; }
    }

    public class MissionActionAvatarResetUltimateCooldownPrototype : MissionActionPrototype
    {
        public DistributionType ApplyTo { get; set; }
    }

    public class MissionActionSetActiveChapterPrototype : MissionActionPrototype
    {
        public ulong Chapter { get; set; }
    }

    public class MissionActionSetAvatarEndurancePrototype : MissionActionPrototype
    {
        public DistributionType ApplyTo { get; set; }
        public float Percentage { get; set; }
        public ManaType ManaType { get; set; }
    }

    public class MissionActionSetAvatarHealthPrototype : MissionActionPrototype
    {
        public DistributionType ApplyTo { get; set; }
        public float Percentage { get; set; }
    }

    public class MissionActionDangerRoomReturnScenarioItemPrototype : MissionActionPrototype
    {
    }

    public class MissionActionEncounterSpawnPrototype : MissionActionPrototype
    {
        public ulong EncounterResource { get; set; }
        public int Phase { get; set; }
        public bool MissionSpawnOnly { get; set; }
    }

    public class MissionActionDifficultyOverridePrototype : MissionActionPrototype
    {
        public int DifficultyIncrement { get; set; }
        public int DifficultyIndex { get; set; }
        public ulong DifficultyOverride { get; set; }
    }

    public class MissionActionRegionScorePrototype : MissionActionPrototype
    {
        public int Amount { get; set; }
    }

    public class MissionActionEntityTargetPrototype : MissionActionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; set; }
        public bool AllowWhenDead { get; set; }
    }

    public class MissionActionEntityCreatePrototype : MissionActionPrototype
    {
        public ulong EntityPrototype { get; set; }
    }

    public class MissionActionEntityDestroyPrototype : MissionActionEntityTargetPrototype
    {
    }

    public class MissionActionEntityKillPrototype : MissionActionEntityTargetPrototype
    {
        public bool SpawnLoot { get; set; }
        public bool GivePlayerCredit { get; set; }
    }

    public class MissionActionEntityPerformPowerPrototype : MissionActionEntityTargetPrototype
    {
        public ulong PowerPrototype { get; set; }
        public bool PowerRemove { get; set; }
        public ulong BrainOverride { get; set; }
        public bool BrainOverrideRemove { get; set; }
        public bool MissionReferencedPowerRemove { get; set; }
        public EvalPrototype EvalProperties { get; set; }
    }

    public class MissionActionEntitySetStatePrototype : MissionActionEntityTargetPrototype
    {
        public ulong EntityState { get; set; }
        public TriBool Interactable { get; set; }
    }

    public class MissionActionEventTeamAssignPrototype : MissionActionPrototype
    {
        public ulong Team { get; set; }
    }

    public class MissionActionFactionSetPrototype : MissionActionPrototype
    {
        public ulong Faction { get; set; }
        public DistributionType SendTo { get; set; }
    }

    public class MissionActionSpawnerTriggerPrototype : MissionActionEntityTargetPrototype
    {
        public EntityTriggerEnum Trigger { get; set; }
    }

    public class MissionActionHideHUDTutorialPrototype : MissionActionPrototype
    {
        public HUDTutorialPrototype HUDTutorial { get; set; }
        public DistributionType SendTo { get; set; }
    }

    public class MissionActionInventoryGiveAvatarPrototype : MissionActionPrototype
    {
        public ulong AvatarPrototype { get; set; }
    }

    public class MissionActionInventoryGiveTeamUpPrototype : MissionActionPrototype
    {
        public ulong TeamUpPrototype { get; set; }
    }

    public class MissionActionInventoryRemoveItemPrototype : MissionActionPrototype
    {
        public ulong ItemPrototype { get; set; }
        public long Count { get; set; }
        public MissionActionPrototype[] OnRemoveActions { get; set; }
    }

    public class MissionActionMetaStateWaveForcePrototype : MissionActionPrototype
    {
        public ulong SetStateProto { get; set; }
        public ulong WaveStateProto { get; set; }
    }

    public class MissionActionMissionActivatePrototype : MissionActionPrototype
    {
        public ulong MissionPrototype { get; set; }
        public WeightedMissionEntryPrototype[] WeightedMissionPickList { get; set; }
        public bool WeightedMissionPickUseRegionSeed { get; set; }
    }

    public class MissionActionRegionShutdownPrototype : MissionActionPrototype
    {
        public ulong RegionPrototype { get; set; }
    }

    public class MissionActionResetAllMissionsPrototype : MissionActionPrototype
    {
        public ulong MissionPrototype { get; set; }
    }

    public class MissionActionTimedActionPrototype : MissionActionPrototype
    {
        public MissionActionPrototype[] ActionsToPerform { get; set; }
        public double DelayInSeconds { get; set; }
        public bool Repeat { get; set; }
    }

    public class MissionActionScoringEventTimerEndPrototype : MissionActionPrototype
    {
        public ulong Timer { get; set; }
    }

    public class MissionActionScoringEventTimerStartPrototype : MissionActionPrototype
    {
        public ulong Timer { get; set; }
    }

    public class MissionActionScoringEventTimerStopPrototype : MissionActionPrototype
    {
        public ulong Timer { get; set; }
    }

    public class MissionActionStoryNotificationPrototype : MissionActionPrototype
    {
        public StoryNotificationPrototype StoryNotification { get; set; }
        public DistributionType SendTo { get; set; }
    }

    public class MissionActionShowBannerMessagePrototype : MissionActionPrototype
    {
        public BannerMessagePrototype BannerMessage { get; set; }
        public DistributionType SendTo { get; set; }
    }

    public class MissionActionShowHUDTutorialPrototype : MissionActionPrototype
    {
        public HUDTutorialPrototype HUDTutorial { get; set; }
        public DistributionType SendTo { get; set; }
    }

    public class MissionActionShowWaypointNotificationPrototype : MissionActionPrototype
    {
        public ulong Waypoint { get; set; }
        public DistributionType SendTo { get; set; }
    }

    public class MissionActionHideWaypointNotificationPrototype : MissionActionPrototype
    {
        public ulong Waypoint { get; set; }
        public DistributionType SendTo { get; set; }
    }

    public class MissionActionEnableRegionAvatarSwapPrototype : MissionActionPrototype
    {
    }

    public class MissionActionDisableRegionAvatarSwapPrototype : MissionActionPrototype
    {
    }

    public class MissionActionSwapAvatarPrototype : MissionActionPrototype
    {
        public ulong AvatarPrototype { get; set; }
        public bool UseAvatarSwapPowers { get; set; }
    }

    public class MissionActionEnableRegionRestrictedRosterPrototype : MissionActionPrototype
    {
    }

    public class MissionActionDisableRegionRestrictedRosterPrototype : MissionActionPrototype
    {
    }

    public class MissionActionUnlockUISystemPrototype : MissionActionPrototype
    {
        public ulong UISystem { get; set; }
    }

    public class MissionActionShowMotionComicPrototype : MissionActionPrototype
    {
        public ulong MotionComic { get; set; }
        public ulong DownloadChunkOverride { get; set; }
        public DistributionType SendTo { get; set; }
    }

    public class MissionActionUpdateMatchPrototype : MissionActionPrototype
    {
        public int MatchPhase { get; set; }
    }

    public class MissionActionShowOverheadTextPrototype : MissionActionEntityTargetPrototype
    {
        public ulong DisplayText { get; set; }
        public int DurationMS { get; set; }
    }

    public class MissionActionWaypointUnlockPrototype : MissionActionPrototype
    {
        public ulong WaypointToUnlock { get; set; }
    }

    public class MissionActionWaypointLockPrototype : MissionActionPrototype
    {
        public ulong WaypointToLock { get; set; }
    }

    public class MissionActionPlayBanterPrototype : MissionActionPrototype
    {
        public ulong BanterAsset { get; set; }
        public DistributionType SendTo { get; set; }
    }

    public class MissionActionPlayKismetSeqPrototype : MissionActionPrototype
    {
        public ulong KismetSeqPrototype { get; set; }
        public DistributionType SendTo { get; set; }
    }

    public class MissionActionParticipantPerformPowerPrototype : MissionActionPrototype
    {
        public ulong Power { get; set; }
        public DistributionType SendTo { get; set; }
    }

    public class MissionActionOpenUIPanelPrototype : MissionActionPrototype
    {
        public ulong PanelName { get; set; }
        public DistributionType SendTo { get; set; }
    }

    public class MissionActionPlayerTeleportPrototype : MissionActionPrototype
    {
        public ulong TeleportRegionTarget { get; set; }
        public DistributionType SendTo { get; set; }
    }

    public class MissionActionRemoveConditionsKwdPrototype : MissionActionPrototype
    {
        public ulong Keyword { get; set; }
        public DistributionType SendTo { get; set; }
    }

    public class MissionActionEntSelEvtBroadcastPrototype : MissionActionEntityTargetPrototype
    {
        public EntitySelectorActionEventType EventToBroadcast { get; set; }
    }

    public class MissionActionAllianceSetPrototype : MissionActionEntityTargetPrototype
    {
        public ulong Alliance { get; set; }
    }

    public class MissionActionShowTeamSelectDialogPrototype : MissionActionPrototype
    {
        public ulong PublicEvent { get; set; }
    }
}
