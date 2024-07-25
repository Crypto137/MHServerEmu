
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Missions.Actions
{
    public interface IMissionActionOwner
    {
        PrototypeId PrototypeDataRef { get; }
    }
}
