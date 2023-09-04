using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.Properties;
using MHServerEmu.GameServer.Regions;
using MHServerEmu.Networking;

namespace MHServerEmu.Common.Commands
{
    [CommandGroup("tower", "Changes region to Avengers Tower (original).")]
    public class TowerCommand : CommandGroup
    {
        [DefaultCommand]
        public string Tower(string[] @params, FrontendClient client)
        {
            if (client == null)
                return "You can only invoke this command from the game.";

            client.CurrentGame.MovePlayerToRegion(client, RegionPrototype.AvengersTowerHUBRegion);

            return "Changing region to Avengers Tower (original)";
        }
    }

    [CommandGroup("costume", "Changes costume.")]
    public class CostumeCommand : CommandGroup
    {
        [DefaultCommand]
        public string Costume(string[] @params, FrontendClient client)
        {
            if (client == null)
                return "You can only invoke this command from the game.";

            if (@params.Length == 0) return "Invalid arguments. Type 'help costume' to get help.";

            try
            {
                // Try to parse costume prototype id from command
                ulong costumePrototypeId = ulong.Parse(@params[0]);

                // Create a new CostumeCurrent property
                Property property = new(0x19e0000000000000, 0);
                property.Value.Set(costumePrototypeId);

                // Get replication id for the client avatar
                ulong replicationId = (ulong)Enum.Parse(typeof(HardcodedAvatarReplicationId), Enum.GetName(typeof(HardcodedAvatarEntity), client.CurrentAvatar));

                // Send NetMessageSetProperty message
                client.SendMessage(1, new(property.ToNetMessageSetProperty(replicationId)));
                return $"Changing costume to {GameDatabase.GetPrototypePath(costumePrototypeId)}";
            }
            catch
            {
                return $"Failed to parse costume id {@params[0]}.";
            }            
        }
    }
}
