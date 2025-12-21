using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.PlayerManagement.Games;
using MHServerEmu.PlayerManagement.Players;
using MHServerEmu.PlayerManagement.Regions;

namespace MHServerEmu.PlayerManagement.Social
{
    public class MasterGuild
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static bool PersistenceEnabled { get => PlayerManagerService.Instance.Config.EnablePersistence; }
        private static MasterGuildManager GuildManager { get => PlayerManagerService.Instance.GuildManager; }

        private readonly DBGuild _data;

        private readonly Dictionary<ulong, MemberEntry> _members = new();

        private readonly Dictionary<ulong, PlayerHandle> _onlineMembers = new();
        private readonly HashSet<GameHandle> _games = new();

        private readonly Dictionary<ulong, string> _pendingInvites = new();

        private MemberEntry? _leader;

        private GuildMessageSetToServer _guildCompleteInfoCache;

        public ulong Id { get => (ulong)_data.Id; }
        public string Name { get => _data.Name; }
        public string Motd { get => _data.Motd; }

        public int MemberCount { get => _members.Count; }
        public bool IsFull { get => MemberCount >= GameDatabase.GlobalsPrototype.PlayerGuildMaxSize; }

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

        public bool HasMember(ulong playerDbId)
        {
            return _members.ContainsKey(playerDbId);
        }

        public GuildChangeNameResultCode ChangeName(PlayerHandle player, string newName)
        {
            if (player == null || player.State != PlayerHandleState.InGame)
                return GuildChangeNameResultCode.eGCNRCNotOnline;

            // Should be validated before we get here.
            if (string.IsNullOrWhiteSpace(newName))
                return GuildChangeNameResultCode.eGCNRCInternalError;

            if (GetMember(player.PlayerDbId) is not MemberEntry member)
                return GuildChangeNameResultCode.eGCNRCNotInGuild;

            if (member.CanChangeName == false)
                return GuildChangeNameResultCode.eGCNRCNoPermission;

            _data.Name = newName;
            InvalidateGuildCompleteInfoCache();

            // Replicate to games
            var serverMessage = GuildMessageSetToServer.CreateBuilder()
                .SetGuildNameChanged(GuildNameChanged.CreateBuilder()
                    .SetGuildId(Id)
                    .SetNewGuildName(newName)
                    .SetChangedByPlayerName(player.PlayerName))
                .Build();

            SendMessageToAllGames(serverMessage);

            // Replicate to database
            SaveToDatabase();

            return GuildChangeNameResultCode.eGCNRCSuccess;
        }

        public GuildChangeMotdResultCode ChangeMotd(PlayerHandle player, string newMotd)
        {
            if (player == null || player.State != PlayerHandleState.InGame)
                return GuildChangeMotdResultCode.eGCMotdRCNotOnline;

            if (newMotd == null)
                return GuildChangeMotdResultCode.eGCMotdRCInternalError;

            if (GetMember(player.PlayerDbId) is not MemberEntry member)
                return GuildChangeMotdResultCode.eGCMotdRCNotInGuild;

            if (member.CanChangeMotd == false)
                return GuildChangeMotdResultCode.eGCMotdRCNoPermission;

            _data.Motd = newMotd;
            InvalidateGuildCompleteInfoCache();

            // Replicate to games
            var serverMessage = GuildMessageSetToServer.CreateBuilder()
                .SetGuildMotdChanged(GuildMotdChanged.CreateBuilder()
                    .SetGuildId(Id)
                    .SetNewGuildMotd(Motd)
                    .SetChangedByPlayerName(player.PlayerName))
                .Build();

            SendMessageToAllGames(serverMessage);

            // Replicate to database
            SaveToDatabase();

            return GuildChangeMotdResultCode.eGCMotdRCSuccess;
        }

        public GuildInviteResultCode InvitePlayer(PlayerHandle toInvitePlayer, PlayerHandle invitedByPlayer)
        {
            if (toInvitePlayer == null || toInvitePlayer.State != PlayerHandleState.InGame)
                return GuildInviteResultCode.eGIRCInvitedUnkownPlayer;

            ulong toInvitePlayerId = toInvitePlayer.PlayerDbId;

            if (toInvitePlayer.Guild == this)
                return GuildInviteResultCode.eGIRCInvitedInGuild;

            if (toInvitePlayer.Guild != null)
                return GuildInviteResultCode.eGIRCInvitedInOtherGuild;

            if (invitedByPlayer == null)
                return GuildInviteResultCode.eGIRCInternalError;

            string invitedByPlayerName = invitedByPlayer.PlayerName;

            if (GetMember(invitedByPlayer.PlayerDbId) is not MemberEntry invitedByMember)
                return GuildInviteResultCode.eGIRCInviterNotInGuild;

            if (invitedByMember.CanInvite == false)
                return GuildInviteResultCode.eGIRCInviterNoPermission;

            if (IsFull)
                return GuildInviteResultCode.eGIRCGuildFull;

            _pendingInvites[toInvitePlayerId] = invitedByPlayerName;

            var clientMessage = GuildMessageSetToClient.CreateBuilder()
                .SetGuildInvitedToJoin(GuildInvitedToJoin.CreateBuilder()
                    .SetGuildId(Id)
                    .SetGuildName(Name)
                    .SetInvitedByPlayerName(invitedByPlayerName))
                .Build();

            ServiceMessage.GuildMessageToClient message = new(toInvitePlayer.CurrentGame.Id, toInvitePlayerId, clientMessage);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);

