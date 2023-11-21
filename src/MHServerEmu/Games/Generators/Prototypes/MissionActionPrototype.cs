using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class MissionActionPrototype : Prototype
    {
        public MissionActionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionPrototype), proto); }
    }

    public enum DistributionType
    {
	    AllInOpenMissionRegion,
	    Participants,
	    Contributors,
    }

    public enum ManaType {
	    Type1 = 0,
	    Type2 = 1,
	    TypeAll = 3,
    }

    public enum TriBool {
	    Undefined = -1,
	    False = 0,
	    True = 1,
    }

    public enum EntityTriggerEnum {
	    NoChange = 0,
	    Enabled = 1,
	    Disabled = 2,
	    Pulse = 3,
    }

    public class IncrementalActionEntryPrototype : Prototype
    {
        public int TriggerCount;
        public MissionActionPrototype[] Actions;
        public IncrementalActionEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(IncrementalActionEntryPrototype), proto); }
    }

    public class WeightedMissionEntryPrototype : Prototype
    {
        public ulong Mission;
        public int Weight;
        public WeightedMissionEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(WeightedMissionEntryPrototype), proto); }
    }


    public class MissionActionAvatarResetUltimateCooldownPrototype : MissionActionPrototype
    {
        public DistributionType ApplyTo;
        public MissionActionAvatarResetUltimateCooldownPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionAvatarResetUltimateCooldownPrototype), proto); }
    }

    public class MissionActionSetActiveChapterPrototype : MissionActionPrototype
    {
        public ulong Chapter;
        public MissionActionSetActiveChapterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionSetActiveChapterPrototype), proto); }
    }

    public class MissionActionSetAvatarEndurancePrototype : MissionActionPrototype
    {
        public DistributionType ApplyTo;
        public float Percentage;
        public ManaType ManaType;
        public MissionActionSetAvatarEndurancePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionSetAvatarEndurancePrototype), proto); }
    }

    public class MissionActionSetAvatarHealthPrototype : MissionActionPrototype
    {
        public DistributionType ApplyTo;
        public float Percentage;
        public MissionActionSetAvatarHealthPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionSetAvatarHealthPrototype), proto); }
    }

    public class MissionActionDangerRoomReturnScenarioItemPrototype : MissionActionPrototype
    {
        public MissionActionDangerRoomReturnScenarioItemPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionDangerRoomReturnScenarioItemPrototype), proto); }
    }

    public class MissionActionEncounterSpawnPrototype : MissionActionPrototype
    {
        public ulong EncounterResource;
        public int Phase;
        public bool MissionSpawnOnly;
        public MissionActionEncounterSpawnPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionEncounterSpawnPrototype), proto); }
    }

    public class MissionActionDifficultyOverridePrototype : MissionActionPrototype
    {
        public int DifficultyIncrement;
        public int DifficultyIndex;
        public ulong DifficultyOverride;
        public MissionActionDifficultyOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionDifficultyOverridePrototype), proto); }
    }

    public class MissionActionRegionScorePrototype : MissionActionPrototype
    {
        public int Amount;
        public MissionActionRegionScorePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionRegionScorePrototype), proto); }
    }

    public class MissionActionEntityTargetPrototype : MissionActionPrototype
    {
        public EntityFilterPrototype EntityFilter;
        public bool AllowWhenDead;
        public MissionActionEntityTargetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionEntityTargetPrototype), proto); }
    }

    public class MissionActionEntityCreatePrototype : MissionActionPrototype
    {
        public ulong EntityPrototype;
        public MissionActionEntityCreatePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionEntityCreatePrototype), proto); }
    }

    public class MissionActionEntityDestroyPrototype : MissionActionEntityTargetPrototype
    {
        public MissionActionEntityDestroyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionEntityDestroyPrototype), proto); }
    }

    public class MissionActionEntityKillPrototype : MissionActionEntityTargetPrototype
    {
        public bool SpawnLoot;
        public bool GivePlayerCredit;
        public MissionActionEntityKillPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionEntityKillPrototype), proto); }
    }

    public class MissionActionEntityPerformPowerPrototype : MissionActionEntityTargetPrototype
    {
        public ulong PowerPrototype;
        public bool PowerRemove;
        public ulong BrainOverride;
        public bool BrainOverrideRemove;
        public bool MissionReferencedPowerRemove;
        public EvalPrototype EvalProperties;
        public MissionActionEntityPerformPowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionEntityPerformPowerPrototype), proto); }
    }

    public class MissionActionEntitySetStatePrototype : MissionActionEntityTargetPrototype
    {
        public ulong EntityState;
        public TriBool Interactable;
        public MissionActionEntitySetStatePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionEntitySetStatePrototype), proto); }
    }

    public class MissionActionEventTeamAssignPrototype : MissionActionPrototype
    {
        public ulong Team;
        public MissionActionEventTeamAssignPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionEventTeamAssignPrototype), proto); }
    }

    public class MissionActionFactionSetPrototype : MissionActionPrototype
    {
        public ulong Faction;
        public DistributionType SendTo;
        public MissionActionFactionSetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionFactionSetPrototype), proto); }
    }

    public class MissionActionSpawnerTriggerPrototype : MissionActionEntityTargetPrototype
    {
        public EntityTriggerEnum Trigger;
        public MissionActionSpawnerTriggerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionSpawnerTriggerPrototype), proto); }
    }

    public class MissionActionHideHUDTutorialPrototype : MissionActionPrototype
    {
        public HUDTutorialPrototype HUDTutorial;
        public DistributionType SendTo;
        public MissionActionHideHUDTutorialPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionHideHUDTutorialPrototype), proto); }
    }

    public class MissionActionInventoryGiveAvatarPrototype : MissionActionPrototype
    {
        public ulong AvatarPrototype;
        public MissionActionInventoryGiveAvatarPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionInventoryGiveAvatarPrototype), proto); }
    }

    public class MissionActionInventoryGiveTeamUpPrototype : MissionActionPrototype
    {
        public ulong TeamUpPrototype;
        public MissionActionInventoryGiveTeamUpPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionInventoryGiveTeamUpPrototype), proto); }
    }

    public class MissionActionInventoryRemoveItemPrototype : MissionActionPrototype
    {
        public ulong ItemPrototype;
        public long Count;
        public MissionActionPrototype[] OnRemoveActions;
        public MissionActionInventoryRemoveItemPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionInventoryRemoveItemPrototype), proto); }
    }

    public class MissionActionMetaStateWaveForcePrototype : MissionActionPrototype
    {
        public ulong SetStateProto;
        public ulong WaveStateProto;
        public MissionActionMetaStateWaveForcePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionMetaStateWaveForcePrototype), proto); }
    }

    public class MissionActionMissionActivatePrototype : MissionActionPrototype
    {
        public ulong MissionPrototype;
        public WeightedMissionEntryPrototype[] WeightedMissionPickList;
        public bool WeightedMissionPickUseRegionSeed;
        public MissionActionMissionActivatePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionMissionActivatePrototype), proto); }
    }

    public class MissionActionRegionShutdownPrototype : MissionActionPrototype
    {
        public ulong RegionPrototype;
        public MissionActionRegionShutdownPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionRegionShutdownPrototype), proto); }
    }

    public class MissionActionResetAllMissionsPrototype : MissionActionPrototype
    {
        public ulong MissionPrototype;
        public MissionActionResetAllMissionsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionResetAllMissionsPrototype), proto); }
    }

    public class MissionActionTimedActionPrototype : MissionActionPrototype
    {
        public MissionActionPrototype[] ActionsToPerform;
        public double DelayInSeconds;
        public bool Repeat;
        public MissionActionTimedActionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionTimedActionPrototype), proto); }
    }

    public class MissionActionScoringEventTimerEndPrototype : MissionActionPrototype
    {
        public ulong Timer;
        public MissionActionScoringEventTimerEndPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionScoringEventTimerEndPrototype), proto); }
    }

    public class MissionActionScoringEventTimerStartPrototype : MissionActionPrototype
    {
        public ulong Timer;
        public MissionActionScoringEventTimerStartPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionScoringEventTimerStartPrototype), proto); }
    }

    public class MissionActionScoringEventTimerStopPrototype : MissionActionPrototype
    {
        public ulong Timer;
        public MissionActionScoringEventTimerStopPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionScoringEventTimerStopPrototype), proto); }
    }

    public class MissionActionStoryNotificationPrototype : MissionActionPrototype
    {
        public StoryNotificationPrototype StoryNotification;
        public DistributionType SendTo;
        public MissionActionStoryNotificationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionStoryNotificationPrototype), proto); }
    }

    public class MissionActionShowBannerMessagePrototype : MissionActionPrototype
    {
        public BannerMessagePrototype BannerMessage;
        public DistributionType SendTo;
        public MissionActionShowBannerMessagePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionShowBannerMessagePrototype), proto); }
    }

    public class MissionActionShowHUDTutorialPrototype : MissionActionPrototype
    {
        public HUDTutorialPrototype HUDTutorial;
        public DistributionType SendTo;
        public MissionActionShowHUDTutorialPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionShowHUDTutorialPrototype), proto); }
    }

    public class MissionActionShowWaypointNotificationPrototype : MissionActionPrototype
    {
        public ulong Waypoint;
        public DistributionType SendTo;
        public MissionActionShowWaypointNotificationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionShowWaypointNotificationPrototype), proto); }
    }

    public class MissionActionHideWaypointNotificationPrototype : MissionActionPrototype
    {
        public ulong Waypoint;
        public DistributionType SendTo;
        public MissionActionHideWaypointNotificationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionHideWaypointNotificationPrototype), proto); }
    }

    public class MissionActionEnableRegionAvatarSwapPrototype : MissionActionPrototype
    {
        public MissionActionEnableRegionAvatarSwapPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionEnableRegionAvatarSwapPrototype), proto); }
    }

    public class MissionActionDisableRegionAvatarSwapPrototype : MissionActionPrototype
    {
        public MissionActionDisableRegionAvatarSwapPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionDisableRegionAvatarSwapPrototype), proto); }
    }

    public class MissionActionSwapAvatarPrototype : MissionActionPrototype
    {
        public ulong AvatarPrototype;
        public bool UseAvatarSwapPowers;
        public MissionActionSwapAvatarPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionSwapAvatarPrototype), proto); }
    }

    public class MissionActionEnableRegionRestrictedRosterPrototype : MissionActionPrototype
    {
        public MissionActionEnableRegionRestrictedRosterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionEnableRegionRestrictedRosterPrototype), proto); }
    }

    public class MissionActionDisableRegionRestrictedRosterPrototype : MissionActionPrototype
    {
        public MissionActionDisableRegionRestrictedRosterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionDisableRegionRestrictedRosterPrototype), proto); }
    }

    public class MissionActionUnlockUISystemPrototype : MissionActionPrototype
    {
        public ulong UISystem;
        public MissionActionUnlockUISystemPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionUnlockUISystemPrototype), proto); }
    }

    public class MissionActionShowMotionComicPrototype : MissionActionPrototype
    {
        public ulong MotionComic;
        public ulong DownloadChunkOverride;
        public DistributionType SendTo;
        public MissionActionShowMotionComicPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionShowMotionComicPrototype), proto); }
    }

    public class MissionActionUpdateMatchPrototype : MissionActionPrototype
    {
        public int MatchPhase;
        public MissionActionUpdateMatchPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionUpdateMatchPrototype), proto); }
    }

    public class MissionActionShowOverheadTextPrototype : MissionActionEntityTargetPrototype
    {
        public ulong DisplayText;
        public int DurationMS;
        public MissionActionShowOverheadTextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionShowOverheadTextPrototype), proto); }
    }

    public class MissionActionWaypointUnlockPrototype : MissionActionPrototype
    {
        public ulong WaypointToUnlock;
        public MissionActionWaypointUnlockPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionWaypointUnlockPrototype), proto); }
    }

    public class MissionActionWaypointLockPrototype : MissionActionPrototype
    {
        public ulong WaypointToLock;
        public MissionActionWaypointLockPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionWaypointLockPrototype), proto); }
    }

    public class MissionActionPlayBanterPrototype : MissionActionPrototype
    {
        public ulong BanterAsset;
        public DistributionType SendTo;
        public MissionActionPlayBanterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionPlayBanterPrototype), proto); }
    }

    public class MissionActionPlayKismetSeqPrototype : MissionActionPrototype
    {
        public ulong KismetSeqPrototype;
        public DistributionType SendTo;
        public MissionActionPlayKismetSeqPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionPlayKismetSeqPrototype), proto); }
    }

    public class MissionActionParticipantPerformPowerPrototype : MissionActionPrototype
    {
        public ulong Power;
        public DistributionType SendTo;
        public MissionActionParticipantPerformPowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionParticipantPerformPowerPrototype), proto); }
    }

    public class MissionActionOpenUIPanelPrototype : MissionActionPrototype
    {
        public ulong PanelName;
        public DistributionType SendTo;
        public MissionActionOpenUIPanelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionOpenUIPanelPrototype), proto); }
    }

    public class MissionActionPlayerTeleportPrototype : MissionActionPrototype
    {
        public ulong TeleportRegionTarget;
        public DistributionType SendTo;
        public MissionActionPlayerTeleportPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionPlayerTeleportPrototype), proto); }
    }

    public class MissionActionRemoveConditionsKwdPrototype : MissionActionPrototype
    {
        public ulong Keyword;
        public DistributionType SendTo;
        public MissionActionRemoveConditionsKwdPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionRemoveConditionsKwdPrototype), proto); }
    }

    public class MissionActionEntSelEvtBroadcastPrototype : MissionActionEntityTargetPrototype
    {
        public EntitySelectorActionEventType EventToBroadcast;
        public MissionActionEntSelEvtBroadcastPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionEntSelEvtBroadcastPrototype), proto); }
    }

    public class MissionActionAllianceSetPrototype : MissionActionEntityTargetPrototype
    {
        public ulong Alliance;
        public MissionActionAllianceSetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionAllianceSetPrototype), proto); }
    }

    public class MissionActionShowTeamSelectDialogPrototype : MissionActionPrototype
    {
        public ulong PublicEvent;
        public MissionActionShowTeamSelectDialogPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionActionShowTeamSelectDialogPrototype), proto); }
    }

}
