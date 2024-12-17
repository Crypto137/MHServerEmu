using MHServerEmu.Core.Config;

namespace MHServerEmu.Leaderboards
{
    public class LeaderboardsConfig : ConfigContainer
    {
        public string FileName { get; private set; } = "Leaderboards.db";
    }
}
