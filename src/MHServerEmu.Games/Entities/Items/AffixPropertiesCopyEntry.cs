using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities.Items
{
    public struct AffixPropertiesCopyEntry
    {
        public AffixPrototype AffixProto;
        public int LevelRequirement;
        public PropertyCollection Properties;
        public PropertyId PowerModifierPropertyId;  // PowerBoost / PowerGrantRank

        public AffixPropertiesCopyEntry()
        {
            AffixProto = null;
            LevelRequirement = 0;
            Properties = null;
            PowerModifierPropertyId = new();
        }
    }
}
