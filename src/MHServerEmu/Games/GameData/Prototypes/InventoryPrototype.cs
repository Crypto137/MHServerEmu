using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
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

    [AssetEnum]
    public enum InventoryEvent
    {
        Invalid,
        RegionChange,
    }

    #endregion

    public class InventoryPrototype : Prototype
    {
        public short Capacity { get; set; }
        public ulong EntityTypeFilter { get; set; }
        public bool ExitWorldOnAdd { get; set; }
        public bool ExitWorldOnRemove { get; set; }
        public bool PersistedToDatabase { get; set; }
        public bool OnPersonLocation { get; set; }
        public bool NotifyUI { get; set; }
        public short CollectionSortOrder { get; set; }
        public bool VisibleToOwner { get; set; }
        public bool VisibleToTrader { get; set; }
        public bool VisibleToParty { get; set; }
        public bool VisibleToProximity { get; set; }
        public bool AvatarTeam { get; set; }
        public ConvenienceLabel ConvenienceLabel { get; set; }
        public bool PlaySoundOnAdd { get; set; }
        public bool CapacityUnlimited { get; set; }
        public bool VendorInvContentsCanBeBought { get; set; }
        public bool ContentsRecoverFromError { get; set; }
        public int DestroyContainedAfterSecs { get; set; }
        public InventoryEvent DestroyContainedOnEvent { get; set; }
        public InventoryCategory Category { get; set; }
        public OfferingInventoryUIDataPrototype OfferingInventoryUIData { get; set; }
        public bool LockedByDefault { get; set; }
        public bool ReplicateForTransfer { get; set; }
        public ulong ItemSortPreferences { get; set; }
        public InventoryUIDataPrototype UIData { get; set; }
        public InventoryExtraSlotsGroupPrototype SoftCapacitySlotGroupsPC { get; set; }
        public int SoftCapacityDefaultSlotsPC { get; set; }
        public InventoryExtraSlotsGroupPrototype SoftCapacitySlotGroupsConsole { get; set; }
        public int SoftCapacityDefaultSlotsConsole { get; set; }
        public ulong DisplayName { get; set; }
    }

    public class InventoryExtraSlotsGroupPrototype : Prototype
    {
        public int MaxExtraSlotCount { get; set; }
    }

    public class PlayerStashInventoryPrototype : InventoryPrototype
    {
        public ulong ForAvatar { get; set; }
        public ulong IconPath { get; set; }
        public ulong FulfillmentName { get; set; }
        public ulong[] StashTabCustomIcons { get; set; }
    }

    public class EntityInventoryAssignmentPrototype : Prototype
    {
        public ulong Inventory { get; set; }
        public ulong LootTable { get; set; }
    }

    public class AvatarEquipInventoryAssignmentPrototype : EntityInventoryAssignmentPrototype
    {
        public EquipmentInvUISlot UISlot { get; set; }
        public int UnlocksAtCharacterLevel { get; set; }
        public ulong UIData { get; set; }
    }

    public class InventoryExtraSlotsGrantPrototype : Prototype
    {
        public ulong DisplayName { get; set; }
        public int GrantSlotCount { get; set; }
        public ulong SlotGroup { get; set; }
    }
}
