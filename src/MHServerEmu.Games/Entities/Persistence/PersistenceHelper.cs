using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
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

            StoreContainer(player, dbAccount, true);

            foreach (Avatar avatar in new AvatarIterator(player))
            {
                StoreContainer(avatar, dbAccount, true);
            }

            EntityManager entityManager = player.Game.EntityManager;

            foreach (var entry in player.GetInventory(InventoryConvenienceLabel.TeamUpLibrary))
            {
                Agent teamUp = entityManager.GetEntity<Agent>(entry.Id);
                if (teamUp == null)
                {
                    Logger.Warn("StoreInventoryEntities(): teamUp == null");
                    continue;
                }

                // Team-ups shouldn't have transferrable summons, but disabling it explicitly just in case.
                StoreContainer(teamUp, dbAccount, false);
            }
        }

        public static void RestoreInventoryEntities(Player player, DBAccount dbAccount)
        {
            RestoreContainer(player, dbAccount.Avatars);
            RestoreContainer(player, dbAccount.TeamUps);
            RestoreContainer(player, dbAccount.Items);
            RestoreContainer(player, dbAccount.TransferredEntities);

            foreach (Avatar avatar in new AvatarIterator(player))
            {
                RestoreContainer(avatar, dbAccount.Items);
                RestoreContainer(avatar, dbAccount.ControlledEntities);
                RestoreContainer(avatar, dbAccount.TransferredEntities);
            }

            EntityManager entityManager = player.Game.EntityManager;

            foreach (var entry in player.GetInventory(InventoryConvenienceLabel.TeamUpLibrary))
            {
                Agent teamUp = entityManager.GetEntity<Agent>(entry.Id);
                if (teamUp == null)
                {
                    Logger.Warn("RestoreInventoryEntities(): teamUp == null");
                    continue;
                }

                RestoreContainer(teamUp, dbAccount.Items);
                RestoreContainer(teamUp, dbAccount.TransferredEntities);
            }
        }

        private static bool StoreContainer(Entity container, DBAccount dbAccount, bool allowReplicateForTransfer)
        {
            foreach (Inventory inventory in new InventoryIterator(container))
            {
                if (inventory.Prototype.PersistedToDatabase == false)
                {
                    if (allowReplicateForTransfer == false || inventory.Prototype.ReplicateForTransfer == false)
                        continue;
                }

                StoreInventory(inventory, dbAccount);
            }

            return true;
        }

        private static bool StoreInventory(Inventory inventory, DBAccount dbAccount)
        {
            if (inventory == null) return Logger.WarnReturn(false, "StoreInventory(): inventory == null");

            DBEntityCollection entities;

            if (inventory.Prototype.PersistedToDatabase == false)
            {
                entities = dbAccount.TransferredEntities;
            }
            else if (inventory.Category == InventoryCategory.PlayerAvatars)
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

            EntityManager entityManager = inventory.Game.EntityManager;

            foreach (var entry in inventory)
            {
                Entity entity = entityManager.GetEntity<Entity>(entry.Id);
                
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

                //Logger.Debug($"StoreInventory(): Archived entity {entity}");
            }

            return true;
        }

        private static bool RestoreContainer(Entity container, DBEntityCollection entities)
        {
            EntityManager entityManager = container.Game.EntityManager;

            long containerDbGuid = (long)container.DatabaseUniqueId;
            ulong containerEntityId = container.Id;

            IReadOnlyList<DBEntity> dbEntityList = entities.GetEntriesForContainer(containerDbGuid);
            for (int i = 0; i < dbEntityList.Count; i++)
            {
                DBEntity dbEntity = dbEntityList[i];

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

                using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
                settings.DbGuid = (ulong)dbEntity.DbGuid;
                settings.InventoryLocation = new(containerEntityId, inventoryProtoRef, dbEntity.Slot);
                settings.EntityRef = entityProtoRef;
                settings.ArchiveSerializeType = ArchiveSerializeType.Database;
                settings.ArchiveData = dbEntity.ArchiveData;

                entityManager.CreateEntity(settings);
            }

            return true;
        }
    }
}
