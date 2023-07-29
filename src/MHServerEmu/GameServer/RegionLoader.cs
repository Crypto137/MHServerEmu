using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Networking;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Data.Types;
using MHServerEmu.GameServer.Data.Enums;

namespace MHServerEmu.GameServer
{
    public static class RegionLoader
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static GameMessage[] GetBeginLoadingMessages(RegionPrototype? region, HardcodedAvatarEntity avatar)
        {
            GameMessage[] messages = Array.Empty<GameMessage>(); ;

            switch (region)
            {
                case RegionPrototype.AvengersTower:

                    List<GameMessage> messageList = new();

                    // Add server info messages
                    messageList.Add(new(GameServerToClientMessage.NetMessageMarkFirstGameFrame, NetMessageMarkFirstGameFrame.CreateBuilder()
                        .SetCurrentservergametime(161351682950)
                        .SetCurrentservergameid(1150669705055451881)
                        .SetGamestarttime(1)
                        .Build().ToByteArray()));

                    messageList.Add(new(GameServerToClientMessage.NetMessageServerVersion, NetMessageServerVersion.CreateBuilder()
                        .SetVersion("1.52.0.1700")
                        .Build().ToByteArray()));

                    messageList.Add(PacketHelper.LoadMessagesFromPacketFile("NetMessageLiveTuningUpdate.bin")[0]);
                    messageList.Add(new(GameServerToClientMessage.NetMessageReadyForTimeSync, NetMessageReadyForTimeSync.DefaultInstance.ToByteArray()));

                    // Load local player data
                    foreach (GameMessage message in LoadLocalPlayerDataMessages(avatar)) messageList.Add(message);
                    messageList.Add(new(GameServerToClientMessage.NetMessageReadyAndLoadedOnGameServer, NetMessageReadyAndLoadedOnGameServer.DefaultInstance.ToByteArray()));

                    // Load region data
                    foreach (GameMessage message in LoadRegionTransitionMessages(region)) messageList.Add(message);

                    // Create waypoint entity
                    byte[] waypointEntityCreateBaseData = {
                        0x20, 0x0C, 0x83, 0x9F, 0x01, 0x20, 0x00, 0x20
                    };

                    byte[] waypointEntityCreateArchiveData = {
                        0x20, 0xF4, 0xC1, 0x02, 0x06, 0x00, 0x00, 0x00, 0xCD, 0x80, 0x01, 0x88,
                        0x80, 0xFC, 0xFF, 0x99, 0xBF, 0x96, 0x81, 0x10, 0xCC, 0xC0, 0x02, 0x02,
                        0xCC, 0x80, 0x03, 0x02, 0xCD, 0x40, 0xD5, 0x82, 0x80, 0xDE, 0x86, 0x80,
                        0x98, 0x04, 0x4D, 0xA1, 0xA1, 0xA4, 0xFE, 0x03, 0x99, 0xC0, 0x01, 0x83,
                        0xB8, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00
                    };

                    messageList.Add(new(GameServerToClientMessage.NetMessageEntityCreate, NetMessageEntityCreate.CreateBuilder()
                        .SetBaseData(ByteString.CopyFrom(waypointEntityCreateBaseData))
                        .SetArchiveData(ByteString.CopyFrom(waypointEntityCreateArchiveData))
                        .Build().ToByteArray()));

                    messages = messageList.ToArray();

                    break;

                case RegionPrototype.DangerRoom:
                    messages = PacketHelper.LoadMessagesFromPacketFile("DangerRoomBeginLoading.bin");
                    break;

                case RegionPrototype.MidtownPatrolCosmic:
                    messages = PacketHelper.LoadMessagesFromPacketFile("MidtownBeginLoading.bin");
                    break;
            }

            return messages;
        }

