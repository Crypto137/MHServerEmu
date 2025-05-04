using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("boost")]
    [CommandGroupDescription("Commands for boosting the stats of the invoker player's current avatar.")]
    [CommandGroupUserLevel(AccountUserLevel.Admin)]
    public class BoostCommands : CommandGroup
    {
        [Command("damage")]
        [CommandDescription("Sets DamagePctBonus for the current avatar.")]
        [CommandUsage("boost damage [1-10000]")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Damage(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            if ((@params.Length > 0 && int.TryParse(@params[0], out int damage)) == false)
                damage = 1000;

            damage = Math.Clamp(damage, 1, 10000);
            avatar.Properties[PropertyEnum.DamagePctBonus] = (float)damage;

            return $"Damage x{damage}";
        }

        [Command("vsboss")]
        [CommandDescription("Sets DamagePctBonusVsBosses for the current avatar.")]
        [CommandUsage("boost vsboss [1-10000]")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string VsBoss(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            if ((@params.Length > 0 && int.TryParse(@params[0], out int vsboss)) == false)
                vsboss = 1000;

            vsboss = Math.Clamp(vsboss, 1, 10000);
            avatar.Properties[PropertyEnum.DamagePctBonusVsBosses] = (float)vsboss;

            return $"Damage vs Bosses x{vsboss}";
        }        
    }
}
