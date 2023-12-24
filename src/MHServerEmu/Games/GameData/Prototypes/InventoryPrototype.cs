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
        public short Capacity { get; private set; }
        public ulong EntityTypeFilter { get; private set; }
        public bool ExitWorldOnAdd { get; private set; }
        public bool ExitWorldOnRemove { get; private set; }
        public bool PersistedToDatabase { get; private set; }
        public bool OnPersonLocation { get; private set; }
        public bool NotifyUI { get; private set; }
        public short CollectionSortOrder { get; private set; }
        public bool VisibleToOwner { get; private set; }
        public bool VisibleToTrader { get; private set; }
        public bool VisibleToParty { get; private set; }
        public bool VisibleToProximity { get; private set; }
        public bool AvatarTeam { get; private set; }
        public ConvenienceLabel ConvenienceLabel { get; private set; }
        public bool PlaySoundOnAdd { get; private set; }
        public bool CapacityUnlimited { get; private set; }
        public bool VendorInvContentsCanBeBought { get; private set; }
        public bool ContentsRecoverFromError { get; private set; }
        public int DestroyContainedAfterSecs { get; private set; }
        public InventoryEvent DestroyContainedOnEvent { get; private set; }
        public InventoryCategory Category { get; private set; }
        public OfferingInventoryUIDataPrototype OfferingInventoryUIData { get; private set; }
        public bool LockedByDefault { get; private set; }
        public bool ReplicateForTransfer { get; private set; }
        public ulong ItemSortPreferences { get; private set; }
        public InventoryUIDataPrototype UIData { get; private set; }
        public InventoryExtraSlotsGroupPrototype SoftCapacitySlotGroupsPC { get; private set; }
        public int SoftCapacityDefaultSlotsPC { get; private set; }
        public InventoryExtraSlotsGroupPrototype SoftCapacitySlotGroupsConsole { get; private set; }
        public int SoftCapacityDefaultSlotsConsole { get; private set; }
        public ulong DisplayName { get; private set; }
    }

    public class InventoryExtraSlotsGroupPrototype : Prototype
    {
        public int MaxExtraSlotCount { get; private set; }
    }

    public class PlayerStashInventoryPrototype : InventoryPrototype
    {
        public ulong ForAvatar { get; private set; }
        public ulong IconPath { get; private set; }
        public ulong FulfillmentName { get; private set; }
        public ulong[] StashTabCustomIcons { get; private set; }
    }

    public class EntityInventoryAssignmentPrototype : Prototype
    {
        public ulong Inventory { get; private set; }
        public ulong LootTable { get; private set; }
    }

    public class AvatarEquipInventoryAssignmentPrototype : EntityInventoryAssignmentPrototype
    {
        public EquipmentInvUISlot UISlot { get; private set; }
        public int UnlocksAtCharacterLevel { get; private set; }
        public ulong UIData { get; private set; }
    }

    public class InventoryExtraSlotsGrantPrototype : Prototype
    {
        public ulong DisplayName { get; private set; }
        public int GrantSlotCount { get; private set; }
        public ulong SlotGroup { get; private set; }
    }
}
