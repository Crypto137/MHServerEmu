using Gazillion;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("unlock", "Provides commands for unlock.", AccountUserLevel.User)]
    public class Unlock : CommandGroup
    {
        [Command("hero", "Unlocks the specified hero using Eternity Splinters.\nUsage: unlock hero [pattern]")]
        public string Hero(string[] @params, FrontendClient client)
        {
            // This command is intentionally exposed to regular users to allow them to unlock F4 heroes.
            // Also because of this, we call it "hero" and not "avatar" to make it clearer.

            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help unlock hero' to get help.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Player player = playerConnection.Player;

            PrototypeId avatarProtoRef = CommandHelper.FindPrototype((BlueprintId)GameDatabase.GlobalsPrototype.AvatarPrototype, @params[0], client);
            if (avatarProtoRef == PrototypeId.Invalid)
                return string.Empty;

            // Run the unlock through the roster code path to validate and pay costs, since this is not a debug command
            PurchaseUnlockResult result = player.PurchaseUnlock(avatarProtoRef);

            if (result != PurchaseUnlockResult.Success)
                return $"Failed to unlock {avatarProtoRef.GetNameFormatted()}: {result}.";

            return $"Unlocked {avatarProtoRef.GetNameFormatted()}.";
        }

        [Command("waypoints", "Unlock all waypoints.\nUsage: unlock waypoints", AccountUserLevel.Admin)]
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

