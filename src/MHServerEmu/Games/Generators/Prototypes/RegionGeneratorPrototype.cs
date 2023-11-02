using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class RegionGeneratorPrototype : Prototype
    {
        public ulong[] POIGroups;
        public RegionGeneratorPrototype(Prototype proto) { FillPrototype(typeof(RegionGeneratorPrototype), proto); }

        public virtual ulong GetStartAreaRef(Region region) { return 0; }

        public virtual ulong GetAreasInGenerator() { return 0; }
    }
}
