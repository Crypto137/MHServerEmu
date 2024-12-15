using MHServerEmu.Commands.Attributes;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.MetaGames.GameModes;
using MHServerEmu.Games.MetaGames.MetaStates;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("metagame", "Provides commands for metagame.", AccountUserLevel.Admin)]
    public class MetaGame : CommandGroup
    {
        public enum ChangeEventType
        {
            Next,
            Stop
        }

        [Command("event", "Change current event.\nUsage: metagame event [next|stop]")]
        public string Event(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            var player = playerConnection.Player;
            var region = player.GetRegion();
            if (region == null) return "Player.GetRegion() failed.";
            if (region.MetaGames.Count == 0) return "MetaGames not found";
            var metagame = player.Game.EntityManager.GetEntity<Games.MetaGames.MetaGame>(region.MetaGames[0]);
            if (region.MetaGames.Count == 0) return "MetaGames not exist";

            if ((@params.Length > 0 && Enum.TryParse(@params[0], true, out ChangeEventType type)) == false)
                type = ChangeEventType.Stop;   // Default Stop            

            if (metagame.CurrentMode is not MetaGameStateMode stateMode) return "MetaGameStateMode is not current";

            PrototypeId stateRef = stateMode.GetCurrentStateRef();
            if (type == ChangeEventType.Stop)
            {
                if (stateRef != PrototypeId.Invalid && metagame.GetState(stateRef) is MetaStateMissionSequencer sequencer)
                    sequencer.OnMissionComplete();
            }
            else
            {
                stateMode.OnStatePickInterval();
                stateRef = stateMode.GetCurrentStateRef();
            }

            return $"Event {type} {stateRef.GetNameFormatted()}";
        }
    }
}