        public static GameMessage[] GetFinishLoadingMessages(RegionPrototype? region, HardcodedAvatarEntity avatar)
        {
            GameMessage[] messages = Array.Empty<GameMessage>();
            switch (region)
            {
                case RegionPrototype.AvengersTower:

                    if (avatar == HardcodedAvatarEntity.BlackCat)
                    {
                        messages = PacketHelper.LoadMessagesFromPacketFile("AvengersTowerFinishLoading.bin");
                    }
                    else
                    {
                        List<GameMessage> messageList = new();

                        // Put player avatar entity in the game world
                        byte[] avatarEntityEnterGameWorldArchiveData = {
                            0x01, 0xB2, 0xF8, 0xFD, 0x06, 0xA0, 0x21, 0xF0, 0xA3, 0x01, 0xBC, 0x40,
                            0x90, 0x2E, 0x91, 0x03, 0xBC, 0x05, 0x00, 0x00, 0x01
                        };

                        EntityEnterGameWorldArchiveData avatarEnterArchiveData = new(avatarEntityEnterGameWorldArchiveData);
                        avatarEnterArchiveData.EntityId = (ulong)avatar;

                        messageList.Add(new(GameServerToClientMessage.NetMessageEntityEnterGameWorld,
                            NetMessageEntityEnterGameWorld.CreateBuilder()
                            .SetArchiveData(ByteString.CopyFrom(avatarEnterArchiveData.Encode()))
                            .Build().ToByteArray()));

                        // Put waypoint entity in the game world
                        byte[] waypointEntityEnterGameWorld = {
                            0x01, 0x0C, 0x02, 0x80, 0x43, 0xE0, 0x6B, 0xD8, 0x2A, 0xC8, 0x01
                        };

                        messageList.Add(new(GameServerToClientMessage.NetMessageEntityEnterGameWorld,
                            NetMessageEntityEnterGameWorld.CreateBuilder().SetArchiveData(ByteString.CopyFrom(waypointEntityEnterGameWorld)).Build().ToByteArray()));

                        // Dequeue loading screen
                        messageList.Add(new(GameServerToClientMessage.NetMessageDequeueLoadingScreen, NetMessageDequeueLoadingScreen.DefaultInstance.ToByteArray()));

                        messages = messageList.ToArray();
                    }

                    break;

                case RegionPrototype.DangerRoom:
                    messages = PacketHelper.LoadMessagesFromPacketFile("DangerRoomFinishLoading.bin");
                    break;

                case RegionPrototype.MidtownPatrolCosmic:
                    messages = PacketHelper.LoadMessagesFromPacketFile("MidtownFinishLoading.bin");
                    break;
            }

            return messages;
        }

        private static GameMessage[] LoadLocalPlayerDataMessages(HardcodedAvatarEntity avatar)
        {
            List<GameMessage> messageList = new();

            var localPlayerMessage = NetMessageLocalPlayer.CreateBuilder()
                .SetLocalPlayerEntityId(14646212)
                .SetGameOptions(NetStructGameOptions.CreateBuilder()
                    .SetTeamUpSystemEnabled(true)
                    .SetAchievementsEnabled(true)
                    .SetOmegaMissionsEnabled(true)
                    .SetVeteranRewardsEnabled(true)
                    .SetMultiSpecRewardsEnabled(true)
                    .SetGiftingEnabled(true)
                    .SetCharacterSelectV2Enabled(true)
                    .SetCommunityNewsV2Enabled(true)
                    .SetLeaderboardsEnabled(true)
                    .SetNewPlayerExperienceEnabled(true)
                    .SetServerTimeOffsetUTC(-7)
                    .SetUseServerTimeOffset(false)
                    .SetMissionTrackerV2Enabled(true)
                    .SetGiftingAccountAgeInDaysRequired(7)
                    .SetGiftingAvatarLevelRequired(20)
                    .SetGiftingLoginCountRequired(5)
                    .SetInfinitySystemEnabled(true)
                    .SetChatBanVoteAccountAgeInDaysRequired(7)
                    .SetChatBanVoteAvatarLevelRequired(20)
                    .SetChatBanVoteLoginCountRequired(5)
                    .SetIsDifficultySliderEnabled(true)
                    .SetOrbisTrophiesEnabled(true)
                    .SetPlatformType(8))
                .Build().ToByteArray();

            messageList.Add(new(GameServerToClientMessage.NetMessageLocalPlayer, localPlayerMessage));

            GameMessage[] localPlayerEntityCreateMessages = PacketHelper.LoadMessagesFromPacketFile("LocalPlayerEntityCreateMessages.bin");
            ulong replacementInventorySlot = 100;   // 100 here because no hero occupies slot 100, this to check that we have successfully swapped heroes

            foreach (GameMessage message in localPlayerEntityCreateMessages)
            {
                var entityCreateMessage = NetMessageEntityCreate.ParseFrom(message.Content);
                EntityCreateBaseData baseData = new(entityCreateMessage.BaseData.ToByteArray());

                if (baseData.EntityId == 14646212)      // add the main local player entity straight away
                {
                    messageList.Add(message);
                }
                else
                {
                    // Modify base data if loading any hero other than Black Cat
                    if (avatar != HardcodedAvatarEntity.BlackCat)
                    {
                        if (baseData.EntityId == (ulong)avatar)
                        {
                            replacementInventorySlot = baseData.DynamicFields[5];
                            baseData.DynamicFields[4] = 273;    // put selected avatar in PlayerAvatarInPlay
                            baseData.DynamicFields[5] = 0;      // set avatar entity inventory slot to 0
                        }
                        else if (baseData.EntityId == (ulong)HardcodedAvatarEntity.BlackCat)
                        {
                            baseData.DynamicFields[4] = 169;                        // put Black Cat in PlayerAvatarLibrary 
                            baseData.DynamicFields[5] = replacementInventorySlot;   // set Black Cat slot to the one previously occupied by the hero who replaces her

                            // Black Cat goes last in the hardcoded messages, so this should always be assigned last
                            if (replacementInventorySlot == 0) Logger.Warn("replacementInventorySlot is 100! Check the hardcoded avatar entity data");
                        }

                        var customEntityCreateMessage = NetMessageEntityCreate.CreateBuilder()
                            .SetBaseData(ByteString.CopyFrom(baseData.Encode()))
                            .SetArchiveData(entityCreateMessage.ArchiveData)
                            .Build().ToByteArray();

                        messageList.Add(new(GameServerToClientMessage.NetMessageEntityCreate, customEntityCreateMessage));
                    }
                    else
                    {
                        messageList.Add(message);
                    }
                }
            }

            return messageList.ToArray();
        }           

