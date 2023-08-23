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

            /*
            if (regionPrototype == RegionPrototype.NPEAvengersTowerHUBRegion)
            {
                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(0x17, 17149607139718797253, new(588f, 1194f, 369f), new(-1.5625f, 0f, 0f),
                    0xA0F4, 608, 1, 608, 0x100259F99FFF0008, 1, 11135337283876558073, true)));
            }
            */

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

            if (regionPrototype == RegionPrototype.NPEAvengersTowerHUBRegion)
            {
                ulong area = GameDatabase.GetPrototypeId("Regions/HUBRevamp/NPEAvengersTowerHubArea.prototype");
                int cellid = 1;
                int areaid = 1;
                ulong repId = 50000;
                ulong entityId = 1000; 

                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++,
                    GameDatabase.GetPrototypeId("Entity/Characters/NPCs/HubNPCs/SHIELDAgentStanLee.prototype"),
                    new(588f, 1194f, 369f), new(-1.5625f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++,
                    GameDatabase.GetPrototypeId("Entity/Characters/Vendors/Prototypes/Endgame/TeamSHIELDRepBuffer.prototype"),
                    new(736f, -352f, 177f), new(-2.15625f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++,
                    GameDatabase.GetPrototypeId("Entity/Characters/NPCs/HubNPCs/MariaHill.prototype"),
                    new(924.5f, 996f, 369f), new(-2.9375f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++, 
                    GameDatabase.GetPrototypeId("Entity/Characters/NPCs/Objects/AvengersStash.prototype"),
                    new(1661.25f, -930.745f, 320f + 60f), new(-0.78541f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++, 
                    GameDatabase.GetPrototypeId("Entity/Characters/NPCs/Objects/AvengersStash.prototype"),
                    new(-208.444f, 1980.73f, 128f + 60f), new(-0.78541f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++, 
                    GameDatabase.GetPrototypeId("Entity/Characters/NPCs/Objects/AvengersStash.prototype"),
                    new(-292.686f, 1896.49f, 128f + 60f), new(-0.78541f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++,
                    GameDatabase.GetPrototypeId("Entity/Characters/NPCs/Objects/AvengersStash.prototype"),
                    new(-376.846f, 1808.53f, 128f + 60f), new(-0.78541f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                /* messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++, // not work
                      GameDatabase.GetPrototypeId("Entity/Characters/Vendors/Prototypes/HUB01AvengersTower/CosmicEventVendor.prototype"),
                      new(1241.92f, -1941.42f, 374f), new(2.45441f, 0f, 0f),
                      repId++, 608, areaid, 608, region.Id, cellid, area, false)));


                  messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++, // not work
                      GameDatabase.GetPrototypeId("Entity/Characters/NPCs/HubNPCs/SpiderWoman.prototype"),
                      new(-1376.1f, -1826.57f, 348.681f), new(6.97051f, 0f, 0f),
                      repId++, 608, areaid, 608, region.Id, cellid, area, false))); */


                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++, // Jarvis
                    GameDatabase.GetPrototypeId("Entity/Characters/Vendors/Prototypes/HUB01AvengersTower/ATVendorArmor.prototype"),
                    new(-192.58f, 870.282f, 180.331f), new(3.14164f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++,
                    GameDatabase.GetPrototypeId("Entity/Characters/Vendors/Prototypes/HUB01AvengersTower/ATVendorWeapon.prototype"),
                    new(-145.725f, 1433.93f, 180.331f), new(-2.84711f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++,
                    GameDatabase.GetPrototypeId("Entity/Characters/NPCs/HubNPCs/SheHulk.prototype"),
                    new(-1236.28f, 823.592f, 352.324f), new(2.35623f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++,
                    GameDatabase.GetPrototypeId("Entity/Characters/NPCs/HubNPCs/Wonderman.prototype"),
                    new(-1516.29f, 870.051f, 374f), new(6.28328f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++,
                    GameDatabase.GetPrototypeId("Entity/Characters/Vendors/Prototypes/HUB01AvengersTower/UruForgedVendor.prototype"),
                    new(-1275.32f, 1110.82f, 304f), new(-0.589058f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++,
                    GameDatabase.GetPrototypeId("Entity/Characters/Vendors/Prototypes/VendorEternitySplinterAdamWarlock.prototype"),
                    new(463.362f, -828.147f, 180.331f), new(2.35623f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));
               
                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++, // Clea
                    GameDatabase.GetPrototypeId("Entity/Characters/Vendors/Prototypes/HUB01AvengersTower/ATVendorHardcore.prototype"),
                    new(2288f, -2720f, 560f), new(-0.687234f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++, // War Machine
                    GameDatabase.GetPrototypeId("Entity/Characters/Vendors/Prototypes/HUB01AvengersTower/ATVendorGuild.prototype"),
                    new(1504.88f, -1701.76f, 371.813f), new(3.63252f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++, // Hank Pym
                    GameDatabase.GetPrototypeId("Entity/Characters/Vendors/Prototypes/HUB01AvengersTower/ATVendorCrafter.prototype"),
                    new(-1204f, 1114f, 359.984f), new(-2.84711f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++,
                    GameDatabase.GetPrototypeId("Entity/Characters/Vendors/Prototypes/HUB01AvengersTower/BIFBuxTaker.prototype"),
                    new(-55.3334f, 240.429f, 136.234f), new(4.01065f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++, 
                    GameDatabase.GetPrototypeId("Entity/Characters/Vendors/Prototypes/HUB01AvengersTower/BIFBuxGiver.prototype"),
                    new(-120f, 304f, 136), new(4.01027f, 0f, 0f),
                    repId++, 608, areaid, 608, region.Id, cellid, area, false)));

            }

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
                                    property.Value.Set(ConfigManager.PlayerData.CostumeOverride);
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
