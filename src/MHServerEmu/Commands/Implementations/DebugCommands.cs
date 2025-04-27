using Gazillion;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Powers.Conditions;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("debug", "Debug commands for development.")]
    public class DebugCommands : CommandGroup
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        [Command("test", "Runs test code.")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        public string Test(string[] @params, FrontendClient client)
        {
            return string.Empty;
        }

        [Command("forcegc", "Requests the garbage collector to reclaim unused server memory.")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        public string ForceGC(string[] @params, FrontendClient client)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            return "Manual garbage collection successfully requested.";
        }

        [Command("cell", "Shows current cell.")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Cell(string[] @params, FrontendClient client)
        {
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            return $"Current cell: {playerConnection.AOI.Region.GetCellAtPosition(avatar.RegionLocation.Position).PrototypeName}";
        }

        [Command("seed", "Shows current seed.")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Seed(string[] @params, FrontendClient client)
        {
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            return $"Current seed: {playerConnection.AOI.Region.RandomSeed}";
        }

        [Command("area", "Shows current area.")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Area(string[] @params, FrontendClient client)
        {
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            return $"Current area: {playerConnection.AOI.Region.GetCellAtPosition(avatar.RegionLocation.Position).Area.PrototypeName}";
        }

        [Command("region", "Shows current region.")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Region(string[] @params, FrontendClient client)
        {
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            return $"Current region: {playerConnection.AOI.Region.PrototypeName}";
        }

        public enum Switch
        {
            Off,
            On
        }

        [Command("setmarker", "Usage: debug setmarker [MarkerRef].")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        public string SetMarker(string[] @params, FrontendClient client)
        {
            if (@params.Length == 0) return "Invalid arguments. Type 'help debug setmarker' to get help.";

            if (PrototypeId.TryParse(@params[0], out PrototypeId markerRef) == false)
                return $"Failed to parse MarkerRef {@params[0]}";

            PopulationManager.MarkerRef = markerRef;

            return $"SetMarker [{markerRef.GetNameFormatted()}]";
        }

        [Command("spawn", "Usage: debug spawn [on|off].")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        public string Spawn(string[] @params, FrontendClient client)
        {
            if ((@params.Length > 0 && Enum.TryParse(@params[0], true, out Switch flags)) == false)
                flags = Switch.Off;   // Default Off

            PopulationManager.Debug = (flags == Switch.On) ? true : false;

            return $"Spawn Log [{flags}]";
        }

        [Command("ai", "Usage: debug ai.")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string AI(string[] @params, FrontendClient client)
        {
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            EntityManager entityManager = playerConnection.Game.EntityManager;

            bool enableAI = entityManager.IsAIEnabled == false;
            entityManager.EnableAI(enableAI);
            return $"AI [{(enableAI ? "On" : "Off")}]";
        }

        [Command("metagame", "Usage: debug metagame [on|off].")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        public string Metagame(string[] @params, FrontendClient client)
        {
            if ((@params.Length > 0 && Enum.TryParse(@params[0], true, out Switch flags)) == false)
                flags = Switch.Off;   // Default Off

            Games.MetaGames.MetaGame.Debug = (flags == Switch.On) ? true : false;

            return $"Metagame Log [{flags}]";
        }

        [Command("navi2obj", "Usage: debug navi2obj [PathFlags].\n Default PathFlags is Walk, can be [None|Fly|Power|Sight].")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Navi2Obj(string[] @params, FrontendClient client)
        {
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            var region = playerConnection.AOI.Region;

            if ((@params.Length > 0 && Enum.TryParse(@params[0], true, out PathFlags flags)) == false)
                flags = PathFlags.Walk;   // Default Walk

            string filename = $"{region.PrototypeName}[{flags}].obj";
            string obj = region.NaviMesh.NaviCdt.MeshToObj(flags);
            FileHelper.SaveTextFileToRoot(filename, obj);
            return $"NaviMesh saved as {filename}";
        }

        [Command("crashgame", "Crashes the current game instance.")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string CrashGame(string[] @params, FrontendClient client)
        {
            throw new("Game instance crash invoked by a debug command.");
        }

        [Command("crashserver", "Crashes the current game instance.")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.ServerConsole)]
        public string CrashServer(string[] @params, FrontendClient client)
        {
            throw new("Server crash invoked by a debug command.");
        }

        [Command("getconditionlist", "Gets a list of all conditions tracked by the ConditionPool.")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string GetConditionList(string[] @params, FrontendClient client)
        {
            string filePath = $"Download/Conditions_{DateTime.UtcNow.ToString(FileHelper.FileNameDateFormat)}.txt";

            client.SendMessage(1, NetMessageAdminCommandResponse.CreateBuilder()
                .SetResponse($"Saved condition list for the current game to {filePath}")
                .SetFilerelativepath(filePath)
                .SetFilecontents(ConditionPool.Instance.GetConditionList())
                .Build());

            return string.Empty;
        }

        [Command("difficulty", "Shows information about the current difficulty level.")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Difficulty(string[] @params, FrontendClient client)
        {
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

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

        [Command("geteventpoolreport", "Returns a report representing the state of the ScheduledEventPool in the current game.")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string GetEventPoolStatus(string[] @params, FrontendClient client)
        {
            CommandHelper.TryGetGame(client, out Game game);
            string reportString = game.GameEventScheduler.GetPoolReportString();

            string filePath = $"Download/ScheduledEventPoolReport_{DateTime.UtcNow.ToString(FileHelper.FileNameDateFormat)}.txt";

            client.SendMessage(1, NetMessageAdminCommandResponse.CreateBuilder()
                .SetResponse($"Saved scheduled event pool report for the current game to {filePath}")
                .SetFilerelativepath(filePath)
                .SetFilecontents(reportString)
                .Build());

            return string.Empty;
        }
    }
}
