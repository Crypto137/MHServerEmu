using Gazillion;
using MHServerEmu.Common;
using MHServerEmu.Common.Config;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Regions;
using MHServerEmu.Networking;
using MHServerEmu.PlayerManagement.Accounts.DBModels;

namespace MHServerEmu.Games
{
    public partial class Game
    {
        private GameMessage[] GetBeginLoadingMessages(FrontendClient client)
        {
            DBAccount account = client.Session.Account;
            List<GameMessage> messageList = new();

            // Add server info messages
            messageList.Add(new(NetMessageMarkFirstGameFrame.CreateBuilder()
                .SetCurrentservergametime((ulong)Clock.GameTime.TotalMilliseconds)
                .SetCurrentservergameid(1150669705055451881)
                .SetGamestarttime(1)
                .Build()));

            messageList.Add(new(NetMessageServerVersion.CreateBuilder().SetVersion(ServerManager.GameVersion).Build()));
            messageList.Add(new(LiveTuningManager.LiveTuningData.ToNetMessageLiveTuningUpdate()));
            messageList.Add(new(NetMessageReadyForTimeSync.DefaultInstance));

            // Load local player data
            messageList.AddRange(LoadPlayerEntityMessages(account));
            messageList.Add(new(NetMessageReadyAndLoadedOnGameServer.DefaultInstance));

            // Before changing to the actual destination region the game seems to first change into a transitional region
            messageList.Add(new(NetMessageRegionChange.CreateBuilder()
                .SetRegionId(0)
                .SetServerGameId(0)
                .SetClearingAllInterest(false)
                .Build()));

            messageList.Add(new(NetMessageQueueLoadingScreen.CreateBuilder()
                .SetRegionPrototypeId((ulong)account.Player.Region)
                .Build()));

            // Run region generation as a task
            Task.Run(() => GetRegionAsync(client, account.Player.Region));
            client.AOI.LoadedCellCount = 0;
            client.IsLoading = true;
            return messageList.ToArray();
        }

        private void GetRegionAsync(FrontendClient client, RegionPrototypeId regionPrototypeId)
        {
            Region region = RegionManager.GetRegion(regionPrototypeId);
            EventManager.AddEvent(client, EventEnum.GetRegion, 0, region);
        }

        private GameMessage[] GetFinishLoadingMessages(FrontendClient client)
        {
            DBAccount account = client.Session.Account;
            List<GameMessage> messageList = new();

            Common.Vector3 entrancePosition = new(client.StartPositon);
            Common.Vector3 entranceOrientation = new(client.StartOrientation);
            entrancePosition.Z += 42; // TODO project to floor

            EnterGameWorldArchive avatarEnterGameWorldArchive = new((ulong)account.Player.Avatar.ToEntityId(), entrancePosition, entranceOrientation.Yaw, 350f);
            messageList.Add(new(NetMessageEntityEnterGameWorld.CreateBuilder()
                .SetArchiveData(avatarEnterGameWorldArchive.Serialize())
                .Build()));

            client.AOI.Update(entrancePosition);
            messageList.AddRange(client.AOI.Messages);

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
                    .SetUseServerTimeOffset(true)  // Although originally this was set to false, it needs to be true because auto offset doesn't work past 2019
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
                    .SetPlatformType((int)Platforms.PC))
                .Build()));

            // Create player and avatar entities
            // For now we're using dumped data as base and changing it where necessary

            // Player entity
            Player player = EntityManager.GetDefaultPlayerEntity();
            player.InitializeFromDBAccount(account);
            messageList.Add(new(player.ToNetMessageEntityCreate()));

            // Avatars
            PrototypeId currentAvatarId = (PrototypeId)account.CurrentAvatar.Prototype;
            ulong avatarEntityId = player.BaseData.EntityId + 1;
            ulong avatarRepId = player.PartyId.ReplicationId + 1;

            List<Avatar> avatarList = new();
            uint librarySlot = 0; 
            foreach (PrototypeId avatarId in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(AvatarPrototype),
                PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly))
            {
                if (avatarId == (PrototypeId)6044485448390219466) continue;   //zzzBrevikOLD.prototype

                Avatar avatar = new(avatarEntityId, avatarRepId);
                avatarEntityId++;
                avatarRepId += 2;

                avatar.InitializeFromDBAccount(avatarId, account);

                avatar.BaseData.InvLoc = (avatarId == currentAvatarId)
                    ? new(player.BaseData.EntityId, (PrototypeId)9555311166682372646, 0)                // Entity/Inventory/PlayerInventories/PlayerAvatarInPlay.prototype
                    : new(player.BaseData.EntityId, (PrototypeId)5235960671767829134, librarySlot++);   // Entity/Inventory/PlayerInventories/PlayerAvatarLibrary.prototype

                avatarList.Add(avatar);
            }

            messageList.AddRange(avatarList.Select(avatar => new GameMessage(avatar.ToNetMessageEntityCreate())));

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
