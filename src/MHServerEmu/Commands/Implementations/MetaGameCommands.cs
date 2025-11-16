using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.MetaGames.GameModes;
using MHServerEmu.Games.MetaGames.MetaStates;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("metagame")]
    [CommandGroupDescription("Commands related to the MetaGame system.")]
    [CommandGroupUserLevel(AccountUserLevel.Admin)]
    public class MetaGameCommands : CommandGroup
    {
        public enum ChangeEventType
        {
            Next,
            Stop
        }

        [Command("event")]
        [CommandDescription("Changes current event. Defaults to stop.")]
        [CommandUsage("metagame event [next|stop]")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Event(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            var player = playerConnection.Player;
            var region = player.GetRegion();
            if (region == null) return "Player.GetRegion() failed.";
            if (region.MetaGames.Count == 0) return "MetaGames not found";
            var metagame = player.Game.EntityManager.GetEntity<Games.MetaGames.MetaGame>(region.MetaGames[0]);
            if (region.MetaGames.Count == 0) return "MetaGames not exist";

            if ((@params.Length > 0 && Enum.TryParse(@params[0], true, out ChangeEventType type)) == false)
                type = ChangeEventType.Stop;   // Default Stop            

            if (type == ChangeEventType.Next && metagame.MetaStates.Count > 0 &&
                metagame.MetaStates[0] is MetaStateMissionProgression progression)
            {
                progression.OnStateInterval();
                return $"Event {type} {progression.GetCurrentStateRef().GetNameFormatted()}";
            }

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
