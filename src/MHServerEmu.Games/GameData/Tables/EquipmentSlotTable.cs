using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Tables
{
    public class EquipmentSlotTable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<(PrototypeId, PrototypeId), EquipmentInvUISlot> _equipmentSlotDict = new();

        public EquipmentSlotTable()
        {
            foreach (var avatarRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<AvatarPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var avatarProto = avatarRef.As<AvatarPrototype>();

                foreach (var itemRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<ItemPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
                {
                    ItemPrototype itemProto = itemRef.As<ItemPrototype>();
                    EquipmentInvUISlot slot = FindEquipmentUISlotForAvatar(itemProto, avatarProto);
                    if (slot != EquipmentInvUISlot.Invalid)
                        _equipmentSlotDict[(itemProto.DataRef, avatarProto.DataRef)] = slot;
                }
            }
        }

        public EquipmentInvUISlot EquipmentUISlotForAvatar(ItemPrototype itemProto, AvatarPrototype avatarProto)
        {
            if (itemProto == null) return Logger.WarnReturn(EquipmentInvUISlot.Invalid, "EquipmentUISlotForAvatar(): itemProto == null");
            if (avatarProto == null) return Logger.WarnReturn(EquipmentInvUISlot.Invalid, "EquipmentUISlotForAvatar(): avatarProto == null");

            if (_equipmentSlotDict.TryGetValue((itemProto.DataRef, avatarProto.DataRef), out EquipmentInvUISlot slot) == false)
                return EquipmentInvUISlot.Invalid;

            return slot;
        }

        private static EquipmentInvUISlot FindEquipmentUISlotForAvatar(ItemPrototype itemProto, AvatarPrototype avatarProto)
        {
            // Named EquipmentSlotTable::equipmentUISlotForAvatar() in the client

            if (itemProto == null) return Logger.WarnReturn(EquipmentInvUISlot.Invalid, "FindEquipmentUISlotForAvatar(): itemProto == null");
            if (avatarProto == null) return Logger.WarnReturn(EquipmentInvUISlot.Invalid, "FindEquipmentUISlotForAvatar(): avatarProto == null");

            foreach (AvatarEquipInventoryAssignmentPrototype assignmentProto in avatarProto.EquipmentInventories)
            {
                InventoryPrototype invProto = assignmentProto.Inventory.As<InventoryPrototype>();
                if (invProto.AllowEntity(itemProto))
                    return assignmentProto.UISlot;
            }

            return EquipmentInvUISlot.Invalid;
        }
    }
}
