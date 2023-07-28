using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Networking;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Data;
using MHServerEmu.GameServer.Data.Types;

namespace MHServerEmu.GameServer
{
    public enum GameRegion
    {
        AvengersTower,
        DangerRoom,
        Midtown
    }

    public static class RegionLoader
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static GameMessage[] GetBeginLoadingMessages(GameRegion region, HardcodedAvatarEntity avatar)
        {
            GameMessage[] messages = Array.Empty<GameMessage>(); ;

            switch (region)
            {
                case GameRegion.AvengersTower:
                    GameMessage[] loadedMessages = PacketHelper.LoadMessagesFromPacketFile("AvengersTowerBeginLoading.bin");
                    GameMessage[] hardcodedAvatarEntityCreateMessages = PacketHelper.LoadMessagesFromPacketFile("HardcodedAvatarEntityCreateMessages.bin");

                    List<GameMessage> messageList = new();

                    for (int i = 0; i < loadedMessages.Length; i++)
                    {
                        if (loadedMessages[i].Id == (byte)GameServerToClientMessage.NetMessageEntityCreate)
                        {
                            if (i == 5)     // player data
                            {
                                messageList.Add(loadedMessages[i]);
                                ulong replacementInventorySlot = 0;

                                foreach (GameMessage avatarEntityCreateMessage in hardcodedAvatarEntityCreateMessages)
                                {
                                    var entityCreateMessage = NetMessageEntityCreate.ParseFrom(avatarEntityCreateMessage.Content);
                                    EntityCreateBaseData baseData = new(entityCreateMessage.BaseData.ToByteArray());

                                    // Modify base data if loading any hero other than Black Cat
                                    if (avatar != HardcodedAvatarEntity.BlackCat)
                                    {
                                        if (baseData.EntityId == (ulong)avatar)
                                        {
                                            replacementInventorySlot = baseData.DynamicFields[5];
                                            baseData.DynamicFields[4] = 273;    // put selected avatar in PlayerAvatarInPlay
                                            baseData.DynamicFields[5] = 0;      // set avatar entity inventory slot to 0

                                            var modifiedEntityCreateMessage = NetMessageEntityCreate.CreateBuilder()
                                                .SetBaseData(ByteString.CopyFrom(baseData.Encode()))
                                                .SetArchiveData(entityCreateMessage.ArchiveData)
                                                .Build().ToByteArray();

                                            messageList.Add(new((byte)GameServerToClientMessage.NetMessageEntityCreate, modifiedEntityCreateMessage));
                                        }
                                        else if (baseData.EntityId == (ulong)HardcodedAvatarEntity.BlackCat)
                                        {
                                            baseData.DynamicFields[4] = 169;                        // put Black Cat in PlayerAvatarLibrary 
                                            baseData.DynamicFields[5] = replacementInventorySlot;   // set Black Cat slot to the one previously occupied by the hero who replaces her

                                            // Black Cat goes last in the hardcoded messages, so this should always be assigned last
                                            if (replacementInventorySlot == 0) Logger.Warn("replacementInventorySlot is 0! Check the hardcoded avatar entity data");

                                            var modifiedEntityCreateMessage = NetMessageEntityCreate.CreateBuilder()
                                                .SetBaseData(ByteString.CopyFrom(baseData.Encode()))
                                                .SetArchiveData(entityCreateMessage.ArchiveData)
                                                .Build().ToByteArray();

                                            messageList.Add(new((byte)GameServerToClientMessage.NetMessageEntityCreate, modifiedEntityCreateMessage));
                                        }
                                    }
                                    else
                                    {
                                        messageList.Add(avatarEntityCreateMessage);
                                    }
                                }
                            }
                            else if (i == 364)  // waypoint
                            {
                                messageList.Add(loadedMessages[i]);
                            }

                            /* Old debug stuff
                            Logger.Debug(baseData.ToString());
                            //EntityCreateArchiveData archiveData = new(entityCreateMessage.ArchiveData.ToByteArray());
                            //File.WriteAllText($"{Directory.GetCurrentDirectory()}\\{i}_entityCreateArchiveData.txt", archiveData.ToString());

                            if (i == 115) Logger.Debug(entityCreateMessage.ArchiveData.ToByteArray().ToHexString());

                            // try to get prototype id from enum
                            if ((int)baseData.EnumValue < Database.PrototypeTable.Length)
                            {
                                Logger.Debug($"enum {baseData.EnumValue} == prototype {Database.PrototypeTable[baseData.EnumValue]}");
                            }
                            else
                            {
                                Logger.Debug($"enum value {baseData.EnumValue} out of range (database table length: {Database.PrototypeTable.Length})");
                            }

                            Console.WriteLine();
                            */
                        }
                        else
                        {
                            messageList.Add(loadedMessages[i]);
                        }
                    }

                    messages = messageList.ToArray();
                    break;

                case GameRegion.DangerRoom:
                    messages = PacketHelper.LoadMessagesFromPacketFile("DangerRoomBeginLoading.bin");
                    break;

                case GameRegion.Midtown:
                    messages = PacketHelper.LoadMessagesFromPacketFile("MidtownBeginLoading.bin");
                    break;
            }

            return messages;
        }

        public static GameMessage[] GetFinishLoadingMessages(GameRegion region, HardcodedAvatarEntity avatar)
        {
            GameMessage[] messages = Array.Empty<GameMessage>(); ;

            switch (region)
            {
                case GameRegion.AvengersTower:

                    List<GameMessage> messageList = new();

                    // Put player avatar entity in the game world
                    byte[] avatarEntityEnterGameWorldArchiveData = {
                        0x01, 0xB2, 0xF8, 0xFD, 0x06, 0xA0, 0x21, 0xF0, 0xA3, 0x01, 0xBC, 0x40,
                        0x90, 0x2E, 0x91, 0x03, 0xBC, 0x05, 0x00, 0x00, 0x01
                    };

                    EntityEnterGameWorldArchiveData avatarEnterArchiveData = new(avatarEntityEnterGameWorldArchiveData);
                    avatarEnterArchiveData.EntityId = (ulong)avatar;

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageEntityEnterGameWorld,
                        NetMessageEntityEnterGameWorld.CreateBuilder()
                        .SetArchiveData(ByteString.CopyFrom(avatarEnterArchiveData.Encode()))
                        .Build().ToByteArray()));

                    // Put waypoint entity in the game world
                    byte[] waypointEntityEnterGameWorld = {
                        0x01, 0x0C, 0x02, 0x80, 0x43, 0xE0, 0x6B, 0xD8, 0x2A, 0xC8, 0x01
                    };

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageEntityEnterGameWorld,
                        NetMessageEntityEnterGameWorld.CreateBuilder().SetArchiveData(ByteString.CopyFrom(waypointEntityEnterGameWorld)).Build().ToByteArray()));

                    // Dequeue loading screen
                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageDequeueLoadingScreen, NetMessageDequeueLoadingScreen.DefaultInstance.ToByteArray()));

                    messages = messageList.ToArray();

                    break;

                case GameRegion.DangerRoom:
                    messages = PacketHelper.LoadMessagesFromPacketFile("DangerRoomFinishLoading.bin");
                    break;

                case GameRegion.Midtown:
                    messages = PacketHelper.LoadMessagesFromPacketFile("MidtownFinishLoading.bin");
                    break;
            }

            return messages;
        }
    }
}
