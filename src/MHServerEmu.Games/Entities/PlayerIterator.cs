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

        private readonly Game _game = null;
        private readonly Region _region = null;
        private readonly IEnumerable<Player> _players = Array.Empty<Player>();

        public PlayerIterator(Game game)
        {
            _game = game;
            _players = _game.EntityManager.Players;
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
            _players = _game.EntityManager.Players;
        }

        public IEnumerator<Player> GetEnumerator()
        {
            foreach (Player player in _players)
            {
                if (_region != null && player.GetRegion() != _region)
                    continue;

                yield return player;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
