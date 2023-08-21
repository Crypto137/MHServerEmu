using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Networking;
using MHServerEmu.Common;
using MHServerEmu.Common.Config;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Entities.Avatars;
using MHServerEmu.GameServer.Powers;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.Regions
{
    public static class RegionLoader
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static GameMessage[] GetBeginLoadingMessages(RegionPrototype regionPrototype, HardcodedAvatarEntity avatar, bool loadEntities = true)
        {
            List<GameMessage> messageList = new();

            // Add server info messages
            messageList.Add(new(NetMessageMarkFirstGameFrame.CreateBuilder()
                .SetCurrentservergametime(161351682950)
                .SetCurrentservergameid(1150669705055451881)
                .SetGamestarttime(1)
                .Build()));

            messageList.Add(new(NetMessageServerVersion.CreateBuilder().SetVersion("1.52.0.1700").Build()));

            messageList.Add(PacketHelper.LoadMessagesFromPacketFile("NetMessageLiveTuningUpdate.bin")[0]);
            messageList.Add(new(NetMessageReadyForTimeSync.DefaultInstance));

            // Load local player data
            if (loadEntities) messageList.AddRange(LoadLocalPlayerDataMessages(avatar));
            messageList.Add(new(NetMessageReadyAndLoadedOnGameServer.DefaultInstance));

            // Load region data
            messageList.AddRange(RegionManager.GetRegion(regionPrototype).GetLoadingMessages(1150669705055451881));

            // Create waypoint entity
            messageList.Add(new(NetMessageEntityCreate.CreateBuilder()
                .SetBaseData(ByteString.CopyFrom(Convert.FromHexString("200C839F01200020")))
                .SetArchiveData(ByteString.CopyFrom(Convert.FromHexString("20F4C10206000000CD80018880FCFF99BF968110CCC00202CC800302CD40D58280DE868098044DA1A1A4FE0399C00183B8030000000000")))
                .Build()));

            return messageList.ToArray();
        }

        public static GameMessage[] GetFinishLoadingMessages(RegionPrototype regionPrototype, HardcodedAvatarEntity avatar)
        {
            List<GameMessage> messageList = new();

            Region region = RegionManager.GetRegion(regionPrototype);

            EnterGameWorldArchive avatarEnterGameWorldArchive = new((ulong)avatar, region.EntrancePosition, region.EntranceOrientation.X, 350f);
            messageList.Add(new(NetMessageEntityEnterGameWorld.CreateBuilder()
                .SetArchiveData(ByteString.CopyFrom(avatarEnterGameWorldArchive.Encode()))
                .Build()));

            // Put waypoint entity in the game world
            EnterGameWorldArchive waypointEnterGameWorldArchiveData = new(12, region.WaypointPosition, region.WaypointOrientation.X);
            messageList.Add(new(NetMessageEntityEnterGameWorld.CreateBuilder()
                .SetArchiveData(ByteString.CopyFrom(waypointEnterGameWorldArchiveData.Encode()))
                .Build()));

            // Load power collection
            messageList.AddRange(PowerLoader.LoadAvatarPowerCollection(avatar).ToList());

            // Dequeue loading screen
            messageList.Add(new(NetMessageDequeueLoadingScreen.DefaultInstance));

            return messageList.ToArray();
        }

        private static GameMessage[] LoadLocalPlayerDataMessages(HardcodedAvatarEntity avatarEntityId)
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
                .Build();

            messageList.Add(new(localPlayerMessage));

            GameMessage[] localPlayerEntityCreateMessages = PacketHelper.LoadMessagesFromPacketFile("LocalPlayerEntityCreateMessages.bin");
            uint replacementInventorySlot = 100;   // 100 here because no hero occupies slot 100, this to check that we have successfully swapped heroes

            foreach (GameMessage message in localPlayerEntityCreateMessages)
            {
                var entityCreateMessage = NetMessageEntityCreate.ParseFrom(message.Content);
                EntityCreateBaseData baseData = new(entityCreateMessage.BaseData.ToByteArray());

                if (baseData.EntityId == 14646212)      // Player entity
                {
                    Player player = new(entityCreateMessage.ArchiveData.ToByteArray());

                    // edit player data here

                    var customEntityCreateMessage = NetMessageEntityCreate.CreateBuilder()
                        .SetBaseData(ByteString.CopyFrom(baseData.Encode()))
                        .SetArchiveData(ByteString.CopyFrom(player.Encode()))
                        .Build();

                    messageList.Add(new(customEntityCreateMessage));
                }
                else
                {
                    Avatar avatar = new(entityCreateMessage.ArchiveData.ToByteArray());

                    // modify base data
                    if (avatarEntityId != HardcodedAvatarEntity.BlackCat)
                    {
                        if (baseData.EntityId == (ulong)avatarEntityId)
                        {
                            replacementInventorySlot = baseData.InvLoc.Slot;
                            baseData.InvLoc.InventoryPrototypeId = GameDatabase.GetPrototypeId("Entity/Inventory/PlayerInventories/PlayerAvatarInPlay.prototype");
                            baseData.InvLoc.Slot = 0;                           // set selected avatar entity inventory slot to 0
                        }
                        else if (baseData.EntityId == (ulong)HardcodedAvatarEntity.BlackCat)
                        {
                            baseData.InvLoc.InventoryPrototypeId = GameDatabase.GetPrototypeId("Entity/Inventory/PlayerInventories/PlayerAvatarLibrary.prototype");
                            baseData.InvLoc.Slot = replacementInventorySlot;    // set Black Cat slot to the one previously occupied by the hero who replaces her

                            // Black Cat goes last in the hardcoded messages, so this should always be assigned last
                            if (replacementInventorySlot == 100) Logger.Warn("replacementInventorySlot is 100! Check the hardcoded avatar entity data");
                        }
                    }

                    if (baseData.EntityId == (ulong)avatarEntityId)
                    {
                        // modify avatar data here

                        avatar.PlayerName.Text = ConfigManager.PlayerData.PlayerName;

                        foreach (Property property in avatar.Properties)
                        {
                            if (property.Info.Name == "CostumeCurrent" && ConfigManager.PlayerData.CostumeOverride != 0)
                            {
                                try
                                {
                                    property.Value = GameDatabase.PrototypeEnumManager.GetEnumValue(ConfigManager.PlayerData.CostumeOverride, PrototypeEnumType.Property);
                                }
                                catch
                                {
                                    Logger.Warn($"Failed to get costume prototype enum for id {ConfigManager.PlayerData.CostumeOverride}");
                                }   
                            }
                        }
                    }

                    var customEntityCreateMessage = NetMessageEntityCreate.CreateBuilder()
                        .SetBaseData(ByteString.CopyFrom(baseData.Encode()))
                        .SetArchiveData(ByteString.CopyFrom(avatar.Encode()))
                        .Build();

                    messageList.Add(new(customEntityCreateMessage));
                }
            }

            return messageList.ToArray();
        }
    }
}
