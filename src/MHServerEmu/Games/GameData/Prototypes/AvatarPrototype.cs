namespace MHServerEmu.Games.GameData.Prototypes
{
    public class AvatarPrototype : AgentPrototype
    {
        public ulong BioText;
        public AbilityAssignmentPrototype[] HiddenPassivePowers;
        public ulong PortraitPath;
        public ulong StartingLootTable;
        public ulong UnlockDialogImage;
        public ulong HUDTheme;
        public AvatarPrimaryStatPrototype[] PrimaryStats;
        public PowerProgressionTablePrototype[] PowerProgressionTables;
        public ItemAssignmentPrototype StartingCostume;
        public ulong ResurrectOtherEntityPower;
        public AvatarEquipInventoryAssignmentPrototype[] EquipmentInventories;
        public ulong PartyBonusPower;
        public ulong UnlockDialogText;
        public ulong SecondaryResourceBehavior;
        public ulong[] LoadingScreens;
        public int PowerProgressionVersion;
        public ulong OnLevelUpEval;
        public EvalPrototype OnPartySizeChange;
        public ulong StatsPower;
        public ulong SocialIconPath;
        public ulong CharacterSelectIconPath;
        public ulong[] StatProgressionTable;
        public TransformModeEntryPrototype[] TransformModes;
        public AvatarSynergyEntryPrototype[] SynergyTable;
        public ulong[] SuperteamMemberships;
        public ulong[] CharacterSelectPowers;
        public PrimaryResourceManaBehaviorPrototype PrimaryResourceBehaviors;
        public StealablePowerInfoPrototype StealablePowersAllowed;
        public bool ShowInRosterIfLocked;
        public ulong CharacterVideoUrl;
        public ulong CharacterSelectIconPortraitSmall;
        public ulong CharacterSelectIconPortraitFull;
        public ulong PrimaryResourceBehaviorNames;
        public bool IsStarterAvatar;
        public int CharacterSelectDisplayOrder;
        public ulong CostumeCore;
        public TalentGroupPrototype[] TalentGroups;
        public ulong TravelPower;
        public AbilityAutoAssignmentSlotPrototype[] AbilityAutoAssignmentSlot;
        public ulong[] LoadingScreensConsole;
        public ItemAssignmentPrototype StartingCostumePS4;
        public ItemAssignmentPrototype StartingCostumeXboxOne;
        public AvatarPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AvatarPrototype), proto); }
    }

    public class ItemAssignmentPrototype : Prototype
    {
        public ulong Item;
        public ulong Rarity;
        public ItemAssignmentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemAssignmentPrototype), proto); }
    }

    public class AvatarPrimaryStatPrototype : Prototype
    {
        public AvatarStat Stat;
        public ulong Tooltip;
        public AvatarPrimaryStatPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AvatarPrimaryStatPrototype), proto); }
    }
    public enum AvatarStat
    {
        Durability = 1,
        Energy = 2,
        Fighting = 3,
        Intelligence = 4,
        Speed = 5,
        Strength = 6,
    }

    public class IngredientLookupEntryPrototype : Prototype
    {
        public long LookupSlot;
        public ulong Ingredient;
        public IngredientLookupEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(IngredientLookupEntryPrototype), proto); }
    }

    public class AvatarSynergyEntryPrototype : Prototype
    {
        public int Level;
        public ulong TooltipTextForIcon;
        public ulong UIData;
        public AvatarSynergyEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AvatarSynergyEntryPrototype), proto); }
    }

    public class AvatarSynergyEvalEntryPrototype : AvatarSynergyEntryPrototype
    {
        public EvalPrototype SynergyEval;
        public AvatarSynergyEvalEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AvatarSynergyEvalEntryPrototype), proto); }
    }

    public class VanityTitlePrototype : Prototype
    {
        public ulong Text;
        public VanityTitlePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(VanityTitlePrototype), proto); }
    }

    public class PowerSpecPrototype : Prototype
    {
        public int Index;
        public PowerSpecPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerSpecPrototype), proto); }
    }

    public class TalentEntryPrototype : Prototype
    {
        public ulong Talent;
        public int UnlockLevel;
        public TalentEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TalentEntryPrototype), proto); }
    }

    public class TalentGroupPrototype : Prototype
    {
        public TalentEntryPrototype[] Talents;
        public float UIPositionPctX;
        public float UIPositionPctY;
        public TalentGroupPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TalentGroupPrototype), proto); }
    }

    public class AvatarModePrototype : Prototype
    {
        public AvatarMode AvatarModeEnum;
        public ConvenienceLabel Inventory;
        public AvatarModePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AvatarModePrototype), proto); }
    }

    public enum AvatarMode
    {
        Normal = 0,
        Hardcore = 1,
        Ladder = 2,
    }

    public class StatProgressionEntryPrototype : Prototype
    {
        public int Level;
        public int DurabilityValue;
        public int EnergyProjectionValue;
        public int FightingSkillsValue;
        public int IntelligenceValue;
        public int SpeedValue;
        public int StrengthValue;
        public StatProgressionEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(StatProgressionEntryPrototype), proto); }
    }

    public class PowerProgressionEntryPrototype : ProgressionEntryPrototype
    {
        public int Level;
        public AbilityAssignmentPrototype PowerAssignment;
        public ulong MaxRankForPowerAtCharacterLevel;
        public ulong[] Prerequisites;
        public float UIPositionPctX;
        public float UIPositionPctY;
        public int UIFanSortNumber;
        public int UIFanTier;
        public ulong[] Antirequisites;
        public bool IsTrait;
        public PowerProgressionEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerProgressionEntryPrototype), proto); }
    }

    public class PowerProgressionTablePrototype : Prototype
    {
        public ulong DisplayName;
        public PowerProgressionEntryPrototype[] PowerProgressionEntries;
        public PowerProgressionTablePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerProgressionTablePrototype), proto); }
    }

    public class PowerProgTableTabRefPrototype : Prototype
    {
        public int PowerProgTableTabIndex;
        public PowerProgTableTabRefPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerProgTableTabRefPrototype), proto); }
    }
}
