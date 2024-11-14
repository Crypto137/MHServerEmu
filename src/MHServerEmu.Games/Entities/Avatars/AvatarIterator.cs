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

    public readonly struct AvatarIterator : IEnumerable<Avatar>
    {
        // NOTE: In the client this iterator uses unfinished AvatarMode functionality that we don't really need to implement
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Game _game;
        private readonly InventoryIterator _inventoryIterator;
        private readonly AvatarIteratorMode _iteratorMode;
        private readonly PrototypeId _avatarProtoRef;

        public AvatarIterator(Player player, AvatarIteratorMode iteratorMode = AvatarIteratorMode.ExcludeArchived)
        {
            _game = player.Game;
            _inventoryIterator = new(player, InventoryIterationFlags.PlayerAvatars);
            _iteratorMode = iteratorMode;
            _avatarProtoRef = PrototypeId.Invalid;
        }

        public AvatarIterator(Player player, AvatarIteratorMode iteratorMode, PrototypeId avatarProtoRef)
        {
            _game = player.Game;
            _inventoryIterator = new(player, InventoryIterationFlags.PlayerAvatars);
            _iteratorMode = iteratorMode;
            _avatarProtoRef = avatarProtoRef;
        }

        public IEnumerator<Avatar> GetEnumerator()
        {
            EntityManager entityManager = _game.EntityManager;

            foreach (Inventory inventory in _inventoryIterator)
            {
                if (inventory.ConvenienceLabel != InventoryConvenienceLabel.AvatarInPlay && inventory.ConvenienceLabel != InventoryConvenienceLabel.AvatarLibrary)
                    continue;

                foreach (var entry in inventory)
                {
                    if (_avatarProtoRef != PrototypeId.Invalid && entry.ProtoRef != _avatarProtoRef)
                        continue;

                    if (_iteratorMode == AvatarIteratorMode.ExcludeArchived && entityManager.IsEntityArchived(entry.Id))
                        continue;

                    // we are skipping avatar mode check here

                    Avatar avatar = entityManager.GetEntity<Avatar>(entry.Id);
                    if (avatar == null)
                    {
                        Logger.Warn("GetEnumerator(): avatar == null");
                        continue;
                    }

                    yield return avatar;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
