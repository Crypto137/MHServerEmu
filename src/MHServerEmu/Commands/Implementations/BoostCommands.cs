using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("boost", "Provides commands for boost.")]
    [CommandGroupUserLevel(AccountUserLevel.Admin)]
    public class BoostCommands : CommandGroup
    {
        [Command("damage", "Increase Damage of current avatar.\nUsage: boost damage [1-10000]")]
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

        [Command("vsboss", "Increase Damage vs Bosses of current avatar.\nUsage: boost vsboss [1-10000]")]
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
