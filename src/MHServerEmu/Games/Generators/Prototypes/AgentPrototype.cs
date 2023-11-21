using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class AgentPrototype : WorldEntityPrototype
    {
        public Allegiance Allegiance;
        public LocomotorPrototype Locomotion;
        public ulong HitReactCondition;
        public BehaviorProfilePrototype BehaviorProfile;
        public PopulationInfoPrototype PopulationInfo;
        public int WakeDelayMS;
        public int WakeRandomStartMS;
        public float WakeRange;
        public float ReturnToDormantRange;
        public bool TriggersOcclusion;
        public int HitReactCooldownMS;
        public ulong BriefDescription;
        public float HealthBarRadius;
        public ulong OnResurrectedPower;
        public bool WakeStartsVisible;
        public VOStoryNotificationPrototype[] VOStoryNotifications;
        public bool HitReactOnClient;
        public ulong CCReactCondition;
        public int InCombatTimerMS;
        public DramaticEntranceType PlayDramaticEntrance;
        public ulong StealablePower;
        public ulong BossRewardIconPath;
        public bool SpawnLootForMissionContributors;
        public int InteractRangeThrow;
        public bool DamageMeterEnabled;
        public ulong MobHealthBaseCurveDCL;
        public AgentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AgentPrototype), proto); }
    }

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

    public class OrbPrototype : AgentPrototype
    {
        public bool IgnoreRegionDifficultyForXPCalc;
        public bool XPAwardRestrictedToAvatar;
        public OrbPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(OrbPrototype), proto); }
    }

    public class TeamUpCostumeOverridePrototype : Prototype
    {
        public ulong AvatarCostumeUnrealClass;
        public ulong TeamUpCostumeUnrealClass;
        public TeamUpCostumeOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TeamUpCostumeOverridePrototype), proto); }
    }

    public class TeamUpStylePrototype : Prototype
    {
        public ulong Power;
        public bool PowerIsOnAvatarWhileAway;
        public bool PowerIsOnAvatarWhileSummoned;
        public bool IsPermanent;
        public TeamUpStylePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TeamUpStylePrototype), proto); }
    }

    public class ProgressionEntryPrototype : Prototype
    {
        public ProgressionEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProgressionEntryPrototype), proto); }
    }

    public class TeamUpPowerProgressionEntryPrototype : ProgressionEntryPrototype
    {
        public ulong Power;
        public bool IsPassiveOnAvatarWhileAway;
        public bool IsPassiveOnAvatarWhileSummoned;
        public ulong[] Antirequisites;
        public ulong[] Prerequisites;
        public ulong MaxRankForPowerAtCharacterLevel;
        public int RequiredLevel;
        public int StartingRank;
        public float UIPositionPctX;
        public float UIPositionPctY;
        public TeamUpPowerProgressionEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TeamUpPowerProgressionEntryPrototype), proto); }
    }

    public class AgentTeamUpPrototype : AgentPrototype
    {
        public AvatarEquipInventoryAssignmentPrototype[] EquipmentInventories;
        public ulong PortraitPath;
        public ulong TooltipDescription;
        public TeamUpCostumeOverridePrototype[] CostumeUnrealOverrides;
        public ulong UnlockDialogImage;
        public ulong UnlockDialogText;
        public ulong FulfillmentName;
        public bool ShowInRosterIfLocked;
        public TeamUpStylePrototype[] Styles;
        public TeamUpPowerProgressionEntryPrototype[] PowerProgression;
        public int PowerProgressionVersion;
        public ulong PowerUIDefault;
        public AgentTeamUpPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AgentTeamUpPrototype), proto); }
    }

}
