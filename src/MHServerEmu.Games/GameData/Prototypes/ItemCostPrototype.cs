using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ProductPrototype : Prototype
    {
        public bool ForSale { get; protected set; }
    }

    public class CurrencyPrototype : Prototype
    {
        public LocaleStringId CostString { get; protected set; }
        public LocaleStringId DisplayName { get; protected set; }
        public AssetId Icon { get; protected set; }
        public LocaleStringId Tooltip { get; protected set; }
        public AssetId IconSmall { get; protected set; }
        public int MaxAmount { get; protected set; }
        public AssetId IconHiRes { get; protected set; }
        public CurveId LootBonusFlatCurve { get; protected set; }
        public CurveId LootBonusPctCurve { get; protected set; }
    }

    public class ItemCostComponentPrototype : Prototype
    {
        //---

        public virtual bool CanAffordItem(Player player, Item item)
        {
            return true;
        }

        public virtual int GetBuyPrice(Player player, Item item)
        {
            return 0;
        }

        public virtual bool PayItemCost(Player player, Item item)
        {
            return true;
        }
    }

    public class ItemCostCreditsPrototype : ItemCostComponentPrototype
    {
        public EvalPrototype Number { get; protected set; }
        public PrototypeId Currency { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override bool CanAffordItem(Player player, Item item)
        {
            int price = GetBuyPrice(player, item);
            PrototypeId creditsProtoRef = GameDatabase.CurrencyGlobalsPrototype.Credits;
            return player.Properties[PropertyEnum.Currency, creditsProtoRef] >= price;
        }

        public override int GetBuyPrice(Player player, Item item)
        {
            if (item.IsInBuybackInventory)
            {
                if (item.Properties.HasProperty(PropertyEnum.ItemSoldPrice))
                    return item.Properties[PropertyEnum.ItemSoldPrice];

                return GetSellPrice(player, item);
            }

            int price = GetNoStackBasePrice(player, item.ItemSpec, item) * item.CurrentStackSize;
            if (price <= 0)
                return price;

            float floatPrice = price;

            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;
            if (globalsProto?.ItemPriceMultiplierBuyFromVendor == null)
                return Logger.WarnReturn(price, "GetBuyPrice(): globalsProto?.ItemPriceMultiplierBuyFromVendor == null");

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, item.Properties);
            float globalItemBuyPriceMultiplier = Eval.RunFloat(globalsProto.ItemPriceMultiplierBuyFromVendor, evalContext);

            if (globalItemBuyPriceMultiplier >= 0f)
                floatPrice *= globalItemBuyPriceMultiplier;
            else
                Logger.Warn("GetBuyPrice(): globalItemBuyPriceMultiplier < 0f");

            floatPrice *= LiveTuningManager.GetLiveGlobalTuningVar(Gazillion.GlobalTuningVar.eGTV_VendorBuyPrice);

            return (int)floatPrice;
        }

        public override bool PayItemCost(Player player, Item item)
        {
            if (CanAffordItem(player, item) == false) return Logger.WarnReturn(false, "PayItemCost(): CanAffordItem(player, item) == false");

            int price = GetBuyPrice(player, item);
            PrototypeId creditsProtoRef = GameDatabase.CurrencyGlobalsPrototype.Credits;
            player.Properties.AdjustProperty(-price, new(PropertyEnum.Currency, creditsProtoRef));

            return true;
        }

        public int GetSellPrice(Player player, Item item)
        {
            return GetNoStackSellPrice(player, item.ItemSpec, item) * item.CurrentStackSize;
        }

        public int GetNoStackSellPrice(Player player, ItemSpec itemSpec, Item item)
        {
            int price = GetNoStackBasePrice(player, itemSpec, item);
            if (price <= 0)
                return price;

            float floatPrice = price;
            int itemLevel = item != null ? item.Properties[PropertyEnum.ItemLevel] : itemSpec.ItemLevel;

            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;
            if (globalsProto?.ItemPriceMultiplierSellToVendor == null)
                return Logger.WarnReturn(price, "GetNoStackSellPrice(): globalsProto?.ItemPriceMultiplierSellToVendor == null");

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetVar_Int(EvalContext.Var1, itemLevel);
            float globalItemSellPriceMultiplier = Eval.RunFloat(globalsProto.ItemPriceMultiplierSellToVendor, evalContext);

            if (globalItemSellPriceMultiplier >= 0f)
                floatPrice *= globalItemSellPriceMultiplier;
            else
                Logger.Warn("GetNoStackSellPrice(): globalItemSellPriceMultiplier < 0f");

            floatPrice *= LiveTuningManager.GetLiveGlobalTuningVar(Gazillion.GlobalTuningVar.eGTV_VendorSellPrice);

            return (int)floatPrice;
        }

        private int GetNoStackBasePrice(Player player, ItemSpec itemSpec, Item item)
        {
            RarityPrototype rarityProto = GameDatabase.GetPrototype<RarityPrototype>(itemSpec.RarityProtoRef);
            int rarityTier = rarityProto != null ? rarityProto.Tier : 0;
            int numAffixes = itemSpec.AffixSpecs.Count;
            int itemLevel = item != null ? item.Properties[PropertyEnum.ItemLevel] : itemSpec.ItemLevel;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, player.Properties);
            evalContext.SetVar_Int(EvalContext.Var1, rarityTier);
            evalContext.SetVar_Int(EvalContext.Var2, numAffixes);
            evalContext.SetVar_Int(EvalContext.Var3, itemLevel);

            return Eval.RunInt(Number, evalContext);
        }
    }

    public class ItemCostLegendaryMarksPrototype : ItemCostComponentPrototype
    {
        public EvalPrototype Number { get; protected set; }
        public EvalPrototype NumberExt { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override bool CanAffordItem(Player player, Item item)
        {
            int count = GetCount(player, item);
            PrototypeId legendaryMarksProtoRef = GameDatabase.CurrencyGlobalsPrototype.LegendaryMarks;
            return player.Properties[PropertyEnum.Currency, legendaryMarksProtoRef] >= count;
        }

        public override int GetBuyPrice(Player player, Item item)
        {
            return GetCount(player, item);
        }

        public override bool PayItemCost(Player player, Item item)
        {
            if (CanAffordItem(player, item) == false) return Logger.WarnReturn(false, "PayItemCost(): CanAffordItem(player, item) == false");

            int price = GetCount(player, item);
            PrototypeId legendaryMarksProtoRef = GameDatabase.CurrencyGlobalsPrototype.LegendaryMarks;
            player.Properties.AdjustProperty(-price, new(PropertyEnum.Currency, legendaryMarksProtoRef));

            return true;
        }

        public int GetCount(Player player, Item item)
        {
            ItemSpec itemSpec = item.ItemSpec;
            RarityPrototype rarityProto = itemSpec.RarityProtoRef.As<RarityPrototype>();
            int rarityTier = rarityProto != null ? rarityProto.Tier : 0;
            int numAffixes = itemSpec.AffixSpecs.Count;

            Avatar avatar = player.CurrentAvatar;
            if (avatar == null)
                return 0;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, player.Properties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, item.Properties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, avatar.Properties);
            evalContext.SetVar_Int(EvalContext.Var1, rarityTier);
            evalContext.SetVar_Int(EvalContext.Var2, numAffixes);

            int count = NumberExt != null
                ? Eval.RunInt(NumberExt, evalContext)
                : Eval.RunInt(Number, evalContext);

            return count * item.CurrentStackSize;
        }
    }

    public class ItemCostRunestonesPrototype : ItemCostComponentPrototype
    {
        public EvalPrototype Number { get; protected set; }
        public EvalPrototype NumberExt { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override bool CanAffordItem(Player player, Item item)
        {
            int count = GetCount(player, item);
            return player.Properties[PropertyEnum.RunestonesAmount] >= count;
        }

        public override int GetBuyPrice(Player player, Item item)
        {
            return GetCount(player, item);
        }

        public override bool PayItemCost(Player player, Item item)
        {
            if (CanAffordItem(player, item) == false) return Logger.WarnReturn(false, "PayItemCost(): CanAffordItem(player, item) == false");

            int count = GetCount(player, item);
            player.Properties.AdjustProperty(-count, PropertyEnum.RunestonesAmount);
            return true;
        }

        public int GetCount(Player player, Item item)
        {
            ItemSpec itemSpec = item.ItemSpec;
            RarityPrototype rarityProto = itemSpec.RarityProtoRef.As<RarityPrototype>();
            int rarityTier = rarityProto != null ? rarityProto.Tier : 0;
            int numAffixes = itemSpec.AffixSpecs.Count;

            Avatar avatar = player.CurrentAvatar;
            if (avatar == null)
                return 0;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, player.Properties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, item.Properties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, avatar.Properties);
            evalContext.SetVar_Int(EvalContext.Var1, rarityTier);
            evalContext.SetVar_Int(EvalContext.Var2, numAffixes);

            int count = NumberExt != null
                ? Eval.RunInt(NumberExt, evalContext)
                : Eval.RunInt(Number, evalContext);

            return count * item.CurrentStackSize;
        }
    }

    public class ItemCostItemStackPrototype : ItemCostComponentPrototype
    {
        public PrototypeId CurrencyItem { get; protected set; }
        public EvalPrototype Number { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override bool CanAffordItem(Player player, Item item)
        {
            int price = GetBuyPrice(player, item);

            InventoryIterationFlags flags = InventoryIterationFlags.PlayerGeneral | InventoryIterationFlags.PlayerGeneralExtra | InventoryIterationFlags.PlayerStashGeneral;
            int numContained = InventoryIterator.GetMatchingContained(player, CurrencyItem, flags);
            return numContained >= price;
        }

        public override int GetBuyPrice(Player player, Item item)
        {
            ItemSpec itemSpec = item.ItemSpec;
            RarityPrototype rarityProto = itemSpec.RarityProtoRef.As<RarityPrototype>();
            int rarityTier = rarityProto != null ? rarityProto.Tier : 0;
            int numAffixes = itemSpec.AffixSpecs.Count;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, player.Properties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, item.Properties);
            evalContext.SetVar_Int(EvalContext.Var1, rarityTier);
            evalContext.SetVar_Int(EvalContext.Var2, numAffixes);

            return Eval.RunInt(Number, evalContext) * item.CurrentStackSize;
        }

        public override bool PayItemCost(Player player, Item item)
        {
            if (CanAffordItem(player, item) == false) return Logger.WarnReturn(false, "PayItemCost(): CanAffordItem(player, item) == false");

            int price = GetBuyPrice(player, item);

            List<ulong> currencyItemList = ListPool<ulong>.Instance.Rent();
            InventoryIterationFlags flags = InventoryIterationFlags.PlayerGeneral | InventoryIterationFlags.PlayerGeneralExtra | InventoryIterationFlags.PlayerStashGeneral;
            InventoryIterator.GetMatchingContained(player, CurrencyItem, flags, currencyItemList);

            EntityManager entityManager = player.Game.EntityManager;
            int remaining = price;

            foreach (ulong currencyItemId in currencyItemList)
            {
                Item currencyItem = entityManager.GetEntity<Item>(currencyItemId);
                if (currencyItem == null)
                {
                    Logger.Warn("PayItemCost(): currencyItem == null");
                    continue;
                }

                int numToSpend = Math.Min(remaining, item.CurrentStackSize);
                remaining -= numToSpend;
                item.DecrementStack(numToSpend);
            }

            ListPool<ulong>.Instance.Return(currencyItemList);

            if (remaining != 0)
                return Logger.WarnReturn(false, $"PayItemCost(): Player [{player}] was not able to spend enough currency item {CurrencyItem.GetName()} to pay for [{item}]");

            return true;
        }
    }

    public class ItemCostCurrencyPrototype : ItemCostComponentPrototype
    {
        public PrototypeId Currency { get; protected set; }
        public EvalPrototype Amount { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public override bool CanAffordItem(Player player, Item item)
        {
            if (Currency == PrototypeId.Invalid) return Logger.WarnReturn(false, "CanAffordItem(): Currency == PrototypeId.Invalid");

            int price = GetBuyPrice(player, item);
            return player.Properties[PropertyEnum.Currency, Currency] >= price;
        }

        public override int GetBuyPrice(Player player, Item item)
        {
            ItemSpec itemSpec = item.ItemSpec;
            RarityPrototype rarityProto = itemSpec.RarityProtoRef.As<RarityPrototype>();
            int rarityTier = rarityProto != null ? rarityProto.Tier : 0;
            int numAffixes = itemSpec.AffixSpecs.Count;

            // Check for live tuning price override
            if (Currency == GameDatabase.CurrencyGlobalsPrototype.EternitySplinters)
            {
                int liveTuneCost = item.ItemPrototype.LiveTuneEternitySplinterCost;
                if (liveTuneCost != 1)
                    return liveTuneCost;
            }

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, player.Properties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, item.Properties);
            evalContext.SetVar_Int(EvalContext.Var1, rarityTier);
            evalContext.SetVar_Int(EvalContext.Var2, numAffixes);

            return Eval.RunInt(Amount, evalContext) * item.CurrentStackSize;
        }

        public override bool PayItemCost(Player player, Item item)
        {
            if (Currency == PrototypeId.Invalid) return Logger.WarnReturn(false, "PayItemCost(): Currency == PrototypeId.Invalid");
            if (CanAffordItem(player, item) == false) return Logger.WarnReturn(false, "PayItemCost(): CanAffordItem(player, item) == false");

            int price = GetBuyPrice(player, item);
            player.Properties.AdjustProperty(-price, new(PropertyEnum.Currency, Currency));

            return true;
        }
    }

    public class ItemCostPrototype : Prototype
    {
        public ItemCostComponentPrototype[] Components { get; protected set; }
        public EvalPrototype Credits { get; protected set; }
        public EvalPrototype Runestones { get; protected set; }

        //---

        public bool CanAffordItem(Player player, Item item)
        {
            // it's free real estate
            if (Components.IsNullOrEmpty())
                return true;

            foreach (ItemCostComponentPrototype componentProto in Components)
            {
                if (componentProto.CanAffordItem(player, item) == false)
                    return false;
            }

            return true;
        }

        public bool PayItemCost(Player player, Item item)
        {
            if (Components.IsNullOrEmpty())
                return true;

            if (CanAffordItem(player, item) == false)
                return false;

            bool success = true;

            foreach (ItemCostComponentPrototype componentProto in Components)
                success &= componentProto.PayItemCost(player, item);

            return success;
        }

        public int GetSellPriceInCredits(Player player, Item item)
        {
            return GetNoStackSellPriceInCredits(player, item.ItemSpec, item) * item.CurrentStackSize;
        }

        public int GetNoStackSellPriceInCredits(Player player, ItemSpec itemSpec, Item item)
        {
            int price = 0;

            if (Components.HasValue())
            {
                foreach (ItemCostComponentPrototype componentProto in Components)
                {
                    if (componentProto is not ItemCostCreditsPrototype creditsComponentProto)
                        continue;

                    price += creditsComponentProto.GetNoStackSellPrice(player, itemSpec, item);
                }
            }

            return price;
        }

        public bool HasEternitySplintersComponent()
        {
            if (Components.IsNullOrEmpty())
                return false;

            PrototypeId eternitySplintersProtoRef = GameDatabase.CurrencyGlobalsPrototype.EternitySplinters;

            foreach (ItemCostComponentPrototype componentProto in Components)
            {
                if (componentProto is not ItemCostCurrencyPrototype currencyComponentProto)
                    continue;

                if (currencyComponentProto.Currency == eternitySplintersProtoRef)
                    return true;
            }

            return false;
        }

        public int GetBuyPriceInEternitySplinters(Player player, Item item)
        {
            int price = 0;

            if (Components.IsNullOrEmpty())
                return price;

            PrototypeId eternitySplintersProtoRef = GameDatabase.CurrencyGlobalsPrototype.EternitySplinters;

            foreach (ItemCostComponentPrototype componentProto in Components)
            {
                if (componentProto is not ItemCostCurrencyPrototype currencyComponentProto)
                    continue;

                if (currencyComponentProto.Currency == eternitySplintersProtoRef)
                    price += currencyComponentProto.GetBuyPrice(player, item);
            }

            return price;
        }
    }
}
