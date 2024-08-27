using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities.Persistence
{
    public static class PersistenceHelper
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static void StoreInventoryEntities(Player player, DBAccount dbAccount)
        {
            dbAccount.ClearEntities();

            StoreContainer(player, dbAccount);

            foreach (Avatar avatar in new AvatarIterator(player))
            {
                StoreContainer(avatar, dbAccount);
            }

            foreach (var entry in player.GetInventory(InventoryConvenienceLabel.TeamUpLibrary))
            {
                Agent teamUp = player.Game.EntityManager.GetEntity<Agent>(entry.Id);
                if (teamUp == null)
                {
                    Logger.Warn("StoreInventoryEntities(): teamUp == null");
                    continue;
                }

                StoreContainer(teamUp, dbAccount);
            }
        }

        public static void RestoreInventoryEntities(Player player, DBAccount dbAccount)
        {
            RestoreContainer(player, dbAccount.Avatars);
            RestoreContainer(player, dbAccount.Items);

            foreach (Avatar avatar in new AvatarIterator(player))
            {
                RestoreContainer(avatar, dbAccount.Items);
                RestoreContainer(avatar, dbAccount.ControlledEntities);
            }

            foreach (var entry in player.GetInventory(InventoryConvenienceLabel.TeamUpLibrary))
            {
                Agent teamUp = player.Game.EntityManager.GetEntity<Agent>(entry.Id);
                if (teamUp == null)
                {
                    Logger.Warn("RestoreInventoryEntities(): teamUp == null");
                    continue;
                }

                RestoreContainer(teamUp, dbAccount.Items);
            }
        }

        private static bool StoreContainer(Entity container, DBAccount dbAccount)
        {
            foreach (Inventory inventory in new InventoryIterator(container))
            {
                if (inventory.Prototype.PersistedToDatabase == false)
                    continue;

                StoreInventory(inventory, dbAccount);
            }

            return true;
        }

        private static bool StoreInventory(Inventory inventory, DBAccount dbAccount)
        {
            if (inventory == null) return Logger.WarnReturn(false, "StoreInventory(): inventory == null");

            DBEntityCollection entities;

            if (inventory.Category == InventoryCategory.PlayerAvatars)
            {
                entities = dbAccount.Avatars;
            }
            else if (inventory.ConvenienceLabel == InventoryConvenienceLabel.TeamUpLibrary)
            {
                entities = dbAccount.TeamUps;
            }
            else if (inventory.ConvenienceLabel == InventoryConvenienceLabel.Controlled)
            {
                entities = dbAccount.ControlledEntities;
            }
            else
            {
                entities = dbAccount.Items;
            }

            // Common data everything stored in this inventory
            long containerDbGuid = (long)inventory.Owner.DatabaseUniqueId;
            long inventoryProtoGuid = (long)GameDatabase.GetPrototypeGuid(inventory.PrototypeDataRef);

            foreach (var entry in inventory)
            {
                Entity entity = inventory.Game.EntityManager.GetEntity<Entity>(entry.Id);
                
                if (entity == null)
                {
                    Logger.Warn("StoreInventory(): entity == null");
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
                        Logger.Error($"StoreInventory(): Failed to serialize entity {entity}");
                        continue;
                    }

                    dbEntity.ArchiveData = archive.AccessAutoBuffer().ToArray();
                }

                entities.Add(dbEntity);

                Logger.Debug($"StoreInventory(): Archived entity {entity}");
            }

            return true;
        }

        private static bool RestoreContainer(Entity container, DBEntityCollection entities)
        {
            long containerDbGuid = (long)container.DatabaseUniqueId;
            ulong containerEntityId = container.Id;

            foreach (DBEntity dbEntity in entities.GetEntriesForContainer(containerDbGuid))
            {
                if (dbEntity.ContainerDbGuid != containerDbGuid)
                {
                    Logger.Warn($"RestoreContainer(): Attempting to restore entity belonging to 0x{dbEntity.ContainerDbGuid:X} in 0x{containerDbGuid:X}");
                    continue;
                }

                PrototypeId inventoryProtoRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)dbEntity.InventoryProtoGuid);
                if (inventoryProtoRef == PrototypeId.Invalid)
                {
                    Logger.Warn($"RestoreContainer(): Failed to retrieve inventory proto ref for guid {dbEntity.InventoryProtoGuid}");
                    continue;
                }

                if (container.GetInventoryByRef(inventoryProtoRef) == null)
                {
                    Logger.Warn($"RestoreContainer(): Container {container} does not have inventory {inventoryProtoRef.GetName()}");
                    continue;
                }

                PrototypeId entityProtoRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)dbEntity.EntityProtoGuid);
                if (entityProtoRef == PrototypeId.Invalid)
                {
                    Logger.Warn($"RestoreContainer(): Failed to retrieve entity proto ref for guid {dbEntity.EntityProtoGuid}");
                    continue;
                }

                EntitySettings settings = new();
                settings.DbGuid = (ulong)dbEntity.DbGuid;
                settings.InventoryLocation = new(containerEntityId, inventoryProtoRef, dbEntity.Slot);
                settings.EntityRef = entityProtoRef;
                settings.ArchiveSerializeType = ArchiveSerializeType.Database;
                settings.ArchiveData = dbEntity.ArchiveData;

                container.Game.EntityManager.CreateEntity(settings);
            }

            return true;
        }
    }
}
