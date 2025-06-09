using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;

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

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public bool ContainsInventory(PrototypeId inventoryProtoRef)
        {
            if (inventoryProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "ContainsInventory(): inventoryProtoRef == PrototypeId.Invalid");

            if (Inventories.IsNullOrEmpty())
                return false;

            foreach (VendorInventoryEntryPrototype inventoryEntry in Inventories)
            {
                if (inventoryEntry.PlayerInventory == inventoryProtoRef)
                    return true;
            }

            return false;
        }

        public bool GetInventories(List<PrototypeId> inventoryList)
        {
            inventoryList.Clear();

            if (Inventories.IsNullOrEmpty())
                return false;

            foreach (VendorInventoryEntryPrototype inventoryEntry in Inventories)
            {
                PrototypeId inventoryProtoRef = inventoryEntry.PlayerInventory;
                if (inventoryProtoRef != PrototypeId.Invalid && inventoryList.Contains(inventoryProtoRef) == false)
                    inventoryList.Add(inventoryProtoRef);
            }

            return inventoryList.Count > 0;
        }

        public bool ContainsCraftingRecipeCategory(PrototypeId recipeCategoryProtoRef)
        {
            if (CraftingRecipeCategories.IsNullOrEmpty())
                return false;

            foreach (PrototypeId itRecipeCategory in CraftingRecipeCategories)
            {
                if (itRecipeCategory == recipeCategoryProtoRef)
                    return true;
            }

            return false;
        }
    }
}
