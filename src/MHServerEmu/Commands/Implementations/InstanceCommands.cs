using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("instance")]
    [CommandGroupDescription("Commands for managing region instances.")]
    public class InstanceCommands : CommandGroup
    {
        [Command("list")]
        [CommandDescription("Lists instances in the player's WorldView.")]
        [CommandUsage("instance list")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string List(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;

            CommandHelper.SendMessage(client, "Reserved Instances:");
            foreach ((PrototypeId regionProtoRef, ulong regionId) in playerConnection.WorldView)
                CommandHelper.SendMessage(client, $"{regionProtoRef.GetNameFormatted()} (0x{regionId:X})", false);

            return string.Empty;
        }

        [Command("reset")]
        [CommandDescription("Resets private instances in the player's WorldView.")]
        [CommandUsage("instance reset")]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandUserLevel(AccountUserLevel.Admin)]
        public string Reset(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;

            int numReset = 0;
            foreach ((PrototypeId regionProtoRef, ulong regionId) in playerConnection.WorldView)
            {
                RegionPrototype regionProto = regionProtoRef.As<RegionPrototype>();
                if (regionProto == null || regionProto.IsPublic)
                    continue;

                ServiceMessage.RequestRegionShutdown requestShutdown = new(regionId);
                ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, requestShutdown);

                numReset++;
            }

            return $"Requested {numReset} private instance(s) to be reset.";
        }
    }
}
