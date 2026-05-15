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
    public static class PersistenceUtility
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static bool StoreInventoryEntities(Player player, DBAccount dbAccount)
        {
            using DBAccount.EntityUpdateScope entityUpdateScope = dbAccount.BeginEntityUpdate();

            try
            {
                StoreContainer(player, dbAccount, true);

                foreach (Avatar avatar in new AvatarIterator(player))
                {
                    StoreContainer(avatar, dbAccount, true);
                }

                EntityManager entityManager = player.Game.EntityManager;

                foreach (var entry in player.GetInventory(InventoryConvenienceLabel.TeamUpLibrary))
                {
                    Agent teamUp = entityManager.GetEntity<Agent>(entry.Id);
                    if (!Verify.IsNotNull(teamUp))
                        continue;

                    // Team-ups shouldn't have transferrable summons, but disabling it explicitly just in case.
                    StoreContainer(teamUp, dbAccount, false);
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, $"StoreInventoryEntities(): Failed to store entities for player [{player}]");
                return false;
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
                if (!Verify.IsNotNull(teamUp))
                    continue;

                RestoreContainer(teamUp, dbAccount.Items);
                RestoreContainer(teamUp, dbAccount.TransferredEntities);
            }
        }

        private static bool StoreContainer(Entity container, DBAccount dbAccount, bool allowReplicateForTransfer)
        {
            if (!Verify.IsNotNull(container, LoggingLevel.Error)) return false;

            foreach (Inventory inventory in new InventoryIterator(container))
            {
                if (inventory.Prototype.PersistedToDatabase == false)
                {
                    if (allowReplicateForTransfer == false || inventory.Prototype.ReplicateForTransfer == false)
                        continue;
                }

                StoreInventory(container, inventory, dbAccount);
            }

            return true;
        }

        private static bool StoreInventory(Entity container, Inventory inventory, DBAccount dbAccount)
        {
            if (!Verify.IsNotNull(inventory, LoggingLevel.Error)) return false;

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

            // Common data for everything stored in this inventory.
            long containerDbGuid = (long)container.DatabaseUniqueId;
            long inventoryProtoGuid = (long)GameDatabase.GetPrototypeGuid(inventory.PrototypeDataRef);

            EntityManager entityManager = inventory.Game.EntityManager;

            foreach (var entry in inventory)
            {
                Entity entity = entityManager.GetEntity<Entity>(entry.Id);
                if (!Verify.IsNotNull(entity, LoggingLevel.Error))
                    continue;

                using Archive archive = new(ArchiveSerializeType.Database);
                if (!Verify.IsTrue(Serializer.Transfer(archive, ref entity), LoggingLevel.Error, $"Failed to serialize entity {entity}"))
                    continue;

                entities.UpdateEntity((long)entity.DatabaseUniqueId, containerDbGuid, inventoryProtoGuid, entry.Slot,
                    (long)GameDatabase.GetPrototypeGuid(entity.PrototypeDataRef), archive.AsSpan());
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

                if (!Verify.IsTrue(dbEntity.ContainerDbGuid == containerDbGuid, LoggingLevel.Error, $"Attempting to restore entity belonging to 0x{dbEntity.ContainerDbGuid:X} in 0x{containerDbGuid:X}"))
                    continue;

                PrototypeId inventoryProtoRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)dbEntity.InventoryProtoGuid);
                if (!Verify.IsTrue(inventoryProtoRef != PrototypeId.Invalid, LoggingLevel.Error, $"Failed to retrieve inventory proto ref for guid {dbEntity.InventoryProtoGuid}"))
                    continue;

                if (!Verify.IsNotNull(container.GetInventoryByRef(inventoryProtoRef), LoggingLevel.Error, $"Container {container} does not have inventory {inventoryProtoRef.GetName()}"))
                    continue;

                PrototypeId entityProtoRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)dbEntity.EntityProtoGuid);
                if (!Verify.IsTrue(entityProtoRef != PrototypeId.Invalid, LoggingLevel.Error, $"Failed to retrieve entity proto ref for guid {dbEntity.EntityProtoGuid}"))
                    continue;

                using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
                settings.DbGuid = (ulong)dbEntity.DbGuid;
                settings.InventoryLocation = new(containerEntityId, inventoryProtoRef, dbEntity.Slot);
                settings.EntityRef = entityProtoRef;
                settings.ArchiveSerializeType = ArchiveSerializeType.Database;
                settings.ArchiveData = dbEntity.ArchiveData;

                Verify.IsNotNull(entityManager.CreateEntity(settings), LoggingLevel.Error);
            }

            return true;
        }
    }
}
