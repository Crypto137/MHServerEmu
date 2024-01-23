using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)None)]
    public enum Allegiance
    {
        None = 0,
        Hero = 1,
        Neutral = 2,
        Villain = 3,
    }

    [AssetEnum((int)None)]
    public enum DramaticEntranceType
    {
        None = 0,
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
        public PrototypeId HitReactCondition { get; protected set; }
        public BehaviorProfilePrototype BehaviorProfile { get; protected set; }
        [Mixin]
        public PopulationInfoPrototype PopulationInfo { get; protected set; }   // This does not seem to be actually used anywhere
        public int WakeDelayMS { get; protected set; }
        public int WakeRandomStartMS { get; protected set; }
        public float WakeRange { get; protected set; }
        public float ReturnToDormantRange { get; protected set; }
        public bool TriggersOcclusion { get; protected set; }
        public int HitReactCooldownMS { get; protected set; }
        public LocaleStringId BriefDescription { get; protected set; }
        public float HealthBarRadius { get; protected set; }
        public PrototypeId OnResurrectedPower { get; protected set; }
        public bool WakeStartsVisible { get; protected set; }
        public VOStoryNotificationPrototype[] VOStoryNotifications { get; protected set; }
        public bool HitReactOnClient { get; protected set; }
        public PrototypeId CCReactCondition { get; protected set; }
        public int InCombatTimerMS { get; protected set; }
        public DramaticEntranceType PlayDramaticEntrance { get; protected set; }
        public PrototypeId StealablePower { get; protected set; }
        public StringId BossRewardIconPath { get; protected set; }
        public bool SpawnLootForMissionContributors { get; protected set; }
        public int InteractRangeThrow { get; protected set; }
        public bool DamageMeterEnabled { get; protected set; }
        public CurveId MobHealthBaseCurveDCL { get; protected set; }
    }

    public class OrbPrototype : AgentPrototype
    {
        public bool IgnoreRegionDifficultyForXPCalc { get; protected set; }
        public bool XPAwardRestrictedToAvatar { get; protected set; }
    }

    public class TeamUpCostumeOverridePrototype : Prototype
    {
        public StringId AvatarCostumeUnrealClass { get; protected set; }
        public StringId TeamUpCostumeUnrealClass { get; protected set; }
    }

    public class TeamUpStylePrototype : Prototype
    {
        public PrototypeId Power { get; protected set; }
        public bool PowerIsOnAvatarWhileAway { get; protected set; }
        public bool PowerIsOnAvatarWhileSummoned { get; protected set; }
        public bool IsPermanent { get; protected set; }
    }

    public class ProgressionEntryPrototype : Prototype
    {
    }

    public class TeamUpPowerProgressionEntryPrototype : ProgressionEntryPrototype
    {
        public PrototypeId Power { get; protected set; }
        public bool IsPassiveOnAvatarWhileAway { get; protected set; }
        public bool IsPassiveOnAvatarWhileSummoned { get; protected set; }
        public PrototypeId[] Antirequisites { get; protected set; }
        public PrototypeId[] Prerequisites { get; protected set; }
        public CurveId MaxRankForPowerAtCharacterLevel { get; protected set; }
        public int RequiredLevel { get; protected set; }
        public int StartingRank { get; protected set; }
        public float UIPositionPctX { get; protected set; }
        public float UIPositionPctY { get; protected set; }
    }

    public class AgentTeamUpPrototype : AgentPrototype
    {
        public AvatarEquipInventoryAssignmentPrototype[] EquipmentInventories { get; protected set; }
        public StringId PortraitPath { get; protected set; }
        public LocaleStringId TooltipDescription { get; protected set; }
        public TeamUpCostumeOverridePrototype[] CostumeUnrealOverrides { get; protected set; }
        public StringId UnlockDialogImage { get; protected set; }
        public LocaleStringId UnlockDialogText { get; protected set; }
        public LocaleStringId FulfillmentName { get; protected set; }
        public bool ShowInRosterIfLocked { get; protected set; }
        public TeamUpStylePrototype[] Styles { get; protected set; }
        public TeamUpPowerProgressionEntryPrototype[] PowerProgression { get; protected set; }
        public int PowerProgressionVersion { get; protected set; }
        public PrototypeId PowerUIDefault { get; protected set; }
    }
}
