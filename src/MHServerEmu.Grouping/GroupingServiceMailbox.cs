using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Grouping
{
    internal sealed class GroupingServiceMailbox : ServiceMailbox
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly GroupingManagerService _groupingManager;

        public GroupingServiceMailbox(GroupingManagerService groupingManager)
        {
            _groupingManager = groupingManager;
        }

        protected override void HandleServiceMessage(IGameServiceMessage message)
        {
            switch (message)
            {
                case ServiceMessage.PlayerNameChanged playerNameChanged:
                    OnPlayerNameChanged(playerNameChanged);
                    break;

                case ServiceMessage.GroupingManagerChat groupingManagerChat:
                    OnGroupingManagerChat(groupingManagerChat);
                    break;

                case ServiceMessage.GroupingManagerTell groupingManagerTell:
                    OnGroupingManagerTell(groupingManagerTell);
                    break;
            }
        }

        #region Handlers

        private bool OnPlayerNameChanged(in ServiceMessage.PlayerNameChanged playerNameChanged)
        {
            ulong playerDbId = playerNameChanged.PlayerDbId;
            string oldPlayerName = playerNameChanged.OldPlayerName;
            string newPlayerName = playerNameChanged.NewPlayerName;

            _groupingManager.ClientManager.OnPlayerNameChanged(playerDbId, oldPlayerName, newPlayerName);
            return true;
        }

        private bool OnGroupingManagerChat(in ServiceMessage.GroupingManagerChat groupingManagerChat)
        {
            DBAccount account = ((IDBAccountOwner)groupingManagerChat.Client).Account;
            NetMessageChat chat = groupingManagerChat.Chat;
            int prestigeLevel = groupingManagerChat.PrestigeLevel;
            List<ulong> playerFilter = groupingManagerChat.PlayerFilter;

            if (string.IsNullOrEmpty(chat.TheMessage.Body) == false)
                Logger.Info($"[{ChatHelper.GetRoomName(chat.RoomType)}] [{account})]: {chat.TheMessage.Body}", LogCategory.Chat);

            ChatNormalMessage message = ChatNormalMessage.CreateBuilder()
                .SetRoomType(chat.RoomType)
                .SetFromPlayerName(account.PlayerName)
                .SetTheMessage(chat.TheMessage)
                .SetPrestigeLevel(prestigeLevel)
                .Build();

            if (playerFilter != null)
                _groupingManager.ClientManager.SendMessageFiltered(message, playerFilter);
            else
                _groupingManager.ClientManager.SendMessageToAll(message);

            return true;
        }

        private bool OnGroupingManagerTell(in ServiceMessage.GroupingManagerTell groupingManagerTell)
        {
            IFrontendClient client = groupingManagerTell.Client;
            NetMessageTell tell = groupingManagerTell.Tell;

            Logger.Trace($"Received tell for {tell.TargetPlayerName}");

            // Respond with an error for now
            _groupingManager.ClientManager.SendMessage(ChatErrorMessage.CreateBuilder().SetErrorMessage(ChatErrorMessages.CHAT_ERROR_NO_SUCH_USER).Build(), client);

            return true;
        }

        #endregion
    }
}
