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
        public Allegiance Allegiance { get; protected set; }
        [Mixin]
        public LocomotorPrototype Locomotion { get; protected set; }
        public ulong HitReactCondition { get; protected set; }
        public BehaviorProfilePrototype BehaviorProfile { get; protected set; }
        [Mixin]
        public PopulationInfoPrototype PopulationInfo { get; protected set; }   // This does not seem to be actually used anywhere
        public int WakeDelayMS { get; protected set; }
        public int WakeRandomStartMS { get; protected set; }
        public float WakeRange { get; protected set; }
        public float ReturnToDormantRange { get; protected set; }
        public bool TriggersOcclusion { get; protected set; }
        public int HitReactCooldownMS { get; protected set; }
        public ulong BriefDescription { get; protected set; }
        public float HealthBarRadius { get; protected set; }
        public ulong OnResurrectedPower { get; protected set; }
        public bool WakeStartsVisible { get; protected set; }
        public VOStoryNotificationPrototype[] VOStoryNotifications { get; protected set; }
        public bool HitReactOnClient { get; protected set; }
        public ulong CCReactCondition { get; protected set; }
        public int InCombatTimerMS { get; protected set; }
        public DramaticEntranceType PlayDramaticEntrance { get; protected set; }
        public ulong StealablePower { get; protected set; }
        public ulong BossRewardIconPath { get; protected set; }
        public bool SpawnLootForMissionContributors { get; protected set; }
        public int InteractRangeThrow { get; protected set; }
        public bool DamageMeterEnabled { get; protected set; }
        public ulong MobHealthBaseCurveDCL { get; protected set; }
    }

    public class OrbPrototype : AgentPrototype
    {
        public bool IgnoreRegionDifficultyForXPCalc { get; protected set; }
        public bool XPAwardRestrictedToAvatar { get; protected set; }
    }

    public class TeamUpCostumeOverridePrototype : Prototype
    {
        public ulong AvatarCostumeUnrealClass { get; protected set; }
        public ulong TeamUpCostumeUnrealClass { get; protected set; }
    }

    public class TeamUpStylePrototype : Prototype
    {
        public ulong Power { get; protected set; }
        public bool PowerIsOnAvatarWhileAway { get; protected set; }
        public bool PowerIsOnAvatarWhileSummoned { get; protected set; }
        public bool IsPermanent { get; protected set; }
    }

    public class ProgressionEntryPrototype : Prototype
    {
    }

    public class TeamUpPowerProgressionEntryPrototype : ProgressionEntryPrototype
    {
        public ulong Power { get; protected set; }
        public bool IsPassiveOnAvatarWhileAway { get; protected set; }
        public bool IsPassiveOnAvatarWhileSummoned { get; protected set; }
        public ulong[] Antirequisites { get; protected set; }
        public ulong[] Prerequisites { get; protected set; }
        public ulong MaxRankForPowerAtCharacterLevel { get; protected set; }
        public int RequiredLevel { get; protected set; }
        public int StartingRank { get; protected set; }
        public float UIPositionPctX { get; protected set; }
        public float UIPositionPctY { get; protected set; }
    }

    public class AgentTeamUpPrototype : AgentPrototype
    {
        public AvatarEquipInventoryAssignmentPrototype[] EquipmentInventories { get; protected set; }
        public ulong PortraitPath { get; protected set; }
        public ulong TooltipDescription { get; protected set; }
        public TeamUpCostumeOverridePrototype[] CostumeUnrealOverrides { get; protected set; }
        public ulong UnlockDialogImage { get; protected set; }
        public ulong UnlockDialogText { get; protected set; }
        public ulong FulfillmentName { get; protected set; }
        public bool ShowInRosterIfLocked { get; protected set; }
        public TeamUpStylePrototype[] Styles { get; protected set; }
        public TeamUpPowerProgressionEntryPrototype[] PowerProgression { get; protected set; }
        public int PowerProgressionVersion { get; protected set; }
        public ulong PowerUIDefault { get; protected set; }
    }
}
