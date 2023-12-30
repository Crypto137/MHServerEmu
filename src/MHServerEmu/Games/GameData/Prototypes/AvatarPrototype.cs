using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum AvatarStat
    {
        Durability = 1,
        Energy = 2,
        Fighting = 3,
        Intelligence = 4,
        Speed = 5,
        Strength = 6,
    }

    [AssetEnum]
    public enum AvatarMode
    {
        Normal = 0,
        Hardcore = 1,
        Ladder = 2,
    }

    #endregion

    public class AvatarPrototype : AgentPrototype
    {
        public ulong BioText { get; protected set; }
        public AbilityAssignmentPrototype[] HiddenPassivePowers { get; protected set; }
        public ulong PortraitPath { get; protected set; }
        public ulong StartingLootTable { get; protected set; }
        public ulong UnlockDialogImage { get; protected set; }
        public ulong HUDTheme { get; protected set; }
        public AvatarPrimaryStatPrototype[] PrimaryStats { get; protected set; }
        public PowerProgressionTablePrototype[] PowerProgressionTables { get; protected set; }
        public ItemAssignmentPrototype StartingCostume { get; protected set; }
        public ulong ResurrectOtherEntityPower { get; protected set; }
        public AvatarEquipInventoryAssignmentPrototype[] EquipmentInventories { get; protected set; }
        public ulong PartyBonusPower { get; protected set; }
        public ulong UnlockDialogText { get; protected set; }
        public ulong SecondaryResourceBehavior { get; protected set; }
        public ulong LoadingScreens { get; protected set; }
        public int PowerProgressionVersion { get; protected set; }
        public ulong OnLevelUpEval { get; protected set; }
        public EvalPrototype OnPartySizeChange { get; protected set; }
        public ulong StatsPower { get; protected set; }
        public ulong SocialIconPath { get; protected set; }
        public ulong CharacterSelectIconPath { get; protected set; }
        public ulong StatProgressionTable { get; protected set; }
        public TransformModeEntryPrototype[] TransformModes { get; protected set; }
        public AvatarSynergyEntryPrototype[] SynergyTable { get; protected set; }
        public ulong[] SuperteamMemberships { get; protected set; }
        public ulong CharacterSelectPowers { get; protected set; }
        public PrimaryResourceManaBehaviorPrototype PrimaryResourceBehaviors { get; protected set; }
        public StealablePowerInfoPrototype StealablePowersAllowed { get; protected set; }
        public bool ShowInRosterIfLocked { get; protected set; }
        public ulong CharacterVideoUrl { get; protected set; }
        public ulong CharacterSelectIconPortraitSmall { get; protected set; }
        public ulong CharacterSelectIconPortraitFull { get; protected set; }
        public ulong PrimaryResourceBehaviorNames { get; protected set; }
        public bool IsStarterAvatar { get; protected set; }
        public int CharacterSelectDisplayOrder { get; protected set; }
        public ulong CostumeCore { get; protected set; }
        public TalentGroupPrototype[] TalentGroups { get; protected set; }
        public ulong TravelPower { get; protected set; }
        public AbilityAutoAssignmentSlotPrototype[] AbilityAutoAssignmentSlot { get; protected set; }
        public ulong LoadingScreensConsole { get; protected set; }
        public ItemAssignmentPrototype StartingCostumePS4 { get; protected set; }
        public ItemAssignmentPrototype StartingCostumeXboxOne { get; protected set; }
    }

    public class ItemAssignmentPrototype : Prototype
    {
        public ulong Item { get; protected set; }
        public ulong Rarity { get; protected set; }
    }

    public class AvatarPrimaryStatPrototype : Prototype
    {
        public AvatarStat Stat { get; protected set; }
        public ulong Tooltip { get; protected set; }
    }

    public class IngredientLookupEntryPrototype : Prototype
    {
        public long LookupSlot { get; protected set; }
        public ulong Ingredient { get; protected set; }
    }

    public class AvatarSynergyEntryPrototype : Prototype
    {
        public int Level { get; protected set; }
        public ulong TooltipTextForIcon { get; protected set; }
        public ulong UIData { get; protected set; }
    }

    public class AvatarSynergyEvalEntryPrototype : AvatarSynergyEntryPrototype
    {
        public EvalPrototype SynergyEval { get; protected set; }
    }

    public class VanityTitlePrototype : Prototype
    {
        public ulong Text { get; protected set; }
    }

    public class PowerSpecPrototype : Prototype
    {
        public int Index { get; protected set; }
    }

    public class TalentEntryPrototype : Prototype
    {
        public ulong Talent { get; protected set; }
        public int UnlockLevel { get; protected set; }
    }

    public class TalentGroupPrototype : Prototype
    {
        public TalentEntryPrototype[] Talents { get; protected set; }
        public float UIPositionPctX { get; protected set; }
        public float UIPositionPctY { get; protected set; }
    }

    public class AvatarModePrototype : Prototype
    {
        public AvatarMode AvatarModeEnum { get; protected set; }
        public ConvenienceLabel Inventory { get; protected set; }
    }

    public class StatProgressionEntryPrototype : Prototype
    {
        public int Level { get; protected set; }
        public int DurabilityValue { get; protected set; }
        public int EnergyProjectionValue { get; protected set; }
        public int FightingSkillsValue { get; protected set; }
        public int IntelligenceValue { get; protected set; }
        public int SpeedValue { get; protected set; }
        public int StrengthValue { get; protected set; }
    }

    public class PowerProgressionEntryPrototype : ProgressionEntryPrototype
    {
        public int Level { get; protected set; }
        public AbilityAssignmentPrototype PowerAssignment { get; protected set; }
        public ulong MaxRankForPowerAtCharacterLevel { get; protected set; }
        public ulong[] Prerequisites { get; protected set; }
        public float UIPositionPctX { get; protected set; }
        public float UIPositionPctY { get; protected set; }
        public int UIFanSortNumber { get; protected set; }
        public int UIFanTier { get; protected set; }
        public ulong[] Antirequisites { get; protected set; }
        public bool IsTrait { get; protected set; }
    }

    public class PowerProgressionTablePrototype : Prototype
    {
        public ulong DisplayName { get; protected set; }
        public PowerProgressionEntryPrototype[] PowerProgressionEntries { get; protected set; }
    }

    public class PowerProgTableTabRefPrototype : Prototype
    {
        public int PowerProgTableTabIndex { get; protected set; }
    }
}
