using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class RegionGeneratorPrototype : Prototype
    {
        public ulong[] POIGroups;
        public RegionGeneratorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionGeneratorPrototype), proto); }

        public virtual ulong GetStartAreaRef(Region region) { return 0; }

        public virtual ulong GetAreasInGenerator() { return 0; }
    }
}
