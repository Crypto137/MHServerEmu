using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Entities.Archives;
using MHServerEmu.GameServer.Powers;
using MHServerEmu.GameServer.Regions;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer.GameInstances
{
    public class GameInstanceService : IGameMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private GameServerManager _gameServerManager;

        public GameInstanceService(GameServerManager gameServerManager)
        {
            _gameServerManager = gameServerManager;
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage[] messages)
        {
            foreach (GameMessage message in messages)
            {
                byte[] response;
                switch ((ClientToGameServerMessage)message.Id)
                {
                    case ClientToGameServerMessage.NetMessageReadyForGameJoin:

                        Logger.Info($"Received NetMessageReadyForGameJoin");
                        try
                        {
                            var parsedReadyForGameJoin = NetMessageReadyForGameJoin.ParseFrom(message.Content);
                            Logger.Trace(parsedReadyForGameJoin.ToString());
                        }
                        catch (InvalidProtocolBufferException e)
                        {
                            Logger.Warn($"Failed to parse NetMessageReadyForGameJoin: {e.Message}");
                        }

                        Logger.Info("Responding with NetMessageReadyAndLoggedIn");
                        response = NetMessageReadyAndLoggedIn.CreateBuilder()
                            .Build().ToByteArray();
                        client.SendMessage(muxId, new(GameServerToClientMessage.NetMessageReadyAndLoggedIn, response));

                        Logger.Info("Responding with NetMessageInitialTimeSync");
                        response = NetMessageInitialTimeSync.CreateBuilder()
                            .SetGameTimeServerSent(161351679299542)     // dumped
                            .SetDateTimeServerSent(1509657957345525)    // dumped
                            .Build().ToByteArray();
                        client.SendMessage(muxId, new(GameServerToClientMessage.NetMessageInitialTimeSync, response));

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
                            .Build().ToByteArray();

                        //client.SendMessage(1, new(GameServerToClientMessage.NetMessageSyncTimeReply, response));
                        break;

                    case ClientToGameServerMessage.NetMessagePing:
                        Logger.Info($"Received NetMessagePing");
                        var parsedPingMessage = NetMessagePing.ParseFrom(message.Content);
                        //Logger.Trace(parsedPingMessage.ToString());
                        break;

                    case ClientToGameServerMessage.NetMessageCellLoaded:
                        Logger.Info($"Received NetMessageCellLoaded");
                        if (client.WaypointRegion is not null && client.ReloadEntities)
                        {
                            client.SendMultipleMessages(1, RegionLoader.GetWaypointRegionChangeFinishLoadingMessages(client.WaypointRegion, client.StartingAvatar));
                            client.SendMultipleMessages(1, PowerLoader.LoadAvatarPowerCollection(client.StartingAvatar));
                            client.ReloadEntities = false;
                        }
                        else if (client.WaypointRegion is null)
                        {
                            client.SendMultipleMessages(1, RegionLoader.GetFinishLoadingMessages(client.StartingRegion, client.StartingAvatar));
                        }
                        break;

                    case ClientToGameServerMessage.NetMessageTryInventoryMove:
                        Logger.Info($"Received NetMessageTryInventoryMove");
                        var tryInventoryMoveMessage = NetMessageTryInventoryMove.ParseFrom(message.Content);
                        var inventoryMoveMessage = NetMessageInventoryMove.CreateBuilder()
                            .SetEntityId(tryInventoryMoveMessage.ItemId)
                            .SetInvLocContainerEntityId(tryInventoryMoveMessage.ToInventoryOwnerId)
                            .SetInvLocInventoryPrototypeId(tryInventoryMoveMessage.ToInventoryPrototype)
                            .SetInvLocSlot(tryInventoryMoveMessage.ToSlot)
                            .Build().ToByteArray();

                        client.SendMessage(1, new(GameServerToClientMessage.NetMessageInventoryMove, inventoryMoveMessage));
                        break;

                    case ClientToGameServerMessage.NetMessageUseWaypoint:
                        Logger.Info($"Received NetMessageUseWaypoint message");
                        var useWaypointMessage = NetMessageUseWaypoint.ParseFrom(message.Content);

                        Logger.Trace(useWaypointMessage.ToString());

                        switch ((RegionPrototype)useWaypointMessage.RegionProtoId)
                        {
                            case RegionPrototype.NPEAvengersTowerHUBRegion:
                                client.WaypointRegion = RegionPrototype.NPEAvengersTowerHUBRegion;
                                client.ReloadEntities = true;
                                client.SendMultipleMessages(1, RegionLoader.GetWaypointRegionChangeMessages(RegionPrototype.NPEAvengersTowerHUBRegion));
                                break;
                            case RegionPrototype.DangerRoomHubRegion:
                                client.WaypointRegion = RegionPrototype.DangerRoomHubRegion;
                                client.ReloadEntities = true;
                                client.SendMultipleMessages(1, RegionLoader.GetWaypointRegionChangeMessages(RegionPrototype.DangerRoomHubRegion));
                                break;
                            case RegionPrototype.XManhattanRegion60Cosmic:
                                client.WaypointRegion = RegionPrototype.XManhattanRegion60Cosmic;
                                client.ReloadEntities = true;
                                client.SendMultipleMessages(1, RegionLoader.GetWaypointRegionChangeMessages(RegionPrototype.XManhattanRegion60Cosmic));
                                break;

                        }

                        break;

                    case ClientToGameServerMessage.NetMessageSwitchAvatar:
                        Logger.Info($"Received NetMessageSwitchAvatar");
                        var switchAvatarMessage = NetMessageSwitchAvatar.ParseFrom(message.Content);
                        Logger.Trace(switchAvatarMessage.ToString());

                        /* WIP - Hardcoded Black Cat -> Thor -> requires triggering an avatar swap back to Black Cat to move Thor again  */
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
                        
                        client.SendMultipleMessages(1, messageList.ToArray());

                        break;

                    case ClientToGameServerMessage.NetMessageGetCatalog:
                        Logger.Info($"Received NetMessageGetCatalog");
                        var dumpedCatalog = NetMessageCatalogItems.ParseFrom(PacketHelper.LoadMessagesFromPacketFile("NetMessageCatalogItems.bin")[0].Content);

                        byte[] catalog = NetMessageCatalogItems.CreateBuilder()
                            .MergeFrom(dumpedCatalog)
                            .SetTimestampSeconds(_gameServerManager.GetDateTime() / 1000000)
                            .SetTimestampMicroseconds(_gameServerManager.GetDateTime())
                            .SetClientmustdownloadimages(false)
                            .Build().ToByteArray();

                        client.SendMessage(1, new(GameServerToClientMessage.NetMessageCatalogItems, catalog));
                        break;

                    case ClientToGameServerMessage.NetMessageUpdateAvatarState:
                        Logger.Info($"Received NetMessageUpdateAvatarState");
                        var updateAvatarState = NetMessageUpdateAvatarState.ParseFrom(message.Content);
                        break;

                    case ClientToGameServerMessage.NetMessageRequestInterestInAvatarEquipment:
                        Logger.Info($"Received NetMessageRequestInterestInAvatarEquipment");
                        var requestInterestInAvatarEquipment = NetMessageRequestInterestInAvatarEquipment.ParseFrom(message.Content);
                        break;

                    default:
                        Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                        break;
                }
            }
        }
    }
}
