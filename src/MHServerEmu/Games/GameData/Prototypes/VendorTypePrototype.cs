namespace MHServerEmu.Games.GameData.Prototypes
{
    public class VendorXPBarTooltipPrototype : Prototype
    {
        public LocaleStringId NextRankTooltip { get; protected set; }
        public LocaleStringId ThisRankTooltip { get; protected set; }
    }

    public class VendorInventoryEntryPrototype : Prototype
    {
        public PrototypeId LootTable { get; protected set; }
        public int UseStartingAtVendorLevel { get; protected set; }
        public VendorXPBarTooltipPrototype VendorXPBarTooltip { get; protected set; }
        public PrototypeId PlayerInventory { get; protected set; }
    }

    public class VendorTypePrototype : Prototype
    {
        public VendorInventoryEntryPrototype[] Inventories { get; protected set; }
        public float VendorEnergyPctPerRefresh { get; protected set; }
        public float VendorEnergyFullRechargeTimeMins { get; protected set; }
        public LocaleStringId VendorXPTooltip { get; protected set; }
        public LocaleStringId VendorRefreshTooltip { get; protected set; }
        public LocaleStringId VendorDonateTooltip { get; protected set; }
        public bool AllowActionDonate { get; protected set; }
        public bool AllowActionRefresh { get; protected set; }
        public bool IsCrafter { get; protected set; }
        public LocaleStringId TypeName { get; protected set; }
        public LocaleStringId VendorIconTooltip { get; protected set; }
        public HUDEntityOverheadIcon InteractIndicator { get; protected set; }
        public LocaleStringId VendorDonateTooltipMax { get; protected set; }
        public PrototypeId GlobalEvent { get; protected set; }
        public bool AllowActionSell { get; protected set; }
        public LocaleStringId VendorFlavorText { get; protected set; }
        public bool IsRaidVendor { get; protected set; }
        public CurveId VendorLevelingCurve { get; protected set; }
        public PrototypeId ReputationDisplayInfo { get; protected set; }
        public PrototypeId[] CraftingRecipeCategories { get; protected set; }
        public bool IsEnchanter { get; protected set; }
        public LocaleStringId VendorRankTooltip { get; protected set; }
    }
}
