using Gazillion;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("pvp")]
    [CommandGroupDescription("Commands related to PvP matchmaking.")]
    [CommandGroupUserLevel(AccountUserLevel.User)]
    public class PvPCommands : CommandGroup
    {
        private static readonly PrototypeId RegionRef =
            GameDatabase.GetPrototypeRefByName("Metagame/DefenderPvP/Regions/PvPDefenderTier5Region.prototype");

        private static readonly PrototypeId DifficultyRef =
            GameDatabase.GetPrototypeRefByName("Difficulty/Tiers/Tier1Normal.prototype");

        private string JoinQueue(int size, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            var player = playerConnection.Player;
            if (player == null) return "Player not found.";

            if (LiveTuningManager.GetLiveGlobalTuningVar(GlobalTuningVar.eGTV_PVPEnabled) == 0f)
            {
                player.SendBannerMessage(GameDatabase.UIGlobalsPrototype.MessagePvPDisabledPortalFail);
                return string.Empty;
            }

            bool isInParty = player.Party != null && player.Party.NumMembers > 1;
            var command = isInParty
                ? RegionRequestQueueCommandVar.eRRQC_AddToQueueParty
                : RegionRequestQueueCommandVar.eRRQC_AddToQueueSolo;

            int limit = (size == 5) ? -1 : size;

            if (player.MatchQueueStatus.TryRegionRequestCommand(RegionRef, DifficultyRef, 0, command, limit) == false)
                return "Matchmaking is currently not available.";
            
            return $"Queued for {size}v{size} PvP! ({(isInParty ? "Party" : "Solo")})";
        }

        [Command("1v1")]
        [CommandDescription("Join 1v1 PvP queue.")]
        [CommandUsage("pvp 1v1")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Queue1v1(string[] @params, NetClient client) => JoinQueue(1, client);

        [Command("2v2")]
        [CommandDescription("Join 2v2 PvP queue.")]
        [CommandUsage("pvp 2v2")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Queue2v2(string[] @params, NetClient client) => JoinQueue(2, client);

        [Command("3v3")]
        [CommandDescription("Join 3v3 PvP queue.")]
        [CommandUsage("pvp 3v3")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Queue3v3(string[] @params, NetClient client) => JoinQueue(3, client);

        [Command("4v4")]
        [CommandDescription("Join 4v4 PvP queue.")]
        [CommandUsage("pvp 4v4")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Queue4v4(string[] @params, NetClient client) => JoinQueue(4, client);

        [Command("5v5")]
        [CommandDescription("Join 5v5 PvP queue.")]
        [CommandUsage("pvp 5v5")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Queue5v5(string[] @params, NetClient client) => JoinQueue(5, client);
    }
}
