using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.PlayerManagement.Matchmaking
{
    /// <summary>
    /// Represents a match queue for a particular <see cref="RegionPrototype"/>.
    /// </summary>
    public class RegionRequestQueue
    {
        public RegionPrototype Prototype { get; }
        public PrototypeId PrototypeDataRef { get => Prototype.DataRef; }

        public RegionRequestQueue(RegionPrototype regionProto)
        {
            Prototype = regionProto;
        }
    }
}
