using MHServerEmu.Games.GameData.Calligraphy.Attributes;
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
        public PrototypeId[] ItemSortPreferences { get; protected set; }
        public InventoryUIDataPrototype UIData { get; protected set; }
        public PrototypeId[] SoftCapacitySlotGroupsPC { get; protected set; }       // VectorPrototypeRefPtr InventoryExtraSlotsGroupPrototype
        public int SoftCapacityDefaultSlotsPC { get; protected set; }
        public PrototypeId[] SoftCapacitySlotGroupsConsole { get; protected set; }  // VectorPrototypeRefPtr InventoryExtraSlotsGroupPrototype
        public int SoftCapacityDefaultSlotsConsole { get; protected set; }
        public LocaleStringId DisplayName { get; protected set; }
    }

    public class InventoryExtraSlotsGroupPrototype : Prototype
    {
        public int MaxExtraSlotCount { get; protected set; }
    }

    public class PlayerStashInventoryPrototype : InventoryPrototype
    {
        public PrototypeId ForAvatar { get; protected set; }
        public AssetId IconPath { get; protected set; }
        public LocaleStringId FulfillmentName { get; protected set; }
        public AssetId[] StashTabCustomIcons { get; protected set; }
    }

    public class EntityInventoryAssignmentPrototype : Prototype
    {
        public PrototypeId Inventory { get; protected set; }
        public PrototypeId LootTable { get; protected set; }
    }

    public class AvatarEquipInventoryAssignmentPrototype : EntityInventoryAssignmentPrototype
    {
        public EquipmentInvUISlot UISlot { get; protected set; }
        public int UnlocksAtCharacterLevel { get; protected set; }
        public PrototypeId UIData { get; protected set; }
    }

    public class InventoryExtraSlotsGrantPrototype : Prototype
    {
        public LocaleStringId DisplayName { get; protected set; }
        public int GrantSlotCount { get; protected set; }
        public PrototypeId SlotGroup { get; protected set; }
    }
}
