using System.Runtime;
using Gazillion;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Populations;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("debug")]
    [CommandGroupDescription("Debug commands for development.")]
    public class DebugCommands : CommandGroup
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        [Command("test")]
        [CommandDescription("Runs test code.")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        public string Test(string[] @params, NetClient client)
        {
            return string.Empty;
        }

        [Command("forcegc")]
        [CommandDescription("Requests the garbage collector to perform a collection.")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        public string ForceGC(string[] @params, NetClient client)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            return "Manual garbage collection successfully requested.";
        }

        [Command("compactloh")]
        [CommandDescription("Requests the garbage collector to compact the large object heap (LOH) during the next full-blocking garbage collection.")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        public string CompactLoh(string[] @params, NetClient client)
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            return "LOH compaction requested.";
        }

        [Command("cell")]
        [CommandDescription("Shows current cell.")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Cell(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            return $"Current cell: {playerConnection.AOI.Region.GetCellAtPosition(avatar.RegionLocation.Position).PrototypeName}";
        }

        [Command("seed")]
        [CommandDescription("Shows current seed.")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Seed(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;

            return $"Current seed: {playerConnection.AOI.Region.RandomSeed}";
        }

        [Command("area")]
        [CommandDescription("Shows current area.")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Area(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            return $"Current area: {playerConnection.AOI.Region.GetCellAtPosition(avatar.RegionLocation.Position).Area.PrototypeName}";
        }

        [Command("region")]
        [CommandDescription("Shows current region.")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Region(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;

            return $"Current region: {playerConnection.AOI.Region.PrototypeName}";
        }

        public enum Switch
        {
            Off,
            On
        }

        [Command("setmarker")]
        [CommandUsage("debug setmarker [MarkerRef]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandParamCount(1)]
        public string SetMarker(string[] @params, NetClient client)
        {
            if (PrototypeId.TryParse(@params[0], out PrototypeId markerRef) == false)
                return $"Failed to parse MarkerRef {@params[0]}";

            PopulationManager.MarkerRef = markerRef;

            return $"SetMarker [{markerRef.GetNameFormatted()}]";
        }

        [Command("spawn")]
        [CommandUsage("debug spawn [on|off]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        public string Spawn(string[] @params, NetClient client)
        {
            if ((@params.Length > 0 && Enum.TryParse(@params[0], true, out Switch flags)) == false)
                flags = Switch.Off;   // Default Off

            PopulationManager.Debug = (flags == Switch.On) ? true : false;

            return $"Spawn Log [{flags}]";
        }

        [Command("ai")]
        [CommandUsage("debug ai")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string AI(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            EntityManager entityManager = playerConnection.Game.EntityManager;

            bool enableAI = entityManager.IsAIEnabled == false;
            entityManager.EnableAI(enableAI);
            return $"AI [{(enableAI ? "On" : "Off")}]";
        }

        [Command("metagame")]
        [CommandUsage("debug metagame [on|off]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        public string Metagame(string[] @params, NetClient client)
        {
            if ((@params.Length > 0 && Enum.TryParse(@params[0], true, out Switch flags)) == false)
                flags = Switch.Off;   // Default Off

            Games.MetaGames.MetaGame.Debug = (flags == Switch.On) ? true : false;

            return $"Metagame Log [{flags}]";
        }

        [Command("navi2obj")]
        [CommandDescription("Default PathFlags is Walk, can be [None|Fly|Power|Sight].")]
        [CommandUsage("debug navi2obj [PathFlags]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Navi2Obj(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;

            var region = playerConnection.AOI.Region;

            if ((@params.Length > 0 && Enum.TryParse(@params[0], true, out PathFlags flags)) == false)
                flags = PathFlags.Walk;   // Default Walk

            string filename = $"{region.PrototypeName}[{flags}].obj";
            string obj = region.NaviMesh.NaviCdt.MeshToObj(flags);
            FileHelper.SaveTextFileToRoot(filename, obj);
            return $"NaviMesh saved as {filename}";
        }

        [Command("crashgame")]
        [CommandDescription("Crashes the current game instance.")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string CrashGame(string[] @params, NetClient client)
        {
            throw new("Game instance crash invoked by a debug command.");
        }

        [Command("crashserver")]
        [CommandDescription("Crashes the entire server.")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.ServerConsole)]
        public string CrashServer(string[] @params, NetClient client)
        {
            throw new("Server crash invoked by a debug command.");
        }

        [Command("getconditionlist")]
        [CommandDescription("Gets a list of all conditions tracked by the ConditionPool in the current game.")]
        [CommandUserLevel(AccountUserLevel.Moderator)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string GetConditionList(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            string filePath = $"Download/Conditions_{DateTime.UtcNow.ToString(FileHelper.FileNameDateFormat)}.txt";

            playerConnection.SendMessage(NetMessageAdminCommandResponse.CreateBuilder()
                .SetResponse($"Saved condition list for the current game to {filePath}")
                .SetFilerelativepath(filePath)
                .SetFilecontents(playerConnection.Game.ConditionPool.GetConditionList())
                .Build());

            return string.Empty;
        }

        [Command("difficulty")]
        [CommandDescription("Shows information about the current difficulty level.")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Difficulty(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;

            Avatar avatar = playerConnection.Player?.CurrentAvatar;
            if (avatar == null || avatar.IsInWorld == false)
                return string.Empty;

            var region = avatar.Region;
            Vector3 position = avatar.RegionLocation.Position;
            TuningTable tuningTable = region.TuningTable;

            float playerToMob = tuningTable.GetDamageMultiplier(true, Rank.Popcorn, position);
            float mobToPlayer = tuningTable.GetDamageMultiplier(false, Rank.Player, position);

            return $"Region={region.Prototype}, TuningTable={tuningTable.Prototype}, playerToMob={playerToMob}, mobToPlayer={mobToPlayer}";
        }

        [Command("geteventpoolreport")]
        [CommandDescription("Returns a report representing the state of the ScheduledEventPool in the current game.")]
        [CommandUserLevel(AccountUserLevel.Moderator)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string GetEventPoolStatus(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection.Game;
            string reportString = game.GameEventScheduler.GetPoolReportString();

            string filePath = $"Download/ScheduledEventPoolReport_{DateTime.UtcNow.ToString(FileHelper.FileNameDateFormat)}.txt";

            playerConnection.SendMessage(NetMessageAdminCommandResponse.CreateBuilder()
                .SetResponse($"Saved scheduled event pool report for the current game to {filePath}")
                .SetFilerelativepath(filePath)
                .SetFilecontents(reportString)
                .Build());

            return string.Empty;
        }
    }
}
