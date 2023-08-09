using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Networking;
using MHServerEmu.Common;
using MHServerEmu.Common.Config;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Entities.Archives;
using MHServerEmu.GameServer.Powers;
using MHServerEmu.GameServer.Data;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Regions
{
    public static class RegionLoader
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static GameMessage[] GetBeginLoadingMessages(RegionPrototype regionPrototype, HardcodedAvatarEntity avatar, bool loadEntities = true)
        {
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
            if (loadEntities) messageList.AddRange(LoadLocalPlayerDataMessages(avatar));
            messageList.Add(new(GameServerToClientMessage.NetMessageReadyAndLoadedOnGameServer, NetMessageReadyAndLoadedOnGameServer.DefaultInstance.ToByteArray()));

            // Load region data
            messageList.AddRange(RegionManager.GetRegion(regionPrototype).GetLoadingMessages(1150669705055451881));

            // Create waypoint entity
            messageList.Add(new(GameServerToClientMessage.NetMessageEntityCreate, NetMessageEntityCreate.CreateBuilder()
                .SetBaseData(ByteString.CopyFrom(Convert.FromHexString("200C839F01200020")))
                .SetArchiveData(ByteString.CopyFrom(Convert.FromHexString("20F4C10206000000CD80018880FCFF99BF968110CCC00202CC800302CD40D58280DE868098044DA1A1A4FE0399C00183B8030000000000")))
                .Build().ToByteArray()));

            return messageList.ToArray();
        }

        public static GameMessage[] GetFinishLoadingMessages(RegionPrototype regionPrototype, HardcodedAvatarEntity avatar)
        {
            List<GameMessage> messageList = new();

            Region region = RegionManager.GetRegion(regionPrototype);

            EntityEnterGameWorldArchiveData avatarEnterGameWorldArchiveData = new((ulong)avatar, region.EntrancePosition, region.EntranceOrientation.X, 350f);

            messageList.Add(new(GameServerToClientMessage.NetMessageEntityEnterGameWorld,
                NetMessageEntityEnterGameWorld.CreateBuilder()
                .SetArchiveData(ByteString.CopyFrom(avatarEnterGameWorldArchiveData.Encode()))
                .Build().ToByteArray()));

            // Put waypoint entity in the game world
            EntityEnterGameWorldArchiveData waypointEnterGameWorldArchiveData = new(12, region.WaypointPosition, region.WaypointOrientation.X);

            messageList.Add(new(GameServerToClientMessage.NetMessageEntityEnterGameWorld,
                NetMessageEntityEnterGameWorld.CreateBuilder().SetArchiveData(ByteString.CopyFrom(waypointEnterGameWorldArchiveData.Encode())).Build().ToByteArray()));

            // Load power collection
            messageList.AddRange(PowerLoader.LoadAvatarPowerCollection(avatar).ToList());

            // Dequeue loading screen
            messageList.Add(new(GameServerToClientMessage.NetMessageDequeueLoadingScreen, NetMessageDequeueLoadingScreen.DefaultInstance.ToByteArray()));

            return messageList.ToArray();
        }

        private static GameMessage[] LoadLocalPlayerDataMessages(HardcodedAvatarEntity avatar)
        {
            List<GameMessage> messageList = new();

            var localPlayerMessage = NetMessageLocalPlayer.CreateBuilder()
                .SetLocalPlayerEntityId(14646212)
                .SetGameOptions(NetStructGameOptions.CreateBuilder()
                    .SetTeamUpSystemEnabled(ConfigManager.GameOptions.TeamUpSystemEnabled)
                    .SetAchievementsEnabled(ConfigManager.GameOptions.AchievementsEnabled)
                    .SetOmegaMissionsEnabled(ConfigManager.GameOptions.OmegaMissionsEnabled)
                    .SetVeteranRewardsEnabled(ConfigManager.GameOptions.VeteranRewardsEnabled)
                    .SetMultiSpecRewardsEnabled(ConfigManager.GameOptions.MultiSpecRewardsEnabled)
                    .SetGiftingEnabled(ConfigManager.GameOptions.GiftingEnabled)
                    .SetCharacterSelectV2Enabled(ConfigManager.GameOptions.CharacterSelectV2Enabled)
                    .SetCommunityNewsV2Enabled(ConfigManager.GameOptions.CommunityNewsV2Enabled)
                    .SetLeaderboardsEnabled(ConfigManager.GameOptions.LeaderboardsEnabled)
                    .SetNewPlayerExperienceEnabled(ConfigManager.GameOptions.NewPlayerExperienceEnabled)
                    .SetServerTimeOffsetUTC(-7)
                    .SetUseServerTimeOffset(false)
                    .SetMissionTrackerV2Enabled(ConfigManager.GameOptions.MissionTrackerV2Enabled)
                    .SetGiftingAccountAgeInDaysRequired(ConfigManager.GameOptions.GiftingAccountAgeInDaysRequired)
                    .SetGiftingAvatarLevelRequired(ConfigManager.GameOptions.GiftingAvatarLevelRequired)
                    .SetGiftingLoginCountRequired(ConfigManager.GameOptions.GiftingLoginCountRequired)
                    .SetInfinitySystemEnabled(ConfigManager.GameOptions.InfinitySystemEnabled)
                    .SetChatBanVoteAccountAgeInDaysRequired(ConfigManager.GameOptions.ChatBanVoteAccountAgeInDaysRequired)
                    .SetChatBanVoteAvatarLevelRequired(ConfigManager.GameOptions.ChatBanVoteAvatarLevelRequired)
                    .SetChatBanVoteLoginCountRequired(ConfigManager.GameOptions.ChatBanVoteLoginCountRequired)
                    .SetIsDifficultySliderEnabled(ConfigManager.GameOptions.IsDifficultySliderEnabled)
                    .SetOrbisTrophiesEnabled(ConfigManager.GameOptions.OrbisTrophiesEnabled)
                    .SetPlatformType(8))
                .Build().ToByteArray();

            messageList.Add(new(GameServerToClientMessage.NetMessageLocalPlayer, localPlayerMessage));

            GameMessage[] localPlayerEntityCreateMessages = PacketHelper.LoadMessagesFromPacketFile("LocalPlayerEntityCreateMessages.bin");
            ulong replacementInventorySlot = 100;   // 100 here because no hero occupies slot 100, this to check that we have successfully swapped heroes

            foreach (GameMessage message in localPlayerEntityCreateMessages)
            {
                var entityCreateMessage = NetMessageEntityCreate.ParseFrom(message.Content);
                EntityCreateBaseData baseData = new(entityCreateMessage.BaseData.ToByteArray());

                if (baseData.EntityId == 14646212)      // Player entity
                {
                    Player player = new(entityCreateMessage.ArchiveData.ToByteArray());

                    // modify player data here

                    var customEntityCreateMessage = NetMessageEntityCreate.CreateBuilder()
                        .SetBaseData(ByteString.CopyFrom(baseData.Encode()))
                        .SetArchiveData(ByteString.CopyFrom(player.Encode()))
                        .Build().ToByteArray();

                    messageList.Add(new(GameServerToClientMessage.NetMessageEntityCreate, customEntityCreateMessage));
                }
                else
                {
                    // Modify base data if loading any hero other than Black Cat
                    Entity entity = new(entityCreateMessage.ArchiveData.ToByteArray());

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
                    }

                    if (ConfigManager.PlayerData.CostumeOverride != 0)
                    {
                        for (int i = 0; i < Database.GlobalEnumRefTable.Length; i++)
                        {
                            if (Database.GlobalEnumRefTable[i] == ConfigManager.PlayerData.CostumeOverride)
                            {
                                foreach (Property property in entity.Properties)
                                {
                                    if (property.Id == 0xE019) property.Value = (ulong)i;
                                }

                                break;
                            }
                        }
                    }

                    var customEntityCreateMessage = NetMessageEntityCreate.CreateBuilder()
                        .SetBaseData(ByteString.CopyFrom(baseData.Encode()))
                        .SetArchiveData(ByteString.CopyFrom(entity.Encode()))
                        .Build().ToByteArray();

                    messageList.Add(new(GameServerToClientMessage.NetMessageEntityCreate, customEntityCreateMessage));
                }
            }

            return messageList.ToArray();
        }
    }
}
