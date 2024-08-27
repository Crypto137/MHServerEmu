using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities.Persistence
{
    public static class PersistenceHelper
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static void StoreInventoryEntities(DBAccount dbAccount, Player player)
        {
            dbAccount.ItemDict.Clear();

            foreach (Inventory inventory in new InventoryIterator(player, InventoryIterationFlags.PlayerGeneral))
                ArchiveInventory(dbAccount, inventory);

            // TODO: Avatars, team-ups, stash tabs, avatar equipment, team-up equipment, controlled entities
        }

        public static void RestoreInventoryEntities(DBAccount dbAccount, Player player)
        {
            foreach (Inventory inventory in new InventoryIterator(player, InventoryIterationFlags.PlayerGeneral))
                RestoreInventory(dbAccount, inventory);
        }

        private static bool ArchiveInventory(DBAccount dbAccount, Inventory inventory)
        {
            if (inventory == null) return Logger.WarnReturn(false, "ArchiveInventory(): inventory == null");

            // Common data everything stored in this inventory
            long containerDbGuid = (long)inventory.Owner.DatabaseUniqueId;
            long inventoryProtoGuid = (long)GameDatabase.GetPrototypeGuid(inventory.PrototypeDataRef);

            foreach (var entry in inventory)
            {
                Entity entity = inventory.Game.EntityManager.GetEntity<Entity>(entry.Id);
                
                if (entity == null)
                {
                    Logger.Warn("ArchiveInventory(): entity == null");
                    continue;
                }

                DBEntity dbEntity = new();
                dbEntity.DbGuid = (long)entity.DatabaseUniqueId;
                dbEntity.ContainerDbGuid = containerDbGuid;
                dbEntity.InventoryProtoGuid = inventoryProtoGuid;
                dbEntity.Slot = entry.Slot;
                dbEntity.EntityProtoGuid = (long)GameDatabase.GetPrototypeGuid(entity.PrototypeDataRef);

                using (Archive archive = new(ArchiveSerializeType.Database))
                {
                    if (Serializer.Transfer(archive, ref entity) == false)
                    {
                        Logger.Error($"ArchiveInventory(): Failed to serialize entity {entity}");
                        continue;
                    }

                    dbEntity.ArchiveData = archive.AccessAutoBuffer().ToArray();
                }

                dbAccount.ItemDict.Add(dbEntity.DbGuid, dbEntity);
            }

            return true;
        }

        private static bool RestoreInventory(DBAccount dbAccount, Inventory inventory)
        {
            if (inventory.Count > 0) return Logger.WarnReturn(false, "RestoreInventory(): Inventory must be empty to be restored!");

            long ownerDbGuid = (long)inventory.Owner.DatabaseUniqueId;
            ulong containerEntityId = inventory.Owner.Id;

            foreach (DBEntity dbEntity in dbAccount.ItemDict.Values)
            {
                if (dbEntity.ContainerDbGuid != ownerDbGuid)
                {
                    Logger.Warn($"RestoreInventory(): Attempting to restore entity belonging to 0x{ownerDbGuid:X}, but the inventory owner is 0x{dbEntity.ContainerDbGuid:X}");
                    continue;
                }

                PrototypeId inventoryProtoRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)dbEntity.InventoryProtoGuid);
                if (inventoryProtoRef != inventory.PrototypeDataRef)
                {
                    Logger.Warn($"RestoreInventory(): Inventory prototype mismatch");
                    continue;
                }

                PrototypeId entityProtoRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)dbEntity.EntityProtoGuid);
                if (entityProtoRef == PrototypeId.Invalid)
                {
                    Logger.Warn($"RestoreInventory(): Failed to retrieve entity proto ref for guid {dbEntity.EntityProtoGuid}");
                }

                EntitySettings settings = new();
                settings.DbGuid = (ulong)dbEntity.DbGuid;
                settings.InventoryLocation = new(containerEntityId, inventoryProtoRef, dbEntity.Slot);
                settings.EntityRef = entityProtoRef;
                settings.ArchiveSerializeType = ArchiveSerializeType.Database;
                settings.ArchiveData = dbEntity.ArchiveData;

                inventory.Game.EntityManager.CreateEntity(settings);
            }

            return true;
        }
    }
}
