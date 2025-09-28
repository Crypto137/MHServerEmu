using System.Text;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("power")]
    [CommandGroupDescription("Commands related to the power system.")]
    [CommandGroupUserLevel(AccountUserLevel.Admin)]
    public class PowerCommands : CommandGroup
    {
        [Command("print")]
        [CommandDescription("Prints the power collection for the current avatar to the console.")]
        [CommandUsage("power print")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Print(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            StringBuilder sb = new();
            sb.AppendLine($"------ Power Collection for Avatar {avatar} ------");
            foreach (var record in avatar.PowerCollection)
                sb.AppendLine(record.Value.ToString());
            sb.AppendLine($"Total Powers: {avatar.PowerCollection.PowerCount}");

            AdminCommandManager.SendAdminCommandResponseSplit(playerConnection, sb.ToString());
            return "Power collection information printed to the console.";
        }

        [Command("cooldownreset")]
        [CommandDescription("Resets all cooldowns and charges.")]
        [CommandUsage("power cooldownreset")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string CooldownReset(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;

            // Player cooldowns
            Player player = playerConnection.Player;
            foreach (PropertyEnum cooldownProperty in Property.CooldownProperties)
                player.Properties.RemovePropertyRange(cooldownProperty);

            // Avatar cooldowns
            Avatar avatar = player.CurrentAvatar;
            foreach (PropertyEnum cooldownProperty in Property.CooldownProperties)
                avatar.Properties.RemovePropertyRange(cooldownProperty);

            // Avatar charges
            Dictionary<PropertyId, PropertyValue> setDict = DictionaryPool<PropertyId, PropertyValue>.Instance.Get();
            foreach (var kvp in avatar.Properties.IteratePropertyRange(PropertyEnum.PowerChargesMax))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId powerProtoRef);
                if (powerProtoRef == PrototypeId.Invalid)
                    continue;

                setDict[new(PropertyEnum.PowerChargesAvailable, powerProtoRef)] = kvp.Value;
            }

            foreach (var kvp in setDict)
                avatar.Properties[kvp.Key] = kvp.Value;

            DictionaryPool<PropertyId, PropertyValue>.Instance.Return(setDict);

            return $"All cooldowns and charges have been reset.";
        }
    }
}
