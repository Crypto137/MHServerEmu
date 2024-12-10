using MHServerEmu.Commands.Attributes;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("unlock", "Provides commands for unlock.", AccountUserLevel.User)]
    public class Unlock : CommandGroup
    {
        [Command("waypoints", "Unlock all waypoints.\nUsage: unlock waypoints")]
        public string Waypoints(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            var player = playerConnection.Player;

            foreach (PrototypeId waypointRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<WaypointPrototype>(PrototypeIterateFlags.NoAbstract))
                player.UnlockWaypoint(waypointRef);

            return "Waypoints unlocked";
        }

        [Command("chapters", "Unlock all chapters.\nUsage: unlock chapters")]
        public string Chapters(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            var player = playerConnection.Player;

            foreach (PrototypeId chapterRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<ChapterPrototype>(PrototypeIterateFlags.NoAbstract))
                player.UnlockChapter(chapterRef);

            return "Chapters unlocked";
        }
    }
}

