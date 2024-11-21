using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;
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

        private readonly HashSet<PrototypeId> _initializedVendorProtoRefs = new();

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

        public bool AwardVendorXP(int amount, PrototypeId vendorProtoRef)
        {
            // TODO: Implement this
            // NOTE: Do weekly rollover checks and reset ePID_VendorXPCapCounter when rolling LootDropVendorXP
            Logger.Debug($"AwardVendorXP(): amount=[{amount}], vendorProtoRef=[{vendorProtoRef}], player=[{this}]");
            return true;
        }

        public bool BuyItemFromVendor(int avatarIndex, ulong itemId, ulong vendorId, uint inventorySlot)
        {
            Logger.Debug($"BuyItemFromVendor(): avatarIndex={avatarIndex}, itemId={itemId}, vendorId={vendorId}, inventorySlot={inventorySlot}");

            if (CanBuyItemFromVendor(avatarIndex, itemId, vendorId) != VendorResult.BuySuccess)
                return false;

            Logger.Debug("BuyItemFromVendor(): CanBuyItemFromVendor = true");

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

            if (isInitializing && _initializedVendorProtoRefs.Add(vendorTypeProtoRef) == false)
                return true;

            Logger.Debug($"RollVendorInventory(): {vendorTypeProto}");

            // Early return if there are no inventories to roll (TODO: pooling? This doesn't happen that frequently I think)
            List<PrototypeId> inventoryList = new();
            if (vendorTypeProto.GetInventories(inventoryList) == false)
                return true;

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
                    if (rollResult != LootRollResult.Success)
                    {
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

                    foreach (ItemSpec itemSpec in lootResultSummary.ItemSpecs)
                    {
                        Logger.Trace($"RollVendorInventory(): {itemSpec.ItemProtoRef.GetName()}");
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

                        InventoryResult inventoryResult = item.ChangeInventoryLocation(inventory);
                        if (inventoryResult != InventoryResult.Success)
                        {
                            Logger.Warn($"RollVendorInventory(): Failed to put item {item} into inventory {inventory} for reason {inventoryResult}");
                            item.Destroy();
                            continue;
                        }

                        item.Properties[PropertyEnum.InventoryStackCount] = itemSpec.StackCount;
                    }
                }

                // TODO: Populate crafter tabs
                if (vendorTypeProto.IsCrafter && vendorTypeProto.CraftingRecipeCategories.HasValue())
                {

                }
            }

            // TODO: Set PropertyEnum.CraftingIngredientAvailable

            // Notify the client if needed
            if (isInitializing == false)
                SendMessage(NetMessageVendorRefresh.CreateBuilder().SetVendorTypeProtoId((ulong)vendorTypeProtoRef).Build());

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
                // TODO
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
            // TODO
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
