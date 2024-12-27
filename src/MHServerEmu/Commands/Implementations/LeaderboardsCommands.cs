using MHServerEmu.Commands.Attributes;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Leaderboards;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("leaderboards", "Manages leaderboards.", AccountUserLevel.Admin)]
    public class LeaderboardsCommands : CommandGroup
    {
        [Command("jsonreload", "Reload json config file for leaderboards.\nUsage: leaderboards jsonreload")]
        public string JsonReload(string[] @params, FrontendClient client)
        {
            LeaderboardDatabase.Instance.ReloadJsonConfig();
            return "Leaderboards Reloaded";
        }
    }
}
