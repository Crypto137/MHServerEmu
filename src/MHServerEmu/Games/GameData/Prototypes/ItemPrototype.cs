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
        public bool IsUsable { get; protected set; }
        public bool CanBeSoldToVendor { get; protected set; }
        public int MaxVisiblePrefixes { get; protected set; }
        public int MaxVisibleSuffixes { get; protected set; }
        public ulong TooltipDescription { get; protected set; }
        public ulong TooltipFlavorText { get; protected set; }
        public ulong TooltipTemplate { get; protected set; }
        public ItemStackSettingsPrototype StackSettings { get; protected set; }
        public bool AlwaysDisplayAsUsable { get; protected set; }
        public ulong[] TooltipEquipRestrictions { get; protected set; }
        public AffixEntryPrototype[] AffixesBuiltIn { get; protected set; }
        public PropertyEntryPrototype[] PropertiesBuiltIn { get; protected set; }
        public ProductPrototype Product { get; protected set; }
        public ulong ItemCategory { get; protected set; }
        public ulong ItemSubcategory { get; protected set; }
        public bool IsAvatarRestricted { get; protected set; }
        public DropRestrictionPrototype[] LootDropRestrictions { get; protected set; }
        public ItemBindingSettingsPrototype BindingSettings { get; protected set; }
        public AffixLimitsPrototype[] AffixLimits { get; protected set; }
        public ulong TextStyleOverride { get; protected set; }
        public ItemAbilitySettingsPrototype AbilitySettings { get; protected set; }
        public ulong StoreIconPath { get; protected set; }
        public bool ClonedWhenPurchasedFromVendor { get; protected set; }
        public ItemActionSetPrototype ActionsTriggeredOnItemEvent { get; protected set; }
        public bool ConfirmOnDonate { get; protected set; }
        public bool CanBeDestroyed { get; protected set; }
        public bool ConfirmPurchase { get; protected set; }
        public ItemCostPrototype Cost { get; protected set; }
        public int TooltipDepthOverride { get; protected set; }
        public EquipRestrictionPrototype[] EquipRestrictions { get; protected set; }
        public EvalPrototype EvalExpirationTimeMS { get; protected set; }
        public ItemTooltipPropertyBlockSettingsPrototype[] TooltipCustomPropertyBlocks { get; protected set; }
        public float LootDropWeightMultiplier { get; protected set; }
        public ConvenienceLabel DestinationFromVendor { get; protected set; }
        public EvalPrototype EvalDisplayLevel { get; protected set; }
        public bool CanBroadcast { get; protected set; }
        public EquipmentInvUISlot DefaultEquipmentSlot { get; protected set; }
        public EvalPrototype EvalCanUse { get; protected set; }
        public ulong[] CannotEquipWithItemsOfKeyword { get; protected set; }
        public ulong SortCategory { get; protected set; }
        public ulong SortSubCategory { get; protected set; }
        public ItemInstrumentedDropGroup InstrumentedDropGroup { get; protected set; }
        public bool IsContainer { get; protected set; }
    }

    public class ItemAbilitySettingsPrototype : Prototype
    {
        public AbilitySlotRestrictionPrototype AbilitySlotRestriction { get; protected set; }
        public bool OnlySlottableWhileEquipped { get; protected set; }
    }

    public class ItemBindingSettingsEntryPrototype : Prototype
    {
        public bool BindsToAccountOnPickup { get; protected set; }
        public bool BindsToCharacterOnEquip { get; protected set; }
        public bool IsTradable { get; protected set; }
        public ulong RarityFilter { get; protected set; }
    }

    public class ItemBindingSettingsPrototype : Prototype
    {
        public ItemBindingSettingsEntryPrototype DefaultSettings { get; protected set; }
        public ItemBindingSettingsEntryPrototype[] PerRaritySettings { get; protected set; }
    }

    public class ItemStackSettingsPrototype : Prototype
    {
        public int ItemLevelOverride { get; protected set; }
        public int MaxStacks { get; protected set; }
        public int RequiredCharLevelOverride { get; protected set; }
        public bool AutoStackWhenAddedToInventory { get; protected set; }
        public bool StacksCanBeSplit { get; protected set; }
    }

    public class ItemActionBasePrototype : Prototype
    {
        public int Weight { get; protected set; }
    }

    public class ItemActionPrototype : ItemActionBasePrototype
    {
        public ItemEventType TriggeringEvent { get; protected set; }
    }

    public class ItemActionAssignPowerPrototype : ItemActionPrototype
    {
        public ulong Power { get; protected set; }
    }

    public class ItemActionDestroySelfPrototype : ItemActionPrototype
    {
    }

    public class ItemActionGuildsUnlockPrototype : ItemActionPrototype
    {
    }

    public class ItemActionReplaceSelfItemPrototype : ItemActionPrototype
    {
        public ulong Item { get; protected set; }
    }

    public class ItemActionReplaceSelfLootTablePrototype : ItemActionPrototype
    {
        public LootTablePrototype LootTable { get; protected set; }
        public bool UseCurrentAvatarLevelForRoll { get; protected set; }
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
        public ulong Power { get; protected set; }
    }

    public class ItemActionUnlockPermaBuffPrototype : ItemActionPrototype
    {
        public ulong PermaBuff { get; protected set; }
    }

    public class ItemActionAwardTeamUpXPPrototype : ItemActionPrototype
    {
        public int XP { get; protected set; }
    }

    public class ItemActionSetPrototype : ItemActionBasePrototype
    {
        public ItemActionBasePrototype[] Choices { get; protected set; }
        public PickMethod PickMethod { get; protected set; }
    }

    public class ItemActionOpenUIPanelPrototype : ItemActionPrototype
    {
        public ulong PanelName { get; protected set; }
    }

    public class CategorizedAffixEntryPrototype : Prototype
    {
        public ulong Category { get; protected set; }
        public short MinAffixes { get; protected set; }
    }

    public class AffixLimitsPrototype : Prototype
    {
        public LootContext[] AllowedContexts { get; protected set; }
        public ulong ItemRarity { get; protected set; }
        public short MaxPrefixes { get; protected set; }
        public short MaxSuffixes { get; protected set; }
        public short MinPrefixes { get; protected set; }
        public short MinSuffixes { get; protected set; }
        public short NumCosmics { get; protected set; }
        public short MaxBlessings { get; protected set; }
        public short NumUltimates { get; protected set; }
        public short MaxRunewords { get; protected set; }
        public short MinTeamUps { get; protected set; }
        public short MaxTeamUps { get; protected set; }
        public short MinUniques { get; protected set; }
        public short MaxUniques { get; protected set; }
        public short RegionAffixMax { get; protected set; }
        public short RegionAffixMin { get; protected set; }
        public short NumSocket1 { get; protected set; }
        public short NumSocket2 { get; protected set; }
        public short NumSocket3 { get; protected set; }
        public int RegionDifficultyIndex { get; protected set; }
        public float DamageRegionMobToPlayer { get; protected set; }
        public float DamageRegionPlayerToMob { get; protected set; }
        public CategorizedAffixEntryPrototype[] CategorizedAffixes { get; protected set; }
    }

    public class EquipRestrictionPrototype : Prototype
    {
    }

    public class EquipRestrictionSuperteamPrototype : EquipRestrictionPrototype
    {
        public ulong SuperteamEquippableBy { get; protected set; }
    }

    public class EquipRestrictionAgentPrototype : EquipRestrictionPrototype
    {
        public ulong Agent { get; protected set; }
    }

    public class ItemTooltipPropertyBlockSettingsPrototype : Prototype
    {
        public ulong[] IncludeAllButProperties { get; protected set; }
        public ulong[] IncludeOnlyProperties { get; protected set; }
        public bool UseBuiltinPropertyOrdering { get; protected set; }
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
        public bool AllowsPlayerAdds { get; protected set; }
    }

    public class CharacterTokenPrototype : ItemPrototype
    {
        public ulong Character { get; protected set; }
        public CharacterTokenType TokenType { get; protected set; }
    }

    public class InventoryStashTokenPrototype : ItemPrototype
    {
        public ulong Inventory { get; protected set; }
    }

    public class EmoteTokenPrototype : ItemPrototype
    {
        public ulong Avatar { get; protected set; }
        public ulong EmotePower { get; protected set; }
    }

    public class CostumePrototype : ItemPrototype
    {
        public ulong CostumeUnrealClass { get; protected set; }
        public ulong FullBodyIconPath { get; protected set; }
        public ulong UsableBy { get; protected set; }
        public new ulong StoreIconPath { get; protected set; }
        public ulong PortraitIconPath { get; protected set; }
        public ulong FullBodyIconPathDisabled { get; protected set; }
        public ulong PartyPortraitIconPath { get; protected set; }
        public ulong MTXStoreInfo { get; protected set; }
        public ulong AvatarBioText { get; protected set; }
        public ulong AvatarDisplayName { get; protected set; }
        public ulong AvatarDisplayNameInformal { get; protected set; }
        public ulong AvatarDisplayNameShort { get; protected set; }
        public bool EquipTriggersVO { get; protected set; }
        public ulong PortraitIconPathHiRes { get; protected set; }
        public ulong FulfillmentDuplicateItem { get; protected set; }
    }

    public class LegendaryPrototype : ItemPrototype
    {
    }

    public class MedalPrototype : ItemPrototype
    {
    }

    public class RelicPrototype : ItemPrototype
    {
        public EvalPrototype EvalOnStackCountChange { get; protected set; }
    }

    public class SuperteamPrototype : Prototype
    {
        public ulong DisplayName { get; protected set; }
    }

    public class TeamUpGearPrototype : ItemPrototype
    {
    }

    public class PermaBuffPrototype : Prototype
    {
        public EvalPrototype EvalAvatarProperties { get; protected set; }
    }
}
