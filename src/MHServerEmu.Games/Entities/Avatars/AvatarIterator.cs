using System.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities.Avatars
{
    public enum AvatarIteratorMode
    {
        IncludeArchived,
        ExcludeArchived
    }

    public readonly struct AvatarIterator
    {
        // NOTE: In the client this iterator uses unfinished AvatarMode functionality that we don't really need to implement
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Player _player;
        private readonly AvatarIteratorMode _iteratorMode;
        private readonly PrototypeId _avatarProtoRef;

        public AvatarIterator(Player player, AvatarIteratorMode iteratorMode = AvatarIteratorMode.ExcludeArchived)
        {
            _player = player;
            _iteratorMode = iteratorMode;
            _avatarProtoRef = PrototypeId.Invalid;
        }

        public AvatarIterator(Player player, AvatarIteratorMode iteratorMode, PrototypeId avatarProtoRef)
        {
            _player = player;
            _iteratorMode = iteratorMode;
            _avatarProtoRef = avatarProtoRef;
        }

        public Enumerator GetEnumerator()
        {
            return new(_player, _iteratorMode, _avatarProtoRef);
        }

        public struct Enumerator : IEnumerator<Avatar>
        {
            private readonly EntityManager _entityManager;
            private readonly InventoryIterator _inventoryIterator;
            private readonly AvatarIteratorMode _iteratorMode;
            private readonly PrototypeId _avatarProtoRef;

            private InventoryIterator.Enumerator _inventoryIteratorEnumerator;
            private Inventory.Enumerator _inventoryEnumerator;
            private bool _hasInventoryEnumerator;

            public Avatar Current { get; private set; }
            object IEnumerator.Current { get => Current; }

            public Enumerator(Player player, AvatarIteratorMode iteratorMode, PrototypeId avatarProtoRef)
            {
                _entityManager = player.Game.EntityManager;
                _inventoryIterator = new(player, InventoryIterationFlags.PlayerAvatars);
                _iteratorMode = iteratorMode;
                _avatarProtoRef = avatarProtoRef;

                _inventoryIteratorEnumerator = _inventoryIterator.GetEnumerator();
                _hasInventoryEnumerator = false;
            }

            public bool MoveNext()
            {
                if (AdvanceToValidAvatar())
                    return true;

                // Try to find the next avatar inventory
                // Find the next avatar inventory
                while (_inventoryIteratorEnumerator.MoveNext())
                {
                    Inventory inventory = _inventoryIteratorEnumerator.Current;
                    if (inventory.ConvenienceLabel != InventoryConvenienceLabel.AvatarInPlay && inventory.ConvenienceLabel != InventoryConvenienceLabel.AvatarLibrary)
                        continue;

                    // Begin iterating the next inventory
                    _inventoryEnumerator = inventory.GetEnumerator();
                    _hasInventoryEnumerator = true;
                    if (AdvanceToValidAvatar())
                        return true;
                }

                // No more avatar inventories
                Current = null;
                return false;
            }

            public void Reset()
            {
                _inventoryIteratorEnumerator.Dispose();
                _inventoryEnumerator.Dispose();

                _inventoryIteratorEnumerator = _inventoryIterator.GetEnumerator();
                _inventoryEnumerator = default;
                _hasInventoryEnumerator = false;
            }

            public void Dispose()
            {
                _inventoryIteratorEnumerator.Dispose();
                _inventoryEnumerator.Dispose();
            }

            private bool AdvanceToValidAvatar()
            {
                if (_hasInventoryEnumerator == false)
                    return false;

                // Iterate the current avatar inventory if there is one
                while (_inventoryEnumerator.MoveNext())
                {
                    var entry = _inventoryEnumerator.Current;

                    if (_avatarProtoRef != PrototypeId.Invalid && entry.ProtoRef != _avatarProtoRef)
                        continue;

                    if (_iteratorMode == AvatarIteratorMode.ExcludeArchived && _entityManager.IsEntityArchived(entry.Id))
                        continue;

                    // we are skipping avatar mode check here

                    Avatar avatar = _entityManager.GetEntity<Avatar>(entry.Id);
                    if (avatar == null)
                    {
                        Logger.Warn("AdvanceToValidAvatar(): avatar == null");
                        continue;
                    }

                    Current = avatar;
                    return true;
                }

                _hasInventoryEnumerator = false;
                return false;
            }
        }
    }
}
