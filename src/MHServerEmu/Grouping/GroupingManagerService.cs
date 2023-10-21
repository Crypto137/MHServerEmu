using Gazillion;
using MHServerEmu.Common.Commands;
using MHServerEmu.Common.Config;
using MHServerEmu.Common.Logging;
using MHServerEmu.Frontend.Accounts;
using MHServerEmu.Networking;

namespace MHServerEmu.Grouping
{
    public class GroupingManagerService : IGameMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private ServerManager _gameServerManager;

        public GroupingManagerService(ServerManager gameServerManager)
        {
            _gameServerManager = gameServerManager;
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageChat:
                    var chatMessageIn = NetMessageChat.ParseFrom(message.Payload);

                    if (CommandManager.TryParse(chatMessageIn.TheMessage.Body, client) == false)
                    {
                        // Limit broadcast and metagame channels to users with moderator privileges and higher
                        if ((chatMessageIn.RoomType == ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS || chatMessageIn.RoomType == ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                            && client.Session.Account.UserLevel < AccountUserLevel.Moderator)
                        {
                            client.SendMessage(1, new(NetMessageChatError.CreateBuilder().SetErrorMessage(ChatErrorMessages.CHAT_ERROR_COMMAND_NOT_RECOGNIZED).Build()));
                        }
                        else
                        {
                            Logger.Trace($"[{chatMessageIn.RoomType}] [{client.Session.Id} ({client.Session.Account.Email})]: {chatMessageIn.TheMessage.Body}");

                            var chatMessageOut = ChatNormalMessage.CreateBuilder()
                                .SetRoomType(chatMessageIn.RoomType)
                                .SetFromPlayerName(client.Session.Account.PlayerName)
                                .SetTheMessage(chatMessageIn.TheMessage)
                                .Build();

                            _gameServerManager.FrontendService.BroadcastMessage(2, new(chatMessageOut));
                        }
                    }

                    break;

                case ClientToGameServerMessage.NetMessageTell:
                    var tellMessage = NetMessageTell.ParseFrom(message.Payload);
                    Logger.Trace($"Received tell for {tellMessage.TargetPlayerName}");

                    // Respond with an error for now
                    client.SendMessage(2, new(ChatErrorMessage.CreateBuilder().SetErrorMessage(ChatErrorMessages.CHAT_ERROR_NO_SUCH_USER).Build()));
                    break;

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage[] messages)
        {
            foreach (GameMessage message in messages) Handle(client, muxId, message);
        }

        public void SendMotd(FrontendClient client)
        {
            client.SendMessage(2, new(ChatBroadcastMessage.CreateBuilder()
                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS)
                .SetFromPlayerName(ConfigManager.GroupingManager.MotdPlayerName)
                .SetTheMessage(ChatMessage.CreateBuilder().SetBody(ConfigManager.GroupingManager.MotdText))
                .SetPrestigeLevel(ConfigManager.GroupingManager.MotdPrestigeLevel)
                .Build()));
        }

        public static void SendMetagameChatMessage(FrontendClient client, string text)
        {
            client.SendMessage(2, new(ChatNormalMessage.CreateBuilder()
                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                .SetFromPlayerName(ConfigManager.GroupingManager.MotdPlayerName)
                .SetTheMessage(ChatMessage.CreateBuilder().SetBody(text))
                .SetPrestigeLevel(ConfigManager.GroupingManager.MotdPrestigeLevel)
                .Build()));
        }

        public static void SendMetagameChatMessages(FrontendClient client, IEnumerable<string> texts)
        {
            List<GameMessage> messageList = new();
            bool headerIsSet = false;               // Flag to add player name to the header (first) message

            foreach (string text in texts)
            {
                messageList.Add(new(ChatNormalMessage.CreateBuilder()
                    .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                    .SetFromPlayerName(headerIsSet ? string.Empty : ConfigManager.GroupingManager.MotdPlayerName)
                    .SetTheMessage(ChatMessage.CreateBuilder().SetBody(text))
                    .SetPrestigeLevel(ConfigManager.GroupingManager.MotdPrestigeLevel)
                    .Build()));

                headerIsSet = true;
            }

            client.SendMessages(2, messageList);
        }
    }
}
