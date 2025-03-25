using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Extensions;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions;
using static MHServerEmu.Commands.Implementations.DebugCommands;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("mission", "commands about missions", AccountUserLevel.User)]
    public class MissionCommands : CommandGroup
    {
        [Command("debug", "Usage: mission debug [on|off].", AccountUserLevel.Admin)]
        public string Debug(string[] @params, FrontendClient client)
        {
            if ((@params.Length > 0 && Enum.TryParse(@params[0], true, out Switch flags)) == false)
                flags = Switch.Off;   // Default Off

            MissionManager.Debug = (flags == Switch.On) ? true : false;

            return $"Mission Log [{flags}]";
        }

        [Command("info", "Usage: mission info prototypeId.", AccountUserLevel.Admin)]
        public string Info(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            if (@params.Length == 0) return "Invalid arguments. Type 'help mission info' to get help.";

            if ((@params.Length > 0 && Enum.TryParse(@params[0], true, out PrototypeId missionPrototypeId)) == false)
                return "No valid PrototypeId found. Type 'help mission info' to get help.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);
            if (game == null) return "Game not found";
            if (playerConnection == null) return "PlayerConnection not found";

            Avatar avatar = playerConnection.Player.CurrentAvatar;
            if (avatar == null || avatar.IsInWorld == false)
                return "Avatar not found.";

            Region region = avatar.Region;
            if (region == null) return "No region found.";

            MissionManager missionManager = region.MissionManager;
            if (missionManager == null) return "No MissionManager found.";

            MissionPrototype missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionPrototypeId);
            if (missionProto == null) return "No valid Mission PrototypeId found. Type 'help mission info' to get help.";

            Mission mission = MissionManager.FindMissionForPlayer(playerConnection.Player, missionPrototypeId);
            if (mission == null) return $"No mission found for {playerConnection.Player}";

            return mission.ToString();
        }
    }
}
