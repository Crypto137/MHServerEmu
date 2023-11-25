namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    public enum Allegiance
    {
        None = 0,
        Hero = 1,
        Neutral = 2,
        Villain = 3,
    }

    public enum DramaticEntranceType
    {
        Always = 1,
        Once = 2,
        Never = 3,
    }

    #endregion

    public class AgentPrototype : WorldEntityPrototype
    {
        public Allegiance Allegiance { get; set; }
        public LocomotorPrototype Locomotion { get; set; }
        public ulong HitReactCondition { get; set; }
        public BehaviorProfilePrototype BehaviorProfile { get; set; }
        public PopulationInfoPrototype PopulationInfo { get; set; }
        public int WakeDelayMS { get; set; }
        public int WakeRandomStartMS { get; set; }
        public float WakeRange { get; set; }
        public float ReturnToDormantRange { get; set; }
        public bool TriggersOcclusion { get; set; }
        public int HitReactCooldownMS { get; set; }
        public ulong BriefDescription { get; set; }
        public float HealthBarRadius { get; set; }
        public ulong OnResurrectedPower { get; set; }
        public bool WakeStartsVisible { get; set; }
        public VOStoryNotificationPrototype[] VOStoryNotifications { get; set; }
        public bool HitReactOnClient { get; set; }
        public ulong CCReactCondition { get; set; }
        public int InCombatTimerMS { get; set; }
        public DramaticEntranceType PlayDramaticEntrance { get; set; }
        public ulong StealablePower { get; set; }
        public ulong BossRewardIconPath { get; set; }
        public bool SpawnLootForMissionContributors { get; set; }
        public int InteractRangeThrow { get; set; }
        public bool DamageMeterEnabled { get; set; }
        public ulong MobHealthBaseCurveDCL { get; set; }
    }

    public class OrbPrototype : AgentPrototype
    {
        public bool IgnoreRegionDifficultyForXPCalc { get; set; }
        public bool XPAwardRestrictedToAvatar { get; set; }
    }

    public class TeamUpCostumeOverridePrototype : Prototype
    {
        public ulong AvatarCostumeUnrealClass { get; set; }
        public ulong TeamUpCostumeUnrealClass { get; set; }
    }

    public class TeamUpStylePrototype : Prototype
    {
        public ulong Power { get; set; }
        public bool PowerIsOnAvatarWhileAway { get; set; }
        public bool PowerIsOnAvatarWhileSummoned { get; set; }
        public bool IsPermanent { get; set; }
    }

    public class ProgressionEntryPrototype : Prototype
    {
    }

    public class TeamUpPowerProgressionEntryPrototype : ProgressionEntryPrototype
    {
        public ulong Power { get; set; }
        public bool IsPassiveOnAvatarWhileAway { get; set; }
        public bool IsPassiveOnAvatarWhileSummoned { get; set; }
        public ulong[] Antirequisites { get; set; }
        public ulong[] Prerequisites { get; set; }
        public ulong MaxRankForPowerAtCharacterLevel { get; set; }
        public int RequiredLevel { get; set; }
        public int StartingRank { get; set; }
        public float UIPositionPctX { get; set; }
        public float UIPositionPctY { get; set; }
    }

    public class AgentTeamUpPrototype : AgentPrototype
    {
        public AvatarEquipInventoryAssignmentPrototype[] EquipmentInventories { get; set; }
        public ulong PortraitPath { get; set; }
        public ulong TooltipDescription { get; set; }
        public TeamUpCostumeOverridePrototype[] CostumeUnrealOverrides { get; set; }
        public ulong UnlockDialogImage { get; set; }
        public ulong UnlockDialogText { get; set; }
        public ulong FulfillmentName { get; set; }
        public bool ShowInRosterIfLocked { get; set; }
        public TeamUpStylePrototype[] Styles { get; set; }
        public TeamUpPowerProgressionEntryPrototype[] PowerProgression { get; set; }
        public int PowerProgressionVersion { get; set; }
        public ulong PowerUIDefault { get; set; }
    }
}