        private static GameMessage[] LoadRegionTransitionMessages(RegionPrototype? region)
        {
            List<GameMessage> messageList = new();

            messageList.Add(new(GameServerToClientMessage.NetMessageRegionChange, NetMessageRegionChange.CreateBuilder()
                .SetRegionId(0)
                .SetServerGameId(0)
                .SetClearingAllInterest(false)
                .Build().ToByteArray()));

            messageList.Add(new(GameServerToClientMessage.NetMessageQueueLoadingScreen, NetMessageQueueLoadingScreen.CreateBuilder()
                .SetRegionPrototypeId((ulong)RegionPrototype.AvengersTower)
                .Build().ToByteArray()));

            byte[] avengersTowerRawRegionArchiveData = {
                0xEF, 0x01, 0xE8, 0xC1, 0x02, 0x02, 0x00, 0x00, 0x00, 0x2C, 0xED, 0xC6,
                0x05, 0x95, 0x80, 0x02, 0x0C, 0x00, 0x04, 0x9E, 0xCB, 0xD1, 0x93, 0xC7,
                0xE8, 0xAF, 0xCC, 0xEE, 0x01, 0x06, 0x00, 0x8B, 0xE5, 0x02, 0x9E, 0xE6,
                0x97, 0xCA, 0x0C, 0x01, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x04, 0x9B, 0xB2, 0x81, 0xF2, 0x83, 0xC6, 0xCD, 0x92, 0x10,
                0x06, 0x00, 0xA2, 0xE0, 0x03, 0xBC, 0x88, 0xA0, 0x89, 0x0E, 0x01, 0x00,
                0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xCC, 0xD7, 0xD1,
                0xBE, 0xA9, 0xB0, 0xBB, 0xFE, 0x44, 0x06, 0x00, 0xCF, 0xF3, 0x04, 0xBC,
                0xA4, 0xAD, 0xD3, 0x0A, 0x01, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0xC3, 0xBE, 0xB9, 0xC8, 0xD6, 0x8F, 0xAF, 0x8C, 0xE7,
                0x01, 0x06, 0x00, 0xC7, 0x98, 0x05, 0xD6, 0x91, 0xB8, 0xA9, 0x0E, 0x01,
                0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00
            };

            messageList.Add(new(GameServerToClientMessage.NetMessageRegionChange, NetMessageRegionChange.CreateBuilder()
                .SetRegionId(1153583383226286088)
                .SetServerGameId(1150669705055451881)
                .SetClearingAllInterest(false)
                .SetRegionPrototypeId((ulong)RegionPrototype.AvengersTower)
                .SetRegionRandomSeed(1488502313)
                .SetArchiveData(ByteString.CopyFrom(avengersTowerRawRegionArchiveData))
                .SetRegionMin(NetStructPoint3.CreateBuilder()
                    .SetX(-5024)
                    .SetY(-5024)
                    .SetZ(-2048))
                .SetRegionMax(NetStructPoint3.CreateBuilder()
                    .SetX(5024)
                    .SetY(5024)
                    .SetZ(2048))
                .SetCreateRegionParams(NetStructCreateRegionParams.CreateBuilder()
                    .SetLevel(10)
                    .SetDifficultyTierProtoId(18016845980090109785))
                .Build().ToByteArray()));

            messageList.Add(new(GameServerToClientMessage.NetMessagePrefetchRegionsForDownload, NetMessagePrefetchRegionsForDownload.CreateBuilder()
                .AddPrototypes(5542489395005235439)
                .AddPrototypes(6121022758926621561)
                .AddPrototypes(6769056952657388355)
                .AddPrototypes(7293929583592937434)
                .AddPrototypes(10115017851235015611)
                .AddPrototypes(11922318117493283053)
                .AddPrototypes(13643380196511063922)
                .AddPrototypes(14928163756943415585)
                .AddPrototypes(15546930156792977757)
                .AddPrototypes(16748618685203816205)
                .Build().ToByteArray()));

            messageList.Add(new(GameServerToClientMessage.NetMessageQueueLoadingScreen, NetMessageQueueLoadingScreen.CreateBuilder()
                .SetRegionPrototypeId((ulong)RegionPrototype.AvengersTower)
                .Build().ToByteArray()));

            messageList.Add(new(GameServerToClientMessage.NetMessageAddArea, NetMessageAddArea.CreateBuilder()
                .SetAreaId(1)
                .SetAreaPrototypeId(11135337283876558073)
                .SetAreaOrigin(NetStructPoint3.CreateBuilder()
                    .SetX(0)
                    .SetY(0)
                    .SetZ(0))
                .SetIsStartArea(true)
                .Build().ToByteArray()));

            messageList.Add(new(GameServerToClientMessage.NetMessageCellCreate, NetMessageCellCreate.CreateBuilder()
                .SetAreaId(1)
                .SetCellId(1)
                .SetCellPrototypeId(14256372356117109756)
                .SetPositionInArea(NetStructPoint3.CreateBuilder()
                    .SetX(0)
                    .SetY(0)
                    .SetZ(0))
                .SetCellRandomSeed(1488502313)
                .AddEncounters(NetStructReservedSpawn.CreateBuilder()
                    .SetAsset(605211710028059265)
                    .SetId(5)
                    .SetUseMarkerOrientation(true))
                .SetBufferwidth(0)
                .SetOverrideLocationName(0)
                .Build().ToByteArray()));

            messageList.Add(new(GameServerToClientMessage.NetMessageEnvironmentUpdate, NetMessageEnvironmentUpdate.CreateBuilder()
                .SetFlags(1)
                .Build().ToByteArray()));

            messageList.Add(new(GameServerToClientMessage.NetMessageUpdateMiniMap, NetMessageUpdateMiniMap.CreateBuilder()
                .SetArchiveData(ByteString.CopyFrom(new byte[] { 0xEF, 0x01, 0x81 } ))
                .Build().ToByteArray()));

            return messageList.ToArray();
        }

