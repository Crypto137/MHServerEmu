using MHServerEmu.Common.Config.Sections;

namespace MHServerEmu.Common.Config
{
    public static class ConfigManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static bool IsInitialized { get; private set; }

        public static ServerConfig Server { get; }
        public static PlayerDataConfig PlayerData { get; }
        public static FrontendConfig Frontend { get; }
        public static GroupingManagerConfig GroupingManager { get; }
        public static GameOptionsConfig GameOptions { get; }

        static ConfigManager()
        {
            string path = $"{Directory.GetCurrentDirectory()}\\Config.ini";

            if (File.Exists(path))
            {
                IniFile configFile = new(path);

                Server = LoadServerConfig(configFile);
                PlayerData = LoadPlayerDataConfig(configFile);
                Frontend = LoadFrontendConfig(configFile);
                GroupingManager = LoadGroupingManagerConfig(configFile);
                GameOptions = LoadGameOptionsConfig(configFile);

                IsInitialized = true;
            }
            else
            {
                Server = new();         // initialize default server config so that logging still works
                Logger.Fatal("Failed to initialize config");
                IsInitialized = false;
            }
        }

        private static ServerConfig LoadServerConfig(IniFile configFile)
        {
            string section = "Server";

            bool enableTimestamps = configFile.ReadBool(section, "EnableTimestamps");

            return new(enableTimestamps);
        }

        private static PlayerDataConfig LoadPlayerDataConfig(IniFile configFile)
        {
            string section = "PlayerData";

            string startingRegion = configFile.ReadString(section, "StartingRegion");
            string startingAvatar = configFile.ReadString(section, "StartingAvatar");

            return new(startingRegion, startingAvatar);
        }

        private static FrontendConfig LoadFrontendConfig(IniFile configFile)
        {
            string section = "Frontend";

            bool simulateQueue = configFile.ReadBool(section, "SimulateQueue");
            ulong queuePlaceInLine = (ulong)configFile.ReadInt(section, "QueuePlaceInLine");
            ulong queueNumberOfPlayersInLine = (ulong)configFile.ReadInt(section, "QueueNumberOfPlayersInLine");

            return new(simulateQueue, queuePlaceInLine, queueNumberOfPlayersInLine);
        }

        private static GroupingManagerConfig LoadGroupingManagerConfig(IniFile configFile)
        {
            string section = "GroupingManager";

            string motdPlayerName = configFile.ReadString(section, "MotdPlayerName");
            string motdText = configFile.ReadString(section, "MotdText");
            int motdPrestigeLevel = configFile.ReadInt(section, "MotdPrestigeLevel");

            return new(motdPlayerName, motdText, motdPrestigeLevel);
        }

        private static GameOptionsConfig LoadGameOptionsConfig(IniFile configFile)
        {
            string section = "GameOptions";

            // this one is a chonker lol
            bool teamUpSystemEnabled = configFile.ReadBool(section, "TeamUpSystemEnabled");
            bool achievementsEnabled = configFile.ReadBool(section, "AchievementsEnabled");
            bool omegaMissionsEnabled = configFile.ReadBool(section, "OmegaMissionsEnabled");
            bool veteranRewardsEnabled = configFile.ReadBool(section, "VeteranRewardsEnabled");
            bool multiSpecRewardsEnabled = configFile.ReadBool(section, "MultiSpecRewardsEnabled");
            bool giftingEnabled = configFile.ReadBool(section, "GiftingEnabled");
            bool characterSelectV2Enabled = configFile.ReadBool(section, "CharacterSelectV2Enabled");
            bool communityNewsV2Enabled = configFile.ReadBool(section, "CommunityNewsV2Enabled");
            bool leaderboardsEnabled = configFile.ReadBool(section, "LeaderboardsEnabled");
            bool newPlayerExperienceEnabled = configFile.ReadBool(section, "NewPlayerExperienceEnabled");
            bool missionTrackerV2Enabled = configFile.ReadBool(section, "MissionTrackerV2Enabled");
            int giftingAccountAgeInDaysRequired = configFile.ReadInt(section, "GiftingAccountAgeInDaysRequired");
            int giftingAvatarLevelRequired = configFile.ReadInt(section, "GiftingAvatarLevelRequired");
            int giftingLoginCountRequired = configFile.ReadInt(section, "GiftingLoginCountRequired");
            bool infinitySystemEnabled = configFile.ReadBool(section, "InfinitySystemEnabled");
            int chatBanVoteAccountAgeInDaysRequired = configFile.ReadInt(section, "ChatBanVoteAccountAgeInDaysRequired");
            int chatBanVoteAvatarLevelRequired = configFile.ReadInt(section, "ChatBanVoteAvatarLevelRequired");
            int chatBanVoteLoginCountRequired = configFile.ReadInt(section, "ChatBanVoteLoginCountRequired");
            bool isDifficultySliderEnabled = configFile.ReadBool(section, "IsDifficultySliderEnabled");
            bool orbisTrophiesEnabled = configFile.ReadBool(section, "OrbisTrophiesEnabled");

            return new(teamUpSystemEnabled, achievementsEnabled, omegaMissionsEnabled, veteranRewardsEnabled, multiSpecRewardsEnabled,
                giftingEnabled, characterSelectV2Enabled, communityNewsV2Enabled, leaderboardsEnabled, newPlayerExperienceEnabled,
                missionTrackerV2Enabled, giftingAccountAgeInDaysRequired, giftingAvatarLevelRequired, giftingLoginCountRequired, infinitySystemEnabled,
                chatBanVoteAccountAgeInDaysRequired, chatBanVoteAvatarLevelRequired, chatBanVoteLoginCountRequired, isDifficultySliderEnabled, orbisTrophiesEnabled);
        }
    }
}
