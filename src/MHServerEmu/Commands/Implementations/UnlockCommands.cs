using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("unlock")]
    [CommandGroupDescription("Commands for unlocking various things.")]
    public class UnlockCommands : CommandGroup
    {
        [Command("hero")]
        [CommandDescription("Unlocks the specified hero using Eternity Splinters.")]
        [CommandUsage("unlock hero [pattern]")]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Hero(string[] @params, NetClient client)
        {
            // This command is intentionally exposed to regular users to allow them to unlock F4 heroes.
            // Also because of this, we call it "hero" and not "avatar" to make it clearer.

            PlayerConnection playerConnection = (PlayerConnection)client;
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

        [Command("waypoints")]
        [CommandDescription("Unlocks all waypoints.")]
        [CommandUsage("unlock waypoints")]
        [CommandUserLevel(AccountUserLevel.Admin)]
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
        [CommandUserLevel(AccountUserLevel.Admin)]
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

