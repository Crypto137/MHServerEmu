
using Gazillion;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Common
{
    public class AdminCommandManager
    {
        private Game _game;
        private AdminFlags _flags;

        public AdminCommandManager(Game game) 
        { 
            _game = game;
            _flags = AdminFlags.LocomotionSync;
        }

        public bool TestAdminFlag(AdminFlags flag)
        {
            return _flags.HasFlag(flag);
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
    }

    [Flags]
    public enum AdminFlags
    {
        LocomotionSync = 1 << 1,
    }
}
