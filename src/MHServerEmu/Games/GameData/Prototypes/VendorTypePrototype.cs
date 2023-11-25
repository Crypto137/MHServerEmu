namespace MHServerEmu.Games.GameData.Prototypes
{
    public class VendorXPBarTooltipPrototype : Prototype
    {
        public ulong NextRankTooltip { get; set; }
        public ulong ThisRankTooltip { get; set; }
    }

    public class VendorInventoryEntryPrototype : Prototype
    {
        public ulong LootTable { get; set; }
        public int UseStartingAtVendorLevel { get; set; }
        public VendorXPBarTooltipPrototype VendorXPBarTooltip { get; set; }
        public ulong PlayerInventory { get; set; }
    }

    public class VendorTypePrototype : Prototype
    {
        public VendorInventoryEntryPrototype[] Inventories { get; set; }
        public float VendorEnergyPctPerRefresh { get; set; }
        public float VendorEnergyFullRechargeTimeMins { get; set; }
        public ulong VendorXPTooltip { get; set; }
        public ulong VendorRefreshTooltip { get; set; }
        public ulong VendorDonateTooltip { get; set; }
        public bool AllowActionDonate { get; set; }
        public bool AllowActionRefresh { get; set; }
        public bool IsCrafter { get; set; }
        public ulong TypeName { get; set; }
        public ulong VendorIconTooltip { get; set; }
        public HUDEntityOverheadIcon InteractIndicator { get; set; }
        public ulong VendorDonateTooltipMax { get; set; }
        public ulong GlobalEvent { get; set; }
        public bool AllowActionSell { get; set; }
        public ulong VendorFlavorText { get; set; }
        public bool IsRaidVendor { get; set; }
        public ulong VendorLevelingCurve { get; set; }
        public ulong ReputationDisplayInfo { get; set; }
        public ulong[] CraftingRecipeCategories { get; set; }
        public bool IsEnchanter { get; set; }
        public ulong VendorRankTooltip { get; set; }
    }
}
