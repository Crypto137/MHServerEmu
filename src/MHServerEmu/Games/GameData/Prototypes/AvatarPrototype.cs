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
        public ulong BioText { get; private set; }
        public AbilityAssignmentPrototype[] HiddenPassivePowers { get; private set; }
        public ulong PortraitPath { get; private set; }
        public ulong StartingLootTable { get; private set; }
        public ulong UnlockDialogImage { get; private set; }
        public ulong HUDTheme { get; private set; }
        public AvatarPrimaryStatPrototype[] PrimaryStats { get; private set; }
        public PowerProgressionTablePrototype[] PowerProgressionTables { get; private set; }
        public ItemAssignmentPrototype StartingCostume { get; private set; }
        public ulong ResurrectOtherEntityPower { get; private set; }
        public AvatarEquipInventoryAssignmentPrototype[] EquipmentInventories { get; private set; }
        public ulong PartyBonusPower { get; private set; }
        public ulong UnlockDialogText { get; private set; }
        public ulong SecondaryResourceBehavior { get; private set; }
        public ulong LoadingScreens { get; private set; }
        public int PowerProgressionVersion { get; private set; }
        public ulong OnLevelUpEval { get; private set; }
        public EvalPrototype OnPartySizeChange { get; private set; }
        public ulong StatsPower { get; private set; }
        public ulong SocialIconPath { get; private set; }
        public ulong CharacterSelectIconPath { get; private set; }
        public ulong StatProgressionTable { get; private set; }
        public TransformModeEntryPrototype[] TransformModes { get; private set; }
        public AvatarSynergyEntryPrototype[] SynergyTable { get; private set; }
        public ulong[] SuperteamMemberships { get; private set; }
        public ulong CharacterSelectPowers { get; private set; }
        public PrimaryResourceManaBehaviorPrototype PrimaryResourceBehaviors { get; private set; }
        public StealablePowerInfoPrototype StealablePowersAllowed { get; private set; }
        public bool ShowInRosterIfLocked { get; private set; }
        public ulong CharacterVideoUrl { get; private set; }
        public ulong CharacterSelectIconPortraitSmall { get; private set; }
        public ulong CharacterSelectIconPortraitFull { get; private set; }
        public ulong PrimaryResourceBehaviorNames { get; private set; }
        public bool IsStarterAvatar { get; private set; }
        public int CharacterSelectDisplayOrder { get; private set; }
        public ulong CostumeCore { get; private set; }
        public TalentGroupPrototype[] TalentGroups { get; private set; }
        public ulong TravelPower { get; private set; }
        public AbilityAutoAssignmentSlotPrototype[] AbilityAutoAssignmentSlot { get; private set; }
        public ulong LoadingScreensConsole { get; private set; }
        public ItemAssignmentPrototype StartingCostumePS4 { get; private set; }
        public ItemAssignmentPrototype StartingCostumeXboxOne { get; private set; }
    }

    public class ItemAssignmentPrototype : Prototype
    {
        public ulong Item { get; private set; }
        public ulong Rarity { get; private set; }
    }

    public class AvatarPrimaryStatPrototype : Prototype
    {
        public AvatarStat Stat { get; private set; }
        public ulong Tooltip { get; private set; }
    }

    public class IngredientLookupEntryPrototype : Prototype
    {
        public long LookupSlot { get; private set; }
        public ulong Ingredient { get; private set; }
    }

    public class AvatarSynergyEntryPrototype : Prototype
    {
        public int Level { get; private set; }
        public ulong TooltipTextForIcon { get; private set; }
        public ulong UIData { get; private set; }
    }

    public class AvatarSynergyEvalEntryPrototype : AvatarSynergyEntryPrototype
    {
        public EvalPrototype SynergyEval { get; private set; }
    }

    public class VanityTitlePrototype : Prototype
    {
        public ulong Text { get; private set; }
    }

    public class PowerSpecPrototype : Prototype
    {
        public int Index { get; private set; }
    }

    public class TalentEntryPrototype : Prototype
    {
        public ulong Talent { get; private set; }
        public int UnlockLevel { get; private set; }
    }

    public class TalentGroupPrototype : Prototype
    {
        public TalentEntryPrototype[] Talents { get; private set; }
        public float UIPositionPctX { get; private set; }
        public float UIPositionPctY { get; private set; }
    }

    public class AvatarModePrototype : Prototype
    {
        public AvatarMode AvatarModeEnum { get; private set; }
        public ConvenienceLabel Inventory { get; private set; }
    }

    public class StatProgressionEntryPrototype : Prototype
    {
        public int Level { get; private set; }
        public int DurabilityValue { get; private set; }
        public int EnergyProjectionValue { get; private set; }
        public int FightingSkillsValue { get; private set; }
        public int IntelligenceValue { get; private set; }
        public int SpeedValue { get; private set; }
        public int StrengthValue { get; private set; }
    }

    public class PowerProgressionEntryPrototype : ProgressionEntryPrototype
    {
        public int Level { get; private set; }
        public AbilityAssignmentPrototype PowerAssignment { get; private set; }
        public ulong MaxRankForPowerAtCharacterLevel { get; private set; }
        public ulong[] Prerequisites { get; private set; }
        public float UIPositionPctX { get; private set; }
        public float UIPositionPctY { get; private set; }
        public int UIFanSortNumber { get; private set; }
        public int UIFanTier { get; private set; }
        public ulong[] Antirequisites { get; private set; }
        public bool IsTrait { get; private set; }
    }

    public class PowerProgressionTablePrototype : Prototype
    {
        public ulong DisplayName { get; private set; }
        public PowerProgressionEntryPrototype[] PowerProgressionEntries { get; private set; }
    }

    public class PowerProgTableTabRefPrototype : Prototype
    {
        public int PowerProgTableTabIndex { get; private set; }
    }
}
