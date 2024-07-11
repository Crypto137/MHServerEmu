using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)None)]
    public enum ItemInstrumentedDropGroup
    {
        None = 0,
        Character = 1,
        Costume = 2,
        RareArtifact = 3,
    }

    [AssetEnum((int)None)]
    public enum ItemEventType
    {
        None = 0,
        OnEquip = 1,
        OnUse = 2,
        OnUsePowerActivated = 3,
    }

    [AssetEnum((int)PickWeight)]
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
        private static readonly Logger Logger = LogManager.CreateLogger();

        public bool IsUsable { get; protected set; }
        public bool CanBeSoldToVendor { get; protected set; }
        public int MaxVisiblePrefixes { get; protected set; }
        public int MaxVisibleSuffixes { get; protected set; }
        public LocaleStringId TooltipDescription { get; protected set; }
        public LocaleStringId TooltipFlavorText { get; protected set; }
        public PrototypeId TooltipTemplate { get; protected set; }
        public ItemStackSettingsPrototype StackSettings { get; protected set; }
        public bool AlwaysDisplayAsUsable { get; protected set; }
        public PrototypeId[] TooltipEquipRestrictions { get; protected set; }
        public AffixEntryPrototype[] AffixesBuiltIn { get; protected set; }
        public PropertyEntryPrototype[] PropertiesBuiltIn { get; protected set; }
        [Mixin]
        public ProductPrototype Product { get; protected set; }
        public LocaleStringId ItemCategory { get; protected set; }
        public LocaleStringId ItemSubcategory { get; protected set; }
        public bool IsAvatarRestricted { get; protected set; }
        public DropRestrictionPrototype[] LootDropRestrictions { get; protected set; }
        public ItemBindingSettingsPrototype BindingSettings { get; protected set; }
        public AffixLimitsPrototype[] AffixLimits { get; protected set; }
        public PrototypeId TextStyleOverride { get; protected set; }
        public ItemAbilitySettingsPrototype AbilitySettings { get; protected set; }
        public AssetId StoreIconPath { get; protected set; }
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
        public InventoryConvenienceLabel DestinationFromVendor { get; protected set; }
        public EvalPrototype EvalDisplayLevel { get; protected set; }
        public bool CanBroadcast { get; protected set; }
        public EquipmentInvUISlot DefaultEquipmentSlot { get; protected set; }
        public EvalPrototype EvalCanUse { get; protected set; }
        public PrototypeId[] CannotEquipWithItemsOfKeyword { get; protected set; }
        public PrototypeId SortCategory { get; protected set; }
        public PrototypeId SortSubCategory { get; protected set; }
        public ItemInstrumentedDropGroup InstrumentedDropGroup { get; protected set; }
        public bool IsContainer { get; protected set; }

        public void OnApplyItemSpec(Item item, ItemSpec itemSpec)
        {
            // TODO
        }

        public TimeSpan GetExpirationTime(PrototypeId rarityProtoRef)
        {
            if (EvalExpirationTimeMS == null) return Logger.WarnReturn(TimeSpan.Zero, "GetExpirationTime(): EvalExpirationTimeMS == null");

            EvalContextData contextData = new();
            contextData.SetReadOnlyVar_ProtoRef(EvalContext.Var1, rarityProtoRef);

            int expirationTimeMS = Eval.RunInt(EvalExpirationTimeMS, contextData);
            return TimeSpan.FromMilliseconds(expirationTimeMS);
        }

        public bool IsUsableByAgent(AgentPrototype agentProto)
        {
            if (EquipRestrictions.IsNullOrEmpty())
                return true;

            foreach (EquipRestrictionPrototype restrictionProto in EquipRestrictions)
            {
                if (restrictionProto.IsEquippableByAgent(agentProto) == false)
                    return false;
            }

            return true;
        }

        public bool IsDroppableForAgent(AgentPrototype agentProto)
        {
            return IsUsableByAgent(agentProto);
        }
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
        public PrototypeId RarityFilter { get; protected set; }
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
        public PrototypeId Power { get; protected set; }
    }

    public class ItemActionDestroySelfPrototype : ItemActionPrototype
    {
    }

    public class ItemActionGuildsUnlockPrototype : ItemActionPrototype
    {
    }

    public class ItemActionReplaceSelfItemPrototype : ItemActionPrototype
    {
        public PrototypeId Item { get; protected set; }
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
        public PrototypeId Power { get; protected set; }
    }

    public class ItemActionUnlockPermaBuffPrototype : ItemActionPrototype
    {
        public PrototypeId PermaBuff { get; protected set; }
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
        public AssetId PanelName { get; protected set; }
    }

    public class CategorizedAffixEntryPrototype : Prototype
    {
        public PrototypeId Category { get; protected set; }
        public short MinAffixes { get; protected set; }
    }

    public class AffixLimitsPrototype : Prototype
    {
        public LootContext[] AllowedContexts { get; protected set; }
        public PrototypeId ItemRarity { get; protected set; }
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
        public virtual bool IsEquippableByAgent(AgentPrototype agentProto)
        {
            return true;
        }
    }

    public class EquipRestrictionSuperteamPrototype : EquipRestrictionPrototype
    {
        public PrototypeId SuperteamEquippableBy { get; protected set; }

        public override bool IsEquippableByAgent(AgentPrototype agentProto)
        {
            if (base.IsEquippableByAgent(agentProto) == false)
                return false;

            if (agentProto is not AvatarPrototype avatarProto)
                return true;

            return avatarProto.IsMemberOfSuperteam(SuperteamEquippableBy);
        }
    }

    public class EquipRestrictionAgentPrototype : EquipRestrictionPrototype
    {
        public PrototypeId Agent { get; protected set; }

        public override bool IsEquippableByAgent(AgentPrototype agentProto)
        {
            if (base.IsEquippableByAgent(agentProto) == false)
                return false;

            if (agentProto == null || Agent == PrototypeId.Invalid)
                return true;

            return agentProto.DataRef == Agent;
        }
    }

    public class ItemTooltipPropertyBlockSettingsPrototype : Prototype
    {
        public PrototypeId[] IncludeAllButProperties { get; protected set; }
        public PrototypeId[] IncludeOnlyProperties { get; protected set; }
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
        public PrototypeId Character { get; protected set; }
        public CharacterTokenType TokenType { get; protected set; }
    }

    public class InventoryStashTokenPrototype : ItemPrototype
    {
        public PrototypeId Inventory { get; protected set; }
    }

    public class EmoteTokenPrototype : ItemPrototype
    {
        public PrototypeId Avatar { get; protected set; }
        public PrototypeId EmotePower { get; protected set; }
    }

    public class CostumePrototype : ItemPrototype
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public AssetId CostumeUnrealClass { get; protected set; }
        public AssetId FullBodyIconPath { get; protected set; }
        public PrototypeId UsableBy { get; protected set; }
        public new AssetId StoreIconPath { get; protected set; }
        public AssetId PortraitIconPath { get; protected set; }
        public AssetId FullBodyIconPathDisabled { get; protected set; }
        public AssetId PartyPortraitIconPath { get; protected set; }
        public LocaleStringId MTXStoreInfo { get; protected set; }
        public LocaleStringId AvatarBioText { get; protected set; }
        public LocaleStringId AvatarDisplayName { get; protected set; }
        public LocaleStringId AvatarDisplayNameInformal { get; protected set; }
        public LocaleStringId AvatarDisplayNameShort { get; protected set; }
        public bool EquipTriggersVO { get; protected set; }
        public AssetId PortraitIconPathHiRes { get; protected set; }
        public PrototypeId FulfillmentDuplicateItem { get; protected set; }

        public override bool ApprovedForUse()
        {
            if (base.ApprovedForUse() == false) return false;

            AvatarPrototype avatar = GameDatabase.GetPrototype<AvatarPrototype>(UsableBy);
            if (avatar == null) return Logger.WarnReturn(false, $"ApprovedForUse(): avatar == null");

            ItemPrototype itemProto = GameDatabase.GetPrototype<ItemPrototype>(FulfillmentDuplicateItem);
            if (itemProto == null || itemProto == this) Logger.WarnReturn(false, "ApprovedForUse(): itemProto == null || itemProto == this");

            return avatar.ApprovedForUse() && itemProto.ApprovedForUse();
        }
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
        public LocaleStringId DisplayName { get; protected set; }
    }

    public class TeamUpGearPrototype : ItemPrototype
    {
    }

    public class PermaBuffPrototype : Prototype
    {
        public EvalPrototype EvalAvatarProperties { get; protected set; }
    }
}
