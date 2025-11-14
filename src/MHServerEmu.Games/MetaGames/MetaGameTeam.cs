using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.MetaGames
{
    public class MetaGameTeam
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        protected readonly List<Player> _players = new();

        public MetaGame MetaGame { get; }
        public PrototypeId ProtoRef { get; }
        public int MaxPlayers { get; }
        public int TeamSize { get => _players.Count; }

        public MetaGameTeam(MetaGame metaGame, PrototypeId protoRef, int maxPlayers)
        {
            MetaGame = metaGame;
            ProtoRef = protoRef;
            MaxPlayers = maxPlayers;
        }

        public List<Player>.Enumerator GetEnumerator()
        {
            return _players.GetEnumerator();
        }

        public virtual bool AddPlayer(Player player)
        {
            if (IndexOf(player) >= 0) return Logger.WarnReturn(false, "Attempt to add a player to a team twice");
            _players.Add(player); 
            return true;
        }

        public virtual void ClearPlayers()
        {
            while (_players.Count > 0) 
                RemovePlayer(_players[0]);
        }

        public virtual bool RemovePlayer(Player player)
        {
            if (_players.Contains(player) == false) return false;
            _players.Remove(player);
            return true;
        }

        public bool Contains(Player player) => IndexOf(player) > -1;
        public int IndexOf(Player player) => _players.FindIndex(found => found == player);

        public void Destroy()
        {
            // TODO destroy chat
        }

        public void SendTeamRoster(Player player)
        {
            var message = NetMessageMatchTeamSizeNotification.CreateBuilder()
                .SetMetaGameEntityId(MetaGame.Id)
                .SetTeamSize((uint)TeamSize).Build();
            player.SendMessage(message);

            var rosterMessage = NetMessageMatchTeamRosterNotification.CreateBuilder()
                .SetMetaGameEntityId(MetaGame.Id)
                .SetTeamPrototypeId((ulong)ProtoRef);

            foreach (var teamPlayer in _players)
                rosterMessage.AddPlayerDbGuids(teamPlayer.DatabaseUniqueId);

            player.SendMessage(rosterMessage.Build());
        }
    }
}
