using System.Diagnostics;
using System.Runtime.CompilerServices;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Tables
{
    public class EquipmentSlotTable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<(PrototypeId, PrototypeId), EquipmentInvUISlot> _equipmentSlotLookup;

        public EquipmentSlotTable()
        {
            // Caching equipment slot table requires preloading all item prototypes, which is too slow unless running in a public server environment
            var config = ConfigManager.Instance.GetConfig<GameDataConfig>();
            if (config.UseEquipmentSlotTableCache == false)
                return;

            Logger.Info("Building EquipmentInvUISlot cache...");
            Stopwatch stopwatch = Stopwatch.StartNew();

            _equipmentSlotLookup = new();
            foreach (PrototypeId avatarRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<AvatarPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                AvatarPrototype avatarProto = avatarRef.As<AvatarPrototype>();
                if (!Verify.IsNotNull(avatarProto))
                    continue;

                foreach (PrototypeId itemRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<ItemPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
                {
                    ItemPrototype itemProto = itemRef.As<ItemPrototype>();
                    if (!Verify.IsNotNull(itemProto))
                        continue;

                    EquipmentInvUISlot slot = FindEquipmentUISlotForAvatar(itemProto, avatarProto);
                    if (slot != EquipmentInvUISlot.Invalid)
                        _equipmentSlotLookup.Add((itemProto.DataRef, avatarProto.DataRef), slot);
                }
            }

            stopwatch.Stop();
            Logger.Info($"Finished building EquipmentInvUISlot cache in {stopwatch.ElapsedMilliseconds} ms");
        }

        public EquipmentInvUISlot EquipmentUISlotForAvatar(ItemPrototype itemProto, AvatarPrototype avatarProto)
        {
            // Do the slow lookup if we don't have a cache
            if (_equipmentSlotLookup == null)
                return FindEquipmentUISlotForAvatar(itemProto, avatarProto);

            if (!Verify.IsNotNull(itemProto)) return EquipmentInvUISlot.Invalid;
            if (!Verify.IsNotNull(avatarProto)) return EquipmentInvUISlot.Invalid;

            if (_equipmentSlotLookup.TryGetValue((itemProto.DataRef, avatarProto.DataRef), out EquipmentInvUISlot slot) == false)
                return EquipmentInvUISlot.Invalid;

            return slot;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static EquipmentInvUISlot FindEquipmentUISlotForAvatar(ItemPrototype itemProto, AvatarPrototype avatarProto)
        {
            // Named EquipmentSlotTable::equipmentUISlotForAvatar() in the client

            if (!Verify.IsNotNull(itemProto)) return EquipmentInvUISlot.Invalid;
            if (!Verify.IsNotNull(avatarProto)) return EquipmentInvUISlot.Invalid;

            foreach (AvatarEquipInventoryAssignmentPrototype assignmentProto in avatarProto.EquipmentInventories)
            {
                InventoryPrototype invProto = assignmentProto.Inventory;
                if (!Verify.IsNotNull(invProto))
                    continue;

                if (invProto.AllowEntity(itemProto))
                    return assignmentProto.UISlot;
            }

            return EquipmentInvUISlot.Invalid;
        }
    }
}
