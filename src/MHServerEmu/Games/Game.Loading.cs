using Gazillion;
using MHServerEmu.Common.Config;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social;
using MHServerEmu.Networking;
using MHServerEmu.PlayerManagement.Accounts.DBModels;

namespace MHServerEmu.Games
{
    public partial class Game
    {
        private GameMessage[] GetBeginLoadingMessages(DBAccount account)
        {
            List<GameMessage> messageList = new();

            // Add server info messages
            messageList.Add(new(NetMessageMarkFirstGameFrame.CreateBuilder()
                .SetCurrentservergametime(161351682950)
                .SetCurrentservergameid(1150669705055451881)
                .SetGamestarttime(1)
                .Build()));

            messageList.Add(new(NetMessageServerVersion.CreateBuilder().SetVersion(ServerManager.GameVersion).Build()));
            messageList.Add(new(LiveTuningManager.LiveTuningData.ToNetMessageLiveTuningUpdate()));
            messageList.Add(new(NetMessageReadyForTimeSync.DefaultInstance));

            // Load local player data
            messageList.AddRange(LoadPlayerEntityMessages(account));
            messageList.Add(new(NetMessageReadyAndLoadedOnGameServer.DefaultInstance));

            // Load region data
            messageList.AddRange(RegionManager.GetRegion(account.Player.Region).GetLoadingMessages(Id));

            // Create a waypoint entity
            messageList.Add(new(EntityManager.Waypoint.ToNetMessageEntityCreate()));

            return messageList.ToArray();
        }

        private GameMessage[] GetFinishLoadingMessages(DBAccount account)
        {
            List<GameMessage> messageList = new();

            Region region = RegionManager.GetRegion(account.Player.Region);

            EnterGameWorldArchive avatarEnterGameWorldArchive = new((ulong)account.Player.Avatar.ToEntityId(), region.EntrancePosition, region.EntranceOrientation.X, 350f);
            messageList.Add(new(NetMessageEntityEnterGameWorld.CreateBuilder()
                .SetArchiveData(avatarEnterGameWorldArchive.Serialize())
                .Build()));

            WorldEntity[] regionEntities = EntityManager.GetWorldEntitiesForRegion(region.Id);
            messageList.AddRange(regionEntities.Select(
                entity => new GameMessage(entity.ToNetMessageEntityCreate())
            ));

            // Put waypoint entity in the game world
            EnterGameWorldArchive waypointEnterGameWorldArchiveData = new(12, region.WaypointPosition, region.WaypointOrientation.X);
            messageList.Add(new(NetMessageEntityEnterGameWorld.CreateBuilder()
                .SetArchiveData(waypointEnterGameWorldArchiveData.Serialize())
                .Build()));

            // Load power collection
            messageList.AddRange(PowerLoader.LoadAvatarPowerCollection(account.Player.Avatar.ToEntityId()).ToList());

            // Dequeue loading screen
            messageList.Add(new(NetMessageDequeueLoadingScreen.DefaultInstance));

            return messageList.ToArray();
        }

