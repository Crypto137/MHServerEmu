using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions;
using MHServerEmu.Grouping;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("instance", "Provides commands for managing private region instances.")]
    public class InstanceCommands : CommandGroup
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        [Command("list", "Lists private instances.\nUsage: instance list", AccountUserLevel.User)]
        public string List(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            if (CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection) == false)
                return string.Empty;

            RegionManager regionManager = playerConnection.Game.RegionManager;

            ChatHelper.SendMetagameMessage(client, "Active Private Instances:");

            foreach (var kvp in playerConnection.WorldView)
            {
                // The region tracked in the world view may have already expired
                Region region = regionManager.GetRegion(kvp.Value);
                if (region == null) continue;

                TimeSpan lifetime = Clock.UnixTime - region.CreatedTime;

                ChatHelper.SendMetagameMessage(client, $"{kvp.Key.GetNameFormatted()} ({(int)lifetime.TotalMinutes:D2}:{lifetime:ss})", false);
            }

            return string.Empty;
        }

        [Command("listall", "Lists all region instances in the current game.\nUsage: instance listall", AccountUserLevel.User)]
        public string ListAll(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            if (CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection) == false)
                return string.Empty;

            RegionManager regionManager = playerConnection.Game.RegionManager;

            ChatHelper.SendMetagameMessage(client, "Active Instances:");

            foreach (Region region in regionManager)
            {
                TimeSpan lifetime = Clock.UnixTime - region.CreatedTime;
                ChatHelper.SendMetagameMessage(client, $"{region.PrototypeDataRef.GetNameFormatted()} ({(int)lifetime.TotalMinutes:D2}:{lifetime:ss})", false);
            }

            return string.Empty;
        }

        [Command("reset", "Resets private instances.\nUsage: instance reset", AccountUserLevel.User)]
        public string Reset(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            if (CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection) == false)
                return string.Empty;

            RegionManager regionManager = playerConnection.Game.RegionManager;

            int numReset = 0;
            foreach (var kvp in playerConnection.WorldView)
            {
                Region region = regionManager.GetRegion(kvp.Value);
                if (region == null) continue;

                // Do no reset the region the player is currently in
                if (region == playerConnection.Player.GetRegion())
                    continue;

                // We should not be having public regions in the world view with our current implementation (this may change later)
                if (region.IsPublic)
                {
                    Logger.Warn($"Reset(): Found public region {region} in the world view for player {playerConnection.Player}");
                    continue;
                }

                region.RequestShutdown();
                playerConnection.WorldView.RemoveRegion(kvp.Key);
                numReset++;
            }

            return $"Reset {numReset} private instance(s).";
        }
    }
}
