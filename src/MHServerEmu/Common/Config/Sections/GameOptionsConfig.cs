namespace MHServerEmu.Common.Config.Sections
{
    public class GameOptionsConfig
    {
        private const string Section = "GameOptions";

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

        public GameOptionsConfig(IniFile configFile)
        {
            TeamUpSystemEnabled = configFile.ReadBool(Section, nameof(TeamUpSystemEnabled));
            AchievementsEnabled = configFile.ReadBool(Section, nameof(AchievementsEnabled));
            OmegaMissionsEnabled = configFile.ReadBool(Section, nameof(OmegaMissionsEnabled));
            VeteranRewardsEnabled = configFile.ReadBool(Section, nameof(VeteranRewardsEnabled));
            MultiSpecRewardsEnabled = configFile.ReadBool(Section, nameof(MultiSpecRewardsEnabled));
            GiftingEnabled = configFile.ReadBool(Section, nameof(GiftingEnabled));
            CharacterSelectV2Enabled = configFile.ReadBool(Section, nameof(CharacterSelectV2Enabled));
            CommunityNewsV2Enabled = configFile.ReadBool(Section, nameof(CommunityNewsV2Enabled));
            LeaderboardsEnabled = configFile.ReadBool(Section, nameof(LeaderboardsEnabled));
            NewPlayerExperienceEnabled = configFile.ReadBool(Section, nameof(NewPlayerExperienceEnabled));
            MissionTrackerV2Enabled = configFile.ReadBool(Section, nameof(MissionTrackerV2Enabled));
            GiftingAccountAgeInDaysRequired = configFile.ReadInt(Section, nameof(GiftingAccountAgeInDaysRequired));
            GiftingAvatarLevelRequired = configFile.ReadInt(Section, nameof(GiftingAvatarLevelRequired));
            GiftingLoginCountRequired = configFile.ReadInt(Section, nameof(GiftingLoginCountRequired));
            InfinitySystemEnabled = configFile.ReadBool(Section, nameof(InfinitySystemEnabled));
            ChatBanVoteAccountAgeInDaysRequired = configFile.ReadInt(Section, nameof(ChatBanVoteAccountAgeInDaysRequired));
            ChatBanVoteAvatarLevelRequired = configFile.ReadInt(Section, nameof(ChatBanVoteAvatarLevelRequired));
            ChatBanVoteLoginCountRequired = configFile.ReadInt(Section, nameof(ChatBanVoteLoginCountRequired));
            IsDifficultySliderEnabled = configFile.ReadBool(Section, nameof(IsDifficultySliderEnabled));
            OrbisTrophiesEnabled = configFile.ReadBool(Section, nameof(OrbisTrophiesEnabled));
        }
    }
}
