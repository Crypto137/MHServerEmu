using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Config;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Entities.Avatars;
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

                case ClientToGameServerMessage.NetMessageTryInventoryMove:
                    Logger.Info($"Received NetMessageTryInventoryMove");
                    var tryInventoryMoveMessage = NetMessageTryInventoryMove.ParseFrom(message.Content);
                    var inventoryMoveMessage = NetMessageInventoryMove.CreateBuilder()
                        .SetEntityId(tryInventoryMoveMessage.ItemId)
                        .SetInvLocContainerEntityId(tryInventoryMoveMessage.ToInventoryOwnerId)
                        .SetInvLocInventoryPrototypeId(tryInventoryMoveMessage.ToInventoryPrototype)
                        .SetInvLocSlot(tryInventoryMoveMessage.ToSlot)
                        .Build();

                    client.SendMessage(1, new(inventoryMoveMessage));
                    break;

                case ClientToGameServerMessage.NetMessageSwitchAvatar:
                    Logger.Info($"Received NetMessageSwitchAvatar");
                    var switchAvatarMessage = NetMessageSwitchAvatar.ParseFrom(message.Content);
                    Logger.Trace(switchAvatarMessage.ToString());

                    // A hack for changing starting avatar without using chat commands
                    if (ConfigManager.Frontend.BypassAuth == false)
                    {
                        string avatarName = Enum.GetName(typeof(AvatarPrototype), switchAvatarMessage.AvatarPrototypeId);

                        if (Enum.TryParse(typeof(HardcodedAvatarEntity), avatarName, true, out object avatar))
                        {
                            client.Session.Account.PlayerData.Avatar = (HardcodedAvatarEntity)avatar;

                            var chatMessage = ChatNormalMessage.CreateBuilder()
                                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                                .SetFromPlayerName(ConfigManager.GroupingManager.MotdPlayerName)
                                .SetTheMessage(ChatMessage.CreateBuilder().SetBody($"Changing avatar to {client.Session.Account.PlayerData.Avatar}. Relog for changes to take effect."))
                                .SetPrestigeLevel(6)
                                .Build();

                            client.SendMessage(2, new(chatMessage));
                        }
                    }

                    /* WIP - Hardcoded Black Cat -> Thor -> requires triggering an avatar swap back to Black Cat to move Thor again  
                    List<GameMessage> messageList = new();
                    messageList.Add(new(GameServerToClientMessage.NetMessageInventoryMove, NetMessageInventoryMove.CreateBuilder()
                        .SetEntityId((ulong)HardcodedAvatarEntity.Thor)
                        .SetDestOwnerDataId((ulong)HardcodedAvatarEntity.Thor)
                        .SetInvLocContainerEntityId(14646212)
                        .SetInvLocInventoryPrototypeId(9555311166682372646)
                        .SetInvLocSlot(0)
                        .Build().ToByteArray()));

                    // Put player avatar entity in the game world
                    byte[] avatarEntityEnterGameWorldArchiveData = {
                        0x01, 0xB2, 0xF8, 0xFD, 0x06, 0xA0, 0x21, 0xF0, 0xA3, 0x01, 0xBC, 0x40,
                        0x90, 0x2E, 0x91, 0x03, 0xBC, 0x05, 0x00, 0x00, 0x01
                    };

                    EntityEnterGameWorldArchiveData avatarEnterArchiveData = new(avatarEntityEnterGameWorldArchiveData);
                    avatarEnterArchiveData.EntityId = (ulong)HardcodedAvatarEntity.Thor;

                    messageList.Add(new(GameServerToClientMessage.NetMessageEntityEnterGameWorld,
                        NetMessageEntityEnterGameWorld.CreateBuilder()
                        .SetArchiveData(ByteString.CopyFrom(avatarEnterArchiveData.Encode()))
                        .Build().ToByteArray()));

                    client.SendMultipleMessages(1, messageList.ToArray());*/

                    break;

                case ClientToGameServerMessage.NetMessageUpdateAvatarState:
                    //Logger.Trace($"Received NetMessageUpdateAvatarState");
                    var updateAvatarState = NetMessageUpdateAvatarState.ParseFrom(message.Content);
                    break;

                case ClientToGameServerMessage.NetMessageRequestInterestInAvatarEquipment:
                    Logger.Info($"Received NetMessageRequestInterestInAvatarEquipment");
                    var requestInterestInAvatarEquipment = NetMessageRequestInterestInAvatarEquipment.ParseFrom(message.Content);
                    break;

                case ClientToGameServerMessage.NetMessageCellLoaded:
                case ClientToGameServerMessage.NetMessageUseWaypoint:
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
