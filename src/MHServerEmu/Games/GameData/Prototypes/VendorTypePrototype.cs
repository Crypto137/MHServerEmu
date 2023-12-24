namespace MHServerEmu.Games.GameData.Prototypes
{
    public class VendorXPBarTooltipPrototype : Prototype
    {
        public ulong NextRankTooltip { get; private set; }
        public ulong ThisRankTooltip { get; private set; }
    }

    public class VendorInventoryEntryPrototype : Prototype
    {
        public ulong LootTable { get; private set; }
        public int UseStartingAtVendorLevel { get; private set; }
        public VendorXPBarTooltipPrototype VendorXPBarTooltip { get; private set; }
        public ulong PlayerInventory { get; private set; }
    }

    public class VendorTypePrototype : Prototype
    {
        public VendorInventoryEntryPrototype[] Inventories { get; private set; }
        public float VendorEnergyPctPerRefresh { get; private set; }
        public float VendorEnergyFullRechargeTimeMins { get; private set; }
        public ulong VendorXPTooltip { get; private set; }
        public ulong VendorRefreshTooltip { get; private set; }
        public ulong VendorDonateTooltip { get; private set; }
        public bool AllowActionDonate { get; private set; }
        public bool AllowActionRefresh { get; private set; }
        public bool IsCrafter { get; private set; }
        public ulong TypeName { get; private set; }
        public ulong VendorIconTooltip { get; private set; }
        public HUDEntityOverheadIcon InteractIndicator { get; private set; }
        public ulong VendorDonateTooltipMax { get; private set; }
        public ulong GlobalEvent { get; private set; }
        public bool AllowActionSell { get; private set; }
        public ulong VendorFlavorText { get; private set; }
        public bool IsRaidVendor { get; private set; }
        public ulong VendorLevelingCurve { get; private set; }
        public ulong ReputationDisplayInfo { get; private set; }
        public ulong[] CraftingRecipeCategories { get; private set; }
        public bool IsEnchanter { get; private set; }
        public ulong VendorRankTooltip { get; private set; }
    }
}
