using System.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    /// <summary>
    /// Iterates <see cref="Player"/> instances in the specified <see cref="Game"/> or <see cref="Region"/>.
    /// </summary>
    public readonly struct PlayerIterator : IEnumerable<Player>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Game _game;
        private readonly Region _region;

        public PlayerIterator(Game game)
        {
            _game = game;
            _region = null;
        }

        public PlayerIterator(Region region)
        {
            if (region == null)
            {
                Logger.Warn("PlayerIterator(): region == null");
                return;
            }

            _game = region.Game;
            _region = region;
        }

        public Enumerator GetEnumerator()
        {
            return new(_game?.EntityManager.Players, _region);
        }

        // NOTE: Missions and MetaGames rely on PlayerIterator being IEnumerable for things like ToArray(). Consider optimizing this.

        IEnumerator<Player> IEnumerable<Player>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<Player>
        {
            private readonly HashSet<Player> _players;
            private readonly Region _region;

            private HashSet<Player>.Enumerator _playerEnumerator;

            public Player Current { get; private set; }
            object IEnumerator.Current { get => Current; }

            public Enumerator(HashSet<Player> players, Region region)
            {
                _players = players;
                _region = region;

                _playerEnumerator = _players != null ? _players.GetEnumerator() : default;
            }

            public bool MoveNext()
            {
                if (_players == null)
                    return false;

                while (_playerEnumerator.MoveNext())
                {
                    Player player = _playerEnumerator.Current;
                    if (_region != null && player.GetRegion() != _region)
                        continue;

                    Current = player;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _playerEnumerator = _players != null ? _players.GetEnumerator() : default;
            }

            public void Dispose()
            {
                _playerEnumerator.Dispose();
            }
        }
    }
}
