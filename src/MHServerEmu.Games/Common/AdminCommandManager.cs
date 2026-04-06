using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Common
{
    [Flags]
    public enum AdminFlags : ulong              // Descriptions from 1.0.4932.0:
    {
        LocomotionSync              = 1 << 1,   // Toggles experimental locomotion sync mode
        CurrencyItemsConvertToggle  = 1 << 47   // Turns on/off conversion of Currency Items to Currency properties
    }

    public delegate string AdminCommandHandler(Player player);

    public class AdminCommandManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private Game _game;
        private AdminFlags _flags;

        public AdminCommandManager(Game game) 
        { 
            _game = game;
            _flags = AdminFlags.LocomotionSync | AdminFlags.CurrencyItemsConvertToggle;
        }

        public bool TestAdminFlag(AdminFlags flag)
        {
            return _flags.HasFlag(flag);
        }

        public void OnAdminCommand(Player player, NetMessageAdminCommand adminCommand)
        {
            (AdminCommandHandler handler, AvailableBadges requiredBadge) = AdminCommands.Get(adminCommand.Command);
            if (handler == null)
            {
                string unhandledResponse = $"Unhandled server admin command: {adminCommand.Command}";
                Logger.Warn(unhandledResponse);
                SendAdminCommandResponse(player, unhandledResponse);
                return;
            }

            if (requiredBadge != 0 && player.HasBadge(requiredBadge) == false)
            {
                // Naughty hacker here
                Logger.Warn($"OnAdminCommand(): Unauthorized admin command [{adminCommand.Command}] received from {player}");
                SendAdminCommandResponse(player, $"{player.GetName()} is not in the sudoers file. This incident will be reported.");
                return;
            }

            string response = handler.Invoke(player);

            if (string.IsNullOrWhiteSpace(response) == false)
                SendAdminCommandResponse(player, response);
        }

        public static void SendAdminCommandResponse(Player player, string response)
        {
            SendAdminCommandResponse(player.PlayerConnection, response);
        }

        public static void SendAdminCommandResponse(PlayerConnection playerConnection, string response)
        {
            playerConnection.SendMessage(NetMessageAdminCommandResponse.CreateBuilder()
                .SetResponse(response)
                .Build());
        }

        public static void SendAdminCommandResponseSplit(PlayerConnection playerConnection, string response)
        {
            foreach (string line in response.Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                SendAdminCommandResponse(playerConnection, line);
        }

        public static void SendVerify(PlayerConnection playerConnection, string message)
        {
            playerConnection.SendMessage(NetMessageVerifyOnClient.CreateBuilder()
                .SetMessage($"(Server) {message}")
                .Build());
        }
    }
}
