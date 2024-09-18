using System.Text;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("power", "Provides commands for interacting with the power collection.", AccountUserLevel.Admin)]
    public class PowerCommands : CommandGroup
    {
        [Command("print", "Prints the power collection for the current avatar to the console.\nUsage: power print", AccountUserLevel.Admin)]
        public string Print(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            StringBuilder sb = new();
            sb.AppendLine($"------ Power Collection for Avatar {avatar} ------");
            foreach (var record in avatar.PowerCollection)
                sb.AppendLine(record.Value.ToString());
            sb.AppendLine($"Total Powers: {avatar.PowerCollection.PowerCount}");

            AdminCommandManager.SendAdminCommandResponseSplit(playerConnection, sb.ToString());
            return "Power collection information printed to the console.";
        }

        [Command("assign", "Assigns the specified power to the current avatar.\nUsage: power assign [pattern]", AccountUserLevel.Admin)]
        public string Assign(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help power assign' to get help.";

            PrototypeId powerProtoRef = CommandHelper.FindPrototype(HardcodedBlueprints.Power, @params[0], client);
            if (powerProtoRef == PrototypeId.Invalid) return string.Empty;

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            if (avatar.GetPower(powerProtoRef) != null)
                return $"Power {GameDatabase.GetPrototypeName(powerProtoRef)} is already assigned to the current avatar";

            if (avatar.AssignPower(powerProtoRef, new()) == null)
                return $"Failed to assign power {GameDatabase.GetPrototypeName(powerProtoRef)} to the current avatar";

            return $"Power {GameDatabase.GetPrototypeName(powerProtoRef)} assigned to the current avatar";
        }

        [Command("unassign", "Unassigns the specified power from the current avatar.\nUsage: power unassign [pattern]", AccountUserLevel.Admin)]
        public string Unassign(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help power unassign' to get help.";

            PrototypeId powerProtoRef = CommandHelper.FindPrototype(HardcodedBlueprints.Power, @params[0], client);
            if (powerProtoRef == PrototypeId.Invalid) return string.Empty;

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            if (avatar.GetPower(powerProtoRef) == null)
                return $"Power {GameDatabase.GetPrototypeName(powerProtoRef)} is not assigned to the current avatar";

            if (avatar.UnassignPower(powerProtoRef, new()) == false)
                return $"Failed to unassign power {GameDatabase.GetPrototypeName(powerProtoRef)} from the current avatar";

            return $"Power {GameDatabase.GetPrototypeName(powerProtoRef)} unassigned from the current avatar";
        }

        [Command("status", "Returns power status for the current avatar.\nUsage: power status", AccountUserLevel.Admin)]
        public string Status(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            PrototypeId activePowerRef = avatar.ActivePowerRef;
            PrototypeId continuousPowerRef = avatar.ContinuousPowerDataRef;

            return $"activePowerRef={GameDatabase.GetPrototypeName(activePowerRef)}, continuousPowerRef={GameDatabase.GetPrototypeName(continuousPowerRef)}";
        }
    }
}
