using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Missions.Actions;

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
        public virtual MissionAction AllocateAction(IMissionActionOwner owner) { return null; }
    }

    public class IncrementalActionEntryPrototype : Prototype
    {
        public int TriggerCount { get; protected set; }
        public MissionActionPrototype[] Actions { get; protected set; }
    }

    public class WeightedMissionEntryPrototype : Prototype
    {
        public PrototypeId Mission { get; protected set; }
        public int Weight { get; protected set; }
    }

    public class MissionActionAvatarResetUltimateCooldownPrototype : MissionActionPrototype
    {
        public DistributionType ApplyTo { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionAvatarResetUltimateCooldown(owner, this);
        }
    }

    public class MissionActionSetActiveChapterPrototype : MissionActionPrototype
    {
        public PrototypeId Chapter { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionSetActiveChapter(owner, this);
        }
    }

    public class MissionActionSetAvatarEndurancePrototype : MissionActionPrototype
    {
        public DistributionType ApplyTo { get; protected set; }
        public float Percentage { get; protected set; }
        public ManaType ManaType { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionSetAvatarEndurance(owner, this);
        }
    }

    public class MissionActionSetAvatarHealthPrototype : MissionActionPrototype
    {
        public DistributionType ApplyTo { get; protected set; }
        public float Percentage { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionSetAvatarHealth(owner, this);
        }
    }

    public class MissionActionDangerRoomReturnScenarioItemPrototype : MissionActionPrototype
    {
        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionDangerRoomReturnScenarioItem(owner, this);
        }
    }

    public class MissionActionEncounterSpawnPrototype : MissionActionPrototype
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public AssetId EncounterResource { get; protected set; }
        public int Phase { get; protected set; }
        public bool MissionSpawnOnly { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionEncounterSpawn(owner, this);
        }

        public PrototypeId GetEncounterRef()
        {
            if (EncounterResource == AssetId.Invalid)
            {
                Logger.Warn($"{ToString()} has no value in its EncounterResource field.");
                return PrototypeId.Invalid;
            }

            PrototypeId encounterProtoRef = GameDatabase.GetDataRefByAsset(EncounterResource);
            if (encounterProtoRef == PrototypeId.Invalid)
            {
                Logger.Warn($"{ToString()} was unable to find resource for asset {GameDatabase.GetAssetName(EncounterResource)}, check file path and verify file exists.");
                return PrototypeId.Invalid;
            }

            return encounterProtoRef;
        }
    }

    public class MissionActionDifficultyOverridePrototype : MissionActionPrototype
    {
        public int DifficultyIncrement { get; protected set; }
        public int DifficultyIndex { get; protected set; }
        public PrototypeId DifficultyOverride { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionDifficultyOverride(owner, this);
        }
    }

    public class MissionActionRegionScorePrototype : MissionActionPrototype
    {
        public int Amount { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionRegionScore(owner, this);
        }
    }

    public class MissionActionEntityTargetPrototype : MissionActionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public bool AllowWhenDead { get; protected set; }

        public void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (EntityFilter != null)
            {
                EntityFilter.GetEntityDataRefs(refs);
                EntityFilter.GetKeywordDataRefs(refs);
                EntityFilter.GetRegionDataRefs(refs);
            }
        }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return null;
        }
    }

    public class MissionActionEntityCreatePrototype : MissionActionPrototype
    {
        public PrototypeId EntityPrototype { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionEntityCreate(owner, this);
        }
    }

    public class MissionActionEntityDestroyPrototype : MissionActionEntityTargetPrototype
    {
        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionEntityDestroy(owner, this);
        }
    }

    public class MissionActionEntityKillPrototype : MissionActionEntityTargetPrototype
    {
        public bool SpawnLoot { get; protected set; }
        public bool GivePlayerCredit { get; protected set; }

        [DoNotCopy]
        public KillFlags KillFlags { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionEntityKill(owner, this);
        }

        public override void PostProcess()
        {
            base.PostProcess();
            KillFlags = KillFlags.None;
            if (GivePlayerCredit == false) KillFlags |= KillFlags.NoDeadEvent;
            if (SpawnLoot == false) KillFlags |= KillFlags.NoLoot | KillFlags.NoExp;
        }
    }

    public class MissionActionEntityPerformPowerPrototype : MissionActionEntityTargetPrototype
    {
        public PrototypeId PowerPrototype { get; protected set; }
        public bool PowerRemove { get; protected set; }
        public PrototypeId BrainOverride { get; protected set; }
        public bool BrainOverrideRemove { get; protected set; }
        public bool MissionReferencedPowerRemove { get; protected set; }
        public EvalPrototype EvalProperties { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionEntityPerformPower(owner, this);
        }
    }

    public class MissionActionEntitySetStatePrototype : MissionActionEntityTargetPrototype
    {
        public PrototypeId EntityState { get; protected set; }
        public TriBool Interactable { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionEntitySetState(owner, this);
        }
    }

    public class MissionActionEventTeamAssignPrototype : MissionActionPrototype
    {
        public PrototypeId Team { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionEventTeamAssign(owner, this);
        }
    }

    public class MissionActionFactionSetPrototype : MissionActionPrototype
    {
        public PrototypeId Faction { get; protected set; }
        public DistributionType SendTo { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionFactionSet(owner, this);
        }
    }

    public class MissionActionSpawnerTriggerPrototype : MissionActionEntityTargetPrototype
    {
        public EntityTriggerEnum Trigger { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionSpawnerTrigger(owner, this);
        }
    }

    public class MissionActionHideHUDTutorialPrototype : MissionActionPrototype
    {
        public HUDTutorialPrototype HUDTutorial { get; protected set; }
        public DistributionType SendTo { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionHideHUDTutorial(owner, this);
        }
    }

    public class MissionActionInventoryGiveAvatarPrototype : MissionActionPrototype
    {
        public PrototypeId AvatarPrototype { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionInventoryGiveAvatar(owner, this);
        }
    }

    public class MissionActionInventoryGiveTeamUpPrototype : MissionActionPrototype
    {
        public PrototypeId TeamUpPrototype { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionInventoryGiveTeamUp(owner, this);
        }
    }

    public class MissionActionInventoryRemoveItemPrototype : MissionActionPrototype
    {
        public PrototypeId ItemPrototype { get; protected set; }
        public long Count { get; protected set; }
        public MissionActionPrototype[] OnRemoveActions { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionInventoryRemoveItem(owner, this);
        }
    }

    public class MissionActionMetaStateWaveForcePrototype : MissionActionPrototype
    {
        public PrototypeId SetStateProto { get; protected set; }
        public PrototypeId WaveStateProto { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionMetaStateWaveForce(owner, this);
        }
    }

    public class MissionActionMissionActivatePrototype : MissionActionPrototype
    {
        public PrototypeId MissionPrototype { get; protected set; }
        public WeightedMissionEntryPrototype[] WeightedMissionPickList { get; protected set; }
        public bool WeightedMissionPickUseRegionSeed { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionMissionActivate(owner, this);
        }
    }

    public class MissionActionRegionShutdownPrototype : MissionActionPrototype
    {
        public PrototypeId RegionPrototype { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionRegionShutdown(owner, this);
        }
    }

    public class MissionActionResetAllMissionsPrototype : MissionActionPrototype
    {
        public PrototypeId MissionPrototype { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionResetAllMissions(owner, this);
        }
    }

    public class MissionActionTimedActionPrototype : MissionActionPrototype
    {
        public MissionActionPrototype[] ActionsToPerform { get; protected set; }
        public double DelayInSeconds { get; protected set; }
        public bool Repeat { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionTimedAction(owner, this);
        }

        public void SetDelayInSeconds(double delayInSeconds)
        {
            DelayInSeconds = delayInSeconds;
        }
    }

    public class MissionActionScoringEventTimerEndPrototype : MissionActionPrototype
    {
        public PrototypeId Timer { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionScoringEventTimerEnd(owner, this);
        }

    }

    public class MissionActionScoringEventTimerStartPrototype : MissionActionPrototype
    {
        public PrototypeId Timer { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionScoringEventTimerStart(owner, this);
        }
    }

    public class MissionActionScoringEventTimerStopPrototype : MissionActionPrototype
    {
        public PrototypeId Timer { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionScoringEventTimerStop(owner, this);
        }
    }

    public class MissionActionStoryNotificationPrototype : MissionActionPrototype
    {
        public StoryNotificationPrototype StoryNotification { get; protected set; }
        public DistributionType SendTo { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionStoryNotification(owner, this);
        }
    }

    public class MissionActionShowBannerMessagePrototype : MissionActionPrototype
    {
        public BannerMessagePrototype BannerMessage { get; protected set; }
        public DistributionType SendTo { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionShowBannerMessage(owner, this);
        }
    }

    public class MissionActionShowHUDTutorialPrototype : MissionActionPrototype
    {
        public HUDTutorialPrototype HUDTutorial { get; protected set; }
        public DistributionType SendTo { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionShowHUDTutorial(owner, this);
        }
    }

    public class MissionActionShowWaypointNotificationPrototype : MissionActionPrototype
    {
        public PrototypeId Waypoint { get; protected set; }
        public DistributionType SendTo { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionShowWaypointNotification(owner, this);
        }
    }

    public class MissionActionHideWaypointNotificationPrototype : MissionActionPrototype
    {
        public PrototypeId Waypoint { get; protected set; }
        public DistributionType SendTo { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionHideWaypointNotification(owner, this);
        }
    }

    public class MissionActionEnableRegionAvatarSwapPrototype : MissionActionPrototype
    {
        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionEnableRegionAvatarSwap(owner, this);
        }
    }

    public class MissionActionDisableRegionAvatarSwapPrototype : MissionActionPrototype
    {
        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionDisableRegionAvatarSwap(owner, this);
        }
    }

    public class MissionActionSwapAvatarPrototype : MissionActionPrototype
    {
        public PrototypeId AvatarPrototype { get; protected set; }
        public bool UseAvatarSwapPowers { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionSwapAvatar(owner, this);
        }
    }

    public class MissionActionEnableRegionRestrictedRosterPrototype : MissionActionPrototype
    {
        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionEnableRegionRestrictedRoster(owner, this);
        }
    }

    public class MissionActionDisableRegionRestrictedRosterPrototype : MissionActionPrototype
    {
        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionDisableRegionRestrictedRoster(owner, this);
        }
    }

    public class MissionActionUnlockUISystemPrototype : MissionActionPrototype
    {
        public AssetId UISystem { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionUnlockUISystem(owner, this);
        }
    }

    public class MissionActionShowMotionComicPrototype : MissionActionPrototype
    {
        public PrototypeId MotionComic { get; protected set; }
        public PrototypeId DownloadChunkOverride { get; protected set; }
        public DistributionType SendTo { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionShowMotionComic(owner, this);
        }
    }

    public class MissionActionUpdateMatchPrototype : MissionActionPrototype
    {
        public int MatchPhase { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionUpdateMatch(owner, this);
        }
    }

    public class MissionActionShowOverheadTextPrototype : MissionActionEntityTargetPrototype
    {
        public LocaleStringId DisplayText { get; protected set; }
        public int DurationMS { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionShowOverheadText(owner, this);
        }
    }

    public class MissionActionWaypointUnlockPrototype : MissionActionPrototype
    {
        public PrototypeId WaypointToUnlock { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionWaypointUnlock(owner, this);
        }
    }

    public class MissionActionWaypointLockPrototype : MissionActionPrototype
    {
        public PrototypeId WaypointToLock { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionWaypointLock(owner, this);
        }
    }

    public class MissionActionPlayBanterPrototype : MissionActionPrototype
    {
        public AssetId BanterAsset { get; protected set; }
        public DistributionType SendTo { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionPlayBanter(owner, this);
        }
    }

    public class MissionActionPlayKismetSeqPrototype : MissionActionPrototype
    {
        public PrototypeId KismetSeqPrototype { get; protected set; }
        public DistributionType SendTo { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionPlayKismetSeq(owner, this);
        }
    }

    public class MissionActionParticipantPerformPowerPrototype : MissionActionPrototype
    {
        public PrototypeId Power { get; protected set; }
        public DistributionType SendTo { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionParticipantPerformPower(owner, this);
        }
    }

    public class MissionActionOpenUIPanelPrototype : MissionActionPrototype
    {
        public AssetId PanelName { get; protected set; }
        public DistributionType SendTo { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionOpenUIPanel(owner, this);
        }
    }

    public class MissionActionPlayerTeleportPrototype : MissionActionPrototype
    {
        public PrototypeId TeleportRegionTarget { get; protected set; }
        public DistributionType SendTo { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionPlayerTeleport(owner, this);
        }
    }

    public class MissionActionRemoveConditionsKwdPrototype : MissionActionPrototype
    {
        public PrototypeId Keyword { get; protected set; }
        public DistributionType SendTo { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionRemoveConditionsKwd(owner, this);
        }
    }

    public class MissionActionEntSelEvtBroadcastPrototype : MissionActionEntityTargetPrototype
    {
        public EntitySelectorActionEventType EventToBroadcast { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionEntSelEvtBroadcast(owner, this);
        }
    }

    public class MissionActionAllianceSetPrototype : MissionActionEntityTargetPrototype
    {
        public PrototypeId Alliance { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionAllianceSet(owner, this);
        }
    }

    public class MissionActionShowTeamSelectDialogPrototype : MissionActionPrototype
    {
        public PrototypeId PublicEvent { get; protected set; }

        public override MissionAction AllocateAction(IMissionActionOwner owner)
        {
            return new MissionActionShowTeamSelectDialog(owner, this);
        }
    }
}
