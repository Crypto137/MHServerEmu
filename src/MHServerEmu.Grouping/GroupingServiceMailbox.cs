using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;

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
            // NOTE: There is a ClientToGroupingManager protocol with a single message - GetPlayerInfoByName.
            // It appears to be unused, but if we ever receive it, it should end up here as a ServiceMessage.RouteMessageBuffer.
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

                case ServiceMessage.GroupingManagerMetagameMessage groupingManagerMetagameMessage:
                    OnGroupingManagerMetagameMessage(groupingManagerMetagameMessage);
                    break;

                case ServiceMessage.GroupingManagerServerNotification groupingManagerServerNotification:
                    OnGroupingManagerServerNotification(groupingManagerServerNotification);
                    break;

                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {message.GetType().Name}");
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
            ulong playerDbId = groupingManagerChat.PlayerDbId;
            NetMessageChat chat = groupingManagerChat.Chat;
            int prestigeLevel = groupingManagerChat.PrestigeLevel;
            List<ulong> playerFilter = groupingManagerChat.PlayerFilter;

            if (_groupingManager.ClientManager.TryGetClient(playerDbId, out IFrontendClient client) == false)
            {
                Logger.Warn($"OnGroupingManagerChat(): Player 0x{playerDbId:X} not found");
                return;
            }

            _groupingManager.ChatManager.OnChat(client, chat, prestigeLevel, playerFilter);
        }

        private void OnGroupingManagerTell(in ServiceMessage.GroupingManagerTell groupingManagerTell)
        {
            ulong playerDbId = groupingManagerTell.PlayerDbId;
            NetMessageTell tell = groupingManagerTell.Tell;
            int prestigeLevel = groupingManagerTell.PrestigeLevel;

            if (_groupingManager.ClientManager.TryGetClient(playerDbId, out IFrontendClient client) == false)
            {
                Logger.Warn($"OnGroupingManagerTell(): Player 0x{playerDbId:X} not found");
                return;
            }

            _groupingManager.ChatManager.OnTell(client, tell, prestigeLevel);
        }

        private void OnGroupingManagerMetagameMessage(in ServiceMessage.GroupingManagerMetagameMessage groupingManagerMetagameMessage)
        {
            ulong playerDbId = groupingManagerMetagameMessage.PlayerDbId;
            string text = groupingManagerMetagameMessage.Text;
            bool showSender = groupingManagerMetagameMessage.ShowSender;

            if (_groupingManager.ClientManager.TryGetClient(playerDbId, out IFrontendClient client) == false)
            {
                Logger.Warn($"OnGroupingManagerMetagameMessage(): Player 0x{playerDbId:X} not found");
                return;
            }

            _groupingManager.ChatManager.OnMetagameMessage(client, text, showSender);
        }

        private void OnGroupingManagerServerNotification(in ServiceMessage.GroupingManagerServerNotification groupingManagerServerNotification)
        {
            string notificationText = groupingManagerServerNotification.NotificationText;

            _groupingManager.ChatManager.OnServerNotification(notificationText);
        }

        #endregion
    }
}
