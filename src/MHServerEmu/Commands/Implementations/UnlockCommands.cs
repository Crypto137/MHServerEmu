using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("unlock")]
    [CommandGroupDescription("Commands for unlocking various things.")]
    [CommandGroupUserLevel(AccountUserLevel.Admin)]
    public class UnlockCommands : CommandGroup
    {
        [Command("waypoints")]
        [CommandDescription("Unlocks all waypoints.")]
        [CommandUsage("unlock waypoints")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Waypoints(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            var player = playerConnection.Player;

            foreach (PrototypeId waypointRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<WaypointPrototype>(PrototypeIterateFlags.NoAbstract))
                player.UnlockWaypoint(waypointRef);

            return "Waypoints unlocked";
        }

        [Command("chapters")]
        [CommandDescription("Unlocks all chapters.")]
        [CommandUsage("unlock chapters")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Chapters(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            var player = playerConnection.Player;

            foreach (PrototypeId chapterRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<ChapterPrototype>(PrototypeIterateFlags.NoAbstract))
                player.UnlockChapter(chapterRef);

            return "Chapters unlocked";
        }
    }
}

