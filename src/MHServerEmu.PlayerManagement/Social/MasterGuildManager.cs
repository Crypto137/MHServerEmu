using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Social
{
    public class MasterGuildManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly PlayerManagerService _playerManager;

        public MasterGuildManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
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