        public static GameMessage[] GetWaypointRegionChangeMessages(RegionPrototype region)
        {
            GameMessage[] messages = Array.Empty<GameMessage>();
            List<GameMessage> messageList = new();
            byte[] minimap;

            byte[] waypointEntityCreateBaseData = {
                0x20, 0x0C, 0x83, 0x9F, 0x01, 0x20, 0x00, 0x20
            };

            byte[] waypointEntityCreateArchiveData = {
                0x20, 0xF4, 0xC1, 0x02, 0x06, 0x00, 0x00, 0x00, 0xCD, 0x80, 0x01, 0x88,
                0x80, 0xFC, 0xFF, 0x99, 0xBF, 0x96, 0x81, 0x10, 0xCC, 0xC0, 0x02, 0x02,
                0xCC, 0x80, 0x03, 0x02, 0xCD, 0x40, 0xD5, 0x82, 0x80, 0xDE, 0x86, 0x80,
                0x98, 0x04, 0x4D, 0xA1, 0xA1, 0xA4, 0xFE, 0x03, 0x99, 0xC0, 0x01, 0x83,
                0xB8, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00
            };

            switch (region)
            {
                case RegionPrototype.AvengersTower:

                    messageList.Add(new(GameServerToClientMessage.NetMessageRegionChange, NetMessageRegionChange.CreateBuilder()
                      .SetRegionId(0)
                      .SetServerGameId(0)
                      .SetClearingAllInterest(false)
                      .Build().ToByteArray()));

                    messageList.Add(new(GameServerToClientMessage.NetMessageQueueLoadingScreen, NetMessageQueueLoadingScreen.CreateBuilder()
                        .SetRegionPrototypeId((ulong)RegionPrototype.AvengersTower)
                        .Build().ToByteArray()));

                    byte[] avengersTowerRawRegionArchiveData = {
                        0xEF, 0x01, 0xE8, 0xC1, 0x02, 0x02, 0x00, 0x00, 0x00, 0x2C, 0xED, 0xC6,
                        0x05, 0x95, 0x80, 0x02, 0x0C, 0x00, 0x04, 0x9E, 0xCB, 0xD1, 0x93, 0xC7,
                        0xE8, 0xAF, 0xCC, 0xEE, 0x01, 0x06, 0x00, 0x8B, 0xE5, 0x02, 0x9E, 0xE6,
                        0x97, 0xCA, 0x0C, 0x01, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x04, 0x9B, 0xB2, 0x81, 0xF2, 0x83, 0xC6, 0xCD, 0x92, 0x10,
                        0x06, 0x00, 0xA2, 0xE0, 0x03, 0xBC, 0x88, 0xA0, 0x89, 0x0E, 0x01, 0x00,
                        0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xCC, 0xD7, 0xD1,
                        0xBE, 0xA9, 0xB0, 0xBB, 0xFE, 0x44, 0x06, 0x00, 0xCF, 0xF3, 0x04, 0xBC,
                        0xA4, 0xAD, 0xD3, 0x0A, 0x01, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0xC3, 0xBE, 0xB9, 0xC8, 0xD6, 0x8F, 0xAF, 0x8C, 0xE7,
                        0x01, 0x06, 0x00, 0xC7, 0x98, 0x05, 0xD6, 0x91, 0xB8, 0xA9, 0x0E, 0x01,
                        0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00
                    };

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageRegionChange, NetMessageRegionChange.CreateBuilder()
                        .SetRegionPrototypeId((ulong)RegionPrototype.AvengersTower)
                        .SetServerGameId(1150669705055451881)
                        .SetClearingAllInterest(false)
                        .SetRegionId(1150669705055451881)
                        .SetRegionRandomSeed(1488502313)
                        .SetArchiveData(ByteString.CopyFrom(avengersTowerRawRegionArchiveData))
                        .SetRegionMax(NetStructPoint3.CreateBuilder()
                            .SetX(5024)
                            .SetY(5024)
                            .SetZ(2048))
                        .SetRegionMin(NetStructPoint3.CreateBuilder()
                            .SetX(-5024)
                            .SetY(-5024)
                            .SetZ(-2048))
                        .SetCreateRegionParams(NetStructCreateRegionParams.CreateBuilder()
                            .SetLevel(10)
                            .SetDifficultyTierProtoId(18016845980090109785))
                        .Build().ToByteArray()));

