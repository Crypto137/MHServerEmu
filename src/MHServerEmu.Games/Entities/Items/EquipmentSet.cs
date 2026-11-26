#if GAME_VERSION_1_53
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities.Items
{
    public struct EquipmentSet
    {
        public int MaxEntryIndex;
        public List<PropertyCollection> PerEntryProperties;
    }
}
#endif
