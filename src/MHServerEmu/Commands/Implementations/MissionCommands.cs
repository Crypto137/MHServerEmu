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
            if (client == null)
                return "You can only invoke this command from the game.";

            if (@params.Length == 0)
                return "Invalid arguments. Type 'help mission info' to get help.";

            if ((@params.Length > 0 && Enum.TryParse(@params[0], true, out PrototypeId missionPrototypeId)) == false)
                return "No valid PrototypeId found. Type 'help mission info' to get help.";

            string errorMessage = GetMission(client, missionPrototypeId, out Mission mission);
            if (errorMessage != null) return errorMessage;

            return mission.ToString();
        }

        [Command("reset", "Usage: mission reset prototypeId.", AccountUserLevel.Admin)]
        public string Reset(string[] @params, FrontendClient client)
        {
            if (client == null)
                return "You can only invoke this command from the game.";

            if (@params.Length == 0)
                return "Invalid arguments. Type 'help mission reset' to get help.";

            if ((@params.Length > 0 && Enum.TryParse(@params[0], true, out PrototypeId missionPrototypeId)) == false)
                return "No valid PrototypeId found. Type 'help mission reset' to get help.";

            string errorMessage = GetMission(client, missionPrototypeId, out Mission mission);
            if (errorMessage != null) return errorMessage;

            mission.RestartMission();

            return $"{missionPrototypeId} restarted";
        }

        private string GetMission(FrontendClient client, PrototypeId missionPrototypeId, out Mission mission)
        {
            mission = null;

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);
            if (game == null) return "Game not found";
            if (playerConnection == null) return "PlayerConnection not found";

            mission = playerConnection.Player?.MissionManager?.FindMissionByDataRef(missionPrototypeId);
            if (mission != null) return null;

            Region region = playerConnection.Player.GetRegion();
            if (region == null) return "No region found.";

            mission = region.MissionManager?.FindMissionByDataRef(missionPrototypeId);
            if (mission == null) return $"No mission found for {playerConnection.Player}";

            return null;
        }
    }
}
