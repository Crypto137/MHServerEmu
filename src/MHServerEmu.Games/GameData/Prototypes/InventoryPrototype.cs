using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class InventoryPrototype : Prototype
    {
        public short Capacity { get; protected set; }
        public PrototypeId[] EntityTypeFilter { get; protected set; }
        public bool ExitWorldOnAdd { get; protected set; }
        public bool ExitWorldOnRemove { get; protected set; }
        public bool PersistedToDatabase { get; protected set; }
        public bool OnPersonLocation { get; protected set; }
        public bool NotifyUI { get; protected set; }
        public short CollectionSortOrder { get; protected set; }
        public bool VisibleToOwner { get; protected set; }
        public bool VisibleToTrader { get; protected set; }
        public bool VisibleToParty { get; protected set; }
        public bool VisibleToProximity { get; protected set; }
        public bool AvatarTeam { get; protected set; }
        public InventoryConvenienceLabel ConvenienceLabel { get; protected set; }
        public bool PlaySoundOnAdd { get; protected set; }
        public bool CapacityUnlimited { get; protected set; }
        public bool VendorInvContentsCanBeBought { get; protected set; }
        public bool ContentsRecoverFromError { get; protected set; }
        public int DestroyContainedAfterSecs { get; protected set; }
        public InventoryEvent DestroyContainedOnEvent { get; protected set; }
        public InventoryCategory Category { get; protected set; }
        public OfferingInventoryUIDataPrototype OfferingInventoryUIData { get; protected set; }
        public bool LockedByDefault { get; protected set; }
        public bool ReplicateForTransfer { get; protected set; }
        public PrototypeId[] ItemSortPreferences { get; protected set; }
        public InventoryUIDataPrototype UIData { get; protected set; }
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        [PrototypeField(PrototypeFieldType.VectorPrototypeRefPtr)]
        public InventoryExtraSlotsGroupPrototype[] SoftCapacitySlotGroupsPC { get; protected set; }
        public int SoftCapacityDefaultSlotsPC { get; protected set; }
        [PrototypeField(PrototypeFieldType.VectorPrototypeRefPtr)]
        public InventoryExtraSlotsGroupPrototype[] SoftCapacitySlotGroupsConsole { get; protected set; }
        public int SoftCapacityDefaultSlotsConsole { get; protected set; }
        public LocaleStringId DisplayName { get; protected set; }
#endif

        //---

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="InventoryPrototype"/> is for a player stash inventory.
        /// </summary>
        [DoNotCopy]
        public bool IsPlayerStashInventory { get => Category == InventoryCategory.PlayerStashAvatarSpecific || Category == InventoryCategory.PlayerStashGeneral; }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="InventoryPrototype"/> is for avatar or team-up equipment.
        /// </summary>
        [DoNotCopy]
        public bool IsEquipmentInventory { get => Category == InventoryCategory.AvatarEquipment || Category == InventoryCategory.TeamUpEquipment; }

        [DoNotCopy]
        public bool IsPlayerGeneralInventory { get => Category == InventoryCategory.PlayerGeneral; }

        [DoNotCopy]
        public bool IsPlayerGeneralExtraInventory { get => Category == InventoryCategory.PlayerGeneralExtra; }

        [DoNotCopy]
        public bool IsPlayerCraftingRecipeInventory { get => Category == InventoryCategory.PlayerCraftingRecipes; }

        [DoNotCopy]
        public bool IsPlayerVendorInventory { get => Category == InventoryCategory.PlayerVendor; }

        [DoNotCopy]
        public bool IsPlayerVendorBuybackInventory { get => ConvenienceLabel == InventoryConvenienceLabel.VendorBuyback; }

        [DoNotCopy]
        public bool IsArtifactInventory { get => ConvenienceLabel >= InventoryConvenienceLabel.AvatarArtifact1 && ConvenienceLabel <= InventoryConvenienceLabel.AvatarArtifact4; }

        [DoNotCopy]
        public bool IsVisible { get => VisibleToOwner || VisibleToTrader || VisibleToParty || VisibleToProximity; }

        /// <summary>
        /// Returns <see langword="true"/> if entities that use the provided <see cref="EntityPrototype"/> are allowed to be stored in inventories that use this <see cref="InventoryPrototype"/>.
        /// </summary>
        public bool AllowEntity(EntityPrototype entityPrototype)
        {
            if (EntityTypeFilter.HasValue())
            {
                DataDirectory dataDirectory = GameDatabase.DataDirectory;
                foreach (PrototypeId entityTypeRef in EntityTypeFilter)
                {
                    BlueprintId entityTypeBlueprintRef = dataDirectory.GetPrototypeBlueprintDataRef(entityTypeRef);
                    if (dataDirectory.PrototypeIsChildOfBlueprint(entityPrototype.DataRef, entityTypeBlueprintRef))
                        return true;
                }
            }

            return false;
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public int GetSoftCapacityDefaultSlots()
        {
            // TODO: consoles
            return SoftCapacityDefaultSlotsPC;
        }

        public InventoryExtraSlotsGroupPrototype[] GetSoftCapacitySlotGroups()
        {
            // TODO: consoles
            return SoftCapacitySlotGroupsPC;
        }
#endif

        public bool InventoryRequiresFlaggedVisibility()
        {
            return IsEquipmentInventory || IsPlayerStashInventory || IsPlayerCraftingRecipeInventory || IsPlayerVendorInventory || IsPlayerVendorBuybackInventory;
        }
    }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
    public class InventoryExtraSlotsGroupPrototype : Prototype
    {
        public int MaxExtraSlotCount { get; protected set; }
    }
#endif

    public class PlayerStashInventoryPrototype : InventoryPrototype
    {
        public PrototypeId ForAvatar { get; protected set; }
        public AssetId IconPath { get; protected set; }
#if GAME_VERSION_1_48
        public LocaleStringId DisplayName { get; protected set; }
#endif
        public LocaleStringId FulfillmentName { get; protected set; }
        public AssetId[] StashTabCustomIcons { get; protected set; }
    }

    public class EntityInventoryAssignmentPrototype : Prototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public InventoryPrototype Inventory { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public LootTablePrototype LootTable { get; protected set; }
    }

    public class AvatarEquipInventoryAssignmentPrototype : EntityInventoryAssignmentPrototype
    {
        public EquipmentInvUISlot UISlot { get; protected set; }
        public int UnlocksAtCharacterLevel { get; protected set; }
        public PrototypeId UIData { get; protected set; }
    }

#if GAME_VERSION_1_53
    public class InventoryExtraSlotsGrantPrototype : ItemPrototype
    {
        public new LocaleStringId DisplayName { get; protected set; }
        public int GrantSlotCount { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public InventoryExtraSlotsGroupPrototype SlotGroup { get; protected set; }
    }
#elif GAME_VERSION_1_52
    public class InventoryExtraSlotsGrantPrototype : Prototype
    {
        public LocaleStringId DisplayName { get; protected set; }
        public int GrantSlotCount { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public InventoryExtraSlotsGroupPrototype SlotGroup { get; protected set; }
    }
#endif
}
