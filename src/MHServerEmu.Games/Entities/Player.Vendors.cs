using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public enum VendorResult    // Names from CPlayer::BuyItemFromVendor(), CPlayer::SellItemToVendor(), CPlayer::DonateItemToVendor()
    {
        BuySuccess,
        BuyFailure,
        BuyOutOfRange,
        BuyInsufficientCredits,
        BuyInsufficientPrestige,
        BuyCannotAffordItem,
        BuyInventoryFull,
        BuyNotInVendorInventory,
        BuyDialogTargetIdMismatch,
        BuyAvatarUltimateAlreadyMaxedOut,
        BuyAvatarUltimateUpgradeCurrentOnly,
        BuyCharacterAlreadyUnlocked,
        BuyPlayerAlreadyHasCraftingRecipe,
        BuyItemDisabledByLiveTuning,

        SellSuccess,
        SellFailure,

        DonateSuccess,
        DonateFailure,
        DonateNotAcceptingDonations,
        DonateNotAcceptingItem,

        RefreshSuccess,
        RefreshFailure,
        RefreshInsufficientEnergy,
        RefreshNotAllowed,

        UnkResult24,
        UnkResult25,

        OpSuccess,
        OpFailure,
    }

    public partial class Player
    {
        private const int VendorMinLevel = 1;
        private const int VendorInvalidXP = -1;

        private readonly HashSet<PrototypeId> _initializedVendorTypeProtoRefs = new();
        private readonly Dictionary<PrototypeId, VendorPurchaseData> _vendorPurchaseDataDict = new();   // InventoryPrototype key

        public void InitializeVendorInventory(PrototypeId inventoryProtoRef)
        {
            foreach (PrototypeId vendorTypeProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy<VendorTypePrototype>(PrototypeIterateFlags.NoAbstract))
            {
                VendorTypePrototype vendorTypeProto = vendorTypeProtoRef.As<VendorTypePrototype>();
                if (vendorTypeProto.ContainsInventory(inventoryProtoRef))
                {
                    RollVendorInventory(vendorTypeProto, true);
                    return;
                }
            }
        }

        public bool AwardVendorXP(int amount, PrototypeId vendorTypeProtoRef, ulong vendorId = InvalidId)
        {
            if (amount <= 0) return Logger.WarnReturn(false, "AwardVendorXP(): amount <= 0");

            VendorTypePrototype vendorTypeProto = vendorTypeProtoRef.As<VendorTypePrototype>();
            if (vendorTypeProto == null) return Logger.WarnReturn(false, "AwardVendorXP(): vendorTypeProto == null");

            Properties.AdjustProperty(amount, new(PropertyEnum.VendorXP, vendorTypeProtoRef));
            TryLevelUpVendor(vendorTypeProto, vendorId, false);
            return true;
        }

        public bool TryDoVendorXPCapRollover(VendorXPCapInfoPrototype vendorXPCapInfoProto)
        {
            if (vendorXPCapInfoProto == null) return Logger.WarnReturn(false, "TryDoVendorXPCapRollover(): vendorXPCapInfoProto == null");
            PrototypeId vendorTypeProtoRef = vendorXPCapInfoProto.Vendor;

            // Find out when the current rollover happened
            using PropertyCollection rolloverProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            rolloverProperties[PropertyEnum.LootCooldownRolloverWallTime, 0, (PropertyParam)vendorXPCapInfoProto.WallClockTimeDay] = vendorXPCapInfoProto.WallClockTime24Hr;
            LootUtilities.GetLastLootCooldownRolloverWallTime(rolloverProperties, Clock.UnixTime + TimeSpan.FromDays(7), out TimeSpan lastRolloverTime);

            // Reset the cap if the current rollover happened after the last recorded one
            if (lastRolloverTime > Properties[PropertyEnum.VendorXPCapRollOverTime, vendorTypeProtoRef])
            {
                Properties[PropertyEnum.VendorXPCapCounter, vendorTypeProtoRef] = 0;
                Properties[PropertyEnum.VendorXPCapRollOverTime, vendorTypeProtoRef] = lastRolloverTime;
            }

            // Still the same rollover, no changes needed
            return false;
        }

        public void TryDoVendorXPCapRollover()
        {
            VendorXPCapInfoPrototype[] infos = GameDatabase.LootGlobalsPrototype.VendorXPCapInfo;
            if (infos.IsNullOrEmpty()) return;

            foreach (VendorXPCapInfoPrototype vendorXPCapInfoProto in GameDatabase.LootGlobalsPrototype.VendorXPCapInfo)
                TryDoVendorXPCapRollover(vendorXPCapInfoProto);
        }

        public bool BuyItemFromVendor(int avatarIndex, ulong itemId, ulong vendorId, uint destinationSlot)
        {
            if (CanBuyItemFromVendor(avatarIndex, itemId, vendorId) != VendorResult.BuySuccess)
                return false;

            EntityManager entityManager = Game.EntityManager;

            Item item = entityManager.GetEntity<Item>(itemId);
            if (item == null) return Logger.WarnReturn(false, "BuyItemFromVendor(): item == null");

            ItemPrototype itemProto = item.ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, "BuyItemFromVendor(): itemProto == null");

            WorldEntity vendor = entityManager.GetEntity<WorldEntity>(vendorId);
            if (vendor == null) return Logger.WarnReturn(false, "BuyItemFromVendor(): vendor == null");

            // Find the inventory slot to put the purchased item in
            uint vendorSlot = Inventory.InvalidSlot;
            bool isInBuybackInventory = item.IsInBuybackInventory;
            VendorPurchaseData purchaseData = null;

            if (isInBuybackInventory == false)
            {
                // Get purchase data for non-buyback items
                purchaseData = GetVendorPurchaseData(item.InventoryLocation.InventoryRef, false);
                if (purchaseData == null) return Logger.WarnReturn(false, "BuyItemFromVendor(): purchaseData == null");

                vendorSlot = item.InventoryLocation.Slot;
                if (purchaseData.HasItemBeenPurchased(vendorSlot)) return Logger.WarnReturn(false, "BuyItemFromVendor(): purchaseData.HasItemBeenPurchased(vendorSlot)");
            }

            Inventory destinationInventory = GetInventory(itemProto.DestinationFromVendor);
            if (destinationInventory == null) return Logger.WarnReturn(false, "BuyItemFromVendor(): destinationInventory == null");

            if (destinationSlot == Inventory.InvalidSlot)
                destinationSlot = destinationInventory.GetFreeSlot(item, true);

            if (destinationSlot == Inventory.InvalidSlot) return Logger.WarnReturn(false, "BuyItemFromVendor(): destinationSlot == Inventory.InvalidSlot");

            // Some items are cloned when they are purchased
            bool isCloning = item.IsClonedWhenPurchasedFromVendor && isInBuybackInventory == false;
            if (isCloning)
            {
                // Create a clone
                using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
                settings.EntityRef = item.PrototypeDataRef;
                settings.ItemSpec = new(item.ItemSpec);
                settings.InventoryLocation = new(Id, destinationInventory.PrototypeDataRef, destinationSlot);

                if (IsInGame == false)
                    settings.OptionFlags &= ~EntitySettingsOptionFlags.EnterGame;

                item = entityManager.CreateEntity(settings) as Item;
            }

            // Pay the cost of the item. We need to do this before we move the item because
            // item sold prices are cleared when items are removed from the buyback inventory.
            itemProto.Cost?.PayItemCost(this, item);

            if (isCloning == false)
            {
                // Move the item to the player's inventory and record the purchase if we are not cloning
                if (item.ChangeInventoryLocation(destinationInventory, destinationSlot) != InventoryResult.Success)
                    return Logger.WarnReturn(false, $"BuyItemFromVendor(): Failed to put purchased item [{item}] into inventory of player [{this}]");

                if (isInBuybackInventory == false && purchaseData != null)
                    purchaseData.RecordItemPurchase(vendorSlot);
            }

            // Use the item if needed
            if (item.Properties[PropertyEnum.ItemAutoUseOnPurchase])
            {
                Avatar avatar = GetActiveAvatarByIndex(avatarIndex);
                avatar?.UseInteractableObject(item.Id, PrototypeId.Invalid);
            }

            return true;
        }

        public bool SellItemToVendor(int avatarIndex, ulong itemId, ulong vendorId)
        {
            if (CanSellItemToVendor(avatarIndex, itemId, vendorId) != VendorResult.SellSuccess)
                return false;

            Item item = Game?.EntityManager.GetEntity<Item>(itemId);
            if (item == null) return Logger.WarnReturn(false, "SellItemToVendor(): item == null");

            int sellPrice = (int)item.GetSellPrice(this);
            if (ValidateItemSellPrice(item, sellPrice) == false)
                return false;

            Inventory buybackInventory = GetInventoryByRef(GameDatabase.GlobalsPrototype.VendorBuybackInventory);
            if (buybackInventory == null) return Logger.WarnReturn(false, "SellItemToVendor(): buybackInventory == false");

            // Find a free slot in the buyback inventory to put this item in
            uint lastSlot = (uint)buybackInventory.GetCapacity() - 1;
            uint freeSlot = Inventory.InvalidSlot;
            for (uint i = 0; i <= lastSlot; i++)
            {
                if (buybackInventory.IsSlotFree(i))
                {
                    freeSlot = i;
                    break;
                }
            }

            // If no free slot is found, free space for the item we are selling
            if (freeSlot == Inventory.InvalidSlot)
            {
                ulong lastEntityId = buybackInventory.GetEntityInSlot(lastSlot);
                Entity lastEntity = Game.EntityManager.GetEntity<Entity>(lastEntityId);
                if (lastEntity == null)
                    Logger.Warn("SellItemToVendor(): lastEntity == null");
                else
                    lastEntity.Destroy();

                freeSlot = lastSlot;
            }

            // Shift all items by 1 slot so that our newly sold item appears at the top of the list
            ulong? stackEntityId = null;

            for (uint i = freeSlot; i > 0; i--)
            {
                ulong entityToShiftId = buybackInventory.GetEntityInSlot(i - 1);
                Entity entityToShift = Game.EntityManager.GetEntity<Entity>(entityToShiftId);
                if (entityToShift == null)
                {
                    Logger.Warn("SellItemToVendor(): entityToShift == null");
                    continue;
                }

                Inventory.ChangeEntityInventoryLocation(entityToShift, buybackInventory, i, ref stackEntityId, false);
            }

            // Put our newly sold item in the first slot
            if (buybackInventory.IsSlotFree(0) == false)
                return Logger.WarnReturn(false, "SellItemToVendor(): buybackInventory.IsSlotFree(0) == false");

            stackEntityId = null;
            if (Inventory.ChangeEntityInventoryLocation(item, buybackInventory, 0, ref stackEntityId, false) != InventoryResult.Success)
                return Logger.WarnReturn(false, $"SellItemToVendor(): Failed to add item {item} to the buyback inventory");

            // Sell successful, record sell price and add credits
            item.Properties[PropertyEnum.ItemSoldPrice] = sellPrice;

            PrototypeId creditsProtoRef = GameDatabase.CurrencyGlobalsPrototype.Credits;
            Properties.AdjustProperty(sellPrice, new(PropertyEnum.Currency, creditsProtoRef));

            return true;
        }

        public bool DonateItemToVendor(int avatarIndex, ulong itemId, ulong vendorId)
        {
            if (CanDonateItemToVendor(avatarIndex, itemId, vendorId) != VendorResult.DonateSuccess)
                return false;

            Item item = Game?.EntityManager.GetEntity<Item>(itemId);
            if (item == null) return Logger.WarnReturn(false, "DonateItemToVendor(): item == null");

            WorldEntity vendor = Game.EntityManager.GetEntity<WorldEntity>(vendorId);
            if (vendor == null) return Logger.WarnReturn(false, "DonateItemToVendor(): vendor == null");

            PrototypeId vendorTypeProtoRef = vendor.Properties[PropertyEnum.VendorType];
            VendorTypePrototype vendorTypeProto = vendorTypeProtoRef.As<VendorTypePrototype>();
            if (vendorTypeProto == null) return Logger.WarnReturn(false, "DonateItemToVendor(): vendorTypeProto == null");

            // TODO: vendor.IsGlobalEventVendor for Events/GlobalEvents/Events/BiFrostUnlock/BifrostUnlock.prototype

            if (IsVendorMaxLevel(vendorTypeProto) == false)
            {
                uint vendorXPGain = item.GetVendorXPGain(vendor, this);
                item.Destroy();
                AwardVendorXP((int)vendorXPGain, vendorTypeProtoRef, vendorId);
            }
            else
            {
                // Convert item to credits if this vendor is already at max level
                uint sellPrice = item.GetSellPrice(this);
                item.Destroy();
                Properties.AdjustProperty((int)sellPrice, new(PropertyEnum.Currency, GameDatabase.CurrencyGlobalsPrototype.Credits));
            }

            return true;
        }

        public bool RefreshVendorInventory(ulong vendorId)
        {
            if (CanRefreshVendorInventory(vendorId) != VendorResult.RefreshSuccess)
                return false;

            WorldEntity vendor = Game.EntityManager.GetEntity<WorldEntity>(vendorId);
            if (vendor == null) return Logger.WarnReturn(false, "RefreshVendorInventory(): vendor == null");

            PrototypeId vendorTypeProtoRef = vendor.Properties[PropertyEnum.VendorType];
            if (vendorTypeProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "RefreshVendorInventory(): vendorTypeProtoRef == PrototypeId.Invalid");

            return RefreshVendorInventoryInternal(vendorTypeProtoRef);
        }

        public bool HasLearnedCraftingRecipe(PrototypeId craftingRecipeProtoRef)
        {
            // Assume all recipes are learned for now
            return true;
        }

        private void InitializeVendors()
        {
            foreach (PrototypeId vendorTypeProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy<VendorTypePrototype>(PrototypeIterateFlags.NoAbstract))
            {
                // Vendor level is not persisted, so it can be used to check if a vendor has been initialized already
                if (Properties[PropertyEnum.VendorLevel, vendorTypeProtoRef] != 0)
                    continue;

                VendorTypePrototype vendorTypeProto = vendorTypeProtoRef.As<VendorTypePrototype>();
                TryLevelUpVendor(vendorTypeProto, InvalidId, true);

                if (Properties[PropertyEnum.VendorLevel, vendorTypeProtoRef] == 0)
                {
                    Logger.Warn($"InitializeVendors(): Failed to initialize vendor level for vendor type {vendorTypeProto}");
                    continue;
                }

                if (Properties[PropertyEnum.VendorRollSeed, vendorTypeProtoRef] == 0)
                {
                    UpdateVendorLootProperties(vendorTypeProto);
                    SetVendorEnergyPct(vendorTypeProtoRef, 1f);
                }
            }
        }

        private bool TryLevelUpVendor(VendorTypePrototype vendorTypeProto, ulong vendorId, bool isInitializing)
        {
            if (vendorTypeProto == null) return Logger.WarnReturn(false, "TryLevelUpVendor(): vendorTypeProto == null");
            PrototypeId vendorTypeProtoRef = vendorTypeProto.DataRef;

            if (CalculateVendorLevel(vendorTypeProto, out int newLevel) == false)
                return true;

            int oldLevel = Properties[PropertyEnum.VendorLevel, vendorTypeProtoRef];
            Properties[PropertyEnum.VendorLevel, vendorTypeProtoRef] = newLevel;

            if (isInitializing || oldLevel == newLevel)
                return true;

            return OnVendorLevelUp(vendorTypeProto, vendorId, newLevel);
        }

        private bool CalculateVendorLevel(VendorTypePrototype vendorTypeProto, out int newLevel)
        {
            newLevel = 0;

            if (vendorTypeProto == null) return Logger.WarnReturn(false, "CalculateVendorLevel(): vendorTypeProto == null");
            PrototypeId vendorTypeProtoRef = vendorTypeProto.DataRef;

            Curve levelingCurve = GetVendorLevelingCurve(vendorTypeProto);
            if (levelingCurve == null) return Logger.WarnReturn(false, "CalculateVendorLevel(): levelingCurve == null");

            int maxLevel = GetVendorMaxLevel(vendorTypeProto);
            int oldLevel = Properties[PropertyEnum.VendorLevel, vendorTypeProtoRef];
            int nextLevel = levelingCurve.MinPosition + 1;

            int xp = Properties[PropertyEnum.VendorXP, vendorTypeProtoRef];
            int prevXPRequirement = 0;
            int nextXPRequirement = GetVendorXPRequirement(vendorTypeProto, nextLevel);

            // Each next xp requirement has to big larger than the previous one, or things are going to break
            while (nextXPRequirement <= xp && nextXPRequirement > prevXPRequirement && nextLevel <= maxLevel)
            {
                prevXPRequirement = nextXPRequirement;

                if (++nextLevel > maxLevel)
                    break;

                nextXPRequirement = GetVendorXPRequirement(vendorTypeProto, nextLevel);
            }

            newLevel = nextLevel - 1;

            return newLevel != oldLevel;
        }

        private Curve GetVendorLevelingCurve(VendorTypePrototype vendorTypeProto)
        {
            if (vendorTypeProto == null) return Logger.WarnReturn<Curve>(null, "GetVendorLevelingCurve(): vendorTypeProto == null");

            CurveId vendorLevelingCurveId = vendorTypeProto.VendorLevelingCurve;
            if (vendorLevelingCurveId == CurveId.Invalid) return Logger.WarnReturn<Curve>(null, "GetVendorLevelingCurve(): vendorLevelingCurveId == CurveId.Invalid");

            return CurveDirectory.Instance.GetCurve(vendorLevelingCurveId);
        }

        private int GetVendorMaxLevel(VendorTypePrototype vendorTypeProto)
        {
            if (vendorTypeProto == null) return Logger.WarnReturn(0, "GetVendorMaxLevel(): vendorTypeProto == null");
            PrototypeId vendorTypeProtoRef = vendorTypeProto.DataRef;

            int currentLevel = Properties[PropertyEnum.VendorLevel, vendorTypeProtoRef];

            Curve levelingCurve = GetVendorLevelingCurve(vendorTypeProto);
            if (levelingCurve == null) return Logger.WarnReturn(0, "GetVendorMaxLevel(): levelingCurve == null");

            // No more valid data in the curve = we are at max level
            int nextLevel = currentLevel + 1;
            if (nextLevel <= levelingCurve.MaxPosition && levelingCurve.GetIntAt(nextLevel) < 0)
                return currentLevel;

            return levelingCurve.MaxPosition;
        }

        private bool IsVendorMaxLevel(VendorTypePrototype vendorTypeProto)
        {
            return GetVendorMaxLevel(vendorTypeProto) == Properties[PropertyEnum.VendorLevel, vendorTypeProto.DataRef];
        }

        private int GetVendorXPRequirement(VendorTypePrototype vendorTypeProto, int level)
        {
            if (vendorTypeProto == null) return Logger.WarnReturn(VendorInvalidXP, "GetVendorXPRequirement(): vendorTypeProto == null");
            if (level < VendorMinLevel) return Logger.WarnReturn(VendorInvalidXP, "GetVendorXPRequirement(): level < VendorMinLevel");

            Curve levelingCurve = GetVendorLevelingCurve(vendorTypeProto);
            if (levelingCurve == null) return Logger.WarnReturn(VendorInvalidXP, "GetVendorXPRequirement(): levelingCurve == null");

            if (level < levelingCurve.MinPosition || level > levelingCurve.MaxPosition)
                return Logger.WarnReturn(VendorInvalidXP, "GetVendorXPRequirement(): level < levelingCurve.MinPosition || level > levelingCurve.MaxPosition");

            return levelingCurve.GetIntAt(level);
        }

        private bool OnVendorLevelUp(VendorTypePrototype vendorTypeProto, ulong vendorId, int newLevel)
        {
            if (vendorTypeProto == null) return Logger.WarnReturn(false, "OnVendorLevelUp(): vendorTypeProto == null");
            PrototypeId vendorTypeProtoRef = vendorTypeProto.DataRef;

            SetVendorEnergyPct(vendorTypeProtoRef, 1f);

            if (vendorTypeProto.IsCrafter)
            {
                UpdateVendorLootProperties(vendorTypeProto);
                RollVendorInventory(vendorTypeProto, false);
            }

            if (vendorId != InvalidId)
                SendMessage(NetMessageVendorLevelUp.CreateBuilder().SetVendorTypeProtoId((ulong)vendorTypeProtoRef).SetVendorID(vendorId).Build());

            return true;
        }

        private bool UpdateVendorLootProperties(VendorTypePrototype vendorTypeProto)
        {
            if (vendorTypeProto == null) return Logger.WarnReturn(false, "UpdateVendorLootProperties(): vendorTypeProto == null");
            PrototypeId vendorTypeProtoRef = vendorTypeProto.DataRef;

            // VendorRollAvatar / VendorRollLevel
            Avatar avatar = CurrentAvatar;

            if (avatar != null)
            {
                Properties[PropertyEnum.VendorRollAvatar, vendorTypeProto.DataRef] = avatar.PrototypeDataRef;
                Properties[PropertyEnum.VendorRollLevel, vendorTypeProto.DataRef] = avatar.CharacterLevel;
            }
            else
            {
                Properties[PropertyEnum.VendorRollAvatar, vendorTypeProto.DataRef] = PrototypeId.Invalid;
                Properties[PropertyEnum.VendorRollLevel, vendorTypeProto.DataRef] = 1;
            }

            // VendorRollSeed
            int oldSeed = Properties[PropertyEnum.VendorRollSeed, vendorTypeProtoRef];
            int newSeed = oldSeed;

            while (newSeed == oldSeed || newSeed == 0)
                newSeed = Game.Random.Next();

            Properties[PropertyEnum.VendorRollSeed, vendorTypeProtoRef] = newSeed;

            // VendorRollTableLevel
            int tableLevel;
            if (vendorTypeProto.IsCrafter == false)
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, avatar?.Properties);
                evalContext.SetReadOnlyVar_ProtoRef(EvalContext.Var1, vendorTypeProtoRef);
                tableLevel = Eval.RunInt(GameDatabase.AdvancementGlobalsPrototype.VendorRollTableLevelEval, evalContext);
            }
            else
            {
                // Table level is equal to vendor level for crafting vendors
                tableLevel = Properties[PropertyEnum.VendorLevel, vendorTypeProtoRef];
            }

            Properties[PropertyEnum.VendorRollTableLevel, vendorTypeProtoRef] = tableLevel;

            // Reset purchase data, since it's no longer valid with new roll properties
            ClearVendorPurchaseData(vendorTypeProto);

            return true;
        }
        
        private VendorPurchaseData GetVendorPurchaseData(PrototypeId inventoryProtoRef, bool createNewData)
        {
            if (_vendorPurchaseDataDict.TryGetValue(inventoryProtoRef, out VendorPurchaseData purchaseData) == false && createNewData)
            {
                purchaseData = new(inventoryProtoRef);
                _vendorPurchaseDataDict.Add(inventoryProtoRef, purchaseData);
            }

            return purchaseData;
        }

        private bool ClearVendorPurchaseData(VendorTypePrototype vendorTypeProto)
        {
            if (vendorTypeProto == null) return Logger.WarnReturn(false, "ClearVendorPurchaseData(): vendorTypeProto == null");

            // Crafters do not have purchase data
            if (vendorTypeProto.IsCrafter)
                return true;

            List<PrototypeId> inventoryList = ListPool<PrototypeId>.Instance.Rent();
            vendorTypeProto.GetInventories(inventoryList);

            foreach (PrototypeId inventoryProtoRef in inventoryList)
            {
                VendorPurchaseData purchaseData = GetVendorPurchaseData(inventoryProtoRef, true);
                purchaseData.Clear();
            }

            ListPool<PrototypeId>.Instance.Return(inventoryList);
            return true;
        }

        private float GetCurrentVendorEnergyPct(VendorTypePrototype vendorTypeProto)
        {
            if (vendorTypeProto == null) return Logger.WarnReturn(0f, "GetCurrentVendorEnergyPct(): vendorTypeProto == null");
            PrototypeId vendorTypeProtoRef = vendorTypeProto.DataRef;

            float lastRefreshPctEngAfter = Properties[PropertyEnum.VendorLastRefreshPctEngAfter, vendorTypeProtoRef];
            float minutesSinceLastRefresh = (float)(Game.CurrentTime - Properties[PropertyEnum.VendorLastRefreshTime, vendorTypeProtoRef]).TotalMinutes;

            float currentVendorEnergyPct = lastRefreshPctEngAfter + (minutesSinceLastRefresh / vendorTypeProto.VendorEnergyFullRechargeTimeMins);
            return Math.Clamp(currentVendorEnergyPct, 0f, 1f);
        }

        private bool SetVendorEnergyPct(PrototypeId vendorTypeProtoRef, float energyPct)
        {
            if (vendorTypeProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "SetVendorEnergyPct(): vendorTypeProtoRef == PrototypeId.Invalid");

            Properties[PropertyEnum.VendorLastRefreshPctEngAfter, vendorTypeProtoRef] = Math.Clamp(energyPct, 0f, 1f);
            Properties[PropertyEnum.VendorLastRefreshTime, vendorTypeProtoRef] = Game.CurrentTime;

            return true;
        }

        private bool RollVendorInventory(VendorTypePrototype vendorTypeProto, bool isInitializing)
        {
            if (vendorTypeProto == null) return Logger.WarnReturn(false, "RollVendorInventory(): vendorTypeProto == null");
            PrototypeId vendorTypeProtoRef = vendorTypeProto.DataRef;

            if (isInitializing && _initializedVendorTypeProtoRefs.Add(vendorTypeProtoRef) == false)
                return true;

            // Early return if there are no inventories to roll
            List<PrototypeId> inventoryList = ListPool<PrototypeId>.Instance.Rent();
            if (vendorTypeProto.GetInventories(inventoryList) == false)
                goto end;

            // Get roll settings from properties
            int rollSeed = Properties[PropertyEnum.VendorRollSeed, vendorTypeProtoRef];
            if (rollSeed == 0) return Logger.WarnReturn(false, "RollVendorInventory(): rollSeed == 0");

            int rollTableLevel = Properties[PropertyEnum.VendorRollTableLevel, vendorTypeProtoRef];

            // Fill inventories
            EntityManager entityManager = Game.EntityManager;

            foreach (PrototypeId inventoryProtoRef in inventoryList)
            {
                // Get the inventory
                Inventory inventory = GetInventoryByRef(inventoryProtoRef);
                if (inventory == null)
                {
                    Logger.Warn("RollVendorInventory(): inventory == null");
                    continue;
                }

                // Find the purchase data for it to filter out items that have already been bought before
                VendorPurchaseData purchaseData = GetVendorPurchaseData(inventoryProtoRef, false);
                if (purchaseData == null && vendorTypeProto.IsCrafter == false)
                {
                    // This should have already been initialized for non-crafter vendors
                    Logger.Warn("RollVendorInventory(): purchaseData == null");
                    purchaseData = GetVendorPurchaseData(inventoryProtoRef, true);
                }

                // Destroy whatever was in it
                inventory.DestroyContained();

                // Find a loot table to roll replacement contents
                int highestUsableLevel = -1;
                PrototypeId lootTableProtoRef = PrototypeId.Invalid;

                if (vendorTypeProto.Inventories != null)
                {
                    foreach (VendorInventoryEntryPrototype inventoryEntry in vendorTypeProto.Inventories)
                    {
                        if (rollTableLevel >= inventoryEntry.UseStartingAtVendorLevel && inventoryEntry.UseStartingAtVendorLevel > highestUsableLevel)
                        {
                            highestUsableLevel = inventoryEntry.UseStartingAtVendorLevel;
                            lootTableProtoRef = inventoryEntry.LootTable;
                        }
                    }
                }

                // Roll new contents
                if (lootTableProtoRef != PrototypeId.Invalid)
                {
                    LootTablePrototype lootTableProto = lootTableProtoRef.As<LootTablePrototype>();
                    if (lootTableProto == null)
                    {
                        Logger.Warn("RollVendorInventory(): lootTableProto == null");
                        continue;
                    }

                    // Initialize settings
                    using LootRollSettings rollSettings = ObjectPoolManager.Instance.Get<LootRollSettings>();
                    rollSettings.Player = this;
                    rollSettings.UsableAvatar = ((PrototypeId)Properties[PropertyEnum.VendorRollAvatar, vendorTypeProtoRef]).As<AvatarPrototype>();
                    rollSettings.Level = Properties[PropertyEnum.VendorRollLevel, vendorTypeProtoRef];

                    // TODO: region keywords
                    Region region = GetRegion();
                    if (region != null)
                        rollSettings.RegionScenarioRarity = region.Settings.ItemRarity;

                    // Initialize resolver and roll
                    using ItemResolver resolver = ObjectPoolManager.Instance.Get<ItemResolver>();
                    resolver.Initialize(new(rollSeed));
                    resolver.SetContext(LootContext.Vendor, this);

                    LootRollResult rollResult = lootTableProto.Roll(rollSettings, resolver);
                    if (rollResult == LootRollResult.Failure)
                    {
                        // Skip the rest of this table if nothing rolled at all
                        Logger.Warn($"RollVendorInventory(): Loot roll failed for loot table {lootTableProto}, vendor type {vendorTypeProto}");
                        continue;
                    }

                    // Create the rolled items
                    using LootResultSummary lootResultSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
                    resolver.FillLootResultSummary(lootResultSummary);

                    if (lootResultSummary.Types != LootType.Item)
                    {
                        Logger.Warn($"RollVendorInventory(): Rolled non-item loot for loot table {lootTableProto}, vendor type {vendorTypeProto}");
                        continue;
                    }

                    // Initialize purchase data for the roll (this will do nothing if we are restoring old purchases)
                    purchaseData?.Initialize((uint)lootResultSummary.ItemSpecs.Count);

                    for (int i = 0; i < lootResultSummary.ItemSpecs.Count; i++)
                    {
                        ItemSpec itemSpec = lootResultSummary.ItemSpecs[i];
                        uint slot = (uint)i;

                        if (inventory.IsSlotFree(slot) == false)
                        {
                            Logger.Warn("RollVendorInventory(): inventory.IsSlotFree(slot) == false");
                            continue;
                        }

                        // Skip purchased items
                        if (purchaseData?.HasItemBeenPurchased(slot) == true)
                            continue;

                        // Skip owned stash tokens
                        InventoryStashTokenPrototype stashTokenProto = itemSpec.ItemProtoRef.As<InventoryStashTokenPrototype>();
                        if (stashTokenProto != null && stashTokenProto.Inventory != PrototypeId.Invalid && IsInventoryUnlocked(stashTokenProto.Inventory))
                            continue;

                        using EntitySettings entitySettings = ObjectPoolManager.Instance.Get<EntitySettings>();
                        entitySettings.EntityRef = itemSpec.ItemProtoRef;
                        entitySettings.ItemSpec = itemSpec;

                        if (IsInGame == false)
                            entitySettings.OptionFlags &= ~EntitySettingsOptionFlags.EnterGame;

                        Item item = entityManager.CreateEntity(entitySettings) as Item;
                        if (item == null)
                        {
                            Logger.Warn("RollVendorInventory(): item == null");
                            continue;
                        }

                        InventoryResult inventoryResult = item.ChangeInventoryLocation(inventory, slot);
                        if (inventoryResult != InventoryResult.Success)
                        {
                            Logger.Warn($"RollVendorInventory(): Failed to put item {item} into inventory {inventory} for reason {inventoryResult}");
                            item.Destroy();
                            continue;
                        }

                        item.Properties[PropertyEnum.InventoryStackCount] = itemSpec.StackCount;

                        // TODO: Initialize crafting recipe
                        if (vendorTypeProto.IsCrafter)
                        {

                        }
                    }
                }

                // TODO: Add learned crafting recipes
                if (vendorTypeProto.IsCrafter && vendorTypeProto.CraftingRecipeCategories.HasValue())
                {

                }
            }

            // TODO: Set PropertyEnum.CraftingIngredientAvailable

            // Notify the client if needed
            if (isInitializing == false)
                SendMessage(NetMessageVendorRefresh.CreateBuilder().SetVendorTypeProtoId((ulong)vendorTypeProtoRef).Build());

            end:
            ListPool<PrototypeId>.Instance.Return(inventoryList);
            return true;
        }

        private bool RefreshVendorInventoryInternal(PrototypeId vendorTypeProtoRef)
        {
            VendorTypePrototype vendorTypeProto = vendorTypeProtoRef.As<VendorTypePrototype>();
            if (vendorTypeProto == null) return Logger.WarnReturn(false, "RefreshVendorInventoryInternal(): vendorTypeProto == null");

            float newVendorEnergyPct = GetCurrentVendorEnergyPct(vendorTypeProto) - vendorTypeProto.VendorEnergyPctPerRefresh;
            if (newVendorEnergyPct < 0f) return Logger.WarnReturn(false, "RefreshVendorInventoryInternal(): newVendorEnergyPct < 0f");

            UpdateVendorLootProperties(vendorTypeProto);
            RollVendorInventory(vendorTypeProto, false);
            SetVendorEnergyPct(vendorTypeProtoRef, newVendorEnergyPct);

            return true;
        }

        private PrototypeId GetGlobalEventCriteriaForVendorItemDonate(ulong itemId, ulong vendorId)
        {
            // TODO: This is used for Events/GlobalEvents/Events/BiFrostUnlock/BifrostUnlock.prototype
            return PrototypeId.Invalid;
        }

        private VendorResult CanBuyItemFromVendor(int avatarIndex, ulong itemId, ulong vendorId)
        {
            // Validate the item
            Item item = Game?.EntityManager.GetEntity<Item>(itemId);
            if (item == null) return Logger.WarnReturn(VendorResult.BuyFailure, "CanBuyItemFromVendor(): item == null");

            ItemPrototype itemProto = item.ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(VendorResult.BuyFailure, "CanBuyItemFromVendor(): itemProto == null");

            if (itemProto.IsLiveTuningVendorEnabled() == false)
                return VendorResult.BuyItemDisabledByLiveTuning;

            // Validate the avatar
            Avatar avatar = GetActiveAvatarByIndex(avatarIndex);
            if (avatar == null) return Logger.WarnReturn(VendorResult.BuyFailure, "CanBuyItemFromVendor(): avatar == null");

            // Validate the inventory
            Inventory inventory = GetInventory(itemProto.DestinationFromVendor);
            if (inventory == null) return Logger.WarnReturn(VendorResult.BuyFailure, "CanBuyItemFromVendor(): inventory == null");

            uint freeSlot = inventory.GetFreeSlot(item, true);
            if (freeSlot == Inventory.InvalidSlot)
                return VendorResult.BuyInventoryFull;

            // Check if the player has enough stuff to afford this item
            if (itemProto.Cost != null && itemProto.Cost.CanAffordItem(this, item) == false)
                return VendorResult.BuyCannotAffordItem;

            // Check if the player is a cool enough person to have this item
            int prestigeLevel = avatar.PrestigeLevel;
            int prestigeLevelRequirement = (int)(float)item.Properties[PropertyEnum.Requirement, PropertyEnum.AvatarPrestigeLevel];
            if (prestigeLevel < prestigeLevelRequirement)
                return VendorResult.BuyInsufficientPrestige;

            // Token validation
            if (itemProto is CharacterTokenPrototype characterTokenProto)
            {
                if (characterTokenProto.IsForTeamUp && IsTeamUpAgentUnlocked(characterTokenProto.Character))
                    return VendorResult.BuyCharacterAlreadyUnlocked;

                if (characterTokenProto.IsForAvatar && HasAvatarFullyUnlocked(characterTokenProto.Character))
                {
                    // Players can still buy avatar tokens to upgrade the current avatar's ultimate
                    if (avatar.PrototypeDataRef == characterTokenProto.Character)
                    {
                        if (avatar.CanUpgradeUltimate() == InteractionValidateResult.AvatarUltimateAlreadyMaxedOut)
                            return VendorResult.BuyAvatarUltimateAlreadyMaxedOut;
                    }
                    else
                    {
                        return VendorResult.BuyAvatarUltimateUpgradeCurrentOnly;
                    }
                }
            }

            // Crafting recipe validation
            if (item.IsCraftingRecipe && HasLearnedCraftingRecipe(itemProto.DataRef))
                return VendorResult.BuyPlayerAlreadyHasCraftingRecipe;

            // Validate the vendor
            WorldEntity vendor = Game.EntityManager.GetEntity<WorldEntity>(vendorId);
            if (vendor == null) return Logger.WarnReturn(VendorResult.BuyFailure, "CanBuyItemFromVendor(): vendor == null");
            if (vendor.IsVendor == false) return Logger.WarnReturn(VendorResult.BuyFailure, "CanBuyItemFromVendor(): vendor.IsVendor == false");

            if (avatar.InInteractRange(vendor, InteractionMethod.Buy) == false)
                return VendorResult.BuyOutOfRange;

            if (vendor.Id != DialogTargetId)
                return VendorResult.BuyDialogTargetIdMismatch;

            // Make sure this item is in one of this vendor's inventories
            PrototypeId vendorTypeProtoRef = vendor.Properties[PropertyEnum.VendorType];
            VendorTypePrototype vendorTypeProto = vendorTypeProtoRef.As<VendorTypePrototype>();
            if (vendorTypeProto == null) return Logger.WarnReturn(VendorResult.BuyFailure, "CanBuyItemFromVendor(): vendorTypeProto == null");

            if (vendorTypeProto.ContainsInventory(item.InventoryLocation.InventoryRef) == false && item.IsInBuybackInventory == false)
                return VendorResult.BuyNotInVendorInventory;

            return VendorResult.BuySuccess;
        }

        private VendorResult CanSellItemToVendor(int avatarIndex, ulong itemId, ulong vendorId)
        {
            WorldEntity vendor = Game?.EntityManager.GetEntity<WorldEntity>(vendorId);
            if (vendor == null) return Logger.WarnReturn(VendorResult.SellFailure, "CanSellItemToVendor(): vendor == null");

            PrototypeId vendorTypeProtoRef = vendor.Properties[PropertyEnum.VendorType];
            VendorTypePrototype vendorTypeProto = vendorTypeProtoRef.As<VendorTypePrototype>();
            if (vendorTypeProto == null) return Logger.WarnReturn(VendorResult.SellFailure, "CanSellItemToVendor(): vendorTypeProto == null");

            if (vendorTypeProto.AllowActionSell == false)
                return VendorResult.SellFailure;

            if (CanPerformVendorOpAtVendor(avatarIndex, itemId, vendorId, InteractionMethod.Sell) != VendorResult.OpSuccess)
                return VendorResult.SellFailure;

            return VendorResult.SellSuccess;
        }

        private bool ValidateItemSellPrice(Item item, int sellPrice)
        {
            PropertyInfoTable propertyInfoTable = GameDatabase.PropertyInfoTable;

            // Check if this price can fit into the ItemSoldPrice property
            PropertyInfoPrototype itemSoldPriceInfoProto = propertyInfoTable.LookupPropertyInfo(PropertyEnum.ItemSoldPrice).Prototype;
            if (itemSoldPriceInfoProto == null) return Logger.WarnReturn(false, "ValidateItemSellPrice(): itemSoldPriceInfoProto == null");

            if (sellPrice > itemSoldPriceInfoProto.Max)
                return Logger.WarnReturn(false, $"ValidateItemSellPrice(): sellPrice [{sellPrice}] exceeds ItemSoldPrice Property Max of [{itemSoldPriceInfoProto.Max}]! item=[{item}] player=[{this}]");

            // Check if this price is within the credits cap
            PropertyInfoPrototype currencyPropInfoProto = propertyInfoTable.LookupPropertyInfo(PropertyEnum.Currency).Prototype;
            if (currencyPropInfoProto == null) return Logger.WarnReturn(false, "ValidateItemSellPrice(): currencyPropInfoProto == null");

            PrototypeId creditsProtoRef = GameDatabase.CurrencyGlobalsPrototype.Credits;
            int currentCredits = Properties[PropertyEnum.Currency, creditsProtoRef];

            if (currentCredits + sellPrice > (int)currencyPropInfoProto.Max)
                return false;

            return true;
        }

        private VendorResult CanDonateItemToVendor(int avatarIndex, ulong itemId, ulong vendorId)
        {
            WorldEntity vendor = Game?.EntityManager.GetEntity<WorldEntity>(vendorId);
            if (vendor == null) return Logger.WarnReturn(VendorResult.DonateFailure, "CanDonateItemToVendor(): vendor == null");

            PrototypeId vendorTypeProtoRef = vendor.Properties[PropertyEnum.VendorType];
            VendorTypePrototype vendorTypeProto = vendorTypeProtoRef.As<VendorTypePrototype>();
            if (vendorTypeProto == null) return Logger.WarnReturn(VendorResult.DonateFailure, "CanDonateItemToVendor(): vendorTypeProto == null");

            if (vendorTypeProto.AllowActionDonate == false)
                return VendorResult.DonateFailure;

            if (CanPerformVendorOpAtVendor(avatarIndex, itemId, vendorId, InteractionMethod.Donate) != VendorResult.OpSuccess)
                return VendorResult.DonateFailure;

            if (vendor.IsGlobalEventVendor)
            {
                PrototypeId globalEventProtoRef = vendor.GetVendorGlobalEvent();
                GlobalEventPrototype globalEventProto = globalEventProtoRef.As<GlobalEventPrototype>();
                if (globalEventProto == null) return Logger.WarnReturn(VendorResult.DonateNotAcceptingDonations, "CanDonateItemToVendor(): globalEventProto == null");

                if (globalEventProto.Active == false)
                    return VendorResult.DonateNotAcceptingDonations;

                if (GetGlobalEventCriteriaForVendorItemDonate(itemId, vendorId) == PrototypeId.Invalid)
                    return VendorResult.DonateNotAcceptingItem;
            }

            return VendorResult.DonateSuccess;
        }

        private VendorResult CanRefreshVendorInventory(ulong vendorId)
        {
            WorldEntity vendor = Game?.EntityManager.GetEntity<WorldEntity>(vendorId);
            if (vendor == null) return Logger.WarnReturn(VendorResult.RefreshFailure, "CanRefreshVendorInventory(): vendor == null");

            PrototypeId vendorTypeProtoRef = vendor.Properties[PropertyEnum.VendorType];
            if (vendorTypeProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(VendorResult.RefreshFailure, "CanRefreshVendorInventory(): vendorTypeProtoRef == PrototypeId.Invalid");

            return CanRefreshVendorInventory(vendorTypeProtoRef, true);
        }

        private VendorResult CanRefreshVendorInventory(PrototypeId vendorTypeProtoRef, bool isPlayerRequest)
        {
            VendorTypePrototype vendorTypeProto = vendorTypeProtoRef.As<VendorTypePrototype>();
            if (vendorTypeProto == null) return Logger.WarnReturn(VendorResult.RefreshFailure, "CanRefreshVendorInventory(): vendorTypeProto == null");

            if (isPlayerRequest && vendorTypeProto.AllowActionRefresh == false)
                return VendorResult.RefreshNotAllowed;

            if (GetCurrentVendorEnergyPct(vendorTypeProto) < vendorTypeProto.VendorEnergyPctPerRefresh)
                return VendorResult.RefreshInsufficientEnergy;

            return VendorResult.RefreshSuccess;
        }

        /// <summary>
        /// Performs common checks for selling and donating items to vendors.
        /// </summary>
        private VendorResult CanPerformVendorOpAtVendor(int avatarIndex, ulong itemId, ulong vendorId, InteractionMethod interactionMethod)
        {
            Game game = Game;
            if (game == null) return Logger.WarnReturn(VendorResult.OpFailure, "CanPerformVendorOpAtVendor(): game == null");

            // Validate the item
            Item item = game.EntityManager.GetEntity<Item>(itemId);
            if (item == null) return Logger.WarnReturn(VendorResult.OpFailure, "CanPerformVendorOpAtVendor(): item == null");

            ItemPrototype itemProto = item.ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(VendorResult.OpFailure, "CanPerformVendorOpAtVendor(): itemProto == null");

            // Check if this item can be sold
            if (itemProto.CanBeSoldToVendor == false)
                return VendorResult.OpFailure;

            // Check if this player owns this item
            InventoryLocation invLoc = item.InventoryLocation;
            if (invLoc.ContainerId != Id)
                return VendorResult.OpFailure;

            // Check if this item is in a player general or a stash inventory
            InventoryPrototype inventoryProto = invLoc.InventoryPrototype;
            if (inventoryProto == null || (inventoryProto.IsPlayerGeneralInventory == false && inventoryProto is not PlayerStashInventoryPrototype))
                return VendorResult.OpFailure;

            // Validate the vendor
            WorldEntity vendor = game.EntityManager.GetEntity<WorldEntity>(vendorId);
            if (vendor == null) return Logger.WarnReturn(VendorResult.OpFailure, "CanPerformVendorOpAtVendor(): vendor == null");
            if (vendor.IsVendor == false) return Logger.WarnReturn(VendorResult.OpFailure, "CanPerformVendorOpAtVendor(): vendor.IsVendor == false");

            // Validate the avatar
            Avatar avatar = GetActiveAvatarByIndex(avatarIndex);
            if (avatar == null) return Logger.WarnReturn(VendorResult.OpFailure, "CanPerformVendorOpAtVendor(): avatar == null");

            // Check if this avatar is within interaction range of the vendor
            if (avatar.InInteractRange(vendor, interactionMethod) == false)
                return VendorResult.OpFailure;

            // Check if this player is currently interacting with the vendor
            if (vendor.Id != DialogTargetId)
                return VendorResult.OpFailure;

            return VendorResult.OpSuccess;
        }
    }
}
