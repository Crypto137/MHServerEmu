using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Social
{
    public class MasterGuildManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, MasterGuild> _guilds = new();
        private readonly Dictionary<ulong, MasterGuild> _guildsByMember = new();

        private readonly GuildNameRegistry _guildNameRegistry = new();

        private readonly PlayerManagerService _playerManager;

        private ulong _currentGuildId = 0;

        public MasterGuildManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        public void Initialize()
        {
            TimeSpan startTime = Clock.UnixTime;

            // We store all guilds in memory, so preload everything.
            List<DBGuild> dbGuilds = new();
            IDBManager.Instance.LoadGuilds(dbGuilds);

            int numMembers = 0;
            foreach (DBGuild dbGuild in dbGuilds)
            {
                if (dbGuild == null)
                {
                    Logger.Warn("Initialize(): dbGuild == null");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(dbGuild.Name))
                {
                    Logger.Warn($"Initialize(): Loaded guild with invalid name, id={dbGuild.Id}");
                    continue;
                }

                if (dbGuild.Members == null || dbGuild.Members.Count == 0)
                {
                    // Automatically clean up empty guilds.
                    Logger.Warn($"Initialize(): Loaded guild [{dbGuild}] has no members, requesting deletion");
                    IDBManager.Instance.DeleteGuild(dbGuild);
                    continue;
                }

                MasterGuild guild = CreateGuild(dbGuild, false);
                if (guild == null)
                {
                    Logger.Warn("Initialize(): guild == null");
                    continue;
                }

                _currentGuildId = Math.Max(_currentGuildId, guild.Id);
                numMembers += guild.MemberCount;
            }

            _guildNameRegistry.Initialize();

            TimeSpan elapsed = Clock.UnixTime - startTime;
            Logger.Info($"Initialized in {(long)elapsed.TotalMilliseconds} ms (guilds={_guilds.Count}, members={numMembers}, currentGuildId={_currentGuildId})");
        }

        public bool RemoveGuild(MasterGuild guild)
        {
            if (guild == null)
                return Logger.WarnReturn(false, "RemoveGuild(): guild == null");

            if (guild.MemberCount != 0)
                Logger.Warn("RemoveGuild(): guild.MemberCount != 0");

            if (_guilds.Remove(guild.Id) == false)
                return Logger.WarnReturn(false, $"RemoveGuild(): Guild {guild} not found");

            return true;
        }

        public MasterGuild GetGuild(ulong guildId)
        {
            if (_guilds.TryGetValue(guildId, out MasterGuild guild) == false)
                return null;

            return guild;
        }

        public MasterGuild GetGuildForPlayer(ulong playerDbId)
        {
            if (_guildsByMember.TryGetValue(playerDbId, out MasterGuild guild) == false)
                return null;

            return guild;
        }

        public void SetGuildForPlayer(ulong playerDbId, MasterGuild guild)
        {
            if (guild == null)
            {
                _guildsByMember.Remove(playerDbId);
                return;
            }

            _guildsByMember[playerDbId] = guild;
        }

        private MasterGuild CreateGuild(DBGuild data, bool saveToDatabase)
        {
            ulong guildId = (ulong)data.Id;
            string guildName = data.Name;

            if (_guilds.ContainsKey(guildId))
                return Logger.WarnReturn<MasterGuild>(null, $"CreateGuild(): Guild id {guildId} is already in use");

            if (_guildNameRegistry.AddGuildNameInUse(guildName) == false)
                return Logger.WarnReturn<MasterGuild>(null, $"CreateGuild(): Guild name {guildName} is already in use");

            MasterGuild guild = new(data, saveToDatabase);
            _guilds.Add(guild.Id, guild);
            return guild;
        }

        #region Message Handling

        public void OnGuildMessage(GuildMessageSetToPlayerManager messages)
        {
            if (messages.HasGuildForm)
                OnGuildForm(messages.GuildForm);

            if (messages.HasGuildChangeName)
                OnGuildChangeName(messages.GuildChangeName);

            if (messages.HasGuildInvite)
                OnGuildInvite(messages.GuildInvite);

            if (messages.HasGuildRespondToInvite)
                OnGuildRespondToInvite(messages.GuildRespondToInvite);

            if (messages.HasGuildChangeMember)
                OnGuildChangeMember(messages.GuildChangeMember);

            if (messages.HasGuildChangeMotd)
                OnGuildChangeMotd(messages.GuildChangeMotd);
        }

        private void OnGuildForm(GuildForm guildForm)
        {
            PlayerHandle player = _playerManager.ClientManager.GetPlayer(guildForm.PlayerId);
            if (player == null || player.State != PlayerHandleState.InGame)
                return;

            string guildName = guildForm.GuildName;
            ulong itemId = guildForm.ItemId;

            GuildFormResultCode result = player.Guild == null
                ? _guildNameRegistry.ValidateNameForGuildForm(guildName)
                : GuildFormResultCode.eGFCAlreadyInGuild;

            if (result == GuildFormResultCode.eGFCSuccess)
            {
                long guildId = (long)++_currentGuildId;
                long creatorDbGuid = (long)player.PlayerDbId;
                long creationTime = (long)Clock.UnixTime.TotalMilliseconds;

                DBGuild dbGuild = new(guildId, guildName, string.Empty, creatorDbGuid, creationTime);

                DBGuildMember creator = new(creatorDbGuid, guildId, (long)GuildMembership.eGMLeader);
                dbGuild.Members.Add(creator);

                if (CreateGuild(dbGuild, true) == null)
                    result = GuildFormResultCode.eGFCInternalError;
            }

            ulong gameId = player.CurrentGame.Id;

            var guildFormResult = GuildFormResult.CreateBuilder()
                .SetGuildName(guildName)
                .SetResultCode(result)
                .SetPlayerId(player.PlayerDbId);

            if (result == GuildFormResultCode.eGFCSuccess && itemId != 0)
                guildFormResult.SetItemId(itemId);

            GuildMessageSetToServer messages = GuildMessageSetToServer.CreateBuilder()
                .SetGuildFormResult(guildFormResult)
                .Build();

            ServiceMessage.GuildMessageToServer message = new(gameId, messages);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
        }

        private void OnGuildChangeName(GuildChangeName guildChangeName)
        {
            PlayerHandle player = _playerManager.ClientManager.GetPlayer(guildChangeName.PlayerId);
            if (player == null || player.State != PlayerHandleState.InGame)
                return;

            // This should have already been trimmed by the client and validated game-side on the server.
            string newGuildName = guildChangeName.GuildName;

            GuildChangeNameResultCode result = _guildNameRegistry.ValidateNameForGuildChangeName(newGuildName);

            if (result == GuildChangeNameResultCode.eGCNRCSuccess)
            {
                MasterGuild guild = player.Guild;

                if (guild != null)
                {
                    string oldGuildName = guild.Name;
                    result = guild.ChangeName(player, newGuildName);

                    if (result == GuildChangeNameResultCode.eGCNRCSuccess)
                    {
                        _guildNameRegistry.RemoveGuildNameInUse(oldGuildName);
                        _guildNameRegistry.AddGuildNameInUse(newGuildName);
                    }
                }
                else
                {
                    result = GuildChangeNameResultCode.eGCNRCNotInGuild;
                }
            }

            var clientMessage = GuildMessageSetToClient.CreateBuilder()
                .SetGuildChangeNameResult(GuildChangeNameResult.CreateBuilder()
                    .SetSubmittedName(newGuildName)
                    .SetResultCode(result))
                .Build();

            ServiceMessage.GuildMessageToClient message = new(player.CurrentGame.Id, player.PlayerDbId, clientMessage);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
        }

        private void OnGuildInvite(GuildInvite guildInvite)
        {
            PlayerHandle invitedByPlayer = _playerManager.ClientManager.GetPlayer(guildInvite.InvitedByPlayerId);
            if (invitedByPlayer == null || invitedByPlayer.State != PlayerHandleState.InGame)
                return;

            MasterGuild guild = invitedByPlayer.Guild;
            if (guild == null)
            {
                Logger.Warn($"OnGuildInvite(): Player [{invitedByPlayer}] is not in a guild");
                return;
            }

            // Cases where toInvitePlayer is null will be handled by InvitePlayer()
            PlayerHandle toInvitePlayer = guildInvite.HasToInvitePlayerId && guildInvite.ToInvitePlayerId != 0
                ? _playerManager.ClientManager.GetPlayer(guildInvite.ToInvitePlayerId)
                : _playerManager.ClientManager.GetPlayer(guildInvite.ToInvitePlayerName);

            GuildInviteResultCode result = guild.InvitePlayer(toInvitePlayer, invitedByPlayer);

            // Send a response to invitedByPlayer.
            if (toInvitePlayer != null)
            {
                // Make sure the GuildInvite has all the correct info (e.g. name case)
                guildInvite = GuildInvite.CreateBuilder()
                    .SetToInvitePlayerName(toInvitePlayer.PlayerName)
                    .SetToInvitePlayerId(toInvitePlayer.PlayerDbId)
                    .SetInvitedByPlayerId(invitedByPlayer.PlayerDbId)
                    .Build();
            }

            var clientMessage = GuildMessageSetToClient.CreateBuilder()
                .SetGuildInviteResult(GuildInviteResult.CreateBuilder()
                    .SetInvite(guildInvite)
                    .SetResultCode(result))
                .Build();

            ServiceMessage.GuildMessageToClient message = new(invitedByPlayer.CurrentGame.Id, invitedByPlayer.PlayerDbId, clientMessage);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
        }

        private void OnGuildRespondToInvite(GuildRespondToInvite guildRespondToInvite)
        {
            PlayerHandle player = _playerManager.ClientManager.GetPlayer(guildRespondToInvite.PlayerId);
            if (player == null || player.State != PlayerHandleState.InGame)
                return;

            GuildRespondToInviteCode respondCode = guildRespondToInvite.RespondCode;

            MasterGuild guild = GetGuild(guildRespondToInvite.GuildId);

            GuildRespondToInviteResultCode result = guild != null
                ? guild.ReceiveInviteResponse(player, respondCode)
                : GuildRespondToInviteResultCode.eGRIRCInvalidGuild;

            if (respondCode != GuildRespondToInviteCode.eGRICAutoIgnored)
            {
                var clientMessage = GuildMessageSetToClient.CreateBuilder()
                    .SetGuildRespondToInviteResult(GuildRespondToInviteResult.CreateBuilder()
                        .SetResultCode(result)
                        .SetGuildName(guild != null ? guild.Name : string.Empty))
                    .Build();

                ServiceMessage.GuildMessageToClient message = new(player.CurrentGame.Id, player.PlayerDbId, clientMessage);
                ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
            }
        }

        private void OnGuildChangeMember(GuildChangeMember guildChangeMember)
        {
            PlayerHandle sourcePlayer = _playerManager.ClientManager.GetPlayer(guildChangeMember.SourcePlayerId);
            if (sourcePlayer == null || sourcePlayer.State != PlayerHandleState.InGame)
                return;

            ulong targetPlayerId = guildChangeMember.TargetPlayerId;
            GuildMembership newMembership = guildChangeMember.TargetNewMembership;

            MasterGuild guild = sourcePlayer.Guild;

            GuildChangeMemberResultCode result = guild != null
                ? guild.ChangeMember(sourcePlayer, targetPlayerId, newMembership)
                : GuildChangeMemberResultCode.eGCMRCInitiatorNotInGuild;

            var clientMessage = GuildMessageSetToClient.CreateBuilder()
                .SetGuildChangeMemberResult(GuildChangeMemberResult.CreateBuilder()
                    .SetTargetPlayerName(guildChangeMember.TargetPlayerName)
                    .SetResultCode(result))
                .Build();

            ServiceMessage.GuildMessageToClient message = new(sourcePlayer.CurrentGame.Id, sourcePlayer.PlayerDbId, clientMessage);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
        }

        private void OnGuildChangeMotd(GuildChangeMotd guildChangeMotd)
        {
            PlayerHandle player = _playerManager.ClientManager.GetPlayer(guildChangeMotd.PlayerId);
            if (player == null || player.State != PlayerHandleState.InGame)
                return;

            string submittedMotd = guildChangeMotd.GuildMotd;

            MasterGuild guild = player.Guild;

            GuildChangeMotdResultCode result = guild != null
                ? guild.ChangeMotd(player, guildChangeMotd.GuildMotd)
                : GuildChangeMotdResultCode.eGCMotdRCNotInGuild;

            var clientMessage = GuildMessageSetToClient.CreateBuilder()
                .SetGuildChangeMotdResult(GuildChangeMotdResult.CreateBuilder()
                    .SetSubmittedMotd(submittedMotd)
                    .SetResultCode(result))
                .Build();

            ServiceMessage.GuildMessageToClient message = new(player.CurrentGame.Id, player.PlayerDbId, clientMessage);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
        }

        #endregion
    }
}
