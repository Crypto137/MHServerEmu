using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.PlayerManagement.Games;
using MHServerEmu.PlayerManagement.Players;
using MHServerEmu.PlayerManagement.Regions;

namespace MHServerEmu.PlayerManagement.Social
{
    public class MasterGuild
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static bool PersistenceEnabled { get => PlayerManagerService.Instance.Config.EnablePersistence; }

        // NOTE: All guild DB operations are currently synchronous.
        // May need some kind of async job queue for these, especially for potential non-SQLite backends.
        private readonly DBGuild _data;

        private readonly Dictionary<ulong, MemberEntry> _members = new();

        private readonly Dictionary<ulong, PlayerHandle> _onlineMembers = new();
        private readonly HashSet<GameHandle> _games = new();

        private MemberEntry? _leader;

        private GuildMessageSetToServer _guildCompleteInfoCache;

        public ulong Id { get => (ulong)_data.Id; }
        public string Name { get => _data.Name; }
        public string Motd { get => _data.Motd; }
        public int MemberCount { get => _data.Members.Count; }

        public MasterGuild(DBGuild data, bool saveToDatabase)
        {
            _data = data;

            foreach (DBGuildMember member in _data.Members)
                AddMember(member);

            ClientManager clientManager = PlayerManagerService.Instance.ClientManager;
            foreach (MemberEntry member in _members.Values)
            {
                PlayerHandle player = clientManager.GetPlayer(member.PlayerDbId);
                if (player != null)
                    AddOnlineMember(player);
            }

            if (saveToDatabase)
            {
                SaveToDatabase();

                foreach (MemberEntry member in _members.Values)
                    member.SaveToDatabase();
            }
        }

        public override string ToString()
        {
            return _data.ToString();
        }

        public bool ContainsMember(ulong playerDbId)
        {
            return _members.ContainsKey(playerDbId);
        }

        public void OnCreated()
        {
            foreach (GameHandle game in _games)
                SendToGame(game);
        }

        public void OnMemberOnline(PlayerHandle player)
        {
            if (player == null)
                return;

            if (ContainsMember(player.PlayerDbId) == false)
                return;

            AddOnlineMember(player);
        }

        public void OnMemberOffline(PlayerHandle player)
        {
            if (player == null)
                return;

            if (ContainsMember(player.PlayerDbId) == false)
                return;

            RemoveOnlineMember(player);
        }

        public void OnMemberRegionChanged(PlayerHandle player, RegionHandle newRegion, RegionHandle prevRegion)
        {

            if (newRegion != null)
            {
                GameHandle newGame = newRegion.Game;
                if (AddGame(newGame))
                    SendToGame(newGame);
            }

            if (prevRegion != null)
                RemoveGame(prevRegion.Game);
        }

        private bool SaveToDatabase()
        {
            if (PersistenceEnabled == false)
                return true;

            return IDBManager.Instance.SaveGuild(_data);
        }

        private bool DeleteFromDatabase()
        {
            if (PersistenceEnabled == false)
                return true;

            return IDBManager.Instance.DeleteGuild(_data);
        }

        private MemberEntry? AddMember(DBGuildMember data)
        {
            ulong playerDbId = (ulong)data.PlayerDbGuid;

            MemberEntry? existingMember = GetMember(playerDbId);
            if (existingMember != null)
                return Logger.WarnReturn<MemberEntry?>(null, $"AddMember(): Attempted to add existing member [{existingMember}] to guild [{this}]");

            bool isLeader = data.Membership == (int)GuildMembership.eGMLeader;
            if (isLeader && _leader != null)
                return Logger.WarnReturn<MemberEntry?>(null, $"AddMember(): Attempted to add a second leader [{data}] when there is an existing leader [{_leader}] in guild [{this}]");

            MemberEntry member = new(data);
            _members.Add(playerDbId, member);

            if (isLeader)
                _leader = member;

            // TODO: Remove this lookup when we remove the member
            PlayerManagerService.Instance.GuildManager.SetGuildForPlayer(playerDbId, this);

            // Invalidate cache. (TODO: Also do it when change membership or guild name/motd)
            _guildCompleteInfoCache = null;

            return member;
        }

