using MHServerEmu.GameServer.Regions;
using MHServerEmu.Networking;

namespace MHServerEmu.Common.Commands
{
    [CommandGroup("tower", "Changes region to Avengers Tower.")]
    public class TowerCommand : CommandGroup
    {
        [DefaultCommand]
        public string Tower(string[]? @params, FrontendClient? client)
        {
            if (client == null)
                return "You can only invoke this command from the game.";

            client.CurrentRegion = RegionPrototype.NPEAvengersTowerHUBRegion;
            client.SendMultipleMessages(1, RegionLoader.GetBeginLoadingMessages(client.CurrentRegion, client.CurrentAvatar));
            client.IsLoading = true;
            return "Changing region to Avengers Tower";
        }
    }
}
