using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class InventoryPrototype : Prototype
    {
        public bool AvatarTeam;
        public short Capacity;
        public bool CapacityUnlimited;
        public int Category;
        public short CollectionSortOrder;
        public bool ContentsRecoverFromError;
        public int ConvenienceLabel;
        public int DestroyContainedAfterSecs;
        public int DestroyContainedOnEvent;
        public ulong DisplayName;
        public bool ExitWorldOnAdd;
        public bool ExitWorldOnRemove;
        public ulong ItemSortPreferences;
        public bool LockedByDefault;
        public bool NotifyUI;
        public ulong UIData;
        public ulong OfferingInventoryUIData;
        public bool OnPersonLocation;
        public bool PersistedToDatabase;
        public bool PlaySoundOnAdd;
        public bool ReplicateForTransfer;
        public int SoftCapacityDefaultSlotsPC;
        public ulong SoftCapacitySlotGroupsPC;
        public int SoftCapacityDefaultSlotsConsole;
        public ulong SoftCapacitySlotGroupsConsole;
        public bool VendorInvContentsCanBeBought;
        public bool VisibleToOwner;
        public bool VisibleToParty;
        public bool VisibleToProximity;
        public bool VisibleToTrader;
        public ulong EntityTypeFilter;

        public InventoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InventoryPrototype), proto); }
    }

    public class PlayerStashInventoryPrototype : InventoryPrototype
    {
        public ulong ForAvatar;
        public ulong IconPath;
        public ulong FulfillmentName;
        public ulong StashTabCustomIcons;
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
        public ulong UIData;
        public EquipmentInvUISlot UISlot;
        public int UnlocksAtCharacterLevel;
        public AvatarEquipInventoryAssignmentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AvatarEquipInventoryAssignmentPrototype), proto); }

    }
}