                    messageList.Add(new(GameServerToClientMessage.NetMessagePrefetchRegionsForDownload, NetMessagePrefetchRegionsForDownload.CreateBuilder()
                        .AddPrototypes(5542489395005235439)
                        .AddPrototypes(6121022758926621561)
                        .AddPrototypes(6769056952657388355)
                        .AddPrototypes(7293929583592937434)
                        .AddPrototypes(10115017851235015611)
                        .AddPrototypes(11922318117493283053)
                        .AddPrototypes(13643380196511063922)
                        .AddPrototypes(14928163756943415585)
                        .AddPrototypes(15546930156792977757)
                        .AddPrototypes(16748618685203816205)
                        .Build().ToByteArray()));

                    messageList.Add(new(GameServerToClientMessage.NetMessageQueueLoadingScreen, NetMessageQueueLoadingScreen.CreateBuilder()
                        .SetRegionPrototypeId((ulong)RegionPrototype.AvengersTower)
                        .Build().ToByteArray()));

                    messageList.Add(new(GameServerToClientMessage.NetMessageAddArea, NetMessageAddArea.CreateBuilder()
                        .SetAreaId(1)
                        .SetAreaPrototypeId(11135337283876558073)
                        .SetAreaOrigin(NetStructPoint3.CreateBuilder()
                            .SetX(0)
                            .SetY(0)
                            .SetZ(0))
                        .SetIsStartArea(true)
                        .Build().ToByteArray()));

                    messageList.Add(new(GameServerToClientMessage.NetMessageCellCreate, NetMessageCellCreate.CreateBuilder()
                        .SetAreaId(1)
                        .SetCellId(1)
                        .SetCellPrototypeId(14256372356117109756)
                        .SetPositionInArea(NetStructPoint3.CreateBuilder()
                            .SetX(0)
                            .SetY(0)
                            .SetZ(0))
                        .SetCellRandomSeed(1488502313)
                        .AddEncounters(NetStructReservedSpawn.CreateBuilder()
                            .SetAsset(605211710028059265)
                            .SetId(5)
                            .SetUseMarkerOrientation(true))
                        .SetBufferwidth(0)
                        .SetOverrideLocationName(0)
                        .Build().ToByteArray()));

                    messageList.Add(new(GameServerToClientMessage.NetMessageEnvironmentUpdate, NetMessageEnvironmentUpdate.CreateBuilder()
                        .SetFlags(1)
                        .Build().ToByteArray()));

