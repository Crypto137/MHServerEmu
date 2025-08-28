using System.Globalization;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Grouping;
using MHServerEmu.PlayerManagement;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("client")]
    [CommandGroupDescription("Commands for interacting with connected clients.")]
    [CommandGroupUserLevel(AccountUserLevel.Moderator)]
    public class ClientCommands : CommandGroup
    {
        private static readonly char[] HexPrefix = ['0', 'x'];

        [Command("info")]
        [CommandDescription("Prints information about the client with the specified session id.")]
        [CommandUsage("client info [sessionId]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandParamCount(1)]
        public string Info(string[] @params, NetClient client)
        {
            if (ulong.TryParse(@params[0].TrimStart(HexPrefix), NumberStyles.HexNumber, null, out ulong sessionId) == false)
                return $"Failed to parse sessionId {@params[0]}";

            var playerManager = ServerManager.Instance.GetGameService(GameServiceType.PlayerManager) as PlayerManagerService;
            if (playerManager == null)
                return "Failed to connect to the player manager.";

            if (playerManager.TryGetSession(sessionId, out ClientSession session) == false)
                return $"SessionId 0x{sessionId:X} not found.";

            return session.GetClientInfo();
        }

        [Command("kick")]
        [CommandDescription("Disconnects the client with the specified player name.")]
        [CommandUsage("client kick [playerName]")]
        [CommandUserLevel(AccountUserLevel.Moderator)]
        [CommandParamCount(1)]
        public string Kick(string[] @params, NetClient client)
        {
            var groupingManager = ServerManager.Instance.GetGameService(GameServiceType.GroupingManager) as GroupingManagerService;
            if (groupingManager == null)
                return "Failed to connect to the grouping manager.";

            if (groupingManager.ClientManager.TryGetClient(@params[0], out IFrontendClient target) == false)
                return $"Player {@params[0]} not found.";

            target.Disconnect();
            return $"Kicked {target}.";
        }
    }
}
