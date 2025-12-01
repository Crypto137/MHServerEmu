using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.GameData;
using MHServerEmu.PlayerManagement.Players;
using MHServerEmu.PlayerManagement.Social;

namespace MHServerEmu.PlayerManagement.Matchmaking
{
    public class RegionRequestGroup
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static ulong _currentGroupId = 0;

        private readonly HashSet<PlayerHandle> _players = new();

        public ulong Id { get; }
        public RegionRequestQueue Queue { get; }
        public PrototypeId DifficultyTierRef { get; }
        public PrototypeId MetaStateRef { get; }
        public bool IsBypass { get; }

        private RegionRequestGroup(ulong id, RegionRequestQueue queue, PrototypeId difficultyTierRef, PrototypeId metaStateRef, bool isBypass)
        {
            Id = id;
            Queue = queue;

            DifficultyTierRef = difficultyTierRef;
            MetaStateRef = metaStateRef;
            IsBypass = isBypass;
        }

        public static RegionRequestGroup Create(RegionRequestQueue queue, PrototypeId difficultyTierRef, PrototypeId metaStateRef,
            PlayerHandle player, MasterParty party, bool isBypass)
        {
            if (queue == null) return Logger.WarnReturn<RegionRequestGroup>(null, "Create(): queue == null");
            if (player == null) return Logger.WarnReturn<RegionRequestGroup>(null, "Create(): player == null");

            ulong groupId = ++_currentGroupId;

            HashSet<PlayerHandle> players = HashSetPool<PlayerHandle>.Instance.Get();
            if (party != null)
                party.GetMembers(players);
            else
                players.Add(player);

            RegionRequestGroup group = new(groupId, queue, difficultyTierRef, metaStateRef, isBypass);
            group.AddPlayers(player, players);

            return group;
        }

        public void AddPlayers(PlayerHandle inviter, HashSet<PlayerHandle> invitees)
        {
            foreach (PlayerHandle invitee in invitees)
            {
                _players.Add(invitee);
                invitee.RegionRequestGroup = this;
            }
        }

        public void RemovePlayers(HashSet<PlayerHandle> players)
        {
            foreach (PlayerHandle player in players)
            {
                _players.Remove(player);
                player.RegionRequestGroup = null;
            }
        }

        public void RemovePlayer(PlayerHandle player)
        {
            HashSet<PlayerHandle> players = HashSetPool<PlayerHandle>.Instance.Get();
            players.Add(player);
            RemovePlayers(players);
            HashSetPool<PlayerHandle>.Instance.Return(players);
        }
    }
}