            return GuildInviteResultCode.eGIRCSuccess;
        }

        public GuildRespondToInviteResultCode ReceiveInviteResponse(PlayerHandle player, GuildRespondToInviteCode respondCode)
        {
            if (player == null || player.State != PlayerHandleState.InGame)
                return GuildRespondToInviteResultCode.eGRIRCNotOnline;

            if (player.Guild == this)
                return GuildRespondToInviteResultCode.eGRIRCAlreadyInGuild;

            if (player.Guild != null)
                return GuildRespondToInviteResultCode.eGRIRAlreadyInOtherGuild;

            if (_pendingInvites.Remove(player.PlayerDbId, out string invitedByPlayerName) == false)
                return GuildRespondToInviteResultCode.eGRIRCNotInvited;

            if (IsFull)
                return GuildRespondToInviteResultCode.eGRIRCGuildFull;

            if (respondCode != GuildRespondToInviteCode.eGRICAccepted)
                return GuildRespondToInviteResultCode.eGRIRCRejected;

            if (CreateNewMember(player, invitedByPlayerName) == false)
                return GuildRespondToInviteResultCode.eGRIRCInternalError;

            return GuildRespondToInviteResultCode.eGRIRCJoined;
        }

        public GuildChangeMemberResultCode ChangeMember(PlayerHandle sourcePlayer, ulong targetPlayerId, GuildMembership newMembership)
        {
            if (sourcePlayer == null)
                return GuildChangeMemberResultCode.eGCMRCInternalError;

            if (GetMember(sourcePlayer.PlayerDbId) is not MemberEntry sourceMember)
                return GuildChangeMemberResultCode.eGCMRCInitiatorNotInGuild;

            if (GetMember(targetPlayerId) is not MemberEntry targetMember)
                return GuildChangeMemberResultCode.eGCMRCUnknownMember;

            if (_leader is not MemberEntry leaderMember)
                return GuildChangeMemberResultCode.eGCMRCInternalError;

            bool isTargetingLeader = targetMember.Equals(leaderMember);
            bool isRemovingTarget = newMembership == GuildMembership.eGMNone;

            // Leaders can't demote themselves to officers without promoting somebody else to be the next leader (unless they leave the guild entirely).
            if (isTargetingLeader && newMembership != GuildMembership.eGMNone)
                return GuildChangeMemberResultCode.eGCMRCCantModifyLeader;

            // Validate changes that require elevated privileges.
            if (isRemovingTarget == false || targetMember.Equals(sourceMember) == false)
            {
                switch (sourceMember.Membership)
                {
                    case GuildMembership.eGMMember:
                        return GuildChangeMemberResultCode.eGCMRCRequiresStaff;

                    case GuildMembership.eGMOfficer:
                        // Do not allow officers to promote to leader.
                        if (newMembership == GuildMembership.eGMLeader)
                            return GuildChangeMemberResultCode.eGCMRCRequiresLeader;

                        // Do not allow officers to kick each other.
                        if (isRemovingTarget && targetMember.Membership != GuildMembership.eGMMember)
                            return GuildChangeMemberResultCode.eGCMRCRequiresLeader;

                        break;

                    // The leader has UNLIMITED POWER muahahaha
                }
            }

            if (targetMember.Membership == newMembership)
                return GuildChangeMemberResultCode.eGCMRCNoChange;

            MemberEntry? nextLeader = null;
            MemberEntry? secondaryTargetMember = null;

            if (isRemovingTarget && isTargetingLeader)
            {
                // Leader leaving
                nextLeader = GetNextLeader(targetMember);
                secondaryTargetMember = nextLeader;
            }
            else if (newMembership == GuildMembership.eGMLeader)
            {
                // Leader passing leadership to another member
                nextLeader = targetMember;
                secondaryTargetMember = leaderMember;
            }

            // Modify memberships
            if (nextLeader != null)
            {
                leaderMember.SetMembership(GuildMembership.eGMOfficer);
                nextLeader.Value.SetMembership(GuildMembership.eGMLeader);
                _leader = nextLeader;
            }

            // The guild will be disbanded if we don't have anyone to pass leadership to.
            bool isDisbanding = false;
            if (isTargetingLeader && isRemovingTarget && nextLeader == null)
            {
                isDisbanding = true;
                foreach (MemberEntry member in _members.Values)
                    member.SetMembership(GuildMembership.eGMNone);
            }
            else
            {
                targetMember.SetMembership(newMembership);
            }

            InvalidateGuildCompleteInfoCache();

            // Replicate to games - this needs to be done before we remove members while we still have our game list.
            GuildMessageSetToServer serverMessage;

            if (isDisbanding == false)
            {
                var guildMembersInfoChanged = GuildMembersInfoChanged.CreateBuilder()
                    .SetGuildId(Id)
                    .SetInitiatingMemberName(sourcePlayer.PlayerName)
                    .AddMembers(targetMember.ToGuildMemberInfo());

                if (secondaryTargetMember != null)
                    guildMembersInfoChanged.AddMembers(secondaryTargetMember.Value.ToGuildMemberInfo());

                serverMessage = GuildMessageSetToServer.CreateBuilder()
                    .SetGuildMembersInfoChanged(guildMembersInfoChanged)
                    .Build();
            }
            else
            {
                serverMessage = GuildMessageSetToServer.CreateBuilder()
                    .SetGuildDisbanded(GuildDisbanded.CreateBuilder()
                        .SetGuildId(Id)
                        .SetDisbandingPlayerName(sourcePlayer.PlayerName))
                    .Build();
            }

            SendMessageToAllGames(serverMessage);

            // Clean up members
            if (targetMember.Membership == GuildMembership.eGMNone)
            {
                if (isDisbanding == false)
                {
                    // Remove just the target
                    RemoveMember(targetMember);
                }
                else
                {
                    foreach (MemberEntry member in _members.Values)
                        RemoveMember(member);
                }
            }

            // Finalize dissolve if needed
            if (MemberCount == 0)
            {
                GuildManager.RemoveGuild(this);
                DeleteFromDatabase();
                return GuildChangeMemberResultCode.eGCMRCSuccessGuildDissolved;
            }

            targetMember.SaveToDatabase();
            secondaryTargetMember?.SaveToDatabase();
            return GuildChangeMemberResultCode.eGCMRCSuccess;
        }

        public void OnMemberOnline(PlayerHandle player)
        {
            if (player == null)
                return;

            if (HasMember(player.PlayerDbId) == false)
                return;

            AddOnlineMember(player);
        }

        public void OnMemberOffline(PlayerHandle player)
        {
            if (player == null)
                return;

            if (HasMember(player.PlayerDbId) == false)
                return;

            RemoveOnlineMember(player);
        }

        public void OnMemberRegionChanged(PlayerHandle player, RegionHandle newRegion, RegionHandle prevRegion)
        {
            if (player == null)
            {
                Logger.Warn("OnMemberRegionChanged(): player == null");
                return;
            }

            if (player.Guild != this)
            {
                Logger.Warn($"OnMemberRegionChanged(): Player [{player}] is not in guild [{this}]");
                return;
            }

            if (newRegion != null)
                AddGame(newRegion.Game);

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

            GuildManager.SetGuildForPlayer(playerDbId, this);

            InvalidateGuildCompleteInfoCache();

            return member;
        }

        private bool CreateNewMember(PlayerHandle player, string initiatingMemberName)
        {
            if (player == null || player.State != PlayerHandleState.InGame)
                return false;

            DBGuildMember memberData = new((long)player.PlayerDbId, (long)Id, (long)GuildMembership.eGMMember);

            if (AddMember(memberData) is not MemberEntry member)
                return false;

            AddOnlineMember(player);

            // Replicate to games
            GuildMessageSetToServer serverMessage = GuildMessageSetToServer.CreateBuilder()
                .SetGuildMembersInfoChanged(GuildMembersInfoChanged.CreateBuilder()
                    .SetGuildId(Id)
                    .AddMembers(member.ToGuildMemberInfo())
                    .SetInitiatingMemberName(initiatingMemberName)
                    .SetNewMember(true))
                .Build();

            SendMessageToAllGames(serverMessage);

            // Replicate to database
            member.SaveToDatabase();

            return true;
        }

        private void RemoveMember(in MemberEntry member)
        {
            ulong playerDbId = member.PlayerDbId;

            _members.Remove(playerDbId);

            if (_onlineMembers.TryGetValue(playerDbId, out PlayerHandle onlinePlayer))
                RemoveOnlineMember(onlinePlayer);

            GuildManager.SetGuildForPlayer(playerDbId, null);

            InvalidateGuildCompleteInfoCache();
        }

        private MemberEntry? GetMember(ulong playerDbId)
        {
            if (_members.TryGetValue(playerDbId, out MemberEntry member) == false)
                return null;

            return member;
        }

        private MemberEntry? GetNextLeader(MemberEntry memberToIgnore)
        {
            MemberEntry? nextLeader = null;

            foreach (MemberEntry member in _members.Values)
            {
                if (member.Equals(memberToIgnore))
                    continue;

                // If we don't have any candidate yet, just pick whoever we have for now.
                if (nextLeader == null)
                {
                    nextLeader = member;
                    continue;
                }

                if (member.Membership != GuildMembership.eGMOfficer)
                    continue;

                // Prioritize officers over regular members
                if (nextLeader.Value.Membership == GuildMembership.eGMMember)
                    nextLeader = member;

                // Prioritize online member over offline
                if (_onlineMembers.ContainsKey(member.PlayerDbId) && _onlineMembers.ContainsKey(nextLeader.Value.PlayerDbId) == false)
                    nextLeader = member;
            }

            return nextLeader;
        }

        private void AddOnlineMember(PlayerHandle player)
        {
            _onlineMembers.Add(player.PlayerDbId, player);
            player.Guild = this;

            if (player.State == PlayerHandleState.InGame)
                AddGame(player.CurrentGame);
        }

        private void RemoveOnlineMember(PlayerHandle player)
        {
            player.Guild = null;
            _onlineMembers.Remove(player.PlayerDbId);

            if (player.State == PlayerHandleState.InGame)
                RemoveGame(player.CurrentGame);
        }

        private bool AddGame(GameHandle game)
        {
            if (game == null) return Logger.WarnReturn(false, "AddGame(): game == null");

            if (_games.Add(game) == false)
                return false;

            SendGuildCompleteInfo(game);
            return true;
        }

        private bool RemoveGame(GameHandle game)
        {
            if (game == null) return Logger.WarnReturn(false, "RemoveGame(): game == null");

            if (HasMembersInGame(game))
                return false;

            // NOTE: The game instance will remove its copy of the guild when all members leave the instance,
            // so we don't need to explicitly send anything here.
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

        private bool SendGuildCompleteInfo(GameHandle game)
        {
            if (game == null) return Logger.WarnReturn(false, "SendGuildCompleteInfo(): game == null");
            if (game.IsRunning == false) return Logger.WarnReturn(false, "SendGuildCompleteInfo(): game.IsRunning == false");

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

        private void SendMessageToAllGames(GuildMessageSetToServer serverMessage)
        {
            ServerManager serverManager = ServerManager.Instance;

            foreach (GameHandle game in _games)
            {
                ServiceMessage.GuildMessageToServer message = new(game.Id, serverMessage);
                serverManager.SendMessageToService(GameServiceType.GameInstance, message);
            }
        }

        private void InvalidateGuildCompleteInfoCache()
        {
            _guildCompleteInfoCache = null;
        }

        /// <summary>
        /// A wrapper for <see cref="DBGuildMember"/> for easier data access.
        /// </summary>
        private readonly struct MemberEntry(DBGuildMember data) : IEquatable<MemberEntry>
        {
            private readonly DBGuildMember _data = data;

            public ulong PlayerDbId { get => (ulong)_data.PlayerDbGuid; }
            public string PlayerName { get => GetPlayerName(); }
            public GuildMembership Membership { get => (GuildMembership)_data.Membership; }

            public bool CanChangeName { get => Membership >= GuildMembership.eGMLeader; }
            public bool CanChangeMotd { get => Membership >= GuildMembership.eGMOfficer; }
            public bool CanInvite { get => Membership >= GuildMembership.eGMOfficer; }

            public override string ToString()
            {
                return $"{PlayerName} (0x{PlayerDbId:X}) - {Membership}";
            }

            public override int GetHashCode()
            {
                return _data.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is not MemberEntry other)
                    return false;

                return Equals(other);
            }

            public bool Equals(MemberEntry other)
            {
                return _data.Equals(other._data);
            }

            public GuildMemberInfo ToGuildMemberInfo()
            {
                return GuildMemberInfo.CreateBuilder()
                    .SetPlayerId(PlayerDbId)
                    .SetPlayerName(PlayerName)
                    .SetMembership(Membership)
                    .Build();
            }

            public void SetMembership(GuildMembership newMembership)
            {
                _data.Membership = (long)newMembership;
            }

            public bool SaveToDatabase()
            {
                if (PersistenceEnabled == false)
                    return true;

                if (Membership == GuildMembership.eGMNone)
                    return IDBManager.Instance.DeleteGuildMember(_data);

                return IDBManager.Instance.SaveGuildMember(_data);
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
