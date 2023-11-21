using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class VendorXPBarTooltipPrototype : Prototype
    {
        public ulong NextRankTooltip;
        public ulong ThisRankTooltip;
        public VendorXPBarTooltipPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(VendorXPBarTooltipPrototype), proto); }
    }

    public class VendorInventoryEntryPrototype : Prototype
    {
        public ulong LootTable;
        public int UseStartingAtVendorLevel;
        public VendorXPBarTooltipPrototype VendorXPBarTooltip;
        public ulong PlayerInventory;
        public VendorInventoryEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(VendorInventoryEntryPrototype), proto); }
    }

    public class VendorTypePrototype : Prototype
    {
        public VendorInventoryEntryPrototype[] Inventories;
        public float VendorEnergyPctPerRefresh;
        public float VendorEnergyFullRechargeTimeMins;
        public ulong VendorXPTooltip;
        public ulong VendorRefreshTooltip;
        public ulong VendorDonateTooltip;
        public bool AllowActionDonate;
        public bool AllowActionRefresh;
        public bool IsCrafter;
        public ulong TypeName;
        public ulong VendorIconTooltip;
        public HUDEntityOverheadIcon InteractIndicator;
        public ulong VendorDonateTooltipMax;
        public ulong GlobalEvent;
        public bool AllowActionSell;
        public ulong VendorFlavorText;
        public bool IsRaidVendor;
        public ulong VendorLevelingCurve;
        public ulong ReputationDisplayInfo;
        public ulong[] CraftingRecipeCategories;
        public bool IsEnchanter;
        public ulong VendorRankTooltip;
        public VendorTypePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(VendorTypePrototype), proto); }
    }

}
