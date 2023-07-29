using Gazillion;
using MHServerEmu.Networking;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Data.Enums;

namespace MHServerEmu.GameServer
{
    public static class PowerLoader
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static GameMessage[] LoadAvatarPowerCollection(HardcodedAvatarEntity avatar)
        {
            List<GameMessage> messageList = new();

            switch (avatar)
            {
                case HardcodedAvatarEntity.BlackCat:
                    GameMessage[] loadedMessages = PacketHelper.LoadMessagesFromPacketFile("AvengersTowerFinishLoading.bin");

                    foreach (GameMessage gameMessage in loadedMessages)
                    {
                        switch(gameMessage.Id)
                        {
                            case (byte)GameServerToClientMessage.NetMessageAssignPowerCollection:
                                messageList.Add(gameMessage);
                                break;
                            case (byte)GameServerToClientMessage.NetMessagePowerCollectionAssignPower:
                                messageList.Add(gameMessage);
                                break;
                            case (byte)GameServerToClientMessage.NetMessagePowerCollectionUnassignPower:
                                messageList.Add(gameMessage);
                                break;
                            default: 
                                break;
                        }
                    }
                    break;
                case HardcodedAvatarEntity.Thor:
                    messageList.Add(new(GameServerToClientMessage.NetMessageAssignPowerCollection, NetMessageAssignPowerCollection.CreateBuilder()
                        .AddPower(NetMessagePowerCollectionAssignPower.CreateBuilder()
                          .SetEntityId((ulong)HardcodedAvatarEntity.Thor)
                          .SetPowerProtoId(4323239930633196258)
                          .SetPowerRank(1)
                          .SetCharacterLevel(60)
                          .SetCombatLevel(60)
                          .SetItemLevel(1)
                          .SetItemVariation(1))
                        .Build().ToByteArray()));
                    break;

            }

            return messageList.ToArray();
        }
    }
}
