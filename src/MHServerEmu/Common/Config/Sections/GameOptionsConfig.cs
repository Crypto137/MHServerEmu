using System;

namespace MHServerEmu.Common.Config.Sections
{
    public class GameOptionsConfig
    {
        public bool TeamUpSystemEnabled { get; }
        public bool AchievementsEnabled { get; }
        public bool OmegaMissionsEnabled { get; }
        public bool VeteranRewardsEnabled { get; }
        public bool MultiSpecRewardsEnabled { get; }
        public bool GiftingEnabled { get; }
        public bool CharacterSelectV2Enabled { get; }
        public bool CommunityNewsV2Enabled { get; }
        public bool LeaderboardsEnabled { get; }
        public bool NewPlayerExperienceEnabled { get; }
        public bool MissionTrackerV2Enabled { get; }
        public int GiftingAccountAgeInDaysRequired { get; }
        public int GiftingAvatarLevelRequired { get; }
        public int GiftingLoginCountRequired { get; }
        public bool InfinitySystemEnabled { get; }
        public int ChatBanVoteAccountAgeInDaysRequired { get; }
        public int ChatBanVoteAvatarLevelRequired { get; }
        public int ChatBanVoteLoginCountRequired { get; }
        public bool IsDifficultySliderEnabled { get; }
        public bool OrbisTrophiesEnabled { get; }

        public GameOptionsConfig(bool teamUpSystemEnabled, bool achievementsEnabled, bool omegaMissionsEnabled, bool veteranRewardsEnabled, bool multiSpecRewardsEnabled,
            bool giftingEnabled, bool characterSelectV2Enabled, bool communityNewsV2Enabled, bool leaderboardsEnabled, bool newPlayerExperienceEnabled,
            bool missionTrackerV2Enabled, int giftingAccountAgeInDaysRequired, int giftingAvatarLevelRequired, int giftingLoginCountRequired, bool infinitySystemEnabled,
            int chatBanVoteAccountAgeInDaysRequired, int chatBanVoteAvatarLevelRequired, int chatBanVoteLoginCountRequired, bool isDifficultySliderEnabled, bool orbisTrophiesEnabled)
        {
            TeamUpSystemEnabled = teamUpSystemEnabled;
            AchievementsEnabled = achievementsEnabled;
            OmegaMissionsEnabled = omegaMissionsEnabled;
            VeteranRewardsEnabled = veteranRewardsEnabled;
            MultiSpecRewardsEnabled = multiSpecRewardsEnabled;
            GiftingEnabled = giftingEnabled;
            CharacterSelectV2Enabled = characterSelectV2Enabled;
            CommunityNewsV2Enabled = communityNewsV2Enabled;
            LeaderboardsEnabled = leaderboardsEnabled;
            NewPlayerExperienceEnabled = newPlayerExperienceEnabled;
            MissionTrackerV2Enabled = missionTrackerV2Enabled;
            GiftingAccountAgeInDaysRequired = giftingAccountAgeInDaysRequired;
            GiftingAvatarLevelRequired = giftingAvatarLevelRequired;
            GiftingLoginCountRequired = giftingLoginCountRequired;
            InfinitySystemEnabled = infinitySystemEnabled;
            ChatBanVoteAccountAgeInDaysRequired = chatBanVoteAccountAgeInDaysRequired;
            ChatBanVoteAvatarLevelRequired = chatBanVoteAvatarLevelRequired;
            ChatBanVoteLoginCountRequired = chatBanVoteLoginCountRequired;
            IsDifficultySliderEnabled = isDifficultySliderEnabled;
            OrbisTrophiesEnabled = orbisTrophiesEnabled;
        }
    }
}
