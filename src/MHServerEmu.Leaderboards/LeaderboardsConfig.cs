using MHServerEmu.Core.Config;

namespace MHServerEmu.Leaderboards
{
    public class LeaderboardsConfig : ConfigContainer
    {
        public string FileName { get; private set; } = "Leaderboards.db";
        public string JsonConfig { get; private set; } = "LeaderboardsConfig.json";
        public int AutoSaveIntervalMinutes { get; private set; } = 5;
    }
}
