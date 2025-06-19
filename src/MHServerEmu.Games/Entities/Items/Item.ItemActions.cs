using Gazillion;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Loot.Specs;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities.Items
{
    public enum ItemActionType
    {
        None,
        AssignPower,
        DestroySelf,
        GuildUnlock,
        PrestigeMode,
        ReplaceSelfItem,
        ReplaceSelfLootTable,
        ResetMissions,
        Respec,
        SaveDangerRoomScenario,
        UnlockPermaBuff,
        UsePower,
        AwardTeamUpXP,
        OpenUIPanel
    }

    public partial class Item
    {
        private void TriggerItemActionOnUse(ItemActionPrototype actionProto, Player player, Avatar avatar, ref bool wasUsed, ref bool isConsumable)
        {
            if (actionProto.TriggeringEvent != ItemEventType.OnUse)
                return;

            switch (actionProto.ActionType)
            {
                case ItemActionType.AssignPower:
                    wasUsed |= DoItemActionAssignPower();
                    break;

                case ItemActionType.DestroySelf:
                    DoItemActionDestroySelf(ref isConsumable);    // This simply flags the item to be destroyed, so we don't need to update wasUsed here
                    break;

                case ItemActionType.GuildUnlock:
                    wasUsed |= DoItemActionGuildUnlock();
                    break;

                case ItemActionType.PrestigeMode:
                    wasUsed |= DoItemActionPrestigeMode(avatar);
                    break;

                case ItemActionType.ReplaceSelfItem:
                    if (actionProto is not ItemActionReplaceSelfItemPrototype replaceSelfItemProto)
                    {
                        Logger.Warn("TriggerItemActionOnUse(): actionProto is not ItemActionReplaceSelfItemPrototype replaceSelfItemProto");
                        return;
                    }

                    wasUsed |= DoItemActionReplaceSelfItem(replaceSelfItemProto.Item, player, avatar);
                    break;

                case ItemActionType.ReplaceSelfLootTable:
                    if (actionProto is not ItemActionReplaceSelfLootTablePrototype replaceSelfLootTableProto)
                    {
                        Logger.Warn("TriggerItemActionOnUse(): actionProto is not ItemActionReplaceSelfLootTablePrototype replaceSelfLootTableProto");
                        return;
                    }

                    wasUsed |= DoItemActionReplaceSelfLootTable(replaceSelfLootTableProto.LootTable, replaceSelfLootTableProto.UseCurrentAvatarLevelForRoll, player, avatar);
                    break;

                case ItemActionType.ResetMissions:
                    wasUsed |= DoItemActionResetMissions(avatar);
                    break;

                case ItemActionType.Respec:
                    wasUsed |= DoItemActionRespec();
                    break;

                case ItemActionType.SaveDangerRoomScenario:
                    wasUsed |= DoItemActionSaveDangerRoomScenario();
                    break;

                case ItemActionType.UnlockPermaBuff:
                    wasUsed |= DoItemActionUnlockPermaBuff();
                    break;

                case ItemActionType.UsePower:
                    if (actionProto is not ItemActionUsePowerPrototype usePowerProto)
                    {
                        Logger.Warn("TriggerItemActionOnUse(): actionProto is not ItemActionUsePowerPrototype usePowerProto");
                        return;
                    }

                    wasUsed |= DoItemActionUsePower(usePowerProto.Power, avatar);
                    break;

                case ItemActionType.AwardTeamUpXP:
                    if (actionProto is not ItemActionAwardTeamUpXPPrototype awardTeamUpXPProto)
                    {
                        Logger.Warn("TriggerItemActionOnUse(): actionProto is not ItemActionAwardTeamUpXPPrototype awardTeamUpXPProto");
                        return;
                    }

                    wasUsed |= DoItemActionAwardTeamUpXP(avatar, awardTeamUpXPProto.XP);
                    break;

                case ItemActionType.OpenUIPanel:
                    if (actionProto is not ItemActionOpenUIPanelPrototype openUIPanelProto)
                    {
                        Logger.Warn("TriggerItemActionOnUse(): actionProto is not ItemActionOpenUIPanelPrototype openUIPanelProto");
                        return;
                    }

                    wasUsed |= DoItemActionOpenUIPanel(player, openUIPanelProto.PanelName);
                    break;
            }
        }

        private bool TriggerItemActionOnUsePowerActivated(ItemActionPrototype itemActionProto)
        {
            if (itemActionProto.TriggeringEvent != ItemEventType.OnUsePowerActivated)
                return false;

            switch (itemActionProto.ActionType)
            {
                case ItemActionType.DestroySelf:
                    DecrementStack();
                    return true;

                default:
                    return Logger.WarnReturn(false, $"TriggerItemActionOnUsePowerActivated(): Unhandled action type {itemActionProto.ActionType}");
            }
        }

        private bool DoItemActionAssignPower()
        {
            Logger.Debug($"DoItemActionAssignPower(): {this}");
            return false;
        }

        private void DoItemActionDestroySelf(ref bool isConsumable)
        {
            // This "action" flags this item's effect as consumable (i.e. it needs to be destroyed on use)
            isConsumable = true;
        }

        private bool DoItemActionGuildUnlock()
        {
            Logger.Debug($"DoItemActionGuildUnlock(): {this}");
            return false;
        }

        private bool DoItemActionPrestigeMode(Avatar avatar)
        {
            Logger.Trace($"DoItemActionPrestigeMode(): [{this}] for [{avatar}]");
            return avatar.ActivatePrestigeMode();
        }

        private bool DoItemActionReplaceSelfItem(PrototypeId itemProtoRef, Player player, Avatar avatar)
        {
            ItemPrototype itemProto = itemProtoRef.As<ItemPrototype>();
            if (itemProto == null) return Logger.WarnReturn(false, "DoItemActionReplaceSelfItem(): itemProto == null");

            using LootResultSummary lootResultSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
            LootResult lootResult;

            if (itemProto.IsCurrency)
            {
                itemProto.GetCurrency(out PrototypeId currencyRef, out int amount);
                CurrencySpec currencySpec = new(itemProto.DataRef, currencyRef, amount);
                lootResult = new(currencySpec);
            }
            else
            {
                ItemSpec itemSpec = Game.LootManager.CreateItemSpec(itemProtoRef, LootContext.CashShop, player, Properties[PropertyEnum.ItemLevel]);
                if (itemSpec == null) return Logger.WarnReturn(false, "DoItemActionReplaceSelfItem(): itemSpec == null");
                lootResult = new(itemSpec);
            }

            lootResultSummary.Add(lootResult);

            NetMessageLootRewardReport.Builder reportBuilder = NetMessageLootRewardReport.CreateBuilder();

            if (ReplaceSelfHelper(lootResultSummary, player, reportBuilder))
            {
                reportBuilder.SetSource(_itemSpec.ToProtobuf());
                player.SendMessage(reportBuilder.Build());
                return true;
            }

            return false;
        }

        private bool DoItemActionReplaceSelfLootTable(LootTablePrototype lootTableProto, bool useAvatarLevel, Player player, Avatar avatar)
        {
            using LootInputSettings inputSettings = ObjectPoolManager.Instance.Get<LootInputSettings>();
            inputSettings.Initialize(LootContext.MysteryChest, player, null);
            inputSettings.LootRollSettings.Level = useAvatarLevel ? avatar.CharacterLevel : Properties[PropertyEnum.ItemLevel];

            using ItemResolver resolver = ObjectPoolManager.Instance.Get<ItemResolver>();
            resolver.Initialize(Game.Random);
            resolver.SetContext(LootContext.MysteryChest, player);

            LootRollResult result = lootTableProto.RollLootTable(inputSettings.LootRollSettings, resolver);
            if (result == LootRollResult.NoRoll || result == LootRollResult.Failure)
            {
                player.SendMessage(NetMessageLootRollFailed.DefaultInstance);
                return Logger.WarnReturn(false, $"DoItemActionReplaceSelfLootTable(): Failed to roll loot table for {this}");
            }

            using LootResultSummary lootResultSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
            resolver.FillLootResultSummary(lootResultSummary);

            NetMessageLootRewardReport.Builder reportBuilder = NetMessageLootRewardReport.CreateBuilder();

            if (ReplaceSelfHelper(lootResultSummary, player, reportBuilder))
            {
                reportBuilder.SetSource(_itemSpec.ToProtobuf());
                player.SendMessage(reportBuilder.Build());
                return true;
            }
            
            return false;
        }

        private bool DoItemActionResetMissions(Avatar avatar)
        {
            Logger.Trace($"DoItemActionResetMissions(): [{this}] for [{avatar}]");
            return avatar.ResetMissions();
        }

        private bool DoItemActionRespec()
        {
            Logger.Debug($"DoItemActionRespec(): {this}");
            return false;
        }

        private bool DoItemActionSaveDangerRoomScenario()
        {
            Logger.Debug($"DoItemActionSaveDangerRoomScenario(): {this}");
            return false;
        }

        private bool DoItemActionUnlockPermaBuff()
        {
            Logger.Debug($"DoItemActionUnlockPermaBuff(): {this}");
            return false;
        }
        
        private bool DoItemActionUsePower(PrototypeId powerProtoRef, Avatar avatar)
        {
            Power power = avatar.GetPower(powerProtoRef);
            if (power == null) return Logger.WarnReturn(false, "DoItemActionUsePower(): power == null");

            // Adjust index properties for this power specifically (if we have different items that activate the same power)
            power.Properties.CopyProperty(Properties, PropertyEnum.ItemLevel);
            power.Properties.CopyProperty(Properties, PropertyEnum.ItemVariation);

            // Activate the power
            Vector3 position = avatar.RegionLocation.Position;
            PowerActivationSettings settings = new(InvalidId, position, position);
            settings.ItemSourceId = Id;
            settings.Flags |= PowerActivationSettingsFlags.NotifyOwner;

            return avatar.ActivatePower(powerProtoRef, ref settings) == PowerUseResult.Success;
        }

        private bool DoItemActionAwardTeamUpXP(Avatar avatar, int amount)
        {
            Agent teamUpAgent = avatar.CurrentTeamUpAgent;
            if (teamUpAgent == null)
                return false;

            teamUpAgent.AwardXP(amount, 0, true);
            return true;
        }

        private bool DoItemActionOpenUIPanel(Player player, AssetId panelNameId)
        {
            return player.SendOpenUIPanel(panelNameId);
        }

        private bool ReplaceSelfHelper(LootResultSummary lootResultSummary, Player player, NetMessageLootRewardReport.Builder reportBuilder)
        {
            // Validation

            // Loot types not defined here cannot be used as MysteryChest replacements
            const LootType LootTypeFilter = LootType.Item | LootType.Currency | LootType.CallbackNode | LootType.VanityTitle;

            LootType unsupportedTypes = lootResultSummary.Types & ~LootTypeFilter;
            if (unsupportedTypes != LootType.None)
                return Logger.WarnReturn(false, $"ReplaceSelfHelper(): Summary contains unsupported loot types {unsupportedTypes}");

            ItemPrototype itemProto = ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, "ReplaceSelfHelper(): itemProto == null");

            if (InventoryLocation.ContainerId != player.Id) return Logger.WarnReturn(false, "ReplaceSelfHelper(): InventoryLocation.ContainerId != player.Id");

            Inventory inventory = player.GetInventoryByRef(InventoryLocation.InventoryRef);
            if (inventory == null) return Logger.WarnReturn(false, "ReplaceSelfHelper(): inventory == null");

            Inventory deliveryBox = player.GetInventory(InventoryConvenienceLabel.DeliveryBox);
            if (deliveryBox == null) return Logger.WarnReturn(false, "ReplaceSelfHelper(): deliveryBox == null");

            // Try to avoid delivery box overflow because people can abuse it to hoard loot and cause performance issues
            int itemCount = lootResultSummary.ItemSpecs.Count;
            if (itemCount > 1 && inventory.Count + itemCount >= inventory.MaxCapacity)
            {
                player.SendMessage(NetMessageInventoryFull.CreateBuilder()
                    .SetPlayerID(player.Id)
                    .SetItemID(InvalidId)
                    .Build());

                return false;
            }

            // If this is the last item in the stack, move it out of the inventory while we try to replace it
            InventoryLocation oldInvLoc = new(InventoryLocation);

            if (CurrentStackSize <= 1 && ChangeInventoryLocation(null) != InventoryResult.Success)
                return Logger.WarnReturn(false, $"ReplaceSelfHelper(): Failed to remove the last item in the stack from its inventory\nItem=[{this}]\nInvLoc=[{InventoryLocation}]");

            // We need to keep track of everything we are doing so we can roll back if something goes wrong
            List<(ulong, int)> replacementItemList = ListPool<(ulong, int)>.Instance.Get();

            using PropertyCollection oldCurrencyProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            oldCurrencyProperties.CopyPropertyRange(player.Properties, PropertyEnum.Currency);

            try
            {
                EntityManager entityManager = Game.EntityManager;

                foreach (ItemSpec itemSpec in lootResultSummary.ItemSpecs)
                {
                    // Create an item
                    using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
                    settings.EntityRef = itemSpec.ItemProtoRef;
                    settings.ItemSpec = itemSpec;

                    Item replacementItem = entityManager.CreateEntity(settings) as Item;
                    if (replacementItem == null)
                    {
                        Logger.Warn("ReplaceSelfHelper(): replacementItem == null");
                        CleanUpReplaceSelfError(player, replacementItemList, oldCurrencyProperties, oldInvLoc);
                        return false;
                    }

                    replacementItem.Properties[PropertyEnum.InventoryStackCount] = itemSpec.StackCount;

                    // Check if this item can be put into this inventory
                    if (replacementItem.CanChangeInventoryLocation(inventory) != InventoryResult.Success)
                    {
                        Logger.Warn($"ReplaceSelfHelper(): Replacement item [{replacementItem}] cannot be put into inventory {inventory}");
                        replacementItemList.Add((replacementItem.Id, replacementItem.CurrentStackSize));
                        CleanUpReplaceSelfError(player, replacementItemList, oldCurrencyProperties, oldInvLoc);
                        return false;
                    }

                    // Add this item to the inventory
                    bool wasAdded = false;
                    ulong? stackEntityId = 0;

                    // Try to stack it
                    uint slot = inventory.GetAutoStackSlot(replacementItem, true);
                    if (slot != Inventory.InvalidSlot && replacementItem.ChangeInventoryLocation(inventory, slot, ref stackEntityId, true) == InventoryResult.Success)
                        wasAdded = true;

                    // Try to put it into the original item's slot
                    if (wasAdded == false && replacementItem.ChangeInventoryLocation(inventory, oldInvLoc.Slot, ref stackEntityId, true) == InventoryResult.Success)
                        wasAdded = true;

                    // Try to put it into a free slot
                    if (wasAdded == false && replacementItem.ChangeInventoryLocation(inventory, Inventory.InvalidSlot, ref stackEntityId, true) == InventoryResult.Success)
                        wasAdded = true;

                    // Try the delivery box as a fallback
                    if (wasAdded == false && replacementItem.ChangeInventoryLocation(deliveryBox, Inventory.InvalidSlot, ref stackEntityId, true) == InventoryResult.Success)
                        wasAdded = true;

                    // Everything failed
                    if (wasAdded == false)
                    {
                        replacementItemList.Add((replacementItem.Id, replacementItem.CurrentStackSize));
                        Logger.Warn($"ReplaceSelfHelper(): Failed to put replacement item [{replacementItem}] anywhere");
                        CleanUpReplaceSelfError(player, replacementItemList, oldCurrencyProperties, oldInvLoc);
                        return false;
                    }

                    // Finalize this item
                    if (stackEntityId.Value == InvalidId)
                    {
                        // The replacement was added as a new item
                        replacementItemList.Add((replacementItem.Id, replacementItem.CurrentStackSize));

                        reportBuilder.AddItemSpecs(NetMessageLootEntity.CreateBuilder()
                            .SetItemSpec(itemSpec.ToProtobuf())
                            .SetItemId(replacementItem.Id));

                        replacementItem.SetRecentlyAdded(true);
                    }
                    else
                    {
                        // The replacement got stacked
                        replacementItemList.Add((replacementItem.Id, itemSpec.StackCount));

                        reportBuilder.AddItemSpecs(NetMessageLootEntity.CreateBuilder()
                            .SetItemSpec(itemSpec.ToProtobuf())
                            .SetItemId(stackEntityId.Value));

                        Item stackEntity = entityManager.GetEntity<Item>(stackEntityId.Value);
                        stackEntity?.SetRecentlyAdded(true);
                    }

                }

                using PropertyCollection replacementCurrencyProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();

                foreach (CurrencySpec currencySpec in lootResultSummary.Currencies)
                {
                    if (currencySpec.IsItem == false)
                    {
                        Logger.Warn($"ReplaceSelfHelper(): Attempted to replace item [{this}] with a non-item currency {currencySpec.AgentOrItemProtoRef.GetName()}");
                        CleanUpReplaceSelfError(player, replacementItemList, oldCurrencyProperties, oldInvLoc);
                        return false;
                    }

                    currencySpec.ApplyCurrency(replacementCurrencyProperties);

                    using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
                    settings.EntityRef = currencySpec.AgentOrItemProtoRef;
                    settings.ItemSpec = new(currencySpec.AgentOrItemProtoRef, GameDatabase.LootGlobalsPrototype.RarityDefault, 1);
                    settings.Properties = replacementCurrencyProperties;

                    Item currencyItem = entityManager.CreateEntity(settings) as Item;
                    if (currencyItem == null)
                    {
                        Logger.Warn("ReplaceSelfHelper(): currencyItem == null");
                        CleanUpReplaceSelfError(player, replacementItemList, oldCurrencyProperties, oldInvLoc);
                        return false;
                    }

                    replacementCurrencyProperties.RemovePropertyRange(PropertyEnum.ItemCurrency);

                    bool acquired = player.AcquireCurrencyItem(currencyItem);
                    currencyItem.Destroy();

                    if (acquired == false)
                    {
                        Logger.Warn($"ReplaceSelfHelper(): Failed to acquire replacement currency from item [{currencyItem}]");
                        CleanUpReplaceSelfError(player, replacementItemList, oldCurrencyProperties, oldInvLoc);
                        return false;
                    }

                    reportBuilder.AddCurrencySpecs(currencySpec.ToProtobuf());
                }

                // Scoring ItemCollected
                foreach (var pair in replacementItemList)
                {
                    var item = entityManager.GetEntity<Item>(pair.Item1);
                    if (item == null) continue;
                    int count = pair.Item2;
                    player.OnScoringEvent(new(ScoringEventType.ItemCollected, item.Prototype, item.RarityPrototype, count));
                }

                // Do callbacks
                foreach (LootNodePrototype callbackNode in lootResultSummary.CallbackNodes)
                    callbackNode.OnResultsEvaluation(player, null);

                // Grant vanity titles
                foreach (PrototypeId vanityTitleProtoRef in lootResultSummary.VanityTitles)
                    player.UnlockVanityTitle(vanityTitleProtoRef);

                // Consume a stack of this item
                DecrementStack();

                return true;
            }
            finally
            {
                // Return our cleanup list to the pool
                ListPool<(ulong, int)>.Instance.Return(replacementItemList);
            }
        }

        private void CleanUpReplaceSelfError(Player player, List<(ulong, int)> replacementItemList, PropertyCollection propertiesToRestore, InventoryLocation invLoc)
        {
            EntityManager entityManager = Game.EntityManager;

            // Clean up partial item replacement
            foreach (var entry in replacementItemList)
            {
                (ulong itemId, int count) = entry;
                Item item = entityManager.GetEntity<Item>(itemId);
                if (item == null)
                {
                    Logger.Warn("CleanUpReplaceSelfError(): item == null");
                    continue;
                }

                item.DecrementStack(count);
            }

            // Restore currency
            player.Properties.CopyPropertyRange(propertiesToRestore, PropertyEnum.Currency);

            // Return this item to its original location
            if (InventoryLocation != invLoc && ChangeInventoryLocation(invLoc.GetInventory(), invLoc.Slot) != InventoryResult.Success)
            {
                Logger.Warn($"CleanUpReplaceSelfError(): Failed to return item [{this}] to its original inventory location {invLoc}");
                Destroy();
            }
        }
    }
}
