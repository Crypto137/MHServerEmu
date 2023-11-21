using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class InventoryPrototype : Prototype
    {
        public short Capacity;
        public ulong EntityTypeFilter;
        public bool ExitWorldOnAdd;
        public bool ExitWorldOnRemove;
        public bool PersistedToDatabase;
        public bool OnPersonLocation;
        public bool NotifyUI;
        public short CollectionSortOrder;
        public bool VisibleToOwner;
        public bool VisibleToTrader;
        public bool VisibleToParty;
        public bool VisibleToProximity;
        public bool AvatarTeam;
        public ConvenienceLabel ConvenienceLabel;
        public bool PlaySoundOnAdd;
        public bool CapacityUnlimited;
        public bool VendorInvContentsCanBeBought;
        public bool ContentsRecoverFromError;
        public int DestroyContainedAfterSecs;
        public InventoryEvent DestroyContainedOnEvent;
        public Category Category;
        public OfferingInventoryUIDataPrototype OfferingInventoryUIData;
        public bool LockedByDefault;
        public bool ReplicateForTransfer;
        public ulong ItemSortPreferences;
        public InventoryUIDataPrototype UIData;
        public InventoryExtraSlotsGroupPrototype SoftCapacitySlotGroupsPC;
        public int SoftCapacityDefaultSlotsPC;
        public InventoryExtraSlotsGroupPrototype SoftCapacitySlotGroupsConsole;
        public int SoftCapacityDefaultSlotsConsole;
        public ulong DisplayName;
        public InventoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InventoryPrototype), proto); }
    }

    public enum Category {
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

    public enum InventoryEvent {
	    Invalid,
	    RegionChange,
    }
    public class InventoryExtraSlotsGroupPrototype : Prototype
    {
        public int MaxExtraSlotCount;
        public InventoryExtraSlotsGroupPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InventoryExtraSlotsGroupPrototype), proto); }
    }

    public class PlayerStashInventoryPrototype : InventoryPrototype
    {
        public ulong ForAvatar;
        public ulong IconPath;
        public ulong FulfillmentName;
        public ulong[] StashTabCustomIcons;
        public PlayerStashInventoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PlayerStashInventoryPrototype), proto); }
    }

    public class EntityInventoryAssignmentPrototype : Prototype
    {
        public InventoryPrototype Inventory;
        public LootTablePrototype LootTable;
        public EntityInventoryAssignmentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityInventoryAssignmentPrototype), proto); }

    }

    public class AvatarEquipInventoryAssignmentPrototype : EntityInventoryAssignmentPrototype
    {
        public EquipmentInvUISlot UISlot;
        public int UnlocksAtCharacterLevel;
        public ulong UIData;
        public AvatarEquipInventoryAssignmentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AvatarEquipInventoryAssignmentPrototype), proto); }
    }

    public class InventoryExtraSlotsGrantPrototype : Prototype
    {
        public ulong DisplayName;
        public int GrantSlotCount;
        public ulong SlotGroup;
        public InventoryExtraSlotsGrantPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InventoryExtraSlotsGrantPrototype), proto); }
    }

}
