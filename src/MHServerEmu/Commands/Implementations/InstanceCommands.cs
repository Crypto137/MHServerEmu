using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("instance")]
    [CommandGroupDescription("Commands for managing  region instances.")]
    public class InstanceCommands : CommandGroup
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        [Command("list")]
        [CommandDescription("Lists private instances.")]
        [CommandUsage("instance list")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string List(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;

            RegionManager regionManager = playerConnection.Game.RegionManager;

            CommandHelper.SendMessage(client, "Active Private Instances:");

            foreach (var kvp in playerConnection.WorldView)
            {
                // The region tracked in the world view may have already expired
                Region region = regionManager.GetRegion(kvp.Value);
                if (region == null) continue;

                TimeSpan lifetime = Clock.UnixTime - region.CreatedTime;

                CommandHelper.SendMessage(client, $"{kvp.Key.GetNameFormatted()} ({(int)lifetime.TotalMinutes:D2}:{lifetime:ss})", false);
            }

            return string.Empty;
        }

        [Command("listall")]
        [CommandDescription("Lists all region instances in the current game.")]
        [CommandUsage("instance listall")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string ListAll(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;

            RegionManager regionManager = playerConnection.Game.RegionManager;

            CommandHelper.SendMessage(client, "Active Instances:");

            foreach (Region region in regionManager)
            {
                TimeSpan lifetime = Clock.UnixTime - region.CreatedTime;
                CommandHelper.SendMessage(client, $"{region.PrototypeDataRef.GetNameFormatted()} ({(int)lifetime.TotalMinutes:D2}:{lifetime:ss})", false);
            }

            return string.Empty;
        }

        [Command("reset")]
        [CommandDescription("Resets private instances.")]
        [CommandUsage("instance reset")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Reset(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;

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
