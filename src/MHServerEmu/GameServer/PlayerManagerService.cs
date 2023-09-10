using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Logging;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer
{
    public class PlayerManagerService : IGameMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private GameServerManager _gameServerManager;

        public PlayerManagerService(GameServerManager gameServerManager)
        {
            _gameServerManager = gameServerManager;
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            IMessage response;
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageReadyForGameJoin:
                    // NetMessageReadyForGameJoin contains a bug where wipesDataIfMismatchedInDb is marked as required but the client doesn't include it
                    // To avoid an exception we build a partial message from the data we receive
                    Logger.Info($"Received NetMessageReadyForGameJoin");
                    var parsedReadyForGameJoin = NetMessageReadyForGameJoin.CreateBuilder().MergeFrom(message.Content).BuildPartial();
                    Logger.Trace(parsedReadyForGameJoin.ToString());

                    Logger.Info("Responding with NetMessageReadyAndLoggedIn");
                    client.SendMessage(muxId, new(NetMessageReadyAndLoggedIn.DefaultInstance)); // add report defect (bug) config here

                    Logger.Info("Responding with NetMessageInitialTimeSync");
                    response = NetMessageInitialTimeSync.CreateBuilder()
                        .SetGameTimeServerSent(161351679299542)     // dumped
                        .SetDateTimeServerSent(1509657957345525)    // dumped
                        .Build();
                    client.SendMessage(muxId, new(response));

                    break;

                case ClientToGameServerMessage.NetMessageSyncTimeRequest:
                    Logger.Info($"Received NetMessageSyncTimeRequest");
                    var parsedSyncTimeRequestMessage = NetMessageSyncTimeRequest.ParseFrom(message.Content);
                    Logger.Trace(parsedSyncTimeRequestMessage.ToString());

                    //Logger.Info("Responding with NetMessageSyncTimeReply");

                    response = NetMessageSyncTimeReply.CreateBuilder()
                        .SetGameTimeClientSent(parsedSyncTimeRequestMessage.GameTimeClientSent)
                        .SetGameTimeServerReceived(_gameServerManager.GetGameTime())
                        .SetGameTimeServerSent(_gameServerManager.GetGameTime())

                        .SetDateTimeClientSent(parsedSyncTimeRequestMessage.DateTimeClientSent)
                        .SetDateTimeServerReceived(_gameServerManager.GetDateTime())
                        .SetDateTimeServerSent(_gameServerManager.GetDateTime())

                        .SetDialation(0.0f)
                        .SetGametimeDialationStarted(_gameServerManager.GetGameTime())
                        .SetDatetimeDialationStarted(_gameServerManager.GetDateTime())
                        .Build();

                    //client.SendMessage(1, new(response));
                    break;

                case ClientToGameServerMessage.NetMessagePing:
                    Logger.Info($"Received NetMessagePing");
                    var parsedPingMessage = NetMessagePing.ParseFrom(message.Content);
                    //Logger.Trace(parsedPingMessage.ToString());
                    break;

                // Game messages
                case ClientToGameServerMessage.NetMessageUpdateAvatarState:
                case ClientToGameServerMessage.NetMessageCellLoaded:
                case ClientToGameServerMessage.NetMessageTryInventoryMove:
                case ClientToGameServerMessage.NetMessageSwitchAvatar:
                case ClientToGameServerMessage.NetMessageUseWaypoint:
                case ClientToGameServerMessage.NetMessageRequestInterestInAvatarEquipment:
                    _gameServerManager.GameManager.GetGameById(client.GameId).Handle(client, muxId, message);
                    break;

                // Grouping Manager messages
                case ClientToGameServerMessage.NetMessageChat:
                case ClientToGameServerMessage.NetMessageTell:
                case ClientToGameServerMessage.NetMessageReportPlayer:
                case ClientToGameServerMessage.NetMessageChatBanVote:
                    _gameServerManager.GroupingManagerService.Handle(client, muxId, message);
                    break;

                // Billing messages
                case ClientToGameServerMessage.NetMessageGetCatalog:
                case ClientToGameServerMessage.NetMessageGetCurrencyBalance:
                case ClientToGameServerMessage.NetMessageBuyItemFromCatalog:
                case ClientToGameServerMessage.NetMessageBuyGiftForOtherPlayer:
                case ClientToGameServerMessage.NetMessagePurchaseUnlock:
                case ClientToGameServerMessage.NetMessageGetGiftHistory:
                    _gameServerManager.BillingService.Handle(client, muxId, message);
                    break;

                case ClientToGameServerMessage.NetMessageGracefulDisconnect:
                    client.SendMuxDisconnect(1);
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
