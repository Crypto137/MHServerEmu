using System.Text;
using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities.Items
{
    public enum InteractionValidateResult       // Result names from CItem::AttemptInteractionBy()
    {
        Success,
        ItemNotOwned,
        Error2,
        Error3,
        ItemNotUsable,
        Error5,
        Error6,
        ItemRequirementsNotMet,
        Error8,
        InventoryAlreadyUnlocked,
        CharacterAlreadyUnlocked,
        CharacterNotYetUnlocked,
        AvatarUltimateNotUnlocked,
        AvatarUltimateAlreadyMaxedOut,
        AvatarUltimateUpgradeCurrentOnly,
        PlayerAlreadyHasCraftingRecipe,
        CannotTriggerPower,
        ItemNotEquipped,
        DownloadRequired,
        UnknownFailure
    }

    public partial class Item : WorldEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private ItemSpec _itemSpec = new();
        private List<AffixPropertiesCopyEntry> _affixProperties = new();

        private ulong _tickerId;

        public ItemPrototype ItemPrototype { get => Prototype as ItemPrototype; }
        public RarityPrototype RarityPrototype { get => GameDatabase.GetPrototype<RarityPrototype>(Properties[PropertyEnum.ItemRarity]); }

        public ItemSpec ItemSpec { get => _itemSpec; }
        public PrototypeId OnUsePower { get; private set; }
        public PrototypeId OnEquipPower { get; private set; }

        public bool IsEquipped { get => InventoryLocation.InventoryPrototype?.IsEquipmentInventory == true; }
        public bool IsInBuybackInventory { get => InventoryLocation.InventoryRef == GameDatabase.GlobalsPrototype.VendorBuybackInventory; }
        
        public bool BindsToAccountOnPickup { get => Properties[PropertyEnum.ItemBindsToAccountOnPickup]; }
        public bool BindsToCharacterOnEquip { get => Properties[PropertyEnum.ItemBindsToCharacterOnEquip]; }
        public bool IsBoundToAccount { get => _itemSpec.GetBindingState(); }
        public bool IsBoundToCharacter { get => _itemSpec.GetBindingState(out PrototypeId agentProtoRef) && agentProtoRef != PrototypeId.Invalid; }
        public bool IsTradable { get => Properties[PropertyEnum.ItemIsTradable] && _itemSpec.GetTradeRestricted() == false; }
        public PrototypeId BoundAgentProtoRef { get => _itemSpec.GetBindingState(out PrototypeId agentProtoRef) ? agentProtoRef : PrototypeId.Invalid; }
        public bool WouldBeDestroyedOnDrop { get => IsBoundToAccount || GameDatabase.DebugGlobalsPrototype.TrashedItemsDropInWorld == false; }
        public bool StacksCanBeSplit { get => ItemPrototype?.StackSettings?.StacksCanBeSplit == true; }

        public bool IsPetItem { get => ItemPrototype?.IsPetItem == true; }
        public bool IsCraftingRecipe { get => Prototype is CraftingRecipePrototype; }
        public bool IsRelic { get => Prototype is RelicPrototype; }
        public bool IsTeamUpGear { get => Prototype is TeamUpGearPrototype; }
        public bool IsGem { get => ItemPrototype?.IsGem == true; }
        public bool IsClonedWhenPurchasedFromVendor { get => ItemPrototype?.ClonedWhenPurchasedFromVendor == true; }

        public Item(Game game) : base(game) 
        {
            SetFlag(EntityFlags.IsNeverAffectedByPowers, true);
        }

        public override bool Initialize(EntitySettings settings)
        {
            base.Initialize(settings);

            // Apply ItemSpec if one was provided with entity settings
            if (settings.ItemSpec != null)
            {
                ApplyItemSpec(settings.ItemSpec);

                // Initialize experience requiremenet for legendary items
                if (Prototype is LegendaryPrototype)
                    Properties[PropertyEnum.ExperiencePointsNeeded] = GetAffixLevelUpXPRequirement(0);
            }

            if (Prototype is RelicPrototype)
                RunRelicEval();

            return true;
        }

        public override bool ApplyInitialReplicationState(ref EntitySettings settings)
        {
            if (base.ApplyInitialReplicationState(ref settings) == false)
                return false;

            if (settings.ArchiveData != null)
            {
                // Serialized entities get their ItemSpec from serialized data rather than as a settings field
                ApplyItemSpec(ItemSpec);

                // Restore affix level from XP for legendary items
                TryLevelUpAffix(true);
            }

            return true;
        }

        public override void OnPostInit(EntitySettings settings)
        {
            base.OnPostInit(settings);
            RefreshProcPowerIndexProperties();
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);
            success &= Serializer.Transfer(archive, ref _itemSpec);
            return success;
        }

        public override bool IsAutoStackedWhenAddedToInventory()
        {
            var itemProto = Prototype as ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, "IsAutoStackedWhenAddedToInventory(): itemProto == null");
            if (itemProto.StackSettings == null) return false;
            return itemProto.StackSettings.AutoStackWhenAddedToInventory;
        }

        public override void OnSelfAddedToOtherInventory()
        {
            InventoryLocation invLoc = InventoryLocation;

            if (invLoc.IsValid)
            {
                InventoryPrototype inventoryProto = invLoc.InventoryPrototype;
                Entity owner = Game.EntityManager.GetEntity<Entity>(invLoc.ContainerId);

                // Account binding
                if (Game.CustomGameOptions.DisableAccountBinding == false)
                {
                    // HACK: Do not account bind tradable items until we get the trade window implemented
                    if (BindsToAccountOnPickup && IsTradable == false)
                    {
                        Player playerOwner = owner?.GetSelfOrOwnerOfType<Player>();
                        if (playerOwner != null && IsBoundToAccount == false)
                            SetBinding(true);
                    }
                }

                // Character binding
                if (Game.CustomGameOptions.DisableCharacterBinding == false)
                {
                    if (owner is Agent && BindsToCharacterOnEquip && IsBoundToCharacter == false)
                        SetBinding(true, owner.PrototypeDataRef);
                }

                // Remove sold price after buyback
                if (IsInBuybackInventory == false)
                    Properties.RemoveProperty(PropertyEnum.ItemSoldPrice);

                if (inventoryProto.IsEquipmentInventory)
                {
                    // Start ticking
                    if (owner != null)
                        StartTicking(owner);

                    if (IsPetItem && IsPetTechFullyUpgraded())
                    {
                        Player player = GetOwnerOfType<Player>();
                        if (player != null && player.IsInGame)
                        {
                            int count = ScoringEvents.GetPlayerFullyUpgradedPetTechCount(player);
                            player.OnScoringEvent(new(ScoringEventType.FullyUpgradedPetTech, count));
                        }
                    }

                    // Update granted power
                    if (GetPowerGranted(out PrototypeId powerProtoRef))
                    {
                        if (owner is Avatar avatar)
                            avatar.InitPowerFromCreationItem(this);
                        else if (owner is Player player)
                            player.InitPowerFromCreationItem(this);
                    }

                    // Apply team-up affixes to avatar if needed
                    if (inventoryProto.Category == InventoryCategory.TeamUpEquipment)
                    {
                        if (owner is not Agent ownerAgent)
                        {
                            Logger.Warn("OnSelfAddedToOtherInventory(): owner is not Agent ownerAgent");
                            return;
                        }

                        Player owningPlayer = ownerAgent.GetOwnerOfType<Player>();
                        if (owningPlayer == null)
                        {
                            Logger.Warn("OnSelfAddedToOtherInventory(): player == null");
                            return;
                        }

                        Avatar avatar = owningPlayer.CurrentAvatar;
                        if (avatar != null && avatar.IsInWorld && avatar.CurrentTeamUpAgent == ownerAgent)
                            ApplyTeamUpAffixesToAvatar(avatar);
                    }
                }
            }

            base.OnSelfAddedToOtherInventory();
        }

        public override void OnSelfRemovedFromOtherInventory(InventoryLocation prevInvLoc)
        {
            base.OnSelfRemovedFromOtherInventory(prevInvLoc);

            if (prevInvLoc.IsValid == false)
                return;

            InventoryPrototype inventoryProto = prevInvLoc.InventoryPrototype;
            Entity prevOwner = Game.EntityManager.GetEntity<Entity>(prevInvLoc.ContainerId);

            if (inventoryProto.Category == InventoryCategory.TeamUpEquipment)
            {
                Player playerOwner = prevOwner?.GetOwnerOfType<Player>();
                Avatar avatar = playerOwner?.CurrentAvatar;

                if (avatar != null && avatar.IsInWorld && avatar.CurrentTeamUpAgent == prevOwner)
                    RemoveTeamUpAffixesFromAvatar(avatar);
            }

            if (prevOwner != null && inventoryProto.IsEquipmentInventory)
                StopTicking(prevOwner);

            if (IsPetItem && IsPetTechFullyUpgraded())
            {
                Player player = GetOwnerOfType<Player>();
                if (player != null && player.IsInGame)
                {
                    int count = ScoringEvents.GetPlayerFullyUpgradedPetTechCount(player);
                    player.OnScoringEvent(new(ScoringEventType.FullyUpgradedPetTech, count));
                }
            }
        }

        public bool ApplyTeamUpAffixesToAvatar(Avatar avatar)
        {
            if (GetOwnerOfType<Player>() != avatar.GetOwnerOfType<Player>()) return Logger.WarnReturn(false, "ApplyTeamUpAffixesToAvatar(): GetOwnerOfType<Player>() != avatar.GetOwnerOfType<Player>()");
            
            foreach (AffixPropertiesCopyEntry copyEntry in _affixProperties)
            {
                if (copyEntry.Properties == null || copyEntry.AffixProto == null)
                {
                    Logger.Warn("ApplyTeamUpAffixesToAvatar(): copyEntry.Properties == null || copyEntry.AffixProto == null");
                    continue;
                }

                if (copyEntry.AffixProto is not AffixTeamUpPrototype affixProto)
                    continue;

                if (affixProto.IsAppliedToOwnerAvatar == false)
                    continue;

                bool didAssignAllPowers = avatar.UpdateProcEffectPowers(copyEntry.Properties, true);
                if (didAssignAllPowers == false)
                    Logger.Warn($"ApplyTeamUpAffixesToAvatar(): UpdateProcEffectPowers failed in ApplyTeamUpAffixesToAvatar for affix=[{affixProto}] item=[{this}] avatar=[{avatar}]");

                if (avatar.Properties.HasChildCollection(copyEntry.Properties))
                    continue;
                
                avatar.Properties.AddChildCollection(copyEntry.Properties);
            }

            return true;
        }

        public void RemoveTeamUpAffixesFromAvatar(Avatar avatar)
        {
            foreach (AffixPropertiesCopyEntry copyEntry in _affixProperties)
            {
                if (copyEntry.Properties == null || copyEntry.AffixProto == null)
                {
                    Logger.Warn("RemoveTeamUpAffixesFromAvatar(): copyEntry.Properties == null || copyEntry.AffixProto == null");
                    continue;
                }

                if (copyEntry.AffixProto is not AffixTeamUpPrototype affixProto)
                    continue;

                if (affixProto.IsAppliedToOwnerAvatar == false)
                    continue;

                if (avatar.Properties.HasChildCollection(copyEntry.Properties) == false)
                    continue;

                if (copyEntry.Properties.RemoveFromParent(avatar.Properties))
                    avatar.UpdateProcEffectPowers(copyEntry.Properties, false);
            }
        }

        public void StartTicking(Entity owner)
        {
            if (_tickerId != PropertyTicker.InvalidId)
            {
                Logger.Warn("StartTicking(): _tickerId != PropertyTicker.InvalidId");
                return;
            }

            _tickerId = owner.StartPropertyTicker(Properties, Id, Id, TimeSpan.FromMilliseconds(1000));
        }

        public void StopTicking(Entity owner)
        {
            owner.StopPropertyTicker(_tickerId);
            _tickerId = PropertyTicker.InvalidId;
        }

        public override void OnPropertyChange(PropertyId id, PropertyValue newValue, PropertyValue oldValue, SetPropertyFlags flags)
        {
            base.OnPropertyChange(id, newValue, oldValue, flags);
            if (flags.HasFlag(SetPropertyFlags.Refresh)) return;
            
            switch (id.Enum)
            {
                case PropertyEnum.InventoryStackCount:
                    RunRelicEval();
                    RefreshProcPowerIndexProperties();

                    int delta = (int)newValue - oldValue;
                    if (delta == 0)
                        return;

                    Player owner = GetOwnerOfType<Player>();
                    if (owner == null)
                        return;

                    Inventory ownerInventory = GetOwnerInventory();
                    if (ownerInventory != null)
                        owner.AdjustCraftingIngredientAvailable(PrototypeDataRef, delta, ownerInventory.Category);

                    // TODO: trade-specific stuff

                    Region region = owner.GetRegion();
                    if (region == null)
                        return;

                    InventoryPrototype inventoryProto = InventoryLocation?.InventoryPrototype;
                    if (inventoryProto == null)
                        return;

                    if (inventoryProto.IsPlayerGeneralInventory == false && inventoryProto.IsEquipmentInventory == false)
                        return;

                    if (delta > 0)
                        region.PlayerCollectedItemEvent.Invoke(new(owner, this, delta));
                    else if (delta < 0)
                        region.PlayerLostItemEvent.Invoke(new(owner, this, delta));

                    break;

                case PropertyEnum.PetItemDonationCount:
                    if (IsPetItem == false)
                    {
                        Logger.Warn("OnPropertyChange(): IsPetItem == false");
                        return;
                    }

                    if (ItemPrototype.IsPetTechAffixUnlocked(this, id))
                    {
                        Property.FromParam(id, 0, out int updatedAffixPos);
                        AwardPetTechAffix((AffixPosition)updatedAffixPos);
                    }
                    else if (newValue == 0 && oldValue != 0)
                    {
                        Property.FromParam(id, 0, out int updatedAffixPos);
                        if (RemovePetTechAffix((AffixPosition)updatedAffixPos) == false)
                            Logger.Warn($"OnPropertyChange(): Failed to remove pet tech affix props!\n Item: {this}\n AffixPos: {(AffixPosition)updatedAffixPos}");
                    }

                    break;
            }
        }

        public override InventoryResult CanChangeInventoryLocation(Inventory destInventory, out PropertyEnum propertyRestriction)
        {
            InventoryResult baseResult = base.CanChangeInventoryLocation(destInventory, out propertyRestriction);
            if (baseResult != InventoryResult.Success)
                return baseResult;

            // Check binding if needed (mirrors CItem::CanChangeInventoryLocation())
            if (IsBoundToCharacter && TestStatus(EntityStatus.SkipItemBindingCheck) == false)
            {
                Player player = GetOwnerOfType<Player>();
                if (player == null) return Logger.WarnReturn(InventoryResult.Invalid, "CanChangeInventoryLocation(): player == null");

                Entity destInvOwner = Game.EntityManager.GetEntity<Entity>(destInventory.OwnerId);
                if (destInvOwner == null) return Logger.WarnReturn(InventoryResult.Invalid, "CanChangeInventoryLocation(): destInvOwner == null");

                Player destInvOwnerAsPlayer = destInvOwner.GetSelfOrOwnerOfType<Player>();
                if (destInvOwnerAsPlayer == null) return Logger.WarnReturn(InventoryResult.Invalid, "CanChangeInventoryLocation(): destInvOwnerAsPlayer == null");

                if (player != destInvOwnerAsPlayer)
                    return InventoryResult.InvalidBound;
            }

            return InventoryResult.Success;
        }

        public bool CanUse(Agent agent, bool checkPower = true, bool checkInventory = true)
        {
            if (agent is not Avatar avatar)
                return false;

            Player player = avatar.GetOwnerOfType<Player>();
            if (player == null)
                return false;

            return PlayerCanUse(player, avatar, checkPower, checkInventory) == InteractionValidateResult.Success;
        }

        public bool PlayerCanMove(Player player, InventoryLocation destInvLoc, out InventoryResult result, out PropertyEnum resultProperty, out Item resultItem)
        {
            result = InventoryResult.Invalid;
            resultProperty = PropertyEnum.Invalid;
            resultItem = null;

            // Validate ownership
            if (player.Owns(this) == false) return Logger.WarnReturn(false, "PlayerCanMove(): player.Owns(this) == false");

            // Validate inventories
            InventoryLocation fromInvLoc = InventoryLocation;

            if (fromInvLoc.IsValid == false)
                return Logger.WarnReturn(false, $"PlayerCanMove() is being called with a fromInvLoc that isn't valid (the pickup interaction should be used for that case!)\nItem: [{this}]");

            if (destInvLoc.IsValid == false)
                return Logger.WarnReturn(false, $"PlayerCanMove() is being called with a destInvLoc that isn't valid (RequestItemTrash() should be used for that case!)\nItem: [{this}]");

            Entity fromInventoryOwner = Game.EntityManager.GetEntity<Entity>(fromInvLoc.ContainerId);
            if (fromInventoryOwner == null)
                return Logger.WarnReturn(false, $"PlayerCanMove(): Unable to get source owner sourceInvLoc=[{fromInvLoc}] when moving [{this}]");

            Inventory fromInventory = fromInventoryOwner.GetInventoryByRef(fromInvLoc.InventoryRef);
            if (fromInventory == null)
                return Logger.WarnReturn(false, $"PlayerCanMove(): Invalid source inventory for sourceInvLoc=[{fromInvLoc}], sourceOwner=[{fromInventoryOwner}], when moving [{this}]");

            Entity toInventoryOwner = Game.EntityManager.GetEntity<Entity>(destInvLoc.ContainerId);
            if (toInventoryOwner == null)
                return Logger.WarnReturn(false, $"PlayerCanMove(): Unable to get destination owner destInvLoc=[{destInvLoc}] when moving [{this}]");

            Inventory toInventory = toInventoryOwner.GetInventoryByRef(destInvLoc.InventoryRef);
            if (toInventory == null)
                return Logger.WarnReturn(false, $"PlayerCanMove(): Invalid dest inventory [{destInvLoc}] on destOwner=[{toInventoryOwner}] when moving [{this}]");

            // Check if this item can be moved to the requested destination
            result = CanChangeInventoryLocation(toInventory, out resultProperty);
            if (result != InventoryResult.Success)
                return false;

            result = Avatar.ValidateEquipmentChange(Game, this, fromInvLoc, destInvLoc, out resultItem);
            if (result != InventoryResult.Success)
                return false;

            result = player.ValidatePlayerInventoryMoveConstraints(fromInvLoc, destInvLoc);
            if (result != InventoryResult.Success)
                return false;

            // Check the destination slot
            ulong entityIdInSlot = toInventory.GetEntityInSlot(destInvLoc.Slot);
            if (entityIdInSlot == InvalidId)
            {
                // Make sure there is a free slot
                if (toInventory.IsSlotFree(destInvLoc.Slot) == false)
                {
                    result = InventoryResult.SlotAlreadyOccupied;
                    return false;
                }
            }
            else
            {
                Item itemInSlot = Game.EntityManager.GetEntity<Item>(entityIdInSlot);
                if (itemInSlot != null && CanStackOnto(itemInSlot) == false)
                {
                    // If two items can't stack, it means they needs to be swapped.
                    // Check if the item in the destination slot can be swapped with this item's current location.
                    result = itemInSlot.CanChangeInventoryLocation(fromInventory, out resultProperty);
                    if (result != InventoryResult.Success)
                        return false;

                    result = Avatar.ValidateEquipmentChange(Game, itemInSlot, destInvLoc, fromInvLoc, out resultItem);
                    if (result != InventoryResult.Success)
                        return false;

                    result = player.ValidatePlayerInventoryMoveConstraints(destInvLoc, fromInvLoc);
                    if (result != InventoryResult.Success)
                        return false;
                }
            }

            return result == InventoryResult.Success;
        }

        public bool PlayerCanDestroy(Player player)
        {
            if (player.Owns(this) == false)
                return false;

            ItemPrototype itemProto = ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, "PlayerCanDestroy(): itemProto == null");

            if (itemProto.CanBeDestroyed == false)
                return false;

            if (Avatar.ValidateEquipmentChange(Game, this, InventoryLocation, InventoryLocation.Invalid, out _) != InventoryResult.Success)
                return false;

            return true;
        }

        public bool CanBeEquippedWithItem(Item otherItem)
        {
            ItemPrototype itemProto = ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, "CanBeEquippedWithItem(): itemProto == null");

            ItemPrototype otherItemProto = otherItem?.ItemPrototype;
            if (otherItemProto == null) return Logger.WarnReturn(false, "CanBeEquippedWithItem(): otherItemProto == null");

            return CanBeEquippedWithItemHelper(itemProto, otherItem) && CanBeEquippedWithItemHelper(otherItemProto, this);
        }

        private static bool CanBeEquippedWithItemHelper(ItemPrototype itemProto, Item otherItem)
        {
            if (itemProto.CannotEquipWithItemsOfKeyword.IsNullOrEmpty())
                return true;

            foreach (PrototypeId keywordProtoRef in itemProto.CannotEquipWithItemsOfKeyword)
            {
                if (otherItem.HasKeyword(keywordProtoRef))
                    return false;
            }

            return true;
        }

        public CraftingResult CanCraftRecipe(Player player, List<ulong> ingredientIds, WorldEntity vendor, bool isRecraft)
        {
            EntityManager entityManager = Game.EntityManager;

            // If this isn't a recraft, the results inventory needs to be empty
            if (isRecraft == false)
            {
                Inventory resultsInv = player.GetInventory(InventoryConvenienceLabel.CraftingResults);
                if (resultsInv == null) return Logger.WarnReturn(CraftingResult.CraftingFailed, "CanCraftRecipe(): resultsInv == null");

                if (resultsInv.Count > 0)
                    return CraftingResult.CraftingFailed;
            }

            // Validate vendor
            CraftingResult vendorResult = player.CanCraftRecipeWithVendor(0, this, vendor);
            if (vendorResult != CraftingResult.Success)
                return vendorResult;

            // Validate ownership
            if (player.Owns(this) == false)
                return CraftingResult.CraftingFailed;

            // Validate the recipe
            if (ItemPrototype is not CraftingRecipePrototype craftingRecipeProto)
                return CraftingResult.CraftingFailed;

            if (craftingRecipeProto.IsLiveTuningEnabled() == false)
                return CraftingResult.RecipeDisabledByLiveTuning;

            PrototypeId vendorTypeProtoRef = vendor.Properties[PropertyEnum.VendorType];
            VendorTypePrototype vendorTypeProto = vendorTypeProtoRef.As<VendorTypePrototype>();
            if (vendorTypeProto == null) return Logger.WarnReturn(CraftingResult.CraftingFailed, "CanCraftRecipe(): vendorTypeProto == null");

            bool isInRecipeLibrary = false;

            List<PrototypeId> inventoryList = ListPool<PrototypeId>.Instance.Get();
            if (vendorTypeProto.GetInventories(inventoryList))
            {
                foreach (PrototypeId crafterVendorInvProtoRef in inventoryList)
                {
                    Inventory crafterVendorInv = player.GetInventoryByRef(crafterVendorInvProtoRef);
                    if (crafterVendorInv == null)
                    {
                        Logger.Warn("CanCraftRecipe(): crafterVendorInv == null");
                        continue;
                    }

                    foreach (var entry in crafterVendorInv)
                    {
                        Item vendorRecipe = entityManager.GetEntity<Item>(entry.Id);
                        if (vendorRecipe == null)
                        {
                            Logger.Warn("CanCraftRecipe(): vendorRecipe == null");
                            continue;
                        }

                        if (vendorRecipe.PrototypeDataRef == PrototypeDataRef)
                        {
                            isInRecipeLibrary = true;
                            break;
                        }
                    }

                    if (isInRecipeLibrary)
                        break;
                }
            }

            ListPool<PrototypeId>.Instance.Return(inventoryList);

            if (isInRecipeLibrary == false)
                return CraftingResult.RecipeNotInRecipeLibrary;

            // Validate ingredients
            CraftingResult ingredientsResult = craftingRecipeProto.ValidateIngredients(player, ingredientIds);
            if (ingredientsResult != CraftingResult.Success)
                return ingredientsResult;

            // Validate cost
            CurrencyGlobalsPrototype currencyGlobals = GameDatabase.CurrencyGlobalsPrototype;

            using PropertyCollection currencyCost = ObjectPoolManager.Instance.Get<PropertyCollection>();
            if (craftingRecipeProto.GetCraftingCost(player, ingredientIds, out uint creditsCost, out uint legendaryMarksCost, currencyCost) == false)
                return CraftingResult.InsufficientIngredients;

            if (creditsCost > 0 && player.Properties[PropertyEnum.Currency, currencyGlobals.Credits] < creditsCost)
                return CraftingResult.InsufficientCredits;

            if (legendaryMarksCost > 0 && player.Properties[PropertyEnum.Currency, currencyGlobals.LegendaryMarks] < legendaryMarksCost)
                return CraftingResult.InsufficientLegendaryMarks;

            foreach (var kvp in currencyCost.IteratePropertyRange(PropertyEnum.Currency))
            {
                // Other currencies use a generic error
                uint cost = kvp.Value;
                if (player.Properties[kvp.Key] < cost)
                    return CraftingResult.InsufficientIngredients;
            }

            return CraftingResult.Success;
        }

        public bool IsGear(AvatarPrototype avatarProto)
        {
            if (Prototype is not ArmorPrototype armorProto) return false;

            return armorProto.GetInventorySlotForAgent(avatarProto) switch
            {
                EquipmentInvUISlot.Gear01
                or EquipmentInvUISlot.Gear02
                or EquipmentInvUISlot.Gear03
                or EquipmentInvUISlot.Gear04
                or EquipmentInvUISlot.Gear05 => true,
                _ => false,
            };
        }

        public bool HasAffixInPosition(AffixPosition affixPosition)
        {
            foreach (AffixPropertiesCopyEntry copyEntry in _affixProperties)
            {
                if (copyEntry.AffixProto == null)
                {
                    Logger.Warn("HasAffixInPosition(): copyEntry.AffixProto == null");
                    continue;
                }

                if (copyEntry.AffixProto.Position == affixPosition)
                    return true;
            }

            return false;
        }

        public bool IsPetTechFullyUpgraded()
        {
            if (IsPetItem == false) return Logger.WarnReturn(false, "IsPetTechFullyUpgraded(): IsPetItem == false");

            for (AffixPosition position = AffixPosition.PetTech1; position <= AffixPosition.PetTech5; position++)
            {
                if (ItemPrototype.IsPetTechAffixUnlocked(this, position) == false)
                    return false;
            }

            return true;
        }

        public PrototypeId GetBoundAgentProtoRef()
        {
            _itemSpec.GetBindingState(out PrototypeId agentProtoRef);
            return agentProtoRef;
        }

        public bool SetBinding(bool bound, PrototypeId agentProtoRef = PrototypeId.Invalid, bool? tradeRestricted = null)
        {
            if (_itemSpec.SetBindingState(bound, agentProtoRef, tradeRestricted) == false)
                return false;

            Player player = GetOwnerOfType<Player>();
            if (player != null && player.InterestedInEntity(this, AOINetworkPolicyValues.AOIChannelOwner))
            {
                var messageBuilder = NetMessageItemBindingChanged.CreateBuilder()
                    .SetItemId(Id)
                    .SetAccountBound(bound)
                    .SetCharacterProtoId((ulong)agentProtoRef);

                if (tradeRestricted != null)
                    messageBuilder.SetTradeRestricted(tradeRestricted == true);

                player.SendMessage(messageBuilder.Build());
            }

            return true;
        }

        public bool GetPowerGranted(out PrototypeId powerProtoRef)
        {
            powerProtoRef = PrototypeId.Invalid;

            PrototypeId onUsePower = OnUsePower;
            if (onUsePower != PrototypeId.Invalid)
            {
                powerProtoRef = onUsePower;
                return true;
            }

            PrototypeId onEquipPower = OnEquipPower;
            if (onEquipPower != PrototypeId.Invalid)
            {
                powerProtoRef = onEquipPower;
                return true;
            }

            return false;
        }

        public uint GetVendorBaseXPGain(Player player)
        {
            if (player == null) return Logger.WarnReturn(0u, "GetVendorBaseXPGain(): player == null");
            float xpGain = GetSellPrice(player);
            xpGain *= LiveTuningManager.GetLiveGlobalTuningVar(Gazillion.GlobalTuningVar.eGTV_VendorXPGain);
            return (uint)xpGain;
        }

        public uint GetVendorXPGain(WorldEntity vendor, Player player)
        {
            if (player == null) return Logger.WarnReturn(0u, "GetVendorXPGain(): player == null");

            // This eval simply returns 1 even back in 1.10
            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, Properties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, vendor?.Properties);
            float xpMult = Eval.RunFloat(GameDatabase.AdvancementGlobalsPrototype.VendorLevelingEval, evalContext);

            uint baseXPGain = GetVendorBaseXPGain(player);
            return (uint)(baseXPGain * xpMult);
        }

        public uint GetSellPrice(Player player)
        {
            if (player == null) return Logger.WarnReturn(0u, "GetSellPrice(): player == null");

            ItemPrototype itemProto = Prototype as ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(0u, "GetSellPrice(): proto == null");

            return itemProto.Cost != null ? (uint)itemProto.Cost.GetSellPriceInCredits(player, this) : 0u;
        }

        public static int GetEquippableAtLevelForItemLevel(int itemLevel)
        {
            AdvancementGlobalsPrototype advanGlobalsProto = GameDatabase.AdvancementGlobalsPrototype;
            if (advanGlobalsProto == null) return Logger.WarnReturn(0, "GetEquippableAtLevelForItemLevel(): advanGlobalsProto == null");

            Curve itemEquipReqOffsetCurve = CurveDirectory.Instance.GetCurve(advanGlobalsProto.ItemEquipRequirementOffset);
            if (itemEquipReqOffsetCurve == null) return Logger.WarnReturn(0, "GetEquippableAtLevelForItemLevel(): itemEquipReqOffsetCurve == null");

            return Math.Clamp(itemLevel + itemEquipReqOffsetCurve.GetIntAt(itemLevel), 1, advanGlobalsProto.GetAvatarLevelCap());
        }

        public TimeSpan GetExpirationTime()
        {
            PrototypeId rarityProtoRef = Properties[PropertyEnum.ItemRarity];
            return ItemPrototype.GetExpirationTime(rarityProtoRef);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);
            sb.AppendLine($"{nameof(_itemSpec)}: {_itemSpec}");
        }

        public bool DecrementStack(int count = 1)
        {
            if (count < 1) return Logger.WarnReturn(false, "DecrementStack(): count < 1");

            int currentStackSize = CurrentStackSize;
            if (count > currentStackSize) return Logger.WarnReturn(false, "DecrementStack(): count > currentStackSize");

            int newCount = Math.Max(0, currentStackSize - count);

            if (newCount > 0)
                Properties[PropertyEnum.InventoryStackCount] = newCount;
            else
                ScheduleDestroyEvent(TimeSpan.Zero);

            return true;
        }

        public InventoryResult SplitStack(InventoryLocation toInvLoc, int count)
        {
            // Some stacks are not splittable
            if (StacksCanBeSplit == false)
                return InventoryResult.StacksNotSplittable;

            // Check if there is enough stuff in the stack for the requested split
            if (CurrentStackSize < (count + 1))
                return InventoryResult.SplitParamExceedsStackSize;

            // Cannot split to the same slot
            InventoryLocation fromInvLoc = InventoryLocation;
            if (fromInvLoc.Equals(toInvLoc))
                return InventoryResult.InvalidSlotParam;

            // Check inventories
            Player player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(InventoryResult.PlayerOwnerNotFound, "SplitStack(): player == null");

            Entity fromInventoryOwner = Game.EntityManager.GetEntity<Entity>(fromInvLoc.ContainerId);
            if (fromInventoryOwner == null) return Logger.WarnReturn(InventoryResult.InventoryHasNoOwner, "SplitStack(): fromInventoryOwner == null");

            Inventory fromInventory = fromInventoryOwner.GetInventoryByRef(fromInvLoc.InventoryRef);
            if (fromInventory == null) return Logger.WarnReturn(InventoryResult.NoAvailableInventory, "SplitStack(): fromInventory == null");

            Entity toInventoryOwner = Game.EntityManager.GetEntity<Entity>(toInvLoc.ContainerId);
            if (toInventoryOwner == null) return Logger.WarnReturn(InventoryResult.InventoryHasNoOwner, "SplitStack(): toInventoryOwner == null");

            Inventory toInventory = toInventoryOwner.GetInventoryByRef(toInvLoc.InventoryRef);
            if (toInventory == null) return Logger.WarnReturn(InventoryResult.InvalidReceivingInventory, "SplitStack(): toInventory == null");

            // Find a free slot if needed
            if (toInvLoc.Slot == Inventory.InvalidSlot)
            {
                uint freeSlot = toInventory.GetFreeSlot(this, true);
                if (freeSlot == Inventory.InvalidSlot)
                    return InventoryResult.InventoryFull;

                toInvLoc.Set(toInvLoc.ContainerId, toInvLoc.InventoryRef, freeSlot);
            }

            // Do move validation
            if (PlayerCanMove(player, toInvLoc, out InventoryResult canMoveResult, out _, out _) == false)
                return canMoveResult;

            // Do the split
            InventoryResult splitResult = DoStackSplit(toInvLoc, toInventory, count, out ulong newItemId);

            if (splitResult != InventoryResult.Success)
            {
                Logger.Error($"SplitStack(): FAILED for item [{this}] belonging to player [{player}], reason=[{splitResult}]");

                // Clean up the newly created item if something went wrong (hopefully this never ever happens)
                Item newItem = Game.EntityManager.GetEntity<Item>(newItemId);
                if (newItem != null)
                {
                    InventoryResult errorRecoveryResult = newItem.ChangeInventoryLocation(fromInventory, fromInvLoc.Slot);
                    if (errorRecoveryResult != InventoryResult.Success)
                    {
                        Logger.Error($"SplitStack(): ERROR RECOVERY FAILED for item [{this}] belonging to player [{player}], reason=[{errorRecoveryResult}], something has gone REALLY wrong");
                        newItem.Destroy();
                    }
                }
            }

            return splitResult;
        }

        private InventoryResult DoStackSplit(InventoryLocation toInvLoc, Inventory toInventory, int count, out ulong newItemId)
        {
            newItemId = InvalidId;

            // Remove from the original stack
            DecrementStack(count);

            // Create a new stack
            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = PrototypeDataRef;
            settings.ItemSpec = new(ItemSpec);
            settings.ItemSpec.StackCount = count;

            Item newItem = Game.EntityManager.CreateEntity(settings) as Item;
            if (newItem == null)
                return InventoryResult.ErrorCreatingNewSplitEntity;

            newItemId = newItem.Id;

            // Move the created item to the destination, ignoring binding checks
            newItem.SetStatus(EntityStatus.SkipItemBindingCheck, true);
            
            ulong? stackEntityId = 0;
            InventoryResult moveResult = newItem.ChangeInventoryLocation(toInventory, toInvLoc.Slot, ref stackEntityId, true);
            
            newItem.SetStatus(EntityStatus.SkipItemBindingCheck, false);

            if (stackEntityId == Id)
                return Logger.WarnReturn(InventoryResult.UnknownFailure, $"DoStackSplit(): Splitting stack [{this}] resulted in the item being stacked with itself");

            return moveResult; 
        }

        public void SetRecentlyAdded(bool value)
        {
            Properties[PropertyEnum.ItemRecentlyAddedGlint] = value;
            Properties[PropertyEnum.ItemRecentlyAddedToInventory] = value;
        }

        public void SetScenarioProperties(PropertyCollection properties)
        {
            properties.CopyProperty(Properties, PropertyEnum.DifficultyTier);
            properties.CopyPropertyRange(Properties, PropertyEnum.RegionAffix);
            properties.CopyProperty(Properties, PropertyEnum.RegionAffixDifficulty);

            PrototypeId itemRarityRef = Properties[PropertyEnum.ItemRarity];
            var itemRarityProto = itemRarityRef.As<RarityPrototype>();
            if (itemRarityProto != null)
                properties[PropertyEnum.ItemRarity] = itemRarityRef;

            var affixLimits = ItemPrototype.GetAffixLimits(itemRarityRef, LootContext.Drop);
            if (affixLimits != null)
            {
                properties[PropertyEnum.DifficultyIndex] = affixLimits.RegionDifficultyIndex;
                properties[PropertyEnum.DamageRegionMobToPlayer] = affixLimits.DamageRegionMobToPlayer;
                properties[PropertyEnum.DamageRegionPlayerToMob] = affixLimits.DamageRegionPlayerToMob;
            }

            properties[PropertyEnum.DangerRoomScenarioItemDbGuid] = DatabaseUniqueId;
        }

        private bool ApplyItemSpec(ItemSpec itemSpec)
        {
            if (itemSpec.IsValid == false) return Logger.WarnReturn(false, $"ApplyItemSpec(): Invalid ItemSpec on Item {this}!");

            _itemSpec.Set(itemSpec);

            ItemPrototype itemProto = ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, "ApplyItemSpec(): itemProto == null");

            if (ApplyItemSpecProperties() == false)
                return Logger.WarnReturn(false, "ApplyItemSpec(): Failed to apply ItemSpec properties");

            itemProto.OnApplyItemSpec(this, _itemSpec);

            GRandom random = new(_itemSpec.Seed);

            // Apply built-in properties
            if (itemProto.PropertiesBuiltIn.HasValue())
            {
                foreach (PropertyEntryPrototype propertyEntryProto in itemProto.PropertiesBuiltIn)
                {
                    float randomMult = random.NextFloat();

                    if (propertyEntryProto is PropertyPickInRangeEntryPrototype pickInRangeProto)
                        OnBuiltInPropertyRoll(randomMult, pickInRangeProto);
                    else if (propertyEntryProto is PropertySetEntryPrototype setProto)
                        OnBuiltInPropertySet(setProto);
                    else
                        Logger.Warn($"ApplyItemSpec(): Invalid property entry prototype {propertyEntryProto}");
                }
            }

            // NOTE: RNG is reseeded for each affix individually.
            // Save the current state of random to restore it later for rolling action index.
            int indexSeed = random.GetSeed();

            // Apply built-in affixes
            List<BuiltInAffixDetails> detailsList = ListPool<BuiltInAffixDetails>.Instance.Get();
            if (itemProto.GenerateBuiltInAffixDetails(_itemSpec, detailsList))
            {
                foreach (BuiltInAffixDetails builtInAffixDetails in detailsList)
                {
                    AffixPrototype affixProto = builtInAffixDetails.AffixEntryProto.Affix.As<AffixPrototype>();
                    if (affixProto == null)
                    {
                        Logger.Warn("ApplyItemSpec(): affixProto == null");
                        continue;
                    }

                    random.Seed(builtInAffixDetails.Seed);
                    OnAffixAdded(random, affixProto, builtInAffixDetails.ScopeProtoRef, builtInAffixDetails.AvatarProtoRef, builtInAffixDetails.LevelRequirement);
                }
            }

            ListPool<BuiltInAffixDetails>.Instance.Return(detailsList);

            // Apply rolled affixes
            IReadOnlyList<AffixSpec> affixSpecs = _itemSpec.AffixSpecs;
            for (int i = 0; i < affixSpecs.Count; i++)
            {
                AffixSpec affixSpec = affixSpecs[i];

                if (affixSpec.Seed == 0) return Logger.WarnReturn(false, "ApplyItemSpec(): affixSpec.Seed == 0");
                random.Seed(affixSpec.Seed);
                
                if (affixSpec.AffixProto == null) return Logger.WarnReturn(false, "ApplyItemSpec(): affixSpec.AffixProto == null");

                OnAffixAdded(random, affixSpec.AffixProto, affixSpec.ScopeProtoRef, _itemSpec.EquippableBy, 0);
            }

            // Pick triggered power
            ItemActionSetPrototype triggeredActions = itemProto.ActionsTriggeredOnItemEvent;
            if (triggeredActions != null && triggeredActions.Choices.HasValue())
            {
                if (triggeredActions.PickMethod == PickMethod.PickWeight)
                {
                    // Restore the previously saved index seed so that affixes don't affect which index gets picked.
                    random.Seed(indexSeed);
                    Picker<int> picker = new(random);

                    for (int i = 0; i < triggeredActions.Choices.Length; i++)
                    {
                        ItemActionBasePrototype actionProto = triggeredActions.Choices[i];

                        if (actionProto.Weight <= 0)
                            continue;

                        picker.Add(i, actionProto.Weight);
                    }

                    picker.Pick(out int index);
                    OnItemEventRoll(index);
                }

                OnUsePower = GetTriggeredPower(ItemEventType.OnUse, ItemActionType.UsePower);
                OnEquipPower = GetTriggeredPower(ItemEventType.OnEquip, ItemActionType.AssignPower);
            }

            return true;
        }

        private bool ApplyItemSpecProperties()
        {
            // We can skip some validation here because this is called only from ApplyItemSpec()
            ItemPrototype itemProto = ItemPrototype;

            // Apply rarity
            RarityPrototype rarityProto = GameDatabase.GetPrototype<RarityPrototype>(_itemSpec.RarityProtoRef);
            if (rarityProto == null) return Logger.WarnReturn(false, "ApplyItemSpec(): rarityProto == null");
            Properties[PropertyEnum.ItemRarity] = _itemSpec.RarityProtoRef;

            // Apply level and level requirement
            int itemLevel = Math.Max(1, _itemSpec.ItemLevel);
            Properties[PropertyEnum.ItemLevel] = Math.Max(1, _itemSpec.ItemLevel);
            Properties[PropertyEnum.Requirement, PropertyEnum.CharacterLevel] = (float)GetEquippableAtLevelForItemLevel(itemLevel);

            // Apply binding settings
            if (itemProto.BindingSettings != null)
            {
                // Apply default settings
                ItemBindingSettingsEntryPrototype defaultSettings = itemProto.BindingSettings.DefaultSettings;
                if (defaultSettings != null)
                {
                    Properties[PropertyEnum.ItemBindsToAccountOnPickup] = defaultSettings.BindsToAccountOnPickup;
                    Properties[PropertyEnum.ItemBindsToCharacterOnEquip] = defaultSettings.BindsToCharacterOnEquip;
                    Properties[PropertyEnum.ItemIsTradable] = defaultSettings.IsTradable;
                }

                // Override with rarity settings if there are any
                if (itemProto.BindingSettings.PerRaritySettings != null)
                {
                    foreach (ItemBindingSettingsEntryPrototype perRaritySettingProto in itemProto.BindingSettings.PerRaritySettings)
                    {
                        if (perRaritySettingProto.RarityFilter != _itemSpec.RarityProtoRef)
                            continue;

                        Properties[PropertyEnum.ItemBindsToAccountOnPickup] = perRaritySettingProto.BindsToAccountOnPickup;
                        Properties[PropertyEnum.ItemBindsToCharacterOnEquip] = perRaritySettingProto.BindsToCharacterOnEquip;
                        Properties[PropertyEnum.ItemIsTradable] = perRaritySettingProto.IsTradable;
                    }
                }
            }

            // Apply stack settings
            ItemStackSettingsPrototype stackSettings = itemProto.StackSettings;
            if (stackSettings != null)
            {
                Properties[PropertyEnum.InventoryStackSizeMax] = stackSettings.MaxStacks;
                Properties[PropertyEnum.ItemLevel] = stackSettings.ItemLevelOverride;
                Properties[PropertyEnum.Requirement, PropertyEnum.CharacterLevel] = (float)stackSettings.RequiredCharLevelOverride;
            }

            // Apply rarity bonus to item level
            Properties.AdjustProperty(rarityProto.ItemLevelBonus, PropertyEnum.ItemLevel);

            // Apply random variation using item spec seed
            GRandom random = new(_itemSpec.Seed);
            Properties[PropertyEnum.ItemVariation] = random.NextFloat();

            return true;
        }

        public bool InteractWithAvatar(Avatar avatar)
        {
            Player player = avatar?.GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "InteractWithAvatar(): player == null");

            ItemPrototype itemProto = ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, "InteractWithAvatar(): itemProto == null");

            if (PlayerCanUse(player, avatar) != InteractionValidateResult.Success)
                return false;

            bool wasUsed = false;
            bool isConsumable = false;

            if (itemProto.ActionsTriggeredOnItemEvent != null && itemProto.ActionsTriggeredOnItemEvent.Choices.HasValue())
            {
                if (itemProto.ActionsTriggeredOnItemEvent.PickMethod == PickMethod.PickWeight)
                {
                    // Do just the action that was picked when this item was rolled
                    ItemActionBasePrototype[] choices = itemProto.ActionsTriggeredOnItemEvent.Choices;

                    int actionIndex = Properties[PropertyEnum.ItemEventActionIndex];
                    if (actionIndex < 0 || actionIndex >= choices.Length)
                        return Logger.WarnReturn(false, "InteractWithAvatar(): actionIndex < 0 || actionIndex >= choices.Length");

                    Prototype choiceProto = choices[actionIndex];
                    if (choiceProto == null) return Logger.WarnReturn(false, "InteractWithAvatar(): choiceProto == null");

                    // Action entries can be single actions or action sets

                    // First check if the picked action is a set
                    if (choiceProto is ItemActionSetPrototype actionSetProto)
                    {
                        // Only the top level action index is rolled, so we can't have any RNG in action sets
                        if (actionSetProto.PickMethod != PickMethod.PickAll)
                            return Logger.WarnReturn(false, "InteractWithAvatar(): actionSetProto.PickMethod != PickMethod.PickAll");

                        if (actionSetProto.Choices == null)
                            return Logger.WarnReturn(false, "InteractWithAvatar(): actionSetProto.Choices == null");

                        foreach (ItemActionBasePrototype actionBaseProto in actionSetProto.Choices)
                        {
                            if (actionBaseProto is not ItemActionPrototype actionProto)
                            {
                                // Nesting of action sets is not supported by this system
                                Logger.Warn("InteractWithAvatar(): actionBaseProto is not ItemActionPrototype itemActionProto");
                                continue;
                            }

                            TriggerItemActionOnUse(actionProto, player, avatar, ref wasUsed, ref isConsumable);
                        }
                    }
                    else if (choiceProto is ItemActionPrototype actionProto)
                    {
                        // If this is not a set, handle it as a single action
                        TriggerItemActionOnUse(actionProto, player, avatar, ref wasUsed, ref isConsumable);
                    }
                }
                else if (itemProto.ActionsTriggeredOnItemEvent.PickMethod == PickMethod.PickAll)
                {
                    // Do all actions OnUse actions if this item doesn't use random actions

                    foreach (ItemActionBasePrototype actionBaseProto in itemProto.ActionsTriggeredOnItemEvent.Choices)
                    {
                        // PickAll is not compatible with action sets
                        if (actionBaseProto is not ItemActionPrototype actionProto)
                        {
                            Logger.Warn("InteractWithAvatar(): actionBaseProto is not ItemActionPrototype itemActionProto");
                            continue;
                        }

                        TriggerItemActionOnUse(actionProto, player, avatar, ref wasUsed, ref isConsumable);
                    }
                }
            }

            // Do special interactions for specific item types
            switch (itemProto)
            {
                case CharacterTokenPrototype characterTokenProto:
                    wasUsed |= DoCharacterTokenInteraction(characterTokenProto, player, avatar);
                    break;

                case InventoryStashTokenPrototype inventoryStashTokenProto:
                    wasUsed |= DoInventoryStashTokenInteraction(inventoryStashTokenProto, player);
                    break;

                case EmoteTokenPrototype emoteTokenProto:
                    wasUsed |= DoEmoteTokenInteraction(emoteTokenProto, player);
                    break;

                case CraftingRecipePrototype craftingRecipeProto:
                    wasUsed |= DoCraftingRecipeInteraction(craftingRecipeProto, player);
                    break;
            }

            // Consume if this is a consumable item that was successfully used
            // NOTE: Power-based consumable items get consumed when their power is activated in OnUsePowerActivated().
            if (isConsumable && wasUsed)
                DecrementStack();

            return true;
        }

        private bool DoCharacterTokenInteraction(CharacterTokenPrototype characterTokenProto, Player player, Avatar avatar)
        {
            bool wasUsed = false;

            PrototypeId characterProtoRef = characterTokenProto.Character;

            EntityPrototype characterProto = characterTokenProto.Character.As<EntityPrototype>();
            if (characterProto == null) return Logger.WarnReturn(false, "DoCharacterTokenInteraction(): characterProto == null");

            if (characterProto is AvatarPrototype)
            {
                if (characterTokenProto.GrantsCharacterUnlock && player.HasAvatarFullyUnlocked(characterProtoRef) == false)
                {
                    wasUsed = player.UnlockAvatar(characterProtoRef, true);
                }

                if (wasUsed == false && characterTokenProto.GrantsUltimateUpgrade)
                {
                    // Upgrade ultimate if the token wasn't used to unlock the avatar
                    if (avatar.PrototypeDataRef != characterTokenProto.Character) return Logger.WarnReturn(false, "DoCharacterTokenInteraction(): avatar.PrototypeDataRef != characterTokenProto.Character");

                    if (avatar.CanUpgradeUltimate() == InteractionValidateResult.Success)
                    {
                        avatar.Properties.AdjustProperty(1, PropertyEnum.AvatarPowerUltimatePoints);
                        wasUsed = true;
                    }
                }
            }
            else if (characterProto is AgentTeamUpPrototype)
            {
                if (player.IsTeamUpAgentUnlocked(characterProtoRef) == false)
                    wasUsed = player.UnlockTeamUpAgent(characterProtoRef);
            }

            return wasUsed;
        }

        private bool DoInventoryStashTokenInteraction(InventoryStashTokenPrototype inventoryStashTokenProto, Player player)
        {
            PrototypeId invStashProtoRef = inventoryStashTokenProto.Inventory;

            if (player.IsInventoryUnlocked(invStashProtoRef))
                return false;

            if (player.UnlockInventory(invStashProtoRef) == false)
                return Logger.WarnReturn(false, $"DoInventoryStashTokenInteraction(): Failed to unlock inventory {invStashProtoRef.GetName()} for player [{player}] by interacting with [{this}]");

            return true;
        }

        private bool DoEmoteTokenInteraction(EmoteTokenPrototype emoteTokenProto, Player player)
        {
            PrototypeId avatarDataRef = emoteTokenProto.Avatar;
            if (avatarDataRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "DoEmoteTokenInteraction(): avatarDataRef == PrototypeId.Invalid");

            PrototypeId emotePowerDataRef = emoteTokenProto.EmotePower;
            if (emotePowerDataRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "DoEmoteTokenInteraction(): emotePowerDataRef == PrototypeId.Invalid");

            if (player.HasAvatarEmoteUnlocked(avatarDataRef, emotePowerDataRef))
                return false;

            if (player.UnlockAvatarEmote(avatarDataRef, emotePowerDataRef) == false)
                return Logger.WarnReturn(false, $"DoEmoteTokenInteraction(): Failed to unlock emote! avatar=[{avatarDataRef.GetName()}], emote=[{emotePowerDataRef.GetName()}], item=[{this}], player=[{player}]");

            return true;
        }

        private bool DoCraftingRecipeInteraction(CraftingRecipePrototype craftingRecipeProto, Player player)
        {
            InventoryPrototype containingInvProto = InventoryLocation.InventoryPrototype;
            if (containingInvProto == null) return Logger.WarnReturn(false, "DoCraftingRecipeInteraction(): containingInvProto == null");

            // This should have already been validated in PlayerCanUseCraftingRecipe()
            if (containingInvProto.IsPlayerGeneralInventory == false)
                return Logger.WarnReturn(false, $"DoCraftingRecipeInteraction(): Player [{player}] attempting to use a crafting recipe from inventory {containingInvProto.ConvenienceLabel}");

            if (player.HasLearnedCraftingRecipe(craftingRecipeProto.DataRef))
                return Logger.WarnReturn(false, $"DoCraftingRecipeInteraction(): Player [{player}] has already learned crafting recipe {craftingRecipeProto}");

            Inventory learnedRecipeInv = player.GetInventory(InventoryConvenienceLabel.CraftingRecipesLearned);
            if (learnedRecipeInv == null) return Logger.WarnReturn(false, "DoCraftingRecipeInteraction(): learnedRecipeInv == null");

            if (ChangeInventoryLocation(learnedRecipeInv) != InventoryResult.Success)
                return Logger.WarnReturn(false, $"DoCraftingRecipeInteraction(): Recipe [{this}] failed to move to the learned recipe inventory for player [{player}]");

            return true;
        }

        public bool OnUsePowerActivated()
        {
            // This method mostly mirrors InteractWithAvatar, but for the OnUsePowerActivated event

            ItemPrototype itemProto = ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, "OnUsePowerActivated(): itemProto == null");

            if (itemProto.ActionsTriggeredOnItemEvent == null || itemProto.ActionsTriggeredOnItemEvent.Choices.IsNullOrEmpty())
                return true;

            if (itemProto.ActionsTriggeredOnItemEvent.PickMethod == PickMethod.PickWeight)
            {
                // Do just the action that was picked when this item was rolled
                ItemActionBasePrototype[] choices = itemProto.ActionsTriggeredOnItemEvent.Choices;

                int actionIndex = Properties[PropertyEnum.ItemEventActionIndex];
                if (actionIndex < 0 || actionIndex >= choices.Length)
                    return Logger.WarnReturn(false, "OnUsePowerActivated(): actionIndex < 0 || actionIndex >= choices.Length");

                Prototype choiceProto = choices[actionIndex];
                if (choiceProto == null) return Logger.WarnReturn(false, "OnUsePowerActivated(): choiceProto == null");

                // Action entries can be single actions or action sets

                // First check if the picked action is a set
                if (choiceProto is ItemActionSetPrototype actionSetProto)
                {
                    if (actionSetProto.Choices == null)
                        return Logger.WarnReturn(false, "OnUsePowerActivated(): actionSetProto.Choices == null");

                    foreach (ItemActionBasePrototype actionBaseProto in actionSetProto.Choices)
                    {
                        if (actionBaseProto is not ItemActionPrototype actionProto)
                        {
                            // Nesting of action sets is not supported by this system
                            Logger.Warn("OnUsePowerActivated(): actionBaseProto is not ItemActionPrototype actionProto");
                            continue;
                        }

                        if (TriggerItemActionOnUsePowerActivated(actionProto))
                            return true;
                    }
                }
                else if (choiceProto is ItemActionPrototype actionProto)
                {
                    // If this is not a set, handle it as a single action
                    if (TriggerItemActionOnUsePowerActivated(actionProto))
                        return true;
                }
            }
            else if (itemProto.ActionsTriggeredOnItemEvent.PickMethod == PickMethod.PickAll)
            {
                foreach (ItemActionBasePrototype actionBaseProto in itemProto.ActionsTriggeredOnItemEvent.Choices)
                {
                    // PickAll is not compatible with action sets
                    if (actionBaseProto is not ItemActionPrototype actionProto)
                    {
                        Logger.Warn("OnUsePowerActivated(): actionBaseProto is not ItemActionPrototype actionProto");
                        continue;
                    }

                    if (TriggerItemActionOnUsePowerActivated(actionProto))
                        return true;
                }
            }

            return true;
        }

        public int GetAffixLevelCap()
        {
            return GameDatabase.AdvancementGlobalsPrototype.GetItemAffixLevelCap();
        }

        public void AwardAffixXP(long amount)
        {
            if (Properties[PropertyEnum.ItemAffixLevel] >= GetAffixLevelCap())
                return;

            Properties.AdjustProperty((int)amount, PropertyEnum.ExperiencePoints);
            TryLevelUpAffix(false);
        }

        private long GetAffixLevelUpXPRequirement(int level)
        {
            return GameDatabase.AdvancementGlobalsPrototype.GetItemAffixLevelUpXPRequirement(level);
        }

        public int GetDisplayItemLevel()
        {
            var itemProto = ItemPrototype;
            if (itemProto.EvalDisplayLevel == null) return 0;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.Game = Game;
            evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Default, this);
            return Eval.RunInt(itemProto.EvalDisplayLevel, evalContext);
        }

        protected override void InitializeProcEffectPowers()
        {
            // Don't do anything for items because they are not supposed to do any procs on their own
        }

        private bool TryLevelUpAffix(bool isDeserializing)
        {
            if (Prototype is not LegendaryPrototype)
                return false;

            int affixLevelCap = GetAffixLevelCap();

            int oldAffixLevel = Properties[PropertyEnum.ItemAffixLevel];
            long experiencePoints = Properties[PropertyEnum.ExperiencePoints];
            long experiencePointsNeeded = Properties[PropertyEnum.ExperiencePointsNeeded];

            // Validate loaded experience numbers if we are deserializing
            if (isDeserializing)
            {
                long affixLevelUpXPRequirement = GetAffixLevelUpXPRequirement(oldAffixLevel);
                if (affixLevelUpXPRequirement != experiencePointsNeeded)
                {
                    // Rescale experience for the current cap
                    double ratio = (double)experiencePoints / experiencePointsNeeded;
                    experiencePoints = (long)(affixLevelUpXPRequirement * ratio);
                    experiencePointsNeeded = affixLevelUpXPRequirement;

                    Properties[PropertyEnum.ExperiencePoints] = experiencePoints;
                    Properties[PropertyEnum.ExperiencePointsNeeded] = experiencePointsNeeded;
                }
                else if (oldAffixLevel == affixLevelCap && experiencePoints > 0)
                {
                    // Capped legendaries should not have any experience
                    experiencePoints = 0;
                    Properties[PropertyEnum.ExperiencePoints] = 0;
                }
            }

            // Level up
            int newAffixLevel = oldAffixLevel;
            while (newAffixLevel < affixLevelCap && experiencePoints >= experiencePointsNeeded)
            {
                experiencePoints -= experiencePointsNeeded;
                experiencePointsNeeded = GetAffixLevelUpXPRequirement(++newAffixLevel);

                // Check for infinite loops with bad data
                if (experiencePointsNeeded <= 0)
                {
                    Logger.Warn("TryLevelUpAffix(): experiencePointsNeeded <= 0");
                    break;
                }
            }

            // Remove overcapped experience
            if (newAffixLevel == affixLevelCap)
                experiencePoints = 0;

            // Update properties
            if (newAffixLevel != oldAffixLevel)
            {
                Properties[PropertyEnum.ItemAffixLevel] = newAffixLevel;
                Properties[PropertyEnum.ExperiencePoints] = experiencePoints;
                Properties[PropertyEnum.ExperiencePointsNeeded] = experiencePointsNeeded;

                if (isDeserializing == false)
                    AwardLevelUpAffixes(oldAffixLevel, newAffixLevel);
            }

            if (isDeserializing || oldAffixLevel != newAffixLevel)
            {
                OnAffixLevelUp();
                return true;
            }

            return false;
        }

        private void AwardLevelUpAffixes(int oldAffixLevel, int newAffixLevel)
        {
            foreach (AffixPropertiesCopyEntry copyEntry in _affixProperties)
            {
                if (copyEntry.AffixProto == null)
                {
                    Logger.Warn("AwardLevelUpAffixes(): copyEntry.AffixProto == null");
                    continue;
                }

                if (copyEntry.LevelRequirement > oldAffixLevel && copyEntry.LevelRequirement <= newAffixLevel)
                {
                    // Attach affix properties if we now match the level requirement
                    WorldEntity owner = GetOwnerOfType<WorldEntity>();
                    if (owner != null && IsEquipped)
                    {
                        if (owner.UpdateProcEffectPowers(copyEntry.Properties, true) == false)
                            Logger.Warn($"AwardLevelUpAffixes(): UpdateProcEffectPowers failed for affixLevel=[{copyEntry.LevelRequirement}] affix=[{copyEntry.AffixProto}] item=[{this}] owner=[{owner}]");
                    }

                    Properties.AddChildCollection(copyEntry.Properties);
                }
                else if (copyEntry.LevelRequirement <= oldAffixLevel && copyEntry.LevelRequirement > newAffixLevel)
                {
                    // Detach affix properties if we no longer match the level requirement
                    if (copyEntry.Properties == null)
                    {
                        Logger.Warn("AwardLevelUpAffixes(): copyEntry.Properties == null");
                        continue;
                    }

                    if (copyEntry.Properties.RemoveFromParent(Properties))
                    {
                        WorldEntity owner = GetOwnerOfType<WorldEntity>();
                        if (owner != null && IsEquipped)
                            owner.UpdateProcEffectPowers(copyEntry.Properties, false);
                    }
                }
            }
        }

        private void OnAffixLevelUp()
        {
            RefreshProcPowerIndexProperties();

            // Restart tickers
            InventoryLocation invLoc = InventoryLocation;
            if (invLoc.IsValid)
            {
                InventoryPrototype inventoryProto = invLoc.InventoryPrototype;
                WorldEntity owner = Game.EntityManager.GetEntity<WorldEntity>(invLoc.ContainerId);

                if (owner != null && inventoryProto.IsEquipmentInventory && owner.IsInWorld && owner.IsSimulated)
                {
                    StopTicking(owner);
                    StartTicking(owner);
                }
            }

            if (Prototype is LegendaryPrototype && Properties[PropertyEnum.ItemAffixLevel] == GetAffixLevelCap()) // TODO check GetAffixLevelCap
            { 
                var player = GetOwnerOfType<Player>();
                player?.OnScoringEvent(new(ScoringEventType.FullyUpgradedLegendaries));
            }

            NetMessageLevelUp levelUpMessage = NetMessageLevelUp.CreateBuilder().SetEntityID(Id).Build();
            Game.NetworkManager.SendMessageToInterested(levelUpMessage, this, AOINetworkPolicyValues.AOIChannelOwner | AOINetworkPolicyValues.AOIChannelProximity);
        }

        private bool OnBuiltInPropertyRoll(float randomMult, PropertyPickInRangeEntryPrototype pickInRangeProto)
        {
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(pickInRangeProto.Prop.Enum);            
            PropertyDataType propDataType = propertyInfo.DataType;

            if (propDataType != PropertyDataType.Boolean && propDataType != PropertyDataType.Real && propDataType != PropertyDataType.Integer)
            {
                return Logger.WarnReturn(false, "OnBuiltInPropertyRoll(): The following Item has a built-in pick-in-range PropertyEntry with a property " +
                    $"that is not an int/float/bool prop, which doesn't work!\nItem: [{this}]\nProperty: [{propertyInfo.PropertyName}]");
            }

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Properties);

            float valueMin = 0f;
            if (pickInRangeProto.ValueMin != null)
                valueMin = Eval.RunFloat(pickInRangeProto.ValueMin, evalContext);

            float valueMax = 0f;
            if (pickInRangeProto.ValueMax != null)
                valueMax = Eval.RunFloat(pickInRangeProto.ValueMax, evalContext);

            if (propDataType == PropertyDataType.Real)
            {
                float value = pickInRangeProto.RollAsInteger
                    ? GenerateTruncatedFloatWithinRange(randomMult, valueMin, valueMax)
                    : GenerateFloatWithinRange(randomMult, valueMin, valueMax);

                Properties[pickInRangeProto.Prop] = value;
            }
            else if (propDataType == PropertyDataType.Integer)
            {
                Properties[pickInRangeProto.Prop] = GenerateIntWithinRange(randomMult, valueMin, valueMax);
            }

            // The client doesn't have assignment for bool properties here.
            // Entity/Items/Armor/UniquePrototypes/Avatars/AnyHero/Slot4/Unique189.prototype has a bool range property (CCResistAlwaysAll),
            // but it seems to be a mistake, since it uses Difficulty/Curves/Items/TenacityItemCurve.curve[ItemLevelProp] to calculate its range.

            return true;
        }

        private bool OnBuiltInPropertySet(PropertySetEntryPrototype setProto)
        {
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(setProto.Prop.Enum);
            PropertyDataType propDataType = propertyInfo.DataType;

            if (propDataType != PropertyDataType.Real && propDataType != PropertyDataType.Integer && propDataType != PropertyDataType.Asset)
            {
                return Logger.WarnReturn(false, "OnBuiltInPropertySet(): The following Item has a built-in set PropertyEntry with a property " +
                    $"that is not an int/float/asset prop, which doesn't work!\nItem: [{this}]\nProperty: [{propertyInfo.PropertyName}]");
            }

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Properties);

            switch (propDataType)
            {
                case PropertyDataType.Real:
                    Properties[setProto.Prop] = setProto.Value != null ? Eval.RunFloat(setProto.Value, evalContext) : 0f;
                    break;

                case PropertyDataType.Integer:
                    Properties[setProto.Prop] = setProto.Value != null ? Eval.RunInt(setProto.Value, evalContext) : 0;
                    break;

                case PropertyDataType.Asset:
                    Properties[setProto.Prop] = setProto.Value != null ? Eval.RunAssetId(setProto.Value, evalContext) : AssetId.Invalid;
                    break;
            }

            return true;
        }

        private bool OnAffixAdded(GRandom random, AffixPrototype affixProto, PrototypeId scopeProtoRef, PrototypeId avatarProtoRef, int levelRequirement)
        {
            if (affixProto.Position == AffixPosition.Metadata)
                return true;

            bool affixHasBonusPropertiesToApply = affixProto.HasBonusPropertiesToApply;

            if (affixHasBonusPropertiesToApply == false && affixProto.DataRef != GameDatabase.GlobalsPrototype.ItemNoVisualsAffix)
                return Logger.WarnReturn(false, "OnAffixAdded(): affixHasBonusPropertiesToApply == false && affixProto.DataRef != GameDatabase.GlobalsPrototype.ItemNoVisualsAffix");

            // Initialized affixes are stored in a struct called AffixPropertiesCopyEntry
            AffixPropertiesCopyEntry affixEntry = new();
            affixEntry.AffixProto = affixProto;
            affixEntry.LevelRequirement = levelRequirement;
            affixEntry.Properties = new();

            if (affixProto.Properties != null)
                affixEntry.Properties.FlattenCopyFrom(affixProto.Properties, true);

            var affixPowerModifierProto = affixProto as AffixPowerModifierPrototype;
            if (affixPowerModifierProto != null)
            {
                int evalLevelVar = 0;

                if (affixPowerModifierProto.IsForSinglePowerOnly)
                {
                    // Verbose validation like in the client

                    if (scopeProtoRef.As<PowerPrototype>() == null)
                    {
                        return Logger.WarnReturn(false, string.Format(
                            $"OnAffixAdded(): AffixPowerModifier IsForSinglePowerOnly but scopeProtoRef is not a power! Affix=[{0}] Scope=[{1}] Item=[{2}]",
                            affixProto.ToString(),
                            scopeProtoRef.GetName(),
                            _itemSpec.ItemProtoRef.GetName()));
                    }

                    if (avatarProtoRef == PrototypeId.Invalid && affixProto.IsGemAffix == false)
                    {
                        return Logger.WarnReturn(false, string.Format(
                            $"OnAffixAdded(): Non-gem AffixPowerModifier IsForSinglePowerOnly, but avatarProtoRef is invalid! Affix=[{0}] Scope=[{1}] Item=[{2}]",
                            affixProto.ToString(),
                            scopeProtoRef.GetName(),
                            _itemSpec.ItemProtoRef.GetName()));
                    }

                    AvatarPrototype avatarProto = avatarProtoRef.As<AvatarPrototype>();
                    if (avatarProto == null)
                    {
                        return Logger.WarnReturn(false, string.Format(
                            $"OnAffixAdded(): Unable to get Avatar=[{0}]. Affix=[{1}] Item=[{2}]",
                            avatarProtoRef.GetName(),
                            affixProto.ToString(),
                            _itemSpec.ItemProtoRef.GetName()));
                    }

                    PowerProgressionEntryPrototype powerProgEntry = avatarProto.GetPowerProgressionEntryForPower(scopeProtoRef);
                    if (powerProgEntry == null)
                    {
                        return Logger.WarnReturn(false, string.Format(
                            $"OnAffixAdded(): Unable to get Power[{0}] in Avatar[{1}] Power Progression Table. Affix=[{2}] Item=[{3}]",
                            scopeProtoRef.GetName(),
                            avatarProtoRef.GetName(),
                            affixProto.ToString(),
                            _itemSpec.ItemProtoRef.GetName()));
                    }

                                                            //
                    evalLevelVar = powerProgEntry.Level;    // <------- THIS IS IMPORTANT: we set an actual value here, and not just validating
                                                            //
                }
                else if (affixPowerModifierProto.PowerProgTableTabRef != PrototypeId.Invalid)
                {
                    if (scopeProtoRef.As<AvatarPrototype>() == null)
                    {
                        return Logger.WarnReturn(false, string.Format(
                            $"OnAffixAdded(): AffixPowerModifier is for PowerProgTableTabRef but scopeProtoRef is not an avatar! Affix=[{0}] Scope=[{1}] Item=[{2}]",
                            affixProto.ToString(),
                            scopeProtoRef.GetName(),
                            _itemSpec.ItemProtoRef.GetName()));
                    }
                }
                else if (affixPowerModifierProto.PowerKeywordFilter != PrototypeId.Invalid)
                {
                    if (scopeProtoRef != PrototypeId.Invalid)
                    {
                        return Logger.WarnReturn(false, string.Format(
                            $"OnAffixAdded(): AffixPowerModifier is for PowerKeywordFilter but scopeProtoRef is NOT invalid as expected! Affix=[{0}] Scope=[{1}] Item=[{2}]",
                            affixProto.ToString(),
                            scopeProtoRef.GetName(),
                            _itemSpec.ItemProtoRef.GetName()));
                    }
                }

                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Properties);
                evalContext.SetVar_Int(EvalContext.Var1, (int)Properties[PropertyEnum.ItemLevel]);
                evalContext.SetVar_Int(EvalContext.Var2, evalLevelVar);

                // NOTE: PowerBoost and PowerGrantRank values are rolled in parallel on the client and the server,
                // so the order needs to be exact, or we are going to get a desync.

                int powerBoostMax = Eval.RunInt(affixPowerModifierProto.PowerBoostMax, evalContext);
                if (powerBoostMax > 0)
                {
                    int powerBoostMin = Eval.RunInt(affixPowerModifierProto.PowerBoostMin, evalContext);

                    if (affixPowerModifierProto.PowerProgTableTabRef != PrototypeId.Invalid)
                        affixEntry.PowerModifierPropertyId = new(PropertyEnum.PowerBoost, affixPowerModifierProto.PowerProgTableTabRef, scopeProtoRef);
                    else if (affixPowerModifierProto.PowerKeywordFilter != PrototypeId.Invalid)
                        affixEntry.PowerModifierPropertyId = new(PropertyEnum.PowerBoost, affixPowerModifierProto.PowerKeywordFilter, PrototypeId.Invalid);
                    else
                        affixEntry.PowerModifierPropertyId = new(PropertyEnum.PowerBoost, scopeProtoRef);

                    affixEntry.Properties[affixEntry.PowerModifierPropertyId] = GenerateIntWithinRange(random.NextFloat(), powerBoostMin, powerBoostMax);
                }

                int powerGrantMaxRank = Eval.RunInt(affixPowerModifierProto.PowerGrantRankMax, evalContext);
                if (powerGrantMaxRank > 0)
                {
                    int powerGrantMinRank = Eval.RunInt(affixPowerModifierProto.PowerGrantRankMin, evalContext);

                    if (affixPowerModifierProto.PowerProgTableTabRef != PrototypeId.Invalid)
                        affixEntry.PowerModifierPropertyId = new(PropertyEnum.PowerGrantRank, affixPowerModifierProto.PowerProgTableTabRef, scopeProtoRef);
                    else if (affixPowerModifierProto.PowerKeywordFilter != PrototypeId.Invalid)
                        affixEntry.PowerModifierPropertyId = new(PropertyEnum.PowerGrantRank, affixPowerModifierProto.PowerKeywordFilter, PrototypeId.Invalid);
                    else
                        affixEntry.PowerModifierPropertyId = new(PropertyEnum.PowerGrantRank, scopeProtoRef);

                    affixEntry.Properties[affixEntry.PowerModifierPropertyId] = GenerateIntWithinRange(random.NextFloat(), powerGrantMinRank, powerBoostMax);
                }
            }
            else if (affixProto is AffixRegionModifierPrototype affixRegionModifierProto)
            {
                RegionAffixPrototype regionAffixProto = scopeProtoRef.As<RegionAffixPrototype>();
                if (regionAffixProto == null)
                {
                    return Logger.WarnReturn(false,
                        $"OnAffixAdded(): AffixRegionModifier without a scope ref!\n Affix: {affixProto}\nItem: {_itemSpec.ItemProtoRef.GetName()}");
                }

                if (regionAffixProto.Difficulty != 0)
                    affixEntry.Properties[PropertyEnum.RegionAffixDifficulty] = regionAffixProto.Difficulty;

                affixEntry.Properties[PropertyEnum.RegionAffix, scopeProtoRef] = true;
            }

            affixEntry.Properties[PropertyEnum.ItemLevel] = Properties[PropertyEnum.ItemLevel];

            if (affixProto.PropertyEntries != null)
            {
                foreach (PropertyPickInRangeEntryPrototype propertyEntry in affixProto.PropertyEntries)
                {
                    // NOTE: Property entries are rolled in parallel on the client and the server,
                    // so the order needs to be exact, or we are going to get a desync.
                    float randomMult = random.NextFloat();

                    PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEntry.Prop.Enum);
                    PropertyDataType propDataType = propertyInfo.DataType;

                    if (propDataType != PropertyDataType.Boolean && propDataType != PropertyDataType.Real && propDataType != PropertyDataType.Integer)
                    {
                        Logger.Warn("OnAffixAdded(): The following Affix has a built-in pick-in-range PropertyEntry with a property " +
                            $"that is not an int/float/bool prop, which doesn't work!\nAffix: [{affixProto}]\nProperty: [{propertyInfo.PropertyName}]");
                        continue;
                    }

                    using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                    evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Properties);

                    float valueMin = 0f;
                    if (propertyEntry.ValueMin != null)
                        valueMin = Eval.RunFloat(propertyEntry.ValueMin, evalContext);

                    float valueMax = 0f;
                    if (propertyEntry.ValueMax != null)
                        valueMax = Eval.RunFloat(propertyEntry.ValueMax, evalContext);

                    switch (propDataType)
                    {
                        case PropertyDataType.Boolean:
                            if (valueMin < 0 || valueMax > 1)
                            {
                                Logger.Warn("OnAffixAdded(): The following Affix has a built-in pick-in-range PropertyEntry with a boolean property " +
                                    $"and a range that is not in [0, 1]\nAffix: [{affixProto}]\nProperty: [{propertyInfo.PropertyName}]");
                                continue;
                            }

                            affixEntry.Properties[propertyEntry.Prop] = GenerateIntWithinRange(randomMult, valueMin, valueMax);

                            break;

                        case PropertyDataType.Real:
                            float valueFloat = propertyEntry.RollAsInteger
                                ? GenerateTruncatedFloatWithinRange(randomMult, valueMin, valueMax)
                                : GenerateFloatWithinRange(randomMult, valueMin, valueMax);

                            if (affixPowerModifierProto != null && affixPowerModifierProto.PowerProgTableTabRef != PrototypeId.Invalid)
                            {
                                affixEntry.PowerModifierPropertyId = new(propertyEntry.Prop.Enum, affixPowerModifierProto.PowerProgTableTabRef, scopeProtoRef);
                                affixEntry.Properties[affixEntry.PowerModifierPropertyId] = valueFloat;
                            }
                            else
                            {
                                affixEntry.Properties[propertyEntry.Prop] = valueFloat;
                            }

                            break;

                        case PropertyDataType.Integer:
                            int valueInt = GenerateIntWithinRange(randomMult, valueMin, valueMax);

                            if (affixPowerModifierProto != null && affixPowerModifierProto.PowerProgTableTabRef != PrototypeId.Invalid)
                            {
                                affixEntry.PowerModifierPropertyId = new(propertyEntry.Prop.Enum, affixPowerModifierProto.PowerProgTableTabRef, scopeProtoRef);
                                affixEntry.Properties[affixEntry.PowerModifierPropertyId] = valueInt;
                            }
                            else
                            {
                                affixEntry.Properties[propertyEntry.Prop] = valueInt;
                            }

                            break;
                    }
                }
            }

            if (IsPetItem)
            {
                if (ItemPrototype.IsPetTechAffixUnlocked(this, affixProto.Position))
                {
                    if (Properties.AddChildCollection(affixEntry.Properties) == false)
                        return Logger.WarnReturn(false, "OnAffixAdded(): Properties.AddChildCollection(affixEntry.Properties) == false");
                }
            }
            else if (affixEntry.LevelRequirement <= Properties[PropertyEnum.ItemAffixLevel])
            {
                if (affixProto is not AffixTeamUpPrototype teamUpAffixProto || teamUpAffixProto.IsAppliedToOwnerAvatar == false)
                {
                    if (Properties.AddChildCollection(affixEntry.Properties) == false)
                        return Logger.WarnReturn(false, "OnAffixAdded(): Properties.AddChildCollection(affixEntry.Properties) == false");
                }
            }

            _affixProperties.Add(affixEntry);
            return true;
        }

        private void OnItemEventRoll(int index)
        {
            Properties[PropertyEnum.ItemEventActionIndex] = index;
        }

        private float GenerateTruncatedFloatWithinRange(float randomMult, float min, float max)
        {
            float result = ((max - min + 1f) * randomMult) + min;
            // NOTE: Using regular Math.Clamp() doesn't work here because it throws when min > max.
            result = MathHelper.ClampNoThrow(result, min, max);
            return MathF.Floor(result);
        }

        private float GenerateFloatWithinRange(float randomMult, float min, float max)
        {
            return ((max - min) * randomMult) + min;
        }

        private int GenerateIntWithinRange(float randomMult, float min, float max)
        {
            return (int)GenerateTruncatedFloatWithinRange(randomMult, min, max);
        }

        private bool RunRelicEval()
        {
            if (Prototype is not RelicPrototype relicProto)
                return false;

            if (relicProto.EvalOnStackCountChange == null)
                return false;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, Properties);
            return Eval.RunBool(relicProto.EvalOnStackCountChange, evalContext);
        }

        private void RefreshProcPowerIndexProperties()
        {
            int itemLevel = Properties[PropertyEnum.ItemLevel];
            float itemVariation = Properties[PropertyEnum.ItemVariation];
            int stackCount = CurrentStackSize;

            // Use a temporary property collection to store proc properties
            // because we can't modify our collections while iterating.
            using PropertyCollection procProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            foreach (PropertyEnum procProperty in Property.ProcPropertyTypesAll)
                procProperties.CopyPropertyRange(Properties, procProperty);

            foreach (var kvp in procProperties)
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId procPowerProtoRef);

                Properties[PropertyEnum.ProcPowerItemLevel, procPowerProtoRef] = itemLevel;
                Properties[PropertyEnum.ProcPowerItemVariation, procPowerProtoRef] = itemVariation;
                Properties[PropertyEnum.ProcPowerInvStackCount, procPowerProtoRef] = stackCount;
            }
        }

        private bool AwardPetTechAffix(AffixPosition affixPos)
        {
            foreach (AffixPropertiesCopyEntry copyEntry in _affixProperties)
            {
                AffixPrototype affixProto = copyEntry.AffixProto;
                if (affixProto == null)
                {
                    Logger.Warn("AwardPetTechAffix(): affixProto == null");
                    continue;
                }

                if (affixProto.Position != affixPos)
                    continue;

                WorldEntity owner = GetOwnerOfType<WorldEntity>();
                if (owner != null && IsEquipped)
                {
                    if (owner.UpdateProcEffectPowers(copyEntry.Properties, true) == false)
                        Logger.Warn($"AwardPetTechAffix(): UpdateProcEffectPowers failed in awardPetTechAffix for affixPos=[{affixPos}] affix=[{affixProto}] item=[{this}] owner=[{owner}]");
                }

                if (Properties.AddChildCollection(copyEntry.Properties) == false)
                {
                    int donationCount = Properties[PropertyEnum.PetItemDonationCount, (int)affixPos];
                    bool isUnlocked = ItemPrototype.IsPetTechAffixUnlocked(this, affixPos);
                    bool hasChild = Properties.HasChildCollection(copyEntry.Properties);

                    Logger.Warn($"AwardPetTechAffix(): Failed to AddChildCollection when awarding PetTech affix!\n Affix: {affixProto}\nItems Donated to this Affix: {donationCount}\n Item: [{this}]\nIsPetTechAffixUnlocked: {isUnlocked}\nItem Has Affix Prop Collection: {hasChild}");
                }

                break;
            }

            if (IsPetTechFullyUpgraded())
            {
                Player player = GetOwnerOfType<Player>();
                if (player != null)
                {
                    int count = ScoringEvents.GetPlayerFullyUpgradedPetTechCount(player);
                    player.OnScoringEvent(new(ScoringEventType.FullyUpgradedPetTech, count));
                }
            }

            return true;
        }

        private bool RemovePetTechAffix(AffixPosition affixPos)
        {
            foreach (AffixPropertiesCopyEntry copyEntry in _affixProperties)
            {
                AffixPrototype affixProto = copyEntry.AffixProto;
                if (affixProto == null)
                {
                    Logger.Warn("RemovePetTechAffix(): affixProto == null");
                    continue;
                }

                if (affixProto.Position != affixPos)
                    continue;

                if (copyEntry.Properties == null)
                    continue;

                if (copyEntry.Properties.RemoveFromParent(Properties) == false)
                    return false;

                WorldEntity owner = GetOwnerOfType<WorldEntity>();
                if (owner != null && IsEquipped)
                    owner.UpdateProcEffectPowers(copyEntry.Properties, false);

                return true;
            }

            return false;
        }

        private PrototypeId GetTriggeredPower(ItemEventType eventType, ItemActionType actionType)
        {
            // This has similar overall structure to HasItemActionType()

            ItemPrototype itemProto = ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(PrototypeId.Invalid, "GetTriggeredPower(): itemProto == null");

            if (itemProto.ActionsTriggeredOnItemEvent == null || itemProto.ActionsTriggeredOnItemEvent.Choices.IsNullOrEmpty())
                return PrototypeId.Invalid;

            ItemActionBasePrototype[] choices = itemProto.ActionsTriggeredOnItemEvent.Choices;

            if (itemProto.ActionsTriggeredOnItemEvent.PickMethod == PickMethod.PickWeight)
            {
                // Check just the action that was picked when this item was rolled
                int actionIndex = Properties[PropertyEnum.ItemEventActionIndex];
                if (actionIndex < 0 || actionIndex >= choices.Length)
                    return Logger.WarnReturn(PrototypeId.Invalid, "GetTriggeredPower(): actionIndex < 0 || actionIndex >= choices.Length");

                Prototype choiceProto = choices[actionIndex];
                if (choiceProto == null) return Logger.WarnReturn(PrototypeId.Invalid, "GetTriggeredPower(): choiceProto == null");

                // Action entries can be single actions or action sets

                // First check if the picked action is a set
                if (choiceProto is ItemActionSetPrototype actionSetProto)
                {
                    if (actionSetProto.Choices.IsNullOrEmpty())
                        return PrototypeId.Invalid;

                    return GetTriggeredPowerFromActionSet(actionSetProto.Choices, eventType, actionType);
                }

                // If this is not a set, handle it as a single action
                if (actionType == ItemActionType.AssignPower && choiceProto is ItemActionAssignPowerPrototype assignPowerProto)
                    return assignPowerProto.Power;

                if (actionType == ItemActionType.UsePower && choiceProto is ItemActionUsePowerPrototype usePowerProto)
                    return usePowerProto.Power;
            }
            else if (itemProto.ActionsTriggeredOnItemEvent.PickMethod == PickMethod.PickAll)
            {
                // Check all actions if this item doesn't use random actions
                return GetTriggeredPowerFromActionSet(choices, eventType, actionType);
            }

            return PrototypeId.Invalid;
        }

        private static PrototypeId GetTriggeredPowerFromActionSet(ItemActionBasePrototype[] actions, ItemEventType eventType, ItemActionType actionType)
        {
            foreach (ItemActionBasePrototype actionBaseProto in actions)
            {
                // There should be no nested action sets
                if (actionBaseProto is not ItemActionPrototype actionProto)
                {
                    Logger.Warn("GetTriggeredPowerFromActionSet(): itemActionBaseProto is not ItemActionPrototype itemActionProto");
                    continue;
                }

                if (actionProto.TriggeringEvent != eventType)
                    continue;

                if (actionType == ItemActionType.AssignPower && actionProto is ItemActionAssignPowerPrototype assignPowerProto)
                    return assignPowerProto.Power;

                if (actionType == ItemActionType.UsePower && actionProto is ItemActionUsePowerPrototype usePowerProto)
                    return usePowerProto.Power;
            }

            return PrototypeId.Invalid;
        }

        private bool HasItemActionType(ItemActionType actionType)
        {
            ItemPrototype itemProto = ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(false, "HasItemActionType(): itemProto == null");

            if (itemProto.ActionsTriggeredOnItemEvent == null || itemProto.ActionsTriggeredOnItemEvent.Choices.IsNullOrEmpty())
                return false;

            ItemActionBasePrototype[] choices = itemProto.ActionsTriggeredOnItemEvent.Choices;

            if (itemProto.ActionsTriggeredOnItemEvent.PickMethod == PickMethod.PickWeight)
            {
                // Check just the action that was picked when this item was rolled
                int actionIndex = Properties[PropertyEnum.ItemEventActionIndex];
                if (actionIndex < 0 || actionIndex >= choices.Length)
                    return Logger.WarnReturn(false, "HasItemActionType(): actionIndex < 0 || actionIndex >= choices.Length");

                Prototype choiceProto = choices[actionIndex];
                if (choiceProto == null) return Logger.WarnReturn(false, "HasItemActionType(): choiceProto == null");

                // Action entries can be single actions or action sets

                // First check if the picked action is a set
                if (choiceProto is ItemActionSetPrototype actionSetProto)
                {
                    if (actionSetProto.Choices.IsNullOrEmpty())
                        return false;

                    return HasItemAction(actionSetProto.Choices, actionType);
                }

                // If this is not a set, handle it as a single action
                if (choiceProto is ItemActionPrototype actionProto)
                    return actionProto.ActionType == actionType;

            }
            else if (itemProto.ActionsTriggeredOnItemEvent.PickMethod == PickMethod.PickAll)
            {
                // Check all actions if this item doesn't use random actions
                return HasItemAction(itemProto.ActionsTriggeredOnItemEvent.Choices, actionType);
            }

            return false;
        }

        private static bool HasItemAction(ItemActionBasePrototype[] actions, ItemActionType actionType)
        {
            foreach (ItemActionBasePrototype actionBaseProto in actions)
            {
                if (actionBaseProto is ItemActionPrototype action && action.ActionType == actionType)
                    return true;
            }

            return false;
        }

        private InteractionValidateResult PlayerCanUse(Player player, Avatar avatar, bool checkPower = true, bool checkInventory = true)
        {
            if (player == null) return Logger.WarnReturn(InteractionValidateResult.UnknownFailure, "PlayerCanUse(): player == null");

            int currentStackSize = CurrentStackSize;
            if (currentStackSize < 1)
                return InteractionValidateResult.UnknownFailure;

            if (player.Owns(this) == false)
                return InteractionValidateResult.ItemNotOwned;

            ItemPrototype itemProto = ItemPrototype;
            if (itemProto == null) return Logger.WarnReturn(InteractionValidateResult.UnknownFailure, "PlayerCanUse(): itemProto == null");

            if (itemProto.IsUsable == false)
                return InteractionValidateResult.ItemNotUsable;

            //
            // Inventory validation
            //

            if (checkInventory)
            {
                InventoryLocation invLoc = InventoryLocation;
                InventoryCategory category = invLoc.InventoryCategory;
                InventoryConvenienceLabel convenienceLabel = invLoc.InventoryConvenienceLabel;

                if (category != InventoryCategory.PlayerGeneral &&
                    category != InventoryCategory.PlayerGeneralExtra &&
                    convenienceLabel != InventoryConvenienceLabel.PvP)
                {
                    // Additional validation for non-general inventories
                    if (category == InventoryCategory.PlayerStashGeneral ||
                        category == InventoryCategory.PlayerStashAvatarSpecific)
                    {
                        // Validate that the player is near a STASH
                        WorldEntity dialogTarget = player.GetDialogTarget(true);
                        if (dialogTarget == null || dialogTarget.Properties[PropertyEnum.OpenPlayerStash] == false)
                            return InteractionValidateResult.UnknownFailure;
                    }
                    else if (category == InventoryCategory.AvatarEquipment)
                    {
                        // Do not allow items equipped on library avatars to be used
                        Avatar containerAvatar = Game.EntityManager.GetEntity<Avatar>(invLoc.ContainerId);
                        if (containerAvatar?.IsInWorld != true)
                            return InteractionValidateResult.UnknownFailure;
                    }
                    else if (convenienceLabel == InventoryConvenienceLabel.DeliveryBox)
                    {
                        // Only containers can be used from the delivery box
                        if (itemProto.IsContainer == false)
                            return InteractionValidateResult.UnknownFailure;
                    }
                    else
                    {
                        // Using items from other inventory types is not allowed
                        return InteractionValidateResult.UnknownFailure;
                    }
                }

                if (itemProto.AbilitySettings?.OnlySlottableWhileEquipped == true && IsEquipped == false)
                    return InteractionValidateResult.ItemNotEquipped;
            }
            
            //
            // Level validation
            //

            int characterLevel = avatar.CharacterLevel;
            int characterLevelRequirement = (int)(float)Properties[PropertyEnum.Requirement, PropertyEnum.CharacterLevel];
            
            // Character level requirement for use is always equal at least to the item's level
            if (characterLevelRequirement <= 0)
                characterLevelRequirement = Properties[PropertyEnum.ItemLevel];

            if (characterLevel < characterLevelRequirement)
                return InteractionValidateResult.ItemRequirementsNotMet;

            int prestigeLevel = avatar.PrestigeLevel;
            int prestigeLevelRequirement = (int)(float)Properties[PropertyEnum.Requirement, PropertyEnum.AvatarPrestigeLevel];
            if (prestigeLevel < prestigeLevelRequirement)
                return InteractionValidateResult.ItemRequirementsNotMet;

            //
            // Eval-based validation
            //

            if (itemProto.EvalCanUse != null)
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Default, this);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Entity, avatar);
                evalContext.SetVar_Int(EvalContext.Var1, player.GetLevelCapForCharacter(avatar.PrototypeDataRef));

                if (Eval.RunBool(itemProto.EvalCanUse, evalContext) == false)
                    return InteractionValidateResult.ItemRequirementsNotMet;
            }

            //
            // Subtype-specific validation
            //
            
            switch (itemProto)
            {
                case CharacterTokenPrototype characterTokenProto:
                    return PlayerCanUseCharacterToken(player, avatar, characterTokenProto);

                case InventoryStashTokenPrototype inventoryStashTokenProto:
                    return PlayerCanUseInventoryStashToken(player, inventoryStashTokenProto);

                case EmoteTokenPrototype emoteTokenProto:
                    return PlayerCanUseEmoteToken(player, emoteTokenProto);
            }

            if (IsCraftingRecipe)
                return PlayerCanUseCraftingRecipe(player);

            if (HasItemActionType(ItemActionType.PrestigeMode))
                return PlayerCanUsePrestigeMode(avatar);

            if (HasItemActionType(ItemActionType.AwardTeamUpXP))
                return PlayerCanUseAwardTeamUpXP(player, avatar);

            AvatarPrototype avatarProto = avatar.AvatarPrototype;
            if (avatarProto == null) return Logger.WarnReturn(InteractionValidateResult.UnknownFailure, "PlayerCanUse(): avatarProto == null");

            if (itemProto.IsUsableByAgent(avatarProto) == false)
                return InteractionValidateResult.ItemRequirementsNotMet;

            if (checkPower && HasItemActionType(ItemActionType.UsePower))
                return PlayerCanUsePowerAction(player, avatar);

            return InteractionValidateResult.Success;
        }

        private InteractionValidateResult PlayerCanUseCharacterToken(Player player, Avatar avatar, CharacterTokenPrototype characterTokenProto)
        {
            if (characterTokenProto.IsLiveTuningEnabled() == false)
                return InteractionValidateResult.ItemNotUsable;

            if (characterTokenProto.GrantsCharacterUnlock)
            {
                // If this is an unlock token and the character is locked, this is valid use
                if (characterTokenProto.HasUnlockedCharacter(player) == false)
                    return InteractionValidateResult.Success;

                // If this character is already unlocked and this token cannot be used to upgrade the ultimate, there is nothing to do
                if (characterTokenProto.GrantsUltimateUpgrade == false)
                    return InteractionValidateResult.CharacterAlreadyUnlocked;
            }

            if (characterTokenProto.GrantsUltimateUpgrade)
            {
                // Team-ups do not have ultimate powers, so these checks concern only avatars

                // Cannot upgrade ultimates of locked avatars
                if (player.HasAvatarFullyUnlocked(characterTokenProto.Character) == false)
                    return InteractionValidateResult.CharacterNotYetUnlocked;

                // Cannot upgrade ultimates of library avatars
                if (avatar.PrototypeDataRef != characterTokenProto.Character)
                    return InteractionValidateResult.AvatarUltimateUpgradeCurrentOnly;

                // Skipping AvatarMode check present in the client here because avatar modes never got implemented

                return avatar.CanUpgradeUltimate();
            }

            return InteractionValidateResult.UnknownFailure;
        }

        private InteractionValidateResult PlayerCanUseInventoryStashToken(Player player, InventoryStashTokenPrototype inventoryStashTokenProto)
        {
            PrototypeId invStashProtoRef = inventoryStashTokenProto.Inventory;
            if (invStashProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(InteractionValidateResult.UnknownFailure, "PlayerCanUseInventoryStashToken(): invStashProtoRef == PrototypeId.Invalid");

            if (player.IsInventoryUnlocked(invStashProtoRef))
                return InteractionValidateResult.InventoryAlreadyUnlocked;

            return InteractionValidateResult.Success;
        }

        private InteractionValidateResult PlayerCanUseEmoteToken(Player player, EmoteTokenPrototype emoteTokenProto)
        {
            PrototypeId avatarDataRef = emoteTokenProto.Avatar;
            if (avatarDataRef == PrototypeId.Invalid) return Logger.WarnReturn(InteractionValidateResult.UnknownFailure, "PlayerCanUseEmoteToken(): avatarDataRef == PrototypeId.Invalid");

            PrototypeId emotePowerDataRef = emoteTokenProto.EmotePower;
            if (emotePowerDataRef == PrototypeId.Invalid) return Logger.WarnReturn(InteractionValidateResult.UnknownFailure, "PlayerCanUseEmoteToken(): emotePowerDataRef == PrototypeId.Invalid");

            if (player.HasAvatarEmoteUnlocked(avatarDataRef, emotePowerDataRef))
                return InteractionValidateResult.Error6;

            return InteractionValidateResult.Success;
        }

        private InteractionValidateResult PlayerCanUseCraftingRecipe(Player player)
        {
            InventoryPrototype containingInvProto = InventoryLocation.InventoryPrototype;
            if (containingInvProto == null) return Logger.WarnReturn(InteractionValidateResult.UnknownFailure, "PlayerCanUseCraftingRecipe(): containingInvProto == null");

            if (containingInvProto.IsPlayerGeneralInventory == false && containingInvProto.IsPlayerVendorInventory == false)
                return InteractionValidateResult.ItemNotUsable;

            if (player.HasLearnedCraftingRecipe(PrototypeDataRef))
                return InteractionValidateResult.PlayerAlreadyHasCraftingRecipe;

            return InteractionValidateResult.Success;
        }

        private InteractionValidateResult PlayerCanUsePrestigeMode(Avatar avatar)
        {
            if (avatar.CanActivatePrestigeMode() == false)
                return InteractionValidateResult.ItemNotUsable;

            return InteractionValidateResult.Success;
        }

        private InteractionValidateResult PlayerCanUseAwardTeamUpXP(Player player, Avatar avatar)
        {
            Agent teamUpAgent = avatar.CurrentTeamUpAgent;

            if (teamUpAgent == null || teamUpAgent.IsAtLevelCap)
                return InteractionValidateResult.ItemNotUsable;

            return InteractionValidateResult.Success;
        }

        private InteractionValidateResult PlayerCanUsePowerAction(Player player, Avatar avatar)
        {
            PowerPrototype powerProto = OnUsePower.As<PowerPrototype>();
            if (powerProto == null) return Logger.WarnReturn(InteractionValidateResult.UnknownFailure, "PlayerCanUsePowerAction(): powerProto == null");

            // Run the usual power validation check if it is assigned already
            Power power = avatar.GetPower(powerProto.DataRef);
            if (power != null && power.CanTrigger(PowerActivationSettingsFlags.Item) != PowerUseResult.Success)
                return InteractionValidateResult.CannotTriggerPower;

            return InteractionValidateResult.Success;
        }
    }
}
