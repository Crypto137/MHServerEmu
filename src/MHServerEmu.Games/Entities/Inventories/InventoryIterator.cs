using System.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities.Inventories
{
    // NOTE: Most iteration flags are for filtering by category or convenience label
    [Flags]
    public enum InventoryIterationFlags
    {
        None                        = 0,
        PlayerGeneral               = 1 << 0,
        PlayerGeneralExtra          = 1 << 1,
        PlayerAvatars               = 1 << 2,
        PlayerStashAvatarSpecific   = 1 << 3,
        PlayerStashGeneral          = 1 << 4,
        DeliveryBoxAndErrorRecovery = 1 << 5,
        Equipment                   = 1 << 6,
        SortByPrototypeRef          = 1 << 7
    }

    /// <summary>
    /// Iterates <see cref="Inventory"/> instances belonging to an <see cref="Entity"/>.
    /// </summary>
    public readonly struct InventoryIterator : IEnumerable<Inventory>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Entity _entity;
        private readonly InventoryIterationFlags _flags;

        public InventoryIterator(Entity entity, InventoryIterationFlags flags = InventoryIterationFlags.None)
        {
            _entity = entity;
            _flags = flags;
        }

        /// <summary>
        /// Returns <see langword="true"/> if any of the inventories belonging to the provided <see cref="Entity"/>
        /// that match the specified <see cref="InventoryIterationFlags"/> contain an entity with the specified <see cref="PrototypeId"/>.
        /// </summary>
        public static bool ContainsMatchingEntity(Entity entity, PrototypeId entityRef, InventoryIterationFlags flags = InventoryIterationFlags.None)
        {
            foreach (Inventory inventory in new InventoryIterator(entity, flags))
                if (inventory.ContainsMatchingEntity(entityRef)) return true;

            return false;
        }

        /// <summary>
        /// Returns the number of entities with the specified <see cref="PrototypeId"/> in the inventories belonging to
        /// the provided <see cref="Entity"/> that match the specified <see cref="InventoryIterationFlags"/>.
        /// </summary>
        public static int GetMatchingContained(Entity entity, PrototypeId entityRef, InventoryIterationFlags flags = InventoryIterationFlags.None, List<ulong> matchList = null)
        {
            int numMatches = 0;

            foreach (Inventory inventory in new InventoryIterator(entity, flags))
                numMatches += inventory.GetMatchingEntities(entityRef, matchList);

            return numMatches;
        }

        public IEnumerator<Inventory> GetEnumerator()
        {
            IEnumerable iterationTarget = _flags.HasFlag(InventoryIterationFlags.SortByPrototypeRef)
                ? _entity.InventoryCollection.OrderBy(inventory => inventory.PrototypeDataRef)
                : _entity.InventoryCollection;

            foreach (Inventory inventory in iterationTarget)
            {
                InventoryPrototype inventoryPrototype = inventory.Prototype;
                if (inventoryPrototype == null)
                {
                    Logger.Warn("GetEnumerator(): inventoryPrototype == null");
                    continue;
                }

                // Return the inventory right away if we don't have any filter flags
                if ((_flags & ~InventoryIterationFlags.SortByPrototypeRef) == InventoryIterationFlags.None)
                {
                    yield return inventory;
                    continue;
                }

                // Filter by flags
                if (_flags.HasFlag(InventoryIterationFlags.PlayerGeneral) && inventoryPrototype.IsPlayerGeneralInventory)
                {
                    yield return inventory;
                    continue;
                }

                if (_flags.HasFlag(InventoryIterationFlags.PlayerGeneralExtra) && inventoryPrototype.Category == InventoryCategory.PlayerGeneralExtra)
                {
                    yield return inventory;
                    continue;
                }

                if (_flags.HasFlag(InventoryIterationFlags.PlayerAvatars) && inventoryPrototype.Category == InventoryCategory.PlayerAvatars)
                {
                    yield return inventory;
                    continue;
                }

                if (_flags.HasFlag(InventoryIterationFlags.Equipment) && inventoryPrototype.IsEquipmentInventory)
                {
                    yield return inventory;
                    continue;
                }

                if (_flags.HasFlag(InventoryIterationFlags.PlayerStashAvatarSpecific) && inventoryPrototype.Category == InventoryCategory.PlayerStashAvatarSpecific)
                {
                    yield return inventory;
                    continue;
                }

                if (_flags.HasFlag(InventoryIterationFlags.PlayerStashGeneral) && inventoryPrototype.Category == InventoryCategory.PlayerStashGeneral)
                {
                    yield return inventory;
                    continue;
                }

                if (_flags.HasFlag(InventoryIterationFlags.DeliveryBoxAndErrorRecovery)
                    && (inventoryPrototype.ConvenienceLabel == InventoryConvenienceLabel.DeliveryBox || inventory.ConvenienceLabel == InventoryConvenienceLabel.ErrorRecovery))
                {
                    yield return inventory;
                    continue;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
