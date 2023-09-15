using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Config;
using MHServerEmu.GameServer.Entities.Avatars;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Frontend.Accounts;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;
using MHServerEmu.GameServer.GameData.Prototypes.Markers;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.Powers;
using MHServerEmu.GameServer.Properties;
using MHServerEmu.GameServer.Regions;
using MHServerEmu.Networking;
using System.Numerics;
using System.Drawing;

namespace MHServerEmu.GameServer.Games
{
    public partial class Game
    {
        private GameMessage[] GetBeginLoadingMessages(PlayerData playerData, bool loadEntities = true)
        {
            List<GameMessage> messageList = new();

            // Add server info messages
            messageList.Add(new(NetMessageMarkFirstGameFrame.CreateBuilder()
                .SetCurrentservergametime(161351682950)
                .SetCurrentservergameid(1150669705055451881)
                .SetGamestarttime(1)
                .Build()));

            messageList.Add(new(NetMessageServerVersion.CreateBuilder().SetVersion("1.52.0.1700").Build()));

            messageList.Add(new(NetMessageLiveTuningUpdate.CreateBuilder()
                .AddRangeTuningTypeKeyValueSettings(GameDatabase.LiveTuningSettingList.Select(setting => setting.ToNetStructProtoEnumValue()))
                .Build()));

            NetMessageLiveTuningUpdate.CreateBuilder().AddRangeTuningTypeKeyValueSettings(GameDatabase.LiveTuningSettingList.Select(setting => setting.ToNetStructProtoEnumValue()));

            messageList.Add(new(NetMessageReadyForTimeSync.DefaultInstance));

            // Load local player data
            if (loadEntities) messageList.AddRange(LoadLocalPlayerDataMessages(playerData));
            messageList.Add(new(NetMessageReadyAndLoadedOnGameServer.DefaultInstance));

            // Load region data
            messageList.AddRange(RegionManager.GetRegion(playerData.Region).GetLoadingMessages(Id));

            // Create waypoint entity
            messageList.Add(new(NetMessageEntityCreate.CreateBuilder()
                .SetBaseData(ByteString.CopyFrom(Convert.FromHexString("200C839F01200020")))
                .SetArchiveData(ByteString.CopyFrom(Convert.FromHexString("20F4C10206000000CD80018880FCFF99BF968110CCC00202CC800302CD40D58280DE868098044DA1A1A4FE0399C00183B8030000000000")))
                .Build()));

            return messageList.ToArray();
        }

