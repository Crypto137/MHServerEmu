using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities
{
    public class TagPlayers
    {
        private readonly SortedSet<TagInfo> _tags = [];
        public SortedSet<TagInfo> Tags { get => _tags; }
        public bool HasTags { get => _tags.Count > 0; }
        public WorldEntity Owner { get; }

        public TagPlayers(WorldEntity worldEntity)
        {
            Owner = worldEntity;
        }

        public PlayerTagIterator GetPlayers(TimeSpan time = default)
        {
            return new PlayerTagIterator(this, time);
        }

        public void Add(Player player, PowerPrototype powerProto)
        {
            var tag = new TagInfo(player.DatabaseUniqueId, powerProto, player.Game.CurrentTime);

            if (_tags.TryGetValue(tag, out var existing))
                _tags.Remove(existing);

            _tags.Add(tag);

            player.AddTag(Owner);
        }

        public void Clear()
        {
            _tags.Clear();
        }
    }

    public readonly struct PlayerTagIterator
    {
        private readonly TagPlayers _tags;
        private readonly TimeSpan _time;

        public PlayerTagIterator(TagPlayers tags, TimeSpan time)
        {
            _tags = tags;
            _time = time;
        }

        public Enumerator GetEnumerator() => new (_tags, _time);

        public struct Enumerator
        {
            private readonly EntityManager _manager;
            private readonly TimeSpan _curTime;
            private readonly TimeSpan _maxAge;

            private ulong _lastUid;
            private Player _current;
            private SortedSet<TagInfo>.Enumerator _enumerator;

            public Enumerator(TagPlayers tagPlayers, TimeSpan maxAge)
            {
                var game = tagPlayers.Owner.Game;
                _manager = game.EntityManager;
                _curTime = game.CurrentTime;
                _maxAge = maxAge;

                _lastUid = 0;
                _current = null;
                _enumerator = tagPlayers.Tags.GetEnumerator();
            }

            public Player Current => _current;

            public bool MoveNext()
            {
                while (_enumerator.MoveNext())
                {
                    var tag = _enumerator.Current;

                    if (_maxAge != default && _curTime - tag.Time > _maxAge)
                        continue;

                    if (_lastUid == tag.PlayerUID)
                        continue;

                    _lastUid = tag.PlayerUID;

                    _current = _manager.GetEntityByDbGuid<Player>(_lastUid);
                    if (_current != null)
                        return true;
                }

                return false;
            }
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
            int uidCompare = PlayerUID.CompareTo(other.PlayerUID);
            if (uidCompare != 0) return uidCompare;

            if (PowerPrototype == null && other.PowerPrototype == null) return 0;
            if (PowerPrototype == null) return -1;
            if (other.PowerPrototype == null) return 1;

            return PowerPrototype.DataRef.CompareTo(other.PowerPrototype.DataRef);
        }
    }

}