        private GameMessage[] LoadPlayerEntityMessages(DBAccount account)
        {
            List<GameMessage> messageList = new();

            // NetMessageLocalPlayer (set local player entity id and game options)
            messageList.Add(new(NetMessageLocalPlayer.CreateBuilder()
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
                .Build()));

            // Create player and avatar entities
            // For now we're using dumped data as base and changing it where necessary

            // Player entity
            Player player = EntityManager.GetDefaultPlayerEntity();

            // Edit player data here

            // Adjust properties
            foreach (var accountAvatar in account.Avatars)
            {
                PropertyParam enumValue = Property.ToParam(PropertyEnum.AvatarLibraryCostume, 1, (PrototypeId)accountAvatar.Prototype);
                var avatarPrototype = (PrototypeId)accountAvatar.Prototype;

                // Set library costumes according to account data
                player.Properties[PropertyEnum.AvatarLibraryCostume, 0, avatarPrototype] = (PrototypeId)accountAvatar.Costume;

                // Set avatar levels to 60
                // Note: setting this to above level 60 sets the prestige level as well
                player.Properties[PropertyEnum.AvatarLibraryLevel, 0, avatarPrototype] = 60;

                // Clean up team ups
                player.Properties[PropertyEnum.AvatarLibraryTeamUp, 0, avatarPrototype] = PrototypeId.Invalid;

                // Unlock start avatars
                var avatarUnlock = (AvatarUnlockType)(int)player.Properties[PropertyEnum.AvatarUnlock, enumValue];
                if (avatarUnlock == AvatarUnlockType.Starter)
                    player.Properties[PropertyEnum.AvatarUnlock, avatarPrototype] = (int)AvatarUnlockType.Type3;
            }
           
            CommunityMember friend = player.Community.CommunityMemberList[0];
            friend.MemberName = "DavidBrevik"; 
            friend.Slots = new AvatarSlotInfo[] { new((PrototypeId)15769648016960461069, (PrototypeId)4881398219179434365, 60, 6) };
            friend.OnlineStatus = CommunityMemberOnlineStatus.Online;
            friend.RegionRef = (PrototypeId)10434222419069901867;
            friend = player.Community.CommunityMemberList[1];
            friend.OnlineStatus = CommunityMemberOnlineStatus.Online;
            friend.MemberName = "TonyStark";
            friend.Slots = new AvatarSlotInfo[] { new((PrototypeId)421791326977791218, (PrototypeId)7150542631074405762, 60, 5) };
            friend.RegionRef = (PrototypeId)RegionPrototypeId.NPEAvengersTowerHUBRegion;

            player.Community.CommunityMemberList.Add(new("Doomsaw", 1, 0, 0, new AvatarSlotInfo[] { new((PrototypeId)17750839636937086083, (PrototypeId)14098108758769669917, 60, 6) }, CommunityMemberOnlineStatus.Online, "", new int[] { 0 }));
            player.Community.CommunityMemberList.Add(new("PizzaTime", 2, 0, 0, new AvatarSlotInfo[] { new((PrototypeId)9378552423541970369, (PrototypeId)6454902525769881598, 60, 5) }, CommunityMemberOnlineStatus.Online, "", new int[] { 0 }));
            player.Community.CommunityMemberList.Add(new("RogueServerEnjoyer", 3, 0, 0, new AvatarSlotInfo[] { new((PrototypeId)1660250039076459846, (PrototypeId)9447440487974639491, 60, 3) }, CommunityMemberOnlineStatus.Online, "", new int[] { 0 }));
            player.Community.CommunityMemberList.Add(new("WhiteQueenXOXO", 4, 0, 0, new AvatarSlotInfo[] { new((PrototypeId)412966192105395660, (PrototypeId)12724924652099869123, 60, 4) }, CommunityMemberOnlineStatus.Online, "", new int[] { 0 }));
            player.Community.CommunityMemberList.Add(new("AlexBond", 5, 0, 0, new AvatarSlotInfo[] { new((PrototypeId)9255468350667101753, (PrototypeId)16813567318560086134, 60, 2) }, CommunityMemberOnlineStatus.Online, "", new int[] { 0 }));
            player.Community.CommunityMemberList.Add(new("Crypto137", 6, 0, 0, new AvatarSlotInfo[] { new((PrototypeId)421791326977791218, (PrototypeId)1195778722002966150, 60, 2) }, CommunityMemberOnlineStatus.Online, "", new int[] { 0 }));
            player.Community.CommunityMemberList.Add(new("yn01", 7, 0, 0, new AvatarSlotInfo[] { new((PrototypeId)12534955053251630387, (PrototypeId)14506515434462517197, 60, 2) }, CommunityMemberOnlineStatus.Online, "", new int[] { 0 }));
            player.Community.CommunityMemberList.Add(new("Gazillion", 8, 0, 0, Array.Empty<AvatarSlotInfo>(), CommunityMemberOnlineStatus.Offline, "", new int[] { 0 }));
            player.Community.CommunityMemberList.Add(new("FriendlyLawyer", 100, 0, 0, new AvatarSlotInfo[] { new((PrototypeId)12394659164528645362, (PrototypeId)2844257346122946366, 99, 1) }, CommunityMemberOnlineStatus.Online, "", new int[] { 2 }));

            messageList.Add(new(player.ToNetMessageEntityCreate()));

            // Avatars
            uint replacementInventorySlot = 100;   // 100 here because no hero occupies slot 100, this to check that we have successfully swapped heroes

            Avatar[] avatars = EntityManager.GetDefaultAvatarEntities();

            foreach (Avatar avatar in avatars)
            {
                // Modify base data
                HardcodedAvatarEntityId playerAvatarEntityId = account.Player.Avatar.ToEntityId();

                if (playerAvatarEntityId != HardcodedAvatarEntityId.BlackCat)
                {
                    if (avatar.BaseData.EntityId == (ulong)playerAvatarEntityId)
                    {
                        replacementInventorySlot = avatar.BaseData.InvLoc.Slot;
                        avatar.BaseData.InvLoc.InventoryPrototypeId = GameDatabase.GetPrototypeRefByName("Entity/Inventory/PlayerInventories/PlayerAvatarInPlay.prototype");
                        avatar.BaseData.InvLoc.Slot = 0;                           // set selected avatar entity inventory slot to 0
                    }
                    else if (avatar.BaseData.EntityId == (ulong)HardcodedAvatarEntityId.BlackCat)
                    {
                        avatar.BaseData.InvLoc.InventoryPrototypeId = GameDatabase.GetPrototypeRefByName("Entity/Inventory/PlayerInventories/PlayerAvatarLibrary.prototype");
                        avatar.BaseData.InvLoc.Slot = replacementInventorySlot;    // set Black Cat slot to the one previously occupied by the hero who replaces her

                        // Black Cat goes last in the hardcoded messages, so this should always be assigned last
                        if (replacementInventorySlot == 100) Logger.Warn("replacementInventorySlot is 100! Check the hardcoded avatar entity data");
                    }
                }

                if (avatar.BaseData.EntityId == (ulong)playerAvatarEntityId)
                {
                    // modify avatar data here

                    avatar.PlayerName.Value = account.PlayerName;

                    avatar.Properties[PropertyEnum.CostumeCurrent] = (PrototypeId)account.CurrentAvatar.Costume;
                    avatar.Properties[PropertyEnum.CharacterLevel] = 60;
                    avatar.Properties[PropertyEnum.CombatLevel] = 60;
                }

                messageList.Add(new(avatar.ToNetMessageEntityCreate()));
            }

            return messageList.ToArray();
        }

        public GameMessage[] GetExitGameMessages()
        {
            return new GameMessage[]
            {
                new(NetMessageBeginExitGame.DefaultInstance),
                new(NetMessageRegionChange.CreateBuilder().SetRegionId(0).SetServerGameId(0).SetClearingAllInterest(true).Build())
            };
        }
    }
}
