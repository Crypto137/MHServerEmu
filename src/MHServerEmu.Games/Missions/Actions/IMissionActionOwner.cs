
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Actions
{
    public interface IMissionActionOwner
    {
        PrototypeId PrototypeDataRef { get; }
        Region Region { get; }
    }
}
