using Gazillion;
using MHServerEmu.Common;
using MHServerEmu.Common.Commands;
using MHServerEmu.Common.Config;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer
{
    public class GroupingManagerService : IGameMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private GameServerManager _gameServerManager;

        public GroupingManagerService(GameServerManager gameServerManager)
        {
            _gameServerManager = gameServerManager;
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageChat:
                    var chatMessageIn = NetMessageChat.ParseFrom(message.Content);

                    if (CommandManager.TryParse(chatMessageIn.TheMessage.Body, client) == false)
                    {
                        // Limit broadcast and metagame channels to users with moderator privileges and higher
                        if ((chatMessageIn.RoomType == ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS || chatMessageIn.RoomType == ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                            && client.Session.Account.UserLevel < Frontend.Accounts.AccountUserLevel.Moderator)
                        {
                            client.SendMessage(1, new(NetMessageChatError.CreateBuilder().SetErrorMessage(ChatErrorMessages.CHAT_ERROR_COMMAND_NOT_RECOGNIZED).Build()));
                        }
                        else
                        {
                            Logger.Trace($"[{chatMessageIn.RoomType}] [{client.Session.Id} ({client.Session.Account.Email})]: {chatMessageIn.TheMessage.Body}");

                            var chatMessageOut = ChatNormalMessage.CreateBuilder()
                                .SetRoomType(chatMessageIn.RoomType)
                                .SetFromPlayerName(client.Session.Account.PlayerData.PlayerName)
                                .SetTheMessage(chatMessageIn.TheMessage)
                                .Build();

                            _gameServerManager.FrontendService.BroadcastMessage(2, new(chatMessageOut));
                        }
                    }

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
    }
}