                    messageList.Add(new(GameServerToClientMessage.NetMessageUpdateMiniMap, NetMessageUpdateMiniMap.CreateBuilder()
                        .SetArchiveData(ByteString.CopyFrom(new byte[] { 0xEF, 0x01, 0x81 }))
                        .Build().ToByteArray()));

                    messageList.Add(new(GameServerToClientMessage.NetMessageEntityCreate, NetMessageEntityCreate.CreateBuilder()
                        .SetBaseData(ByteString.CopyFrom(waypointEntityCreateBaseData))
                        .SetArchiveData(ByteString.CopyFrom(waypointEntityCreateArchiveData))
                        .Build().ToByteArray()));

                    messages = messageList.ToArray();
                    break;

                case RegionPrototype.DangerRoom:

                    messageList.Add(new(GameServerToClientMessage.NetMessageRegionChange, NetMessageRegionChange.CreateBuilder()
                        .SetRegionId(0)
                        .SetServerGameId(0)
                        .SetClearingAllInterest(false)
                        .Build().ToByteArray()));

                    messageList.Add(new(GameServerToClientMessage.NetMessageQueueLoadingScreen, NetMessageQueueLoadingScreen.CreateBuilder()
                        .SetRegionPrototypeId((ulong)RegionPrototype.DangerRoom)
                        .Build().ToByteArray()));

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageRegionChange,
                        NetMessageRegionChange.CreateBuilder()
                        .SetRegionPrototypeId((ulong)RegionPrototype.DangerRoom)
                        .SetServerGameId(1)
                        .SetClearingAllInterest(false)
                        .SetRegionId(1)
                        .SetRegionRandomSeed(1)
                        .SetCreateRegionParams(NetStructCreateRegionParams.CreateBuilder().SetLevel(10).SetDifficultyTierProtoId(18016845980090109785).Build())
                        .SetRegionMax(NetStructPoint3.CreateBuilder().SetX(1664).SetY(1664).SetZ(1664).Build())
                        .SetRegionMin(NetStructPoint3.CreateBuilder().SetX(-1664).SetY(-1664).SetZ(-1664).Build())
                        .Build().ToByteArray()));

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageAddArea, NetMessageAddArea.CreateBuilder()
                        .SetAreaId(1)
                        .SetAreaPrototypeId(12475690031293798605)
                        .SetAreaOrigin(NetStructPoint3.CreateBuilder()
                            .SetX(0)
                            .SetY(0)
                            .SetZ(0))
                        .SetIsStartArea(true)
                        .Build().ToByteArray()));

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageCellCreate, NetMessageCellCreate.CreateBuilder()
                        .SetAreaId(1)
                        .SetCellId(1)
                        .SetCellPrototypeId(5938132704447044414)
                        .SetPositionInArea(NetStructPoint3.CreateBuilder()
                            .SetX(1000)
                            .SetY(700)
                            .SetZ(0))
                        .SetCellRandomSeed(1)
                        .SetBufferwidth(0)
                        .SetOverrideLocationName(0)
                        .Build().ToByteArray()));


                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageEnvironmentUpdate, NetMessageEnvironmentUpdate.CreateBuilder()
                        .SetFlags(1)
                        .Build().ToByteArray()));

                    messageList.Add(new(GameServerToClientMessage.NetMessageUpdateMiniMap, NetMessageUpdateMiniMap.CreateBuilder()
                        .SetArchiveData(ByteString.CopyFrom(new byte[] { 0xEF, 0x01, 0x81 }))
                        .Build().ToByteArray()));

                    messageList.Add(new(GameServerToClientMessage.NetMessageEntityCreate, NetMessageEntityCreate.CreateBuilder()
                        .SetBaseData(ByteString.CopyFrom(waypointEntityCreateBaseData))
                        .SetArchiveData(ByteString.CopyFrom(waypointEntityCreateArchiveData))
                        .Build().ToByteArray()));

                    messages = messageList.ToArray();
                    break;

                case RegionPrototype.MidtownPatrolCosmic:

                    messageList.Add(new(GameServerToClientMessage.NetMessageRegionChange, NetMessageRegionChange.CreateBuilder()
                        .SetRegionId(0)
                        .SetServerGameId(0)
                        .SetClearingAllInterest(false)
                        .Build().ToByteArray()));

                    messageList.Add(new(GameServerToClientMessage.NetMessageQueueLoadingScreen, NetMessageQueueLoadingScreen.CreateBuilder()
                        .SetRegionPrototypeId((ulong)RegionPrototype.MidtownPatrolCosmic)
                        .Build().ToByteArray()));

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageRegionChange, NetMessageRegionChange.CreateBuilder()
                        .SetRegionPrototypeId((ulong)RegionPrototype.MidtownPatrolCosmic)
                        .SetServerGameId(1150669705055451881)
                        .SetClearingAllInterest(false)
                        .SetRegionId(1154146333179724697)
                        .SetRegionRandomSeed(1883928786)
                        .SetCreateRegionParams(NetStructCreateRegionParams.CreateBuilder().SetLevel(63).SetSeed(1).SetDifficultyTierProtoId(586640101754933627).Build())
                        .SetRegionMax(NetStructPoint3.CreateBuilder().SetX(12672).SetY(12672).SetZ(1152).Build())
                        .SetRegionMin(NetStructPoint3.CreateBuilder().SetX(-20000).SetY(-20000).SetZ(-1152).Build())
                        .Build().ToByteArray()));

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessagePrefetchRegionsForDownload, NetMessagePrefetchRegionsForDownload.CreateBuilder().AddPrototypes((ulong)RegionPrototype.AvengersTower).Build().ToByteArray()));

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageMatchTeamSizeNotification,
                     NetMessageMatchTeamSizeNotification.CreateBuilder().SetMetaGameEntityId(63).SetTeamSize(10).Build().ToByteArray()));

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageMatchTeamRosterNotification,
                     NetMessageMatchTeamRosterNotification.CreateBuilder().SetTeamPrototypeId(2619106790731156870).SetMetaGameEntityId(63).AddPlayerDbGuids(2305843009214561539).Build().ToByteArray()));

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageAddArea,
                     NetMessageAddArea.CreateBuilder()
                       .SetAreaId(1)
                       .SetAreaPrototypeId(307100709842327667)
                       .SetAreaOrigin(NetStructPoint3.CreateBuilder().SetX(-10800).SetY(-6500).SetZ(-20).Build())
                       .SetIsStartArea(true)
                       .Build().ToByteArray()));

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageCellCreate,
                        NetMessageCellCreate.CreateBuilder()
                        .SetAreaId(1)
                        .SetCellId(30)
                        .SetCellPrototypeId(16904680670227997475)
                        .SetPositionInArea(NetStructPoint3.CreateBuilder().SetX(0).SetY(0).SetZ(0).Build())
                        .SetCellRandomSeed(1883928786)
                        .SetBufferwidth(0)
                        .SetOverrideLocationName(0)
                        .Build().ToByteArray()));

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageCellCreate,
                        NetMessageCellCreate.CreateBuilder()
                        .SetAreaId(1)
                        .SetCellId(23)
                        .SetCellPrototypeId(6471827512511636368)
                        .SetPositionInArea(NetStructPoint3.CreateBuilder().SetX(0).SetY(0).SetZ(0).Build())
                        .SetCellRandomSeed(1883928786)
                        .SetBufferwidth(0)
                        .SetOverrideLocationName(0)
                        .Build().ToByteArray()));

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageCellCreate,
                        NetMessageCellCreate.CreateBuilder()
                        .SetAreaId(1)
                        .SetCellId(35)
                        .SetCellPrototypeId(92949505051927936)
                        .SetPositionInArea(NetStructPoint3.CreateBuilder().SetX(0).SetY(0).SetZ(0).Build())
                        .SetCellRandomSeed(1883928786)
                        .SetBufferwidth(0)
                        .SetOverrideLocationName(0)
                        .Build().ToByteArray()));

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageCellCreate,
                        NetMessageCellCreate.CreateBuilder()
                        .SetAreaId(1)
                        .SetCellId(31)
                        .SetCellPrototypeId(5807200255061009190)
                        .SetPositionInArea(NetStructPoint3.CreateBuilder().SetX(0).SetY(0).SetZ(0).Build())
                        .SetCellRandomSeed(1883928786)
                        .SetBufferwidth(0)
                        .SetOverrideLocationName(0)
                        .Build().ToByteArray()));

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageCellCreate,
                        NetMessageCellCreate.CreateBuilder()
                        .SetAreaId(1)
                        .SetCellId(12)
                        .SetCellPrototypeId(4255131489983407209)
                        .SetPositionInArea(NetStructPoint3.CreateBuilder().SetX(0).SetY(0).SetZ(0).Build())
                        .SetCellRandomSeed(1883928786)
                        .SetBufferwidth(0)
                        .SetOverrideLocationName(0)
                        .Build().ToByteArray()));

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageCellCreate,
                        NetMessageCellCreate.CreateBuilder()
                        .SetAreaId(1)
                        .SetCellId(13)
                        .SetCellPrototypeId(16358808346792043741)
                        .SetPositionInArea(NetStructPoint3.CreateBuilder().SetX(0).SetY(0).SetZ(0).Build())
                        .SetCellRandomSeed(1883928786)
                        .SetBufferwidth(0)
                        .SetOverrideLocationName(0)
                        .Build().ToByteArray()));

                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageEnvironmentUpdate,
                        NetMessageEnvironmentUpdate.CreateBuilder()
                        .SetFlags(1)
                        .Build().ToByteArray()));

                    minimap = Convert.FromHexString("EF0101C00500000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
                    messageList.Add(new((byte)GameServerToClientMessage.NetMessageUpdateMiniMap, NetMessageUpdateMiniMap.CreateBuilder().SetArchiveData(ByteString.CopyFrom(minimap)).Build().ToByteArray()));


                    messageList.Add(new(GameServerToClientMessage.NetMessageEntityCreate, NetMessageEntityCreate.CreateBuilder()
                        .SetBaseData(ByteString.CopyFrom(waypointEntityCreateBaseData))
                        .SetArchiveData(ByteString.CopyFrom(waypointEntityCreateArchiveData))
                        .Build().ToByteArray()));

                    messages = messageList.ToArray();
                    break;
            }

            return messages;
        }

        public static GameMessage[] GetWaypointRegionChangeFinishLoadingMessages(RegionPrototype? waypointRegion, HardcodedAvatarEntity avatar)
        {
            GameMessage[] messages = Array.Empty<GameMessage>();
            List<GameMessage> messageList = new();

            // Put player avatar entity in the game world
            byte[] avatarEntityEnterGameWorldArchiveData = {
                0x01, 0xB2, 0xF8, 0xFD, 0x06, 0xA0, 0x21, 0xF0, 0xA3, 0x01, 0xBC, 0x40,
                0x90, 0x2E, 0x91, 0x03, 0xBC, 0x05, 0x00, 0x00, 0x01
            };

            EntityEnterGameWorldArchiveData avatarEnterArchiveData = new(avatarEntityEnterGameWorldArchiveData);
            avatarEnterArchiveData.EntityId = (ulong)avatar;


            messageList.Add(new(GameServerToClientMessage.NetMessageEntityEnterGameWorld,
                NetMessageEntityEnterGameWorld.CreateBuilder()
                .SetArchiveData(ByteString.CopyFrom(avatarEnterArchiveData.Encode()))
                .Build().ToByteArray()));

            // Put waypoint entity in the game world
            byte[] waypointEntityEnterGameWorld = {
                0x01, 0x0C, 0x02, 0x80, 0x43, 0xE0, 0x6B, 0xD8, 0x2A, 0xC8, 0x01
            };

            messageList.Add(new(GameServerToClientMessage.NetMessageEntityEnterGameWorld,
                NetMessageEntityEnterGameWorld.CreateBuilder().SetArchiveData(ByteString.CopyFrom(waypointEntityEnterGameWorld)).Build().ToByteArray()));


            if (waypointRegion is not null)
            {
                switch (waypointRegion)
                {
                    case RegionPrototype.AvengersTower:
                        break;
                    case RegionPrototype.DangerRoom:
                        messageList.Add(new(GameServerToClientMessage.NetMessageEntityPosition, NetMessageEntityPosition.CreateBuilder()
                            .SetAreaId(1)
                            .SetCellId(1)
                            .SetIdEntity(12)
                            .SetFlags(1)
                            .SetPosition(NetStructPoint3.CreateBuilder()
                                .SetX(710)
                                .SetY(295)
                                .SetZ(0))
                            .SetOrientation(NetStructPoint3.CreateBuilder()
                                .SetX(0)
                                .SetY(0)
                                .SetZ(0))
                            .Build().ToByteArray()));
                        break;
                    case RegionPrototype.MidtownPatrolCosmic:

                        messageList.Add(new(GameServerToClientMessage.NetMessageEntityPosition, NetMessageEntityPosition.CreateBuilder()
                            .SetAreaId(1)
                            .SetCellId(1)
                            .SetIdEntity(12)
                            .SetFlags(1)
                            .SetPosition(NetStructPoint3.CreateBuilder()
                                .SetX(1300)
                                .SetY(515)
                                .SetZ(0))
                            .SetOrientation(NetStructPoint3.CreateBuilder()
                                .SetX(0)
                                .SetY(0)
                                .SetZ(0))
                            .Build().ToByteArray()));
                        break;
                }
            }

            // Dequeue loading screen
            messageList.Add(new(GameServerToClientMessage.NetMessageDequeueLoadingScreen, NetMessageDequeueLoadingScreen.DefaultInstance.ToByteArray()));
            messages = messageList.ToArray();

            return messages;
        }
    }
}
