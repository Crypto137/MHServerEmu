using MHServerEmu.Commands.Attributes;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Core.Memory;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("Dummy", "Provides commands for dummy spawn.", AccountUserLevel.Admin)]
    public class DummyCommands : CommandGroup
    {
        [Command("spawn", "Spawn boss without dummy.\nUsage: dummy spawn [pattern]")]
        public string Spawn(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help item give' to get help.";

            PrototypeId agentRef = CommandHelper.FindPrototype(HardcodedBlueprints.Agent, @params[0], client);
            if (agentRef == PrototypeId.Invalid) return string.Empty;
          
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Player player = playerConnection.Player;

            var manager = playerConnection.Game.EntityManager;

            var region = player.GetRegion();
            if (region.PrototypeDataRef != (PrototypeId)12181996598405306634) // TrainingRoomSHIELDRegion
                return "Player is not in Training Room";

            bool found = false;
            Agent dummy = null;
            foreach (var entity in region.Entities)
                if (entity.PrototypeDataRef == (PrototypeId)6534964972476177451)
                {
                    found = true;
                    dummy = entity as Agent;
                }

            if (found == false) return "Dummy is not found";
            dummy.SetDormant(true);

            using (EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>())
            using (PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>())
            {   
                var avatarProp = player.CurrentAvatar.Properties;
                int health = avatarProp[PropertyEnum.HealthMax];
                properties[PropertyEnum.HealthBase] = avatarProp[PropertyEnum.HealthBase];

                settings.EntityRef = agentRef;
                settings.Position = dummy.RegionLocation.Position;
                settings.Orientation = dummy.RegionLocation.Orientation;
                settings.RegionId = region.Id;

                var agent = manager.CreateEntity(settings);
                agent.Properties[PropertyEnum.HealthMax] = health;
                agent.Properties[PropertyEnum.Health] = health;
            }

            return string.Empty;
        }
    }
}
