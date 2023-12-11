using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum ItemInstrumentedDropGroup
    {
        Character = 1,
        Costume = 2,
        RareArtifact = 3,
    }

    [AssetEnum]
    public enum ItemEventType
    {
        None = 0,
        OnEquip = 1,
        OnUse = 2,
        OnUsePowerActivated = 3,
    }

    [AssetEnum]
    public enum PickMethod
    {
        PickWeight = 0,
        PickWeightTryAll = 1,
        PickAll = 2,
    }

    [AssetEnum]
    public enum CharacterTokenType  // Entity/Items/CharacterTokens/TokenType.type
    {
        None = 0,
        UnlockCharacterOnly = 1,
        UnlockCharOrUpgradeUlt = 2,
        UpgradeUltimateOnly = 4,
    }

    #endregion

    public class ItemPrototype : WorldEntityPrototype
    {
        public bool IsUsable { get; set; }
        public bool CanBeSoldToVendor { get; set; }
        public int MaxVisiblePrefixes { get; set; }
        public int MaxVisibleSuffixes { get; set; }
        public ulong TooltipDescription { get; set; }
        public ulong TooltipFlavorText { get; set; }
        public ulong TooltipTemplate { get; set; }
        public ItemStackSettingsPrototype StackSettings { get; set; }
        public bool AlwaysDisplayAsUsable { get; set; }
        public ulong TooltipEquipRestrictions { get; set; }
        public AffixEntryPrototype[] AffixesBuiltIn { get; set; }
        public PropertyEntryPrototype[] PropertiesBuiltIn { get; set; }
        public ProductPrototype Product { get; set; }
        public ulong ItemCategory { get; set; }
        public ulong ItemSubcategory { get; set; }
        public bool IsAvatarRestricted { get; set; }
        public DropRestrictionPrototype[] LootDropRestrictions { get; set; }
        public ItemBindingSettingsPrototype BindingSettings { get; set; }
        public AffixLimitsPrototype[] AffixLimits { get; set; }
        public ulong TextStyleOverride { get; set; }
        public ItemAbilitySettingsPrototype AbilitySettings { get; set; }
        public ulong StoreIconPath { get; set; }
        public bool ClonedWhenPurchasedFromVendor { get; set; }
        public ItemActionSetPrototype ActionsTriggeredOnItemEvent { get; set; }
        public bool ConfirmOnDonate { get; set; }
        public bool CanBeDestroyed { get; set; }
        public bool ConfirmPurchase { get; set; }
        public ItemCostPrototype Cost { get; set; }
        public int TooltipDepthOverride { get; set; }
        public EquipRestrictionPrototype[] EquipRestrictions { get; set; }
        public EvalPrototype EvalExpirationTimeMS { get; set; }
        public ItemTooltipPropertyBlockSettingsPrototype[] TooltipCustomPropertyBlocks { get; set; }
        public float LootDropWeightMultiplier { get; set; }
        public ConvenienceLabel DestinationFromVendor { get; set; }
        public EvalPrototype EvalDisplayLevel { get; set; }
        public bool CanBroadcast { get; set; }
        public EquipmentInvUISlot DefaultEquipmentSlot { get; set; }
        public EvalPrototype EvalCanUse { get; set; }
        public ulong[] CannotEquipWithItemsOfKeyword { get; set; }
        public ulong SortCategory { get; set; }
        public ulong SortSubCategory { get; set; }
        public ItemInstrumentedDropGroup InstrumentedDropGroup { get; set; }
        public bool IsContainer { get; set; }
    }

    public class ItemAbilitySettingsPrototype : Prototype
    {
        public AbilitySlotRestrictionPrototype AbilitySlotRestriction { get; set; }
        public bool OnlySlottableWhileEquipped { get; set; }
    }

    public class ItemBindingSettingsEntryPrototype : Prototype
    {
        public bool BindsToAccountOnPickup { get; set; }
        public bool BindsToCharacterOnEquip { get; set; }
        public bool IsTradable { get; set; }
        public ulong RarityFilter { get; set; }
    }

    public class ItemBindingSettingsPrototype : Prototype
    {
        public ItemBindingSettingsEntryPrototype DefaultSettings { get; set; }
        public ItemBindingSettingsEntryPrototype[] PerRaritySettings { get; set; }
    }

    public class ItemStackSettingsPrototype : Prototype
    {
        public int ItemLevelOverride { get; set; }
        public int MaxStacks { get; set; }
        public int RequiredCharLevelOverride { get; set; }
        public bool AutoStackWhenAddedToInventory { get; set; }
        public bool StacksCanBeSplit { get; set; }
    }

    public class ItemActionBasePrototype : Prototype
    {
        public int Weight { get; set; }
    }

    public class ItemActionPrototype : ItemActionBasePrototype
    {
        public ItemEventType TriggeringEvent { get; set; }
    }

    public class ItemActionAssignPowerPrototype : ItemActionPrototype
    {
        public ulong Power { get; set; }
    }

    public class ItemActionDestroySelfPrototype : ItemActionPrototype
    {
    }

    public class ItemActionGuildsUnlockPrototype : ItemActionPrototype
    {
    }

    public class ItemActionReplaceSelfItemPrototype : ItemActionPrototype
    {
        public ulong Item { get; set; }
    }

    public class ItemActionReplaceSelfLootTablePrototype : ItemActionPrototype
    {
        public LootTablePrototype LootTable { get; set; }
        public bool UseCurrentAvatarLevelForRoll { get; set; }
    }

    public class ItemActionSaveDangerRoomScenarioPrototype : ItemActionPrototype
    {
    }

    public class ItemActionRespecPrototype : ItemActionPrototype
    {
    }

    public class ItemActionResetMissionsPrototype : ItemActionPrototype
    {
    }

    public class ItemActionPrestigeModePrototype : ItemActionPrototype
    {
    }

    public class ItemActionUsePowerPrototype : ItemActionPrototype
    {
        public ulong Power { get; set; }
    }

    public class ItemActionUnlockPermaBuffPrototype : ItemActionPrototype
    {
        public ulong PermaBuff { get; set; }
    }

    public class ItemActionAwardTeamUpXPPrototype : ItemActionPrototype
    {
        public int XP { get; set; }
    }

    public class ItemActionSetPrototype : ItemActionBasePrototype
    {
        public ItemActionBasePrototype[] Choices { get; set; }
        public PickMethod PickMethod { get; set; }
    }

    public class ItemActionOpenUIPanelPrototype : ItemActionPrototype
    {
        public ulong PanelName { get; set; }
    }

    public class CategorizedAffixEntryPrototype : Prototype
    {
        public ulong Category { get; set; }
        public short MinAffixes { get; set; }
    }

    public class AffixLimitsPrototype : Prototype
    {
        public LootContext[] AllowedContexts { get; set; }
        public ulong ItemRarity { get; set; }
        public short MaxPrefixes { get; set; }
        public short MaxSuffixes { get; set; }
        public short MinPrefixes { get; set; }
        public short MinSuffixes { get; set; }
        public short NumCosmics { get; set; }
        public short MaxBlessings { get; set; }
        public short NumUltimates { get; set; }
        public short MaxRunewords { get; set; }
        public short MinTeamUps { get; set; }
        public short MaxTeamUps { get; set; }
        public short MinUniques { get; set; }
        public short MaxUniques { get; set; }
        public short RegionAffixMax { get; set; }
        public short RegionAffixMin { get; set; }
        public short NumSocket1 { get; set; }
        public short NumSocket2 { get; set; }
        public short NumSocket3 { get; set; }
        public int RegionDifficultyIndex { get; set; }
        public float DamageRegionMobToPlayer { get; set; }
        public float DamageRegionPlayerToMob { get; set; }
        public CategorizedAffixEntryPrototype[] CategorizedAffixes { get; set; }
    }

    public class EquipRestrictionPrototype : Prototype
    {
    }

    public class EquipRestrictionSuperteamPrototype : EquipRestrictionPrototype
    {
        public ulong SuperteamEquippableBy { get; set; }
    }

    public class EquipRestrictionAgentPrototype : EquipRestrictionPrototype
    {
        public ulong Agent { get; set; }
    }

    public class ItemTooltipPropertyBlockSettingsPrototype : Prototype
    {
        public ulong IncludeAllButProperties { get; set; }
        public ulong IncludeOnlyProperties { get; set; }
        public bool UseBuiltinPropertyOrdering { get; set; }
    }

    public class LimitedEditionPrototype : Prototype
    {
    }

    public class ArmorPrototype : ItemPrototype
    {
    }

    public class ArtifactPrototype : ItemPrototype
    {
    }

    public class BagItemPrototype : ItemPrototype
    {
        public bool AllowsPlayerAdds { get; set; }
    }

    public class CharacterTokenPrototype : ItemPrototype
    {
        public ulong Character { get; set; }
        public CharacterTokenType TokenType { get; set; }
    }

    public class InventoryStashTokenPrototype : ItemPrototype
    {
        public ulong Inventory { get; set; }
    }

    public class EmoteTokenPrototype : ItemPrototype
    {
        public ulong Avatar { get; set; }
        public ulong EmotePower { get; set; }
    }

    public class CostumePrototype : ItemPrototype
    {
        public ulong CostumeUnrealClass { get; set; }
        public ulong FullBodyIconPath { get; set; }
        public ulong UsableBy { get; set; }
        public new ulong StoreIconPath { get; set; }
        public ulong PortraitIconPath { get; set; }
        public ulong FullBodyIconPathDisabled { get; set; }
        public ulong PartyPortraitIconPath { get; set; }
        public ulong MTXStoreInfo { get; set; }
        public ulong AvatarBioText { get; set; }
        public ulong AvatarDisplayName { get; set; }
        public ulong AvatarDisplayNameInformal { get; set; }
        public ulong AvatarDisplayNameShort { get; set; }
        public bool EquipTriggersVO { get; set; }
        public ulong PortraitIconPathHiRes { get; set; }
        public ulong FulfillmentDuplicateItem { get; set; }
    }

    public class LegendaryPrototype : ItemPrototype
    {
    }

    public class MedalPrototype : ItemPrototype
    {
    }

    public class RelicPrototype : ItemPrototype
    {
        public EvalPrototype EvalOnStackCountChange { get; set; }
    }

    public class SuperteamPrototype : Prototype
    {
        public ulong DisplayName { get; set; }
    }

    public class TeamUpGearPrototype : ItemPrototype
    {
    }

    public class PermaBuffPrototype : Prototype
    {
        public EvalPrototype EvalAvatarProperties { get; set; }
    }
}
