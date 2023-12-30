namespace MHServerEmu.Games.GameData.Prototypes
{
    public class VendorXPBarTooltipPrototype : Prototype
    {
        public ulong NextRankTooltip { get; protected set; }
        public ulong ThisRankTooltip { get; protected set; }
    }

    public class VendorInventoryEntryPrototype : Prototype
    {
        public ulong LootTable { get; protected set; }
        public int UseStartingAtVendorLevel { get; protected set; }
        public VendorXPBarTooltipPrototype VendorXPBarTooltip { get; protected set; }
        public ulong PlayerInventory { get; protected set; }
    }

    public class VendorTypePrototype : Prototype
    {
        public VendorInventoryEntryPrototype[] Inventories { get; protected set; }
        public float VendorEnergyPctPerRefresh { get; protected set; }
        public float VendorEnergyFullRechargeTimeMins { get; protected set; }
        public ulong VendorXPTooltip { get; protected set; }
        public ulong VendorRefreshTooltip { get; protected set; }
        public ulong VendorDonateTooltip { get; protected set; }
        public bool AllowActionDonate { get; protected set; }
        public bool AllowActionRefresh { get; protected set; }
        public bool IsCrafter { get; protected set; }
        public ulong TypeName { get; protected set; }
        public ulong VendorIconTooltip { get; protected set; }
        public HUDEntityOverheadIcon InteractIndicator { get; protected set; }
        public ulong VendorDonateTooltipMax { get; protected set; }
        public ulong GlobalEvent { get; protected set; }
        public bool AllowActionSell { get; protected set; }
        public ulong VendorFlavorText { get; protected set; }
        public bool IsRaidVendor { get; protected set; }
        public ulong VendorLevelingCurve { get; protected set; }
        public ulong ReputationDisplayInfo { get; protected set; }
        public ulong[] CraftingRecipeCategories { get; protected set; }
        public bool IsEnchanter { get; protected set; }
        public ulong VendorRankTooltip { get; protected set; }
    }
}