        private MemberEntry? GetMember(ulong playerDbId)
        {
            if (_members.TryGetValue(playerDbId, out MemberEntry member) == false)
                return null;

            return member;
        }

        private void AddOnlineMember(PlayerHandle player)
        {
            _onlineMembers.Add(player.PlayerDbId, player);
            player.Guild = this;

            if (player.State == PlayerHandleState.InGame && player.CurrentGame != null)
                AddGame(player.CurrentGame);
        }

        private void RemoveOnlineMember(PlayerHandle player)
        {
            player.Guild = null;
            _onlineMembers.Remove(player.PlayerDbId);
        }

        private bool AddGame(GameHandle game)
        {
            if (game == null) return Logger.WarnReturn(false, "AddGame(): game == null");

            return _games.Add(game);
        }

        private bool RemoveGame(GameHandle game)
        {
            if (game == null) return Logger.WarnReturn(false, "RemoveGame(): game == null");

            if (HasMembersInGame(game))
                return false;

            return _games.Remove(game);
        }

        private bool HasMembersInGame(GameHandle game)
        {
            foreach (PlayerHandle player in _onlineMembers.Values)
            {
                if (player.State == PlayerHandleState.InGame && player.CurrentGame == game)
                    return true;
            }

            return false;
        }

        private bool SendToGame(GameHandle game)
        {
            if (game == null) return Logger.WarnReturn(false, "SendToGame(): game == null");
            if (game.IsRunning == false) return Logger.WarnReturn(false, "SendToGame(): game.IsRunning == false");

            // The cache is invalidated when something about the guild changes (name / motd / memberships).
            // (Re)build it if needed.
            if (_guildCompleteInfoCache == null)
            {
                var guildCompleteInfo = GuildCompleteInfo.CreateBuilder()
                    .SetGuildId(Id)
                    .SetGuildName(Name);

                foreach (MemberEntry member in _members.Values)
                    guildCompleteInfo.AddMembers(member.ToGuildMemberInfo());

                if (string.IsNullOrWhiteSpace(Motd) == false)
                    guildCompleteInfo.SetGuildMotd(Motd);

                _guildCompleteInfoCache = GuildMessageSetToServer.CreateBuilder()
                    .SetGuildCompleteInfo(guildCompleteInfo)
                    .Build();
            }

            ServiceMessage.GuildMessageToServer message = new(game.Id, _guildCompleteInfoCache);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);

            return true;
        }

        /// <summary>
        /// A wrapper for <see cref="DBGuildMember"/> for easier data access.
        /// </summary>
        private readonly struct MemberEntry(DBGuildMember data)
        {
            private readonly DBGuildMember _data = data;

            public ulong PlayerDbId { get => (ulong)_data.PlayerDbGuid; }
            public string PlayerName { get => GetPlayerName(); }
            public GuildMembership Membership { get => (GuildMembership)_data.Membership; }

            public override string ToString()
            {
                return $"{PlayerName} (0x{PlayerDbId:X}) - {Membership}";
            }

            public GuildMemberInfo ToGuildMemberInfo()
            {
                return GuildMemberInfo.CreateBuilder()
                    .SetPlayerId(PlayerDbId)
                    .SetPlayerName(PlayerName)
                    .SetMembership(Membership)
                    .Build();
            }

            public bool SaveToDatabase()
            {
                if (PersistenceEnabled == false)
                    return true;

                return IDBManager.Instance.SaveGuildMember(_data);
            }

            public bool DeleteFromDatabase()
            {
                if (PersistenceEnabled == false)
                    return true;

                return IDBManager.Instance.DeleteGuildMember(_data);
            }

            private string GetPlayerName()
            {
                ulong playerDbId = PlayerDbId;

                // Doing lookups every time is somewhat suboptimal, but it's more straightforward than
                // keeping guild members in sync if a member's name changes. Reevaluate this if needed.
                if (PlayerNameCache.Instance.TryGetPlayerName(playerDbId, out string playerName) == false)
                    return $"0x{playerDbId:X}";

                return playerName;
            }
        }
    }
}
