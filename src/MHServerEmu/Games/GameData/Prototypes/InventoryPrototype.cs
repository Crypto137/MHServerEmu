using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)None)]
    public enum InventoryCategory   // Entity/Inventory/Category.type
    {
        None = 0,
        AvatarEquipment = 1,
        BagItem = 2,
        PlayerAdmin = 3,
        PlayerAvatars = 4,
        PlayerCraftingRecipes = 12,
        PlayerGeneral = 5,
        PlayerGeneralExtra = 6,
        PlayerStashAvatarSpecific = 7,
        PlayerStashGeneral = 8,
        PlayerTrade = 10,
        PlayerVendor = 11,
        TeamUpEquipment = 13,
        PlayerStashTeamUpGear = 9,
    }

    [AssetEnum((int)Invalid)]
    public enum InventoryEvent
    {
        Invalid,
        RegionChange,
    }

    #endregion

    public class InventoryPrototype : Prototype
    {
        public short Capacity { get; protected set; }
        public ulong[] EntityTypeFilter { get; protected set; }
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
        public ConvenienceLabel ConvenienceLabel { get; protected set; }
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
        public ulong[] ItemSortPreferences { get; protected set; }
        public InventoryUIDataPrototype UIData { get; protected set; }
        public ulong[] SoftCapacitySlotGroupsPC { get; protected set; }       // VectorPrototypeRefPtr InventoryExtraSlotsGroupPrototype
        public int SoftCapacityDefaultSlotsPC { get; protected set; }
        public ulong[] SoftCapacitySlotGroupsConsole { get; protected set; }  // VectorPrototypeRefPtr InventoryExtraSlotsGroupPrototype
        public int SoftCapacityDefaultSlotsConsole { get; protected set; }
        public ulong DisplayName { get; protected set; }
    }

    public class InventoryExtraSlotsGroupPrototype : Prototype
    {
        public int MaxExtraSlotCount { get; protected set; }
    }

    public class PlayerStashInventoryPrototype : InventoryPrototype
    {
        public ulong ForAvatar { get; protected set; }
        public ulong IconPath { get; protected set; }
        public ulong FulfillmentName { get; protected set; }
        public ulong[] StashTabCustomIcons { get; protected set; }
    }

    public class EntityInventoryAssignmentPrototype : Prototype
    {
        public ulong Inventory { get; protected set; }
        public ulong LootTable { get; protected set; }
    }

    public class AvatarEquipInventoryAssignmentPrototype : EntityInventoryAssignmentPrototype
    {
        public EquipmentInvUISlot UISlot { get; protected set; }
        public int UnlocksAtCharacterLevel { get; protected set; }
        public ulong UIData { get; protected set; }
    }

    public class InventoryExtraSlotsGrantPrototype : Prototype
    {
        public ulong DisplayName { get; protected set; }
        public int GrantSlotCount { get; protected set; }
        public ulong SlotGroup { get; protected set; }
    }
}
