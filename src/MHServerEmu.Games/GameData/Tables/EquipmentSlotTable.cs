using System.Diagnostics;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Tables
{
    public class EquipmentSlotTable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<(PrototypeId, PrototypeId), EquipmentInvUISlot> _equipmentSlotDict;

        public EquipmentSlotTable()
        {
            // Caching equipment slot table requires preloading all item prototypes, which is too slow unless running in a public server environment
            var config = ConfigManager.Instance.GetConfig<GameDataConfig>();
            if (config.UseEquipmentSlotTableCache == false)
                return;

            Logger.Info("Building EquipmentInvUISlot cache...");
            Stopwatch stopwatch = Stopwatch.StartNew();

            _equipmentSlotDict = new();
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

            stopwatch.Stop();
            Logger.Info($"Finished building EquipmentInvUISlot cache in {stopwatch.ElapsedMilliseconds} ms");
        }

        public EquipmentInvUISlot EquipmentUISlotForAvatar(ItemPrototype itemProto, AvatarPrototype avatarProto)
        {
            // To do the slow lookup if we don't have a cache
            if (_equipmentSlotDict == null)
                return FindEquipmentUISlotForAvatar(itemProto, avatarProto);

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