        private GameMessage[] GetFinishLoadingMessages(PlayerData playerData)
        {
            List<GameMessage> messageList = new();

            Region region = RegionManager.GetRegion(playerData.Region);

            EnterGameWorldArchive avatarEnterGameWorldArchive = new((ulong)playerData.Avatar, region.EntrancePosition, region.EntranceOrientation.X, 350f);
            messageList.Add(new(NetMessageEntityEnterGameWorld.CreateBuilder()
                .SetArchiveData(ByteString.CopyFrom(avatarEnterGameWorldArchive.Encode()))
                .Build()));

            ulong area;
            int cellid = 1;
            int areaid = 1;
            ulong repId = 50000;
            ulong entityId = 1000;
            Common.Vector3 areaOrigin = new();

            void MarkersAdd(CellPrototype Entry, int cell_id, bool AddProp = false)
            {
                for (int i = 0; i < Entry.MarkerSet.Length; i++)
                {
                    if (Entry.MarkerSet[i] is EntityMarkerPrototype)
                    {
                        EntityMarkerPrototype npc = (EntityMarkerPrototype)Entry.MarkerSet[i];
                        float zfix = 0.0f;
                        string marker = npc.LastKnownEntityName;

                        if (marker.Contains("DestructibleGarbageCanCity")) continue;
                        if (marker.Contains("GLFLieutenant")) continue; // Blocking controll
                        if (marker.Contains("Coulson")) continue; //  Blocking controll
                        if (marker.Contains("GambitMTXStore")) continue; // Invisible
                        if (marker.Contains("CosmicEventVendor")) continue; // Invisible
                        if (marker.Contains("Magik")) continue; // TODO fixme

                        if (marker.Contains("Entity/Characters/") || (AddProp && marker.Contains("Entity/Props/")))
                        {
                            if (marker.Contains("Stash"))
                            {
                                zfix = 60.0f;
                                if (npc.Position.Z == -208.0f) zfix = +13f; // fix 
                            }

                            if (marker.Contains("DestructibleWarehouseCrateA")) zfix = +20f;

                            messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++,
                                GameDatabase.GetPrototypeId(npc.EntityGuid),
                                new(npc.Position.X + areaOrigin.X, npc.Position.Y + areaOrigin.Y, npc.Position.Z + areaOrigin.Z + zfix), npc.Rotation,
                                repId++, 608, areaid, 608, region.Id, cell_id, area, false)));
                        }
                    }
                }
            }

            void MarkersAddDistrict(string path, bool AddProp = false)
            {
                District district = GameDatabase.Resource.DistrictDict[path];
                for (cellid = 0; cellid < district.CellMarkerSet.Length; cellid++)
                    MarkersAdd(GameDatabase.Resource.CellDict[district.CellMarkerSet[cellid].Resource], cellid + 1, AddProp);
            }

            switch (playerData.Region)
            {
                case RegionPrototype.AsgardiaRegion:

                    area = (ulong)AreaPrototype.AsgardiaArea;
                    MarkersAddDistrict("Resource/Districts/AsgardHubDistrict.district");

                    break;

                case RegionPrototype.XManhattanRegion1to60:
                case RegionPrototype.XManhattanRegion60Cosmic:

                    area = (ulong)AreaPrototype.XManhattanArea1;
                    MarkersAddDistrict("Resource/Districts/MidtownStatic/MidtownStatic_A.district");

                    break;

                case RegionPrototype.HelicarrierRegion:

                    area = (ulong)AreaPrototype.HelicarrierArea;
                    MarkersAdd(GameDatabase.Resource.CellDict["Resource/Cells/DistrictCells/Helicarrier/Helicarrier_HUB.cell"], cellid);

                    break;

                case RegionPrototype.CosmicDoopSectorSpaceRegion:

                    area = GameDatabase.GetPrototypeId("Regions/EndGame/Special/CosmicDoopSectorSpace/CosmicDoopSectorSpaceAreaA.prototype");
                    ulong[] doop = new ulong[]
                    {
                        8886032254367441193, // CosmicDoopRangedMinion
                        905954195879503067, // CosmicDoopMeleeMinionLargeAggro
                        11242103498987545924, // CosmicDoopRangedMinionLargeAggro
                        1173113805575694864, // CosmicDoopDoopZoneMiniBossVariantLargeAggro
                        8852879594302677942, // CosmicDoopOverlordLargeAggro
                        10884818398647164828 // CosmicDoopDoopZone
                    };

                    static Vector3[] DrawCirclePoints(float radius, int numPoints)
                    {
                        Vector3[] points = new Vector3[numPoints];

                        double angle = 2 * Math.PI / numPoints;

                        for (int i = 0; i < numPoints; i++)
                        {
                            float x = (float)(radius * Math.Cos(i * angle));
                            float y = (float)(radius * Math.Sin(i * angle));
                            float z = (float)(i * angle);
                            points[i] = new Vector3(x, y, z);
                        }

                        return points;
                    }

                    Vector3[] Doops = DrawCirclePoints(400.0f, 5);

                    void AddSmallDoop(Vector3 PosOrient, Common.Vector3 SpawnPos)
                    {
                        Common.Vector3 pos = new(SpawnPos.X + PosOrient.X, SpawnPos.Y + PosOrient.Y, SpawnPos.Z);
                        messageList.Add(new(EntityHelper.SpawnEntityEnemy(entityId++,
                                            doop[2],
                                            pos, new(PosOrient.Z, 0, 0),
                                            repId++, 608, areaid, 608, region.Id, cellid, area, false, 60, 60)));
                    }

                    void DrawGroupDoops(Common.Vector3 SpawnPos)
                    {
                        for (int i = 0; i < Doops.Count(); i++)
                        {
                            AddSmallDoop(Doops[i], SpawnPos);
                        }
                    }
                    ulong bossEntity;
                    ulong bossRep = repId;
                    Area areaDoop = region.AreaList[0];
                    for (int j = 0; j < region.AreaList[0].CellList.Count; j++)
                    {
                        cellid = (int)areaDoop.CellList[j].Id;
                        areaOrigin = areaDoop.CellList[j].PositionInArea;
                        CellPrototype Cell = GameDatabase.Resource.CellDict[GameDatabase.GetPrototypePath(areaDoop.CellList[j].PrototypeId)];
                        int num = 0;
                        for (int i = 0; i < Cell.MarkerSet.Length; i++)
                        {
                            if (Cell.MarkerSet[i] is EntityMarkerPrototype)
                            {                                
                                EntityMarkerPrototype npc = (EntityMarkerPrototype)Cell.MarkerSet[i];
                                Common.Vector3 pos = new(npc.Position.X + areaOrigin.X, npc.Position.Y + areaOrigin.Y, npc.Position.Z + areaOrigin.Z);
                                switch (npc.EntityGuid)
                                {
                                    case 2888059748704716317: // EncounterSmall
                                        num++;                  
                                        if (num == 1)
                                            messageList.Add(new(EntityHelper.SpawnEntityEnemy(entityId++,
                                                doop[3],
                                                pos, npc.Rotation,
                                                repId++, 608, areaid, 608, region.Id, cellid, area, false, 60, 60)));
                                        else
                                            DrawGroupDoops(pos);

                                        break;

                                    case 13880579250584290847: // EncounterMedium
                                        bossEntity = entityId;
                                        bossRep = repId;
                                        messageList.Add(new(EntityHelper.SpawnEntityEnemy(entityId++,
                                            doop[4],
                                            pos, npc.Rotation,
                                            repId++, 608, areaid, 608, region.Id, cellid, area, false, 60, 60)));

                                        break;

                                }
                            }
                        }
                    }
                    
                    Property SetProperty = new(PropertyEnum.Health, 600);
                    messageList.Add(
                        new(SetProperty.ToNetMessageSetProperty(bossRep))
                    );

                    break;

                case RegionPrototype.TrainingRoomSHIELDRegion:

                    area = (ulong)AreaPrototype.TrainingRoomSHIELDArea;
                    CellPrototype Entry = GameDatabase.Resource.CellDict["Resource/Cells/DistrictCells/Training_Rooms/TrainingRoom_SHIELD_B.cell"];
                    MarkersAdd(Entry, cellid, true);

                    cellid = 1;
                    for (int i = 0; i < Entry.MarkerSet.Length; i++)
                    {
                        if (Entry.MarkerSet[i] is EntityMarkerPrototype)
                        {
                            EntityMarkerPrototype npc = (EntityMarkerPrototype)Entry.MarkerSet[i];
                            Logger.Trace($"[{i}].EntityGuid = {npc.EntityGuid}");
                            switch (npc.EntityGuid)
                            {
                                case 9760489745388478121: // EncounterTinyV12                                    
                                    messageList.Add(new(EntityHelper.SpawnEntityEnemy(entityId++,
                                        GameDatabase.GetPrototypeId("Entity/Characters/Mobs/TrainingRoom/TrainingHPDummyBoss.prototype"),
                                        npc.Position, npc.Rotation,
                                        repId++, 608, areaid, 608, region.Id, cellid, area, false, 60, 60)));
                                    break;

                                case 1411432581376189649: // EncounterTinyV13                                    
                                    messageList.Add(new(EntityHelper.SpawnEntityEnemy(entityId++,
                                        GameDatabase.GetPrototypeId("Entity/Characters/Mobs/TrainingRoom/TrainingHPDummyRaidBoss.prototype"),
                                        npc.Position, npc.Rotation,
                                        repId++, 608, areaid, 608, region.Id, cellid, area, false, 60, 60)));
                                    break;

                                case 9712873838200498938: // EncounterTinyV14                                    
                                    messageList.Add(new(EntityHelper.SpawnEntityEnemy(entityId++,
                                        GameDatabase.GetPrototypeId("Entity/Characters/Mobs/CowsEG/SpearCowD1.prototype"), // why not?
                                        npc.Position, npc.Rotation, //Entity/Characters/Mobs/TrainingRoom/TrainingDamageDummy.prototype
                                        repId++, 608, areaid, 608, region.Id, cellid, area, false, 10, 10)));
                                    break;

                                case 17473025685948150052: // EncounterTinyV15                                    
                                    messageList.Add(new(EntityHelper.SpawnEntityEnemy(entityId++,
                                        GameDatabase.GetPrototypeId("Entity/Characters/Mobs/TrainingRoom/TrainingHPDummy.prototype"),
                                        npc.Position, npc.Rotation,
                                        repId++, 608, areaid, 608, region.Id, cellid, area, false, 10, 10)));
                                    break;

                            }
                        }
                    }
                    /* zero effects 
                    messageList.Add(new(NetMessageAIToggleState.CreateBuilder()
                        .SetState(true)
                        .Build())
                        );

                    messageList.Add(new(NetMessageDamageToggleState.CreateBuilder()
                        .SetState(false)
                        .Build())
                        );
                    */
                    break;

                case RegionPrototype.DangerRoomHubRegion:

                    area = (ulong)AreaPrototype.DangerRoomHubArea;
                    MarkersAdd(GameDatabase.Resource.CellDict["Resource/Cells/EndGame/EndlessDungeon/DangerRoom_LaunchTerminal.cell"], cellid);

                    break;

                case RegionPrototype.GenoshaHUBRegion:

                    area = (ulong)AreaPrototype.GenoshaHUBArea;
                    areaOrigin = region.AreaList[0].Origin;
                    MarkersAddDistrict("Resource/Districts/GenoshaHUB.district");

                    break;

                case RegionPrototype.XaviersMansionRegion:

                    area = (ulong)AreaPrototype.XaviersMansionArea;
                    MarkersAddDistrict("Resource/Districts/XaviersMansion.district");

                    break;

                case RegionPrototype.CH0701SavagelandRegion:

                    area = GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH07SavageLand/Areas/DinoJungle/DinoJungleArea.prototype");

                    Area areaL = region.AreaList[0];
                    for (int i = 11; i < 14; i++)
                    {
                        cellid = (int)areaL.CellList[i].Id;
                        areaOrigin = areaL.CellList[i].PositionInArea;
                        MarkersAdd(GameDatabase.Resource.CellDict[GameDatabase.GetPrototypePath(areaL.CellList[i].PrototypeId)], cellid);
                    }

                    break;

                case RegionPrototype.AvengersTowerHUBRegion:

                    area = (ulong)AreaPrototype.AvengersTowerHubArea;
                    MarkersAdd(GameDatabase.Resource.CellDict["Resource/Cells/DistrictCells/Avengers_Tower/AvengersTower_HUB.cell"], cellid);

                    break;

                case RegionPrototype.NPEAvengersTowerHUBRegion:

                    area = (ulong)AreaPrototype.NPEAvengersTowerHubArea;
                    MarkersAdd(GameDatabase.Resource.CellDict["Resource/Cells/DistrictCells/Avengers_Tower/AvengersTowerNPE_HUB.cell"], cellid);

                    /* Encounter PopulationMarker = GameDatabase.Resource.EncounterDict["Resource/Encounters/Discoveries/Social_BenUrich_JessicaJones.encounter"];
                       npc = (EntityMarkerPrototype)PopulationMarker.MarkerSet[0]; // BenUrich
                       messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++,
                           GameDatabase.GetPrototypeId(npc.LastKnownEntityName),
                           new(npc.Position.X - 464, npc.Position.Y, npc.Position.Z + 192), npc.Rotation,
                           repId++, 608, areaid, 608, region.Id, cellid, area, false)));
                       npc = (EntityMarkerPrototype)PopulationMarker.MarkerSet[2]; // JessicaJones
                       messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++,
                           GameDatabase.GetPrototypeId(npc.LastKnownEntityName),
                           new(npc.Position.X - 464, npc.Position.Y, npc.Position.Z + 192), npc.Rotation,
                           repId++, 608, areaid, 608, region.Id, cellid, area, false)));*/

                    messageList.Add(new(EntityHelper.GenerateEntityCreateMessage(entityId++,
                        GameDatabase.GetPrototypeId("Entity/Characters/Vendors/Prototypes/Endgame/TeamSHIELDRepBuffer.prototype"),
                        new(736f, -352f, 177f), new(-2.15625f, 0f, 0f),
                        repId++, 608, areaid, 608, region.Id, cellid, area, false)));

                    break;

            }

            // Put waypoint entity in the game world
            EnterGameWorldArchive waypointEnterGameWorldArchiveData = new(12, region.WaypointPosition, region.WaypointOrientation.X);
            messageList.Add(new(NetMessageEntityEnterGameWorld.CreateBuilder()
                .SetArchiveData(ByteString.CopyFrom(waypointEnterGameWorldArchiveData.Encode()))
                .Build()));

            // Load power collection
            messageList.AddRange(PowerLoader.LoadAvatarPowerCollection(playerData.Avatar).ToList());

            // Dequeue loading screen
            messageList.Add(new(NetMessageDequeueLoadingScreen.DefaultInstance));

            return messageList.ToArray();
        }

        private GameMessage[] LoadLocalPlayerDataMessages(PlayerData playerData)
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

                    foreach (Property property in player.PropertyCollection.List)
                    {
                        switch (property.Enum)
                        {
                            // Unlock starter avatars
                            case PropertyEnum.AvatarUnlock:
                                if ((AvatarUnlockType)property.Value.Get() == AvatarUnlockType.Starter) property.Value.Set((int)AvatarUnlockType.Type3);
                                break;

                            // Configure avatar library
                            case PropertyEnum.AvatarLibraryLevel:
                                property.Value.Set(60);     // Set all avatar levels to 60
                                break;
                            case PropertyEnum.AvatarLibraryCostume:
                                property.Value.Set(0ul);    // Reset the costume to default
                                break;
                            case PropertyEnum.AvatarLibraryTeamUp:
                                property.Value.Set(0ul);    // Clean up team ups
                                break;
                        }
                    }

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
                    if (playerData.Avatar != HardcodedAvatarEntity.BlackCat)
                    {
                        if (baseData.EntityId == (ulong)playerData.Avatar)
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

                    if (baseData.EntityId == (ulong)playerData.Avatar)
                    {
                        // modify avatar data here

                        avatar.PlayerName.Text = playerData.PlayerName;

                        bool hasCostumeCurrent = false;
                        bool hasCharacterLevel = false;
                        bool hasCombatLevel = false;

                        foreach (Property property in avatar.PropertyCollection.List)
                        {
                            switch (property.Enum)
                            {
                                case PropertyEnum.CostumeCurrent:
                                    try
                                    {
                                        property.Value.Set(playerData.CostumeOverride);
                                    }
                                    catch
                                    {
                                        Logger.Warn($"Failed to get costume prototype enum for id {ConfigManager.PlayerData.CostumeOverride}");
                                        property.Value.Set(0ul);
                                    }
                                    hasCostumeCurrent = true;
                                    break;
                                case PropertyEnum.CharacterLevel:
                                    property.Value.Set(60);
                                    hasCharacterLevel = true;
                                    break;
                                case PropertyEnum.CombatLevel:
                                    property.Value.Set(60);
                                    hasCombatLevel = true;
                                    break;
                            }
                        }

                        // Create properties if not found
                        if (hasCostumeCurrent == false) avatar.PropertyCollection.List.Add(new(PropertyEnum.CostumeCurrent, playerData.CostumeOverride));
                        if (hasCharacterLevel == false) avatar.PropertyCollection.List.Add(new(PropertyEnum.CharacterLevel, 60));
                        if (hasCombatLevel == false) avatar.PropertyCollection.List.Add(new(PropertyEnum.CombatLevel, 60));
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
