using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities
{
    public class TagPlayers
    {
        private EntityManager _manager;
        private WorldEntity _owner;
        private SortedSet<TagInfo> _tags;

        public SortedSet<TagInfo> Tags { get => _tags; }
        public bool HasTags { get => _tags.Count > 0; }

        public TagPlayers(WorldEntity worldEntity)
        {
            _owner = worldEntity;
            _manager = worldEntity.Game.EntityManager;
            _tags = new();
        }

        public IEnumerable<Player> GetPlayers()
        {
            ulong playerUid = 0;           
            foreach(var tag in _tags)
            {
                if (playerUid == tag.PlayerUID) continue;
                else playerUid = tag.PlayerUID;

                var player = _manager.GetEntityByDbGuid<Player>(playerUid);
                if (player != null)
                    yield return player;
            }
        }

        public void Add(Player player, PowerPrototype powerProto)
        {
            var tag = new TagInfo(player.DatabaseUniqueId, powerProto, player.Game.CurrentTime);

            if (_tags.Add(tag) == false)
            {
                _tags.Remove(tag);
                _tags.Add(tag);
            }

            player.AddTag(_owner);
        }
    }

    public struct TagInfo : IComparable<TagInfo>
    {
        public ulong PlayerUID;
        public PowerPrototype PowerPrototype;
        public TimeSpan Time;

        public TagInfo(ulong playerUID, PowerPrototype powerPrototype, TimeSpan time)
        {
            PlayerUID = playerUID;
            PowerPrototype = powerPrototype;
            Time = time;
        }

        public int CompareTo(TagInfo other)
        {
            if (PlayerUID == other.PlayerUID && PowerPrototype == other.PowerPrototype) return 0;
            return PlayerUID.CompareTo(other.PlayerUID);
        }
    }
}
