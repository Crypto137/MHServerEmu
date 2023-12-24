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
        public bool IsUsable { get; private set; }
        public bool CanBeSoldToVendor { get; private set; }
        public int MaxVisiblePrefixes { get; private set; }
        public int MaxVisibleSuffixes { get; private set; }
        public ulong TooltipDescription { get; private set; }
        public ulong TooltipFlavorText { get; private set; }
        public ulong TooltipTemplate { get; private set; }
        public ItemStackSettingsPrototype StackSettings { get; private set; }
        public bool AlwaysDisplayAsUsable { get; private set; }
        public ulong TooltipEquipRestrictions { get; private set; }
        public AffixEntryPrototype[] AffixesBuiltIn { get; private set; }
        public PropertyEntryPrototype[] PropertiesBuiltIn { get; private set; }
        public ProductPrototype Product { get; private set; }
        public ulong ItemCategory { get; private set; }
        public ulong ItemSubcategory { get; private set; }
        public bool IsAvatarRestricted { get; private set; }
        public DropRestrictionPrototype[] LootDropRestrictions { get; private set; }
        public ItemBindingSettingsPrototype BindingSettings { get; private set; }
        public AffixLimitsPrototype[] AffixLimits { get; private set; }
        public ulong TextStyleOverride { get; private set; }
        public ItemAbilitySettingsPrototype AbilitySettings { get; private set; }
        public ulong StoreIconPath { get; private set; }
        public bool ClonedWhenPurchasedFromVendor { get; private set; }
        public ItemActionSetPrototype ActionsTriggeredOnItemEvent { get; private set; }
        public bool ConfirmOnDonate { get; private set; }
        public bool CanBeDestroyed { get; private set; }
        public bool ConfirmPurchase { get; private set; }
        public ItemCostPrototype Cost { get; private set; }
        public int TooltipDepthOverride { get; private set; }
        public EquipRestrictionPrototype[] EquipRestrictions { get; private set; }
        public EvalPrototype EvalExpirationTimeMS { get; private set; }
        public ItemTooltipPropertyBlockSettingsPrototype[] TooltipCustomPropertyBlocks { get; private set; }
        public float LootDropWeightMultiplier { get; private set; }
        public ConvenienceLabel DestinationFromVendor { get; private set; }
        public EvalPrototype EvalDisplayLevel { get; private set; }
        public bool CanBroadcast { get; private set; }
        public EquipmentInvUISlot DefaultEquipmentSlot { get; private set; }
        public EvalPrototype EvalCanUse { get; private set; }
        public ulong[] CannotEquipWithItemsOfKeyword { get; private set; }
        public ulong SortCategory { get; private set; }
        public ulong SortSubCategory { get; private set; }
        public ItemInstrumentedDropGroup InstrumentedDropGroup { get; private set; }
        public bool IsContainer { get; private set; }
    }

    public class ItemAbilitySettingsPrototype : Prototype
    {
        public AbilitySlotRestrictionPrototype AbilitySlotRestriction { get; private set; }
        public bool OnlySlottableWhileEquipped { get; private set; }
    }

    public class ItemBindingSettingsEntryPrototype : Prototype
    {
        public bool BindsToAccountOnPickup { get; private set; }
        public bool BindsToCharacterOnEquip { get; private set; }
        public bool IsTradable { get; private set; }
        public ulong RarityFilter { get; private set; }
    }

    public class ItemBindingSettingsPrototype : Prototype
    {
        public ItemBindingSettingsEntryPrototype DefaultSettings { get; private set; }
        public ItemBindingSettingsEntryPrototype[] PerRaritySettings { get; private set; }
    }

    public class ItemStackSettingsPrototype : Prototype
    {
        public int ItemLevelOverride { get; private set; }
        public int MaxStacks { get; private set; }
        public int RequiredCharLevelOverride { get; private set; }
        public bool AutoStackWhenAddedToInventory { get; private set; }
        public bool StacksCanBeSplit { get; private set; }
    }

    public class ItemActionBasePrototype : Prototype
    {
        public int Weight { get; private set; }
    }

    public class ItemActionPrototype : ItemActionBasePrototype
    {
        public ItemEventType TriggeringEvent { get; private set; }
    }

    public class ItemActionAssignPowerPrototype : ItemActionPrototype
    {
        public ulong Power { get; private set; }
    }

    public class ItemActionDestroySelfPrototype : ItemActionPrototype
    {
    }

    public class ItemActionGuildsUnlockPrototype : ItemActionPrototype
    {
    }

    public class ItemActionReplaceSelfItemPrototype : ItemActionPrototype
    {
        public ulong Item { get; private set; }
    }

    public class ItemActionReplaceSelfLootTablePrototype : ItemActionPrototype
    {
        public LootTablePrototype LootTable { get; private set; }
        public bool UseCurrentAvatarLevelForRoll { get; private set; }
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
        public ulong Power { get; private set; }
    }

    public class ItemActionUnlockPermaBuffPrototype : ItemActionPrototype
    {
        public ulong PermaBuff { get; private set; }
    }

    public class ItemActionAwardTeamUpXPPrototype : ItemActionPrototype
    {
        public int XP { get; private set; }
    }

    public class ItemActionSetPrototype : ItemActionBasePrototype
    {
        public ItemActionBasePrototype[] Choices { get; private set; }
        public PickMethod PickMethod { get; private set; }
    }

    public class ItemActionOpenUIPanelPrototype : ItemActionPrototype
    {
        public ulong PanelName { get; private set; }
    }

    public class CategorizedAffixEntryPrototype : Prototype
    {
        public ulong Category { get; private set; }
        public short MinAffixes { get; private set; }
    }

    public class AffixLimitsPrototype : Prototype
    {
        public LootContext[] AllowedContexts { get; private set; }
        public ulong ItemRarity { get; private set; }
        public short MaxPrefixes { get; private set; }
        public short MaxSuffixes { get; private set; }
        public short MinPrefixes { get; private set; }
        public short MinSuffixes { get; private set; }
        public short NumCosmics { get; private set; }
        public short MaxBlessings { get; private set; }
        public short NumUltimates { get; private set; }
        public short MaxRunewords { get; private set; }
        public short MinTeamUps { get; private set; }
        public short MaxTeamUps { get; private set; }
        public short MinUniques { get; private set; }
        public short MaxUniques { get; private set; }
        public short RegionAffixMax { get; private set; }
        public short RegionAffixMin { get; private set; }
        public short NumSocket1 { get; private set; }
        public short NumSocket2 { get; private set; }
        public short NumSocket3 { get; private set; }
        public int RegionDifficultyIndex { get; private set; }
        public float DamageRegionMobToPlayer { get; private set; }
        public float DamageRegionPlayerToMob { get; private set; }
        public CategorizedAffixEntryPrototype[] CategorizedAffixes { get; private set; }
    }

    public class EquipRestrictionPrototype : Prototype
    {
    }

    public class EquipRestrictionSuperteamPrototype : EquipRestrictionPrototype
    {
        public ulong SuperteamEquippableBy { get; private set; }
    }

    public class EquipRestrictionAgentPrototype : EquipRestrictionPrototype
    {
        public ulong Agent { get; private set; }
    }

    public class ItemTooltipPropertyBlockSettingsPrototype : Prototype
    {
        public ulong IncludeAllButProperties { get; private set; }
        public ulong IncludeOnlyProperties { get; private set; }
        public bool UseBuiltinPropertyOrdering { get; private set; }
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
        public bool AllowsPlayerAdds { get; private set; }
    }

    public class CharacterTokenPrototype : ItemPrototype
    {
        public ulong Character { get; private set; }
        public CharacterTokenType TokenType { get; private set; }
    }

    public class InventoryStashTokenPrototype : ItemPrototype
    {
        public ulong Inventory { get; private set; }
    }

    public class EmoteTokenPrototype : ItemPrototype
    {
        public ulong Avatar { get; private set; }
        public ulong EmotePower { get; private set; }
    }

    public class CostumePrototype : ItemPrototype
    {
        public ulong CostumeUnrealClass { get; private set; }
        public ulong FullBodyIconPath { get; private set; }
        public ulong UsableBy { get; private set; }
        public new ulong StoreIconPath { get; private set; }
        public ulong PortraitIconPath { get; private set; }
        public ulong FullBodyIconPathDisabled { get; private set; }
        public ulong PartyPortraitIconPath { get; private set; }
        public ulong MTXStoreInfo { get; private set; }
        public ulong AvatarBioText { get; private set; }
        public ulong AvatarDisplayName { get; private set; }
        public ulong AvatarDisplayNameInformal { get; private set; }
        public ulong AvatarDisplayNameShort { get; private set; }
        public bool EquipTriggersVO { get; private set; }
        public ulong PortraitIconPathHiRes { get; private set; }
        public ulong FulfillmentDuplicateItem { get; private set; }
    }

    public class LegendaryPrototype : ItemPrototype
    {
    }

    public class MedalPrototype : ItemPrototype
    {
    }

    public class RelicPrototype : ItemPrototype
    {
        public EvalPrototype EvalOnStackCountChange { get; private set; }
    }

    public class SuperteamPrototype : Prototype
    {
        public ulong DisplayName { get; private set; }
    }

    public class TeamUpGearPrototype : ItemPrototype
    {
    }

    public class PermaBuffPrototype : Prototype
    {
        public EvalPrototype EvalAvatarProperties { get; private set; }
    }
}
