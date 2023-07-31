using System;

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
            TeamUpSystemEnabled = configFile.ReadBool(Section, "TeamUpSystemEnabled");
            AchievementsEnabled = configFile.ReadBool(Section, "AchievementsEnabled");
            OmegaMissionsEnabled = configFile.ReadBool(Section, "OmegaMissionsEnabled");
            VeteranRewardsEnabled = configFile.ReadBool(Section, "VeteranRewardsEnabled");
            MultiSpecRewardsEnabled = configFile.ReadBool(Section, "MultiSpecRewardsEnabled");
            GiftingEnabled = configFile.ReadBool(Section, "GiftingEnabled");
            CharacterSelectV2Enabled = configFile.ReadBool(Section, "CharacterSelectV2Enabled");
            CommunityNewsV2Enabled = configFile.ReadBool(Section, "CommunityNewsV2Enabled");
            LeaderboardsEnabled = configFile.ReadBool(Section, "LeaderboardsEnabled");
            NewPlayerExperienceEnabled = configFile.ReadBool(Section, "NewPlayerExperienceEnabled");
            MissionTrackerV2Enabled = configFile.ReadBool(Section, "MissionTrackerV2Enabled");
            GiftingAccountAgeInDaysRequired = configFile.ReadInt(Section, "GiftingAccountAgeInDaysRequired");
            GiftingAvatarLevelRequired = configFile.ReadInt(Section, "GiftingAvatarLevelRequired");
            GiftingLoginCountRequired = configFile.ReadInt(Section, "GiftingLoginCountRequired");
            InfinitySystemEnabled = configFile.ReadBool(Section, "InfinitySystemEnabled");
            ChatBanVoteAccountAgeInDaysRequired = configFile.ReadInt(Section, "ChatBanVoteAccountAgeInDaysRequired");
            ChatBanVoteAvatarLevelRequired = configFile.ReadInt(Section, "ChatBanVoteAvatarLevelRequired");
            ChatBanVoteLoginCountRequired = configFile.ReadInt(Section, "ChatBanVoteLoginCountRequired");
            IsDifficultySliderEnabled = configFile.ReadBool(Section, "IsDifficultySliderEnabled");
            OrbisTrophiesEnabled = configFile.ReadBool(Section, "OrbisTrophiesEnabled");
        }
    }
}
