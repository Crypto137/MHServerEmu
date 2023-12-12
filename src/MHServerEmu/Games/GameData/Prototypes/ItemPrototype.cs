using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ItemPrototype : WorldEntityPrototype
    {
        public bool IsUsable;
        public bool CanBeSoldToVendor;
        public int MaxVisiblePrefixes;
        public int MaxVisibleSuffixes;
        public ulong TooltipDescription;
        public ulong TooltipFlavorText;
        public ulong TooltipTemplate;
        public ItemStackSettingsPrototype StackSettings;
        public bool AlwaysDisplayAsUsable;
        public ulong TooltipEquipRestrictions;
        public AffixEntryPrototype[] AffixesBuiltIn;
        public PropertyEntryPrototype[] PropertiesBuiltIn;
        public ProductPrototype Product;
        public ulong ItemCategory;
        public ulong ItemSubcategory;
        public bool IsAvatarRestricted;
        public DropRestrictionPrototype[] LootDropRestrictions;
        public ItemBindingSettingsPrototype BindingSettings;
        public AffixLimitsPrototype[] AffixLimits;
        public ulong TextStyleOverride;
        public ItemAbilitySettingsPrototype AbilitySettings;
        public ulong StoreIconPath;
        public bool ClonedWhenPurchasedFromVendor;
        public ItemActionSetPrototype ActionsTriggeredOnItemEvent;
        public bool ConfirmOnDonate;
        public bool CanBeDestroyed;
        public bool ConfirmPurchase;
        public ItemCostPrototype Cost;
        public int TooltipDepthOverride;
        public EquipRestrictionPrototype[] EquipRestrictions;
        public EvalPrototype EvalExpirationTimeMS;
        public ItemTooltipPropertyBlockSettingsPrototype[] TooltipCustomPropertyBlocks;
        public float LootDropWeightMultiplier;
        public ConvenienceLabel DestinationFromVendor;
        public EvalPrototype EvalDisplayLevel;
        public bool CanBroadcast;
        public EquipmentInvUISlot DefaultEquipmentSlot;
        public EvalPrototype EvalCanUse;
        public ulong[] CannotEquipWithItemsOfKeyword;
        public ulong SortCategory;
        public ulong SortSubCategory;
        public ItemInstrumentedDropGroup InstrumentedDropGroup;
        public bool IsContainer;
        public ItemPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemPrototype), proto); }
    }
    public enum ItemInstrumentedDropGroup
    {
        Character = 1,
        Costume = 2,
        RareArtifact = 3,
    }

    public class ItemAbilitySettingsPrototype : Prototype
    {
        public AbilitySlotRestrictionPrototype AbilitySlotRestriction;
        public bool OnlySlottableWhileEquipped;
        public ItemAbilitySettingsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemAbilitySettingsPrototype), proto); }
    }

    public class ItemBindingSettingsEntryPrototype : Prototype
    {
        public bool BindsToAccountOnPickup;
        public bool BindsToCharacterOnEquip;
        public bool IsTradable;
        public ulong RarityFilter;
        public ItemBindingSettingsEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemBindingSettingsEntryPrototype), proto); }
    }

    public class ItemBindingSettingsPrototype : Prototype
    {
        public ItemBindingSettingsEntryPrototype DefaultSettings;
        public ItemBindingSettingsEntryPrototype[] PerRaritySettings;
        public ItemBindingSettingsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemBindingSettingsPrototype), proto); }
    }

    public class ItemStackSettingsPrototype : Prototype
    {
        public int ItemLevelOverride;
        public int MaxStacks;
        public int RequiredCharLevelOverride;
        public bool AutoStackWhenAddedToInventory;
        public bool StacksCanBeSplit;
        public ItemStackSettingsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemStackSettingsPrototype), proto); }
    }

    public class ItemActionBasePrototype : Prototype
    {
        public int Weight;
        public ItemActionBasePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemActionBasePrototype), proto); }
    }

    public class ItemActionPrototype : ItemActionBasePrototype
    {
        public ItemEventType TriggeringEvent;
        public ItemActionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemActionPrototype), proto); }
    }
    public enum ItemEventType
    {
        None = 0,
        OnEquip = 1,
        OnUse = 2,
        OnUsePowerActivated = 3,
    }

    public class ItemActionAssignPowerPrototype : ItemActionPrototype
    {
        public ulong Power;
        public ItemActionAssignPowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemActionAssignPowerPrototype), proto); }
    }

    public class ItemActionDestroySelfPrototype : ItemActionPrototype
    {
        public ItemActionDestroySelfPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemActionDestroySelfPrototype), proto); }
    }

    public class ItemActionGuildsUnlockPrototype : ItemActionPrototype
    {
        public ItemActionGuildsUnlockPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemActionGuildsUnlockPrototype), proto); }
    }

    public class ItemActionReplaceSelfItemPrototype : ItemActionPrototype
    {
        public ulong Item;
        public ItemActionReplaceSelfItemPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemActionReplaceSelfItemPrototype), proto); }
    }

    public class ItemActionReplaceSelfLootTablePrototype : ItemActionPrototype
    {
        public LootTablePrototype LootTable;
        public bool UseCurrentAvatarLevelForRoll;
        public ItemActionReplaceSelfLootTablePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemActionReplaceSelfLootTablePrototype), proto); }
    }

    public class ItemActionSaveDangerRoomScenarioPrototype : ItemActionPrototype
    {
        public ItemActionSaveDangerRoomScenarioPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemActionSaveDangerRoomScenarioPrototype), proto); }
    }

    public class ItemActionRespecPrototype : ItemActionPrototype
    {
        public ItemActionRespecPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemActionRespecPrototype), proto); }
    }

    public class ItemActionResetMissionsPrototype : ItemActionPrototype
    {
        public ItemActionResetMissionsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemActionResetMissionsPrototype), proto); }
    }

    public class ItemActionPrestigeModePrototype : ItemActionPrototype
    {
        public ItemActionPrestigeModePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemActionPrestigeModePrototype), proto); }
    }

    public class ItemActionUsePowerPrototype : ItemActionPrototype
    {
        public ulong Power;
        public ItemActionUsePowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemActionUsePowerPrototype), proto); }
    }

    public class ItemActionUnlockPermaBuffPrototype : ItemActionPrototype
    {
        public ulong PermaBuff;
        public ItemActionUnlockPermaBuffPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemActionUnlockPermaBuffPrototype), proto); }
    }

    public class ItemActionAwardTeamUpXPPrototype : ItemActionPrototype
    {
        public int XP;
        public ItemActionAwardTeamUpXPPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemActionAwardTeamUpXPPrototype), proto); }
    }

    public class ItemActionSetPrototype : ItemActionBasePrototype
    {
        public ItemActionBasePrototype[] Choices;
        public PickMethodType PickMethod;
        public ItemActionSetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemActionSetPrototype), proto); }
    }
    public enum PickMethodType
    {
        PickWeight = 0,
        PickWeightTryAll = 1,
        PickAll = 2,
    }

    public class ItemActionOpenUIPanelPrototype : ItemActionPrototype
    {
        public ulong PanelName;
        public ItemActionOpenUIPanelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemActionOpenUIPanelPrototype), proto); }
    }

    public class CategorizedAffixEntryPrototype : Prototype
    {
        public ulong Category;
        public short MinAffixes;
        public CategorizedAffixEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CategorizedAffixEntryPrototype), proto); }
    }

    public class AffixLimitsPrototype : Prototype
    {
        public LootContext[] AllowedContexts;
        public ulong ItemRarity;
        public short MaxPrefixes;
        public short MaxSuffixes;
        public short MinPrefixes;
        public short MinSuffixes;
        public short NumCosmics;
        public short MaxBlessings;
        public short NumUltimates;
        public short MaxRunewords;
        public short MinTeamUps;
        public short MaxTeamUps;
        public short MinUniques;
        public short MaxUniques;
        public short RegionAffixMax;
        public short RegionAffixMin;
        public short NumSocket1;
        public short NumSocket2;
        public short NumSocket3;
        public int RegionDifficultyIndex;
        public float DamageRegionMobToPlayer;
        public float DamageRegionPlayerToMob;
        public CategorizedAffixEntryPrototype[] CategorizedAffixes;
        public AffixLimitsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AffixLimitsPrototype), proto); }
    }

    public class EquipRestrictionPrototype : Prototype
    {
        public EquipRestrictionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EquipRestrictionPrototype), proto); }
    }

    public class EquipRestrictionSuperteamPrototype : EquipRestrictionPrototype
    {
        public ulong SuperteamEquippableBy;
        public EquipRestrictionSuperteamPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EquipRestrictionSuperteamPrototype), proto); }
    }

    public class EquipRestrictionAgentPrototype : EquipRestrictionPrototype
    {
        public ulong Agent;
        public EquipRestrictionAgentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EquipRestrictionAgentPrototype), proto); }
    }

    public class ItemTooltipPropertyBlockSettingsPrototype : Prototype
    {
        public ulong IncludeAllButProperties;
        public ulong IncludeOnlyProperties;
        public bool UseBuiltinPropertyOrdering;
        public ItemTooltipPropertyBlockSettingsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemTooltipPropertyBlockSettingsPrototype), proto); }
    }

    public class LimitedEditionPrototype : Prototype
    {
        public LimitedEditionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LimitedEditionPrototype), proto); }
    }

    public class ArmorPrototype : ItemPrototype
    {
        public ArmorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ArmorPrototype), proto); }
    }

    public class ArtifactPrototype : ItemPrototype
    {
        public ArtifactPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ArtifactPrototype), proto); }
    }

    public class BagItemPrototype : ItemPrototype
    {
        public bool AllowsPlayerAdds;
        public BagItemPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(BagItemPrototype), proto); }
    }

    public class CharacterTokenPrototype : ItemPrototype
    {
        public ulong Character;
        public CharacterTokenType TokenType;
        public CharacterTokenPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CharacterTokenPrototype), proto); }
    }
    public enum CharacterTokenType
    {
        None = 0,
        UnlockCharacterOnly = 1,
        UnlockCharOrUpgradeUlt = 2,
        UpgradeUltimateOnly = 4,
    }

    public class InventoryStashTokenPrototype : ItemPrototype
    {
        public ulong Inventory;
        public InventoryStashTokenPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InventoryStashTokenPrototype), proto); }
    }

    public class EmoteTokenPrototype : ItemPrototype
    {
        public ulong Avatar;
        public ulong EmotePower;
        public EmoteTokenPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EmoteTokenPrototype), proto); }
    }

    public class CostumePrototype : ItemPrototype
    {
        public ulong CostumeUnrealClass;
        public ulong FullBodyIconPath;
        public ulong UsableBy;
        public new ulong StoreIconPath;
        public ulong PortraitIconPath;
        public ulong FullBodyIconPathDisabled;
        public ulong PartyPortraitIconPath;
        public ulong MTXStoreInfo;
        public ulong AvatarBioText;
        public ulong AvatarDisplayName;
        public ulong AvatarDisplayNameInformal;
        public ulong AvatarDisplayNameShort;
        public bool EquipTriggersVO;
        public ulong PortraitIconPathHiRes;
        public ulong FulfillmentDuplicateItem;
        public CostumePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CostumePrototype), proto); }
    }

    public class LegendaryPrototype : ItemPrototype
    {
        public LegendaryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LegendaryPrototype), proto); }
    }

    public class MedalPrototype : ItemPrototype
    {
        public MedalPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MedalPrototype), proto); }
    }

    public class RelicPrototype : ItemPrototype
    {
        public EvalPrototype EvalOnStackCountChange;
        public RelicPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RelicPrototype), proto); }
    }

    public class SuperteamPrototype : Prototype
    {
        public ulong DisplayName;
        public SuperteamPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SuperteamPrototype), proto); }
    }

    public class TeamUpGearPrototype : ItemPrototype
    {
        public TeamUpGearPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TeamUpGearPrototype), proto); }
    }

    public class PermaBuffPrototype : Prototype
    {
        public EvalPrototype EvalAvatarProperties;
        public PermaBuffPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PermaBuffPrototype), proto); }
    }

}
