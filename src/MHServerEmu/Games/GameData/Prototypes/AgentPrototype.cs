using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum Allegiance
    {
        None = 0,
        Hero = 1,
        Neutral = 2,
        Villain = 3,
    }

    [AssetEnum]
    public enum DramaticEntranceType
    {
        Always = 1,
        Once = 2,
        Never = 3,
    }

    #endregion

    public class AgentPrototype : WorldEntityPrototype
    {
        public Allegiance Allegiance { get; private set; }
        public LocomotorPrototype Locomotion { get; private set; }
        public ulong HitReactCondition { get; private set; }
        public BehaviorProfilePrototype BehaviorProfile { get; private set; }
        public PopulationInfoPrototype PopulationInfo { get; private set; }
        public int WakeDelayMS { get; private set; }
        public int WakeRandomStartMS { get; private set; }
        public float WakeRange { get; private set; }
        public float ReturnToDormantRange { get; private set; }
        public bool TriggersOcclusion { get; private set; }
        public int HitReactCooldownMS { get; private set; }
        public ulong BriefDescription { get; private set; }
        public float HealthBarRadius { get; private set; }
        public ulong OnResurrectedPower { get; private set; }
        public bool WakeStartsVisible { get; private set; }
        public VOStoryNotificationPrototype[] VOStoryNotifications { get; private set; }
        public bool HitReactOnClient { get; private set; }
        public ulong CCReactCondition { get; private set; }
        public int InCombatTimerMS { get; private set; }
        public DramaticEntranceType PlayDramaticEntrance { get; private set; }
        public ulong StealablePower { get; private set; }
        public ulong BossRewardIconPath { get; private set; }
        public bool SpawnLootForMissionContributors { get; private set; }
        public int InteractRangeThrow { get; private set; }
        public bool DamageMeterEnabled { get; private set; }
        public ulong MobHealthBaseCurveDCL { get; private set; }
    }

    public class OrbPrototype : AgentPrototype
    {
        public bool IgnoreRegionDifficultyForXPCalc { get; private set; }
        public bool XPAwardRestrictedToAvatar { get; private set; }
    }

    public class TeamUpCostumeOverridePrototype : Prototype
    {
        public ulong AvatarCostumeUnrealClass { get; private set; }
        public ulong TeamUpCostumeUnrealClass { get; private set; }
    }

    public class TeamUpStylePrototype : Prototype
    {
        public ulong Power { get; private set; }
        public bool PowerIsOnAvatarWhileAway { get; private set; }
        public bool PowerIsOnAvatarWhileSummoned { get; private set; }
        public bool IsPermanent { get; private set; }
    }

    public class ProgressionEntryPrototype : Prototype
    {
    }

    public class TeamUpPowerProgressionEntryPrototype : ProgressionEntryPrototype
    {
        public ulong Power { get; private set; }
        public bool IsPassiveOnAvatarWhileAway { get; private set; }
        public bool IsPassiveOnAvatarWhileSummoned { get; private set; }
        public ulong[] Antirequisites { get; private set; }
        public ulong[] Prerequisites { get; private set; }
        public ulong MaxRankForPowerAtCharacterLevel { get; private set; }
        public int RequiredLevel { get; private set; }
        public int StartingRank { get; private set; }
        public float UIPositionPctX { get; private set; }
        public float UIPositionPctY { get; private set; }
    }

    public class AgentTeamUpPrototype : AgentPrototype
    {
        public AvatarEquipInventoryAssignmentPrototype[] EquipmentInventories { get; private set; }
        public ulong PortraitPath { get; private set; }
        public ulong TooltipDescription { get; private set; }
        public TeamUpCostumeOverridePrototype[] CostumeUnrealOverrides { get; private set; }
        public ulong UnlockDialogImage { get; private set; }
        public ulong UnlockDialogText { get; private set; }
        public ulong FulfillmentName { get; private set; }
        public bool ShowInRosterIfLocked { get; private set; }
        public TeamUpStylePrototype[] Styles { get; private set; }
        public TeamUpPowerProgressionEntryPrototype[] PowerProgression { get; private set; }
        public int PowerProgressionVersion { get; private set; }
        public ulong PowerUIDefault { get; private set; }
    }
}
