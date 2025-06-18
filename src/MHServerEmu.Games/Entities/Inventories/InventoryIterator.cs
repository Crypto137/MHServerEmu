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
        SortByPrototypeRef          = 1 << 7,

        CraftingIngredients         = PlayerGeneral | PlayerGeneralExtra | PlayerStashAvatarSpecific | PlayerStashGeneral,
    }

    /// <summary>
    /// Iterates <see cref="Inventory"/> instances belonging to an <see cref="Entity"/>.
    /// </summary>
    public readonly struct InventoryIterator
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

        public Enumerator GetEnumerator()
        {
            return new(_entity, _flags);
        }

        public struct Enumerator : IEnumerator<Inventory>
        {
            private readonly InventoryCollection _inventoryCollection;
            private readonly InventoryIterationFlags _flags;

            private InventoryCollection.Enumerator _inventoryCollectionEnumerator;

            private List<PrototypeId> _sortedInventoryRefs;
            private List<PrototypeId>.Enumerator _sortedInventoryRefsEnumerator;

            public Inventory Current { get; private set; }
            object IEnumerator.Current { get => Current; }

            public Enumerator(Entity entity, InventoryIterationFlags flags)
            {
                _inventoryCollection = entity.InventoryCollection;
                _flags = flags;

                if (flags.HasFlag(InventoryIterationFlags.SortByPrototypeRef))
                {
                    // Prepare a sorted list of inventories if requested.
                    // Kinda sucks we have to allocate a list here, but at least it's better than OrderBy() I guess.
                    _sortedInventoryRefs = new(_inventoryCollection.Count);

                    foreach (Inventory inventory in _inventoryCollection)
                        _sortedInventoryRefs.Add(inventory.PrototypeDataRef);

                    _sortedInventoryRefs.Sort();
                    _sortedInventoryRefsEnumerator = _sortedInventoryRefs.GetEnumerator();
                }
                else
                {
                    // Iterate in whatever order everything is in already
                    _inventoryCollectionEnumerator = _inventoryCollection.GetEnumerator();
                }
            }

            public bool MoveNext()
            {
                if (_flags.HasFlag(InventoryIterationFlags.SortByPrototypeRef))
                {
                    while (_sortedInventoryRefsEnumerator.MoveNext())
                    {
                        Inventory inventory = _inventoryCollection.GetInventoryByRef(_sortedInventoryRefsEnumerator.Current);
                        if (IsValid(inventory) == false)
                            continue;

                        Current = inventory;
                        return true;
                    }
                }
                else
                {
                    while (_inventoryCollectionEnumerator.MoveNext())
                    {
                        Inventory inventory = _inventoryCollectionEnumerator.Current;
                        if (IsValid(inventory) == false)
                            continue;

                        Current = inventory;
                        return true;
                    }
                }

                Current = null;
                return false;
            }

            public void Reset()
            {
                if (_flags.HasFlag(InventoryIterationFlags.SortByPrototypeRef))
                {
                    _sortedInventoryRefsEnumerator.Dispose();
                    _sortedInventoryRefsEnumerator = _sortedInventoryRefs.GetEnumerator();
                }
                else
                {
                    _inventoryCollectionEnumerator.Dispose();
                    _inventoryCollectionEnumerator = _inventoryCollection.GetEnumerator();
                }
            }

            public void Dispose()
            {
                if (_flags.HasFlag(InventoryIterationFlags.SortByPrototypeRef))
                {
                    _sortedInventoryRefsEnumerator.Dispose();
                }
                else
                {
                    _inventoryCollectionEnumerator.Dispose();
                }
            }

            private bool IsValid(Inventory inventory)
            {
                // Early out if not filter flags
                if ((_flags & ~InventoryIterationFlags.SortByPrototypeRef) == InventoryIterationFlags.None)
                    return true;

                InventoryPrototype inventoryPrototype = inventory.Prototype;
                if (inventoryPrototype == null)
                    return Logger.WarnReturn(false, $"IsValid(): Unable to get inventory prototype for inventory {inventory}");

                // Filter by flags
                if (_flags.HasFlag(InventoryIterationFlags.PlayerGeneral) && inventoryPrototype.IsPlayerGeneralInventory)
                    return true;

                if (_flags.HasFlag(InventoryIterationFlags.PlayerGeneralExtra) && inventoryPrototype.Category == InventoryCategory.PlayerGeneralExtra)
                    return true;

                if (_flags.HasFlag(InventoryIterationFlags.PlayerAvatars) && inventoryPrototype.Category == InventoryCategory.PlayerAvatars)
                    return true;

                if (_flags.HasFlag(InventoryIterationFlags.Equipment) && inventoryPrototype.IsEquipmentInventory)
                    return true;

                if (_flags.HasFlag(InventoryIterationFlags.PlayerStashAvatarSpecific) && inventoryPrototype.Category == InventoryCategory.PlayerStashAvatarSpecific)
                    return true;

                if (_flags.HasFlag(InventoryIterationFlags.PlayerStashGeneral) && inventoryPrototype.Category == InventoryCategory.PlayerStashGeneral)
                    return true;

                if (_flags.HasFlag(InventoryIterationFlags.DeliveryBoxAndErrorRecovery)
                    && (inventoryPrototype.ConvenienceLabel == InventoryConvenienceLabel.DeliveryBox || inventory.ConvenienceLabel == InventoryConvenienceLabel.ErrorRecovery))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
