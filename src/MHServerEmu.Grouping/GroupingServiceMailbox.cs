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

        // Try to keep these handlers free from logic and just route the requests to appropriate managers.

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
            IFrontendClient client = groupingManagerChat.Client;
            NetMessageChat chat = groupingManagerChat.Chat;
            int prestigeLevel = groupingManagerChat.PrestigeLevel;
            List<ulong> playerFilter = groupingManagerChat.PlayerFilter;

            _groupingManager.ChatManager.OnChat(client, chat, prestigeLevel, playerFilter);
            return true;
        }

        private bool OnGroupingManagerTell(in ServiceMessage.GroupingManagerTell groupingManagerTell)
        {
            IFrontendClient client = groupingManagerTell.Client;
            NetMessageTell tell = groupingManagerTell.Tell;

            _groupingManager.ChatManager.OnTell(client, tell);
            return true;
        }

        #endregion
    }
}
