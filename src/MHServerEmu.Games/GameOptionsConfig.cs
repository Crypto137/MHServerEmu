using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games
{
    public class GameOptionsConfig : ConfigContainer
    {
        public bool TeamUpSystemEnabled { get; private set; } = true;
        public bool AchievementsEnabled { get; private set; } = true;
        public bool OmegaMissionsEnabled { get; private set; } = true;
        public bool VeteranRewardsEnabled { get; private set; } = true;
        public bool MultiSpecRewardsEnabled { get; private set; } = true;
        public bool GiftingEnabled { get; private set; } = true;
        public bool CharacterSelectV2Enabled { get; private set; } = true;
        public bool CommunityNewsV2Enabled { get; private set; } = true;
        public bool LeaderboardsEnabled { get; private set; } = true;
        public bool NewPlayerExperienceEnabled { get; private set; } = true;
        public bool MissionTrackerV2Enabled { get; private set; } = true;
        public int GiftingAccountAgeInDaysRequired { get; private set; } = 7;
        public int GiftingAvatarLevelRequired { get; private set; } = 20;
        public int GiftingLoginCountRequired { get; private set; } = 5;
        public bool InfinitySystemEnabled { get; private set; } = true;
        public int ChatBanVoteAccountAgeInDaysRequired { get; private set; } = 7;
        public int ChatBanVoteAvatarLevelRequired { get; private set; } = 20;
        public int ChatBanVoteLoginCountRequired { get; private set; } = 5;
        public bool IsDifficultySliderEnabled { get; private set; } = true;
        public bool OrbisTrophiesEnabled { get; private set; } = true;

        /// <summary>
        /// Converts this <see cref="GameOptionsConfig"/> instance to <see cref="NetStructGameOptions"/>.
        /// </summary>
        public NetStructGameOptions ToProtobuf()
        {
            return NetStructGameOptions.CreateBuilder()
                .SetTeamUpSystemEnabled(TeamUpSystemEnabled)
                .SetAchievementsEnabled(AchievementsEnabled)
                .SetOmegaMissionsEnabled(OmegaMissionsEnabled)
                .SetVeteranRewardsEnabled(VeteranRewardsEnabled)
                .SetMultiSpecRewardsEnabled(MultiSpecRewardsEnabled)
                .SetGiftingEnabled(GiftingEnabled)
                .SetCharacterSelectV2Enabled(CharacterSelectV2Enabled)
                .SetCommunityNewsV2Enabled(CommunityNewsV2Enabled)
#if GAME_VERSION_1_48
                .SetDynamicCombatLevelEnabled(true)
#endif
                .SetLeaderboardsEnabled(LeaderboardsEnabled)
                .SetNewPlayerExperienceEnabled(NewPlayerExperienceEnabled)
                .SetServerTimeOffsetUTC(-7)
                .SetUseServerTimeOffset(true)  // Although originally this was set to false, it needs to be true because auto offset doesn't work past 2019
#if GAME_VERSION_1_48
                .SetMissionTrackerV2Enabled(false) // V48_NOTE: Enabling this crashes the client.
#else
                .SetMissionTrackerV2Enabled(MissionTrackerV2Enabled)
#endif
                .SetGiftingAccountAgeInDaysRequired(GiftingAccountAgeInDaysRequired)
                .SetGiftingAvatarLevelRequired(GiftingAvatarLevelRequired)
                .SetGiftingLoginCountRequired(GiftingLoginCountRequired)
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                .SetInfinitySystemEnabled(InfinitySystemEnabled)
#endif
                .SetChatBanVoteAccountAgeInDaysRequired(ChatBanVoteAccountAgeInDaysRequired)
                .SetChatBanVoteAvatarLevelRequired(ChatBanVoteAvatarLevelRequired)
                .SetChatBanVoteLoginCountRequired(ChatBanVoteLoginCountRequired)
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                .SetIsDifficultySliderEnabled(IsDifficultySliderEnabled)
                .SetOrbisTrophiesEnabled(OrbisTrophiesEnabled)
                .SetPlatformType((int)Platforms.PC)
#endif
#if GAME_VERSION_1_53
                .SetMetaGamePanelV2Enabled(true)
#endif
                .Build();
        }
    }
}
