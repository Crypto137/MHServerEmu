namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    public enum AvatarStat
    {
        Durability = 1,
        Energy = 2,
        Fighting = 3,
        Intelligence = 4,
        Speed = 5,
        Strength = 6,
    }

    public enum AvatarMode
    {
        Normal = 0,
        Hardcore = 1,
        Ladder = 2,
    }

    #endregion

    public class AvatarPrototype : AgentPrototype
    {
        public ulong BioText { get; set; }
        public AbilityAssignmentPrototype[] HiddenPassivePowers { get; set; }
        public ulong PortraitPath { get; set; }
        public ulong StartingLootTable { get; set; }
        public ulong UnlockDialogImage { get; set; }
        public ulong HUDTheme { get; set; }
        public AvatarPrimaryStatPrototype[] PrimaryStats { get; set; }
        public PowerProgressionTablePrototype[] PowerProgressionTables { get; set; }
        public ItemAssignmentPrototype StartingCostume { get; set; }
        public ulong ResurrectOtherEntityPower { get; set; }
        public AvatarEquipInventoryAssignmentPrototype[] EquipmentInventories { get; set; }
        public ulong PartyBonusPower { get; set; }
        public ulong UnlockDialogText { get; set; }
        public ulong SecondaryResourceBehavior { get; set; }
        public ulong LoadingScreens { get; set; }
        public int PowerProgressionVersion { get; set; }
        public ulong OnLevelUpEval { get; set; }
        public EvalPrototype OnPartySizeChange { get; set; }
        public ulong StatsPower { get; set; }
        public ulong SocialIconPath { get; set; }
        public ulong CharacterSelectIconPath { get; set; }
        public ulong StatProgressionTable { get; set; }
        public TransformModeEntryPrototype[] TransformModes { get; set; }
        public AvatarSynergyEntryPrototype[] SynergyTable { get; set; }
        public ulong[] SuperteamMemberships { get; set; }
        public ulong CharacterSelectPowers { get; set; }
        public PrimaryResourceManaBehaviorPrototype PrimaryResourceBehaviors { get; set; }
        public StealablePowerInfoPrototype StealablePowersAllowed { get; set; }
        public bool ShowInRosterIfLocked { get; set; }
        public ulong CharacterVideoUrl { get; set; }
        public ulong CharacterSelectIconPortraitSmall { get; set; }
        public ulong CharacterSelectIconPortraitFull { get; set; }
        public ulong PrimaryResourceBehaviorNames { get; set; }
        public bool IsStarterAvatar { get; set; }
        public int CharacterSelectDisplayOrder { get; set; }
        public ulong CostumeCore { get; set; }
        public TalentGroupPrototype[] TalentGroups { get; set; }
        public ulong TravelPower { get; set; }
        public AbilityAutoAssignmentSlotPrototype[] AbilityAutoAssignmentSlot { get; set; }
        public ulong LoadingScreensConsole { get; set; }
        public ItemAssignmentPrototype StartingCostumePS4 { get; set; }
        public ItemAssignmentPrototype StartingCostumeXboxOne { get; set; }
    }

    public class ItemAssignmentPrototype : Prototype
    {
        public ulong Item { get; set; }
        public ulong Rarity { get; set; }
    }

    public class AvatarPrimaryStatPrototype : Prototype
    {
        public AvatarStat Stat { get; set; }
        public ulong Tooltip { get; set; }
    }

    public class IngredientLookupEntryPrototype : Prototype
    {
        public long LookupSlot { get; set; }
        public ulong Ingredient { get; set; }
    }

    public class AvatarSynergyEntryPrototype : Prototype
    {
        public int Level { get; set; }
        public ulong TooltipTextForIcon { get; set; }
        public ulong UIData { get; set; }
    }

    public class AvatarSynergyEvalEntryPrototype : AvatarSynergyEntryPrototype
    {
        public EvalPrototype SynergyEval { get; set; }
    }

    public class VanityTitlePrototype : Prototype
    {
        public ulong Text { get; set; }
    }

    public class PowerSpecPrototype : Prototype
    {
        public int Index { get; set; }
    }

    public class TalentEntryPrototype : Prototype
    {
        public ulong Talent { get; set; }
        public int UnlockLevel { get; set; }
    }

    public class TalentGroupPrototype : Prototype
    {
        public TalentEntryPrototype[] Talents { get; set; }
        public float UIPositionPctX { get; set; }
        public float UIPositionPctY { get; set; }
    }

    public class AvatarModePrototype : Prototype
    {
        public AvatarMode AvatarModeEnum { get; set; }
        public ConvenienceLabel Inventory { get; set; }
    }

    public class StatProgressionEntryPrototype : Prototype
    {
        public int Level { get; set; }
        public int DurabilityValue { get; set; }
        public int EnergyProjectionValue { get; set; }
        public int FightingSkillsValue { get; set; }
        public int IntelligenceValue { get; set; }
        public int SpeedValue { get; set; }
        public int StrengthValue { get; set; }
    }

    public class PowerProgressionEntryPrototype : ProgressionEntryPrototype
    {
        public int Level { get; set; }
        public AbilityAssignmentPrototype PowerAssignment { get; set; }
        public ulong MaxRankForPowerAtCharacterLevel { get; set; }
        public ulong[] Prerequisites { get; set; }
        public float UIPositionPctX { get; set; }
        public float UIPositionPctY { get; set; }
        public int UIFanSortNumber { get; set; }
        public int UIFanTier { get; set; }
        public ulong[] Antirequisites { get; set; }
        public bool IsTrait { get; set; }
    }

    public class PowerProgressionTablePrototype : Prototype
    {
        public ulong DisplayName { get; set; }
        public PowerProgressionEntryPrototype[] PowerProgressionEntries { get; set; }
    }

    public class PowerProgTableTabRefPrototype : Prototype
    {
        public int PowerProgTableTabIndex { get; set; }
    }
}
