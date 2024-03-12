namespace MHServerEmu.Core.Config.Containers
{
    public class GameOptionsConfig : ConfigContainer
    {
        public bool TeamUpSystemEnabled { get; private set; }
        public bool AchievementsEnabled { get; private set; }
        public bool OmegaMissionsEnabled { get; private set; }
        public bool VeteranRewardsEnabled { get; private set; }
        public bool MultiSpecRewardsEnabled { get; private set; }
        public bool GiftingEnabled { get; private set; }
        public bool CharacterSelectV2Enabled { get; private set; }
        public bool CommunityNewsV2Enabled { get; private set; }
        public bool LeaderboardsEnabled { get; private set; }
        public bool NewPlayerExperienceEnabled { get; private set; }
        public bool MissionTrackerV2Enabled { get; private set; }
        public int GiftingAccountAgeInDaysRequired { get; private set; }
        public int GiftingAvatarLevelRequired { get; private set; }
        public int GiftingLoginCountRequired { get; private set; }
        public bool InfinitySystemEnabled { get; private set; }
        public int ChatBanVoteAccountAgeInDaysRequired { get; private set; }
        public int ChatBanVoteAvatarLevelRequired { get; private set; }
        public int ChatBanVoteLoginCountRequired { get; private set; }
        public bool IsDifficultySliderEnabled { get; private set; }
        public bool OrbisTrophiesEnabled { get; private set; }

        public GameOptionsConfig(IniFile configFile) : base(configFile) { }
    }
}
