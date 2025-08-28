using Gazillion;
using MHServerEmu.Core.Network;

namespace MHServerEmu.Grouping
{
    internal sealed class GroupingServiceMailbox : ServiceMailbox
    {
        private readonly GroupingManagerService _groupingManager;

        public GroupingServiceMailbox(GroupingManagerService groupingManager)
        {
            _groupingManager = groupingManager;
        }

        protected override void HandleServiceMessage(IGameServiceMessage message)
        {
            switch (message)
            {
                case ServiceMessage.AddClient addClient:
                    OnAddClient(addClient);
                    break;

                case ServiceMessage.RemoveClient removeClient:
                    OnRemoveClient(removeClient);
                    break;

                case ServiceMessage.PlayerNameChanged playerNameChanged:
                    OnPlayerNameChanged(playerNameChanged);
                    break;

                case ServiceMessage.GroupingManagerChat groupingManagerChat:
                    OnGroupingManagerChat(groupingManagerChat);
                    break;

                case ServiceMessage.GroupingManagerTell groupingManagerTell:
                    OnGroupingManagerTell(groupingManagerTell);
                    break;

                case ServiceMessage.GroupingManagerServerNotification groupingManagerServerNotification:
                    OnGroupingManagerServerNotification(groupingManagerServerNotification);
                    break;
            }
        }

        #region Handlers

        // Try to keep these handlers free from logic and just route the requests to appropriate managers.

        private void OnAddClient(in ServiceMessage.AddClient addClient)
        {
            IFrontendClient client = addClient.Client;

            if (_groupingManager.ClientManager.AddClient(client) == false)
                return;

            _groupingManager.ChatManager.OnClientAdded(client);
        }

        private void OnRemoveClient(in ServiceMessage.RemoveClient removeClient)
        {
            IFrontendClient client = removeClient.Client;
            _groupingManager.ClientManager.RemoveClient(client);
        }

        private void OnPlayerNameChanged(in ServiceMessage.PlayerNameChanged playerNameChanged)
        {
            ulong playerDbId = playerNameChanged.PlayerDbId;
            string oldPlayerName = playerNameChanged.OldPlayerName;
            string newPlayerName = playerNameChanged.NewPlayerName;

            _groupingManager.ClientManager.OnPlayerNameChanged(playerDbId, oldPlayerName, newPlayerName);
        }

        private void OnGroupingManagerChat(in ServiceMessage.GroupingManagerChat groupingManagerChat)
        {
            IFrontendClient client = groupingManagerChat.Client;
            NetMessageChat chat = groupingManagerChat.Chat;
            int prestigeLevel = groupingManagerChat.PrestigeLevel;
            List<ulong> playerFilter = groupingManagerChat.PlayerFilter;

            _groupingManager.ChatManager.OnChat(client, chat, prestigeLevel, playerFilter);
        }

        private void OnGroupingManagerTell(in ServiceMessage.GroupingManagerTell groupingManagerTell)
        {
            IFrontendClient client = groupingManagerTell.Client;
            NetMessageTell tell = groupingManagerTell.Tell;
            int prestigeLevel = groupingManagerTell.PrestigeLevel;

            _groupingManager.ChatManager.OnTell(client, tell, prestigeLevel);
        }

        private void OnGroupingManagerServerNotification(in ServiceMessage.GroupingManagerServerNotification groupingManagerServerNotification)
        {
            string notificationText = groupingManagerServerNotification.NotificationText;

            _groupingManager.ChatManager.OnServerNotification(notificationText);
        }

        #endregion
    }
}
