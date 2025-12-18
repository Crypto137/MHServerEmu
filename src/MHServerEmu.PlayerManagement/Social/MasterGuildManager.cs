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
                    Logger.Warn($"Initialize(): Loaded guild [{dbGuild}] has no members");
                    continue;
                }

                MasterGuild guild = new(dbGuild);

                ulong guildId = guild.Id;
                _guilds.Add(guildId, guild);
                _currentGuildId = Math.Max(_currentGuildId, guildId);

                numMembers += guild.MemberCount;
            }

            TimeSpan elapsed = Clock.UnixTime - startTime;
            Logger.Info($"Initialized in {(long)elapsed.TotalMilliseconds} ms (guilds={_guilds.Count}, members={numMembers}, currentGuildId={_currentGuildId})");
        }

        #region Message Handling

        public void OnGuildMessage(GuildMessageSetToPlayerManager messages)
        {
            Logger.Debug($"OnGuildMessage():\n{messages}");

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

            // REMOVEME: Send debug response for testing
            ulong gameId = player.CurrentGame.Id;
            List<ulong> playerDbIds = new() { player.PlayerDbId };

            GuildMessageSetToClient clientMessages = GuildMessageSetToClient.CreateBuilder()
                .SetGuildFormResult(GuildFormResult.CreateBuilder()
                    .SetGuildName(guildForm.GuildName)
                    .SetResultCode(GuildFormResultCode.eGFCRestrictedName)
                    .SetPlayerId(player.PlayerDbId))
                .Build();

            ServiceMessage.GuildMessageToGame message = new(gameId, playerDbIds, null, clientMessages);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
        }

        private void OnGuildChangeName(GuildChangeName guildChangeName)
        {

        }

        private void OnGuildInvite(GuildInvite guildInvite)
        {

        }

        private void OnGuildRespondToInvite(GuildRespondToInvite guildRespondToInvite)
        {

        }

        private void OnGuildChangeMember(GuildChangeMember guildChangeMember)
        {

        }

        private void OnGuildChangeMotd(GuildChangeMotd guildChangeMotd)
        {

        }

        #endregion
    }
}
