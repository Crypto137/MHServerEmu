using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class RegionGeneratorPrototype : Prototype
    {
        public PrototypeId[] POIGroups { get; protected set; }

        //---

        public virtual PrototypeId GetStartAreaRef(Region region) { return PrototypeId.Invalid; }
        public virtual void GetAreasInGenerator(HashSet<PrototypeId> areas) { }
    }
}
